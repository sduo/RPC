
using Microsoft.Extensions.Primitives;
using RPC.Logger.Configuration;
using System.IO.Compression;
using System.Net.Http.Headers;
using System.Text;
using Yarp.ReverseProxy.Model;

namespace RPC.Logger
{
    public class RpcLoggerMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<RpcLoggerMiddleware> _logger;
        private readonly IConfiguration _configuration;
        private readonly RpcLogStore _store;

        public RpcLoggerMiddleware(RequestDelegate next,
            IConfiguration configuration,
            ILogger<RpcLoggerMiddleware> logger)
        {
            _next = next ?? throw new ArgumentNullException(nameof(next));
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _store = new RpcLogStore(configuration.GetSection("LogStore"));
        }

        private IConfigurationSection? GetConfigurationSection(IConfigurationSection rpc, string route)
        {
            string? id = _configuration.GetValue<string?>($"YARP:Routes:{route}:RpcId", null);
            if (string.IsNullOrEmpty(id) || string.Equals("disable", id, StringComparison.OrdinalIgnoreCase)) { return null; }
            return Utils.GetChildSection(rpc, id);
        }

        private static void LogReverseProxyFeature(IReverseProxyFeature rpf, Dictionary<string, string> log, IConfigurationSection rpc)
        {

            if (rpc.GetValue("RouteId", true))
            {
                log.TryAdd("Route Id", rpf.Route.Config.RouteId);
            }

            if (rpc.GetValue("ClusterId", true))
            {
                log.TryAdd("Cluster Id", rpf.Cluster.Config.ClusterId);
            }

            if (rpf.ProxiedDestination == null) { return; }
            if (rpc.GetValue("DestinationId", true))
            {
                log.TryAdd("Destination Id", rpf.ProxiedDestination.DestinationId);
            }

            if (rpc.GetValue("DestinationAddress", true))
            {
                log.TryAdd("Destination Address", rpf.ProxiedDestination.Model.Config.Address);
            }
        }

        private static async Task LogRequest(HttpContext context, Dictionary<string, string> log, IConfigurationSection rpc)
        {
            if (rpc.GetValue("TraceId", true))
            {
                log.TryAdd("Trace Id", context.TraceIdentifier);
            }

            if (rpc.GetValue("ConnectionId", true))
            {
                log.TryAdd("Connection Id", context.Connection.Id);
            }

            if (rpc.GetValue("IpFamily", false))
            {
                log.TryAdd("Ip Family", $"{context.Connection.RemoteIpAddress?.AddressFamily}");
            }

            if (rpc.GetValue("Ip", true))
            {
                log.TryAdd("Ip Address", $"{context.Connection.RemoteIpAddress}");
            }

            if (rpc.GetValue("Host", true))
            {
                log.TryAdd("Host", $"{context.Request.Host}");
            }

            if (rpc.GetValue("Path", true))
            {
                log.TryAdd("Path", $"{context.Request.Path}");
            }

            if (rpc.GetValue("Scheme", true))
            {
                log.TryAdd("Scheme", $"{context.Request.Scheme}");
            }

            if (rpc.GetValue("Protocol", true))
            {
                log.TryAdd("Protocol", $"{context.Request.Protocol}");
            }

            if (rpc.GetValue("Method", true))
            {
                log.TryAdd("Method", $"{context.Request.Method}");
            }

            
            LogCollection(context.Request.Query, log, Utils.GetChildSection(rpc.GetSection("Request"), "Query"), "Request Query");
            LogCollection(context.Request.Headers, log, Utils.GetChildSection(rpc.GetSection("Request"), "Header"), "Request Header");

            await LogRequestBody(context, log, rpc);
        }        

        private static void LogTimestamp(string point,Dictionary<string,string> log)
        {
            if (string.IsNullOrEmpty(point)) { return; }
            log.TryAdd(point, $"{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}");
        }

        private static void LogCollection(IEnumerable<KeyValuePair<string, StringValues>> collection, Dictionary<string, string> log, IConfigurationSection? rpc, string name)
        {
            if (rpc == null) { return; }
            Dictionary<string, IEnumerable<string>> remainder = LogCollection(collection, log, rpc);
            log.TryAdd(name, Utils.ToJson(remainder));
        }

        private static Dictionary<string, IEnumerable<string>> LogCollection(IEnumerable<KeyValuePair<string, StringValues>> collection, Dictionary<string, string> log, IConfigurationSection configuration)
        {
            Dictionary<string, IEnumerable< string>> remainder = new();
            if (collection?.Any() == true)
            {
                List<Match>? blockList = configuration?.GetSection("Block").Get<List<Match>>();
                List<Mask>? maskList = configuration?.GetSection("Mask").Get<List<Mask>>();
                List<Map>? exposeList = configuration?.GetSection("Expose").Get<List<Map>>();
                foreach (var item in collection)
                {
                    if (blockList?.Any(x => x.IsMatch(item.Key)) == true)
                    {
                        continue;
                    }
                    Map? expose = exposeList?.FirstOrDefault(x => x.IsMatch(item.Key));
                    if (expose != null)
                    {
                        log.TryAdd(expose.MapName(item.Key)!, Utils.ToJson(item.Value.Select(x=>expose.GetMask(x))));
                        continue;
                    }
                    Mask? mask = maskList?.FirstOrDefault(x => x.IsMatch(item.Key));
                    remainder.TryAdd(item.Key, item.Value.Select(x => mask?.GetMask(x) ?? x));
                }
            }
            return remainder;
        }

        private static bool TryLogRequestBodyAsForm(HttpContext context, Dictionary<string, string> log, IConfigurationSection? rpc)
        {
            if(rpc == null) { return false; }
            if (!context.Request.HasFormContentType) { return false; }

            LogCollection(context.Request.Form, log, rpc, "Request Body");

            if (rpc.GetValue("File", false))
            {
                log.Add("Request File", Utils.ToJson(context.Request.Form.Files.Select(x => new { x.FileName, x.Length, x.Name })));
            }
            return true;
        }

        private static async Task<bool> TryLogRequestBodyAsText(HttpContext context, Dictionary<string, string> log, IConfigurationSection? rpc)
        {
            if (rpc == null) { return false; }
            string[]? types = rpc.GetSection("Type").Get<string[]?>();
            MediaTypeHeaderValue? media = MatchMediaType(context.Request.ContentType, types);
            if (media==null) { return false; }
            Encoding encoding = Utils.GetEncoding(media?.CharSet ?? rpc.GetValue<string?>("Charset"), Encoding.UTF8);

            using MemoryStream ms = new();
            await context.Request.Body.CopyToAsync(ms);
            ms.Seek(0, SeekOrigin.Begin);

            using StreamReader sr = new(ms, encoding);
            log.Add("Request Body", await sr.ReadToEndAsync());
            
            return true;
        }

        private static MediaTypeHeaderValue? MatchMediaType(string? content, string[]? types)
        {
            if(string.IsNullOrEmpty(content)) { return null; }            
            if (types == null || types.Length == 0) { return null; }
            if (!MediaTypeHeaderValue.TryParse(content, out MediaTypeHeaderValue? media)) { return null; }
            if (string.IsNullOrEmpty(media?.MediaType)) { return null; }
            if (!types.Any(x => string.Equals(x, media.MediaType, StringComparison.OrdinalIgnoreCase))) { return null; }
            return media;
        }

        private static async Task<bool> TryLogRequestBodyAsRaw(HttpContext context, Dictionary<string, string> log, IConfigurationSection? rpc)
        {
            if (rpc == null) { return false; }
            string[]? types = rpc.GetSection("Type").Get<string[]?>();
            if (types?.Length > 0)
            {
                MediaTypeHeaderValue? media = MatchMediaType(context.Request.ContentType, types);
                if (media == null) { return false; }
            }            

            using MemoryStream ms = new();
            await context.Request.Body.CopyToAsync(ms);
            ms.Seek(0, SeekOrigin.Begin);
            log.Add("Request Body", rpc.GetValue("Base64", false) ? Convert.ToBase64String(ms.ToArray()) : Convert.ToHexString(ms.ToArray()));
            return true;
        }

        private static async Task LogRequestBody(HttpContext context, Dictionary<string, string> log, IConfigurationSection rpc)
        {
            var section = Utils.GetChildSection(rpc.GetSection("Request"), "Body");
            if (section == null) { return; }
            if (!context.Request.Body.CanRead) { return; }
            
            try
            {
                context.Request.EnableBuffering();              

                context.Request.Body.Seek(0, SeekOrigin.Begin);

                if(TryLogRequestBodyAsForm(context, log, Utils.GetChildSection(section, "Form"))) { return; }
                if(await TryLogRequestBodyAsText(context, log, Utils.GetChildSection(section, "Text"))) { return; }
                if(await TryLogRequestBodyAsRaw(context, log, Utils.GetChildSection(section, "Raw"))) { return; }
            }
            finally
            {
                context.Request.Body.Seek(0, SeekOrigin.Begin);
            }
        }

        private static async Task LogResponse(HttpContext context, Dictionary<string, string> log, IConfigurationSection rpc, Stream? response)
        {
            if (rpc.GetValue("Code", true))
            {
                log.TryAdd("Status Code", $"{context.Response.StatusCode}");
            }
            LogCollection(context.Response.Headers, log, Utils.GetChildSection(rpc.GetSection("Response"), "Header"), "Response Header");
            await LogResponseBody(context, log, rpc,response);            
        }

        private static async Task LogResponseBody(HttpContext context, Dictionary<string, string> log, IConfigurationSection rpc, Stream? response)
        {
            if(response == null) { return; }
            var section = Utils.GetChildSection(rpc.GetSection("Response"), "Body");
            if(section == null) { return; }
            if (!context.Response.Body.CanRead) { return; }

            try
            {
                context.Response.Body.Seek(0, SeekOrigin.Begin);
                
                if (await TryLogResponseBodyAsText(context, log, Utils.GetChildSection(section, "Text"))) { return; }
                if (await TryLogResponseBodyAsRaw(context, log, Utils.GetChildSection(section, "Raw"))) { return; }                
            }
            finally
            {
                context.Response.Body.Seek(0, SeekOrigin.Begin);
                await context.Response.Body.CopyToAsync(response);
            }
        }

        private static async Task<bool> TryBrotliDecompress(Stream source, Dictionary<string, string> log, Encoding encoding, IHeaderDictionary headers)
        {
            if (!string.Equals(headers.ContentEncoding, "br", StringComparison.OrdinalIgnoreCase)){ return false; }            
            using MemoryStream target = new();
            source.Seek(0, SeekOrigin.Begin);
            using BrotliStream bs = new (source, CompressionMode.Decompress);
            await bs.CopyToAsync(target);
            target.Seek(0, SeekOrigin.Begin);
            using StreamReader sr = new(target, encoding);
            log.Add("Response Body", await sr.ReadToEndAsync());
            return true;
        }

        private static async Task<bool> TryGZipDecompress(Stream source, Dictionary<string, string> log, Encoding encoding, IHeaderDictionary headers)
        {
            if (!string.Equals(headers.ContentEncoding, "gzip", StringComparison.OrdinalIgnoreCase)) { return false; }
            using MemoryStream target = new();
            source.Seek(0, SeekOrigin.Begin);
            using GZipStream gzip = new(source, CompressionMode.Decompress);
            await gzip.CopyToAsync(target);
            target.Seek(0, SeekOrigin.Begin);
            using StreamReader sr = new(target, encoding);
            log.Add("Response Body", await sr.ReadToEndAsync());
            return true;
        }

        private static async Task<bool> TryDeflateDecompress(Stream source, Dictionary<string, string> log, Encoding encoding, IHeaderDictionary headers)
        {
            if (!string.Equals(headers.ContentEncoding, "deflate", StringComparison.OrdinalIgnoreCase)) { return false; }
            using MemoryStream target = new();
            source.Seek(0, SeekOrigin.Begin);
            using DeflateStream gzip = new(source, CompressionMode.Decompress);
            await gzip.CopyToAsync(target);
            target.Seek(0, SeekOrigin.Begin);
            using StreamReader sr = new(target, encoding);
            log.Add("Response Body", await sr.ReadToEndAsync());
            return true;
        }

        private static async Task<bool> TryLogResponseBodyAsText(HttpContext context, Dictionary<string, string> log, IConfigurationSection? rpc) 
        {
            if (rpc == null) { return false; }
            string[]? types = rpc.GetSection("Type").Get<string[]?>();
            MediaTypeHeaderValue? media = MatchMediaType(context.Response.ContentType, types);
            if (media == null) { return false; }
            Encoding encoding = Utils.GetEncoding(media?.CharSet ?? rpc.GetValue<string?>("Charset"), Encoding.UTF8);


            using MemoryStream ms = new();
            await context.Response.Body.CopyToAsync(ms);

            if (rpc.GetValue("Brotli", true) && await TryBrotliDecompress(ms, log, encoding, context.Response.Headers))
            {
                return true;
            }

            if (rpc.GetValue("GZip", true) && await TryGZipDecompress(ms, log, encoding, context.Response.Headers))
            {
                return true;
            }

            if (rpc.GetValue("Deflate", true) && await TryDeflateDecompress(ms, log, encoding, context.Response.Headers))
            {
                return true;
            }

            ms.Seek(0, SeekOrigin.Begin);
            using StreamReader sr = new(ms, encoding);
            log.Add("Response Body", await sr.ReadToEndAsync());
            return true;
        }

        private static async Task<bool> TryLogResponseBodyAsRaw(HttpContext context, Dictionary<string, string> log, IConfigurationSection? rpc)
        {
            if (rpc == null) { return false; }
            string[]? types = rpc.GetSection("Type").Get<string[]?>();
            if (types?.Length > 0)
            {
                MediaTypeHeaderValue? media = MatchMediaType(context.Response.ContentType, types);
                if (media == null) { return false; }
            }

            using MemoryStream ms = new();
            await context.Response.Body.CopyToAsync(ms);
            ms.Seek(0, SeekOrigin.Begin);
            log.Add("Response Body", rpc.GetValue("Base64", false) ? Convert.ToBase64String(ms.ToArray()) : Convert.ToHexString(ms.ToArray()));
            return true;
        }

        private static Stream? PrepareResponseBodyStream(HttpContext context, IConfigurationSection rpc)
        {
            var section = Utils.GetChildSection(rpc.GetSection("Response"), "Body");
            if (section == null) { return null; }
            Stream response = context.Response.Body;
            context.Response.Body = new MemoryStream();
            return response;             
        }

        public async Task Invoke(HttpContext context)
        {
            var root = _configuration.GetSection("YARP:RPC");
            if (!root.Exists())
            {
                await _next.Invoke(context);
                return;
            }

            IReverseProxyFeature rpf = context.GetReverseProxyFeature();

            IConfigurationSection? rpc = GetConfigurationSection(root, rpf.Route.Config.RouteId);
            if (rpc == null)
            {
                await _next.Invoke(context);
                return;
            }            

            Dictionary<string, string> log = new();
            LogTimestamp("TS1", log);
            
            await LogRequest(context, log, rpc);

            Stream? response = PrepareResponseBodyStream(context, rpc);
            LogTimestamp("TS2", log);
            await _next.Invoke(context);
            LogTimestamp("TS3", log);

            await LogResponse(context,log,rpc, response);
            LogTimestamp("TS4", log);

            LogReverseProxyFeature(rpf, log, rpc);

            LogTimestamp("TS5", log);

            await _store.Log(log, rpf);
        }
    }
}
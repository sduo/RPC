using Aliyun.Api.LogService;
using Aliyun.Api.LogService.Domain.Log;
using Yarp.ReverseProxy.Model;

namespace RPC.Logger.Provider
{
    public class AliCloudSlsRpcLogProvider : IRpcLogProvider
    {

        private ILogServiceClient _sls;
        private string? _store;

        public AliCloudSlsRpcLogProvider(IConfiguration configuration)
        {
            string? endpoint = configuration.GetValue<string?>("Endpoint", null);
            if (string.IsNullOrEmpty(endpoint)) { throw new ArgumentNullException("Endpoint"); }

            string? project = configuration.GetValue<string?>("Project", null); 
            if (string.IsNullOrEmpty(project)) { throw new ArgumentNullException("Project"); }

            string? ak = configuration.GetValue<string?>("AK", null);
            if (string.IsNullOrEmpty(ak)) { throw new ArgumentNullException("AK"); }

            string? sk = configuration.GetValue<string?>("SK", null);
            if (string.IsNullOrEmpty(sk)) { throw new ArgumentNullException("sk"); }

            _store = configuration.GetValue<string?>("Store", null);
            if (string.IsNullOrEmpty(_store)) { throw new ArgumentNullException("Store"); }

            _sls = LogServiceClientBuilders.HttpBuilder.Endpoint(endpoint, project).Credential(ak, sk).Build();
        }

        public async Task OnLog(Dictionary<string, string> log, IReverseProxyFeature rpf)
        {
            var submit = await _sls.PostLogStoreLogsAsync(new (_store, new ()
            {
                Logs = new List<LogInfo>() {
                        new () {
                            Contents=log,
                            Time=DateTimeOffset.UtcNow
                        }
                },
                Source = rpf.Cluster.Config.ClusterId,
                Topic = rpf.Route.Config.RouteId
            }));

            submit.EnsureSuccess();
        }

    }
}

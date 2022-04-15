using System.Text.Json;
using System.Text.Encodings.Web;
using System.Text;

namespace RPC
{
    public sealed class Utils
    {
        public static readonly Dictionary<string, string> CommondLineArgumentsMap = new()
        {
            { "-config", "configuration" }
        };

        public static JsonSerializerOptions CreateJsonSerializerOptions()
        {
            return new JsonSerializerOptions()
            {
                Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
            };
        }

        public static IConfigurationSection? GetChildSection(IConfiguration configuration,string key,bool ignorCase=true)
        {
            return configuration.GetChildren().FirstOrDefault(x => string.Equals(x.Key, key, ignorCase ? StringComparison.OrdinalIgnoreCase:StringComparison.Ordinal));
        }

        public static Encoding GetEncoding(string? charset) => GetEncoding(charset,Encoding.UTF8);

        public static Encoding GetEncoding(string? charset,Encoding @default)
        {
            if (string.IsNullOrEmpty(charset)) { return @default; }
            try
            {
                return Encoding.GetEncoding(charset);
            }
            catch 
            {
                return @default;
            }
        }


        public static string ToJson<T>(T value, Action<JsonSerializerOptions>? configure = null)
        {
            JsonSerializerOptions options = CreateJsonSerializerOptions();
            configure?.Invoke(options);
            return JsonSerializer.Serialize(value, options);
        }


    }
}

using RPC.Logger.Provider;
using System.Diagnostics.CodeAnalysis;
using Yarp.ReverseProxy.Model;

namespace RPC.Logger
{
    public class RpcLogStore
    {
        public event OnLog? OnLog;
        public RpcLogStore(IConfiguration configuration)
        {
            foreach(IConfigurationSection child in configuration.GetChildren())
            {
                string? type = child.GetValue<string?>("Type", null);
                if (string.IsNullOrEmpty(type)) { continue; }
                if(!Enum.TryParse(type, true, out RpcLogProvider provider)) { continue; }
                switch (provider)
                {
                    case RpcLogProvider.Console:
                        {
                            OnLog += new ConsoleRpcLogProvider().OnLog;
                            break;
                        }
                    case RpcLogProvider.AliCloudSLS:
                        {
                            OnLog += new AliCloudSlsRpcLogProvider(child.GetSection("Configuration")).OnLog;
                            break;
                        }
                }                
            }                     
        }

        public async Task Log(Dictionary<string, string> log, IReverseProxyFeature rpf)
        {
            if(OnLog == null) { return; }
            try
            {
                await OnLog.Invoke(log, rpf);
            }
            finally
            {

            }            
        }
    }
}

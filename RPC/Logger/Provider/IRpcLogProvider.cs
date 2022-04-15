using Yarp.ReverseProxy.Model;

namespace RPC.Logger.Provider
{

    public enum RpcLogProvider
    {
        Console, AliCloudSLS
    }

    public delegate Task OnLog(Dictionary<string, string> log, IReverseProxyFeature rpf);
    public interface IRpcLogProvider
    {
        Task OnLog(Dictionary<string, string> log, IReverseProxyFeature rpf);
    }
}

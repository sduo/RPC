using Yarp.ReverseProxy.Model;

namespace RPC.Logger.Provider
{
    public class ConsoleRpcLogProvider: IRpcLogProvider
    {
        public Task OnLog(Dictionary<string, string> log, IReverseProxyFeature rpf)
        {
            Console.WriteLine(Utils.ToJson(log));
            return Task.CompletedTask;
        }
    }
}

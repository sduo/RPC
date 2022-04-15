using RPC;
using RPC.CORS;
using RPC.Logger;
using System.Text;

Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

var builder = WebApplication.CreateBuilder(args);

builder.Host.ConfigureLogging(logging => {
    logging.ClearProviders();
    logging.AddConsole();
});

builder.Host.ConfigureAppConfiguration(configuration => {
    configuration.Sources.Clear();    
    configuration.AddEnvironmentVariables("RPC_");
    configuration.AddCommandLine(args,Utils.CommondLineArgumentsMap);   
    var json = builder.Configuration.GetValue<string>("configuration", "appsettings.json");
    configuration.AddJsonFile(json,false,true);
    var secret = builder.Configuration.GetValue<string?>("secret", null);
    if (!string.IsNullOrEmpty(secret)) 
    {
        configuration.AddUserSecrets(secret,true);
    }
    var nacos = builder.Configuration.GetSection("NACOS");
    if (nacos.Exists())
    {
        configuration.AddNacosV2Configuration(nacos);
    }
});

builder.Host.ConfigureServices(services => {
    var yarp = builder.Configuration.GetRequiredSection("YARP");
    services.LoadCorsFromConfig(yarp.GetSection("CORS")).AddReverseProxy().LoadFromConfig(yarp);    
});

var app = builder.Build();

app.UseCors();
app.MapReverseProxy(proxy => {
    proxy.UseMiddleware<RpcLoggerMiddleware>(); 
});

app.Run();

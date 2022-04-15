namespace RPC.CORS
{   
    public static class PolicyExtensions
    {
        public static IServiceCollection LoadCorsFromConfig(this IServiceCollection services, IConfigurationSection config)
        {
            if (!config.Exists()) { return services; }
            var cors = config.GetChildren().Where(x=>!string.IsNullOrEmpty(x.Key));
            if (!cors.Any()) { return services; }

            services.AddCors(setup => {
                foreach (var policy in cors)
                {
                    setup.AddPolicy(policy.Key, cp => {
                        var origins = policy.GetSection("Origins").Get<string[]>();
                        if (origins == null) { cp.AllowAnyOrigin(); } else { cp.WithOrigins(origins); }
                        var methods = policy.GetSection("Methods").Get<string[]>();
                        if (methods == null) { cp.AllowAnyMethod(); } else { cp.WithMethods(methods); }
                        var headers = policy.GetSection("Headers").Get<string[]>();
                        if (headers == null) { cp.AllowAnyHeader(); } else { cp.WithHeaders(headers); }
                        var exposed_headers = policy.GetSection("ExposedHeaders").Get<string[]>();
                        if (exposed_headers != null) { cp.WithExposedHeaders(exposed_headers); }
                    });
                }
            });

            return services;
        }
    }
}

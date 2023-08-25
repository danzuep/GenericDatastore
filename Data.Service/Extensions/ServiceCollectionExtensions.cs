using Data.Mongo;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.Diagnostics.CodeAnalysis;

namespace Data.Service.Extensions
{
    [ExcludeFromCodeCoverage]
    internal static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddJobScheduler(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddDatastoreWithOptions(configuration);
            //using var serviceProvider = services.BuildServiceProvider();
            //using var scope = serviceProvider.CreateScope();
            return services;
        }

        public static IServiceCollection AddCorsAllowedOrigins(this IServiceCollection services, IConfiguration configuration, string? corsName = null)
        {
            var headers = new string[] { "Grpc-Status", "Grpc-Message", "Grpc-Encoding", "Grpc-Accept-Encoding" };
            if (string.IsNullOrEmpty(corsName))
            {
                services.AddCors(options => options
                    .AddDefaultPolicy(policy => policy
                        .AllowAnyOrigin()
                        .AllowAnyMethod()
                        .AllowAnyHeader()
                        .WithExposedHeaders(headers)));
            }
            else
            {
                var allowedHosts = configuration["AllowedHosts"]
                    ?.Split(';', StringSplitOptions.RemoveEmptyEntries);
                var allowedOrigins = allowedHosts?.Length > 0 ? allowedHosts :
                    new string[] { "localhost" };
                services.AddCors(options => options
                    .AddPolicy(corsName, policy => policy.WithOrigins(allowedOrigins)
                        .SetIsOriginAllowedToAllowWildcardSubdomains()
                        .AllowAnyOrigin()
                        .AllowAnyMethod()
                        .AllowAnyHeader()
                        .WithExposedHeaders(headers)));
            }
            return services;
        }
    }
}

using Data.Service.Extensions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Data.Service
{
    public sealed class Startup
    {
        private readonly IConfiguration _configuration;

        public Startup(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddHttpsRedirection(options =>
                options.RedirectStatusCode = StatusCodes.Status308PermanentRedirect);
            services.AddHttpLogging();

            services.AddAuthentication();
            services.AddAuthorization();

            services.AddCorsAllowedOrigins(_configuration, null);

            services.AddGrpc();
            services.AddGrpcReflection();

            services.AddJobScheduler(_configuration);
            // https://learn.microsoft.com/en-us/dotnet/core/extensions/scoped-service?pivots=dotnet-7-0
            //services.AddScoped<IScopedProcessingService, ApiService>();
            //services.AddHostedService<ScopedBackgroundService>();
        }

        public static void Configure(IApplicationBuilder app)
        {
            app.UseHttpsRedirection();
            app.UseHttpLogging();

            // UseCors must be before UseResponseCaching if used
            app.UseCors();
            //app.UseHealthChecks("/healthcheck");

            app.UseRouting();
            app.UseAuthentication();
            app.UseAuthorization();
            // UseGrpcWeb must be added between UseRouting and UseEndpoints
            //app.UseGrpcWeb();
            app.UseEndpoints(endpoints =>
            {
                //endpoints.MapGrpcService<ServiceImplementation>().EnableGrpcWeb();
                endpoints.MapGrpcReflectionService();
            });
        }
    }
}

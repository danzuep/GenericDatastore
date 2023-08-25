using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using System.Diagnostics.CodeAnalysis;

namespace Data.Service
{
    [ExcludeFromCodeCoverage]
    public static class Program
    {
        public static async Task Main(string[] args)
        {
            var builder = CreateHostBuilder<Startup>(args);
            using var host = builder.Build();
            await host.RunAsync();
        }

        private static IHostBuilder CreateHostBuilder<TStartup>(string[] args) where TStartup : class =>
            Host.CreateDefaultBuilder()
                .ConfigureAppConfiguration((_, config) => config.AddCommandLine(args))
                .ConfigureWebHostDefaults(webBuilder => webBuilder.UseKestrel()
                .UseStartup<TStartup>());
    }
}
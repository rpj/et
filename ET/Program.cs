using System;
using System.IO;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ET.Config;

namespace ET
{
    public static class Program
    {
        private static readonly KeyVault KeyVault = new KeyVault();

        public static void Main(string[] args)
        {
            if ((args.Length > 0 && args[0] == "monitor") ||
                Environment.GetEnvironmentVariable("ET_RUN_MONITOR") == "1")
            {
                Console.WriteLine($"Entering monitor mode (via the " +
                    $"{((args.Length > 0 && args[0] == "monitor") ? "CLI" : "environment")})...");

                var builder = new ConfigurationBuilder()
                 .SetBasePath(Directory.GetCurrentDirectory())
                 .AddEnvironmentVariables()
                 .AddJsonFile("appsettings.json", optional: true, reloadOnChange: false)
                 .AddJsonFile($"appsettings.{Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")}.json");

                Console.WriteLine("Configuring and querying Azure Key Vault...");
                KeyVault.AddKeyVaultToBuilder(builder);

                IConfiguration configuration = builder.Build();

                Console.WriteLine("Configuring and building application runtime...");
                var svcProv = new ServiceCollection()
                    .AddLogging()
                    .AddSingleton<IMonitorMode, MonitorMode>()
                    .AddSingleton(configuration)
                    .Configure<AzureConfig>(configuration.GetSection("Azure"))
                    .BuildServiceProvider();

                svcProv.GetService<IMonitorMode>().Run();
            }
            else
            {
                CreateWebHostBuilder(args).Build().Run();
            }
        }

        private static IWebHostBuilder CreateWebHostBuilder(string[] args) =>
            WebHost.CreateDefaultBuilder(args)
                .ConfigureLogging((context, config) =>
                {
                    config.AddConsole();
                    
                    if (context.HostingEnvironment.IsDevelopment())
                    {
                        config.AddEventSourceLogger();
#if DEBUG
                        config.AddDebug();
                        Console.WriteLine("DEBUG is enabled");
#endif
                    }
                })
                .ConfigureAppConfiguration((context, config) => { KeyVault.AddKeyVaultToBuilder(config); })
                .ConfigureServices((context, services) => { services.AddSingleton<IKeyVault>(KeyVault); })
                .UseStartup<Startup>();
    }
}

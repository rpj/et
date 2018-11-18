using System;
using System.IO;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ET.Config;
using ET.Controllers;

namespace ET
{
    public static class Program
    {
        private static readonly IKeyVault KeyVault = new KeyVault();

        public static void Main(string[] args)
        {
            Console.WriteLine("Starting...");
#if DEBUG
            Console.WriteLine("DEBUG is enabled");
#endif

            Action runLambda = null;

            if ((args.Length > 0 && args[0] == "monitor") ||
                Environment.GetEnvironmentVariable("ET_RUN_MONITOR") == "1")
            {
                if (Environment.GetEnvironmentVariable("ET_DEPLOYED") == "1")
                    throw new Exception("");

                Console.WriteLine($"Entering monitor mode (via the " +
                    $"{((args.Length > 0 && args[0] == "monitor") ? "CLI" : "environment")})...");

                var ancEnv = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
                var builder = new ConfigurationBuilder()
                 .SetBasePath(Directory.GetCurrentDirectory())
                 .AddEnvironmentVariables()
                 .AddJsonFile("appsettings.json", optional: false)
                 .AddJsonFile($"appsettings.{ancEnv}.json", optional: true);

                Console.WriteLine("Configuring and querying Azure Key Vault...");
                KeyVault.AddKeyVaultToBuilder(builder);
                IConfiguration configuration = builder.Build();

                Console.WriteLine("Configuring and building application runtime...");
                var svcProv = new ServiceCollection()
                    .AddLogging()
                    .AddSingleton<IMonitorMode, MonitorMode>()
                    .AddSingleton<IRedisController, RedisController>()
                    .AddSingleton(KeyVault)
                    .AddSingleton(configuration)
                    .Configure<AzureConfig>(configuration.GetSection("Azure"))
                    .BuildServiceProvider();

                runLambda = () => { svcProv.GetService<IMonitorMode>().Run(); };
            }
            else
            {
                Console.WriteLine("Configuring web hosting environment...");
                runLambda = () => { CreateWebHostBuilder(args).Build().Run(); };
            }

            if (runLambda != null)
                runLambda();
            else
                throw new Exception("Ended without anything to run!");
        }

        private static IWebHostBuilder CreateWebHostBuilder(string[] args) =>
            WebHost.CreateDefaultBuilder(args)
                .ConfigureLogging((context, config) =>
                {
                    Console.WriteLine("Configuring logging...");
                    config.AddConsole();
                    
                    if (context.HostingEnvironment.IsDevelopment())
                    {
                        config.AddEventSourceLogger();
#if DEBUG
                        config.AddDebug();
#endif
                    }
                })
                .ConfigureAppConfiguration((context, config) => {
                    Console.WriteLine("Configuring Azure Key Vault...");
                    KeyVault.AddKeyVaultToBuilder(config);
                })
                .ConfigureServices((context, services) => {
                    Console.WriteLine("Configuring services...");
                    services.AddSingleton<IKeyVault>(KeyVault);
                })
                .UseStartup<Startup>();
    }
}

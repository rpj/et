using System;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.CodeAnalysis;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace ET
{
    public static class Program
    {
        private static readonly KeyVault KeyVault = new KeyVault();

        public static void Main(string[] args)
        {
            CreateWebHostBuilder(args).Build().Run();
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

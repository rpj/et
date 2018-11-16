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
        private static readonly KeyVault _keyVault = new KeyVault();

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
                .ConfigureAppConfiguration((context, config) => { _keyVault.AddKeyVaultToBuilder(config); })
                .ConfigureServices((context, services) => { services.AddSingleton<IKeyVault>(_keyVault); })
                .UseStartup<Startup>();
    }
}

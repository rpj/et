using System;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Logging;

namespace ET
{
    public class Program
    {
        public static void Main(string[] args)
        {
            CreateWebHostBuilder(args).Build().Run();
        }

        public static IWebHostBuilder CreateWebHostBuilder(string[] args) =>
            WebHost.CreateDefaultBuilder(args)
                .ConfigureAppConfiguration((context, config) =>
                {
                    KeyVault.AddKeyVaultToBuilder(config);
                })
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
                .UseStartup<Startup>();
    }
}

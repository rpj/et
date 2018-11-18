using System;
using System.Threading;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using ET.Config;
using ET.Controllers;

namespace ET
{
    public interface IMonitorMode
    {
        void Run();
    }

    public class MonitorMode : IMonitorMode
    {
        private readonly TableStorageController _tsc;

        public MonitorMode(IConfiguration appConfig, IOptions<AzureConfig> azConfig, IRedisController redis)
        {
            var storageConfig = azConfig.Value.Storage;
            Console.WriteLine($"Connecting to table '{storageConfig.Table.Name}' " +
                $"at DB specified by vault secret '{storageConfig.ConnectionStringSecretName}'...");
            _tsc = new TableStorageController(appConfig, storageConfig);
        }

        public void Run()
        {
            var _mmRun = true;
            Console.CancelKeyPress += new ConsoleCancelEventHandler((obj, evArgs) =>
            {
                evArgs.Cancel = !(_mmRun = false);
            });

            Console.WriteLine("Monitoring; press CTRL+C to end.");
            Console.WriteLine("");
            
            while (_mmRun)
            {
                var qList = _tsc.QueryAll();

                foreach (TableStorageEntity tse in qList.Result)
                {
                    Console.WriteLine($"TSE! {tse.PartitionKey} {tse.RowKey} {tse.Data}");
                }
                
                Thread.Sleep(1000);
            }

            Console.WriteLine("");
            Console.WriteLine("Done monitoring; exiting.");
        }
    }
}

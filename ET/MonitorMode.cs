using System;
using System.Threading;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using ET.Config;
using ET.Controllers;

namespace ET
{

    public class MonitorMode : IMonitorMode
    {
        private readonly TableStorageController _tsc;

        public MonitorMode(IConfiguration appConfig, IOptions<AzureConfig> azConfig, IRedisController redis)
        {
            var storageConfig = azConfig.Value.Storage;
            Console.WriteLine($"Connecting to table '{storageConfig.Table.Name}' " +
                $"at DB specified by vault secret '{storageConfig.ConnectionStringSecretName}'...");
            _tsc = new TableStorageController(appConfig, storageConfig, redis);
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

            _tsc.MonitorNewRows((newRow) =>
            {
                Console.WriteLine($"New Row! {newRow.PartitionKey} {newRow.RowKey}");
            });
            
            while (_mmRun)
            {
                Thread.Sleep(1000);
            }

            Console.WriteLine("");
            Console.WriteLine("Done monitoring; exiting.");
        }
    }
}

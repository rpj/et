using System;
using System.Threading;
using Microsoft.Extensions.Configuration;
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
        /* TODO: WHY CAN'T I MAKE THIS WORK WITH DI!?! */
        public MonitorMode(IConfiguration appConfig, AzureConfig azConfig)
        {
            Console.WriteLine($"CTOR! {azConfig.Storage.ConnectionStringSecretName}");
        }

        private readonly TableStorageController _tsc;

        public MonitorMode(IConfiguration configuration)
        {

            var tableConfig = new AzureConfig.StorageConfig()
            {
                ConnectionStringSecretName = configuration.GetSection("Azure:Storage:ConnectionStringSecretName").Value,
                Table = new AzureConfig.StorageConfig.TableConfig()
                {
                    Name = configuration.GetSection("Azure:Storage:Table:Name").Value
                }
            };

            Console.WriteLine($"Connecting to table '{tableConfig.Table.Name}' " +
                $"at DB specified by vault secret '{tableConfig.ConnectionStringSecretName}'...");
            _tsc = new TableStorageController(configuration, tableConfig);
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

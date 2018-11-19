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
        private readonly KeyVault.Key _key;

        public MonitorMode(IConfiguration appConfig, IOptions<AzureConfig> azConfig, 
            IRedisController redis, IKeyVault keyVault)
        {
            var storageConfig = azConfig.Value.Storage;
            Console.WriteLine($"Connecting to table '{storageConfig.Table.Name}' " +
                $"at DB specified by vault secret '{storageConfig.ConnectionStringSecretName}'...");
            _key = ((KeyVault)keyVault)[azConfig.Value.KeyVault.DefaultKeyName];
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
                Console.WriteLine($"PartitionKey: {newRow.PartitionKey}");
                Console.WriteLine($"RowKey:       {newRow.RowKey}");
                Console.WriteLine($"PostTime:     {newRow.PostTime}");
                Console.WriteLine($"Timestamp:    {newRow.Timestamp}");
                Console.Write("Data ");
                var dataStr = newRow.Data;

                try
                {
                    var dataBytes = Convert.FromBase64String(newRow.Data);
                    Console.WriteLine("(Decrypted):");
                    dataStr = _key.Decrypt(dataBytes).Result;
                }
                catch (FormatException)
                {
                    Console.WriteLine("(Plaintext!):");
                }

                Console.WriteLine(dataStr);
                Console.WriteLine();
            });
            
            while (_mmRun)
                Thread.Sleep(500);

            Console.WriteLine("");
            Console.WriteLine("Done monitoring; exiting.");
        }
    }
}

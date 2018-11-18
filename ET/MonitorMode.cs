using System;
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
        /* WHY CAN'T I MAKE THIS WORK WITH DI!?!
        public MonitorMode(IConfiguration appConfig, AzureConfig azConfig)
        {
            Console.WriteLine($"CTOR! {azConfig.Storage.ConnectionStringSecretName}");
        }*/

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
            Console.WriteLine("RUN!");
        }
    }
}

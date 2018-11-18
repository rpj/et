using System;
using System.Net;
using Microsoft.AspNetCore.Mvc;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Table;
using Microsoft.Extensions.Configuration;
using ET.Config;

namespace ET.Controllers
{
    [Serializable]
    public class TableStorageEntity : TableEntity
    {
        private static readonly DateTime Epoch = new DateTime(2018, 1, 1);

        public TableStorageEntity(Guid entityId, DateTime timestamp)
        {
            PostTime = timestamp;
            Timestamp = DateTime.UtcNow;
            PartitionKey = entityId.ToString();
            RowKey = (Timestamp.Ticks - Epoch.Ticks).ToString();
        }
        
        public DateTime PostTime { get; set; }
        public string Data { get; set; }
    }

    public class TableStorageController : ControllerBase
    {
        private CloudTable _tableRef;

        public TableStorageController(IConfiguration appConfig, AzureConfig.StorageConfig azConfig)
        {
            string connStr = null;
            if (string.IsNullOrWhiteSpace(azConfig.Table.Name) || 
                string.IsNullOrWhiteSpace(azConfig.ConnectionStringSecretName) || 
                (connStr = appConfig.GetSection(azConfig.ConnectionStringSecretName).Value) == null)
            {
                throw new InvalidProgramException("No Azure storage configuration specified");
            }

#if DEBUG
            Console.Error.WriteLine($"ConnectionString: {connStr}");
            Console.Error.WriteLine($"TableName: {azConfig.Table.Name}");
#endif
            Init(connStr, azConfig.Table.Name);
        }

        public TableStorageController(string connStr, string tableName)
        {
            Init(connStr, tableName);
        }

        private async void Init(string connectionString, string tableName)
        {
            var storageAccount = CloudStorageAccount.Parse(connectionString);
            _tableRef = storageAccount.CreateCloudTableClient().GetTableReference(tableName);
            await _tableRef.CreateIfNotExistsAsync();
        }

        public async void Add(TableStorageEntity newEntity)
        {
            var tRes = await _tableRef.ExecuteAsync(TableOperation.Insert(newEntity));
            if (!(tRes.HttpStatusCode < (int)HttpStatusCode.Ambiguous))
            {
                throw new Exception($"Bad 'add': {tRes.HttpStatusCode}");
            }
        }
    }
}
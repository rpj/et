using System;
using System.Net;
using System.Collections.Generic;
using System.Threading.Tasks;
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

        // a default, no-argument ctor is required to use this class 
        // as the generic type 'T' of TableQuery<T>
        public TableStorageEntity() { }
        
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
            Console.Error.WriteLine("TableStorageController initialized with the following parameters:");
            Console.Error.WriteLine($"\tTableName: {azConfig.Table.Name}");
            Console.Error.WriteLine($"\tConnectionString: {connStr}");
#endif
            Init(connStr, azConfig.Table.Name);
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

        public async Task<TableQuerySegment<TableStorageEntity>> QueryAll()
        {
            var query = new TableQuery<TableStorageEntity>();

            var tct = new TableContinuationToken();
            var qRes = await _tableRef.ExecuteQuerySegmentedAsync(query, tct);

            return qRes;
        }
    }
}
using System;
using System.Net;
using Microsoft.AspNetCore.Mvc;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Table;
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

        public TableStorageController(AzureConfig.StorageConfig config)
        {
            if (string.IsNullOrWhiteSpace(config.ConnectionString) || 
                string.IsNullOrWhiteSpace(config.Table.Name))
            {
                throw new InvalidProgramException("No Azure storage configuration specified");
            }

            Init(config);
        }

        private async void Init(AzureConfig.StorageConfig config)
        {
            var storageAccount = CloudStorageAccount.Parse(config.ConnectionString);
            _tableRef = storageAccount.CreateCloudTableClient().GetTableReference(config.Table.Name);
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
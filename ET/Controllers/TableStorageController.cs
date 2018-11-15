using System;
using System.Net;
using Microsoft.AspNetCore.Mvc;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Table;
using Microsoft.Extensions.Configuration;

namespace ET.Controllers
{
    [Serializable]
    public class TableStorageEntity : TableEntity
    {
        private static readonly DateTime _epoch = new DateTime(2018, 1, 1);

        public TableStorageEntity(Guid entityId, DateTime timestamp)
        {
            PostTime = timestamp;
            Timestamp = DateTime.UtcNow;
            PartitionKey = entityId.ToString();
            RowKey = (Timestamp.Ticks - _epoch.Ticks).ToString();
        }
        
        public DateTime PostTime { get; set; }
        public string Data { get; set; }
    }

    public class TableStorageController : ControllerBase
    {
        private CloudTable _tableRef;

        public TableStorageController(IConfiguration config)
        {
            var azStorageConnStr = config.GetConnectionString("AzureStorageConnectionString");
            var tableName = config["TableName"];

            if (string.IsNullOrWhiteSpace(azStorageConnStr) || string.IsNullOrWhiteSpace(tableName))
            {
                throw new InvalidProgramException("No Azure storage configuration specified");
            }

            Init(azStorageConnStr, tableName);
        }

        private async void Init(string azStorageConnStr, string tableName)
        {
            var storageAccount = CloudStorageAccount.Parse(azStorageConnStr);
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
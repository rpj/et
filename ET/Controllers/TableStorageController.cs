using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Auth;
using Microsoft.WindowsAzure.Storage.Table;
using Microsoft.Extensions.Configuration;

namespace ET.Controllers
{
    [Serializable]
    public class TableStorageEntity : TableEntity
    {
        private static DateTime _epoch = new DateTime(2001, 1, 1);
        public TableStorageEntity(Guid entityId, DateTime msgTime)
        {
            Timestamp = DateTime.UtcNow;
            PartitionKey = entityId.ToString();
            RowKey = (Timestamp.Ticks - _epoch.Ticks).ToString();
            Id = entityId;
            MsgTime = msgTime;
        }

        public Guid Id { get; set; }
        public DateTime MsgTime { get; set; }
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

            CloudStorageAccount storageAccount = CloudStorageAccount.Parse(azStorageConnStr);
            _tableRef = storageAccount.CreateCloudTableClient().GetTableReference(tableName);
            _tableRef.CreateIfNotExistsAsync();
        }

        public async void Add(TableStorageEntity newEntity)
        {
            TableResult tRes = await _tableRef.ExecuteAsync(TableOperation.Insert(newEntity));
            if (tRes.HttpStatusCode > 300)
            {
                throw new Exception($"Bad 'add': {tRes.HttpStatusCode}");
            }
        }
    }
}
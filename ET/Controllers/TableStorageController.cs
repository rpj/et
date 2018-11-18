using System;
using System.Net;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Table;
using Microsoft.Extensions.Configuration;
using ET.Config;
using Newtonsoft.Json;

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

        public TableStorageEntity(string pKey, string rKey)
        {
            PartitionKey = pKey;
            RowKey = rKey;
        }

        public DateTime PostTime { get; set; }
        public string Data { get; set; }
    }

    public delegate void TableStorageRowMonitorDelegate(TableStorageEntity newRow);

    public class TableStorageController : ControllerBase
    {

        private CloudTable _tableRef;
        private readonly IRedisController _redisController;

        public TableStorageController(IConfiguration appConfig, 
            AzureConfig.StorageConfig azConfig, IRedisController redisController = null)
        {
            string connStr = null;
            if (string.IsNullOrWhiteSpace(azConfig.Table.Name) || 
                string.IsNullOrWhiteSpace(connStr = azConfig.ConnectionString(appConfig)))
            {
                throw new InvalidProgramException("No Azure storage configuration specified");
            }

            _redisController = redisController;

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

            if (_redisController != null)
            {
                _redisController.Publish(new Dictionary<string, string>
                {
                    { "ParitionKey", newEntity.PartitionKey },
                    { "RowKey", newEntity.RowKey }
                });
            }
        }

        private class _Mon : IChannelSubscriber
        {
            private readonly TableStorageRowMonitorDelegate _md;
            public _Mon(TableStorageRowMonitorDelegate md) { _md = md; }
            public void NewMessage(string channel, string message)
            {
                var obj = JsonConvert.DeserializeObject(message) as Newtonsoft.Json.Linq.JObject;
                var dObj = obj.ToObject<Dictionary<string, string>>();
                // TODO: have to lookup actual object from storage, build proper TableStorageEntity
                // but WITHOUT unencrypting the Data object!
                _md(new TableStorageEntity(dObj["PartitionKey"], dObj["RowKey"]));
            }
        }

        public bool MonitorNewRows(TableStorageRowMonitorDelegate monitorDelegate)
        {
            if (_redisController != null)
            {
                _redisController.Subscribe(new _Mon(monitorDelegate));
                return true;
            }

            return false;
        }
    }
}
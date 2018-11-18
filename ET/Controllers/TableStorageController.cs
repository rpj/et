﻿using System;
using System.Net;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Table;
using Microsoft.Extensions.Configuration;
using ET.Config;
using Newtonsoft.Json;

namespace ET.Controllers
{
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

        public bool MonitorNewRows(TableStorageRowMonitorDelegate monitorDelegate)
        {
            if (_redisController != null)
            {
                _redisController.Subscribe(new InternalChannelMonitor(this, monitorDelegate));
                return true;
            }

            return false;
        }

        #region Private

        private async Task<TableQuerySegment<TableStorageEntity>> LookupRow(string partitionKey, string rowKey)
        {
            var tQuery = new TableQuery<TableStorageEntity>().
                Where(TableQuery.CombineFilters(
                    TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, partitionKey),
                    TableOperators.And,
                    TableQuery.GenerateFilterCondition("RowKey", QueryComparisons.Equal, rowKey)));

            return await _tableRef.ExecuteQuerySegmentedAsync(tQuery, new TableContinuationToken());
        }

        private class InternalChannelMonitor : IChannelSubscriber
        {
            private readonly TableStorageRowMonitorDelegate _md;
            private readonly TableStorageController _tsc;
            public InternalChannelMonitor(TableStorageController tsc, 
                TableStorageRowMonitorDelegate md)
            {
                _md = md;
                _tsc = tsc;
            }

            public void NewMessage(string channel, string message)
            {
                var dObj = ((Newtonsoft.Json.Linq.JObject)JsonConvert.DeserializeObject(message))
                    .ToObject<Dictionary<string, string>>();
                var rowList = _tsc.LookupRow(dObj["PartitionKey"], dObj["RowKey"]).Result.Results;
                
                if (rowList.Count > 1)
                    throw new Exception($"How is this even possible?!");

                _md(rowList[0]);
            }
        }

        #endregion
    }
}
using System;
using System.Runtime.Serialization;
using Microsoft.Extensions.Configuration;
using Microsoft.WindowsAzure.Storage.Table;

namespace ET
{
    #region Interfaces
    public interface IChannelSubscriber
    {
        void NewMessage(string channel, string message);
    }

    public interface IRedisController
    {
        void Publish(ISerializable publishObject);
        void Subscribe(IChannelSubscriber channelSubscriber);
    }

    public interface IKeyVault
    {
        void AddKeyVaultToBuilder(IConfigurationBuilder config);
    }

    public interface IMonitorMode
    {
        void Run();
    }
    #endregion

    #region Data structures
    [Serializable]
    public struct APIv1Post
    {
        public Guid Id;
        public DateTime Timestamp;
        public string Data;
    }

    [Serializable]
    public class TableStorageEntity : TableEntity
    {
        public static readonly string PartitionKeyName = "PartitionKey";
        public static readonly string RowKeyName = "RowKey";
        public static readonly DateTime Epoch = new DateTime(2018, 1, 1);

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
    #endregion
}
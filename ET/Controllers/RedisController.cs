using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using StackExchange.Redis;
using ET.Config;
using Newtonsoft.Json;

namespace ET.Controllers
{
    public delegate void SubscribeMessageRecieved(RedisController ctrlr, string msgName, object data);

    public interface IRedisController
    {
        void Publish(ISerializable publishObject);
        void Subscribe(SubscribeMessageRecieved msgRxedCallback);
    }

    public class RedisController : IRedisController
    {
        private readonly List<SubscribeMessageRecieved> _subs = new List<SubscribeMessageRecieved>();
        private readonly ConnectionMultiplexer _redis;
        private readonly AzureConfig.RedisConfig _config;

        public RedisController(IConfiguration appConfig, IOptions<AzureConfig> config)
        {
            _config = config.Value.Redis;
            var redisConnStr = _config.ConnectionString(appConfig);

            if (string.IsNullOrEmpty(redisConnStr) || string.IsNullOrEmpty(_config.ChannelName))
                throw new Exception("Bad Redis configuration!");

            _config = config.Value.Redis;
            _redis = ConnectionMultiplexer.Connect(redisConnStr);
        }

        public void Publish(ISerializable publishObject)
        {
            _redis.GetSubscriber().Publish(_config.ChannelName, JsonConvert.SerializeObject(publishObject));
        }

        public void Subscribe(SubscribeMessageRecieved msgRxedCallback)
        {
            lock (_subs)
            {
                if (_subs.IndexOf(msgRxedCallback) == -1)
                    _subs.Add(msgRxedCallback);
            }
        }
    }
}

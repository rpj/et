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
    public class RedisController : IRedisController
    {
        private readonly List<IChannelSubscriber> _subs = new List<IChannelSubscriber>();
        private readonly ConnectionMultiplexer _redis;
        private readonly AzureConfig.RedisConfig _config;
        private bool _subscriberRunning = false;

        public RedisController(IConfiguration appConfig, IOptions<AzureConfig> config)
        {
            _config = config.Value.Redis;
            var redisConnStr = _config.ConnectionString(appConfig);

            if (string.IsNullOrEmpty(redisConnStr) || string.IsNullOrEmpty(_config.ChannelName))
                throw new Exception("Bad Redis configuration!");

            _config = config.Value.Redis;
            _redis = ConnectionMultiplexer.Connect(redisConnStr);
        }

        private async void StartSubscriber()
        {
            if (!_subscriberRunning)
            {
                _subscriberRunning = true;
                await _redis.GetSubscriber().SubscribeAsync(_config.ChannelName, ChannelMessageReceived);
            }
        }

        private void ChannelMessageReceived(RedisChannel channel, RedisValue message)
        {
            lock (_subs)
            {
                _subs.ForEach((chanSub) => { chanSub.NewMessage(channel.ToString(), message.ToString()); });
            }
        }

        public void Publish(ISerializable publishObject)
        {
            _redis.GetSubscriber().Publish(_config.ChannelName, JsonConvert.SerializeObject(publishObject));
        }

        public void Subscribe(IChannelSubscriber channelSubscriber)
        {
            StartSubscriber();

            lock (_subs)
            {
                if (_subs.IndexOf(channelSubscriber) == -1)
                    _subs.Add(channelSubscriber);
            }
        }
    }
}

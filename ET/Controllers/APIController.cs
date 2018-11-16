using System;
using System.Security.Cryptography;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using ET.Config;

namespace ET.Controllers
{
    [Serializable]
    public struct APIv1Post
    {
        public Guid Id;
        public DateTime Timestamp;
        public string Data;
    }

    [Route("api/[controller]")]
    [ApiController]
    public class V1Controller : ControllerBase
    {
        private readonly TableStorageController _tsc;
        private readonly RSACryptoServiceProvider _rsaCrypt;
        private readonly AzureConfig _azConfig;

#if DEBUG
        private readonly Guid _appGuid;

        [HttpGet("{plaintext}")]
        public void Get(string plaintext)
        {
            Post(new APIv1Post()
            {
                Id = _appGuid,
                Timestamp = DateTime.Now,
                Data = KeyVault.Keys[_azConfig.KeyVault.DefaultKeyName].Encrypt(plaintext)
            });
        }
#endif

        public V1Controller(IOptions<AzureConfig> config)
        {
            _azConfig = config.Value as AzureConfig;
            _tsc = new TableStorageController(_azConfig.Storage);
            _rsaCrypt = new RSACryptoServiceProvider();

#if DEBUG
            if (!Guid.TryParse(_azConfig.AppId, out _appGuid))
            {
                throw new Exception();
            }
#endif
        }

        [HttpPost]
        public void Post([FromBody] APIv1Post value)
        {
            try
            {
                Convert.FromBase64String(value.Data);
            }
            catch (FormatException)
            {
                // TODO: log as a real error for analytics, etc... though really, shouldn't ever happen!
                Console.WriteLine($"ERROR: Received unecrypted data! Encypting now, but this is still bad...");
                value.Data = KeyVault.Keys[_azConfig.KeyVault.DefaultKeyName].Encrypt(value.Data);
            }

            _tsc.Add(new TableStorageEntity(value.Id, value.Timestamp)
            {
                Data = value.Data
            });
        }

    }
}

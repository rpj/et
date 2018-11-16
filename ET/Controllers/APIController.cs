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
        private readonly AzureConfig _azConfig;
        private readonly KeyVault _keyVault;

#if DEBUG
        private readonly Guid _appGuid;

        [HttpGet("{plaintext}")]
        public void Get(string plaintext)
        {
            Post(new APIv1Post()
            {
                Id = _appGuid,
                Timestamp = DateTime.Now,
                Data = _keyVault.Keys[_azConfig.KeyVault.DefaultKeyName].Encrypt(plaintext)
            });
        }
#endif

        public V1Controller(IOptions<AzureConfig> config, IKeyVault keyVault)
        {
            _azConfig = config.Value;
            _tsc = new TableStorageController(_azConfig.Storage);
            _keyVault = keyVault as KeyVault;

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
                // try to convert, ignoring the result, as we only care
                // whether the data is valid Base64 or not (and not *what* the data is
                // as if it *is* encoded it should also be encrypted!)
                var _ = Convert.FromBase64String(value.Data);
            }
            catch (FormatException)
            {
                // TODO: log as a real error for analytics, etc... though really, shouldn't ever happen!
                Console.WriteLine($"ERROR: Received unencrypted data! Encrypting now, but this is still bad...");
                value.Data = _keyVault.Keys[_azConfig.KeyVault.DefaultKeyName].Encrypt(value.Data);
            }

            _tsc.Add(new TableStorageEntity(value.Id, value.Timestamp)
            {
                Data = value.Data
            });
        }

    }
}

using System;
using System.Security.Cryptography;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;

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
#if DEBUG
        private readonly Guid _appGuid;

        [HttpGet("{plaintext}")]
        public void Get(string plaintext)
        {
            Post(new APIv1Post()
            {
                Id = _appGuid,
                Timestamp = DateTime.Now,
                Data = KeyVault.Encrypt("ET-key-0-rsa", plaintext)
            });
        }
#endif

        public V1Controller(IConfiguration config)
        {
            _tsc = new TableStorageController(config);
            _rsaCrypt = new RSACryptoServiceProvider();
#if DEBUG
            if (!Guid.TryParse(config["AzureAppId"], out _appGuid))
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
                value.Data = KeyVault.Encrypt("ET-key-0-rsa", value.Data);
            }

            _tsc.Add(new TableStorageEntity(value.Id, value.Timestamp)
            {
                Data = value.Data
            });
        }

    }
}

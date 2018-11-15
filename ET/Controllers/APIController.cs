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
            Console.WriteLine($"GOT plaintext! {plaintext}");
            Console.WriteLine($"POST THAT SHIT WITH {_appGuid}");
            Post(new APIv1Post()
            {
                Id = _appGuid,
                Timestamp = DateTime.Now,
                Data = plaintext // TODO: ENC THIS WITH THE PUBKEY FROM KV!!!!
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
            _tsc.Add(new TableStorageEntity(value.Id, value.Timestamp)
            {
                Data = value.Data
            });
        }

    }
}

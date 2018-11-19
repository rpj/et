using System;
using System.IO;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Configuration;
using ET.Config;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace ET.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class V1Controller : ControllerBase
    {
        private readonly TableStorageController _tsc;
        private readonly AzureConfig _azConfig;
        private readonly KeyVault _keyVault;
        private readonly IConfiguration _appConfig;

//#if DEBUG
        private readonly Guid _appGuid;

        [HttpGet("{plaintext}")]
        public void Get(string plaintext)
        {
            Console.WriteLine($"GET '{plaintext}'");
            Post(new APIv1Post()
            {
                Id = _appGuid,
                Timestamp = DateTime.Now,
                Data = _keyVault[_azConfig.KeyVault.DefaultKeyName].Encrypt(plaintext)
            });
        }
//#endif

        public V1Controller(IConfiguration appConfig, IOptions<AzureConfig> config, 
            IKeyVault keyVault, IRedisController redis)
        {
            _azConfig = config.Value;
            _appConfig = appConfig;
            _tsc = new TableStorageController(_appConfig, _azConfig.Storage, redis);
            _keyVault = keyVault as KeyVault;

//#if DEBUG
            if (!Guid.TryParse(_azConfig.AppId, out _appGuid))
            {
                throw new Exception();
            }
//#endif
        }

        [HttpPost]
        public void Post([FromBody] APIv1Post value)
        {
            Console.WriteLine($"POST '{value.Id}' '{value.Timestamp}'");

            var addResult = _tsc.Add(new TableStorageEntity(value.Id, value.Timestamp)
            {
                Data = value.Data
            });

            var tableRes = addResult.Result;
            Response.StatusCode = tableRes.HttpStatusCode;
            Console.WriteLine($"POST RESULT {Response.StatusCode}");
        }
    }
}

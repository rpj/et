using System;
using System.Security.Cryptography;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using ET.Config;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.IdentityModel.Protocols;
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
        private readonly AzureConfig _azConfig;
        private readonly KeyVault _keyVault;
        private readonly IConfiguration _appConfig;

#if DEBUG
        private readonly Guid _appGuid;

        [HttpGet("{plaintext}")]
        public void Get(string plaintext)
        {
            Post(new APIv1Post()
            {
                Id = _appGuid,
                Timestamp = DateTime.Now,
                Data = _keyVault[_azConfig.KeyVault.DefaultKeyName].Encrypt(plaintext)
            });
        }
#endif

        public V1Controller(IConfiguration appConfig, IOptions<AzureConfig> config, IKeyVault keyVault)
        {
            _azConfig = config.Value;
            _appConfig = appConfig;
            _tsc = new TableStorageController(_appConfig, _azConfig.Storage);
            _keyVault = keyVault as KeyVault;

            Console.Error.WriteLine($"V1Controller has mode {_appConfig["ASPNETCORE_ENVIRONMENT"]}");

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
            var okToPost = true;// WHYCANTIGETDEVMODEONDEPLOY!?!?! false;

            try
            {
                // try to convert, ignoring the result, as we only care
                // whether the data is valid Base64 or not (and not *what* the data is
                // as if it *is* encoded it should also be encrypted!)
                okToPost = Convert.FromBase64String(value.Data).Length != 0;
            }
            catch (FormatException)
            {
            }

            if (okToPost || _appConfig["ASPNETCORE_ENVIRONMENT"] == "Development")
            {
                if (!okToPost)
                {
                    Console.Error.WriteLine($"WARNING: Posting unencrypted data in 'Development' mode!");
                    Console.Error.WriteLine($"DATA:\n{value.Data}");
                }

                _tsc.Add(new TableStorageEntity(value.Id, value.Timestamp)
                {
                    Data = value.Data
                });
            }
        }

    }
}

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using StackExchange.Redis;
using ET.Config;

namespace ET.Controllers
{
    public class RedisController
    {
        public RedisController(IConfiguration appConfig, IOptions<AzureConfig> config, IKeyVault keyVault)
        {

        }
    }
}

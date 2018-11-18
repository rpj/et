using Microsoft.Extensions.Configuration;

namespace ET
{
    namespace Config
    {
        public class ConnStringGetter
        {
            public string ConnectionStringSecretName { get; set; }

            public string ConnectionString(IConfiguration appConfig)
            {
                return ConnectionStringSecretName != null ? 
                    appConfig.GetSection(ConnectionStringSecretName).Value : null;
            }
        }

        public class AzureConfig
        {
            public class KeyVaultConfig
            {
                public string Uri { get; set; }
                public string DefaultKeyName { get; set; }
            }

            public class StorageConfig : ConnStringGetter
            {
                public class TableConfig
                {
                    public string Name { get; set; }
                }
                
                public TableConfig Table { get; set; }
            }

            public class RedisConfig : ConnStringGetter
            {
                public string ChannelName { get; set; }
            }

            public string AppId { get; set; }
            public KeyVaultConfig KeyVault { get; set; }
            public StorageConfig Storage { get; set; }
            public RedisConfig Redis { get; set; }
        }
    }
}

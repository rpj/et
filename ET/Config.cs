namespace ET
{
    namespace Config
    {
        public class AzureConfig
        {
            public class KeyVaultConfig
            {
                public string Uri { get; set; }
                public string DefaultKeyName { get; set; }
            }

            public class StorageConfig
            {
                public class TableConfig
                {
                    public string Name { get; set; }
                }

                public string ConnectionStringSecretName { get; set; }
                public TableConfig Table { get; set; }
            }

            public string AppId { get; set; }
            public KeyVaultConfig KeyVault { get; set; }
            public StorageConfig Storage { get; set; }
        }
    }
}

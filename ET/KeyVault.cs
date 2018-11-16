using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Azure.KeyVault;
using Microsoft.Azure.KeyVault.Models;
using Microsoft.Azure.Services.AppAuthentication;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.AzureKeyVault;

namespace ET
{
    public static class KeyVault
    {
        public class Key
        {
            public string Name { get; }
            public string Version { get; }

            public Key(KeyItem item, KeyBundle bundle, string vaultUri)
            {
                Item = item;
                Bundle = bundle;
                VaultUri = vaultUri;
                AsRSAParams = Bundle.Key.ToRSAParameters();
                Name = Item.Identifier.Name;
                Version = Item.Identifier.Version;
            }

            public async Task<string> Decrypt(string cryptText)
            {
                var cBytes = Convert.FromBase64String(cryptText);
                var decRes = await AzureClient.DecryptWithHttpMessagesAsync(VaultUri, Name, Version, "RSA1_5", cBytes);
                return Encoding.Unicode.GetString(decRes.Body.Result);
            }

            public string Encrypt(string plainText)
            {
                using (var rsp = new RSACryptoServiceProvider())
                {
                    rsp.ImportParameters(AsRSAParams);
                    return Convert.ToBase64String(rsp.Encrypt(Encoding.Unicode.GetBytes(plainText), false));
                }
            }

            public override string ToString()
            {
                return $"Key<Name='{Name}' Version='{Version}'>";
            }

            private readonly KeyItem Item;
            private readonly KeyBundle Bundle;
            private readonly RSAParameters AsRSAParams;
            private readonly string VaultUri;
        };

        public static readonly Dictionary<string, Key> Keys = new Dictionary<string, Key>();

        public static void AddKeyVaultToBuilder(IConfigurationBuilder config)
        {
            var kvUri = config.Build().GetSection("Azure:KeyVault:Uri").Value as string;

            if (string.IsNullOrWhiteSpace(kvUri))
            {
                throw new InvalidProgramException("Bad Azure app configuration!");
            }

            AzureVaultUri = kvUri;
            AzureClient = new KeyVaultClient(
                new KeyVaultClient.AuthenticationCallback(
                    new AzureServiceTokenProvider().KeyVaultTokenCallback));
            
            config.AddAzureKeyVault(AzureVaultUri, AzureClient, new DefaultKeyVaultSecretManager());
            DiscoverAvailableKeysAsync();
        }

        #region Private
        private static string AzureVaultUri;
        private static KeyVaultClient AzureClient;

        private static async void DiscoverAvailableKeysAsync()
        {
            var keysResponse = await AzureClient.GetKeysWithHttpMessagesAsync(AzureVaultUri);
            if (!keysResponse.Response.IsSuccessStatusCode)
            {
                throw new InvalidProgramException();
            }

            using (var respBodyEnum = keysResponse.Body.GetEnumerator())
            {
                while (respBodyEnum.MoveNext())
                {
                    var cur = respBodyEnum.Current;
                    var keyData =
                        await AzureClient.GetKeyWithHttpMessagesAsync(AzureVaultUri, 
                        cur.Identifier.Name, cur.Identifier.Version);

                    if (keyData.Response.IsSuccessStatusCode)
                    {
                        lock (Keys)
                        {
                            Keys[cur.Identifier.Name] = new Key(cur, keyData.Body, AzureVaultUri);
                        }

                        _DEBUG_KeyEncDecVerify(Keys[cur.Identifier.Name]);
                    }
                }
            }
        }

        private static void _DEBUG_KeyEncDecVerify(Key key)
        {
#if DEBUG
            var plaintext = "123456789abcdefghijklmnopqrstuvqyxz";
            var dec = key.Decrypt(key.Encrypt(plaintext)).Result;

            if (plaintext != dec)
            {
                throw new InvalidProgramException($"Bad unit test! {key}");
            }

            Console.WriteLine($"Unit test of {key} passed");
#endif
        }
        #endregion
    }
}

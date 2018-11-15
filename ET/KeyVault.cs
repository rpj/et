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
        public static string AzureVaultUri;
        public static KeyVaultClient AzureClient;

        public static void Configure(IConfigurationBuilder config)
        {
            var bCfg = config.Build();
            var kvUri = bCfg["AzureKeyVaultUri"];

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

        // TODO: Encrypt()/Decrypt() *really* should just be methods of Key (which needs to be abonafide class)
        public static async Task<string> Decrypt(string keyId, string cryptText)
        {
            if (!Keys.ContainsKey(keyId))
            {
                return null;
            }

            var cBytes = Convert.FromBase64String(cryptText);
            var decRes = await AzureClient.DecryptWithHttpMessagesAsync(AzureVaultUri, 
                Keys[keyId].Item.Identifier.Name, Keys[keyId].Item.Identifier.Version, "RSA1_5", cBytes);
            return Encoding.Unicode.GetString(decRes.Body.Result);
        }

        public static string Encrypt(string keyId, string plainText)
        {
            if (!Keys.ContainsKey(keyId))
            {
                return null;
            }

            using (var rsp = new RSACryptoServiceProvider())
            {
                rsp.ImportParameters(Keys[keyId].AsRSAParams);
                return Convert.ToBase64String(rsp.Encrypt(Encoding.Unicode.GetBytes(plainText), false));
            }
        }

        private struct Key
        {
            public KeyItem Item;
            public KeyBundle Bundle;
            public RSAParameters AsRSAParams;
        };

        private static readonly Dictionary<string, Key> Keys = new Dictionary<string, Key>();

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
                            Keys[cur.Identifier.Name] = new Key()
                            {
                                Item = cur,
                                Bundle = keyData.Body,
                                AsRSAParams = keyData.Body.Key.ToRSAParameters()
                            };
                        }

#if DEBUG
                        var sw = new System.IO.StringWriter();
                        new Newtonsoft.Json.JsonSerializer().Serialize(sw, Keys[cur.Identifier.Name].AsRSAParams);
                        Console.WriteLine($"Key name: {cur.Identifier.Name}");
                        Console.WriteLine($"RSA JSON: {sw.ToString()}");
#endif
                    }
                }
            }
        }
    }
}

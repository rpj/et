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
    public interface IKeyVault
    {
        void AddKeyVaultToBuilder(IConfigurationBuilder config);
    }

    public class KeyVault : IKeyVault
    {
        public class Key
        {
            public string Name { get; }
            public string Version { get; }

            public Key(KeyItem item, KeyBundle bundle, KeyVault vault)
            {
                _vault = vault;
                _rsaParams = bundle.Key.ToRSAParameters();
                Name = item.Identifier.Name;
                Version = item.Identifier.Version;
            }

            public async Task<string> Decrypt(string cryptText)
            {
                var cBytes = Convert.FromBase64String(cryptText);
                var decRes = await _vault._client.DecryptWithHttpMessagesAsync(_vault._uri, Name, Version, "RSA1_5", cBytes);
                return Encoding.Unicode.GetString(decRes.Body.Result);
            }

            public string Encrypt(string plainText)
            {
                using (var rsp = new RSACryptoServiceProvider())
                {
                    rsp.ImportParameters(_rsaParams);
                    return Convert.ToBase64String(rsp.Encrypt(Encoding.Unicode.GetBytes(plainText), false));
                }
            }

            public override string ToString()
            {
                return $"Key<Name='{Name}' Version='{Version}'>";
            }

            private readonly RSAParameters _rsaParams;
            private readonly KeyVault _vault;
        };
        
        public readonly Dictionary<string, Key> Keys = new Dictionary<string, Key>();
        private string _uri;
        private KeyVaultClient _client;

        public void AddKeyVaultToBuilder(IConfigurationBuilder config)
        {
            var kvUri = config.Build().GetSection("Azure:KeyVault:Uri").Value as string;

            if (string.IsNullOrWhiteSpace(kvUri))
            {
                throw new InvalidProgramException("Bad Azure app configuration!");
            }

            _uri = kvUri;
            _client = new KeyVaultClient(
                new KeyVaultClient.AuthenticationCallback(
                    new AzureServiceTokenProvider().KeyVaultTokenCallback));
            
            config.AddAzureKeyVault(_uri, _client, new DefaultKeyVaultSecretManager());
            DiscoverAvailableKeysAsync();
        }

        #region Private

        private async void DiscoverAvailableKeysAsync()
        {
            var keysResponse = await _client.GetKeysWithHttpMessagesAsync(_uri);
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
                        await _client.GetKeyWithHttpMessagesAsync(_uri, 
                        cur.Identifier.Name, cur.Identifier.Version);

                    if (keyData.Response.IsSuccessStatusCode)
                    {
                        lock (Keys)
                        {
                            Keys[cur.Identifier.Name] = new Key(cur, keyData.Body, this);
                            _DEBUG_KeyEncDecVerify(Keys[cur.Identifier.Name]);
                        }
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

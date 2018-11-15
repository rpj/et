using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Net;
using System.Text;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Azure.KeyVault;
using Microsoft.Azure.KeyVault.Models;
using Microsoft.Azure.Services.AppAuthentication;
using Microsoft.Extensions.Configuration.AzureKeyVault;
using Microsoft.Extensions.Logging;
using Microsoft.Rest.Azure;

namespace ET
{
    public class Program
    {
        private struct Key
        {
            public KeyItem Item;
            public KeyBundle Bundle;
        };

        private static readonly Dictionary<string, Key> Keys = new Dictionary<string, Key>();

        public static void Main(string[] args)
        {
            CreateWebHostBuilder(args).Build().Run();
        }

        private static async void DiscoverAvailableKeysAsync(string kvUri, IKeyVaultClient kvClient)
        {
            var keysResponse = await kvClient.GetKeysWithHttpMessagesAsync(kvUri);
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
                        await kvClient.GetKeyWithHttpMessagesAsync(kvUri, cur.Identifier.Name, cur.Identifier.Version);

                    if (keyData.Response.IsSuccessStatusCode)
                    {
                        lock (Keys)
                        {
                            Keys[cur.Identifier.Name] = new Key()
                            {
                                Item = cur,
                                Bundle = keyData.Body
                            };
                        }

                        Console.WriteLine($"Found PubKey named '{cur.Identifier.Name}'");

                        var rsaParams = keyData.Body.Key.ToRSAParameters();
                        var writer = new System.IO.StringWriter();
                        new Newtonsoft.Json.JsonSerializer().Serialize(writer, rsaParams);
                        Console.WriteLine($"PubKey as string: {writer.ToString()}");
                        var rsp = new RSACryptoServiceProvider();
                        rsp.ImportParameters(rsaParams);

                        var pText = Encoding.Unicode.GetBytes("plaintext! you can't see me!");
                        var cB64Text = Convert.ToBase64String(rsp.Encrypt(pText, false));

                        Console.WriteLine($"Crypted text: {cB64Text}");

                        var cBytes = Convert.FromBase64String(cB64Text);
                        var decRes = await kvClient.DecryptWithHttpMessagesAsync(kvUri, cur.Identifier.Name,
                            cur.Identifier.Version, "RSA1_5", cBytes);
                        var decText = Encoding.Unicode.GetString(decRes.Body.Result);

                        Console.WriteLine($"Dec text (from THE CLOUDDDDDDD): {decText}");
                    }
                }
            }
        }

        private static void ConfigureAzureKeyVault(IConfigurationBuilder config)
        {
            var bCfg = config.Build();
            var kvUri = bCfg["AzureKeyVaultUri"];

            if (string.IsNullOrWhiteSpace(kvUri))
            {
                throw new InvalidProgramException("Bad Azure app configuration!");
            }

            var tokenProvider = new AzureServiceTokenProvider();
            var kvClient =
                new KeyVaultClient(
                    new KeyVaultClient.AuthenticationCallback(tokenProvider.KeyVaultTokenCallback));

            config.AddAzureKeyVault(kvUri, kvClient, new DefaultKeyVaultSecretManager());
            DiscoverAvailableKeysAsync(kvUri, kvClient);
        }

        public static IWebHostBuilder CreateWebHostBuilder(string[] args) =>
            WebHost.CreateDefaultBuilder(args)
                .ConfigureAppConfiguration((context, config) =>
                {
                    ConfigureAzureKeyVault(config);
                })
                .ConfigureLogging((context, config) =>
                {
                    var env = context.HostingEnvironment;

                    if (env.IsDevelopment())
                    {
                        config.AddDebug();
                        config.AddEventSourceLogger();
                    }

                    config.AddConsole();
                })
                .UseStartup<Startup>();
    }
}

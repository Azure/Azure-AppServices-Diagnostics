using Diagnostics.Logger;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class EncryptedConfigProviderExtensions
    {
        public static IConfigurationBuilder AddEncryptedProvider(this IConfigurationBuilder builder, string encryptionKey, string initializationVector, string file)
        {
            return builder.Add(new EncryptedConfigProvider(encryptionKey, initializationVector, file));
        }
    }

    public class EncryptedConfigProvider : ConfigurationProvider, IConfigurationSource
    {
        static string EncryptionKey;

        static string EncryptedFile;

        static string InitializationVector;

        public EncryptedConfigProvider()
        {

        }

        public EncryptedConfigProvider(string configEncryptionKey, string configInitVector, string file)
        {
            EncryptionKey = configEncryptionKey;
            EncryptedFile = file;
            InitializationVector = configInitVector;
        }

        public override void Load()
        {
            Data = DecryptSettings();           
        }

        private IDictionary<string, string> DecryptSettings()
        {
            var config = new Dictionary<string, string>();
            try
            {
                using (StreamReader reader = new StreamReader(EncryptedFile))
                {
                    string json = reader.ReadToEnd();
                    var result = JsonConvert.DeserializeObject<Dictionary<string, string>>(json);

                    foreach (KeyValuePair<string, string> item in result)
                    {
                        var plainText = DecryptSetting(item.Value);
                        if (!string.IsNullOrEmpty(plainText))
                        {
                            config.Add(item.Key, plainText);
                        }
                    }

                }
            } catch (Exception ex)
            {
                DiagnosticsETWProvider.Instance.LogRuntimeHostMessage($"Exception in {nameof(EncryptedConfigProvider)} : {ex.ToString()}");
            }
            
            return config;
        }

        private string DecryptSetting(string secretSetting)
        {
            string plainText = null;
            try
            {
                byte[] iv = Convert.FromBase64String(InitializationVector);
                byte[] buffer = Convert.FromBase64String(secretSetting);
                using (Aes aes = Aes.Create())
                {
                    aes.Key = Convert.FromBase64String(EncryptionKey);
                    aes.IV = iv;
                    aes.Padding = PaddingMode.PKCS7;
                    ICryptoTransform decryptor = aes.CreateDecryptor(aes.Key, aes.IV);
                    var unencryptedBytes = decryptor.TransformFinalBlock(buffer, 16, buffer.Length - 16);
                    plainText = Encoding.UTF8.GetString(unencryptedBytes);
                }
            } catch (Exception ex)
            {
                DiagnosticsETWProvider.Instance.LogRuntimeHostMessage($"Exception while decrypting setting: {ex.ToString()}");
            }         
            return plainText;
        }

        IConfigurationProvider IConfigurationSource.Build(IConfigurationBuilder builder)
        {
            return new EncryptedConfigProvider(EncryptionKey, InitializationVector, EncryptedFile);
        }
    }
}

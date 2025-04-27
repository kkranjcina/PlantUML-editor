using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace PlantUMLEditor.Services
{
    internal class ApiKeyManager
    {
        private readonly string _configFilePath;

        public ApiKeyManager(string configFilePath)
        {
            _configFilePath = configFilePath;
        }

        public void SaveApiKey(string apiKey)
        {
            byte[] entropy = new byte[16];
            using (var rng = new RNGCryptoServiceProvider())
            {
                rng.GetBytes(entropy);
            }

            byte[] encryptedData = ProtectedData.Protect(
                Encoding.UTF8.GetBytes(apiKey),
                entropy,
                DataProtectionScope.CurrentUser);

            using (var fs = new FileStream(_configFilePath, FileMode.Create))
            using (var bw = new BinaryWriter(fs))
            {
                bw.Write(entropy.Length);
                bw.Write(entropy);
                bw.Write(encryptedData.Length);
                bw.Write(encryptedData);
            }
        }

        public string GetApiKey()
        {
            try
            {
                if (!File.Exists(_configFilePath))
                    return null;

                using (var fs = new FileStream(_configFilePath, FileMode.Open))
                using (var br = new BinaryReader(fs))
                {
                    int entropyLength = br.ReadInt32();
                    byte[] entropy = br.ReadBytes(entropyLength);
                    int encryptedDataLength = br.ReadInt32();
                    byte[] encryptedData = br.ReadBytes(encryptedDataLength);

                    byte[] decryptedData = ProtectedData.Unprotect(
                        encryptedData,
                        entropy,
                        DataProtectionScope.CurrentUser);

                    return Encoding.UTF8.GetString(decryptedData);
                }
            }
            catch
            {
                return null;
            }
        }
    }
}

using Newtonsoft.Json;
using SAFE.NetworkDrive.Encryption;
using SAFE.NetworkDrive.Mounter.Config;
using Scrambler = SAFE.NetworkDrive.Gateways.Utils.Scrambler;
using System.IO;

namespace SAFE.NetworkDrive.Mounter
{
    class ConsoleApp
    {
        public UserConfig GetUserConfig()
        {
            var (user, pwd) = GetUserLogin();
            var config = DecrypUserConfig(user, pwd);
            return config;
        }

        (string user, string pwd) GetUserLogin()
        {
            var uReader = new UserReader();
            return (uReader.GetUserName(), uReader.GetPassword());
        }

        UserConfig DecrypUserConfig(string username, string password)
        {
            var userFolder = Scrambler.Obfuscate(username, password);
            var dirPath = "../sndc";
            var filePath = $"{dirPath}/{userFolder}".ToLowerInvariant();
            if (!File.Exists(filePath))
            {
                var config = CreateConfig(username);
                var bytes = BytesCrypto.Encrypt(password, JsonConvert.SerializeObject(config));
                if (!Directory.Exists(dirPath))
                    Directory.CreateDirectory(dirPath);
                File.WriteAllBytes(filePath, bytes);
                return config;
            }
            var data = File.ReadAllBytes(filePath);
            var json = BytesCrypto.Decrypt(password, data);
            return JsonConvert.DeserializeObject<UserConfig>(json);
        }

        UserConfig CreateConfig(string username)
        {
            var dReader = new DriveConfigReader();
            var config = new UserConfig
            {
                UserName = username,
                Drives = dReader.ConfigureDrives(username)
            };

            return config;
        }
    }
}
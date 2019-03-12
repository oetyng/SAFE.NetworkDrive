using Newtonsoft.Json;
using SAFE.NetworkDrive.Encryption;
using SAFE.NetworkDrive.Gateways.Utils;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace SAFE.NetworkDrive.Mounter.Config
{
    public class UserConfigHandler
    {
        public UserConfig CreateOrDecrypUserConfig(string username, string password)
        {
            var userFolder = Scrambler.Obfuscate(username, password);
            var dirPath = "../sndc";
            var filePath = $"{dirPath}/{userFolder}".ToLowerInvariant();
            if (!File.Exists(filePath))
            {
                var config = CreateUserConfig(username);
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

        UserConfig CreateUserConfig(string username)
        {
            var config = new UserConfig
            {
                Username = username,
                Drives = new List<DriveConfig>()
            };

            return config;
        }

        public DriveConfig CreateDriveConfig(char driveLetter, string location, string secret)
        {
            var dirPath = Path.DirectorySeparatorChar.ToString();
            return new DriveConfig
            {
                Locator = location,
                Secret = secret,
                Root = driveLetter.ToString(),
                Schema = "safenetwork",
                Parameters = $"root={dirPath}"
            };
        }

        public bool AddDrive(DriveConfig config, string username, string password)
        {
            var user = CreateOrDecrypUserConfig(username, password);
            if (user.Drives.Any(c => c.Root == config.Root))
                return false;

            user.Drives.Add(config);
            Save(user, password);
            return true;
        }

        public bool AddDrives(List<DriveConfig> config, string username, string password)
        {
            var user = CreateOrDecrypUserConfig(username, password);
            config.RemoveAll(c => user.Drives.Any(d => d.Root == c.Root));
            if (config.Count == 0)
                return false;

            user.Drives.AddRange(config);
            Save(user, password);
            return true;
        }

        void Save(UserConfig user, string password)
        {
            var bytes = BytesCrypto.Encrypt(password, JsonConvert.SerializeObject(user));
            var userFolder = Scrambler.Obfuscate(user.Username, password);
            var dirPath = "../sndc";
            var filePath = $"{dirPath}/{userFolder}".ToLowerInvariant();
            File.WriteAllBytes(filePath, bytes);
        }
    }
}
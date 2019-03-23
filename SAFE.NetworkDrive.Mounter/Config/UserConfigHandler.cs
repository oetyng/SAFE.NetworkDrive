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
        readonly string DIR_PATH = $"..{Path.DirectorySeparatorChar}sndc";
        readonly string _username;
        readonly string _password;

        public UserConfigHandler(string username, string password)
        {
            _username = username;
            _password = password;
        }

        public UserConfig CreateOrDecrypUserConfig()
        {
            var userFolder = Scrambler.Obfuscate(_username, _password);
            var filePath = $"{DIR_PATH}{Path.DirectorySeparatorChar}{userFolder}".ToLowerInvariant();
            if (!File.Exists(filePath))
            {
                var config = CreateUserConfig();
                var bytes = BytesCrypto.Encrypt(_password, JsonConvert.SerializeObject(config));
                if (!Directory.Exists(DIR_PATH))
                    Directory.CreateDirectory(DIR_PATH);
                File.WriteAllBytes(filePath, bytes);
                return config;
            }
            var data = File.ReadAllBytes(filePath);
            var json = BytesCrypto.Decrypt(_password, data);
            return JsonConvert.DeserializeObject<UserConfig>(json);
        }

        public bool DeleteUser()
        {
            var userFolder = Scrambler.Obfuscate(_username, _password);
            var filePath = $"{DIR_PATH}{Path.DirectorySeparatorChar}{userFolder}".ToLowerInvariant();
            if (!File.Exists(filePath))
                return false;
            File.Delete(filePath);
            return true;
        }

        UserConfig CreateUserConfig()
        {
            var config = new UserConfig
            {
                Username = _username,
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

        public bool AddDrive(DriveConfig config)
        {
            var user = CreateOrDecrypUserConfig();
            if (user.Drives.Any(c => c.Root == config.Root))
                return false;

            user.Drives.Add(config);
            Save(user);
            return true;
        }

        public bool RemoveDrive(char driveLetter)
        {
            var user = CreateOrDecrypUserConfig();
            var removed = user.Drives.RemoveAll(c => c.Root[0] == driveLetter);

            if (removed == 0)
                return false;

            Save(user);
            return true;
        }

        public bool AddDrives(List<DriveConfig> config)
        {
            var user = CreateOrDecrypUserConfig();
            config.RemoveAll(c => user.Drives.Any(d => d.Root == c.Root));
            if (config.Count == 0)
                return false;

            user.Drives.AddRange(config);
            Save(user);
            return true;
        }

        void Save(UserConfig user)
        {
            var bytes = BytesCrypto.Encrypt(_password, JsonConvert.SerializeObject(user));
            var userFolder = Scrambler.Obfuscate(user.Username, _password);
            var filePath = $"{DIR_PATH}{Path.DirectorySeparatorChar}{userFolder}".ToLowerInvariant();
            File.WriteAllBytes(filePath, bytes);
        }
    }
}
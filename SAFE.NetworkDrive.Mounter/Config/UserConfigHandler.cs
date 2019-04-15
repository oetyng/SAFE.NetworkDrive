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
        readonly string _password;
        readonly string _filePath;

        public UserConfigHandler(string password)
        {
            _password = password;
            var username = Scrambler.ShortCode(_password, _password);
            _filePath = $"{DIR_PATH}{Path.DirectorySeparatorChar}{username}".ToLowerInvariant();
        }

        public UserConfig CreateOrDecrypUserConfig()
        {
            if (!File.Exists(_filePath))
            {
                var config = CreateUserConfig();
                var bytes = BytesCrypto.Encrypt(_password, JsonConvert.SerializeObject(config));
                if (!Directory.Exists(DIR_PATH))
                    Directory.CreateDirectory(DIR_PATH);
                File.WriteAllBytes(_filePath, bytes);
                return config;
            }
            var data = File.ReadAllBytes(_filePath);
            var json = BytesCrypto.Decrypt(_password, data);
            return JsonConvert.DeserializeObject<UserConfig>(json);
        }

        public bool Equals(UserConfigHandler other) => _password == other._password;

        public bool DeleteUser()
        {
            if (!File.Exists(_filePath))
                return false;
            File.Delete(_filePath);
            return true;
        }

        UserConfig CreateUserConfig()
        {
            var config = new UserConfig
            {
                VolumeNrCheckpoint = 0,
                Drives = new List<DriveConfig>()
            };

            return config;
        }

        public DriveConfig CreateDriveConfig(char driveLetter)
        {
            var volumeNr = CreateOrDecrypUserConfig().VolumeNrCheckpoint;
            var volumeId = Scrambler.VolumeId(volumeNr, _password);
            var (locator, secret) = GetLocatorAndSecret(volumeId);

            //var dirPath = Path.DirectorySeparatorChar.ToString();
            return new DriveConfig
            {
                Locator = locator,
                Secret = secret,
                Root = driveLetter.ToString(),
                VolumeNr = volumeNr,
                VolumeId = volumeId,
                Schema = "safenetworkdrive_v1",
                //Parameters = $"root={dirPath}"
            };
        }

        (string, string) GetLocatorAndSecret(string volumeId)
        {
            var locator = GenerateString(volumeId);
            var secret = GenerateString(locator);
            return (locator, secret);
        }

        string GenerateString(string source)
        {
            var first = Scrambler.ShortCode(source, _password);
            var second = Scrambler.ShortCode(first, _password);
            var third = Scrambler.ShortCode(second, _password);
            var fourth = Scrambler.ShortCode(third, _password);
            return second + third + fourth;
        }

        public bool AddDrive(DriveConfig config)
        {
            var user = CreateOrDecrypUserConfig();
            if (user.Drives.Any(c => c.Root == config.Root))
                return false;
            if (config.VolumeNr != user.VolumeNrCheckpoint)
                return false;

            user.Drives.Add(config);
            ++user.VolumeNrCheckpoint;

            Save(user);
            return true;
        }

        public Data.Result<DriveConfig> TrySetDriveLetter(char driveLetter, char newDriveLetter)
        {
            if (driveLetter == newDriveLetter)
                return Data.Result.Fail<DriveConfig>(-999, "");

            var user = CreateOrDecrypUserConfig();
            var drive = user.Drives.SingleOrDefault(c => c.Root[0] == driveLetter);

            if (drive == null)
                return Data.Result.Fail<DriveConfig>(-999, "");

            drive.Root = newDriveLetter.ToString();

            Save(user);
            return Data.Result.OK(drive);
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
            File.WriteAllBytes(_filePath, bytes);
        }
    }
}
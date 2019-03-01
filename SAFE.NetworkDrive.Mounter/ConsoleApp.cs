using Newtonsoft.Json;
using SAFE.NetworkDrive.Encryption;
using SAFE.NetworkDrive.Mounter.Config;
using System.IO;

namespace SAFE.NetworkDrive.Mounter
{
    class ConsoleApp
    {
        public UserConfig GetUserConfig()
        {
            var (user, pwd) = GetLogin();
            var config = DecrypUserConfig(user, pwd);
            return config;
        }

        (string user, string pwd) GetLogin()
        {
            var csReader = new UserReader();
            return (csReader.GetUserName(), csReader.GetPassword());
        }

        UserConfig DecrypUserConfig(string username, string password)
        {
            var encrUsr = StringCrypto.Encrypt(password, username);
            var data = File.ReadAllBytes($"../snduc/{encrUsr}.dat");
            var json = BytesCrypto.Decrypt(password, data);
            return JsonConvert.DeserializeObject<UserConfig>(json);
        }
    }
}
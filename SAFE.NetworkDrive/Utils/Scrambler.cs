
namespace SAFE.NetworkDrive.Gateways.Utils
{
    class Scrambler
    {
        public static string Obfuscate(string val, string secretKey)
        {
            return Encryption.StringCrypto
                .Encrypt(secretKey, val)
                .ToDeterministicGuid()
                .ToString()
                .Substring(0, 5)
                .ToLowerInvariant();
        }
    }
}

namespace SAFE.NetworkDrive.Gateways.Utils
{
    class PathScrambler
    {
        public static string Obfuscate(string val, string secretKey)
        {
            return Encryption.StringCrypto
                .Encrypt(secretKey, val)
                .Substring(0, 5)
                .ToLowerInvariant();
        }
    }
}

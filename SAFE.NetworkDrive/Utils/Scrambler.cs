
namespace SAFE.NetworkDrive.Gateways.Utils
{
    class Scrambler
    {
        public static string ShortCode(string val, string secretKey)
        {
            return Encryption.StringCrypto
                .Encrypt(secretKey, val)
                .ToDeterministicGuid()
                .ToString("N")
                .Substring(0, 5);
        }

        public static string VolumeId(uint nr, string secretKey)
        {
            var volumeId = secretKey;
            for (int i = -1; i < nr; i++)
                volumeId = GetVolumeId(volumeId);
            return volumeId;
        }

        static string GetVolumeId(string source)
        {
            return Encryption.StringCrypto
                .Encrypt(source, source)
                .ToDeterministicGuid()
                .ToString("N");
        }
    }
}
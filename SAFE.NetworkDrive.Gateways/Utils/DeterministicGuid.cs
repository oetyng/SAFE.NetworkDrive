using System;
using System.Security.Cryptography;
using System.Text;

namespace SAFE.NetworkDrive.Gateways.Utils
{
    public static class DeterministicGuid
    {
        public static Guid ToDeterministicGuid(this string value)
        {
            if (IsEmpty(value))
                return Guid.Empty;

            var input = Encoding.UTF8.GetBytes(value);

            var md5 = MD5.Create();
            var hash = md5.ComputeHash(input);
            return new Guid(hash);
        }

        static bool IsEmpty(string value)
        {
            return string.IsNullOrEmpty(value) || string.IsNullOrWhiteSpace(value);
        }
    }
}

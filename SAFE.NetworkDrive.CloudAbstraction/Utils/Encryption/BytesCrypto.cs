using System;
using System.Security.Cryptography;

namespace SAFE.NetworkDrive.Encryption
{
    public class BytesCrypto : Crypto
    {
        public static byte[] EncryptFromBytes(string key, byte[] data)
        {
            var stringData = Convert.ToBase64String(data);

            byte[] encData = null;
            byte[][] keys = GetHashKeys(key);

            try
            {
                encData = EncryptStringToBytes_Aes(stringData, keys[0], keys[1]);
            }
            catch (CryptographicException) { }
            catch (ArgumentNullException) { }

            return encData;
        }

        public static byte[] DecryptToBytes(string key, byte[] data)
        {
            byte[] decData = null;
            byte[][] keys = GetHashKeys(key);

            try
            {
                var stringData = DecryptStringFromBytes_Aes(data, keys[0], keys[1]);
                decData = Convert.FromBase64String(stringData);
            }
            catch (CryptographicException) { }
            catch (ArgumentNullException) { }

            return decData;
        }

        public static byte[] Encrypt(string key, string data)
        {
            byte[] encData = null;
            byte[][] keys = GetHashKeys(key);

            try
            {
                encData = EncryptStringToBytes_Aes(data, keys[0], keys[1]);
            }
            catch (CryptographicException) { }
            catch (ArgumentNullException) { }

            return encData;
        }

        public static string Decrypt(string key, byte[] data)
        {
            string decData = null;
            byte[][] keys = GetHashKeys(key);

            try
            {
                decData = DecryptStringFromBytes_Aes(data, keys[0], keys[1]);
            }
            catch (CryptographicException) { }
            catch (ArgumentNullException) { }

            return decData;
        }
    }
}
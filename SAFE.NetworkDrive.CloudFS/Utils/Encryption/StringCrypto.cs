using System;
using System.Security.Cryptography;

namespace SAFE.NetworkDrive.Encryption
{
    public class StringCrypto : Crypto
    {
        public static string Encrypt(string key, string data)
        {
            string encData = null;
            byte[][] keys = GetHashKeys(key);

            try
            {
                encData = EncryptStringToString_Aes(data, keys[0], keys[1]);
            }
            catch (CryptographicException) { }
            catch (ArgumentNullException) { }

            return encData;
        }

        public static string Decrypt(string key, string data)
        {
            string decData = null;
            byte[][] keys = GetHashKeys(key);

            try
            {
                decData = DecryptStringFromString_Aes(data, keys[0], keys[1]);
            }
            catch (CryptographicException) { }
            catch (ArgumentNullException) { }

            return decData;
        }

        static string EncryptStringToString_Aes(string plainText, byte[] Key, byte[] IV)
        {
            byte[] encrypted = EncryptStringToBytes_Aes(plainText, Key, IV);
            var cipherTextString = Convert.ToBase64String(encrypted);
            return cipherTextString;
        }

        static string DecryptStringFromString_Aes(string cipherTextString, byte[] Key, byte[] IV)
        {
            byte[] cipherText = Convert.FromBase64String(cipherTextString);
            string plaintext = DecryptStringFromBytes_Aes(cipherText, Key, IV);
            return plaintext;
        }
    }
}
using System;
using System.IO;
using System.Security.Cryptography;

namespace SAFE.NetworkDrive.Encryption
{
    public class StreamCrypto : Crypto
    {
        public static MemoryStream Encrypt(string key, Stream data)
        {
            MemoryStream encData = null;
            byte[][] keys = GetHashKeys(key);

            try
            {
                encData = Encrypt_Aes(data, keys[0], keys[1]);
            }
            catch (CryptographicException) { }
            catch (ArgumentNullException) { }

            return encData;
        }

        public static Stream Decrypt(string key, Stream data)
        {
            Stream decData = null;
            byte[][] keys = GetHashKeys(key);

            try
            {
                decData = Decrypt_Aes(data, keys[0], keys[1]);
            }
            catch (CryptographicException) { }
            catch (ArgumentNullException) { }

            return decData;
        }

        //source: https://msdn.microsoft.com/de-de/library/system.security.cryptography.aes(v=vs.110).aspx
        static MemoryStream Encrypt_Aes(Stream dataStream, byte[] Key, byte[] IV)
        {
            if (dataStream == null || dataStream.Length <= 0)
                throw new ArgumentNullException("data");
            if (Key == null || Key.Length <= 0)
                throw new ArgumentNullException("Key");
            if (IV == null || IV.Length <= 0)
                throw new ArgumentNullException("IV");

            var encryptedResult = new MemoryStream();

            using (var aesAlg = new AesManaged())
            {
                aesAlg.Key = Key;
                aesAlg.IV = IV;

                ICryptoTransform encryptor = aesAlg.CreateEncryptor(aesAlg.Key, aesAlg.IV);
                var encyptedStream = new MemoryStream();
                using (var csEncrypt = new CryptoStream(encyptedStream, encryptor, CryptoStreamMode.Write))
                {
                    using (dataStream)
                    {
                        int data;
                        while ((data = dataStream.ReadByte()) != -1)
                            csEncrypt.WriteByte((byte)data);
                    }
                    encyptedStream.CopyTo(encryptedResult);
                    encryptedResult.Seek(0, SeekOrigin.Begin);
                }
            }
            
            return encryptedResult;
        }

        //source: https://msdn.microsoft.com/de-de/library/system.security.cryptography.aes(v=vs.110).aspx
        static Stream Decrypt_Aes(Stream encryptedStream, byte[] Key, byte[] IV)
        {
            if (encryptedStream == null || encryptedStream.Length <= 0)
                throw new ArgumentNullException("cipherText");
            if (Key == null || Key.Length <= 0)
                throw new ArgumentNullException("Key");
            if (IV == null || IV.Length <= 0)
                throw new ArgumentNullException("IV");

            var decryptedStream = new MemoryStream();

            using (var aesAlg = Aes.Create())
            {
                aesAlg.Key = Key;
                aesAlg.IV = IV;

                ICryptoTransform decryptor = aesAlg.CreateDecryptor(aesAlg.Key, aesAlg.IV);

                using (var csDecrypt = new CryptoStream(encryptedStream, decryptor, CryptoStreamMode.Read))
                {
                    int data;

                    while ((data = csDecrypt.ReadByte()) != -1)
                        decryptedStream.WriteByte((byte)data);
                }
            }

            decryptedStream.Seek(0, SeekOrigin.Begin);
            return decryptedStream;
        }
    }
}
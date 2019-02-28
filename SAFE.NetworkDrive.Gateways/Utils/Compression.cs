using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text;

namespace SAFE.NetworkDrive.Gateways.Utils
{
    public static class CompressionHelper
    {
        public static byte[] Compress(this byte[] data)
        {
            byte[] compressArray = null;
            try
            {
                using (var memoryStream = new MemoryStream())
                {
                    using (var deflateStream = new DeflateStream(memoryStream, CompressionMode.Compress))
                    {
                        deflateStream.Write(data, 0, data.Length);
                    }
                    compressArray = memoryStream.ToArray();
                }
            }
            catch (Exception ex)
            {
                // do something !
            }
            return compressArray;
        }

        public static byte[] Decompress(this byte[] data)
        {
            byte[] decompressedArray = null;
            try
            {
                using (var decompressedStream = new MemoryStream())
                {
                    using (var compressStream = new MemoryStream(data))
                    {
                        using (var deflateStream = new DeflateStream(compressStream, CompressionMode.Decompress))
                        {
                            deflateStream.CopyTo(decompressedStream);
                        }
                    }
                    decompressedArray = decompressedStream.ToArray();
                }
            }
            catch (Exception ex)
            {
                // do something !
            }

            return decompressedArray;
        }
    }
}

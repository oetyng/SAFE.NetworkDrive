using SAFE.NetworkDrive.Gateways.Events;
using SAFE.NetworkDrive.Gateways.Utils;
using System;
using System.Collections.Concurrent;
using System.Text;
using System.Threading.Tasks;

namespace SAFE.NetworkDrive.Gateways.AsyncWAL
{
    class EncryptedInfo
    {
        // Event => json => bytes => compressed => Encrypted => 
    }
    class SAFENetworkEventService
    {
        const int MAX_BYTES = 20_000;

        public Task<bool> Upload(byte[] zipEncryptedBytes)
        {
            var e = ZipEncryptedEvent.From(zipEncryptedBytes);
            var entry = new WALEntry();
            var bytes = e.ZipEncryptedData;

            // the data we pass in here, can be large, i.e. contain file content and so on
            // if it is small we can store directly in the WALEntry, otherwise
            // we need to upload it to an immutable data
            if (MAX_BYTES > bytes.Length)
                entry.EventOrDatamap = bytes;
            else
                entry.EventOrDatamap = GetDatamap(bytes, e.AssemblyQualifiedName);

            Append(entry);

            return Task.FromResult(true);
        }

        ConcurrentQueue<byte[]> _data = new ConcurrentQueue<byte[]>();
        void Append(WALEntry entry)
        {
            // get reference to the relevant AD 
            // append entry.EventOrDatamap to it
            _data.Enqueue(entry.EventOrDatamap);
        }

        byte[] GetDatamap<T>(byte[] data)
        {
            return GetDatamap(data, typeof(T).AssemblyQualifiedName);
        }

        byte[] GetDatamap(byte[] data, string assemblyQualifiedName)
        {
            // this is mocking that we
            // upload as immutable data, 
            // and get a datamap back, which is smaller
            var smallerSize = data.Length / 2;
            var smaller = new byte[smallerSize];
            data.CopyTo(smaller, smallerSize);

            var map = new Datamap
            {
                AssemblyQualifiedName = assemblyQualifiedName,
                Data = smaller
            };

            var datamapBytes = map.GetBytes();

            // while the datamap is larger than MAX_BYTES, we need to
            // create a datamap out of the datamap.
            if (datamapBytes.Length > MAX_BYTES)
                return GetDatamap<Datamap>(datamapBytes);

            return datamapBytes;
        }
    }

    class Datamap
    {
        [NonSerialized]
        byte[] _bytes;

        public string AssemblyQualifiedName;
        public byte[] Data;
        public byte[] GetBytes()
        {
            if (_bytes == null)
                _bytes = Encoding.UTF8.GetBytes(this.Json());
            return _bytes;
        }
    }

    class WALEntry
    {
        // If the size is small, we don't need to 
        // store it in immutable data. So then this will
        // contain the actual event. Otherwise, 
        public byte[] EventOrDatamap { get; set; }
    }
}

using SAFE.AppendOnlyDb;
using SAFE.NetworkDrive.Replication.Events;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SAFE.NetworkDrive.Gateways.AsyncEvents
{
    class SAFENetworkEventService
    {
        readonly IStreamAD _stream;
        readonly string _pwd;

        public SAFENetworkEventService(IStreamAD stream, string pwd)
        {
            _stream = stream;
            _pwd = pwd;
        }

        // Pass in data that was encrypted at rest
        // for upload to SAFENetwork.
        public async Task<bool> Upload(byte[] zipEncryptedBytes)
        {
            var data = ZipEncryptedEvent.From(zipEncryptedBytes);
            var evt = data.GetEvent(_pwd);
            var result = await _stream.AppendAsync(new StoredValue(evt));
            return result.HasValue;
        }

        // Event => json => bytes => compressed => Encrypted => byte[]
        public IAsyncEnumerable<Event> LoadAsync(ulong version)
        {
            var data = _stream.ReadForwardFromAsync(version);

            //var bag = new ConcurrentBag<Event>();
            //Parallel.ForEach(data, c =>
            //{
            //    var e = ZipEncryptedEvent.From(c);
            //    bag.Add(e.GetEvent(pwd));
            //});
            return data
                .Select(c => c.Item2)
                .Select(c => c.Parse<Event>());
        }
    }
}
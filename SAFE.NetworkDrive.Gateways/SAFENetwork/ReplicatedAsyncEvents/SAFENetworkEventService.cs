using SAFE.DataStore;
using SAFE.NetworkDrive.Gateways.Events;
using SAFE.NetworkDrive.Gateways.Utils;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SAFE.NetworkDrive.Gateways.AsyncEvents
{
    class SAFENetworkEventService
    {
        readonly IDatabase _db;

        public SAFENetworkEventService(IDatabase db)
            => _db = db;

        public async Task<bool> Upload(byte[] zipEncryptedBytes)
        {
            var result = await _db.AddAsync(SequentialGuid.NewGuid().ToString(), zipEncryptedBytes);
            return result.HasValue;
        }

        // Event => json => bytes => compressed => Encrypted => byte[]
        public async Task<IEnumerable<Event>> LoadAsync(string pwd)
        {
            var data = await _db.GetAllAsync<byte[]>();
            var bag = new ConcurrentBag<Event>();
            Parallel.ForEach(data, c =>
            {
                var e = ZipEncryptedEvent.From(c);
                bag.Add(e.GetEvent(pwd));
            });
            return bag.OrderBy(c => c.Id);
        }
    }
}
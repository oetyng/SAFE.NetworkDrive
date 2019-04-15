
using SAFE.Data.Utils;
using System;
using System.Threading.Tasks;

namespace SAFE.NetworkDrive.Gateways.AsyncEvents
{
    class SequenceNr
    {
        readonly Guid _id = Guid.NewGuid();
        readonly AsyncDuplicateLock _asyncLock = new AsyncDuplicateLock();

        public ulong? Value { get; private set; }
        public ulong Next => Value.HasValue ? (ulong)Value + 1 : 0;

        public void Set(ulong sequenceNr)
            => Value = sequenceNr;

        public bool IsValidSequence(ulong sequenceNr)
        {
            if (!Value.HasValue && sequenceNr != 0)
                return false;
            else if (Value.HasValue && Value != sequenceNr - 1)
                return false;
            return true;
        }

        public IDisposable Lock() => _asyncLock.Lock(_id);
        public Task<IDisposable> LockAsync() => _asyncLock.LockAsync(_id);
    }
}
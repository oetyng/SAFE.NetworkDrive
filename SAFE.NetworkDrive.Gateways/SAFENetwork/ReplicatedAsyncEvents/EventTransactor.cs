
using SAFE.NetworkDrive.Gateways.Events;
using System;
using System.Collections.Generic;
using System.Threading;

namespace SAFE.NetworkDrive.Gateways.AsyncWAL
{
    class EventTransactor
    {
        readonly DriveWriter _materializer;
        readonly NonIntrusiveDiskQueueWorker _queueWorker;
        readonly string _password;

        public EventTransactor(DriveWriter materializer, NonIntrusiveDiskQueueWorker synch, string password)
        {
            _materializer = materializer;
            _queueWorker = synch;
            _password = password;
        }

        public void Start(CancellationToken cancellation)
        {
            _queueWorker.Start(cancellation);
        }

        public bool Transact(Event e)
        {
            return Transact<object>(e).Item1;
        }

        public (bool, T) Transact<T>(Event e)
        {
            try
            {
                var data = ZipEncryptedEvent.For(e, _password).GetBytes();
                return _queueWorker.Enqueue<T>(data,
                    onEnqueued: () => _materializer.Apply(e));
            }
            catch (Exception ex)
            {
                return (false, default);
            }
        }

        public List<Event> GetNetworkWAL(long fromVersion = 0)
        {
            return new List<Event>();
        }
    }
}

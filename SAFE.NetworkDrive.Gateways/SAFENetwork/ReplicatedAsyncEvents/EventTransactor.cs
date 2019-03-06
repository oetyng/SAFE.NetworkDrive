
using SAFE.NetworkDrive.Gateways.Events;
using System;
using System.Threading;

namespace SAFE.NetworkDrive.Gateways.AsyncEvents
{
    class EventTransactor
    {
        readonly DriveWriter _driveWriter;
        readonly DiskQueueWorker _queueWorker;
        readonly string _password;

        public EventTransactor(DriveWriter driveWriter, DiskQueueWorker synch, string password)
        {
            _driveWriter = driveWriter;
            _queueWorker = synch;
            _password = password;
        }

        public void Start(CancellationToken cancellation)
            => _queueWorker.Start(cancellation);

        public bool Transact(Event e)
            => Transact<object>(e).Item1;

        public (bool, T) Transact<T>(Event e)
        {
            try
            {
                var data = ZipEncryptedEvent.For(e, _password).GetBytes();
                return _queueWorker.Enqueue<T>(data,
                    onEnqueued: () => _driveWriter.Apply(e));
            }
            catch (Exception ex)
            {
                return (false, default);
            }
        }

        internal long ReadSequenceNr()
        {
            return 0L;
            // fetch NetworkWAL (can be snapshot + all events since)
            // recreate the filesystem locally - but encrypted
            //RootName root)
        }
    }
}

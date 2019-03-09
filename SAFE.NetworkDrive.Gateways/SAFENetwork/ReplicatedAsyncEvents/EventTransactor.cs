
using SAFE.NetworkDrive.Replication.Events;
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

        public bool Transact(LocalEvent e)
            => Transact<object>(e).Item1;

        public (bool, T) Transact<T>(LocalEvent e)
        {
            try
            {
                var data = ZipEncryptedEvent.For(e, _password).GetBytes();
                var locator = new WALContent { EncryptedContent = data, SequenceNr = e.SequenceNr };
                return _queueWorker.Enqueue<T>(locator,
                    onEnqueued: () => _driveWriter.Apply(e));
            }
            catch
            {
                return (false, default);
            }
        }
    }
}
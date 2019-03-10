
using SAFE.NetworkDrive.Replication.Events;
using System.Threading;

namespace SAFE.NetworkDrive.Gateways.AsyncEvents
{
    /// <summary>
    /// Receives drive write requests in form of
    /// events. Transacts it into a disk persisted WAL (encrypted)
    /// as well as into the DriveWriter, which writes to
    /// the in memory representation of the file system.
    /// </summary>
    class EventTransactor
    {
        readonly DriveWriter _driveWriter;
        readonly DiskWALTransactor _wal;
        readonly string _password;

        public EventTransactor(DriveWriter driveWriter, DiskWALTransactor wal, string password)
        {
            _driveWriter = driveWriter;
            _wal = wal;
            _password = password;
        }

        public void Start(CancellationToken cancellation)
            => _wal.StartDequeueing(cancellation);

        public bool Transact(LocalEvent e)
            => Transact<object>(e).Item1;

        public (bool, T) Transact<T>(LocalEvent e)
        {
            try
            {
                var data = ZipEncryptedEvent.For(e, _password).GetBytes();
                var content = new WALContent
                {
                    EncryptedContent = data,
                    SequenceNr = e.SequenceNr
                };
                return _wal.Enqueue<T>(content, onEnqueued: () => _driveWriter.Apply(e));
            }
            catch
            {
                return (false, default);
            }
        }
    }
}
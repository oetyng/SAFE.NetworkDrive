using SAFE.AppendOnlyDb.Snapshots;
using SAFE.NetworkDrive.Gateways.AsyncEvents;
using SAFE.NetworkDrive.MemoryFS;
using SAFE.NetworkDrive.Interface;
using SAFE.NetworkDrive.Replication.Events;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SAFE.NetworkDrive.Snapshots
{
    public class DriveSnapshotFactory
    {
        readonly RootName _root;

        public DriveSnapshotFactory(RootName root)
            => _root = root;

        public async Task<(MemoryGateway, SequenceNr)> ReadFromSnapshotAsync(AppendOnlyDb.IStreamAD stream, Data.Client.IImDStore imdStore)
        {
            var snapshotter = GetSnapshotter(imdStore);
            var snapshotReading = await stream.ReadFromSnapshot();
            if (!snapshotReading.HasValue)
                return (new MemoryGateway(_root), new SequenceNr());

            Snapshot snapshot = default;
            IAsyncEnumerable<NetworkEvent> events = snapshotReading.Value.NewEvents.Select(c => c.Item2.Parse<NetworkEvent>());
            
            if (snapshotReading.Value.SnapshotMap != null) // load all events from network (since we don't store it locally)
                snapshot = await snapshotter.GetSnapshotAsync(snapshotReading.Value.SnapshotMap);

            var currentState = await MaterializeAsync(snapshot, events);
            return currentState;
        }

        public Snapshotter<NetworkEvent> GetSnapshotter(Data.Client.IImDStore store)
            => new Snapshotter<NetworkEvent>(store, SnapshotFunc);

        async Task<Snapshot> SnapshotFunc(Snapshot previousSnapshot, IAsyncEnumerable<NetworkEvent> changes)
        {
            var currentState = await MaterializeAsync(previousSnapshot, changes);
            var snapshot = new Snapshot(currentState);
            return snapshot;
        }

        async Task<(MemoryGateway, SequenceNr)> MaterializeAsync(Snapshot previousSnapshot, IAsyncEnumerable<NetworkEvent> changes)
        {
            var currentState = new MemoryGateway(_root);

            if (previousSnapshot != null)
                currentState = previousSnapshot.GetState<MemoryGateway>();

            var sequenceNr = new SequenceNr();
            sequenceNr.Set((await changes.FirstAsync()).SequenceNr - 1);

            var materializer = new DriveMaterializer(currentState, sequenceNr);

            var isMaterialized = await materializer.Materialize(changes);
            if (!isMaterialized)
                throw new System.IO.InvalidDataException("Could not materialize network filesystem!");

            return (currentState, sequenceNr);
        }
    }
}
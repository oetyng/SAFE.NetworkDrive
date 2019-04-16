using SAFE.AppendOnlyDb.Snapshots;
using SAFE.NetworkDrive.Gateways.AsyncEvents;
using SAFE.NetworkDrive.Gateways.Memory;
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

            var snapshot = await snapshotter.GetSnapshotAsync(snapshotReading.Value.SnapshotMap);
            var currentState = await MaterializeAsync(snapshot, snapshotReading.Value.NewEvents.Select(c => c.Item2.Parse<NetworkEvent>()));
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

            await materializer.Materialize(changes);

            return (currentState, sequenceNr);
        }
    }
}
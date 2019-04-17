using SAFE.NetworkDrive.Gateways.AsyncEvents;
using SAFE.NetworkDrive.MemoryFS;
using SAFE.NetworkDrive.Interface;
using SAFE.NetworkDrive.Parameters;
using SAFE.NetworkDrive.Snapshots;
using System.Threading.Tasks;

namespace SAFE.NetworkDrive
{
    internal sealed class SAFEDriveFactory
    {
        internal ISAFEDrive CreateDrive(string schema, string volumeId, string root, SAFEDriveParameters parameters)
        {
            var rootName = new RootName(schema, volumeId, root);
            var context = GetContextAsync(rootName, parameters).GetAwaiter().GetResult();
            var gateway = new SAFENetworkGateway(context);
            return new SAFEDrive(rootName, gateway);
        }

        async Task<SAFENetworkContext> GetContextAsync(RootName root, SAFEDriveParameters parameters)
        {
            var snapshotter = new DriveSnapshotFactory(root);
            var dbFactory = new DbFactory(parameters);
            var (stream, store) = await dbFactory.GetDriveDbsAsync(root.VolumeId, snapshotter.GetSnapshotter);
            var network = new SAFENetworkEventService(stream, store, parameters.Secret);

            // loads snapshot + all events since, and materializes into current state, to be built on locally
            // events from network that are materialized, could be from this machine or any machine writing to same stream
            // thus there is a risk of conclict (read more further down)
            var (localState, sequenceNr) = await network.LoadStateAsync(snapshotter.ReadFromSnapshotAsync);
            var driveCache = new SAFENetworkDriveCache(store, localState);

            var materializer = new DriveMaterializer(localState, sequenceNr);
            var conflictHandler = new VersionConflictHandler(network, materializer);
            var driveWriter = new DriveWriter(driveCache, sequenceNr);

            var dbName = Gateways.Utils.Scrambler.ShortCode(root.VolumeId, parameters.Secret);
            var transactor = new EventTransactor(driveWriter,
                new DiskWALTransactor(dbName, conflictHandler.Upload), parameters.Secret);

            // All events in local WAL gets persisted to network
            // after we materialize events from network.
            // The version conflict handler will try to resolve the conflicts. 
            // Beware: Currently (as it is not implemented) there is no merging done, only naively appending, which could lead to corrupt state.
            transactor.Start(parameters.Cancellation); // start uploading to network
            while (DiskWALTransactor.AnyInQueue(dbName)) // wait until queue is empty
                await Task.Delay(500); // Beware: this will - currently - spin eternally if there is an unresolved version conflict

            var context = new SAFENetworkContext(transactor, new DriveReader(driveCache), sequenceNr);
            return context;
        }
    }
}
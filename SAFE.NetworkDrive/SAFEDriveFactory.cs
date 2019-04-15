using SAFE.NetworkDrive.Gateways.AsyncEvents;
using SAFE.NetworkDrive.Interface;
using SAFE.NetworkDrive.Parameters;
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
            var dbFactory = new DbFactory(parameters);
            var (stream, store) = await dbFactory.GetDriveDbsAsync(root.VolumeId, null);

            var localState = new Gateways.Memory.MemoryGateway(root);
            var driveCache = new Gateways.Memory.SAFENetworkDriveCache(store, localState);
            var network = new SAFENetworkEventService(stream, store, parameters.Secret);

            var sequenceNr = new SequenceNr();

            var materializer = new DriveMaterializer(localState, sequenceNr);
            var conflictHandler = new VersionConflictHandler(network, materializer);
            var driveWriter = new DriveWriter(driveCache, sequenceNr);

            var dbName = Gateways.Utils.Scrambler.ShortCode(root.VolumeId, parameters.Secret);
            var transactor = new EventTransactor(driveWriter,
                new DiskWALTransactor(dbName, conflictHandler.Upload), parameters.Secret);
            var context = new SAFENetworkContext(transactor, new DriveReader(driveCache), sequenceNr);

            var _ = driveCache.GetDrive(); // needs to be loaded

            // We need to wait for all events in local WAL to have been persisted to network
            // before we materialize new events from network.
            transactor.Start(parameters.Cancellation); // start uploading to network
            while (DiskWALTransactor.AnyInQueue(dbName)) // wait until queue is empty
                await Task.Delay(500); // beware, this will - currently - spin eternally if there is an unresolved version conflict

            // (todo: should load snapshot + all events since)
            var allEvents = network.LoadAsync(fromVersion: 0); // load all events from network (since we don't store it locally)
            var isMaterialized = await materializer.Materialize(allEvents); // recreate the filesystem locally in memory
            if (!isMaterialized)
                throw new System.IO.InvalidDataException("Could not materialize network filesystem!");
            return context;
        }
    }
}
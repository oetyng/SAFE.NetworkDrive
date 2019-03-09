using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Authentication;
using System.Threading;
using System.Threading.Tasks;
using Polly;
using Polly.Retry;
using SAFE.AppendOnlyDb;
using SAFE.AppendOnlyDb.Factories;
using SAFE.Data.Client;
using SAFE.Data.Client.Auth;
using SAFE.NetworkDrive.IO;
using SAFE.NetworkDrive.Interface;
using SAFE.NetworkDrive.Interface.Composition;
using SAFE.NetworkDrive.Interface.IO;
using SAFE.NetworkDrive.Replication.Events;

namespace SAFE.NetworkDrive.Gateways.AsyncEvents
{
    [System.Diagnostics.DebuggerDisplay("{DebuggerDisplay(),nq}")]
    public sealed class DiskReplicatedSAFEGateway : IAsyncCloudGateway, IPersistGatewaySettings
    {
        class SAFENetworkContext
        {
            public EventTransactor Writer { get; }
            public DriveReader Reader { get; }

            public SAFENetworkContext(EventTransactor writer, DriveReader reader)
            {
                Writer = writer;
                Reader = reader;
            }
        }

        //readonly AsyncRetryPolicy _retryPolicy = Policy.Handle<ServiceException>().WaitAndRetryAsync(5, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)));
        readonly IDictionary<RootName, SAFENetworkContext> _contextCache = new Dictionary<RootName, SAFENetworkContext>();
        readonly string _secretKey;
        long _sequenceNr;

        public DiskReplicatedSAFEGateway(string secretKey)
            => _secretKey = secretKey;

        async Task<SAFENetworkContext> RequireContextAsync(RootName root, string apiKey = null)
        {
            if (root == null)
                throw new ArgumentNullException(nameof(root));

            if (!_contextCache.TryGetValue(root, out SAFENetworkContext result))
            {
                var appInfo = new AppInfo
                {
                    Id = "safe.networkdrive",
                    Name = "SAFE.NetworkDrive",
                    Vendor = "oetyng"
                };
                SAFEClient.SetFactory(async (sess, app, db) => (object)await StreamDbFactory.CreateForApp(sess, app, db));
                var factory = new ClientFactory(appInfo, (session, appId) => new SAFEClient(session, appId));

                var client = await factory.GetMockNetworkClient(Credentials(apiKey, _secretKey));
                var db = await client.GetOrAddDbAsync<IStreamDb>(root.Value);
                var stream = await db.GetOrAddStreamAsync(root.Root);

                var store = client.GetImDStore();
                var replicatedGateway = new Memory.MemoryReplicatedSAFEGateway(store);
                

                var service = new SAFENetworkEventService(stream.Value, store, _secretKey);
                var materializer = new DriveMaterializer(root, replicatedGateway);
                var conflictHandler = new VersionConflictHandler(service, materializer);

                var driveWriter = new DriveWriter(root, replicatedGateway);

                var transactor = new EventTransactor(
                    driveWriter,
                    new DiskQueueWorker(conflictHandler.Upload), _secretKey);
                _contextCache.Add(root, result = new SAFENetworkContext(transactor, new DriveReader(replicatedGateway)));

                // We need to wait for all events in local WAL to have been persisted to network
                // before we materialize new events from network
                transactor.Start(CancellationToken.None); // start uploading to network
                while (DiskQueueWorker.AnyInQueue()) // wait until queue is empty
                    await Task.Delay(500); // beware, this will spin eternally if there is an unresolved version conflict

                _sequenceNr = DiskQueueWorker.GetVersion(); // read version from local db
                var readFrom = _sequenceNr > -1 ? (ulong)_sequenceNr : 0;
                var newEvents = service.LoadAsync(readFrom); // load new events from network // (todo: can be snapshot + all events since)
                var materialized = await materializer.Materialize(newEvents); // recreate the filesystem locally in memory
                if (!materialized)
                    throw new Exception();
            }

            return result;
        }

        Credentials Credentials(string locator, string secret)
            => new Credentials(locator, secret);

        public async Task<bool> TryAuthenticateAsync(RootName root, string apiKey, IDictionary<string, string> parameters)
        {
            try
            {
                await RequireContextAsync(root, apiKey);
                return true;
            }
            catch (AuthenticationException) { return false; }
        }

        public async Task<DriveInfoContract> GetDriveAsync(RootName root, string apiKey, 
            IDictionary<string, string> parameters)
            => (await RequireContextAsync(root)).Reader.GetDrive(root, apiKey, parameters);

        public async Task<RootDirectoryInfoContract> GetRootAsync(RootName root, string apiKey, 
            IDictionary<string, string> parameters)
            => (await RequireContextAsync(root)).Reader.GetRoot(root, apiKey, parameters);

        public async Task<IEnumerable<FileSystemInfoContract>> GetChildItemAsync(RootName root, DirectoryId parent)
            => (await RequireContextAsync(root)).Reader.GetChildItem(root, parent);

        public async Task<Stream> GetContentAsync(RootName root, FileId source)
            => (await RequireContextAsync(root)).Reader.GetContent(root, source);

        public Task<bool> SetContentAsync(RootName root, FileId target, Stream content, 
            IProgress<ProgressValue> progress, Func<FileSystemInfoLocator> locatorResolver)
            => Transact(root,
                (sequenceNr) => new LocalFileContentSet(sequenceNr, target.Value, content.ReadFully()));

        public Task<bool> ClearContentAsync(RootName root, FileId target, Func<FileSystemInfoLocator> locatorResolver)
            => Transact(root,
                (sequenceNr) => new LocalFileContentCleared(sequenceNr, target.Value));

        public Task<bool> RemoveItemAsync(RootName root, FileSystemId target, bool recurse)
            => Transact(root,
                (sequenceNr) => new LocalItemRemoved(sequenceNr, target.Value, GetType(target), recurse));

        public Task<FileInfoContract> NewFileItemAsync(RootName root, DirectoryId parent,
            string name, Stream content, IProgress<ProgressValue> progress)
            => Transact<FileInfoContract>(root,
                (sequenceNr) => new LocalFileItemCreated(sequenceNr, parent.Value, name, content.ReadFully()));

        public Task<FileSystemInfoContract> CopyItemAsync(RootName root, FileSystemId source, 
            string copyName, DirectoryId destination, bool recurse)
            => Transact<FileSystemInfoContract>(root,
                (sequenceNr) => new LocalItemCopied(sequenceNr, source.Value, GetType(source), copyName, destination.Value, recurse));

        public Task<FileSystemInfoContract> MoveItemAsync(RootName root, FileSystemId source, 
            string moveName, DirectoryId destination, Func<FileSystemInfoLocator> locatorResolver)
            => Transact<FileSystemInfoContract>(root,
                (sequenceNr) => new LocalItemMoved(sequenceNr, source.Value, GetType(source), moveName, destination.Value));

        public Task<DirectoryInfoContract> NewDirectoryItemAsync(RootName root, DirectoryId parent, string name)
            => Transact<DirectoryInfoContract>(root,
                (sequenceNr) => new LocalDirectoryItemCreated(sequenceNr, parent.Value, name));

        public Task<FileSystemInfoContract> RenameItemAsync(RootName root, FileSystemId target, 
            string newName, Func<FileSystemInfoLocator> locatorResolver)
            => Transact<FileSystemInfoContract>(root, 
                (sequenceNr) => new LocalItemRenamed(sequenceNr, target.Value, GetType(target), newName));

        FSType GetType(FileSystemId id) => id is DirectoryId ? FSType.Directory : FSType.File;

        async Task<T> Transact<T>(RootName root, Func<ulong, LocalEvent> func)
        {
            var context = await RequireContextAsync(root);
            var sequenceNr = (ulong)(_sequenceNr + 1);
            var e = func(sequenceNr);
            var info = context.Writer.Transact<T>(e);
            if (info.Item1) Interlocked.Increment(ref _sequenceNr);
            return info.Item2;
        }

        async Task<bool> Transact(RootName root, Func<ulong, LocalEvent> func)
        {
            var context = await RequireContextAsync(root);
            var sequenceNr = (ulong)(_sequenceNr + 1);
            var e = func(sequenceNr);
            var info = context.Writer.Transact(e);
            if (info) Interlocked.Increment(ref _sequenceNr);
            return info;
        }

        public void PurgeSettings(RootName root)
        {
            //OAuthAuthenticator.PurgeRefreshToken(root?.UserName);
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Justification = "Debugger Display")]
        [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
        static string DebuggerDisplay() => nameof(DiskReplicatedSAFEGateway);
    }
}
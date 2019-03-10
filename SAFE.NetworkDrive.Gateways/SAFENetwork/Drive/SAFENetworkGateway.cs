using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Authentication;
using System.Threading;
using System.Threading.Tasks;
using Polly;
using Polly.Retry;
using SAFE.NetworkDrive.IO;
using SAFE.NetworkDrive.Interface;
using SAFE.NetworkDrive.Interface.Composition;
using SAFE.NetworkDrive.Interface.IO;
using SAFE.NetworkDrive.Replication.Events;

namespace SAFE.NetworkDrive.Gateways.AsyncEvents
{
    /// <summary>
    /// 1. Stores to an encrypted WAL on disk.
    /// 2. Applies the requests to an in-memory file system.
    /// 3. Background job decrypts and uploads events and file content from WAL (separately) to SAFENetwork.
    /// On connect, downloads events from SAFENetwork (without the file contents)
    /// and materializes the file system in memory (folder structure and file names etc.).
    /// When file content is requested, it is downloaded and cached locally.
    /// When file content is produced it is cached locally.
    /// </summary>
    [System.Diagnostics.DebuggerDisplay("{DebuggerDisplay(),nq}")]
    public sealed class SAFENetworkGateway : IAsyncCloudGateway, IPersistGatewaySettings
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
        long _sequenceNr = -1;

        IDictionary<string, string> _parameters;

        public SAFENetworkGateway(string secretKey)
            => _secretKey = secretKey;

        async Task<SAFENetworkContext> RequireContextAsync(RootName root, string apiKey = null)
        {
            if (root == null)
                throw new ArgumentNullException(nameof(root));

            if (!_contextCache.TryGetValue(root, out SAFENetworkContext result))
            {
                var (stream, store) = await DbFactory.GetDriveDbsAsync(root, apiKey, _secretKey);
                var driveCache = new Memory.SAFENetworkDriveCache(store);
                var service = new SAFENetworkEventService(stream, store, _secretKey);

                var materializer = new DriveMaterializer(root, driveCache);
                var conflictHandler = new VersionConflictHandler(service, materializer);
                var driveWriter = new DriveWriter(root, driveCache);

                var transactor = new EventTransactor(
                    driveWriter,
                    new DiskWALTransactor(conflictHandler.Upload), _secretKey);
                _contextCache.Add(root, result = new SAFENetworkContext(transactor, new DriveReader(driveCache)));

                //var _ = driveCache.GetDrive(root, apiKey, );

                // We need to wait for all events in local WAL to have been persisted to network
                // before we materialize new events from network.
                transactor.Start(CancellationToken.None); // start uploading to network
                while (DiskWALTransactor.AnyInQueue()) // wait until queue is empty
                    await Task.Delay(500); // beware, this will - currently - spin eternally if there is an unresolved version conflict

                // (todo: should load snapshot + all events since)
                var allEvents = service.LoadAsync(fromVersion: 0); // load all events from network (since we don't store it locally)
                var isMaterialized = await materializer.Materialize(allEvents); // recreate the filesystem locally in memory
                if (!isMaterialized)
                    throw new InvalidDataException("Could not materialize network filesystem!");
                _sequenceNr = materializer.SequenceNr.HasValue ? (long)materializer.SequenceNr : -1;
            }

            return result;
        }

        public async Task<bool> TryAuthenticateAsync(RootName root, string apiKey, IDictionary<string, string> parameters)
        {
            try
            {
                await RequireContextAsync(root, apiKey);
                return true;
            }
            catch (AuthenticationException) { return false; }
        }

        public async Task<DriveInfoContract> GetDriveAsync(RootName root, string apiKey, IDictionary<string, string> parameters)
            => (await RequireContextAsync(root)).Reader.GetDrive(root, apiKey, parameters);

        public async Task<RootDirectoryInfoContract> GetRootAsync(RootName root, string apiKey, IDictionary<string, string> parameters)
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
        static string DebuggerDisplay() => nameof(SAFENetworkGateway);
    }
}
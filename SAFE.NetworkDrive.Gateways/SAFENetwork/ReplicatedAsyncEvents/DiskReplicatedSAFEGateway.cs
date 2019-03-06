using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Authentication;
using System.Threading;
using System.Threading.Tasks;
using Polly;
using Polly.Retry;
using SAFE.Data.Client;
using SAFE.AppendOnlyDb.Factories;
using SAFE.NetworkDrive.Replication.Events;
using SAFE.NetworkDrive.Gateways.Utils;
using SAFE.NetworkDrive.Interface;
using SAFE.NetworkDrive.Interface.Composition;
using SAFE.NetworkDrive.Interface.IO;
using SAFE.Data.Client.Auth;
using SAFE.AppendOnlyDb;

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

        readonly IDictionary<RootName, SAFENetworkContext> _contextCache = new Dictionary<RootName, SAFENetworkContext>();
        //readonly AsyncRetryPolicy _retryPolicy = Policy.Handle<ServiceException>().WaitAndRetryAsync(5, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)));
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
                _sequenceNr = -1;   // read from local db
                                   // fetch NetworkWAL (can be snapshot + all events since)
                                   // recreate the filesystem locally in memory

                var appInfo = new AppInfo
                {
                    Id = "safe.networkdrive",
                    Name = "SAFE.NetworkDrive",
                    Vendor = "oetyng"
                };
                SAFEClient.SetFactory(async (sess, app, db) => (object)await StreamDbFactory.CreateForApp(sess, app, db));
                var factory = new ClientFactory(appInfo, (session, appId) => new SAFEClient(session, appId));

                var client = await factory.GetMockNetworkClient(Credentials(apiKey, _secretKey), false);
                var db = await client.GetOrAddDbAsync<IStreamDb>(root.Value);
                var stream = await db.GetOrAddStreamAsync(root.Root);

                var memoryGateway = new Memory.MemoryGateway();
                var service = new SAFENetworkEventService(stream.Value, _secretKey);
                var driveWriter = new DriveWriter(root, memoryGateway);

                var newEvents = service.LoadAsync((ulong)_sequenceNr); // load new events from network
                await foreach (var e in newEvents)
                    driveWriter.Apply(e); // apply to local state

                var userFolder = PathScrambler.Obfuscate(root.UserName, _secretKey);
                var rootFolder = PathScrambler.Obfuscate(root.Root, _secretKey);

                var transactor = new EventTransactor(
                    driveWriter,
                    new DiskQueueWorker($"../sndr/{userFolder}/{rootFolder}", service.Upload), _secretKey);
                _contextCache.Add(root, result = new SAFENetworkContext(transactor, new DriveReader(memoryGateway)));

                transactor.Start(CancellationToken.None);
            }

            return result;
        }

        Credentials Credentials(string locator, string secret)
            => new Credentials(locator, secret);

        //async Task<Item> ChunkedUploadAsync(ChunkedUploadProvider provider, IProgress<ProgressValue> progress)
        //{
        //    var readBuffer = new byte[MaxChunkSize];
        //    var exceptions = new List<Exception>();

        //    var uploadChunkRequests = provider.GetUploadChunkRequests();
        //    var bytesTransferred = 0;
        //    var bytesTotal = uploadChunkRequests.Sum(u => u.RangeLength);
        //    progress?.Report(new ProgressValue(bytesTransferred, bytesTotal));

        //    foreach (var currentChunkRequest in uploadChunkRequests)
        //    {
        //        var uploadChunkResult = await _retryPolicy.ExecuteAsync(() => provider.GetChunkRequestResponseAsync(currentChunkRequest, readBuffer, exceptions));
        //        progress?.Report(new ProgressValue(bytesTransferred += currentChunkRequest.RangeLength, bytesTotal));

        //        if (uploadChunkResult.UploadSucceeded)
        //            return uploadChunkResult.ItemResponse;
        //    }

        //    await _retryPolicy.ExecuteAsync(() => provider.UpdateSessionStatusAsync());

        //    throw new TaskCanceledException(RETRIES_EXHAUSTED, new AggregateException(exceptions));
        //}

        public async Task<bool> TryAuthenticateAsync(RootName root, string apiKey, IDictionary<string, string> parameters)
        {
            try
            {
                await RequireContextAsync(root, apiKey);
                return true;
            }
            catch (AuthenticationException)
            {
                return false;
            }
        }

        public async Task<DriveInfoContract> GetDriveAsync(RootName root, string apiKey, 
            IDictionary<string, string> parameters)
        {
            var context = await RequireContextAsync(root, apiKey);
            return context.Reader.GetDrive(root, apiKey, parameters);
        }

        public async Task<RootDirectoryInfoContract> GetRootAsync(RootName root, string apiKey, 
            IDictionary<string, string> parameters)
        {
            var context = await RequireContextAsync(root, apiKey);
            return context.Reader.GetRoot(root, apiKey, parameters);
        }

        public async Task<IEnumerable<FileSystemInfoContract>> GetChildItemAsync(RootName root, DirectoryId parent)
        {
            var context = await RequireContextAsync(root);
            return context.Reader.GetChildItem(root, parent);
        }

        public async Task<Stream> GetContentAsync(RootName root, FileId source)
        {
            var context = await RequireContextAsync(root);
            return context.Reader.GetContent(root, source);
        }

        public async Task<bool> ClearContentAsync(RootName root, FileId target, Func<FileSystemInfoLocator> locatorResolver)
        {
            var context = await RequireContextAsync(root);
            var sequenceNr = Interlocked.Increment(ref _sequenceNr);
            var e = new FileContentCleared(sequenceNr, target.Value);
            return context.Writer.Transact(e);
        }

        public async Task<bool> SetContentAsync(RootName root, FileId target, Stream content, 
            IProgress<ProgressValue> progress, Func<FileSystemInfoLocator> locatorResolver)
        {
            var context = await RequireContextAsync(root);
            var sequenceNr = Interlocked.Increment(ref _sequenceNr);
            // store content to local db, get a content id
            var contentId = Guid.NewGuid();
            var e = new FileContentSet(sequenceNr, target.Value, contentId); // ReadFully(content)
            return context.Writer.Transact(e);
        }

        byte[] ReadFully(Stream input)
        {
            using (var ms = new MemoryStream())
            {
                input.CopyTo(ms);
                return ms.ToArray();
            }
        }

        public async Task<FileSystemInfoContract> CopyItemAsync(RootName root, FileSystemId source, 
            string copyName, DirectoryId destination, bool recurse)
        {
            var context = await RequireContextAsync(root);
            var fsType = source is DirectoryId ? FSType.Directory : FSType.File;
            var sequenceNr = Interlocked.Increment(ref _sequenceNr);
            var e = new ItemCopied(sequenceNr, source.Value, fsType, copyName, destination.Value, recurse);
            var info = context.Writer.Transact<FileSystemInfoContract>(e);
            return info.Item2;
        }

        public async Task<FileSystemInfoContract> MoveItemAsync(RootName root, FileSystemId source, 
            string moveName, DirectoryId destination, Func<FileSystemInfoLocator> locatorResolver)
        {
            var context = await RequireContextAsync(root);
            var fsType = source is DirectoryId ? FSType.Directory : FSType.File;
            var sequenceNr = Interlocked.Increment(ref _sequenceNr);
            var e = new ItemMoved(sequenceNr, source.Value, fsType, moveName, destination.Value);
            var info = context.Writer.Transact<FileSystemInfoContract>(e);
            return info.Item2;
        }

        public async Task<DirectoryInfoContract> NewDirectoryItemAsync(RootName root, DirectoryId parent, 
            string name)
        {
            var context = await RequireContextAsync(root);
            var sequenceNr = Interlocked.Increment(ref _sequenceNr);
            var e = new DirectoryItemCreated(sequenceNr, parent.Value, name);
            var info = context.Writer.Transact<DirectoryInfoContract>(e);
            return info.Item2;
        }

        public async Task<FileInfoContract> NewFileItemAsync(RootName root, DirectoryId parent, 
            string name, Stream content, IProgress<ProgressValue> progress)
        {
            var context = await RequireContextAsync(root);
            var sequenceNr = Interlocked.Increment(ref _sequenceNr);
            // store content to local db, get a content id
            var contentId = Guid.NewGuid();
            var e = new FileItemCreated(sequenceNr, parent.Value, name, contentId); // ReadFully(content)
            var info = context.Writer.Transact<FileInfoContract>(e);
            return info.Item2;
        }

        public async Task<bool> RemoveItemAsync(RootName root, FileSystemId target, bool recurse)
        {
            var context = await RequireContextAsync(root);
            var fsType = target is DirectoryId ? FSType.Directory : FSType.File;
            var sequenceNr = Interlocked.Increment(ref _sequenceNr);
            var e = new ItemRemoved(sequenceNr, target.Value, fsType, recurse);
            return context.Writer.Transact(e);
        }

        public async Task<FileSystemInfoContract> RenameItemAsync(RootName root, FileSystemId target, 
            string newName, Func<FileSystemInfoLocator> locatorResolver)
        {
            var context = await RequireContextAsync(root);
            var fsType = target is DirectoryId ? FSType.Directory : FSType.File;
            var sequenceNr = Interlocked.Increment(ref _sequenceNr);
            var e = new ItemRenamed(sequenceNr, target.Value, fsType, newName);
            var info = context.Writer.Transact<FileSystemInfoContract>(e);
            return info.Item2;
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

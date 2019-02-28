using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Authentication;
using System.Threading.Tasks;
using Polly;
using Polly.Retry;
using SAFE.Filesystem.Interface.IO;
using SAFE.NetworkDrive.Interface;
using SAFE.NetworkDrive.Interface.Composition;
using SAFE.NetworkDrive.Interface.IO;

namespace SAFE.NetworkDrive.Gateways.AsyncWAL
{
    [System.Diagnostics.DebuggerDisplay("{DebuggerDisplay(),nq}")]
    public sealed class DiskReplicatedSAFEGateway : IAsyncCloudGateway, IPersistGatewaySettings
    {
        const string SCHEMA = "safenetwork";
        const GatewayCapabilities CAPABILITIES = GatewayCapabilities.All;
        const string RETRIES_EXHAUSTED = "Task failed too many times. See InnerException property for a list of Exceptions that occured.";

        static readonly FileSize LargeFileThreshold = new FileSize("50MB");
        static readonly FileSize MaxChunkSize = new FileSize("5MB");

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
        readonly string _settingsPassPhrase;

        public DiskReplicatedSAFEGateway(string settingsPassPhrase)
        {
            _settingsPassPhrase = settingsPassPhrase;
        }

        async Task<SAFENetworkContext> RequireContextAsync(RootName root, string apiKey = null)
        {
            if (root == null)
                throw new ArgumentNullException(nameof(root));

            if (!_contextCache.TryGetValue(root, out SAFENetworkContext result))
            {
                var fileGateway = new File.FileGateway();
                var service = new SAFENetworkEventService();
                var transactor = new EventTransactor(
                    new DriveWriter(root, fileGateway),
                    new NonIntrusiveDiskQueueWorker("../path", service.Upload),
                    _settingsPassPhrase);
                _contextCache.Add(root, result = new SAFENetworkContext(transactor, new DriveReader(fileGateway)));
            }

            return result;
        }

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
            var e = new Events.FileContentCleared(target.Value);
            return context.Writer.Transact(e);
        }

        public async Task<bool> SetContentAsync(RootName root, FileId target, Stream content, 
            IProgress<ProgressValue> progress, Func<FileSystemInfoLocator> locatorResolver)
        {
            var context = await RequireContextAsync(root);
            var e = new Events.FileContentSet(target.Value, ReadFully(content));
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
            var fsType = source is DirectoryId ? Events.FSType.Directory : Events.FSType.File;
            var e = new Events.ItemCopied(source.Value, fsType, copyName, destination.Value, recurse);
            var info = context.Writer.Transact<FileSystemInfoContract>(e);
            return info.Item2;
        }

        public async Task<FileSystemInfoContract> MoveItemAsync(RootName root, FileSystemId source, 
            string moveName, DirectoryId destination, Func<FileSystemInfoLocator> locatorResolver)
        {
            var context = await RequireContextAsync(root);
            var fsType = source is DirectoryId ? Events.FSType.Directory : Events.FSType.File;
            var e = new Events.ItemMoved(source.Value, fsType, moveName, destination.Value);
            var info = context.Writer.Transact<FileSystemInfoContract>(e);
            return info.Item2;
        }

        public async Task<DirectoryInfoContract> NewDirectoryItemAsync(RootName root, DirectoryId parent, 
            string name)
        {
            var context = await RequireContextAsync(root);
            var e = new Events.DirectoryItemCreated(parent.Value, name);
            var info = context.Writer.Transact<DirectoryInfoContract>(e);
            return info.Item2;
        }

        public async Task<FileInfoContract> NewFileItemAsync(RootName root, DirectoryId parent, 
            string name, Stream content, IProgress<ProgressValue> progress)
        {
            var context = await RequireContextAsync(root);
            var e = new Events.FileItemCreated(parent.Value, name, ReadFully(content));
            var info = context.Writer.Transact<FileInfoContract>(e);
            return info.Item2;
        }

        public async Task<bool> RemoveItemAsync(RootName root, FileSystemId target, bool recurse)
        {
            var context = await RequireContextAsync(root);
            var fsType = target is DirectoryId ? Events.FSType.Directory : Events.FSType.File;
            var e = new Events.ItemRemoved(target.Value, fsType, recurse);
            return context.Writer.Transact(e);
        }

        public async Task<FileSystemInfoContract> RenameItemAsync(RootName root, FileSystemId target, 
            string newName, Func<FileSystemInfoLocator> locatorResolver)
        {
            var context = await RequireContextAsync(root);
            var fsType = target is DirectoryId ? Events.FSType.Directory : Events.FSType.File;
            var e = new Events.ItemRenamed(target.Value, fsType, newName);
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

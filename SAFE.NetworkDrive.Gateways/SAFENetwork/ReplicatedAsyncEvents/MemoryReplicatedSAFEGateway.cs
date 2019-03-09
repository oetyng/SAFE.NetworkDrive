using SAFE.NetworkDrive.IO;
using SAFE.NetworkDrive.Interface;
using SAFE.NetworkDrive.Interface.Composition;
using SAFE.NetworkDrive.Interface.IO;
using SAFE.NetworkDrive.Replication.Events;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using SAFE.Data.Client;

namespace SAFE.NetworkDrive.Gateways.Memory
{
    [System.Diagnostics.DebuggerDisplay("{DebuggerDisplay(),nq}")]
    public sealed class MemoryReplicatedSAFEGateway : ICloudGateway
    {
        readonly IImDStore _imdStore;
        readonly MemoryGateway _gateway;

        readonly ConcurrentDictionary<FileId, System.IO.Stream> _contentCache = new ConcurrentDictionary<FileId, System.IO.Stream>();

        public MemoryReplicatedSAFEGateway(IImDStore imdStore)
        {
            _imdStore = imdStore;
            _gateway = new MemoryGateway();
        }

        public bool TryAuthenticate(RootName root, string apiKey, IDictionary<string, string> parameters) => true;

        public DriveInfoContract GetDrive(RootName root, string apiKey, IDictionary<string, string> parameters)
            => _gateway.GetDrive(root, apiKey, parameters);

        public RootDirectoryInfoContract GetRoot(RootName root, string apiKey, IDictionary<string, string> parameters)
            => _gateway.GetRoot(root, apiKey, parameters);

        public IEnumerable<FileSystemInfoContract> GetChildItem(RootName root, DirectoryId parent)
            => _gateway.GetChildItem(root, parent);

        public void ClearContent(RootName root, FileId target)
            => _gateway.ClearContent(root, target);

        public System.IO.Stream GetContent(RootName root, FileId source)
        {
            if (!_contentCache.ContainsKey(source))
            {
                var data = _gateway.GetContent(root, source);
                var evt = NetworkContentLocator.FromBytes(data.ReadFully());
                var content = evt.MapOrContent;
                if (evt.IsMap)
                    content = _imdStore.GetImDAsync(evt.MapOrContent).GetAwaiter().GetResult();

                _contentCache[source] = new System.IO.MemoryStream(content);
            }
            
            return new System.IO.BufferedStream(_contentCache[source]);
        }

        public void SetContent(RootName root, FileId target, System.IO.Stream content, IProgress<ProgressValue> progress)
        {
            _gateway.SetContent(root, target, content, progress);
            _contentCache[target] = new System.IO.BufferedStream(content);
        }

        public FileSystemInfoContract CopyItem(RootName root, FileSystemId source, string copyName, DirectoryId destination, bool recurse)
        {
            var contract = _gateway.CopyItem(root, source, copyName, destination, recurse);
            var fileId = new FileId(source.Value);
            if (_contentCache.ContainsKey(fileId))
                _contentCache[new FileId(contract.Id.Value)] = _contentCache[fileId];
            return contract;
        }

        public FileSystemInfoContract MoveItem(RootName root, FileSystemId source, string moveName, DirectoryId destination)
        {
            var contract = _gateway.MoveItem(root, source, moveName, destination);
            TryReplace(source, contract.Id);
            return contract;
        }

        public DirectoryInfoContract NewDirectoryItem(RootName root, DirectoryId parent, string name)
            => _gateway.NewDirectoryItem(root, parent, name);

        // TODO
        public FileInfoContract NewFileItem(RootName root, DirectoryId parent, 
            string name, System.IO.Stream content, IProgress<ProgressValue> progress)
        {
            var contract = _gateway.NewFileItem(root, parent, name, content, progress);
            _contentCache[contract.Id] = new System.IO.BufferedStream(content);
            return contract;
        }

        public void RemoveItem(RootName root, FileSystemId target, bool recurse)
        {
            _gateway.RemoveItem(root, target, recurse);
            var fileId = new FileId(target.Value);
            if (_contentCache.ContainsKey(fileId))
                _contentCache.Remove(fileId, out _);
        }

        public FileSystemInfoContract RenameItem(RootName root, FileSystemId target, string newName)
        {
            var contract = _gateway.RenameItem(root, target, newName);
            TryReplace(target, contract.Id);
            return contract;
        }

        void TryReplace(FileSystemId previous, FileSystemId newId)
        {
            var previousFileId = new FileId(previous.Value);
            if (_contentCache.ContainsKey(previousFileId))
            {
                var newFileId = new FileId(newId.Value);
                _contentCache[newFileId] = _contentCache[previousFileId];
                _contentCache.Remove(previousFileId, out _);
            }
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Justification = "Debugger Display")]
        [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
        static string DebuggerDisplay() => nameof(MemoryReplicatedSAFEGateway);
    }
}
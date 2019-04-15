using SAFE.NetworkDrive.IO;
using SAFE.NetworkDrive.Interface;
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
    public sealed class SAFENetworkDriveCache
    {
        readonly IImDStore _imdStore;
        readonly MemoryGateway _localState;
        readonly ConcurrentDictionary<FileId, byte[]> _contentCache;

        public SAFENetworkDriveCache(IImDStore imdStore, MemoryGateway localState)
        {
            _imdStore = imdStore;
            _localState = localState;
            _contentCache = new ConcurrentDictionary<FileId, byte[]>();
        }

        public DriveInfoContract GetDrive()
            => _localState.GetDrive();

        public RootDirectoryInfoContract GetRoot()
            => _localState.GetRoot();

        public IEnumerable<FileSystemInfoContract> GetChildItem(DirectoryId parent)
            => _localState.GetChildItem(parent);

        public void ClearContent(FileId target)
            => _localState.ClearContent(target);

        public System.IO.Stream GetContent(FileId source)
        {
            if (!_contentCache.ContainsKey(source))
            {
                var data = _localState.GetContent(source);
                var evt = NetworkContentLocator.FromBytes(data.ReadFully());
                var content = evt.MapOrContent;
                if (evt.IsMap)
                    content = _imdStore.GetImDAsync(evt.MapOrContent).GetAwaiter().GetResult();

                _contentCache[source] = content;
            }

            // If you create a MemoryStream over a pre-allocated byte array, 
            // it can't expand (ie. get longer than the size you specified when you started).
            // The key is to use the empty (no params) MemoryStream() ctor, which creates it as expandable.
            var ms = new System.IO.MemoryStream();
            var bytes = _contentCache[source];
            ms.Write(bytes, 0, bytes.Length);
            ms.Seek(0, System.IO.SeekOrigin.Begin);
            return new System.IO.BufferedStream(ms);
        }

        public void SetContent(FileId target, System.IO.Stream content, IProgress<ProgressValue> progress)
        {
            _localState.SetContent(target, content, progress);
            _contentCache[target] = content.ReadFully();
        }

        public FileSystemInfoContract CopyItem(FileSystemId source, string copyName, DirectoryId destination, bool recurse)
        {
            var contract = _localState.CopyItem(source, copyName, destination, recurse);
            var fileId = new FileId(source.Value);
            if (_contentCache.ContainsKey(fileId))
                _contentCache[new FileId(contract.Id.Value)] = _contentCache[fileId];
            return contract;
        }

        public FileSystemInfoContract MoveItem(FileSystemId source, string moveName, DirectoryId destination)
        {
            var contract = _localState.MoveItem(source, moveName, destination);
            TryReplace(source, contract.Id);
            return contract;
        }

        public DirectoryInfoContract NewDirectoryItem(DirectoryId parent, string name)
            => _localState.NewDirectoryItem(parent, name);

        public FileInfoContract NewFileItem(DirectoryId parent, 
            string name, System.IO.Stream content, IProgress<ProgressValue> progress)
        {
            var contract = _localState.NewFileItem(parent, name, content, progress);
            _contentCache[contract.Id] = content.ReadFully();
            return contract;
        }

        public void RemoveItem(FileSystemId target, bool recurse)
        {
            _localState.RemoveItem(target, recurse);
            var fileId = new FileId(target.Value);
            if (_contentCache.ContainsKey(fileId))
                _contentCache.Remove(fileId, out _);
        }

        public FileSystemInfoContract RenameItem(FileSystemId target, string newName)
        {
            var contract = _localState.RenameItem(target, newName);
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
        static string DebuggerDisplay() => nameof(SAFENetworkDriveCache);
    }
}
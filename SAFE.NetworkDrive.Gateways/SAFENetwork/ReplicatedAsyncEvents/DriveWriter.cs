using SAFE.NetworkDrive.Replication.Events;
using SAFE.NetworkDrive.Gateways.Memory;
using SAFE.NetworkDrive.Interface;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace SAFE.NetworkDrive.Gateways.AsyncEvents
{
    class DriveWriter
    {
        readonly RootName _root;
        readonly SAFENetworkDriveCache _localState;
        readonly ConcurrentDictionary<Type, Func<LocalEvent, object>> _apply;
        readonly object _lockObj = new object();
        ulong? _sequenceNr;

        public DriveWriter(RootName root, SAFENetworkDriveCache gateway)
        {
            _root = root;
            _localState = gateway;
            _apply = new ConcurrentDictionary<Type, Func<LocalEvent, object>>();
            var applyMethods = GetAllMethods(this.GetType())
                .Where(m => m.Name == "Apply");
            foreach (var m in applyMethods)
                _apply[m.GetParameters().First().ParameterType] = new Func<LocalEvent, object>((e) => m.Invoke(this, new object[] { e }));
        }

        IEnumerable<MethodInfo> GetAllMethods(Type t) // recursive
        {
            if (t == null)
                return Enumerable.Empty<MethodInfo>();

            var flags = BindingFlags.NonPublic | BindingFlags.Instance;
            return t.GetMethods(flags).Concat(GetAllMethods(t.BaseType));
        }

        public (bool, object) Apply(LocalEvent e)
        {
            object obj;
            try
            {
                lock (_lockObj)
                {
                    if (!IsValidSequence(e.SequenceNr))
                        return (false, null);
                    obj = _apply[e.GetType()](e);
                    _sequenceNr = e.SequenceNr;
                    return (true, obj);
                }
            }
            catch { return (false, null); }
        }

        bool IsValidSequence(ulong sequenceNr)
        {
            if (!_sequenceNr.HasValue && sequenceNr != 0)
                return false;
            else if (_sequenceNr.HasValue && _sequenceNr != sequenceNr - 1)
                return false;
            return true;
        }

        object Apply(LocalFileItemCreated e)
            => _localState.NewFileItem(_root, new DirectoryId(e.ParentDirId), e.Name, new MemoryStream(e.Content), null);

        object Apply(LocalFileContentSet e)
        {
            _localState.SetContent(_root, new FileId(e.FileId), new MemoryStream(e.Content), null);
            return new object();
        }

        object Apply(LocalFileContentCleared e)
        {
            _localState.ClearContent(_root, new FileId(e.FileId));
            return new object();
        }

        object Apply(LocalItemCopied e)
        {
            FileSystemId fileSystemId;
            switch (e.FSType)
            {
                case FSType.Directory:
                    fileSystemId = new DirectoryId(e.FileSystemId);
                    break;
                case FSType.File:
                    fileSystemId = new FileId(e.FileSystemId);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(e.FSType));
            }

            var item = _localState.CopyItem(_root, fileSystemId, e.CopyName, new DirectoryId(e.DestDirId), true);
            return item;
        }

        object Apply(LocalItemMoved e)
        {
            FileSystemId fileSystemId;
            switch (e.FSType)
            {
                case FSType.Directory:
                    fileSystemId = new DirectoryId(e.FileSystemId);
                    break;
                case FSType.File:
                    fileSystemId = new FileId(e.FileSystemId);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(e.FSType));
            }

            var item = _localState.MoveItem(_root, fileSystemId, e.MoveName, new DirectoryId(e.DestDirId));
            return item;
        }

        object Apply(LocalDirectoryItemCreated e)
            => _localState.NewDirectoryItem(_root, new DirectoryId(e.ParentDirId), e.Name);

        object Apply(LocalItemRemoved e)
        {
            FileSystemId fileSystemId;
            switch (e.FSType)
            {
                case FSType.Directory:
                    fileSystemId = new DirectoryId(e.FileSystemId);
                    break;
                case FSType.File:
                    fileSystemId = new FileId(e.FileSystemId);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(e.FSType));
            }
            _localState.RemoveItem(_root, fileSystemId, e.Recursive);
            return new object();
        }

        object Apply(LocalItemRenamed e)
        {
            FileSystemId fileSystemId;
            switch (e.FSType)
            {
                case FSType.Directory:
                    fileSystemId = new DirectoryId(e.FileSystemId);
                    break;
                case FSType.File:
                    fileSystemId = new FileId(e.FileSystemId);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(e.FSType));
            }
            var item = _localState.RenameItem(_root, fileSystemId, e.NewName);
            return item;
        }
    }
}
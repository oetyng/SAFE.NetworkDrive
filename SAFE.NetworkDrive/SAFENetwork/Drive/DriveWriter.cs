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
        readonly SAFENetworkDriveCache _localState;
        readonly SequenceNr _sequenceNr;
        readonly ConcurrentDictionary<Type, Func<LocalEvent, object>> _apply;

        public DriveWriter(SAFENetworkDriveCache gateway, SequenceNr sequenceNr)
        {
            _localState = gateway;
            _sequenceNr = sequenceNr;
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
                using (var sync = _sequenceNr.Lock())
                {
                    if (!_sequenceNr.IsValidSequence(e.SequenceNr))
                        return (false, null);
                    obj = _apply[e.GetType()](e);
                    _sequenceNr.Set(e.SequenceNr);
                    return (true, obj);
                }
            }
            catch { return (false, null); }
        }

        object Apply(LocalFileItemCreated e)
            => _localState.NewFileItem(new DirectoryId(e.ParentDirId), e.Name, new MemoryStream(e.Content), null);

        object Apply(LocalFileContentSet e)
        {
            _localState.SetContent(new FileId(e.FileId), new MemoryStream(e.Content), null);
            return new object();
        }

        object Apply(LocalFileContentCleared e)
        {
            _localState.ClearContent(new FileId(e.FileId));
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

            var item = _localState.CopyItem(fileSystemId, e.CopyName, new DirectoryId(e.DestDirId), true);
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

            var item = _localState.MoveItem(fileSystemId, e.MoveName, new DirectoryId(e.DestDirId));
            return item;
        }

        object Apply(LocalDirectoryItemCreated e)
            => _localState.NewDirectoryItem(new DirectoryId(e.ParentDirId), e.Name);

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
            _localState.RemoveItem(fileSystemId, e.Recursive);
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
            var item = _localState.RenameItem(fileSystemId, e.NewName);
            return item;
        }
    }
}
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
        readonly MemoryGateway _localState;
        readonly ConcurrentDictionary<Type, Func<Event, object>> _apply = new ConcurrentDictionary<Type, Func<Event, object>>();
        readonly object _lockObj = new object();
        long _sequenceNr;

        public DriveWriter(RootName root, MemoryGateway memGateway)
        {
            _root = root;
            _localState = memGateway;
            var applyMethods = GetAllMethods(this.GetType())
                .Where(m => m.Name == "Apply");
            foreach (var m in applyMethods)
                _apply[m.GetParameters().First().ParameterType] = new Func<Event, object>((e) => m.Invoke(this, new object[] { e }));
        }

        IEnumerable<MethodInfo> GetAllMethods(Type t) // recursive
        {
            if (t == null)
                return Enumerable.Empty<MethodInfo>();

            var flags = BindingFlags.NonPublic | BindingFlags.Instance;
            return t.GetMethods(flags).Concat(GetAllMethods(t.BaseType));
        }

        public (bool, object) Apply(Event e)
        {
            object obj;
            try
            {
                lock (_lockObj)
                {
                    if (_sequenceNr != e.SequenceNr - 1)
                        return (false, null);
                    obj = _apply[e.GetType()](e);
                    _sequenceNr = e.SequenceNr;
                    return (true, obj);
                }
            }
            catch
            {
                return (false, null);
            }
        }

        Stream LoadContent(Guid contentId)
        {
            // if not exists in local db: get from network
            return new MemoryStream(new byte[0]);
        }

        object Apply(FileContentCleared e)
        {
            _localState.ClearContent(_root, new FileId(e.FileId));
            return new object();
        }

        object Apply(FileContentSet e)
        {
            _localState.SetContent(_root, new FileId(e.FileId), LoadContent(e.ContentId), null);
            return new object();
        }

        object Apply(ItemCopied e)
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

        object Apply(ItemMoved e)
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

        object Apply(DirectoryItemCreated e)
            => _localState.NewDirectoryItem(_root, new DirectoryId(e.ParentDirId), e.Name);

        object Apply(FileItemCreated e)
            => _localState.NewFileItem(_root, new DirectoryId(e.ParentDirId), e.Name, LoadContent(e.ContentId), null);

        object Apply(ItemRemoved e)
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

        object Apply(ItemRenamed e)
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
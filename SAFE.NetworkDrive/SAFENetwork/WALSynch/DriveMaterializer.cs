﻿using SAFE.NetworkDrive.Replication.Events;
using SAFE.NetworkDrive.Gateways.Memory;
using SAFE.NetworkDrive.Interface;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using SAFE.AppendOnlyDb.Utils;

namespace SAFE.NetworkDrive.Gateways.AsyncEvents
{
    // Materializes an in-memory filesystem from 
    // network events passed into the Materialize method.
    // This will setup the entire folder and file structure
    // but not download any content other than the minimal data
    // that fits in the NetworkEvents. Instead, larger content is downloaded on demand.
    class DriveMaterializer
    {
        readonly RootName _root;
        readonly MemoryGateway _localState;
        readonly ConcurrentDictionary<Type, Func<NetworkEvent, object>> _apply;
        readonly SequenceNr _sequenceNr;

        public DriveMaterializer(RootName root, MemoryGateway localState, SequenceNr sequenceNr)
        {
            _root = root;
            _localState = localState;
            _sequenceNr = sequenceNr;
            _apply = new ConcurrentDictionary<Type, Func<NetworkEvent, object>>();
            var applyMethods = GetAllMethods(this.GetType())
                .Where(m => m.Name == "Apply");
            foreach (var m in applyMethods)
                _apply[m.GetParameters().First().ParameterType] = new Func<NetworkEvent, object>((e) => m.Invoke(this, new object[] { e }));
        }

        IEnumerable<MethodInfo> GetAllMethods(Type t) // recursive
        {
            if (t == null)
                return Enumerable.Empty<MethodInfo>();

            var flags = BindingFlags.NonPublic | BindingFlags.Instance;
            return t.GetMethods(flags).Concat(GetAllMethods(t.BaseType));
        }

        public async Task<bool> Materialize(IAsyncEnumerable<NetworkEvent> events)
        {
            try
            {
                using (var sync = await _sequenceNr.LockAsync())
                {
                    await foreach (var e in events)
                    {
                        if (!_sequenceNr.IsValidSequence(e.SequenceNr))
                            return false;
                        _apply[e.GetType()](e);
                        _sequenceNr.Set(e.SequenceNr);
                    }
                    return true;
                }
            }
            catch (Exception e) when (e.StackTrace.Contains("IAsyncEnumerable")) { return true; } // NB: SHOULD RETURN FALSE
            catch { return false; }
        }

        object Apply(NetworkFileItemCreated e)
        {
            var locator = new NetworkContentLocator
            {
                ContentId = e.SequenceNr,
                IsMap = e.IsMap,
                MapOrContent = e.MapOrContent
            };
            var stream = new MemoryStream(locator.GetBytes());
            return _localState.NewFileItem(_root, new DirectoryId(e.ParentDirId), e.Name, stream, null);
        }

        object Apply(NetworkFileContentSet e)
        {
            var locator = new NetworkContentLocator
            {
                ContentId = e.SequenceNr,
                IsMap = e.IsMap,
                MapOrContent = e.MapOrContent
            };
            var stream = new MemoryStream(locator.GetBytes());
            _localState.SetContent(_root, new FileId(e.FileId), stream, null);
            return new object();
        }

        object Apply(NetworkFileContentCleared e)
        {
            _localState.ClearContent(_root, new FileId(e.FileId));
            return new object();
        }

        object Apply(NetworkItemCopied e)
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

        object Apply(NetworkItemMoved e)
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

        object Apply(NetworkDirectoryItemCreated e)
            => _localState.NewDirectoryItem(_root, new DirectoryId(e.ParentDirId), e.Name);

        object Apply(NetworkItemRemoved e)
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

        object Apply(NetworkItemRenamed e)
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
using SAFE.NetworkDrive.Gateways.Events;
using SAFE.NetworkDrive.Gateways.File;
using SAFE.NetworkDrive.Interface;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace SAFE.NetworkDrive.Gateways.AsyncWAL
{
    class DriveWriter
    {
        readonly RootName _root;
        readonly FileGateway _fileGateway;
        readonly ConcurrentDictionary<Type, Func<Event, object>> _apply = new ConcurrentDictionary<Type, Func<Event, object>>();

        public DriveWriter(RootName root,  FileGateway fileGateway)
        {
            _root = root;
            _fileGateway = fileGateway;
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
                obj = _apply[e.GetType()](e);
                return (true, obj);
            }
            catch
            {
                return (false, null);
            }
        }

        object Apply(FileContentCleared e)
        {
            _fileGateway.ClearContent(_root, new FileId(e.FileId));
            return new object();
        }

        object Apply(FileContentSet e)
        {
            _fileGateway.SetContent(_root, new FileId(e.FileId), new MemoryStream(e.Content), null);
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

            var item = _fileGateway.CopyItem(_root, fileSystemId, e.CopyName, new DirectoryId(e.DestDirId), true);
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

            var item = _fileGateway.MoveItem(_root, fileSystemId, e.MoveName, new DirectoryId(e.DestDirId));
            return item;
        }

        object Apply(DirectoryItemCreated e)
        {
            var item = _fileGateway.NewDirectoryItem(_root, new DirectoryId(e.ParentDirId), e.Name);
            return item;
        }

        object Apply(FileItemCreated e)
        {
            var item = _fileGateway.NewFileItem(_root, new DirectoryId(e.ParentDirId), e.Name, new MemoryStream(e.Content), null);
            return item;
        }

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
            _fileGateway.RemoveItem(_root, fileSystemId, e.Recursive);
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
            var item = _fileGateway.RenameItem(_root, fileSystemId, e.NewName);
            return item;
        }
    }

    class DriveReader
    {
        FileGateway _fileGateway;

        public DriveReader(FileGateway fileGateway)
        {
            _fileGateway = fileGateway;
        }

        public async Task LoadDrive(RootName root)
        {
            // fetch NetworkWAL (can be snapshot + all events since)
            // recreate the filesystem locally - but encrypted
        }

        public DriveInfoContract GetDrive(RootName root, string apiKey, IDictionary<string, string> parameters)
        {
            return _fileGateway.GetDrive(root, apiKey, parameters);
        }


        public RootDirectoryInfoContract GetRoot(RootName root, string apiKey, IDictionary<string, string> parameters)
        {
            return _fileGateway.GetRoot(root, apiKey, parameters);
        }

        public IEnumerable<FileSystemInfoContract> GetChildItem(RootName root, DirectoryId parent)
        {
            return _fileGateway.GetChildItem(root, parent);
        }

        public Stream GetContent(RootName root, FileId source)
        {
            return _fileGateway.GetContent(root, source);
        }
    }
}

using System;
using System.Collections.Generic;
using System.IO;
using SAFE.NetworkDrive.IO;
using SAFE.NetworkDrive.Interface;
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
    internal sealed class SAFENetworkGateway : ISAFEGateway
    {
        readonly SequenceNr _sequenceNr;
        readonly SAFENetworkContext _context;

        public SAFENetworkGateway(SAFENetworkContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _sequenceNr = context.SequenceNr;
        }

        public DriveInfoContract GetDrive(RootName root)
            => _context.Reader.GetDrive(root);

        public RootDirectoryInfoContract GetRoot(RootName root)
            => _context.Reader.GetRoot(root);

        public IEnumerable<FileSystemInfoContract> GetChildItem(RootName root, DirectoryId parent)
            => _context.Reader.GetChildItem(root, parent);

        public Stream GetContent(RootName root, FileId source)
            => _context.Reader.GetContent(root, source);

        public bool SetContent(RootName root, FileId target, Stream content, 
            IProgress<ProgressValue> progress, Func<FileSystemInfoLocator> locatorResolver)
            => Transact((sequenceNr) => new LocalFileContentSet(sequenceNr, target.Value, content.ReadFully()));

        public bool ClearContent(RootName root, FileId target, Func<FileSystemInfoLocator> locatorResolver)
            => Transact((sequenceNr) => new LocalFileContentCleared(sequenceNr, target.Value));

        public bool RemoveItem(RootName root, FileSystemId target, bool recurse)
            => Transact((sequenceNr) => new LocalItemRemoved(sequenceNr, target.Value, GetType(target), recurse));

        public FileInfoContract NewFileItem(RootName root, DirectoryId parent,
            string name, Stream content, IProgress<ProgressValue> progress)
            => Transact<FileInfoContract>((sequenceNr) => new LocalFileItemCreated(sequenceNr, parent.Value, name, content.ReadFully()));

        public FileSystemInfoContract CopyItem(RootName root, FileSystemId source, 
            string copyName, DirectoryId destination, bool recurse)
            => Transact<FileSystemInfoContract>((sequenceNr) => new LocalItemCopied(sequenceNr, source.Value, GetType(source), copyName, destination.Value, recurse));

        public FileSystemInfoContract MoveItem(RootName root, FileSystemId source, 
            string moveName, DirectoryId destination, Func<FileSystemInfoLocator> locatorResolver)
            => Transact<FileSystemInfoContract>((sequenceNr) => new LocalItemMoved(sequenceNr, source.Value, GetType(source), moveName, destination.Value));

        public DirectoryInfoContract NewDirectoryItem(RootName root, DirectoryId parent, string name)
            => Transact<DirectoryInfoContract>((sequenceNr) => new LocalDirectoryItemCreated(sequenceNr, parent.Value, name));

        public FileSystemInfoContract RenameItem(RootName root, FileSystemId target, 
            string newName, Func<FileSystemInfoLocator> locatorResolver)
            => Transact<FileSystemInfoContract>((sequenceNr) => new LocalItemRenamed(sequenceNr, target.Value, GetType(target), newName));

        FSType GetType(FileSystemId id) => id is DirectoryId ? FSType.Directory : FSType.File;

        T Transact<T>(Func<ulong, LocalEvent> getEvent)
        {
            var e = getEvent(_sequenceNr.Next);
            var result = _context.Writer.Transact<T>(e);
            //if (info.Item1) _sequenceNr.Increment();
            return result.Data;
        }

        bool Transact(Func<ulong, LocalEvent> getEvent)
        {
            var e = getEvent(_sequenceNr.Next);
            var result = _context.Writer.Transact(e);
            //if (info) _sequenceNr.Increment();
            return result;
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Justification = "Debugger Display")]
        [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
        static string DebuggerDisplay() => nameof(SAFENetworkGateway);
    }
}
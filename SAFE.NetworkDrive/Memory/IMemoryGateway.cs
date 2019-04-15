using System;
using System.Collections.Generic;
using System.IO;
using SAFE.NetworkDrive.Interface;
using SAFE.NetworkDrive.Interface.IO;

namespace SAFE.NetworkDrive.Interfaces
{
    public interface IMemoryGateway
    {
        void ClearContent(RootName root, FileId target);
        FileSystemInfoContract CopyItem(RootName root, FileSystemId source, string copyName, DirectoryId destination, bool recurse);
        IEnumerable<FileSystemInfoContract> GetChildItem(RootName root, DirectoryId parent);
        Stream GetContent(RootName root, FileId source);
        DriveInfoContract GetDrive(RootName root);
        RootDirectoryInfoContract GetRoot(RootName root);
        FileSystemInfoContract MoveItem(RootName root, FileSystemId source, string moveName, DirectoryId destination);
        DirectoryInfoContract NewDirectoryItem(RootName root, DirectoryId parent, string name);
        FileInfoContract NewFileItem(RootName root, DirectoryId parent, string name, Stream content, IProgress<ProgressValue> progress);
        void RemoveItem(RootName root, FileSystemId target, bool recurse);
        FileSystemInfoContract RenameItem(RootName root, FileSystemId target, string newName);
        void SetContent(RootName root, FileId target, Stream content, IProgress<ProgressValue> progress);
    }
}
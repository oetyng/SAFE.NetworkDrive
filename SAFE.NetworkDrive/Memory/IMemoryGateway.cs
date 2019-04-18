using System;
using System.Collections.Generic;
using System.IO;
using SAFE.NetworkDrive.Interface;
using SAFE.NetworkDrive.Interface.IO;

namespace SAFE.NetworkDrive.Interfaces
{
    public interface IMemoryGateway
    {
        void ClearContent(FileId target);
        FileSystemInfoContract CopyItem(FileSystemId source, string copyName, DirectoryId destination, DateTime timestamp, bool recurse);
        IEnumerable<FileSystemInfoContract> GetChildItem(DirectoryId parent);
        Stream GetContent(FileId source);
        DriveInfoContract GetDrive();
        RootDirectoryInfoContract GetRoot();
        FileSystemInfoContract MoveItem(FileSystemId source, string moveName, DirectoryId destination);
        DirectoryInfoContract NewDirectoryItem(DirectoryId parent, string name, DateTime timestamp);
        FileInfoContract NewFileItem(DirectoryId parent, string name, Stream content, DateTime timestamp, IProgress<ProgressValue> progress);
        void RemoveItem(FileSystemId target, bool recurse);
        FileSystemInfoContract RenameItem(FileSystemId target, string newName);
        void SetContent(FileId target, Stream content, DateTime timestamp, IProgress<ProgressValue> progress);
    }
}
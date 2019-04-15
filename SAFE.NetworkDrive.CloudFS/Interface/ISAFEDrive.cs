using System;
using System.Collections.Generic;
using System.IO;
using SAFE.NetworkDrive.Interface;

namespace SAFE.NetworkDrive
{
    public interface ISAFEDrive : IDisposable
    {
        string DisplayRoot { get; }
        long? Free { get; }
        long? Used { get; }
        RootDirectoryInfoContract GetRoot();
        IEnumerable<FileSystemInfoContract> GetChildItem(DirectoryInfoContract parent);
        Stream GetContent(FileInfoContract source);
        void SetContent(FileInfoContract target, Stream content);
        FileSystemInfoContract MoveItem(FileSystemInfoContract source, string movePath, DirectoryInfoContract destination);
        DirectoryInfoContract NewDirectoryItem(DirectoryInfoContract parent, string name);
        FileInfoContract NewFileItem(DirectoryInfoContract parent, string name, Stream content);
        void RemoveItem(FileSystemInfoContract target, bool recurse);
    }
}
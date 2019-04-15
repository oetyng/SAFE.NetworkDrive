using SAFE.NetworkDrive.Gateways.Memory;
using SAFE.NetworkDrive.Interface;
using System.Collections.Generic;
using System.IO;

namespace SAFE.NetworkDrive.Gateways.AsyncEvents
{
    class DriveReader
    {
        readonly SAFENetworkDriveCache _gateway;

        public DriveReader(SAFENetworkDriveCache gateway)
            => _gateway = gateway;

        public DriveInfoContract GetDrive(RootName root)
            => _gateway.GetDrive(root);

        public RootDirectoryInfoContract GetRoot(RootName root)
            => _gateway.GetRoot(root);

        public IEnumerable<FileSystemInfoContract> GetChildItem(RootName root, DirectoryId parent)
            => _gateway.GetChildItem(root, parent);

        public Stream GetContent(RootName root, FileId source)
            => _gateway.GetContent(root, source);
    }
}
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

        public DriveInfoContract GetDrive()
            => _gateway.GetDrive();

        public RootDirectoryInfoContract GetRoot()
            => _gateway.GetRoot();

        public IEnumerable<FileSystemInfoContract> GetChildItem(DirectoryId parent)
            => _gateway.GetChildItem(parent);

        public Stream GetContent(FileId source)
            => _gateway.GetContent(source);
    }
}
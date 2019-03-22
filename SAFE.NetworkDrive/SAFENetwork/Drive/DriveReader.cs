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

        public DriveInfoContract GetDrive(RootName root, string apiKey, IDictionary<string, string> parameters)
            => _gateway.GetDrive(root, apiKey, parameters);

        public RootDirectoryInfoContract GetRoot(RootName root, string apiKey, IDictionary<string, string> parameters)
            => _gateway.GetRoot(root, apiKey, parameters);

        public IEnumerable<FileSystemInfoContract> GetChildItem(RootName root, DirectoryId parent)
            => _gateway.GetChildItem(root, parent);

        public Stream GetContent(RootName root, FileId source)
            => _gateway.GetContent(root, source);
    }
}
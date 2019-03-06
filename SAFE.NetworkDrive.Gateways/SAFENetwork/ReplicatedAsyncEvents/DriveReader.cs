using SAFE.NetworkDrive.Gateways.File;
using SAFE.NetworkDrive.Interface;
using System.Collections.Generic;
using System.IO;

namespace SAFE.NetworkDrive.Gateways.AsyncEvents
{
    class DriveReader
    {
        readonly FileGateway _fileGateway;

        public DriveReader(FileGateway fileGateway)
            => _fileGateway = fileGateway;

        public DriveInfoContract GetDrive(RootName root, string apiKey, IDictionary<string, string> parameters)
            => _fileGateway.GetDrive(root, apiKey, parameters);

        public RootDirectoryInfoContract GetRoot(RootName root, string apiKey, IDictionary<string, string> parameters)
            => _fileGateway.GetRoot(root, apiKey, parameters);

        public IEnumerable<FileSystemInfoContract> GetChildItem(RootName root, DirectoryId parent)
            => _fileGateway.GetChildItem(root, parent);

        public Stream GetContent(RootName root, FileId source)
            => _fileGateway.GetContent(root, source);
    }
}
using SAFE.NetworkDrive.Gateways.File;
using SAFE.NetworkDrive.Interface;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace SAFE.NetworkDrive.Gateways.AsyncEvents
{
    class DriveReader
    {
        FileGateway _fileGateway;

        public DriveReader(FileGateway fileGateway)
        {
            _fileGateway = fileGateway;
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

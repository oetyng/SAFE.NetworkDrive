using SAFE.NetworkDrive.Interface;
using SAFE.NetworkDrive.Interface.IO;
using SAFE.NetworkDrive.IO;
using SAFE.Filesystem.Interface.IO;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;

namespace SAFE.NetworkDrive
{
    [System.Diagnostics.DebuggerDisplay("{DebuggerDisplay,nq}")]
    internal class SAFEDrive : SAFEDriveBase, ISAFEDrive
    {
        readonly ISAFEGateway _gateway;

        public SAFEDrive(RootName rootName, ISAFEGateway gateway) 
            : base(rootName)
            => _gateway = gateway;

        protected override DriveInfoContract GetDrive()
        {
            try
            {
                if (_drive == null)
                {
                    _drive = _gateway.GetDrive(_rootName);
                    _drive.Name = DisplayRoot + Path.VolumeSeparatorChar;
                }
                return _drive;
            }
            catch (AggregateException ex) when (ex.InnerExceptions.Count == 1)
            {
                throw ex.InnerExceptions[0];
            }
        }

        public RootDirectoryInfoContract GetRoot()
        {
            return ExecuteInSemaphore(() => {
                GetDrive();
                var root = _gateway.GetRoot(_rootName);
                root.Drive = _drive;
                return root;
            });
        }

        public IEnumerable<FileSystemInfoContract> GetChildItem(DirectoryInfoContract parent)
            => ExecuteInSemaphore(() => _gateway.GetChildItem(_rootName, parent.Id));

        public Stream GetContent(FileInfoContract source)
        {
            return ExecuteInSemaphore(() => {
                var content = _gateway.GetContent(_rootName, source.Id).ToSeekableStream();
                source.Size = (FileSize)content.Length;
                return content;
            });
        }

        public void SetContent(FileInfoContract target, Stream content)
        {
            ExecuteInSemaphore(() => {
                target.Size = (FileSize)content.Length;
                FileSystemInfoLocator locator() => new FileSystemInfoLocator(target);
                _gateway.SetContent(_rootName, target.Id, content, null, locator);
            }, true);
        }

        public FileSystemInfoContract MoveItem(FileSystemInfoContract source, string movePath, DirectoryInfoContract destination)
        {
            return ExecuteInSemaphore(() => {
                var proxySource = source as ProxyFileInfoContract;
                if (proxySource != null)
                    return new ProxyFileInfoContract(movePath);

                FileSystemInfoLocator locator() => new FileSystemInfoLocator(source);
                return _gateway.MoveItem(_rootName, source.Id, movePath, destination.Id, locator);
            }, true);
        }

        public DirectoryInfoContract NewDirectoryItem(DirectoryInfoContract parent, string name)
            => ExecuteInSemaphore(() => _gateway.NewDirectoryItem(_rootName, parent.Id, name), true);

        public FileInfoContract NewFileItem(DirectoryInfoContract parent, string name, Stream content)
        {
            return ExecuteInSemaphore(() => {
                if (content.Length == 0)
                    return new ProxyFileInfoContract(name);
                var result = _gateway.NewFileItem(_rootName, parent.Id, name, content, null);
                result.Size = (FileSize)content.Length;
                return result;
            }, true);
        }

        public void RemoveItem(FileSystemInfoContract target, bool recurse)
        {
            ExecuteInSemaphore(() => {
                if (!(target is ProxyFileInfoContract))
                    _gateway.RemoveItem(_rootName, target.Id, recurse);
            }, true);
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Justification = "Debugger Display")]
        [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
        string DebuggerDisplay => $"{nameof(SAFEDrive)} {DisplayRoot}".ToString(CultureInfo.CurrentCulture);
    }
}
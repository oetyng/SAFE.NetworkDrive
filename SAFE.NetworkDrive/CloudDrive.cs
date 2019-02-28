/*
The MIT License(MIT)
Copyright(c) 2015 IgorSoft
Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:
The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.
THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.
*/

using SAFE.NetworkDrive.Interface;
using SAFE.NetworkDrive.Interface.Composition;
using SAFE.NetworkDrive.IO;
using SAFE.NetworkDrive.Parameters;
using SAFE.Filesystem.Interface.IO;
using System.Collections.Generic;
using System.Globalization;
using System.IO;

namespace SAFE.NetworkDrive
{
    [System.Diagnostics.DebuggerDisplay("{DebuggerDisplay,nq}")]
    internal class CloudDrive : CloudDriveBase, ICloudDrive
    {
        readonly ICloudGateway _gateway;

        readonly IDictionary<string, string> _parameters;

        public CloudDrive(RootName rootName, ICloudGateway gateway, CloudDriveParameters parameters) : base(rootName, parameters)
        {
            _gateway = gateway;
            _parameters = parameters.Parameters;
        }

        public IPersistGatewaySettings PersistSettings => _gateway as IPersistGatewaySettings;

        protected override DriveInfoContract GetDrive()
        {
            if (_drive == null)
            {
                _drive = _gateway.GetDrive(_rootName, _apiKey, _parameters);
                _drive.Name = DisplayRoot + Path.VolumeSeparatorChar;
            }
            return _drive;
        }

        public bool TryAuthenticate()
        {
            return _gateway.TryAuthenticate(_rootName, _apiKey, _parameters);
        }

        public RootDirectoryInfoContract GetRoot()
        {
            return ExecuteInSemaphore(() => {
                GetDrive();
                var root = _gateway.GetRoot(_rootName, _apiKey, _parameters);
                root.Drive = _drive;
                return root;
            }, nameof(GetRoot));
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Language", "CSE0003:Use expression-bodied members")]
        public IEnumerable<FileSystemInfoContract> GetChildItem(DirectoryInfoContract parent)
        {
            return ExecuteInSemaphore(() => {
                return _gateway.GetChildItem(_rootName, parent.Id);
            }, nameof(GetChildItem));
        }

        public Stream GetContent(FileInfoContract source)
        {
            return ExecuteInSemaphore(() => {
                var gatewayContent = _gateway.GetContent(_rootName, source.Id).ToSeekableStream();

                var content = gatewayContent.DecryptOrPass(_encryptionKey);
                if (content != gatewayContent)
                    gatewayContent.Close();
                source.Size = (FileSize)content.Length;

//#if DEBUG
//                CompositionInitializer.SatisfyImports(content = new TraceStream(nameof(source), source.Name, content));
//#endif
                return content;
            }, nameof(GetContent));
        }

        public void SetContent(FileInfoContract target, Stream content)
        {
            ExecuteInSemaphore(() => {
                var gatewayContent = content.EncryptOrPass(_encryptionKey);
                target.Size = (FileSize)content.Length;

//#if DEBUG
//                CompositionInitializer.SatisfyImports(gatewayContent = new TraceStream(nameof(target), target.Name, gatewayContent));
//#endif
                _gateway.SetContent(_rootName, target.Id, gatewayContent, null);
                if (content != gatewayContent)
                    gatewayContent.Close();
            }, nameof(SetContent), true);
        }

        public FileSystemInfoContract MoveItem(FileSystemInfoContract source, string movePath, DirectoryInfoContract destination)
        {
            return ExecuteInSemaphore(() => {
                return !(source is ProxyFileInfoContract) ? _gateway.MoveItem(_rootName, source.Id, movePath, destination.Id) : new ProxyFileInfoContract(movePath);
            }, nameof(MoveItem), true);
        }

        public DirectoryInfoContract NewDirectoryItem(DirectoryInfoContract parent, string name)
        {
            return ExecuteInSemaphore(() => {
                return _gateway.NewDirectoryItem(_rootName, parent.Id, name);
            }, nameof(NewDirectoryItem), true);
        }

        public FileInfoContract NewFileItem(DirectoryInfoContract parent, string name, Stream content)
        {
            return ExecuteInSemaphore(() => {
                if (content.Length == 0)
                    return new ProxyFileInfoContract(name);

                var gatewayContent = content.EncryptOrPass(_encryptionKey);

                var result = _gateway.NewFileItem(_rootName, parent.Id, name, gatewayContent, null);
                result.Size = (FileSize)content.Length;
                return result;
            }, nameof(NewFileItem), true);
        }

        public void RemoveItem(FileSystemInfoContract target, bool recurse)
        {
            ExecuteInSemaphore(() => {
                if (!(target is ProxyFileInfoContract))
                    _gateway.RemoveItem(_rootName, target.Id, recurse);
            }, nameof(RemoveItem), true);
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Justification = "Debugger Display")]
        [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
        string DebuggerDisplay => $"{nameof(CloudDrive)} {DisplayRoot}".ToString(CultureInfo.CurrentCulture);
    }
}
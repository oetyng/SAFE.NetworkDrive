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
using SAFE.NetworkDrive.Interface.IO;
using SAFE.NetworkDrive.IO;
using SAFE.NetworkDrive.Parameters;
using SAFE.Filesystem.Interface.IO;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;

namespace SAFE.NetworkDrive
{
    [System.Diagnostics.DebuggerDisplay("{DebuggerDisplay,nq}")]
    internal class AsyncCloudDrive : CloudDriveBase, ICloudDrive
    {
        readonly IAsyncCloudGateway _gateway;

        readonly IDictionary<string, string> _parameters;

        public AsyncCloudDrive(RootName rootName, IAsyncCloudGateway gateway, CloudDriveParameters parameters) 
            : base(rootName, parameters)
        {
            this._gateway = gateway;
            this._parameters = parameters.Parameters;
        }

        public IPersistGatewaySettings PersistSettings => _gateway as IPersistGatewaySettings;

        protected override DriveInfoContract GetDrive()
        {
            try
            {
                if (_drive == null)
                {
                    _drive = _gateway.GetDriveAsync(_rootName, _apiKey, _parameters).Result;
                    _drive.Name = DisplayRoot + Path.VolumeSeparatorChar;
                }
                return _drive;
            }
            catch (AggregateException ex) when (ex.InnerExceptions.Count == 1)
            {
                throw ex.InnerExceptions[0];
            }
        }

        public bool TryAuthenticate()
        {
            return _gateway.TryAuthenticateAsync(_rootName, _apiKey, _parameters).Result;
        }

        public RootDirectoryInfoContract GetRoot()
        {
            return ExecuteInSemaphore(() => {
                GetDrive();
                var root = _gateway.GetRootAsync(_rootName, _apiKey, _parameters).Result;
                root.Drive = _drive;
                return root;
            }, nameof(GetRoot));
        }

        public IEnumerable<FileSystemInfoContract> GetChildItem(DirectoryInfoContract parent)
        {
            return ExecuteInSemaphore(() => {
                return _gateway.GetChildItemAsync(_rootName, parent.Id).Result;
            }, nameof(GetChildItem));
        }

        public Stream GetContent(FileInfoContract source)
        {
            return ExecuteInSemaphore(() => {
                var gatewayContent = _gateway.GetContentAsync(_rootName, source.Id).Result.ToSeekableStream();

                var content = gatewayContent.DecryptOrPass(_encryptionKey);
                if (content != gatewayContent)
                    gatewayContent.Close();
                source.Size = (FileSize)content.Length;

//#if DEBUG
//                content = new TraceStream(nameof(GetContent), source.Name, content);
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
//                gatewayContent = new TraceStream(nameof(SetContent), target.Name, gatewayContent);
//#endif
                FileSystemInfoLocator locator() => new FileSystemInfoLocator(target);
                _gateway.SetContentAsync(_rootName, target.Id, gatewayContent, null, locator).Wait();
                if (content != gatewayContent)
                    gatewayContent.Close();
            }, nameof(SetContent), true);
        }

        public FileSystemInfoContract MoveItem(FileSystemInfoContract source, string movePath, DirectoryInfoContract destination)
        {
            return ExecuteInSemaphore(() => {
                var proxySource = source as ProxyFileInfoContract;
                if (proxySource != null)
                    return new ProxyFileInfoContract(movePath);

                FileSystemInfoLocator locator() => new FileSystemInfoLocator(source);
                return _gateway.MoveItemAsync(_rootName, source.Id, movePath, destination.Id, locator).Result;
            }, nameof(MoveItem), true);
        }

        public DirectoryInfoContract NewDirectoryItem(DirectoryInfoContract parent, string name)
        {
            return ExecuteInSemaphore(() => {
                return _gateway.NewDirectoryItemAsync(_rootName, parent.Id, name).Result;
            }, nameof(NewDirectoryItem), true);
        }

        public FileInfoContract NewFileItem(DirectoryInfoContract parent, string name, Stream content)
        {
            return ExecuteInSemaphore(() => {
                if (content.Length == 0)
                    return new ProxyFileInfoContract(name);

                var gatewayContent = content.EncryptOrPass(_encryptionKey);

                var result = _gateway.NewFileItemAsync(_rootName, parent.Id, name, gatewayContent, null).Result;
                result.Size = (FileSize)content.Length;
                return result;
            }, nameof(NewFileItem), true);
        }

        public void RemoveItem(FileSystemInfoContract target, bool recurse)
        {
            ExecuteInSemaphore(() => {
                if (!(target is ProxyFileInfoContract))
                    _gateway.RemoveItemAsync(_rootName, target.Id, recurse).Wait();
            }, nameof(RemoveItem), true);
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Justification = "Debugger Display")]
        [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
        string DebuggerDisplay => $"{nameof(AsyncCloudDrive)} {DisplayRoot}".ToString(CultureInfo.CurrentCulture);
    }
}
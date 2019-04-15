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
using SAFE.NetworkDrive.Tests.Gateway.Config;
using System;
using System.Linq;

namespace SAFE.NetworkDrive.Tests.Gateway
{
    public partial class GenericGatewayTests
    {
        class TestDirectoryFixture : IDisposable
        {
            readonly Interfaces.IMemoryGateway _gateway;
            readonly RootName _root;
            readonly DirectoryInfoContract _directory;

            internal DirectoryId Id => _directory.Id;

            TestDirectoryFixture(Interfaces.IMemoryGateway gateway, RootName root, string path)
            {
                _gateway = gateway;
                _root = root;

                gateway.GetDrive();
                var rootDirectory = gateway.GetRoot();

                var residualDirectory = gateway.GetChildItem(rootDirectory.Id).SingleOrDefault(f => f.Name == path) as DirectoryInfoContract;
                if (residualDirectory != null)
                    gateway.RemoveItem(residualDirectory.Id, true);

                _directory = gateway.NewDirectoryItem(rootDirectory.Id, path);
            }

            internal static TestDirectoryFixture CreateTestDirectory(Interfaces.IMemoryGateway gateway, GatewaySection config, GatewayTestsFixture fixture)
                => new TestDirectoryFixture(gateway, fixture.GetRootName(config), config.TestDirectory);

            internal DirectoryInfoContract ToContract()
                => _directory;

            void IDisposable.Dispose()
                => _gateway.RemoveItem(_directory.Id, true);
        }
    }
}
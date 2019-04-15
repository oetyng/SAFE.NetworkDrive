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

using System;
using System.Globalization;
using System.IO;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SAFE.Filesystem.Interface.IO;
using SAFE.NetworkDrive.Interface;

namespace SAFE.NetworkDrive.Tests.Gateway
{
    [TestClass]
    public partial class GenericGatewayTests
    {
        const string _smallContent = @"Lorem ipsum dolor sit amet, consectetur adipiscing elit, sed do eiusmod tempor incididunt ut labore et dolore magna aliqua. Ut enim ad minim veniam, quis nostrud exercitation ullamco laboris nisi ut aliquip ex ea commodo consequat. Duis aute irure dolor in reprehenderit in voluptate velit esse cillum dolore eu fugiat nulla pariatur. Excepteur sint occaecat cupidatat non proident, sunt in culpa qui officia deserunt mollit anim id est laborum.";
        static byte[] _largeContent;
        GatewayTestsFixture _fixture;

        [ClassInitialize]
        public static void ClassInitialize(TestContext context)
        {
            _largeContent = new byte[new FileSize("12MB")];
            for (int i = 0; i < _largeContent.Length; ++i)
                _largeContent[i] = (byte)(i % 251 + 1);
        }

        [TestInitialize]
        public void Initialize() => _fixture = new GatewayTestsFixture();

        [TestCleanup]
        public void Cleanup() => _fixture = null;

        [TestMethod, TestCategory(nameof(TestCategories.Online))]
        public void GetDrive_ReturnsResult()
        {
            _fixture.ExecuteByConfiguration((gateway, rootName, config) => {
                _fixture.OnCondition(config, GatewayCapabilities.GetDrive, () =>
                {
                    var drive = gateway.GetDrive();

                    Assert.IsNotNull(drive, $"Drive is null ({config.Schema})".ToString(CultureInfo.CurrentCulture));
                    Assert.IsNotNull(drive.Id, $"Missing drive ID ({config.Schema})".ToString(CultureInfo.CurrentCulture));
                    Assert.IsNotNull(drive.FreeSpace, $"Missing free space ({config.Schema})".ToString(CultureInfo.CurrentCulture));
                    Assert.IsNotNull(drive.UsedSpace, $"Missing used space ({config.Schema})".ToString(CultureInfo.CurrentCulture));
                });
            });
        }

        [TestMethod, TestCategory(nameof(TestCategories.Online))]
        public void GetRoot_ReturnsResult()
        {
            _fixture.ExecuteByConfiguration((gateway, rootName, config) => {
                gateway.GetDrive();

                _fixture.OnCondition(config, GatewayCapabilities.GetRoot, () =>
                {
                    var root = gateway.GetRoot();

                    Assert.IsNotNull(root, "Root is null");
                    Assert.AreEqual(Path.DirectorySeparatorChar.ToString(), root.Name, "Unexpected root name");
                });
            });
        }

        [TestMethod, TestCategory(nameof(TestCategories.Online))]
        public void GetChildItem_ReturnsResults()
        {
            _fixture.ExecuteByConfiguration((gateway, rootName, config) => {
                using (var testDirectory = TestDirectoryFixture.CreateTestDirectory(gateway, config, _fixture))
                {
                    gateway.GetDrive();

                    gateway.NewDirectoryItem(testDirectory.Id, "DirectoryContent");
                    gateway.NewFileItem(testDirectory.Id, "File.ext", new MemoryStream(new byte[100]), _fixture.GetProgressReporter());

                    _fixture.OnCondition(config, GatewayCapabilities.GetChildItem, () =>
                    {
                        var items = gateway.GetChildItem(testDirectory.Id).ToList();

                        Assert.AreEqual(2, items.Count, "Unexpected number of results");
                        Assert.IsTrue(items.OfType<DirectoryInfoContract>().Any(i => i.Name == "DirectoryContent"), "Expected directory is missing");
                        Assert.IsTrue(items.OfType<FileInfoContract>().Any(i => i.Name == "File.ext" && i.Size == 100), "Expected file is missing");
                    });
                }
            });
        }

        [TestMethod, TestCategory(nameof(TestCategories.Online))]
        public void ClearContent_ExecutesClear()
        {
            _fixture.ExecuteByConfiguration((gateway, rootName, config) => {
                using (var testDirectory = TestDirectoryFixture.CreateTestDirectory(gateway, config, _fixture))
                {
                    gateway.GetDrive();

                    var testFile = gateway.NewFileItem(testDirectory.Id, "File.ext", new MemoryStream(new byte[100]), _fixture.GetProgressReporter());
                    testFile.Directory = testDirectory.ToContract();

                    _fixture.OnCondition(config, GatewayCapabilities.ClearContent, () =>
                    {
                        gateway.ClearContent(testFile.Id);

                        var items = gateway.GetChildItem(testDirectory.Id).ToList();

                        testFile = (FileInfoContract)items.Single();
                        Assert.AreEqual("File.ext", testFile.Name, "Expected file is missing");
                        Assert.AreEqual(FileSize.Empty, testFile.Size, "Mismatched content size");
                    });
                }
            });
        }

        [TestMethod, TestCategory(nameof(TestCategories.Online))]
        public void GetContent_ReturnsResult()
        {
            _fixture.ExecuteByConfiguration((gateway, rootName, config) => {
                using (var testDirectory = TestDirectoryFixture.CreateTestDirectory(gateway, config, _fixture))
                {
                    gateway.GetDrive();

                    var testFile = gateway.NewFileItem(testDirectory.Id, "File.ext", _smallContent.ToStream(), _fixture.GetProgressReporter());

                    _fixture.OnCondition(config, GatewayCapabilities.GetContent, () =>
                    {
                        using (var result = gateway.GetContent(testFile.Id))
                        using (var streamReader = new StreamReader(result))
                        {
                            Assert.AreEqual(_smallContent, streamReader.ReadToEnd(), "Mismatched content");
                        }
                    });
                }
            });
        }

        [TestMethod, TestCategory(nameof(TestCategories.Online))]
        public void SetContent_ExecutesSet()
        {
            _fixture.ExecuteByConfiguration((gateway, rootName, config) => {
                using (var testDirectory = TestDirectoryFixture.CreateTestDirectory(gateway, config, _fixture))
                {
                    gateway.GetDrive();

                    var testFile = gateway.NewFileItem(testDirectory.Id, "File.ext", new MemoryStream(new byte[100]), _fixture.GetProgressReporter());
                    testFile.Directory = testDirectory.ToContract();

                    _fixture.OnCondition(config, GatewayCapabilities.SetContent, () =>
                    {
                        gateway.SetContent(testFile.Id, _smallContent.ToStream(), _fixture.GetProgressReporter());

                        using (var result = gateway.GetContent(testFile.Id))
                        using (var streamReader = new StreamReader(result))
                        {
                            Assert.AreEqual(_smallContent, streamReader.ReadToEnd(), "Mismatched content");
                        }
                    });
                }
            });
        }

        [TestMethod, TestCategory(nameof(TestCategories.Online))]
        public void SetContent_AfterGetContent_ExecutesSet()
        {
            _fixture.ExecuteByConfiguration((gateway, rootName, config) => {
                using (var testDirectory = TestDirectoryFixture.CreateTestDirectory(gateway, config, _fixture))
                {
                    gateway.GetDrive();

                    var testFile = gateway.NewFileItem(testDirectory.Id, "File.ext", _smallContent.ToStream(), _fixture.GetProgressReporter());
                    testFile.Directory = testDirectory.ToContract();

                    _fixture.OnCondition(config, GatewayCapabilities.SetContent, () =>
                    {
                        using (var result = gateway.GetContent(testFile.Id))
                        using (var streamReader = new StreamReader(result))
                        {
                            Assert.AreEqual(_smallContent, streamReader.ReadToEnd(), "Mismatched initial content");
                        }

                        var changedContent = new string(_smallContent.Reverse().ToArray());
                        gateway.SetContent(testFile.Id, changedContent.ToStream(), _fixture.GetProgressReporter());

                        using (var result = gateway.GetContent(testFile.Id))
                        using (var streamReader = new StreamReader(result))
                        {
                            Assert.AreEqual(changedContent, streamReader.ReadToEnd(), "Mismatched updated content");
                        }
                    });
                }
            });
        }

        [TestMethod, TestCategory(nameof(TestCategories.Online)), Timeout(30000)]
        public void SetContent_WhereContentIsLarge_ExecutesSet()
        {
            _fixture.ExecuteByConfiguration((gateway, rootName, config) => {
                using (var testDirectory = TestDirectoryFixture.CreateTestDirectory(gateway, config, _fixture))
                {
                    gateway.GetDrive();

                    var testFile = gateway.NewFileItem(testDirectory.Id, "File.ext", new MemoryStream(new byte[100]), _fixture.GetProgressReporter());
                    testFile.Directory = testDirectory.ToContract();

                    _fixture.OnCondition(config, GatewayCapabilities.SetContent, () =>
                    {
                        gateway.SetContent(testFile.Id, new MemoryStream(_largeContent), _fixture.GetProgressReporter());

                        using (var result = gateway.GetContent(testFile.Id))
                        {
                            var buffer = new byte[_largeContent.Length];
                            int position = 0, bytesRead = 0;
                            do
                            {
                                position += bytesRead = result.Read(buffer, position, buffer.Length - position);
                            } while (bytesRead != 0);
                            Assert.AreEqual(buffer.Length, position, "Truncated result content");
                            Assert.AreEqual(-1, result.ReadByte(), "Excessive result content");
                            CollectionAssert.AreEqual(_largeContent, buffer, "Mismatched result content");
                        }
                    });
                }
            }, 4);
        }

        [TestMethod, TestCategory(nameof(TestCategories.Online))]
        public void CopyItem_WhereItemIsDirectory_ToSameDirectoryExecutesCopy()
        {
            _fixture.ExecuteByConfiguration((gateway, rootName, config) => {
                using (var testDirectory = TestDirectoryFixture.CreateTestDirectory(gateway, config, _fixture))
                {
                    gateway.GetDrive();

                    var directoryOriginal = gateway.NewDirectoryItem(testDirectory.Id, "Directory");
                    var fileOriginal = gateway.NewFileItem(directoryOriginal.Id, "File.ext", _smallContent.ToStream(), _fixture.GetProgressReporter());

                    _fixture.OnCondition(config, GatewayCapabilities.CopyDirectoryItem, () =>
                    {
                        var directoryCopy = (DirectoryInfoContract)gateway.CopyItem(directoryOriginal.Id, "Directory-Copy", testDirectory.Id, true);

                        var items = gateway.GetChildItem(testDirectory.Id);
                        Assert.AreEqual(items.Single(i => i.Name == "Directory-Copy").Id, directoryCopy.Id, "Mismatched copied directory Id");
                        Assert.IsNotNull(items.SingleOrDefault(i => i.Name == "Directory"), "Original directory is missing");
                        var copiedFile = (FileInfoContract)gateway.GetChildItem(directoryCopy.Id).SingleOrDefault(i => i.Name == "File.ext");
                        Assert.IsTrue(copiedFile != null, "Expected copied file is missing");
                        using (var result = gateway.GetContent(copiedFile.Id))
                        using (var streamReader = new StreamReader(result))
                        {
                            Assert.AreEqual(_smallContent, streamReader.ReadToEnd(), "Mismatched content");
                        }
                        Assert.AreNotEqual(fileOriginal.Id, copiedFile.Id, "Duplicate copied file Id");
                    });
                }
            });
        }

        [TestMethod, TestCategory(nameof(TestCategories.Online))]
        public void CopyItem_WhereItemIsDirectory_ToDifferentDirectoryExecutesCopy()
        {
            _fixture.ExecuteByConfiguration((gateway, rootName, config) => {
                using (var testDirectory = TestDirectoryFixture.CreateTestDirectory(gateway, config, _fixture))
                {
                    gateway.GetDrive();

                    var directoryOriginal = gateway.NewDirectoryItem(testDirectory.Id, "Directory");
                    var fileOriginal = gateway.NewFileItem(directoryOriginal.Id, "File.ext", _smallContent.ToStream(), _fixture.GetProgressReporter());
                    var directoryTarget = gateway.NewDirectoryItem(testDirectory.Id, "Target");

                    _fixture.OnCondition(config, GatewayCapabilities.CopyDirectoryItem, () =>
                    {
                        var directoryCopy = (DirectoryInfoContract)gateway.CopyItem(directoryOriginal.Id, "Directory-Copy", directoryTarget.Id, true);

                        var items = gateway.GetChildItem(testDirectory.Id);
                        var targetItems = gateway.GetChildItem(directoryTarget.Id);
                        Assert.AreEqual(targetItems.Single(i => i.Name == "Directory-Copy").Id, directoryCopy.Id, "Mismatched copied directory Id");
                        Assert.IsNotNull(items.SingleOrDefault(i => i.Name == "Directory"), "Original directory is missing");
                        var copiedFile = (FileInfoContract)gateway.GetChildItem(directoryCopy.Id).SingleOrDefault(i => i.Name == "File.ext");
                        Assert.IsTrue(copiedFile != null, "Expected copied file is missing");
                        using (var result = gateway.GetContent(copiedFile.Id))
                        using (var streamReader = new StreamReader(result))
                        {
                            Assert.AreEqual(_smallContent, streamReader.ReadToEnd(), "Mismatched content");
                        }
                        Assert.AreNotEqual(fileOriginal.Id, copiedFile.Id, "Duplicate copied file Id");
                    });
                }
            });
        }

        [TestMethod, TestCategory(nameof(TestCategories.Online))]
        public void CopyItem_WhereItemIsFile_ToSameDirectory_ExecutesCopy()
        {
            _fixture.ExecuteByConfiguration((gateway, rootName, config) => {
                using (var testDirectory = TestDirectoryFixture.CreateTestDirectory(gateway, config, _fixture))
                {
                    gateway.GetDrive();

                    var fileOriginal = gateway.NewFileItem(testDirectory.Id, "File.ext", _smallContent.ToStream(), _fixture.GetProgressReporter());

                    _fixture.OnCondition(config, GatewayCapabilities.CopyFileItem, () =>
                    {
                        var fileCopy = (FileInfoContract)gateway.CopyItem(fileOriginal.Id, "File-Copy.ext", testDirectory.Id, false);

                        var items = gateway.GetChildItem(testDirectory.Id);
                        Assert.AreEqual(items.Single(i => i.Name == "File-Copy.ext").Id, fileCopy.Id, "Mismatched copied file Id");
                        Assert.IsNotNull(items.SingleOrDefault(i => i.Name == "File.ext"), "Original file is missing");
                        using (var result = gateway.GetContent(fileCopy.Id))
                        using (var streamReader = new StreamReader(result))
                        {
                            Assert.AreEqual(_smallContent, streamReader.ReadToEnd(), "Mismatched content");
                        }
                    });
                }
            });
        }

        [TestMethod, TestCategory(nameof(TestCategories.Online))]
        public void CopyItem_WhereItemIsFile_ToDifferentDirectory_ExecutesCopy()
        {
            _fixture.ExecuteByConfiguration((gateway, rootName, config) => {
                using (var testDirectory = TestDirectoryFixture.CreateTestDirectory(gateway, config, _fixture))
                {
                    gateway.GetDrive();

                    var fileOriginal = gateway.NewFileItem(testDirectory.Id, "File.ext", _smallContent.ToStream(), _fixture.GetProgressReporter());
                    var directoryTarget = gateway.NewDirectoryItem(testDirectory.Id, "Target");

                    _fixture.OnCondition(config, GatewayCapabilities.CopyFileItem, () =>
                    {
                        var fileCopy = (FileInfoContract)gateway.CopyItem(fileOriginal.Id, "File-Copy.ext", directoryTarget.Id, false);

                        var items = gateway.GetChildItem(testDirectory.Id);
                        var targetItems = gateway.GetChildItem(directoryTarget.Id);
                        Assert.AreEqual(targetItems.Single(i => i.Name == "File-Copy.ext").Id, fileCopy.Id, "Mismatched copied file Id");
                        Assert.IsNotNull(items.SingleOrDefault(i => i.Name == "File.ext"), "Original file is missing");
                        using (var result = gateway.GetContent(fileCopy.Id))
                        using (var streamReader = new StreamReader(result))
                        {
                            Assert.AreEqual(_smallContent, streamReader.ReadToEnd(), "Mismatched content");
                        }
                    });
                }
            });
        }

        [TestMethod, TestCategory(nameof(TestCategories.Online))]
        public void MoveItem_WhereItemIsDirectory_ExecutesMove()
        {
            _fixture.ExecuteByConfiguration((gateway, rootName, config) => {
                using (var testDirectory = TestDirectoryFixture.CreateTestDirectory(gateway, config, _fixture))
                {
                    gateway.GetDrive();

                    var directoryOriginal = gateway.NewDirectoryItem(testDirectory.Id, "Directory");
                    directoryOriginal.Parent = testDirectory.ToContract();
                    var directoryTarget = gateway.NewDirectoryItem(testDirectory.Id, "DirectoryTarget");
                    var fileOriginal = gateway.NewFileItem(directoryOriginal.Id, "File.ext", _smallContent.ToStream(), _fixture.GetProgressReporter());

                    _fixture.OnCondition(config, GatewayCapabilities.MoveDirectoryItem, () =>
                    {
                        var directoryMoved = (DirectoryInfoContract)gateway.MoveItem(directoryOriginal.Id, "Directory", directoryTarget.Id);

                        var targetItems = gateway.GetChildItem(directoryTarget.Id);
                        Assert.AreEqual(targetItems.Single(i => i.Name == "Directory").Id, directoryMoved.Id, "Mismatched moved directory Id");
                        var originalItems = gateway.GetChildItem(testDirectory.Id);
                        Assert.IsNull(originalItems.SingleOrDefault(i => i.Name == "Directory"), "Original directory remains");
                        var fileMoved = (FileInfoContract)gateway.GetChildItem(directoryMoved.Id).SingleOrDefault(i => i.Name == "File.ext");
                        Assert.IsTrue(fileMoved != null, "Expected moved file is missing");
                        using (var result = gateway.GetContent(fileMoved.Id))
                        using (var streamReader = new StreamReader(result))
                        {
                            Assert.AreEqual(_smallContent, streamReader.ReadToEnd(), "Mismatched content");
                        }
                        if (!config.Exclusions.HasFlag(GatewayCapabilities.ItemId))
                        {
                            Assert.AreEqual(directoryOriginal.Id, directoryMoved.Id, "Mismatched moved directory Id");
                            Assert.AreEqual(fileOriginal.Id, fileMoved.Id, "Mismatched moved file Id");
                        }
                    });
                }
            });
        }

        [TestMethod, TestCategory(nameof(TestCategories.Online))]
        public void MoveItem_WhereItemIsFile_ExecutesMove()
        {
            _fixture.ExecuteByConfiguration((gateway, rootName, config) => {
                using (var testDirectory = TestDirectoryFixture.CreateTestDirectory(gateway, config, _fixture))
                {
                    gateway.GetDrive();

                    var directoryTarget = gateway.NewDirectoryItem(testDirectory.Id, "DirectoryTarget");
                    directoryTarget.Parent = testDirectory.ToContract();
                    var fileOriginal = gateway.NewFileItem(testDirectory.Id, "File.ext", _smallContent.ToStream(), _fixture.GetProgressReporter());

                    _fixture.OnCondition(config, GatewayCapabilities.MoveFileItem, () =>
                    {
                        var fileMoved = (FileInfoContract)gateway.MoveItem(fileOriginal.Id, "File.ext", directoryTarget.Id);

                        var targetItems = gateway.GetChildItem(directoryTarget.Id);
                        Assert.AreEqual(targetItems.Single(i => i.Name == "File.ext").Id, fileMoved.Id, "Mismatched moved file Id");
                        var originalItems = gateway.GetChildItem(testDirectory.Id);
                        Assert.IsNull(originalItems.SingleOrDefault(i => i.Name == "File.ext"), "Original file remains");
                        using (var result = gateway.GetContent(fileMoved.Id))
                        using (var streamReader = new StreamReader(result))
                        {
                            Assert.AreEqual(_smallContent, streamReader.ReadToEnd(), "Mismatched content");
                        }
                        if (!config.Exclusions.HasFlag(GatewayCapabilities.ItemId))
                            Assert.AreEqual(fileOriginal.Id, fileMoved.Id, "Mismatched moved file Id");
                    });
                }
            });
        }

        [TestMethod, TestCategory(nameof(TestCategories.Online))]
        public void NewDirectoryItem_CreatesDirectory()
        {
            _fixture.ExecuteByConfiguration((gateway, rootName, config) => {
                using (var testDirectory = TestDirectoryFixture.CreateTestDirectory(gateway, config, _fixture))
                {
                    gateway.GetDrive();

                    _fixture.OnCondition(config, GatewayCapabilities.NewDirectoryItem, () =>
                    {
                        var newDirectory = gateway.NewDirectoryItem(testDirectory.Id, "Directory");

                        var items = gateway.GetChildItem(testDirectory.Id);
                        Assert.AreEqual(1, items.Count(i => i.Name == "Directory"), "Expected directory is missing");
                        Assert.AreEqual(items.Single(i => i.Name == "Directory").Id, newDirectory.Id, "Mismatched directory Id");
                    });
                }
            });
        }

        [TestMethod, TestCategory(nameof(TestCategories.Online))]
        public void NewFileItem_CreatesFile()
        {
            _fixture.ExecuteByConfiguration((gateway, rootName, config) => {
                using (var testDirectory = TestDirectoryFixture.CreateTestDirectory(gateway, config, _fixture))
                {
                    gateway.GetDrive();

                    _fixture.OnCondition(config, GatewayCapabilities.NewFileItem, () =>
                    {
                        var newFile = gateway.NewFileItem(testDirectory.Id, "File.ext", _smallContent.ToStream(), _fixture.GetProgressReporter());

                        var items = gateway.GetChildItem(testDirectory.Id);
                        Assert.AreEqual(1, items.Count(i => i.Name == "File.ext"), "Expected file is missing");
                        Assert.AreEqual(items.Single(i => i.Name == "File.ext").Id, newFile.Id, "Mismatched file Id");
                        using (var result = gateway.GetContent(newFile.Id))
                        using (var streamReader = new StreamReader(result))
                        {
                            Assert.AreEqual(_smallContent, streamReader.ReadToEnd(), "Mismatched content");
                        }
                    });
                }
            });
        }

        [TestMethod, TestCategory(nameof(TestCategories.Online)), Timeout(300000)]
        public void NewFileItem_WhereContentIsLarge_CreatesFile()
        {
            _fixture.ExecuteByConfiguration((gateway, rootName, config) => {
                using (var testDirectory = TestDirectoryFixture.CreateTestDirectory(gateway, config, _fixture))
                {
                    gateway.GetDrive();

                    _fixture.OnCondition(config, GatewayCapabilities.NewFileItem, () =>
                    {
                        var newFile = gateway.NewFileItem(testDirectory.Id, "File.ext", new MemoryStream(_largeContent), _fixture.GetProgressReporter());

                        var items = gateway.GetChildItem(testDirectory.Id);
                        Assert.AreEqual(1, items.Count(i => i.Name == "File.ext"), "Expected file is missing");
                        Assert.AreEqual(items.Single(i => i.Name == "File.ext").Id, newFile.Id, "Mismatched file Id");
                        using (var result = gateway.GetContent(newFile.Id))
                        {
                            var buffer = new byte[_largeContent.Length];
                            int position = 0, bytesRead = 0;
                            do
                            {
                                position += bytesRead = result.Read(buffer, position, buffer.Length - position);
                            } while (bytesRead != 0);
                            Assert.AreEqual(buffer.Length, position, "Truncated result content");
                            Assert.AreEqual(-1, result.ReadByte(), "Excessive result content");
                            CollectionAssert.AreEqual(_largeContent, buffer, "Mismatched result content");
                        }
                    });
                }
            }, 4);
        }

        [TestMethod, TestCategory(nameof(TestCategories.Online))]
        public void RemoveItem_WhereItemIsDirectory_ExecutesRemove()
        {
            _fixture.ExecuteByConfiguration((gateway, rootName, config) => {
                using (var testDirectory = TestDirectoryFixture.CreateTestDirectory(gateway, config, _fixture))
                {
                    gateway.GetDrive();

                    var directory = gateway.NewDirectoryItem(testDirectory.Id, "Directory");
                    gateway.NewFileItem(directory.Id, "File.ext", _smallContent.ToStream(), _fixture.GetProgressReporter());

                    _fixture.OnCondition(config, GatewayCapabilities.RemoveItem, () =>
                    {
                        gateway.RemoveItem(directory.Id, true);

                        var items = gateway.GetChildItem(testDirectory.Id);
                        Assert.IsFalse(items.Any(i => i.Name == "Directory"), "Excessive directory found");
                    });
                }
            });
        }

        [TestMethod, TestCategory(nameof(TestCategories.Online))]
        public void RemoveItem_WhereItemIsFile_ExecutesRemove()
        {
            _fixture.ExecuteByConfiguration((gateway, rootName, config) => {
                using (var testDirectory = TestDirectoryFixture.CreateTestDirectory(gateway, config, _fixture))
                {
                    gateway.GetDrive();

                    var file = gateway.NewFileItem(testDirectory.Id, "File.ext", _smallContent.ToStream(), _fixture.GetProgressReporter());

                    _fixture.OnCondition(config, GatewayCapabilities.RemoveItem, () =>
                    {
                        gateway.RemoveItem(file.Id, false);

                        var items = gateway.GetChildItem(testDirectory.Id);
                        Assert.IsFalse(items.Any(i => i.Name == "File.ext"), "Excessive file found");
                    });
                }
            });
        }

        [TestMethod, TestCategory(nameof(TestCategories.Online))]
        public void RenameItem_WhereItemIsDirectory_ExecutesRename()
        {
            _fixture.ExecuteByConfiguration((gateway, rootName, config) => {
                using (var testDirectory = TestDirectoryFixture.CreateTestDirectory(gateway, config, _fixture))
                {
                    gateway.GetDrive();

                    var directory = gateway.NewDirectoryItem(testDirectory.Id, "Directory");
                    directory.Parent = testDirectory.ToContract();

                    _fixture.OnCondition(config, GatewayCapabilities.RenameDirectoryItem, () =>
                    {
                        gateway.RenameItem(directory.Id, "Directory-Renamed");

                        var items = gateway.GetChildItem(testDirectory.Id);
                        Assert.IsTrue(items.Any(i => i.Name == "Directory-Renamed"), "Expected renamed directory is missing");
                        Assert.IsFalse(items.Any(i => i.Name == "Directory"), "Excessive directory found");
                    });
                }
            });
        }

        [TestMethod, TestCategory(nameof(TestCategories.Online))]
        public void RenameItem_WhereItemIsFile_ExecutesRename()
        {
            _fixture.ExecuteByConfiguration((gateway, rootName, config) => {
                using (var testDirectory = TestDirectoryFixture.CreateTestDirectory(gateway, config, _fixture))
                {
                    gateway.GetDrive();

                    var file = gateway.NewFileItem(testDirectory.Id, "File.ext", _smallContent.ToStream(), _fixture.GetProgressReporter());
                    file.Directory = testDirectory.ToContract();

                    _fixture.OnCondition(config, GatewayCapabilities.RenameFileItem, () =>
                    {
                        gateway.RenameItem(file.Id, "File-Renamed.ext");

                        var items = gateway.GetChildItem(testDirectory.Id);
                        Assert.IsTrue(items.Any(i => i.Name == "File-Renamed.ext"), "Expected renamed file is missing");
                        using (var result = gateway.GetContent(((FileInfoContract)items.Single(i => i.Name == "File-Renamed.ext")).Id))
                        using (var streamReader = new StreamReader(result))
                        {
                            Assert.AreEqual(_smallContent, streamReader.ReadToEnd(), "Mismatched content");
                        }
                        Assert.IsFalse(items.Any(i => i.Name == "File.ext"), "Excessive file found");
                    });
                }
            });
        }
    }
}
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
using System.IO;
using System.Linq;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SAFE.NetworkDrive.Interface;
using SAFE.Filesystem.Interface.IO;

namespace SAFE.NetworkDrive.Tests
{
    [TestClass]
    public sealed partial class FileSystemTests
    {
        static Fixture _fixture;

        public TestContext TestContext { get; set; }

        [ClassInitialize]
        public static void ClassInitialize(TestContext context)
        {
            _fixture = Fixture.Initialize();
        }

        [ClassCleanup]
        public static void ClassCleanup()
        {
            _fixture?.Dispose();
            _fixture = null;
        }

        [TestInitialize]
        public void Initialize()
        {
            _fixture.Reset(TestContext.TestName);
        }

        [TestMethod, TestCategory(nameof(TestCategories.Offline))]
        public void DriveInfo_GetAvailableFreeSpace_Succeeds()
        {
            const int freeSpace = 64 * 1 << 20;
            const int usedSpace = 36 * 1 << 20;

            _fixture.SetupGetFree(freeSpace);
            _fixture.SetupGetUsed(usedSpace);

            var sut = _fixture.GetDriveInfo();

            var result = sut.AvailableFreeSpace;

            Assert.AreEqual(freeSpace, result);

            _fixture.Verify();
        }

        [TestMethod, TestCategory(nameof(TestCategories.Offline))]
        public void DriveInfo_GetDriveFormat_Succeeds()
        {
            var sut = _fixture.GetDriveInfo();

            var result = sut.DriveFormat;

            Assert.AreEqual("SAFE.NetworkDrive", result);

            _fixture.Verify();
        }

        [TestMethod, TestCategory(nameof(TestCategories.Offline))]
        public void DriveInfo_GetDriveType_Succeeds()
        {
            var sut = _fixture.GetDriveInfo();

            var result = sut.DriveType;

            Assert.AreEqual(result, DriveType.Network);

            _fixture.Verify();
        }

        [TestMethod, TestCategory(nameof(TestCategories.Offline))]
        public void DriveInfo_GetIsReady_Succeeds()
        {
            _fixture.SetupGetRoot();

            var sut = _fixture.GetDriveInfo();

            var result = sut.IsReady;

            Assert.IsTrue(result);

            _fixture.Verify();
        }

        [TestMethod, TestCategory(nameof(TestCategories.Offline))]
        public void DriveInfo_GetName_Succeeds()
        {
            var sut = _fixture.GetDriveInfo();

            var result = sut.Name;

            Assert.AreEqual(Fixture.MOUNT_POINT + Path.DirectorySeparatorChar, result);

            _fixture.Verify();
        }

        [TestMethod, TestCategory(nameof(TestCategories.Offline))]
        public void DriveInfo_GetTotalFreeSpace_Succeeds()
        {
            const int freeSpace = 64 * 1 << 20;
            const int usedSpace = 36 * 1 << 20;

            _fixture.SetupGetFree(freeSpace);
            _fixture.SetupGetUsed(usedSpace);

            var sut = _fixture.GetDriveInfo();

            var result = sut.TotalFreeSpace;

            Assert.AreEqual(usedSpace, result);

            _fixture.Verify();
        }

        [TestMethod, TestCategory(nameof(TestCategories.Offline))]
        public void DriveInfo_GetTotalSize_Succeeds()
        {
            const int freeSpace = 64 * 1 << 20;
            const int usedSpace = 36 * 1 << 20;

            _fixture.SetupGetFree(freeSpace);
            _fixture.SetupGetUsed(usedSpace);

            var sut = _fixture.GetDriveInfo();

            var result = sut.TotalSize;

            Assert.AreEqual(freeSpace + usedSpace, result);

            _fixture.Verify();
        }

        [TestMethod, TestCategory(nameof(TestCategories.Offline))]
        public void DriveInfo_GetVolumeLabel_Succeeds()
        {
            const string volumeLabel = "MockVolume";

            _fixture.SetupGetDisplayRoot(volumeLabel);

            var sut = _fixture.GetDriveInfo();

            var result = sut.VolumeLabel;

            Assert.AreEqual(volumeLabel, result);

            _fixture.Verify();
        }

        [TestMethod, TestCategory(nameof(TestCategories.Offline))]
        public void DriveInfo_GetRootDirectory_Succeeds()
        {
            var sut = _fixture.GetDriveInfo();

            var result = sut.RootDirectory;

            Assert.IsNotNull(result);
            Assert.AreEqual(Fixture.MOUNT_POINT + Path.DirectorySeparatorChar, result.Name);

            _fixture.Verify();
        }

        [TestMethod, TestCategory(nameof(TestCategories.Offline))]
        [ExpectedException(typeof(DirectoryNotFoundException))]
        public void Directory_Delete_WhereDirectoryIsUndefined_Throws()
        {
            var directoryName = $"{Fixture.MOUNT_POINT}\\NonExistingDirectory";

            _fixture.SetupGetRootDirectoryItems();

            Directory.Delete(directoryName);
        }

        [TestMethod, TestCategory(nameof(TestCategories.Offline))]
        [ExpectedException(typeof(DirectoryNotFoundException))]
        public void Directory_Move_WhereDirectoryIsUndefined_Throws()
        {
            var directoryName = $"{Fixture.MOUNT_POINT}\\NonExistingDirectory";
            var targetName = $"{Fixture.MOUNT_POINT}\\{_fixture.RootDirectoryItems.OfType<DirectoryInfoContract>().Last().Name}";

            _fixture.SetupGetRootDirectoryItems();

            Directory.Move(directoryName, targetName);
        }

        [TestMethod, TestCategory(nameof(TestCategories.Offline))]
        public void DirectoryInfo_GetDirectories_Succeeds()
        {
            _fixture.SetupGetRootDirectoryItems();

            var sut = _fixture.GetDriveInfo().RootDirectory;
            var directories = sut.GetDirectories();

            CollectionAssert.AreEqual(_fixture.RootDirectoryItems.OfType<DirectoryInfoContract>().Select(d => d.Name).ToList(), directories.Select(i => i.Name).ToList(), "Mismatched result");

            _fixture.Verify();
        }

        [TestMethod, TestCategory(nameof(TestCategories.Offline))]
        public void DirectoryInfo_GetFiles_Succeeds()
        {
            _fixture.SetupGetRootDirectoryItems();

            var sut = _fixture.GetDriveInfo().RootDirectory;
            var files = sut.GetFiles();

            CollectionAssert.AreEqual(_fixture.RootDirectoryItems.OfType<FileInfoContract>().Select(f => f.Name).ToList(), files.Select(i => i.Name).ToList(), "Mismatched result");

            _fixture.Verify();
        }

        [TestMethod, TestCategory(nameof(TestCategories.Offline))]
        public void DirectoryInfo_GetFileSystemInfos_Succeeds()
        {
            _fixture.SetupGetRootDirectoryItems();

            var sut = _fixture.GetDriveInfo().RootDirectory;
            var items = sut.GetFileSystemInfos();

            CollectionAssert.AreEqual(_fixture.RootDirectoryItems.OfType<FileSystemInfoContract>().Select(f => f.Name).ToList(), items.Select(i => i.Name).ToList(), "Mismatched result");

            _fixture.Verify();
        }

        [TestMethod, TestCategory(nameof(TestCategories.Offline))]
        public void DirectoryInfo_Create_Succeeds()
        {
            var directoryName = _fixture.Named("NewDir");

            _fixture.SetupGetRootDirectoryItems();
            _fixture.SetupNewDirectory(Path.DirectorySeparatorChar.ToString(), directoryName);

            var sut = _fixture.GetDriveInfo().RootDirectory;
            var newDirectory = new DirectoryInfo(sut.FullName + directoryName);
            newDirectory.Create();

            Assert.IsTrue(newDirectory.Exists, "Directory creation failed");

            _fixture.Verify();
        }

        [TestMethod, TestCategory(nameof(TestCategories.Offline))]
        public void DirectoryInfo_Create_WhereParentIsUndefined_Succeeds()
        {
            var directoryName = _fixture.Named("NewDir");
            var parentDirectoryName = _fixture.Named("Parent");

            _fixture.SetupGetRootDirectoryItems();
            _fixture.SetupNewDirectory(Path.DirectorySeparatorChar.ToString(), parentDirectoryName);
            _fixture.SetupGetEmptyDirectoryItems(Path.DirectorySeparatorChar + parentDirectoryName + Path.DirectorySeparatorChar);
            _fixture.SetupNewDirectory(Path.DirectorySeparatorChar + parentDirectoryName + Path.DirectorySeparatorChar, directoryName);

            var sut = _fixture.GetDriveInfo().RootDirectory;
            var newDirectory = new DirectoryInfo(sut.FullName + parentDirectoryName + @"\" + directoryName);
            newDirectory.Create();
        }

        // System.ArgumentException: The directory specified, 'NewSubDir', is not a subdirectory of 'Z:\'.
        // Parameter name: path
        [TestMethod, TestCategory(nameof(TestCategories.Offline))]
        public void DirectoryInfo_CreateSubdirectory_Succeeds()
        {
            var directoryName = _fixture.Named("NewSubDir");

            _fixture.SetupGetRootDirectoryItems();
            _fixture.SetupNewDirectory(Path.DirectorySeparatorChar.ToString(), directoryName);

            var sut = _fixture.GetDriveInfo().RootDirectory;
            var newDirectory = sut.CreateSubdirectory(directoryName);

            Assert.IsTrue(newDirectory.Exists, "Directory creation failed");

            _fixture.Verify();
        }

        [TestMethod, TestCategory(nameof(TestCategories.Offline))]
        public void DirectoryInfo_Delete_Succeeds()
        {
            var sutContract = _fixture.RootDirectoryItems.OfType<DirectoryInfoContract>().First();

            _fixture.SetupGetRootDirectoryItems();
            _fixture.SetupGetEmptyDirectoryItems(sutContract.Id.Value);
            _fixture.SetupDeleteDirectoryOrFile(sutContract);

            var root = _fixture.GetDriveInfo().RootDirectory;
            var sut = root.GetDirectories(sutContract.Name).Single();

            Assert.IsTrue(sut.Exists, "Expected directory missing");

            sut.Delete();
            sut.Refresh();

            Assert.IsFalse(sut.Exists, "Directory deletion failed");

            var residualDirectories = root.GetDirectories(sutContract.Name);
            Assert.IsFalse(residualDirectories.Any(), "Excessive directory found");

            _fixture.Verify();
        }

        [TestMethod, TestCategory(nameof(TestCategories.Offline))]
        [ExpectedException(typeof(IOException))]
        public void DirectoryInfo_Delete_WhereDirectoryIsNonEmpty_Throws()
        {
            var sutContract = _fixture.RootDirectoryItems.OfType<DirectoryInfoContract>().Single(d => d.Name == "SubDir2");

            _fixture.SetupGetRootDirectoryItems();
            _fixture.SetupGetSubDirectory2Items();
            _fixture.SetupDeleteDirectoryOrFile(sutContract);

            var root = _fixture.GetDriveInfo().RootDirectory;
            var sut = root.GetDirectories(sutContract.Name).Single();

            Assert.IsTrue(sut.Exists, "Expected directory missing");

            sut.Delete();
        }

        [TestMethod, TestCategory(nameof(TestCategories.Offline))]
        [ExpectedException(typeof(DirectoryNotFoundException))]
        public void DirectoryInfo_Delete_WhereDirectoryIsUndefined_Throws()
        {
            var sutContract = _fixture.RootDirectoryItems.OfType<DirectoryInfoContract>().First();

            _fixture.SetupGetRootDirectoryItems();

            var sut = new DirectoryInfo(Path.DirectorySeparatorChar + "UNDEFINED");

            Assert.IsFalse(sut.Exists, "Unexpected directory found");

            sut.Delete();
        }

        [TestMethod, TestCategory(nameof(TestCategories.Offline))]
        public void DirectoryInfo_GetAttributes_ReturnsExpectedValue()
        {
            var directoryName = _fixture.Named("NewSubDir");

            _fixture.SetupGetRootDirectoryItems();
            _fixture.SetupNewDirectory(Path.DirectorySeparatorChar.ToString(), directoryName);

            var sut = _fixture.GetDriveInfo().RootDirectory;
            var newDirectory = sut.CreateSubdirectory(directoryName);

            Assert.IsTrue(newDirectory.Exists, "Directory creation failed");
            Assert.AreEqual(FileAttributes.Directory, sut.Attributes, "Directory possesses unexpected Attributes");

            _fixture.Verify();
        }

        [TestMethod, TestCategory(nameof(TestCategories.Offline))]
        public void DirectoryInfo_MoveToDirectory_Succeeds()
        {
            var sutContract = _fixture.RootDirectoryItems.OfType<DirectoryInfoContract>().First();
            var targetContract = _fixture.RootDirectoryItems.OfType<DirectoryInfoContract>().Last();

            var moveCalled = false;

            _fixture.SetupGetRootDirectoryItems();
            _fixture.SetupMoveDirectoryOrFile(sutContract, targetContract, () => moveCalled = true);
            _fixture.SetupGetSubDirectory2Items(() => moveCalled ? _fixture.SubDirectory2Items.Concat(new[] { sutContract }) : _fixture.SubDirectory2Items);

            var root = _fixture.GetDriveInfo().RootDirectory;
            var sut = root.GetDirectories(sutContract.Name).Single();
            var target = root.GetDirectories(targetContract.Name).Single();

            sut.MoveTo(target.FullName + Path.DirectorySeparatorChar + sutContract.Name);

            var residualDirectories = root.GetDirectories(sutContract.Name);
            Assert.IsFalse(residualDirectories.Any(), "Original directory not removed");

            var movedDirectories = target.GetDirectories(sutContract.Name);
            Assert.AreEqual(1, movedDirectories.Count(), "Directory not moved");
            Assert.AreEqual(target.FullName, sut.Parent.FullName, "Directory not moved");

            _fixture.Verify();
        }

        // GIVES PAGE FAULT
        // [TestMethod, TestCategory(nameof(TestCategories.Offline))]
        public void DirectoryInfo_Rename_Succeeds()
        {
            var directoryName = _fixture.Named("RenamedDirectory");

            var sutContract = _fixture.RootDirectoryItems.OfType<DirectoryInfoContract>().First();

            _fixture.SetupGetRootDirectoryItems();
            _fixture.SetupRenameDirectoryOrFile(sutContract, directoryName);

            var root = _fixture.GetDriveInfo().RootDirectory;
            var sut = root.GetDirectories(sutContract.Name).Single();

            sut.MoveTo(root.FullName + Path.DirectorySeparatorChar + directoryName);

            var residualDirectories = root.GetDirectories(sutContract.Name);
            Assert.IsFalse(residualDirectories.Any(), "Original directory not removed");

            var renamedDirectories = root.GetDirectories(directoryName);
            Assert.AreEqual(1, renamedDirectories.Count(), "Directory not renamed");

            _fixture.Verify();
        }

        [TestMethod, TestCategory(nameof(TestCategories.Offline))]
        //[ExpectedException(typeof(FileNotFoundException))]
        public void File_Delete_WhereFileIsUndefined_Throws()
        {
            var fileName = $"{Fixture.MOUNT_POINT}\\NonExistingFile.ext";

            _fixture.SetupGetRootDirectoryItems();

            File.Delete(fileName);

            Assert.Inconclusive();
        }

        [TestMethod, TestCategory(nameof(TestCategories.Offline))]
        [ExpectedException(typeof(FileNotFoundException))]
        public void File_Move_WhereFileIsUndefined_Throws()
        {
            var fileName = $"{Fixture.MOUNT_POINT}\\NonExistingFile.ext";
            var targetName = $"{Fixture.MOUNT_POINT}\\{_fixture.RootDirectoryItems.OfType<DirectoryInfoContract>().Last().Name}";

            _fixture.SetupGetRootDirectoryItems();

            File.Move(fileName, targetName);
        }

        [TestMethod, TestCategory(nameof(TestCategories.Offline))]
        public void FileInfo_Create_Succeeds()
        {
            var fileName = _fixture.Named("NewFile.ext");
            var fileContent = Encoding.Default.GetBytes("Why did the chicken cross the road?");

            _fixture.SetupGetRootDirectoryItems();
            var file = _fixture.SetupNewFile(Path.DirectorySeparatorChar.ToString(), fileName);
            _fixture.SetupSetFileContent(file, fileContent);

            var sut = _fixture.GetDriveInfo().RootDirectory;
            var newFile = new FileInfo(sut.FullName + fileName);

            using (var fileStream = newFile.Create()) {
                fileStream.WriteAsync(fileContent, 0, fileContent.Length).Wait();
            }

            Assert.IsTrue(newFile.Exists, "File creation failed");

            _fixture.Verify();
        }

        [TestMethod, TestCategory(nameof(TestCategories.Offline))]
        [ExpectedException(typeof(DirectoryNotFoundException))]
        public void FileInfo_Create_WhereParentIsUndefined_Throws()
        {
            var fileName = _fixture.Named("NewFile.ext");

            _fixture.SetupGetRootDirectoryItems();

            var sut = _fixture.GetDriveInfo().RootDirectory;
            var newFile = new FileInfo(sut.FullName + @"UNDEFINED\" + fileName);

            newFile.Create().Dispose();
        }

        [TestMethod, TestCategory(nameof(TestCategories.Offline))]
        public void FileInfo_Delete_Succeeds()
        {
            var sutContract = _fixture.RootDirectoryItems.OfType<FileInfoContract>().First();

            _fixture.SetupGetRootDirectoryItems();
            _fixture.SetupDeleteDirectoryOrFile(sutContract);

            var root = _fixture.GetDriveInfo().RootDirectory;
            var sut = root.GetFiles(sutContract.Name).Single();

            Assert.IsTrue(sut.Exists, "Expected file missing");

            sut.Delete();
            sut.Refresh();

            Assert.IsFalse(sut.Exists, "File deletion failed");

            var residualFiles = root.GetFiles(sutContract.Name);
            Assert.IsFalse(residualFiles.Any(), "Excessive file found");

            _fixture.Verify();
        }

        [TestMethod, TestCategory(nameof(TestCategories.Offline))]
        public void FileInfo_GetAttributes_ReturnsExpectedValue()
        {
            var sutContract = _fixture.RootDirectoryItems.OfType<FileInfoContract>().First();

            _fixture.SetupGetRootDirectoryItems();

            var root = _fixture.GetDriveInfo().RootDirectory;
            var sut = root.GetFiles(sutContract.Name).Single();

            Assert.IsTrue(sut.Exists, "Expected file missing");
            Assert.AreEqual(FileAttributes.NotContentIndexed, sut.Attributes, "File possesses unexpected Attributes");

            _fixture.Verify();
        }

        [TestMethod, TestCategory(nameof(TestCategories.Offline))]
        public void FileInfo_GetIsReadOnly_ReturnsFalse()
        {
            var sutContract = _fixture.RootDirectoryItems.OfType<FileInfoContract>().First();

            _fixture.SetupGetRootDirectoryItems();

            var root = _fixture.GetDriveInfo().RootDirectory;
            var sut = root.GetFiles(sutContract.Name).Single();

            Assert.IsTrue(sut.Exists, "Expected file missing");
            Assert.IsFalse(sut.IsReadOnly, "File is read-only");

            _fixture.Verify();
        }

        [TestMethod, TestCategory(nameof(TestCategories.Offline))]
        public void FileInfo_GetLength_ReturnsExpectedValue()
        {
            var sutContract = _fixture.RootDirectoryItems.OfType<FileInfoContract>().First();

            _fixture.SetupGetRootDirectoryItems();

            var root = _fixture.GetDriveInfo().RootDirectory;
            var sut = root.GetFiles(sutContract.Name).Single();

            Assert.IsTrue(sut.Exists, "Expected file missing");
            Assert.AreEqual(sutContract.Size.Value, sut.Length, "File length differs");

            _fixture.Verify();
        }

        // System.IO.IOException: Incorrect function. : 'Z:\File.ext'
        [TestMethod, TestCategory(nameof(TestCategories.Offline))]
        public void FileInfo_CopyToDirectory_Succeeds()
        {
            var fileContent = Encoding.Default.GetBytes("Why did the chicken cross the road?");

            var sutContract = _fixture.RootDirectoryItems.OfType<FileInfoContract>().First();
            var targetContract = _fixture.RootDirectoryItems.OfType<DirectoryInfoContract>().Last();
            var copyContract = new FileInfoContract(targetContract.Id + Path.DirectorySeparatorChar.ToString() + sutContract.Name, sutContract.Name, sutContract.Created, sutContract.Updated, sutContract.Size, sutContract.Hash) {
                Directory = targetContract
            };

            _fixture.SetupGetRootDirectoryItems();
            _fixture.SetupGetSubDirectory2Items(_fixture.SubDirectory2Items);
            _fixture.SetupGetFileContent(sutContract, fileContent);
            _fixture.SetupNewFile(targetContract.Id.Value, copyContract.Name);
            _fixture.SetupSetFileContent(copyContract, fileContent);

            var root = _fixture.GetDriveInfo().RootDirectory;
            var sut = root.GetFiles(sutContract.Name).Single();
            var target = root.GetDirectories(targetContract.Name).Single();

            sut.CopyTo(target.FullName + Path.DirectorySeparatorChar + copyContract.Name);

            var residualFiles = root.GetFiles(sutContract.Name);
            Assert.AreEqual(1, residualFiles.Count(), "Original file removed");
            Assert.AreEqual(root.FullName, sut.Directory.FullName, "Original file relocated");

            var copiedFiles = target.GetFiles(copyContract.Name);
            Assert.AreEqual(1, copiedFiles.Count(), "File not copied");
            Assert.AreEqual(target.FullName, copiedFiles[0].Directory.FullName, "Unexpected copy location");

            _fixture.Verify();
        }

        [TestMethod, TestCategory(nameof(TestCategories.Offline))]
        public void FileInfo_MoveToDirectory_Succeeds()
        {
            var sutContract = _fixture.RootDirectoryItems.OfType<FileInfoContract>().First();
            var targetContract = _fixture.RootDirectoryItems.OfType<DirectoryInfoContract>().Last();

            var moveCalled = false;

            _fixture.SetupGetRootDirectoryItems();
            _fixture.SetupMoveDirectoryOrFile(sutContract, targetContract, () => moveCalled = true);
            _fixture.SetupGetSubDirectory2Items(() => moveCalled ? _fixture.SubDirectory2Items.Concat(new[] { sutContract }) : _fixture.SubDirectory2Items);

            var root = _fixture.GetDriveInfo().RootDirectory;
            var sut = root.GetFiles(sutContract.Name).Single();
            var target = root.GetDirectories(targetContract.Name).Single();

            sut.MoveTo(target.FullName + Path.DirectorySeparatorChar + sutContract.Name);

            var residualFiles = root.GetFiles(sutContract.Name);
            Assert.IsFalse(residualFiles.Any(), "Original file not removed");

            var movedFiles = target.GetFiles(sutContract.Name);
            Assert.AreEqual(1, movedFiles.Count(), "File not moved");
            Assert.AreEqual(target.FullName, sut.Directory.FullName, "File not moved");

            _fixture.Verify();
        }

        [TestMethod, TestCategory(nameof(TestCategories.Offline))]
        [ExpectedException(typeof(IOException))]
        public void FileInfo_Open_CreateNew_WhereFileExists_Throws()
        {
            var fileName = _fixture.Named("NewFile.ext");

            _fixture.SetupGetRootDirectoryItems();
            var file = _fixture.SetupNewFile(Path.DirectorySeparatorChar.ToString(), fileName);

            var sut = _fixture.GetDriveInfo().RootDirectory;
            var newFile = new FileInfo(sut.FullName + fileName);

            newFile.Create().Dispose();

            Assert.IsTrue(newFile.Exists, "File creation failed");

            newFile.Open(FileMode.CreateNew).Dispose();
        }

        [TestMethod, TestCategory(nameof(TestCategories.Offline))]
        public void FileInfo_Rename_Succeeds()
        {
            var fileName = _fixture.Named("RenamedFile");

            var sutContract = _fixture.RootDirectoryItems.OfType<FileInfoContract>().First();

            _fixture.SetupGetRootDirectoryItems();
            _fixture.SetupRenameDirectoryOrFile(sutContract, fileName);

            var root = _fixture.GetDriveInfo().RootDirectory;
            var sut = root.GetFiles(sutContract.Name).Single();

            sut.MoveTo(root.FullName + Path.DirectorySeparatorChar + fileName);

            var residualFiles = root.GetFiles(sutContract.Name);
            Assert.IsFalse(residualFiles.Any(), "Original file not removed");

            var renamedFiles = root.GetFiles(fileName);
            Assert.AreEqual(1, renamedFiles.Count(), "File not renamed");

            _fixture.Verify();
        }

        // System.IO.IOException: Incorrect function. : 'Z:\File.ext'
        [TestMethod, TestCategory(nameof(TestCategories.Offline))]
        public void FileStream_Read_OnOpenRead_Succeeds()
        {
            var fileContent = Encoding.Default.GetBytes("Why did the chicken cross the road?");
            var sutContract = _fixture.RootDirectoryItems.OfType<FileInfoContract>().First();
            sutContract.Size = (FileSize)fileContent.Length;

            _fixture.SetupGetRootDirectoryItems();
            _fixture.SetupGetFileContent(sutContract, fileContent);

            var root = _fixture.GetDriveInfo().RootDirectory;
            var sut = new FileInfo(root.FullName + sutContract.Name);

            var buffer = default(byte[]);
            using (var fileStream = sut.OpenRead()) {
                buffer = new byte[fileStream.Length];
                fileStream.Read(buffer, 0, buffer.Length);
            }

            Assert.AreEqual(sut.Length, buffer.Length, "Invalid file size");
            CollectionAssert.AreEqual(fileContent, buffer.Take(fileContent.Length).ToArray(), "Unexpected file content");

            _fixture.Verify();
        }

        [TestMethod, TestCategory(nameof(TestCategories.Offline))]
        public void FileStream_Write_OnOpenWrite_Succeeds()
        {
            var fileContent = Encoding.Default.GetBytes("Why did the chicken cross the road?");
            var sutContract = _fixture.RootDirectoryItems.OfType<FileInfoContract>().First();

            _fixture.SetupGetRootDirectoryItems();
            _fixture.SetupSetFileContent(sutContract, fileContent);

            var root = _fixture.GetDriveInfo().RootDirectory;
            var sut = new FileInfo(root.FullName + sutContract.Name);

            using (var fileStream = sut.OpenWrite()) {
                fileStream.WriteAsync(fileContent, 0, fileContent.Length).Wait();
            }

            _fixture.Verify();
        }

        [TestMethod, TestCategory(nameof(TestCategories.Offline))]
        public void FileStream_Write_OnOpen_WhereModeIsAppend_Succeeds()
        {
            var fileContent = Encoding.Default.GetBytes("Why did the chicken cross the road?");
            var sutContract = _fixture.RootDirectoryItems.OfType<FileInfoContract>().First();

            _fixture.SetupGetRootDirectoryItems();
            _fixture.SetupSetFileContent(sutContract, Enumerable.Repeat<byte>(0, (int)sutContract.Size).Concat(fileContent).ToArray());

            var root = _fixture.GetDriveInfo().RootDirectory;
            var sut = root.GetFiles(sutContract.Name).Single();

            using (var fileStream = sut.Open(FileMode.Append, FileAccess.Write)) {
                fileStream.WriteAsync(fileContent, 0, fileContent.Length).Wait();
            }

            _fixture.Verify();
        }

        [TestMethod, TestCategory(nameof(TestCategories.Offline))]
        public void FileStream_Write_OnOpen_WhereModeIsCreate_Succeeds()
        {
            var fileContent = Encoding.Default.GetBytes("Why did the chicken cross the road?");
            var sutContract = _fixture.RootDirectoryItems.OfType<FileInfoContract>().First();

            _fixture.SetupGetRootDirectoryItems();
            _fixture.SetupSetFileContent(sutContract, Array.Empty<byte>());
            _fixture.SetupSetFileContent(sutContract, fileContent);

            var root = _fixture.GetDriveInfo().RootDirectory;
            var sut = root.GetFiles(sutContract.Name).Single();

            using (var fileStream = sut.Open(FileMode.Create)) {
                fileStream.WriteAsync(fileContent, 0, fileContent.Length).Wait();
            }

            _fixture.Verify();
        }

        [TestMethod, TestCategory(nameof(TestCategories.Offline))]
        public void FileStream_Write_OnOpen_WhereModeIsCreateNew_Succeeds()
        {
            var fileName = _fixture.Named("NewFile.ext");
            var fileContent = Encoding.Default.GetBytes("Why did the chicken cross the road?");

            _fixture.SetupGetRootDirectoryItems();
            var sutContract = _fixture.SetupNewFile(Path.DirectorySeparatorChar.ToString(), fileName);

            _fixture.SetupGetRootDirectoryItems();
            _fixture.SetupSetFileContent(sutContract, fileContent);

            var root = _fixture.GetDriveInfo().RootDirectory;
            var sut = new FileInfo(root.FullName + fileName);

            using (var fileStream = sut.Open(FileMode.CreateNew)) {
                fileStream.WriteAsync(fileContent, 0, fileContent.Length).Wait();
            }

            _fixture.Verify();
        }

        // System.IO.IOException: IO operation will not work. 
        // Most likely the file will become too long or the handle was not opened to 
        // support synchronous IO operations.
        // (info.Context is null in CloudOperations.WriteFile)
        [TestMethod, TestCategory(nameof(TestCategories.Offline))]
        public async System.Threading.Tasks.Task FileStream_Write_OnOpen_WhereModeIsOpen_Succeeds()
        {
            var fileContent = Encoding.Default.GetBytes("Why did the chicken cross the road?");
            var sutContract = _fixture.RootDirectoryItems.OfType<FileInfoContract>().First();

            _fixture.SetupGetRootDirectoryItems();
            _fixture.SetupSetFileContent(sutContract, fileContent);

            var root = _fixture.GetDriveInfo().RootDirectory;
            var sut = root.GetFiles(sutContract.Name).Single();

            using (var fileStream = sut.Open(FileMode.Open, FileAccess.Write))
                await fileStream.WriteAsync(fileContent, 0, fileContent.Length);

            _fixture.Verify();
        }

        [TestMethod, TestCategory(nameof(TestCategories.Offline))]
        public void FileStream_Write_OnOpen_WhereModeIsOpenOrCreate_Succeeds()
        {
            var fileContent = Encoding.Default.GetBytes("Why did the chicken cross the road?");
            var sutContract = _fixture.RootDirectoryItems.OfType<FileInfoContract>().First();

            _fixture.SetupGetRootDirectoryItems();
            _fixture.SetupSetFileContent(sutContract, fileContent);

            var root = _fixture.GetDriveInfo().RootDirectory;
            var sut = root.GetFiles(sutContract.Name).Single();

            using (var fileStream = sut.Open(FileMode.OpenOrCreate)) {
                fileStream.WriteAsync(fileContent, 0, fileContent.Length).Wait();
            }

            _fixture.Verify();
        }

        // System.IO.IOException: IO operation will not work. 
        // Most likely the file will become too long or the handle was not opened to 
        // support synchronous IO operations.
        [TestMethod, TestCategory(nameof(TestCategories.Offline))]
        public async System.Threading.Tasks.Task FileStream_Write_OnOpen_WhereModeIsTruncate_Succeeds()
        {
            var fileContent = Encoding.Default.GetBytes("Why did the chicken cross the road?");
            var sutContract = _fixture.RootDirectoryItems.OfType<FileInfoContract>().First();

            _fixture.SetupGetRootDirectoryItems();
            _fixture.SetupSetFileContent(sutContract, fileContent);

            var root = _fixture.GetDriveInfo().RootDirectory;
            var sut = root.GetFiles(sutContract.Name).Single();

            using (var fileStream = sut.Open(FileMode.Truncate, FileAccess.Write))
                await fileStream.WriteAsync(fileContent, 0, fileContent.Length);

            _fixture.Verify();
        }

        [TestMethod, TestCategory(nameof(TestCategories.Offline))]
        public void FileStream_Flush_Succeeds()
        {
            var sutContract = _fixture.RootDirectoryItems.OfType<FileInfoContract>().First();

            _fixture.SetupGetRootDirectoryItems();

            var root = _fixture.GetDriveInfo().RootDirectory;
            var sut = new FileInfo(root.FullName + sutContract.Name);
            using (var fileStream = sut.OpenWrite()) {
                fileStream.FlushAsync().Wait();
            }

            _fixture.Verify();
        }

        [TestMethod, TestCategory(nameof(TestCategories.Offline))]
        public void FileStream_FlushAfterWrite_Succeeds()
        {
            var fileContent = Encoding.Default.GetBytes("Why did the chicken cross the road?");
            var sutContract = _fixture.RootDirectoryItems.OfType<FileInfoContract>().First();

            _fixture.SetupGetRootDirectoryItems();
            _fixture.SetupSetFileContent(sutContract, fileContent);

            var root = _fixture.GetDriveInfo().RootDirectory;
            var sut = new FileInfo(root.FullName + sutContract.Name);
            using (var fileStream = sut.OpenWrite()) {
                fileStream.WriteAsync(fileContent, 0, fileContent.Length).Wait();
                fileStream.FlushAsync().Wait();
            }

            _fixture.Verify();
        }

        // System.IO.IOException: Incorrect function. : 'Z:\File.ext'
        [TestMethod, TestCategory(nameof(TestCategories.Offline))]
        public void FileStream_Lock_Succeeds()
        {
            var sutContract = _fixture.RootDirectoryItems.OfType<FileInfoContract>().First();

            _fixture.SetupGetRootDirectoryItems();

            var root = _fixture.GetDriveInfo().RootDirectory;
            var sut = new FileInfo(root.FullName + sutContract.Name);
            using (var fileStream = sut.OpenRead()) {
                fileStream.Lock(0, 65536);
            }

            _fixture.Verify();
        }

        [TestMethod, TestCategory(nameof(TestCategories.Offline))]
        [ExpectedException(typeof(IOException))]
        public void FileStream_Lock_WhereFileIsLocked_Throws()
        {
            var sutContract = _fixture.RootDirectoryItems.OfType<FileInfoContract>().First();

            _fixture.SetupGetRootDirectoryItems();

            var root = _fixture.GetDriveInfo().RootDirectory;
            var sut = new FileInfo(root.FullName + sutContract.Name);
            using (var fileStream = sut.OpenRead()) {
                fileStream.Lock(0, 65536);

                fileStream.Lock(0, 65536);
            }
        }

        // System.IO.IOException: Incorrect function. : 'Z:\File.ext'
        [TestMethod, TestCategory(nameof(TestCategories.Offline))]
        public void FileStream_Unlock_Succeeds()
        {
            var sutContract = _fixture.RootDirectoryItems.OfType<FileInfoContract>().First();

            _fixture.SetupGetRootDirectoryItems();

            var root = _fixture.GetDriveInfo().RootDirectory;
            var sut = new FileInfo(root.FullName + sutContract.Name);
            using (var fileStream = sut.OpenRead()) {
                fileStream.Lock(0, 65536);

                fileStream.Unlock(0, 65536);
            }

            _fixture.Verify();
        }

        // System.IO.IOException: Incorrect function. : 'Z:\File.ext'
        [TestMethod, TestCategory(nameof(TestCategories.Offline))]
        public void FileStream_Unlock_WhereFileIsNotLocked_DoesNotThrow()
        {
            var sutContract = _fixture.RootDirectoryItems.OfType<FileInfoContract>().First();

            _fixture.SetupGetRootDirectoryItems();

            var root = _fixture.GetDriveInfo().RootDirectory;
            var sut = new FileInfo(root.FullName + sutContract.Name);
            using (var fileStream = sut.OpenRead()) {
                fileStream.Unlock(0, 65536);

                fileStream.Unlock(0, 65536);
            }

            _fixture.Verify();
        }

        [TestMethod, TestCategory(nameof(TestCategories.Offline))]
        [ExpectedException(typeof(IOException))]
        public void FileStream_ExceptionDuringRead_Throws()
        {
            var sutContract = _fixture.RootDirectoryItems.OfType<FileInfoContract>().First();

            _fixture.SetupGetRootDirectoryItems();
            _fixture.SetupGetFileContentWithError(sutContract);

            var root = _fixture.GetDriveInfo().RootDirectory;
            var sut = new FileInfo(root.FullName + sutContract.Name);
            var buffer = default(byte[]);
            using (var fileStream = sut.OpenRead()) {
                buffer = new byte[fileStream.Length];
                var bytesRead = fileStream.Read(buffer, 0, buffer.Length);
            }
        }

        [TestMethod, TestCategory(nameof(TestCategories.Offline))]
        public void FileStream_IOExceptionDuringWrite_RemovesFile()
        {
            var fileContent = Encoding.Default.GetBytes("Why did the chicken cross the road?");
            var sutContract = _fixture.RootDirectoryItems.OfType<FileInfoContract>().First();

            _fixture.SetupGetRootDirectoryItems();
            _fixture.SetupSetFileContentWithError<IOException>(sutContract, fileContent);
            _fixture.SetupDeleteDirectoryOrFile(sutContract);

            var root = _fixture.GetDriveInfo().RootDirectory;
            var sut = new FileInfo(root.FullName + sutContract.Name);
            using (var fileStream = sut.OpenWrite()) {
                fileStream.WriteAsync(fileContent, 0, fileContent.Length).Wait();
            }

            Assert.IsFalse(sut.Exists, "Defective file found");

            _fixture.Verify();
        }

        [TestMethod, TestCategory(nameof(TestCategories.Offline))]
        public void FileStream_UnauthorizedAccessExceptionDuringWrite_KeepsFile()
        {
            var fileContent = Encoding.Default.GetBytes("Why did the chicken cross the road?");
            var sutContract = _fixture.RootDirectoryItems.OfType<FileInfoContract>().First();

            _fixture.SetupGetRootDirectoryItems();
            _fixture.SetupSetFileContentWithError<UnauthorizedAccessException>(sutContract, fileContent);

            var root = _fixture.GetDriveInfo().RootDirectory;
            var sut = new FileInfo(root.FullName + sutContract.Name);
            using (var fileStream = sut.OpenWrite()) {
                fileStream.WriteAsync(fileContent, 0, fileContent.Length).Wait();
            }

            Assert.IsTrue(sut.Exists, "File removed");

            _fixture.Verify();
        }

        [TestMethod, TestCategory(nameof(TestCategories.Offline))]
        public void FileInfo_NativeAppendTo_Succeeds()
        {
            var fileContent = Encoding.Default.GetBytes("Why did the chicken cross the road?");

            _fixture.SetupGetRootDirectoryItems();
            var fileContract = _fixture.RootDirectoryItems.OfType<FileInfoContract>().First();

            var root = _fixture.GetDriveInfo().RootDirectory;

            var result = NativeMethods.AppendTo(root.FullName + fileContract.Name, fileContent, out int bytesWritten);

            Assert.IsFalse(result, "File operation succeeded unexpectedly");
            Assert.AreEqual(0, bytesWritten, "Unexpected number of bytes written");

            _fixture.Verify();
        }

        // Message: Assert.IsTrue failed. File operation failed
        [TestMethod, TestCategory(nameof(TestCategories.Offline))]
        public void FileInfo_NativeTruncate_Succeeds()
        {
            var fileContent = Encoding.Default.GetBytes("Why did the chicken cross the road?");
            var sutContract = _fixture.RootDirectoryItems.OfType<FileInfoContract>().First();

            _fixture.SetupGetRootDirectoryItems();
            var fileContract = _fixture.RootDirectoryItems.OfType<FileInfoContract>().First();
            _fixture.SetupSetFileContent(sutContract, fileContent);

            var root = _fixture.GetDriveInfo().RootDirectory;

            var result = NativeMethods.Truncate(root.FullName + fileContract.Name, fileContent, out int bytesWritten);
            //var file = root.GetFiles(fileContract.Name).Single();

            Assert.IsTrue(result, "File operation failed");
            Assert.AreEqual(fileContent.Length, bytesWritten, "Unexpected number of bytes written");

            _fixture.Verify();
        }

        ////Temporarily excluded from CI builds due to instability
        //[TestMethod, TestCategory(nameof(TestCategories.Manual))]
        ////[TestMethod, TestCategory(nameof(TestCategories.Offline))]
        //[DeploymentItem("FileSystemTests.Configuration.xml")]
        //[DataSource("Microsoft.VisualStudio.TestTools.DataSource.XML", "|DataDirectory|\\FileSystemTests.Configuration.xml", "ConfigRead", DataAccessMethod.Sequential)]
        //public void FileInfo_NativeReadOverlapped_Succeeds()
        //{
        //    var bufferSize = int.Parse((string)TestContext.DataRow["BufferSize"]);
        //    var fileSize = int.Parse((string)TestContext.DataRow["FileSize"]);
        //    var testInput = Enumerable.Range(0, fileSize).Select(i => (byte)(i % 251 + 1)).ToArray();
        //    var sutContract = new FileInfoContract(@"\File_NativeReadOverlapped.ext", "File_NativeReadOverlapped.ext", "2016-01-02 10:11:12".ToDateTime(), "2016-01-02 20:21:22".ToDateTime(), new FileSize("16kB"), "16384".ToHash());

        //    _fixture.SetupGetRootDirectoryItems(_fixture.RootDirectoryItems.Concat(new[] { sutContract }));
        //    _fixture.SetupGetFileContent(sutContract, testInput);

        //    var root = _fixture.GetDriveInfo().RootDirectory;
        //    var chunks = NativeMethods.ReadEx(root.FullName + sutContract.Name, bufferSize, fileSize);

        //    Assert.IsTrue(chunks.All(c => c.Win32Error == 0), "Win32Error occured");
        //    var result = chunks.Aggregate(Enumerable.Empty<byte>(), (b, c) => b.Concat(c.Buffer), b => b.ToArray());
        //    Assert.IsFalse(result.Any(b => b == default(byte)), "Uninitialized data detected");
        //    CollectionAssert.AreEqual(testInput, result, "Unexpected file content");

        //    _fixture.Verify();
        //}

        //[TestMethod, TestCategory(nameof(TestCategories.Offline))]
        //[DeploymentItem("FileSystemTests.Configuration.xml")]
        //[DataSource("Microsoft.VisualStudio.TestTools.DataSource.XML", "|DataDirectory|\\FileSystemTests.Configuration.xml", "ConfigWrite", DataAccessMethod.Sequential)]
        //public void FileInfo_NativeWriteOverlapped_Succeeds()
        //{
        //    var bufferSize = int.Parse((string)TestContext.DataRow["BufferSize"]);
        //    var fileSize = int.Parse((string)TestContext.DataRow["FileSize"]);
        //    var testInput = Enumerable.Range(0, fileSize).Select(i => (byte)(i % 251 + 1)).ToArray();
        //    var sutContract = _fixture.RootDirectoryItems.OfType<FileInfoContract>().First();
        //    var differences = new SynchronizedCollection<Tuple<int, int, byte[], byte[]>>();

        //    _fixture.SetupGetRootDirectoryItems();
        //    var file = _fixture.RootDirectoryItems.OfType<FileInfoContract>().First();
        //    _fixture.SetupSetFileContent(file, testInput, differences);

        //    var root = _fixture.GetDriveInfo().RootDirectory;
        //    var chunks = Enumerable.Range(0, Fixture.NumberOfChunks(bufferSize, fileSize))
        //        .Select(i => new NativeMethods.OverlappedChunk(testInput.Skip(i * bufferSize).Take(NativeMethods.BufferSize(bufferSize, fileSize, i)).ToArray())).ToArray();

        //    NativeMethods.WriteEx(root.FullName + file.Name, bufferSize, fileSize, chunks);

        //    Assert.IsTrue(chunks.All(c => c.Win32Error == 0), "Win32Error occured");

        //    Assert.IsFalse(differences.Any(), "Mismatched data detected");

        //    _fixture.Verify();
        //}
    }
}
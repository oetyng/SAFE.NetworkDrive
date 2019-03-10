﻿/*
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
using System.Collections.Generic;
using System.IO;
using System.Security.AccessControl;
using DokanNet;
using FileAccess = DokanNet.FileAccess;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using NLog;
using SAFE.Filesystem.Interface.IO;
using SAFE.NetworkDrive.Interface;

namespace SAFE.NetworkDrive.Tests
{
    [TestClass]
    public sealed class CloudOperationsTests
    {
        Mock<ICloudDrive> _driveMock;

        CloudOperations _sut;

        [TestInitialize]
        public void Initialize()
        {
            _driveMock = new Mock<ICloudDrive>();
            _sut = new CloudOperations(_driveMock.Object, new Mock<ILogger>().Object);
        }

        [TestCleanup]
        public void Cleanup()
        {
            _sut = null;
            _driveMock = null;
        }

        private static DokanFileInfo CreateDokanFileInfo()
        {
            return (DokanFileInfo)Activator.CreateInstance(typeof(DokanFileInfo), true);
        }

        private RootDirectoryInfoContract SetupGetRoot()
        {
            var root = new RootDirectoryInfoContract(@"\", DateTimeOffset.FromFileTime(0), DateTimeOffset.FromFileTime(0)) { Drive = new DriveInfoContract("Z:", null, null) };
            _driveMock
                .Setup(d => d.GetRoot())
                .Returns(root);
            return root;
        }

        [TestMethod, TestCategory(nameof(TestCategories.Offline))]
        [ExpectedException(typeof(ArgumentNullException))]
        public void CloudOperations_New_WhereDriveIsNull_Throws()
        {
            _sut = new CloudOperations(null, new Mock<ILogger>().Object);
        }

        [TestMethod, TestCategory(nameof(TestCategories.Offline))]
        [ExpectedException(typeof(ArgumentNullException))]
        public void CloudOperations_Cleanup_WhereInfoIsNull_Throws()
        {
            _sut.Cleanup("File.ext", null);
        }

        [TestMethod, TestCategory(nameof(TestCategories.Offline))]
        [ExpectedException(typeof(ArgumentNullException))]
        public void CloudOperations_CloseFile_WhereInfoIsNull_Throws()
        {
            _sut.CloseFile("File.ext", null);
        }

        [TestMethod, TestCategory(nameof(TestCategories.Offline))]
        [ExpectedException(typeof(ArgumentNullException))]
        public void CloudOperations_CreateFile_WhereInfoIsNull_Throws()
        {
            _sut.CreateFile("File.ext", default, default, default, default, default, null);
        }

        [TestMethod, TestCategory(nameof(TestCategories.Offline))]
        public void CloudOperations_CreateFile_WhereFileNameIsStar_Succeeds()
        {
            var result = _sut.CreateFile(@"\*", FileAccess.ReadAttributes, default, default, default, default, CreateDokanFileInfo());

            Assert.AreEqual(DokanResult.Success, result);
        }

        [TestMethod, TestCategory(nameof(TestCategories.Offline))]
        public void CloudOperations_CreateFile_WhereFileModeIsOpenOrCreateAndFileAccessIsReadData_Succeeds()
        {
            var info = CreateDokanFileInfo();
            var root = SetupGetRoot();
            _driveMock
                .Setup(d => d.NewFileItem(root, "File.ext", Stream.Null))
                .Returns(new FileInfoContract(@"\File.ext", "File.ext", DateTimeOffset.FromFileTime(0), DateTimeOffset.FromFileTime(0), FileSize.Empty, string.Empty));

            var result = _sut.CreateFile("File.ext", FileAccess.ReadData, default, FileMode.OpenOrCreate, default, default, info);

            Assert.AreEqual(DokanResult.Success, result);
            Assert.IsNotNull(info.Context);
        }

        [TestMethod, TestCategory(nameof(TestCategories.Offline))]
        public void CloudOperations_CreateFile_WhereFileModeIsAppend_Fails()
        {
            SetupGetRoot();

            var result = _sut.CreateFile("File.ext", default, default, FileMode.Append, default, default, CreateDokanFileInfo());

            Assert.AreEqual(DokanResult.NotImplemented, result);
        }

        [TestMethod, TestCategory(nameof(TestCategories.Offline))]
        public void CloudOperations_CreateFile_WhereFileModeIsTruncate_Fails() // TODO: Rename method, since it does not fail anymore
        {
            SetupGetRoot();

            var result = _sut.CreateFile("File.ext", default, default, FileMode.Truncate, default, default, CreateDokanFileInfo());

            Assert.AreEqual(DokanResult.FileNotFound, result);
        }

        [TestMethod, TestCategory(nameof(TestCategories.Offline))]
        public void CloudOperations_CreateFile_WhereFileModeIsUnknown_Fails()
        {
            SetupGetRoot();

            var result = _sut.CreateFile("File.ext", default, default, default, default, default, CreateDokanFileInfo());

            Assert.AreEqual(DokanResult.NotImplemented, result);
        }

        [TestMethod, TestCategory(nameof(TestCategories.Offline))]
        [ExpectedException(typeof(ArgumentNullException))]
        public void CloudOperations_DeleteFile_WhereInfoIsNull_Throws()
        {
            _sut.DeleteFile("File.ext", null);
        }

        [TestMethod, TestCategory(nameof(TestCategories.Offline))]
        [ExpectedException(typeof(ArgumentNullException))]
        public void CloudOperations_FindFilesWithPattern_WhereSearchPatternIsNull_Throws()
        {
            _sut.FindFilesWithPattern("Directory", null, out IList<FileInformation> files, CreateDokanFileInfo());
        }

        [TestMethod, TestCategory(nameof(TestCategories.Offline))]
        [ExpectedException(typeof(ArgumentNullException))]
        public void CloudOperations_FlushFileBuffers_WhereInfoIsNull_Throws()
        {
            _sut.FlushFileBuffers("File.ext", null);
        }

        [TestMethod, TestCategory(nameof(TestCategories.Offline))]
        [ExpectedException(typeof(ArgumentNullException))]
        public void CloudOperations_GetFileInformation_WhereInfoIsNull_Throws()
        {
            _sut.GetFileInformation("File.ext", out FileInformation fileInformation, null);
        }

        [TestMethod, TestCategory(nameof(TestCategories.Offline))]
        [ExpectedException(typeof(ArgumentNullException))]
        public void CloudOperations_GetFileSecurity_WhereInfoIsNull_Throws()
        {
            _sut.GetFileSecurity("File.ext", out FileSystemSecurity security, default, null);
        }

        [TestMethod, TestCategory(nameof(TestCategories.Offline))]
        [ExpectedException(typeof(ArgumentNullException))]
        public void CloudOperations_LockFile_WhereInfoIsNull_Throws()
        {
            _sut.LockFile("File.ext", 0, 1, null);
        }

        [TestMethod, TestCategory(nameof(TestCategories.Offline))]
        [ExpectedException(typeof(ArgumentNullException))]
        public void CloudOperations_ReadFile_WhereBufferIsNull_Throws()
        {
            var info = CreateDokanFileInfo();
            _sut.ReadFile("File.ext", null, out int bytesRead, 0, info);
        }

        [TestMethod, TestCategory(nameof(TestCategories.Offline))]
        [ExpectedException(typeof(ArgumentOutOfRangeException))]
        public void CloudOperations_ReadFile_WhereOffsetIsNegative_Throws()
        {
            var buffer = new byte[1];
            var info = CreateDokanFileInfo();
            _sut.ReadFile("File.ext", buffer, out int bytesRead, -1, info);
        }

        [TestMethod, TestCategory(nameof(TestCategories.Offline))]
        [ExpectedException(typeof(ArgumentNullException))]
        public void CloudOperations_ReadFile_WhereInfoIsNull_Throws()
        {
            var buffer = new byte[1];
            _sut.ReadFile("File.ext", buffer, out int bytesRead, 0, null);
        }

        [TestMethod, TestCategory(nameof(TestCategories.Offline))]
        [ExpectedException(typeof(ArgumentNullException))]
        public void CloudOperations_SetAllocationSize_WhereInfoIsNull_Throws()
        {
            _sut.SetAllocationSize("File.ext", 1, null);
        }

        [TestMethod, TestCategory(nameof(TestCategories.Offline))]
        public void CloudOperations_SetFileSecurity_Fails()
        {
            var result = _sut.SetFileSecurity("File.ext", null, new AccessControlSections(), CreateDokanFileInfo());

            Assert.AreEqual(DokanResult.NotImplemented, result);
        }

        [TestMethod, TestCategory(nameof(TestCategories.Offline))]
        [ExpectedException(typeof(ArgumentNullException))]
        public void CloudOperations_UnlockFile_WhereInfoIsNull_Throws()
        {
            _sut.UnlockFile("File.ext", 0, 1, null);
        }

        [TestMethod, TestCategory(nameof(TestCategories.Offline))]
        [ExpectedException(typeof(ArgumentNullException))]
        public void CloudOperations_Unmounted_WhereInfoIsNull_Throws()
        {
            _sut.Unmounted(null);
        }

        [TestMethod, TestCategory(nameof(TestCategories.Offline))]
        public void CloudOperations_Unmounted_Succeeds()
        {
            var result = _sut.Unmounted(CreateDokanFileInfo());

            Assert.AreEqual(DokanResult.Success, result);
        }

        [TestMethod, TestCategory(nameof(TestCategories.Offline))]
        [ExpectedException(typeof(ArgumentNullException))]
        public void CloudOperations_WriteFile_WhereBufferIsNull_Throws()
        {
            var info = CreateDokanFileInfo();
            _sut.WriteFile("File.ext", null, out int bytesWritten, 0, info);
        }

        [TestMethod, TestCategory(nameof(TestCategories.Offline))]
        [ExpectedException(typeof(ArgumentNullException))]
        public void CloudOperations_WriteFile_WhereInfoIsNull_Throws()
        {
            var buffer = new byte[1];
            _sut.WriteFile("File.ext", buffer, out int bytesWritten, 0, null);
        }
    }
}

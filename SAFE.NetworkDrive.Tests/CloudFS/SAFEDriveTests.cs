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
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SAFE.NetworkDrive.Interface;

namespace SAFE.NetworkDrive.Tests
{
    [TestClass]
    public sealed partial class SAFEDriveTests
    {
        Fixture _fixture;

        [ClassInitialize]
        public static void ClassInitialize(TestContext testContext) { }

        [TestInitialize]
        public void Initialize() => _fixture = Fixture.Initialize();

        [TestMethod]
        public void SAFEDrive_Create_Succeeds()
        {
            using (var result = _fixture.Create())
                Assert.IsNotNull(result, "Missing result");
        }

        [TestMethod]
        public void SAFEDrive_GetFree_Succeeds()
        {
            _fixture.SetupGetDrive();

            using (var sut = _fixture.Create()) {
                var result = sut.Free;
                Assert.AreEqual(Fixture.FREE_SPACE, result, "Unexpected Free value");
            }
        }

        [TestMethod]
        public void SAFEDrive_GetUsed_Succeeds()
        {
            _fixture.SetupGetDrive();

            using (var sut = _fixture.Create()) {
                var result = sut.Used;
                Assert.AreEqual(Fixture.USED_SPACE, result, "Unexpected Used value");
            }
        }

        [TestMethod]
        [ExpectedException(typeof(ApplicationException))]
        public void SAFEDrive_GetFree_WhereGetDriveFails_Throws()
        {
            _fixture.SetupGetDriveThrows<ApplicationException>();

            using (var sut = _fixture.Create())
            {
                var result = sut.Free;
            }
        }

        [TestMethod]
        public void SAFEDrive_GetRoot_Succeeds()
        {
            _fixture.SetupGetDrive();
            _fixture.SetupGetRoot();

            using (var sut = _fixture.Create()) {
                var result = sut.GetRoot();
                Assert.AreEqual($"{Fixture.SCHEMA}@{Fixture.VOLUME_ID}|{Fixture.MOUNT_POINT}{Path.VolumeSeparatorChar}{Path.DirectorySeparatorChar}".ToString(CultureInfo.CurrentCulture), result.FullName, "Unexpected root name");
            }
        }

        [TestMethod]
        public void SAFEDrive_GetDisplayRoot_Succeeds()
        {
            _fixture.SetupGetDrive();
            _fixture.SetupGetRoot();

            using (var sut = _fixture.Create()) {
                var result = sut.DisplayRoot;
                Assert.AreEqual($"{Fixture.SCHEMA}@{Fixture.VOLUME_ID}|{Fixture.MOUNT_POINT}".ToString(CultureInfo.CurrentCulture), result, "Unexpected DisplayRoot value");
            }
        }

        [TestMethod]
        public void SAFEDrive_GetChildItem_WhereEncryptionKeyIsEmpty_Succeeds()
        {
            _fixture.SetupGetDrive();
            _fixture.SetupGetRoot();
            _fixture.SetupGetRootDirectoryItems();

            using (var sut = _fixture.Create()) {
                var result = sut.GetChildItem(sut.GetRoot()).ToList();
                CollectionAssert.AreEqual(_fixture.RootDirectoryItems, result, "Mismatched result");
            }
        }

        [TestMethod]
        public void SAFEDrive_GetChildItem_WhereEncryptionKeyIsSet_Succeeds()
        {
            _fixture.SetupGetDrive();
            _fixture.SetupGetRoot();
            _fixture.SetupGetRootDirectoryItems();

            using (var sut = _fixture.Create()) {
                var result = sut.GetChildItem(sut.GetRoot()).ToList();
                CollectionAssert.AreEqual(_fixture.RootDirectoryItems, result, "Mismatched result");
            }
        }

        [TestMethod]
        public void SAFEDrive_GetContent_Succeeds()
        {
            var fileContent = Encoding.Default.GetBytes("Why did the chicken cross the road?");
            var sutContract = _fixture.RootDirectoryItems.OfType<FileInfoContract>().First();

            _fixture.SetupGetContent(sutContract, fileContent);

            var buffer = default(byte[]);
            using (var sut = _fixture.Create())
            using (var stream = sut.GetContent(sutContract)) {
                buffer = new byte[stream.Length];
                stream.Read(buffer, 0, buffer.Length);
            }

            Assert.AreEqual(fileContent.Length, buffer.Length, "Invalid content size");
            CollectionAssert.AreEqual(fileContent, buffer.ToArray(), "Unexpected content");

            _fixture.VerifyAll();
        }

        [TestMethod]
        public void SAFEDrive_GetContent_WhereContentIsUnencrypted_Succeeds()
        {
            var fileContent = Encoding.Default.GetBytes("Why did the chicken cross the road?");
            var sutContract = _fixture.RootDirectoryItems.OfType<FileInfoContract>().First();

            _fixture.SetupGetContent(sutContract, fileContent);

            var buffer = default(byte[]);
            using (var sut = _fixture.Create())
            using (var stream = sut.GetContent(sutContract)) {
                buffer = new byte[stream.Length];
                stream.Read(buffer, 0, buffer.Length);
            }

            Assert.AreEqual(fileContent.Length, buffer.Length, "Invalid content size");
            CollectionAssert.AreEqual(fileContent, buffer.ToArray(), "Unexpected content");

            _fixture.VerifyAll();
        }

        [TestMethod]
        public void SAFEDrive_GetContent_WhereContentIsNotSeekable_Succeeds()
        {
            var fileContent = Encoding.Default.GetBytes("Why did the chicken cross the road?");
            var sutContract = _fixture.RootDirectoryItems.OfType<FileInfoContract>().First();

            _fixture.SetupGetContent(sutContract, fileContent, canSeek: false);

            var buffer = default(byte[]);
            using (var sut = _fixture.Create())
            using (var stream = sut.GetContent(sutContract)) {
                buffer = new byte[stream.Length];
                stream.Read(buffer, 0, buffer.Length);
            }

            Assert.AreEqual(fileContent.Length, buffer.Length, "Invalid content size");
            CollectionAssert.AreEqual(fileContent, buffer.ToArray(), "Unexpected content");

            _fixture.VerifyAll();
        }

        [TestMethod]
        public void SAFEDrive_MoveDirectoryItem_Succeeds()
        {
            var sutContract = _fixture.RootDirectoryItems.OfType<DirectoryInfoContract>().Last();
            var directory = _fixture.TargetDirectory;

            _fixture.SetupMoveDirectoryOrFile(sutContract, directory);

            using (var sut = _fixture.Create())
                sut.MoveItem(sutContract, sutContract.Name, directory);

            _fixture.VerifyAll();
        }

        [TestMethod]
        public void SAFEDrive_MoveFileItem_Succeeds()
        {
            var sutContract = _fixture.RootDirectoryItems.OfType<FileInfoContract>().Last();
            var directory = _fixture.TargetDirectory;

            _fixture.SetupMoveDirectoryOrFile(sutContract, directory);

            using (var sut = _fixture.Create())
                sut.MoveItem(sutContract, sutContract.Name, directory);

            _fixture.VerifyAll();
        }

        [TestMethod]
        public void SAFEDrive_NewDirectoryItem_Succeeds()
        {
            const string newName = "NewDirectory";
            var directory = _fixture.TargetDirectory;

            _fixture.SetupNewDirectoryItem(directory, newName);

            using (var sut = _fixture.Create())
                sut.NewDirectoryItem(directory, newName);

            _fixture.VerifyAll();
        }

        [TestMethod]
        public void SAFEDrive_NewFileItem_Succeeds()
        {
            const string newName = "NewFile.ext";
            var fileContent = Encoding.Default.GetBytes("Why did the chicken cross the road?");
            var directory = _fixture.TargetDirectory;

            _fixture.SetupNewFileItem(directory, newName, fileContent);

            using (var sut = _fixture.Create())
            using (var stream = new MemoryStream(fileContent))
                sut.NewFileItem(directory, newName, stream);

            _fixture.VerifyAll();
        }

        [TestMethod]
        public void SAFEDrive_NewFileItem_WhereContentIsEmpty_Succeeds()
        {
            const string newName = "NewFile.ext";
            var directory = _fixture.TargetDirectory;

            FileInfoContract contract;
            using (var sut = _fixture.Create())
                contract = sut.NewFileItem(directory, newName, Stream.Null);

            Assert.IsInstanceOfType(contract, typeof(ProxyFileInfoContract));

            _fixture.VerifyAll();
        }

        [TestMethod]
        public void SAFEDrive_RemoveDirectoryItem_Succeeds()
        {
            var sutContract = _fixture.RootDirectoryItems.OfType<DirectoryInfoContract>().First();

            _fixture.SetupRemoveDirectoryOrFile(sutContract, true);

            using (var sut = _fixture.Create())
                sut.RemoveItem(sutContract, true);

            _fixture.VerifyAll();
        }

        [TestMethod]
        public void SAFEDrive_RemoveFileItem_Succeeds()
        {
            var sutContract = _fixture.RootDirectoryItems.OfType<FileInfoContract>().First();

            _fixture.SetupRemoveDirectoryOrFile(sutContract, false);

            using (var sut = _fixture.Create())
                sut.RemoveItem(sutContract, false);

            _fixture.VerifyAll();
        }

        [TestMethod]
        public void SAFEDrive_SetContent_Succeeds()
        {
            var fileContent = Encoding.Default.GetBytes("Why did the chicken cross the road?");
            var sutContract = _fixture.RootDirectoryItems.OfType<FileInfoContract>().First();

            _fixture.SetupSetContent(sutContract, fileContent);

            using (var sut = _fixture.Create())
            using (var stream = new MemoryStream(fileContent))
                sut.SetContent(sutContract, stream);

            _fixture.VerifyAll();
        }
    }
}
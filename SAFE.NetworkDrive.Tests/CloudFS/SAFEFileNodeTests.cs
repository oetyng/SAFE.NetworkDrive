using System;
using System.IO;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace SAFE.NetworkDrive.Tests
{
    [TestClass]
    public sealed partial class SAFEFileNodeTests
    {
        Fixture _fixture;

        [TestInitialize]
        public void Initialize()
        {
            _fixture = Fixture.Initialize();
        }

        [TestMethod, TestCategory(nameof(TestCategories.Offline))]
        [ExpectedException(typeof(ArgumentNullException))]
        public void SAFEFileNodeTests_Create_WhereContractIsMissing_Throws()
        {
            _fixture.GetFile(null);
        }

        [TestMethod, TestCategory(nameof(TestCategories.Offline))]
        public void SAFEFileNodeTests_Create_WhereContractIsSpecified_StoresContract()
        {
            var contract = _fixture.TestFile;

            var sut = _fixture.GetFile(contract);

            Assert.AreEqual(contract, sut.Contract);
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2202:Objekte nicht mehrmals verwerfen")]
        [TestMethod, TestCategory(nameof(TestCategories.Offline))]
        [ExpectedException(typeof(ArgumentNullException))]
        public void SAFEFileNodeTests_GetContent_WhereDriveIsNull_Throws()
        {
            var contract = _fixture.TestFile;

            var sut = _fixture.GetFile(contract);
            sut.GetContent(null);
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2202:Objekte nicht mehrmals verwerfen")]
        [TestMethod, TestCategory(nameof(TestCategories.Offline))]
        public void SAFEFileNodeTests_GetContent_Succeeds()
        {
            const string fileContent = "Mary had a little lamb";
            var contract = _fixture.TestFile;

            _fixture.SetupGetContent(contract, fileContent);

            var sut = _fixture.GetFile(contract);
            using (var stream = sut.GetContent(_fixture.Drive))
            using (var reader = new StreamReader(stream)) {
                Assert.AreEqual(fileContent, reader.ReadToEnd(), "Mismatched result");
            }

            _fixture.VerifyAll();
        }

        [TestMethod, TestCategory(nameof(TestCategories.Offline))]
        public void SAFEFileNodeTests_Move_Succeeds()
        {
            var contract = _fixture.TestFile;
            var directory = _fixture.TargetDirectory;

            _fixture.SetupGetChildItems(directory, _fixture.SubDirectoryItems);
            _fixture.SetupMove(contract, contract.Name, directory);

            var sut = _fixture.GetFile(contract);
            sut.Move(_fixture.Drive, contract.Name, new SAFEDirectoryNode(directory));

            _fixture.VerifyAll();
        }

        [TestMethod, TestCategory(nameof(TestCategories.Offline))]
        public void SAFEFileNodeTests_MoveAndRename_Succeeds()
        {
            const string newName = "RenamedFile.ext";
            var contract = _fixture.TestFile;
            var directory = _fixture.TargetDirectory;

            _fixture.SetupGetChildItems(directory, _fixture.SubDirectoryItems);
            _fixture.SetupMove(contract, newName, directory);

            var sut = _fixture.GetFile(contract);
            sut.Move(_fixture.Drive, newName, new SAFEDirectoryNode(directory));

            _fixture.VerifyAll();
        }

        [TestMethod, TestCategory(nameof(TestCategories.Offline))]
        public void SAFEFileNodeTests_Remove_Succeeds()
        {
            var contract = _fixture.TestFile;

            _fixture.SetupRemove(contract);

            var sut = _fixture.GetFile(contract);
            sut.Remove(_fixture.Drive);

            _fixture.VerifyAll();
        }

        [TestMethod, TestCategory(nameof(TestCategories.Offline))]
        [ExpectedException(typeof(ArgumentNullException))]
        public void SAFEFileNodeTests_SetContent_WhereDriveIsNull_Throws()
        {
            var contract = _fixture.TestFile;

            var sut = _fixture.GetFile(contract);
            sut.SetContent(null, Stream.Null);
        }

        [TestMethod, TestCategory(nameof(TestCategories.Offline))]
        public void SAFEFileNodeTests_SetContent_Succeeds()
        {
            var fileContent = Encoding.Default.GetBytes("Mary had a little lamb");
            var contract = _fixture.TestFile;

            _fixture.SetupSetContent(contract, fileContent);

            var sut = _fixture.GetFile(contract);
            using (var stream = new MemoryStream(fileContent)) {
                sut.SetContent(_fixture.Drive, stream);
            }

            _fixture.VerifyAll();
        }

        [TestMethod, TestCategory(nameof(TestCategories.Offline))]
        public void SAFEFileNodeTests_SetContent_OnProxyFileInfo_Succeeds()
        {
            var fileContent = Encoding.Default.GetBytes("Mary had a little lamb");
            var contract = _fixture.ProxyTestFile;

            _fixture.SetupNewFileItem(_fixture.ProxyParentDirectory, contract.Name, fileContent);

            var sut = _fixture.GetFile(contract, _fixture.ProxyParentDirectory);

            Assert.IsInstanceOfType(sut.Contract, typeof(NetworkDrive.Interface.ProxyFileInfoContract));

            using (var stream = new MemoryStream(fileContent)) {
                sut.SetContent(_fixture.Drive, stream);
            }

            Assert.IsInstanceOfType(sut.Contract, typeof(NetworkDrive.Interface.FileInfoContract));

            _fixture.VerifyAll();
        }

        [TestMethod, TestCategory(nameof(TestCategories.Offline))]
        [ExpectedException(typeof(ArgumentNullException))]
        public void SAFEFileNodeTests_Truncate_WhereDriveIsNull_Throws()
        {
            var contract = _fixture.TestFile;

            var sut = _fixture.GetFile(contract);
            sut.Truncate(null);
        }

        [TestMethod, TestCategory(nameof(TestCategories.Offline))]
        public void SAFEFileNodeTests_Truncate_Succeeds()
        {
            var contract = _fixture.TestFile;

            _fixture.SetupTruncate(contract);

            var sut = _fixture.GetFile(contract);
            sut.Truncate(_fixture.Drive);

            _fixture.VerifyAll();
        }
    }
}

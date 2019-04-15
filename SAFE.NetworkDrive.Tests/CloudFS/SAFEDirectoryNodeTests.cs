using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace SAFE.NetworkDrive.Tests
{
    [TestClass]
    public sealed partial class SAFEDirectoryNodeTests
    {
        Fixture _fixture;

        [TestInitialize]
        public void Initialize() => _fixture = Fixture.Initialize();

        [TestMethod, TestCategory(nameof(TestCategories.Offline))]
        [ExpectedException(typeof(ArgumentNullException))]
        public void SAFEDirectoryNode_Create_WhereContractIsMissing_Throws()
            => _fixture.GetDirectory(null);

        [TestMethod, TestCategory(nameof(TestCategories.Offline))]
        public void SAFEDirectoryNode_Create_WhereContractIsSpecified_StoresContract()
        {
            var contract = _fixture.TargetDirectory;

            var sut = _fixture.GetDirectory(contract);

            Assert.AreEqual(contract, sut.Contract);
        }

        [TestMethod, TestCategory(nameof(TestCategories.Offline))]
        public void SAFEDirectoryNode_GetChildItemByName_CallsDriveCorrectly()
        {
            var fileName = _fixture.SubDirectoryItems.First().Name;
            var directory = _fixture.TargetDirectory;

            _fixture.SetupGetChildItems(directory, _fixture.SubDirectoryItems);

            var sut = _fixture.GetDirectory(directory);
            var result = sut.GetChildItemByName(_fixture.Drive, fileName);

            Assert.AreEqual(fileName, result.Name, "Mismatched result");

            _fixture.VerifyAll();
        }

        [TestMethod, TestCategory(nameof(TestCategories.Offline))]
        [ExpectedException(typeof(ArgumentNullException))]
        public void SAFEDirectoryNode_GetChildItems_WhereDriveIsNull_Throws()
        {
            var directory = _fixture.TargetDirectory;

            var sut = _fixture.GetDirectory(directory);
            var result = sut.GetChildItems(null);
        }

        [TestMethod, TestCategory(nameof(TestCategories.Offline))]
        public void SAFEDirectoryNode_GetChildItems_CallsDriveCorrectly()
        {
            var directory = _fixture.TargetDirectory;

            _fixture.SetupGetChildItems(directory, _fixture.SubDirectoryItems);

            var sut = _fixture.GetDirectory(directory);
            var result = sut.GetChildItems(_fixture.Drive);

            CollectionAssert.AreEqual(_fixture.SubDirectoryItems, result.Select(i => i.Contract).ToArray(), "Mismatched result");

            _fixture.VerifyAll();
        }

        [TestMethod, TestCategory(nameof(TestCategories.Offline))]
        public void SAFEDirectoryNode_Move_Succeeds()
        {
            var contract = _fixture.TestDirectory;
            var directory = _fixture.TargetDirectory;

            _fixture.SetupGetChildItems(directory, _fixture.SubDirectoryItems);
            _fixture.SetupMove(contract, contract.Name, directory);

            var sut = _fixture.GetDirectory(contract);
            sut.Move(_fixture.Drive, contract.Name, new SAFEDirectoryNode(directory));

            _fixture.VerifyAll();
        }

        [TestMethod, TestCategory(nameof(TestCategories.Offline))]
        public void SAFEDirectoryNode_MoveAndRename_Succeeds()
        {
            const string newName = "RenamedDirectory";
            var contract = _fixture.TestDirectory;
            var directory = _fixture.TargetDirectory;

            _fixture.SetupGetChildItems(directory, _fixture.SubDirectoryItems);
            _fixture.SetupMove(contract, newName, directory);

            var sut = _fixture.GetDirectory(contract);
            sut.Move(_fixture.Drive, newName, new SAFEDirectoryNode(directory));

            _fixture.VerifyAll();
        }

        [TestMethod, TestCategory(nameof(TestCategories.Offline))]
        [ExpectedException(typeof(ArgumentNullException))]
        public void SAFEDirectoryNode_NewDirectoryItem_WhereDriveIsNull_Throws()
        {
            const string newName = "NewDirectory";
            var contract = _fixture.TestDirectory;

            _fixture.SetupGetChildItems(contract, _fixture.SubDirectoryItems);

            var sut = _fixture.GetDirectory(contract);
            sut.GetChildItems(_fixture.Drive);
            sut.NewDirectoryItem(null, newName);
        }

        [TestMethod, TestCategory(nameof(TestCategories.Offline))]
        public void SAFEDirectoryNode_NewDirectoryItem_Succeeds()
        {
            const string newName = "NewDirectory";
            var contract = _fixture.TestDirectory;

            _fixture.SetupGetChildItems(contract, _fixture.SubDirectoryItems);
            _fixture.SetupNewDirectoryItem(contract, newName);

            var sut = _fixture.GetDirectory(contract);
            sut.GetChildItems(_fixture.Drive);
            var result = sut.NewDirectoryItem(_fixture.Drive, newName);

            Assert.IsNotNull(result, "DirectoryNode not created");

            _fixture.VerifyAll();
        }

        [TestMethod, TestCategory(nameof(TestCategories.Offline))]
        [ExpectedException(typeof(ArgumentNullException))]
        public void SAFEDirectoryNode_NewFileItem_WhereDriveIsNull_Throws()
        {
            const string newName = "NewFile.ext";
            var contract = _fixture.TestDirectory;

            _fixture.SetupGetChildItems(contract, _fixture.SubDirectoryItems);

            var sut = _fixture.GetDirectory(contract);
            sut.GetChildItems(_fixture.Drive);
            sut.NewFileItem(null, newName);
        }

        [TestMethod, TestCategory(nameof(TestCategories.Offline))]
        public void SAFEDirectoryNode_NewFileItem_Succeeds()
        {
            const string newName = "NewFile.ext";
            var contract = _fixture.TestDirectory;

            _fixture.SetupGetChildItems(contract, _fixture.SubDirectoryItems);
            _fixture.SetupNewFileItem(contract, newName);

            var sut = _fixture.GetDirectory(contract);
            sut.GetChildItems(_fixture.Drive);
            var result = sut.NewFileItem(_fixture.Drive, newName);

            Assert.IsNotNull(result, "FileNode not created");

            _fixture.VerifyAll();
        }

        [TestMethod, TestCategory(nameof(TestCategories.Offline))]
        public void SAFEDirectoryNode_Remove_Succeeds()
        {
            var contract = _fixture.TestDirectory;

            _fixture.SetupRemove(contract);

            var sut = _fixture.GetDirectory(contract);
            sut.Remove(_fixture.Drive);

            _fixture.VerifyAll();
        }
    }
}
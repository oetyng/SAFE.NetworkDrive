using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace SAFE.NetworkDrive.Tests
{
    [TestClass]
    public sealed partial class SAFEItemNodeTests
    {
        Fixture _fixture;

        [TestInitialize]
        public void Initialize()
        {
            _fixture = Fixture.Initialize();
        }

        [TestMethod, TestCategory(nameof(TestCategories.Offline))]
        [ExpectedException(typeof(InvalidOperationException))]
        public void SAFEItemNode_CreateNew_WhereContractIsUnknownType_Throws()
        {
            var contract = _fixture.TestItem;

            SAFEItemNode.CreateNew(contract);
        }

        [TestMethod, TestCategory(nameof(TestCategories.Offline))]
        [ExpectedException(typeof(ArgumentNullException))]
        public void SAFEItemNode_Move_WhereDriveIsNull_Throws()
        {
            var contract = _fixture.TestItem;

            var sut = _fixture.GetItem(contract);
            sut.Move(null, "MovedItem", new SAFEDirectoryNode(_fixture.TargetDirectory));
        }

        [TestMethod, TestCategory(nameof(TestCategories.Offline))]
        [ExpectedException(typeof(ArgumentNullException))]
        public void SAFEItemNode_Move_WhereNewNameIsEmpty_Throws()
        {
            var contract = _fixture.TestItem;

            var sut = _fixture.GetItem(contract);
            sut.Move(_fixture.Drive, string.Empty, new SAFEDirectoryNode(_fixture.TargetDirectory));
        }

        [TestMethod, TestCategory(nameof(TestCategories.Offline))]
        [ExpectedException(typeof(ArgumentNullException))]
        public void SAFEItemNode_Move_WhereDestinationDirectoryIsNull_Throws()
        {
            var contract = _fixture.TestItem;

            var sut = _fixture.GetItem(contract);
            sut.Move(_fixture.Drive, "MovedItem", null);
        }

        [TestMethod, TestCategory(nameof(TestCategories.Offline))]
        [ExpectedException(typeof(InvalidOperationException))]
        public void SAFEItemNode_Move_WhereParentIsNull_Throws()
        {
            var contract = _fixture.TestItem;

            var sut = _fixture.GetItem(contract);
            sut.Move(_fixture.Drive, "MovedItem", new SAFEDirectoryNode(_fixture.TargetDirectory));
        }

        [TestMethod, TestCategory(nameof(TestCategories.Offline))]
        [ExpectedException(typeof(InvalidOperationException))]
        public void SAFEItemNode_ResolveContract_WhereCloudItemNodeIsResolved_Throws()
        {
            var contract = _fixture.TestFile;

            var sut = _fixture.GetItem(contract) as Fixture.TestCloudItemNode;
            sut.ResolveContract(contract);
        }

        [TestMethod, TestCategory(nameof(TestCategories.Offline))]
        [ExpectedException(typeof(InvalidOperationException))]
        public void SAFEItemNode_ResolveContract_WhereContractIsMismatched_Throws()
        {
            var proxyContract = _fixture.MismatchedProxyTestFile;
            var contract = _fixture.TestFile;

            var sut = _fixture.GetItem(proxyContract) as Fixture.TestCloudItemNode;
            sut.ResolveContract(contract);
        }

        [TestMethod, TestCategory(nameof(TestCategories.Offline))]
        [ExpectedException(typeof(ArgumentNullException))]
        public void SAFEItemNode_Remove_WhereDriveIsNull_Throws()
        {
            var contract = _fixture.TestItem;

            var sut = _fixture.GetItem(contract);
            sut.Remove(null);
        }
    }
}

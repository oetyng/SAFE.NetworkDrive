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
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace SAFE.NetworkDrive.Tests
{
    [TestClass]
    public sealed partial class CloudItemNodeTests
    {
        Fixture _fixture;

        [TestInitialize]
        public void Initialize()
        {
            _fixture = Fixture.Initialize();
        }

        [TestMethod, TestCategory(nameof(TestCategories.Offline))]
        [ExpectedException(typeof(InvalidOperationException))]
        public void CloudItemNode_CreateNew_WhereContractIsUnknownType_Throws()
        {
            var contract = _fixture.TestItem;

            CloudItemNode.CreateNew(contract);
        }

        [TestMethod, TestCategory(nameof(TestCategories.Offline))]
        [ExpectedException(typeof(ArgumentNullException))]
        public void CloudItemNode_Move_WhereDriveIsNull_Throws()
        {
            var contract = _fixture.TestItem;

            var sut = _fixture.GetItem(contract);
            sut.Move(null, "MovedItem", new CloudDirectoryNode(_fixture.TargetDirectory));
        }

        [TestMethod, TestCategory(nameof(TestCategories.Offline))]
        [ExpectedException(typeof(ArgumentNullException))]
        public void CloudItemNode_Move_WhereNewNameIsEmpty_Throws()
        {
            var contract = _fixture.TestItem;

            var sut = _fixture.GetItem(contract);
            sut.Move(_fixture.Drive, string.Empty, new CloudDirectoryNode(_fixture.TargetDirectory));
        }

        [TestMethod, TestCategory(nameof(TestCategories.Offline))]
        [ExpectedException(typeof(ArgumentNullException))]
        public void CloudItemNode_Move_WhereDestinationDirectoryIsNull_Throws()
        {
            var contract = _fixture.TestItem;

            var sut = _fixture.GetItem(contract);
            sut.Move(_fixture.Drive, "MovedItem", null);
        }

        [TestMethod, TestCategory(nameof(TestCategories.Offline))]
        [ExpectedException(typeof(InvalidOperationException))]
        public void CloudItemNode_Move_WhereParentIsNull_Throws()
        {
            var contract = _fixture.TestItem;

            var sut = _fixture.GetItem(contract);
            sut.Move(_fixture.Drive, "MovedItem", new CloudDirectoryNode(_fixture.TargetDirectory));
        }

        [TestMethod, TestCategory(nameof(TestCategories.Offline))]
        [ExpectedException(typeof(InvalidOperationException))]
        public void CloudItemNode_ResolveContract_WhereCloudItemNodeIsResolved_Throws()
        {
            var contract = _fixture.TestFile;

            var sut = _fixture.GetItem(contract) as Fixture.TestCloudItemNode;
            sut.ResolveContract(contract);
        }

        [TestMethod, TestCategory(nameof(TestCategories.Offline))]
        [ExpectedException(typeof(InvalidOperationException))]
        public void CloudItemNode_ResolveContract_WhereContractIsMismatched_Throws()
        {
            var proxyContract = _fixture.MismatchedProxyTestFile;
            var contract = _fixture.TestFile;

            var sut = _fixture.GetItem(proxyContract) as Fixture.TestCloudItemNode;
            sut.ResolveContract(contract);
        }

        [TestMethod, TestCategory(nameof(TestCategories.Offline))]
        [ExpectedException(typeof(ArgumentNullException))]
        public void CloudItemNode_Remove_WhereDriveIsNull_Throws()
        {
            var contract = _fixture.TestItem;

            var sut = _fixture.GetItem(contract);
            sut.Remove(null);
        }
    }
}

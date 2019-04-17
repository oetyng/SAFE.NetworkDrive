using System;
using Moq;
using SAFE.NetworkDrive.Interface;
using SAFE.Filesystem.Interface.IO;

namespace SAFE.NetworkDrive.Tests
{
    public sealed partial class SAFEItemNodeTests
    {
        internal class Fixture
        {
            internal sealed class TestCloudItemNode : SAFEItemNode
            {
                public TestCloudItemNode(FileSystemInfoContract contract) 
                    : base(contract)
                { }

                public new void ResolveContract(FileInfoContract contract)
                    => base.ResolveContract(contract);
            }

            sealed class TestFileSystemInfoContract : FileSystemInfoContract
            {
                public TestFileSystemInfoContract(string id, string name, DateTimeOffset created, DateTimeOffset updated)
                    : base(new TestFileSystemId(id), name, created, updated)
                { }

                public override string FullName => Name;
            }

            sealed class TestFileSystemId : FileSystemId
            {
                public TestFileSystemId(string id) : base(id)
                { }
            }

            Mock<ISAFEDrive> _drive;

            public ISAFEDrive Drive => _drive?.Object ?? (_drive = new Mock<ISAFEDrive>(MockBehavior.Strict)).Object;

            public readonly FileSystemInfoContract TestItem = new TestFileSystemInfoContract(@"\Item.ext", "Item.ext", "2015-12-31 10:11:12".ToDateTime(), "2015-12-31 20:21:22".ToDateTime());
            public readonly FileInfoContract TestFile = new FileInfoContract(@"\File.ext", "File.ext", "2015-01-02 10:11:12".ToDateTime(), "2015-01-02 20:21:22".ToDateTime(), new FileSize("16kB"), "16384".ToHash());
            public readonly ProxyFileInfoContract MismatchedProxyTestFile = new ProxyFileInfoContract("MismatchedFile.ext");
            public readonly DirectoryInfoContract TargetDirectory = new DirectoryInfoContract(@"\SubDir", "SubDir", "2015-01-01 10:11:12".ToDateTime(), "2015-01-01 20:21:22".ToDateTime());

            public static Fixture Initialize() => new Fixture();

            public SAFEItemNode GetItem(FileSystemInfoContract contract)
                => new TestCloudItemNode(contract);
        }
    }
}
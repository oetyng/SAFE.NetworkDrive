using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Moq;
using SAFE.NetworkDrive.Interface;
using SAFE.Filesystem.Interface.IO;

namespace SAFE.NetworkDrive.Tests
{
    public sealed partial class SAFEFileNodeTests
    {
        internal class Fixture
        {
            const string _mountPoint = "Z:";
            const long _freeSpace = 64 * 1 << 20;
            const long _usedSpace = 36 * 1 << 20;

            readonly Mock<ISAFEDrive> _drive;
            readonly SAFEDirectoryNode _root;

            public ISAFEDrive Drive => _drive.Object;
            public readonly FileInfoContract TestFile = new FileInfoContract(@"\File.ext", "File.ext", "2015-01-02 10:11:12".ToDateTime(), "2015-01-02 20:21:22".ToDateTime(), new FileSize("16kB"), "16384".ToHash());
            public readonly ProxyFileInfoContract ProxyTestFile = new ProxyFileInfoContract("File.ext");
            public readonly DirectoryInfoContract ProxyParentDirectory = new DirectoryInfoContract(@"\Dir", "Dir", "2016-01-01 10:11:12".ToDateTime(), "2016-01-01 20:21:22".ToDateTime());
            public readonly DirectoryInfoContract TargetDirectory = new DirectoryInfoContract(@"\SubDir", "SubDir", "2015-01-01 10:11:12".ToDateTime(), "2015-01-01 20:21:22".ToDateTime());

            public FileSystemInfoContract[] SubDirectoryItems { get; } = new FileSystemInfoContract[] {
                new DirectoryInfoContract(@"\SubDir\SubSubDir", "SubSubDir", "2015-02-01 10:11:12".ToDateTime(), "2015-02-01 20:21:22".ToDateTime()),
                new FileInfoContract(@"\SubDir\SubFile.ext", "SubFile.ext", "2015-02-02 10:11:12".ToDateTime(), "2015-02-02 20:21:22".ToDateTime(), (FileSize)981256915, "981256915".ToHash()),
                new FileInfoContract(@"\SubDir\SecondSubFile.ext", "SecondSubFile.ext", "2015-02-03 10:11:12".ToDateTime(), "2015-02-03 20:21:22".ToDateTime(), (FileSize)30858025, "30858025".ToHash()),
                new FileInfoContract(@"\SubDir\ThirdSubFile.ext", "ThirdSubFile.ext", "2015-02-04 10:11:12".ToDateTime(), "2015-02-04 20:21:22".ToDateTime(), (FileSize)45357, "45357".ToHash())
            };

            public static Fixture Initialize() => new Fixture();

            Fixture()
            {
                _drive = new Mock<ISAFEDrive>(MockBehavior.Strict);
                _root = new SAFEDirectoryNode(new RootDirectoryInfoContract(Path.DirectorySeparatorChar.ToString(), "2015-01-01 00:00:00".ToDateTime(), "2015-01-01 00:00:00".ToDateTime()) {
                    Drive = new DriveInfoContract(_mountPoint, _freeSpace, _usedSpace)
                }) { children = new Dictionary<string, SAFEItemNode>() };
            }

            public SAFEFileNode GetFile(FileInfoContract contract, DirectoryInfoContract parent = null)
            {
                var result = new SAFEFileNode(contract);
                result.SetParent(parent != null ? new SAFEDirectoryNode(parent) : _root);
                return result;
            }

            public void SetupGetContent(FileInfoContract file, string content)
            {
                _drive
                    .Setup(d => d.GetContent(file))
                    .Returns(new MemoryStream(Encoding.Default.GetBytes(content)));
            }

            public void SetupGetChildItems(DirectoryInfoContract parent, IEnumerable<FileSystemInfoContract> childItems)
            {
                _drive
                    .Setup(d => d.GetChildItem(parent))
                    .Returns(childItems);
            }

            public void SetupMove(FileInfoContract source, string movePath, DirectoryInfoContract destination)
            {
                var newName = !string.IsNullOrEmpty(movePath) ? movePath : source.Name;
                _drive
                    .Setup(d => d.MoveItem(source, movePath, destination))
                    .Returns(new FileInfoContract(destination.Id.Value + Path.DirectorySeparatorChar + newName, newName, source.Created, source.Updated, source.Size, source.Hash));
            }

            public void SetupNewFileItem(DirectoryInfoContract parent, string name, byte[] content)
            {
                _drive
                    .Setup(d => d.NewFileItem(It.Is<DirectoryInfoContract>(c => c.Id == parent.Id), name, It.Is<Stream>(s => s.Contains(content))))
                    .Returns((DirectoryInfoContract p, string n, Stream s) => new FileInfoContract($"{p.Id}\\{n}", n, DateTimeOffset.Now, DateTimeOffset.Now, (FileSize)s.Length, null));
            }

            public void SetupRemove(FileInfoContract target)
                => _drive
                     .Setup(d => d.RemoveItem(target, false));

            public void SetupSetContent(FileInfoContract file, byte[] content)
                => _drive
                        .Setup(d => d.SetContent(file, It.Is<Stream>(s => s.Contains(content))));

            public void SetupTruncate(FileInfoContract file)
                => _drive
                    .Setup(d => d.SetContent(file, It.Is<Stream>(m => m.Length == 0)));

            public void VerifyAll() => _drive.VerifyAll();
        }
    }
}
using System;
using System.Collections.Generic;
using System.IO;
using Moq;
using SAFE.NetworkDrive.Interface;
using SAFE.Filesystem.Interface.IO;

namespace SAFE.NetworkDrive.Tests
{
    public sealed partial class SAFEDirectoryNodeTests
    {
        internal class Fixture
        {
            const string _mountPoint = "Z:";
            const long _freeSpace = 64 * 1 << 20;
            const long _usedSpace = 36 * 1 << 20;

            readonly Mock<ISAFEDrive> _drive;
            readonly SAFEDirectoryNode _root;

            public ISAFEDrive Drive => _drive.Object;
            public readonly DirectoryInfoContract TestDirectory = new DirectoryInfoContract(@"\Dir", "Dir", "2015-01-02 20:11:12".ToDateTime(), "2015-01-02 20:21:22".ToDateTime());
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

            public SAFEDirectoryNode GetDirectory(DirectoryInfoContract contract)
            {
                var result = new SAFEDirectoryNode(contract);
                result.SetParent(_root);
                return result;
            }

            public void SetupGetChildItems(DirectoryInfoContract parent, IEnumerable<FileSystemInfoContract> childItems)
            {
                _drive
                    .Setup(d => d.GetChildItem(parent))
                    .Returns(childItems);
            }

            public void SetupMove(DirectoryInfoContract source, string movePath, DirectoryInfoContract destination)
            {
                var newName = !string.IsNullOrEmpty(movePath) ? movePath : source.Name;
                _drive
                    .Setup(d => d.MoveItem(source, movePath, destination))
                    .Returns(new DirectoryInfoContract(destination.Id.Value + Path.DirectorySeparatorChar + newName, newName, source.Created, source.Updated));
            }

            public void SetupNewDirectoryItem(DirectoryInfoContract parent, string directoryName)
            {
                _drive
                    .Setup(d => d.NewDirectoryItem(parent, directoryName))
                    .Returns(new DirectoryInfoContract(parent.Id + Path.DirectorySeparatorChar.ToString() + directoryName, directoryName, DateTimeOffset.Now, DateTimeOffset.Now));
            }

            public void SetupNewFileItem(DirectoryInfoContract parent, string fileName)
            {
                _drive
                    .Setup(d => d.NewFileItem(parent, fileName, It.Is<Stream>(s => s.Length == 0)))
                    .Returns(new FileInfoContract(parent.Id + Path.DirectorySeparatorChar.ToString() + fileName, fileName, DateTimeOffset.Now, DateTimeOffset.Now, FileSize.Empty, string.Empty.ToHash()));
            }

            public void SetupRemove(DirectoryInfoContract target)
                => _drive.Setup(d => d.RemoveItem(target, false));

            public void VerifyAll() => _drive.VerifyAll();
        }
    }
}
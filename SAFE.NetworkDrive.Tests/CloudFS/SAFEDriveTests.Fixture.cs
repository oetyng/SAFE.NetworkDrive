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
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using Moq;
using SAFE.NetworkDrive.Interface;
using SAFE.NetworkDrive.Interface.IO;
using SAFE.NetworkDrive.IO;
using SAFE.Filesystem.Interface.IO;

namespace SAFE.NetworkDrive.Tests
{
    public sealed partial class SAFEDriveTests
    {
        internal class Fixture
        {
            public const string MOUNT_POINT = "Z";
            public const string SCHEMA = "mock";
            public const string VOLUME_ID = "00000000000000000000000000000000";
            public const long FREE_SPACE = 64 * 1 << 20;
            public const long USED_SPACE = 36 * 1 << 20;

            readonly Mock<ISAFEGateway> _gateway;
            readonly RootDirectoryInfoContract _root;
            readonly RootName _rootName = new RootName(SCHEMA, VOLUME_ID, MOUNT_POINT);

            public ISAFEGateway Gateway => _gateway.Object;
            public readonly DirectoryInfoContract TargetDirectory = new DirectoryInfoContract(@"\SubDir", "SubDir", "2015-01-01 10:11:12".ToDateTime(), "2015-01-01 20:21:22".ToDateTime());

            public FileSystemInfoContract[] RootDirectoryItems { get; } = new FileSystemInfoContract[] {
                new DirectoryInfoContract(@"\SubDir", "SubDir", "2015-01-01 10:11:12".ToDateTime(), "2015-01-01 20:21:22".ToDateTime()),
                new DirectoryInfoContract(@"\SubDir2", "SubDir2", "2015-01-01 13:14:15".ToDateTime(), "2015-01-01 23:24:25".ToDateTime()),
                new FileInfoContract(@"\File.ext", "File.ext", "2015-01-02 10:11:12".ToDateTime(), "2015-01-02 20:21:22".ToDateTime(), new FileSize("16kB"), "16384".ToHash()),
                new FileInfoContract(@"\SecondFile.ext", "SecondFile.ext", "2015-01-03 10:11:12".ToDateTime(), "2015-01-03 20:21:22".ToDateTime(), new FileSize("32kB"), "32768".ToHash()),
                new FileInfoContract(@"\ThirdFile.ext", "ThirdFile.ext", "2015-01-04 10:11:12".ToDateTime(), "2015-01-04 20:21:22".ToDateTime(), new FileSize("64kB"), "65536".ToHash())
            };

            public static Fixture Initialize() => new Fixture();

            private Fixture()
            {
                _gateway = new Mock<ISAFEGateway>(MockBehavior.Strict);
                _root = new RootDirectoryInfoContract(Path.DirectorySeparatorChar.ToString(), "2015-01-01 00:00:00".ToDateTime(), "2015-01-01 00:00:00".ToDateTime()) {
                    Drive = new DriveInfoContract(MOUNT_POINT, FREE_SPACE, USED_SPACE)
                };
            }

            public SAFEDrive Create()
                => new SAFEDrive(new RootName(SCHEMA, VOLUME_ID, MOUNT_POINT), _gateway.Object);

            public void SetupGetDrive()
            {
                _gateway
                    .Setup(g => g.GetDrive())
                    .Returns(_root.Drive);
            }

            public void SetupGetDriveThrows<TException>()
                where TException : Exception, new()
            {
                _gateway
                    .Setup(g => g.GetDrive())
                    .Throws(new AggregateException(Activator.CreateInstance<TException>()));
            }

            public void SetupGetRoot()
            {
                _gateway
                    .Setup(g => g.GetRoot())
                    .Returns(_root);
            }

            public void SetupGetRootDirectoryItems(string encryptionKey = null)
            {
                _gateway
                    .Setup(g => g.GetChildItem(new DirectoryId(Path.DirectorySeparatorChar.ToString())))
                    .Returns(RootDirectoryItems);

                if (!string.IsNullOrEmpty(encryptionKey))
                    foreach (var fileInfo in RootDirectoryItems.OfType<FileInfoContract>())
                        using (var rawStream = new MemoryStream(Enumerable.Repeat<byte>(0, (int)fileInfo.Size).ToArray()))
                            _gateway
                                .SetupSequence(g => g.GetContent(fileInfo.Id))
                                .Returns(rawStream.EncryptOrPass(encryptionKey));
            }

            public void SetupGetContent(FileInfoContract source, byte[] content, string encryptionKey = null, bool canSeek = true)
            {
                // The first constructor does not expose the underlying stream. GetBuffer throws UnauthorizedAccessException.
                var stream = new MemoryStream(content, 0, content.Length, false, publiclyVisible: true); // publiclyVisible: true to enable GetBuffer(), which returns the unsigned byte array from which the stream was created; otherwise, false.
                if (!canSeek)
                    stream = new LinearReadMemoryStream(stream);
                _gateway
                    .Setup(g => g.GetContent(source.Id))
                    .Returns(stream);
            }

            public void SetupSetContent(FileInfoContract target, byte[] content)
            {
                Func<Stream, bool> checkContent = stream => stream.Contains(content);
                _gateway
                    .Setup(g => g.SetContent(target.Id, It.Is<Stream>(s => checkContent(s)), It.IsAny<IProgress<ProgressValue>>()))
                    .Returns(true);
            }

            public void SetupMoveDirectoryOrFile(FileSystemInfoContract directoryOrFile, DirectoryInfoContract target)
                => SetupMoveItem(directoryOrFile, directoryOrFile.Name, target);

            public void SetupRenameDirectoryOrFile(FileSystemInfoContract directoryOrFile, string name)
                => SetupMoveItem(directoryOrFile, name, (directoryOrFile as DirectoryInfoContract)?.Parent ?? (directoryOrFile as FileInfoContract)?.Directory ?? null);

            void SetupMoveItem(FileSystemInfoContract directoryOrFile, string name, DirectoryInfoContract target)
            {
                _gateway
                    .Setup(g => g.MoveItem(directoryOrFile.Id, name, target.Id))
                    .Returns((RootName _rootName, FileSystemId source, string movePath, DirectoryId destination, Func<FileSystemInfoLocator> resolver) => {
                        var directorySource = source as DirectoryId;
                        if (directorySource != null)
                            return new DirectoryInfoContract(source.Value, movePath, directoryOrFile.Created, directoryOrFile.Updated) { Parent = target };
                        var fileSource = source as FileId;
                        if (fileSource != null)
                            return new FileInfoContract(source.Value, movePath, directoryOrFile.Created, directoryOrFile.Updated, ((FileInfoContract)directoryOrFile).Size, ((FileInfoContract)directoryOrFile).Hash) { Directory = target };
                        throw new InvalidOperationException($"Unsupported type '{source.GetType().Name}'".ToString(CultureInfo.CurrentCulture));
                    });
            }

            public void SetupNewDirectoryItem(DirectoryInfoContract parent, string directoryName)
            {
                _gateway
                    .Setup(g => g.NewDirectoryItem(parent.Id, directoryName))
                    .Returns(new DirectoryInfoContract(parent.Id + Path.DirectorySeparatorChar.ToString() + directoryName, directoryName, DateTimeOffset.Now, DateTimeOffset.Now));
            }

            public void SetupNewFileItem(DirectoryInfoContract parent, string fileName, byte[] content)
            {
                Func<Stream, bool> checkContent = stream => stream.Contains(content);
                _gateway
                    .Setup(g => g.NewFileItem(parent.Id, fileName, It.Is<Stream>(s => checkContent(s)), It.IsAny<IProgress<ProgressValue>>()))
                    .Returns(new FileInfoContract(parent.Id + Path.DirectorySeparatorChar.ToString() + fileName, fileName, DateTimeOffset.Now, DateTimeOffset.Now, (FileSize)content.Length, Encoding.Default.GetString(content).ToHash()));
            }

            public void SetupRemoveDirectoryOrFile(FileSystemInfoContract directoryOrFile, bool recurse)
                => _gateway
                    .Setup(g => g.RemoveItem(directoryOrFile.Id, recurse))
                    .Returns(true);

            public void VerifyAll() => _gateway.VerifyAll();
        }
    }
}
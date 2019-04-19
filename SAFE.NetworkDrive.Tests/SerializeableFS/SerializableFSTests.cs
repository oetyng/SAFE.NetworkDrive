using Microsoft.VisualStudio.TestTools.UnitTesting;
using SAFE.NetworkDrive.SerializableFS;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace SAFE.NetworkDrive.Tests
{
    [TestClass]
    public sealed partial class SerializableFSTests
    {
        Fixture _fixture;

        [ClassInitialize]
        public static void ClassInitialize(TestContext testContext) { }

        [TestInitialize]
        public void Initialize() => _fixture = Fixture.Initialize();

        [TestMethod]
        public void SerializableFS_BuildFileSystem_Succeeds()
        {
            var maxLevels = 8;
            var maxFoldersPerLevel = 30;
            var maxFilesPerFolder = 8;

            Parallel.ForEach(Enumerable.Range(1, maxLevels), levels =>
                Parallel.ForEach(Enumerable.Range(1, maxFoldersPerLevel), foldersPerLevel =>
                    Parallel.ForEach(Enumerable.Range(1, maxFilesPerFolder), filesPerFolder =>
                        BuildFileSystem(levels, foldersPerLevel, filesPerFolder))));
        }

        MemoryFS.MemoryFolder BuildFileSystem(int levels, int foldersPerLevel, int filesPerFolder)
        {
            // Arrange
            var fileCount = levels * foldersPerLevel * filesPerFolder;
            var folderCount = 2 + (levels * foldersPerLevel) + Math.Max(0, levels - foldersPerLevel);

            // Act
            var root = _fixture.BuildFileSystem(levels, foldersPerLevel, filesPerFolder);

            // Assert
            var files = _fixture.GetAllFiles(root).ToList();
            var folders = _fixture.GetAllFolders(root).ToList();
            Assert.AreEqual(folderCount, folders.Count, $"Folder fail at levels: {levels}, foldersPerLevel: {foldersPerLevel}, filesPerFolder: {filesPerFolder}");
            Assert.AreEqual(fileCount, files.Count, $"File fail at levels: {levels}, foldersPerLevel: {foldersPerLevel}, filesPerFolder: {filesPerFolder}");

            return root;
        }

        [TestMethod]
        public void SerializableFS_SerializeFileSystem_Succeeds()
        {
            var maxLevels = 3;
            var maxFoldersPerLevel = 8;
            var maxFilesPerFolder = 3;

            //SerializeFileSystem(BuildFileSystem(maxLevels, maxFoldersPerLevel, maxFilesPerFolder));

            Parallel.ForEach(Enumerable.Range(1, maxLevels), levels =>
                Parallel.ForEach(Enumerable.Range(1, maxFoldersPerLevel), foldersPerLevel =>
                    Parallel.ForEach(Enumerable.Range(1, maxFilesPerFolder), filesPerFolder =>
                        SerializeFileSystem(BuildFileSystem(levels, foldersPerLevel, filesPerFolder)))));

            //foreach (var levels in Enumerable.Range(1, maxLevels))
            //    foreach (var foldersPerLevel in Enumerable.Range(1, maxFoldersPerLevel))
            //        foreach (var filesPerFolder in Enumerable.Range(1, maxFilesPerFolder))
            //            SerializeFileSystem(BuildFileSystem(levels, foldersPerLevel, filesPerFolder));
        }

        MemoryFS.MemoryFolder SerializeFileSystem(MemoryFS.MemoryFolder originalRoot)
        {
            var serializable = FSSerializer.Map(originalRoot);
            var serialized = new SerializedFS(serializable);
            // var compressed = serialized.Data.Compress(); // ~10x compression
            var deserialized = serialized.Deserialize();
            var newRoot = FSSerializer.Map(deserialized);

            var originalFiles = _fixture.GetAllFiles(originalRoot).ToList();
            var originalFolders = _fixture.GetAllFolders(originalRoot).ToList();
            var newFiles = _fixture.GetAllFiles(newRoot).ToList();
            var newFolders = _fixture.GetAllFolders(newRoot).ToList();

            Assert.AreEqual(originalFiles.Count, newFiles.Count, $"File count failed.");
            Assert.AreEqual(originalFolders.Count, newFolders.Count, $"Folder count failed.");

            Parallel.ForEach(originalFiles, original =>
            {
                var newfile = newFiles.Single(c => c.FullName == original.FullName);
                Assert.AreEqual(original.Name, newfile.Name);
                Assert.AreEqual(original.FullName, newfile.FullName);
                Assert.AreEqual(original.Size, newfile.Size);
                Assert.AreEqual(original.Parent?.FullName, original.Parent?.FullName);

                Assert.AreEqual(original.Attributes, newfile.Attributes);
                Assert.AreEqual(original.LastAccessTime, newfile.LastAccessTime);
                Assert.AreEqual(original.LastWriteTime, newfile.LastWriteTime);
                Assert.AreEqual(original.CreationTime, newfile.CreationTime);

                var originalData = new byte[original.Size];
                original.Read(0, originalData);

                var newData = new byte[newfile.Size];
                newfile.Read(0, newData);

                Assert.IsTrue(Enumerable.SequenceEqual(originalData, newData));
            });

            Parallel.ForEach(originalFolders, original =>
            {
                var newFolder = newFolders.Single(c => c.FullName == original.FullName);
                Assert.AreEqual(original.Name, newFolder.Name);
                Assert.AreEqual(original.FullName, newFolder.FullName);
                Assert.AreEqual(original.Parent?.FullName, original.Parent?.FullName);

                Assert.AreEqual(original.Attributes, newFolder.Attributes);
                Assert.AreEqual(original.LastAccessTime, newFolder.LastAccessTime);
                Assert.AreEqual(original.LastWriteTime, newFolder.LastWriteTime);
                Assert.AreEqual(original.CreationTime, newFolder.CreationTime);
            });

            return newRoot;
        }
    }
}
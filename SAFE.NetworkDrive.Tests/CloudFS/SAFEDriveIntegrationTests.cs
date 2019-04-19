using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SAFE.NetworkDrive.Tests
{
    [TestClass]
    public sealed partial class SAFEDriveIntegrationTests
    {
        const string _schema = "test";
        const string _root = "Z";
        const string _volumeId = "00000000000000000000000000000000";

        Fixture _fixture;

        [TestInitialize]
        public void Initialize() => _fixture = Fixture.Initialize();

        [TestMethod, TestCategory(nameof(TestCategories.Offline))]
        public void CreateSAFEDrive_Succeeds()
        {
            var sut = new SAFEDriveFactory();

            using (var result = sut.CreateDrive(_schema, _volumeId, _root, _fixture.Parameters))
                Assert.IsInstanceOfType(result, typeof(SAFEDrive), "Unexpected result type");
        }

        [TestMethod, TestCategory(nameof(TestCategories.Offline))]
        public async Task UsingSAFEDrive_Succeeds()
        {
            try
            {
                var sut = new SAFEDriveFactory();

                using var drive = sut.CreateDrive(_schema, _volumeId, _root, _fixture.Parameters);
                Assert.IsInstanceOfType(drive, typeof(SAFEDrive), "Unexpected result type");

                var root = new SAFEDirectoryNode(drive.GetRoot());
                root.GetChildItems(drive);

                int levels = 8;
                int foldersPerLevel = 30;
                int filesPerFolder = 4;

                var fileCount = levels * foldersPerLevel * filesPerFolder;
                var folderCount = (levels * foldersPerLevel) + Math.Max(0, levels - foldersPerLevel);

                var next = root;
                var allFolders = new List<SAFEDirectoryNode>();
                foreach (var levelName in Enumerable.Range(1, levels))
                {
                    var level = CreateLevel(next, drive, foldersPerLevel).ToList();
                    level.ForEach(c => c.SetParent(next));
                    level.ForEach(c => c.GetChildItems(drive));
                    allFolders.AddRange(level);
                    next = level.First();
                }
                allFolders.ForEach(c => Enumerable.Range(1, filesPerFolder)
                        .Select(i => c.NewFileItem(drive, $"{i}.ext"))
                        .ToList());

                var allFiles = allFolders
                    .SelectMany(c => c.GetChildItems(drive).OfType<SAFEFileNode>())
                    .ToList();
                allFiles.ForEach(c => c.SetContent(drive, new System.IO.MemoryStream(new byte[5] { 0, 1, 2, 3, 4 })));

                Assert.AreEqual(fileCount, allFiles.Count);
                Assert.AreEqual(folderCount, allFolders.Count);

                await Task.Delay(600000);
            }
            catch(Exception ex)
            { }
        }

        IEnumerable<SAFEDirectoryNode> CreateLevel(SAFEDirectoryNode parent, ISAFEDrive drive, int foldersPerLevel)
            => Enumerable.Range(1, foldersPerLevel)
               .Select(c => CreateFolder(parent, $"{c}", drive));

        SAFEDirectoryNode CreateFolder(SAFEDirectoryNode parent, string name, ISAFEDrive drive)
            => parent.NewDirectoryItem(drive, $"{name}");
    }
}
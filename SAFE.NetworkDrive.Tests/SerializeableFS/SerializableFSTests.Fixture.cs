
using SAFE.NetworkDrive.MemoryFS;
using System.Collections.Generic;
using System.Linq;

namespace SAFE.NetworkDrive.Tests
{
    public sealed partial class SerializableFSTests
    {
        internal class Fixture
        {
            public static Fixture Initialize() => new Fixture();

            Fixture()
            { }

            public MemoryFolder BuildFileSystem(int levels, int foldersPerLevel, int filesPerFolder)
            {
                var data = new byte[5] { 0, 1, 2, 3, 4 };
                var root = new MemoryFolder(null, "", TimeComponent.Now);
                var paths = Enumerable.Range(1, levels)
                    //.AsParallel()
                    .Select(c => GetPath(c))
                    .SelectMany(c => Enumerable.Range(0, foldersPerLevel)
                        .Select(i => $"{c}\\{i}"))
                    .ToList();
                paths.ForEach(c => root.CreatePath(c, TimeComponent.Now));
                var files = paths
                    //.AsParallel()
                    .Select(c => root.GetFolderByPath(c))
                    .SelectMany(c => Enumerable.Range(0, filesPerFolder)
                        .Select(f => MemoryFile.New(c, $"{f}.ext", TimeComponent.Now)))
                    .Select(c => c.Write(0, data))
                    .ToList();
                return root;
            }

            string GetPath(int level)
                => "\\" + string.Join("\\", Enumerable.Range(0, level));


            public IEnumerable<MemoryStreamFile> GetAllFiles(MemoryFolder folder)
                => GetAllFolders(folder).SelectMany(c => c.EnumerateFiles().Cast<MemoryStreamFile>());

            public IEnumerable<MemoryFolder> GetAllFolders(MemoryFolder folder)
                => folder.EnumerateDirectories().SelectMany(c => GetAllFolders(c)).Concat(new [] { folder });
        }
    }
}
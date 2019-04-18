using SAFE.Data.Utils;
using SAFE.NetworkDrive.MemoryFS;
using System.Linq;

namespace SAFE.NetworkDrive.SerializableFS
{
    public class SerializedFS
    {
        public SerializedFS(SerializableFolder root)
            => Data = root.GetBytes();

        public SerializedFS(byte[] data)
            => Data = data;

        public byte[] Data { get; }

        public SerializableFolder Deserialize() => Data.Parse<SerializableFolder>();
    }

    internal class FSSerializer
    {
        internal static SerializableFolder Map(MemoryFolder root)
        {
            var mapped = new SerializableFolder(null, root.Name, root.TimeComponent);
            var subDirectories = root.Children.OfType<MemoryFolder>()
                .AsParallel()
                .Select(c => Map(mapped, c))
                .ToList();
            var files = root.Children.OfType<MemoryStreamFile>()
                .AsParallel()
                .Select(c => Map(mapped, c))
                .ToList();

            mapped.SubDirectories = subDirectories;
            mapped.Files = files;

            return mapped;
        }

        static SerializableFolder Map(SerializableFolder parent, MemoryFolder folder)
        {
            var mapped = new SerializableFolder(parent, folder.Name, folder.TimeComponent);
            var subDirectories = folder.Children.OfType<MemoryFolder>()
                .AsParallel()
                .Select(c => Map(mapped, c))
                .ToList();
            var files = folder.Children.OfType<MemoryStreamFile>()
                .AsParallel()
                .Select(c => Map(mapped, c))
                .ToList();

            mapped.SubDirectories = subDirectories;
            mapped.Files = files;

            return mapped;
        }

        static SerializableFile Map(SerializableFolder parent, MemoryStreamFile file)
        {
            var mapped = new SerializableFile(parent, file.Name, file.TimeComponent);
            mapped.ContentOrMap = new byte[file.Size];
            file.Read(0, mapped.ContentOrMap);
            return mapped;
        }

        // ----------------------------------------------------------------

        internal static MemoryFolder Map(SerializableFolder root)
        {
            var mapped = new MemoryFolder(null, root.Name, root.TimeComponent);
            var subDirectories = root.SubDirectories
                //.AsParallel()
                .Select(c => Map(mapped, c))
                .ToList();
            var files = root.Files
                //.AsParallel()
                .Select(c => Map(mapped, c))
                .ToList();

            mapped.Children = subDirectories.Cast<MemoryItem>().ToList();
            mapped.Children.AddRange(files);

            return mapped;
        }

        static MemoryFolder Map(MemoryFolder parent, SerializableFolder folder)
        {
            var mapped = new MemoryFolder(parent, folder.Name, folder.TimeComponent);
            var subDirectories = folder.SubDirectories
                //.AsParallel()
                .Select(c => Map(mapped, c))
                .ToList();
            var files = folder.Files
                //.AsParallel()
                .Select(c => Map(mapped, c))
                .ToList();

            mapped.Children = subDirectories.Cast<MemoryItem>().ToList();
            mapped.Children.AddRange(files);

            return mapped;
        }

        static MemoryStreamFile Map(MemoryFolder parent, SerializableFile file)
        {
            var mapped = new MemoryStreamFile(parent, file.Name, file.TimeComponent);
            mapped.Write(0, file.ContentOrMap);
            return mapped;
        }
    }
}
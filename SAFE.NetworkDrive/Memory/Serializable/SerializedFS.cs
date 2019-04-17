﻿using SAFE.Data.Utils;
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
        internal static SerializedFS SerializeFS(MemoryFolder root)
        {
            var mapped = new SerializableFolder(null, root.Name);
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

            return new SerializedFS(mapped);
        }

        static SerializableFolder Map(SerializableFolder parent, MemoryFolder folder)
        {
            var mapped = new SerializableFolder(parent, folder.Name);
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
            var mapped = new SerializableFile(parent, file.Name);
            mapped.ContentOrMap = new byte[file.Size];
            file.Read(0, mapped.ContentOrMap);
            return mapped;
        }
    }
}
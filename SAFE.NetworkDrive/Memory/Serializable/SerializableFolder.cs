using Newtonsoft.Json;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace SAFE.NetworkDrive.SerializableFS
{
    public class SerializableFolder : SerializableItem
    {
        [JsonConstructor]
        SerializableFolder() { }

        public SerializableFolder(SerializableFolder parent, string name, TimeComponent time)
            : base(parent, name, time)
            => Attributes = FileAttributes.Directory;

        public List<SerializableFolder> SubDirectories { get; set; } = new List<SerializableFolder>();
        public List<SerializableFile> Files { get; set; } = new List<SerializableFile>();

        /// <summary>
        /// The size of the files and the files in any subfolders
        /// </summary>
        [JsonIgnore]
        public ulong UsedSize
        {
            get
            {
                // the total size of the files in this folder;
                var fileSize = Files.Sum(c => (decimal)c.Size);
                // plus size of subfolders;
                var dirSize = SubDirectories.Sum(c => (decimal)c.UsedSize);

                return (ulong)(fileSize + dirSize);
            }
        }
    }
}
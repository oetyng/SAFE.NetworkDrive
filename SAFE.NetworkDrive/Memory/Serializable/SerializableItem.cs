using Newtonsoft.Json;
using SAFE.Data.Utils;
using System;
using System.IO;

namespace SAFE.NetworkDrive.SerializableFS
{
    /// <summary>
    /// Base class;
    /// Every folder or file in memory gets derived from this thing
    /// </summary>
    public abstract class SerializableItem
    {
        [JsonConstructor]
        protected SerializableItem() { }

        protected SerializableItem(SerializableFolder parent, string name)
        {
            ParentName = parent.Name;
            FullName = parent.FullName + "\\" + name;
            Name = name;

            CreationTime = DateTime.Now;
            LastAccessTime = DateTime.Now;
            LastWriteTime = DateTime.Now;
        }

        /// <summary>
        /// This is a reference to the parent-folder, where this item is located in
        /// </summary>
        public string ParentName { get; set; }

        /// <summary>
        /// This is the name of the item (file or folder), without a path
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// The represent the attributes of the item;
        /// flags indicating whether it's a readonly/hidden
        /// </summary>
        public FileAttributes Attributes { get; set; }

        //
        // These represent the filedates;        
        public DateTime LastAccessTime { get; set; }
        public DateTime LastWriteTime { get; set; }
        public DateTime CreationTime { get; set; }

        /// <summary>
        /// Returns the full path to the memory-item
        /// </summary>
        public string FullName { get; set; }

        public T Deserialize<T>(string data, string type) where T : SerializableItem
            => (T)data.Parse(type);
    }
}
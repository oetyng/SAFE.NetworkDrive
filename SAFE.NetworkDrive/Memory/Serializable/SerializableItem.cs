using Newtonsoft.Json;
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

        protected SerializableItem(SerializableFolder parent, string name, TimeComponent time)
        {
            ParentFullName = parent?.FullName;
            Name = name ?? throw new ArgumentNullException(nameof(name));
            TimeComponent = time;
        }

        /// <summary>
        /// This is the full path of the parent-folder, where this item is located in
        /// </summary>
        public string ParentFullName { get; set; }

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
        public TimeComponent TimeComponent { get; set; }

        /// <summary>
        /// The full path to the memory-item
        /// </summary>
        [JsonIgnore]
        public string FullName => Name == string.Empty ? Name : ParentFullName + "\\" + Name;
    }
}
using System;
using System.IO;
using System.Linq;

namespace SAFE.NetworkDrive.Gateways.Memory
{
    /// <summary>
    /// Base class;
    /// Every folder or file in memory gets derived from this thing
    /// </summary>
    class MemoryItem
    {
        MemoryFolder _parent;

        MemoryItem() => throw new NotSupportedException();

        protected MemoryItem(MemoryFolder parent, string name)
        {
            Parent = parent;
            Name = name;

            CreationTime = DateTime.Now;
            LastAccessTime = DateTime.Now;
            LastWriteTime = DateTime.Now;
        }

        /// <summary>
        /// This is a reference to the parent-folder, where this item is located in
        /// </summary>
        public MemoryFolder Parent
        {
            get => _parent;
            set
            {
                if (_parent != value)
                {
                    if (_parent != null) _parent.Children.Remove(this);
                    if (value != null) value.Children.Add(this);

                    _parent = value;
                }
            }
        }

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
        public string FullName
        {
            get
            {
                if (_parent == null)
                    return Name;
                else
                    return _parent.FullName + "\\" + Name;
            }
        }

        public void MoveTo(MemoryFolder newParent, string moveName)
        {
            // EVALUATE: is throwing here the correct behaviour?
            if (newParent.Children.Any(c => c.Name == moveName))
                throw new IOException($"The folder {newParent.Name} already has an item with this name: {moveName}");
            
            // set the new parent
            Parent = newParent;
            // rename the actual item
            Name = moveName;
        }
    }
}
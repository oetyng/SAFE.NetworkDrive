
using System;

namespace SAFE.NetworkDrive.MemoryFS
{
    internal abstract class MemoryFile : MemoryItem
    {
    	protected MemoryFile(MemoryFolder parent, string name, TimeComponent time)
    		: base(parent, name, time)
    	{ }
    	
        internal abstract long Size { get; set; }
        internal abstract uint Write(long offset, byte[] buffer);
        internal abstract uint Read(long offset, byte[] buffer);
        internal abstract System.IO.Stream OpenRead();
        internal abstract void Clear();
        internal abstract void SetContent(System.IO.Stream content, DateTime timestamp);
        internal abstract MemoryFile CopyTo(MemoryFolder parent, string copyName, DateTime timestamp);

        internal static MemoryFile New(MemoryFolder parent, string name, TimeComponent time)
            => new MemoryStreamFile(parent, name, time);
    }
}
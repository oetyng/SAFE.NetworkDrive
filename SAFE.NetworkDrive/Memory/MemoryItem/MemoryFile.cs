
namespace SAFE.NetworkDrive.MemoryFS
{
    internal abstract class MemoryFile : MemoryItem
    {
    	protected MemoryFile(MemoryFolder parent, string name)
    		: base (parent, name)
    	{ }
    	
        internal abstract long Size { get; set; }
        internal abstract uint Write(long offset, byte[] buffer);
        internal abstract uint Read(long offset, byte[] buffer);
        internal abstract System.IO.Stream OpenRead();
        internal abstract void Clear();
        internal abstract void SetContent(System.IO.Stream content);
        internal abstract MemoryFile CopyTo(MemoryFolder parent, string copyName);

        internal static MemoryFile New(MemoryFolder parent, string name)
            => new MemoryStreamFile(parent, name);
    }
}
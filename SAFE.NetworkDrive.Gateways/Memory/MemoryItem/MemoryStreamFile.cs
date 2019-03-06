using System;
using System.IO;

namespace SAFE.NetworkDrive.Gateways.Memory
{
    /// <summary>
    /// Represents a file in memory, based on MemoryStreams
    /// This is the base-class from which the file & folder
    /// are derived
    /// </summary>
    class MemoryStreamFile : MemoryFile
    {
        readonly MemoryStream _content;

        internal MemoryStreamFile(MemoryFolder parent, string name)
            : base(parent, name)
        {
        	_content = new MemoryStream();
        	
            Attributes = FileAttributes.Normal
                & FileAttributes.NotContentIndexed;
        }

        internal override long Size
        {
        	get => _content.Length;
			set 
			{ 				
				if (_content.Length != value)
					_content.SetLength(value);
			}
        }
        
        internal override uint Write(long offset, byte[] buffer)
        {
            Stream writeStream = _content;
            writeStream.Seek(offset, SeekOrigin.Begin);
            writeStream.Write(buffer, 0, buffer.Length);
            return (uint)buffer.Length;
        }
        
        internal override uint Read(long offset, byte[] buffer)
        {
        	Stream readStream = _content;
            readStream.Seek(offset, SeekOrigin.Begin);
            return (uint)readStream.Read(buffer, 0, buffer.Length);
        }

        internal override Stream OpenRead()
        {
            Stream readStream = _content;
            readStream.Seek(0, SeekOrigin.Begin);
            var memStream = new MemoryStream();
            readStream.CopyTo(memStream);
            memStream.Seek(0, SeekOrigin.Begin);
            return memStream;
        }

        internal override void Clear()
        {
            byte[] buffer = _content.GetBuffer();
            Array.Clear(buffer, 0, buffer.Length);
            _content.Position = 0;
            _content.SetLength(0);
        }

        internal override void SetContent(Stream content)
        {
            Clear();
            content.CopyTo(_content);
        }

        internal override MemoryFile CopyTo(MemoryFolder parent, string copyName)
        {
            var copyFile = New(parent, copyName);
            copyFile.CreationTime = CreationTime;
            copyFile.LastAccessTime = DateTime.Now;
            copyFile.LastWriteTime = DateTime.Now;
            copyFile.Attributes = Attributes;
            var buffer = new byte[Size];
            Read(0, buffer);
            copyFile.Write(0, buffer);
            return copyFile;
        }
    }
}
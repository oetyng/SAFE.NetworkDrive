using Newtonsoft.Json;
using SAFE.Data.Utils;
using SAFE.NetworkDrive.Encryption;
using System;
using System.Text;

namespace SAFE.NetworkDrive.Replication.Events
{
    public abstract class LocalEvent
    {
        protected LocalEvent(ulong sequenceNr)
        {
            TimeStamp = DateTime.UtcNow;
            SequenceNr = sequenceNr;
        }

        public DateTime TimeStamp { get; private set; }
        public ulong SequenceNr { get; private set; }
        public abstract NetworkEvent ToNetworkEvent();
    }

    public class WALContent
    {
        [SqlNado.SQLiteColumn(IsPrimaryKey = true)]
        public ulong SequenceNr { get; set; } // Serves as EventId and ContentId
        public byte[] EncryptedContent { get; set; }
        public bool Persisted { get; set; }
    }

    class LocalFileContentSet : LocalEvent
    {
        public LocalFileContentSet(ulong sequenceNr, string fileId, byte[] content)
            : base(sequenceNr)
        {
            FileId = fileId;
            Content = content;
        }

        public string FileId { get; }
        public byte[] Content { get; }

        public override NetworkEvent ToNetworkEvent()
            => throw new NotImplementedException();
    }

    class LocalFileItemCreated : LocalEvent
    {
        public LocalFileItemCreated(ulong sequenceNr, string parentDirId, string name, byte[] content)
            : base(sequenceNr)
        {
            ParentDirId = parentDirId;
            Name = name;
            Content = content;
        }

        public string ParentDirId { get; }
        public string Name { get; }
        public byte[] Content { get; }

        public override NetworkEvent ToNetworkEvent()
            => throw new NotImplementedException();
    }

    class LocalFileContentCleared : LocalEvent
    {
        public LocalFileContentCleared(ulong sequenceNr, string fileId)
            : base(sequenceNr)
        {
            FileId = fileId;
        }

        public string FileId { get; }

        public override NetworkEvent ToNetworkEvent()
            => new NetworkFileContentCleared(SequenceNr, FileId, TimeStamp);
    }

    class LocalItemCopied : LocalEvent
    {
        public LocalItemCopied(ulong sequenceNr, string fileSystemId, FSType fSType, 
            string copyName, string destDirId, bool recursive)
            : base(sequenceNr)
        {
            FileSystemId = fileSystemId;
            FSType = fSType;
            CopyName = copyName;
            DestDirId = destDirId;
            Recursive = recursive;
        }

        public string FileSystemId { get; }
        public FSType FSType { get; }
        public string CopyName { get; }
        public string DestDirId { get; }
        public bool Recursive { get; }

        public override NetworkEvent ToNetworkEvent()
            => new NetworkItemCopied(SequenceNr, FileSystemId, FSType, CopyName, DestDirId, Recursive, TimeStamp);
    }
    
    public enum FSType
    {
        File,
        Directory
    }

    class LocalItemMoved : LocalEvent
    {
        public LocalItemMoved(ulong sequenceNr, string fileSystemId, FSType fSType, string moveName, string destDirId)
            : base(sequenceNr)
        {
            FileSystemId = fileSystemId;
            FSType = fSType;
            MoveName = moveName;
            DestDirId = destDirId;
        }

        public string FileSystemId { get; }
        public FSType FSType { get; }
        public string MoveName { get; }
        public string DestDirId { get; }

        public override NetworkEvent ToNetworkEvent()
            => new NetworkItemMoved(SequenceNr, FileSystemId, FSType, MoveName, DestDirId, TimeStamp);
    }

    class LocalDirectoryItemCreated : LocalEvent
    {
        public LocalDirectoryItemCreated(ulong sequenceNr, string parentDirId, string name)
            : base(sequenceNr)
        {
            ParentDirId = parentDirId;
            Name = name;
        }

        public string ParentDirId { get; }
        public string Name { get; }

        public override NetworkEvent ToNetworkEvent()
            => new NetworkDirectoryItemCreated(SequenceNr, ParentDirId, Name, TimeStamp);
    }

    class LocalItemRemoved : LocalEvent
    {
        public LocalItemRemoved(ulong sequenceNr, string fileSystemId, FSType fSType, bool recursive)
            : base(sequenceNr)
        {
            FileSystemId = fileSystemId;
            FSType = fSType;
            Recursive = recursive;
        }

        public string FileSystemId { get; }
        public FSType FSType { get; }
        public bool Recursive { get; }

        public override NetworkEvent ToNetworkEvent()
            => new NetworkItemRemoved(SequenceNr, FileSystemId, FSType, Recursive, TimeStamp);
    }

    class LocalItemRenamed : LocalEvent
    {
        public LocalItemRenamed(ulong sequenceNr, string fileSystemId, FSType fSType, string newName)
            : base(sequenceNr)
        {
            FileSystemId = fileSystemId;
            FSType = fSType;
            NewName = newName;
        }

        public string FileSystemId { get; }
        public FSType FSType { get; }
        public string NewName { get; }

        public override NetworkEvent ToNetworkEvent()
            => new NetworkItemRenamed(SequenceNr, FileSystemId, FSType, NewName, TimeStamp);
    }


    public class ZipEncryptedEvent
    {
        [NonSerialized]
        byte[] _bytes;
        [NonSerialized]
        LocalEvent _event;

        [JsonConstructor]
        ZipEncryptedEvent() { }

        ZipEncryptedEvent(byte[] zipEncryptedData, string eventName)
        {
            ZipEncryptedData = zipEncryptedData;
            AssemblyQualifiedName = eventName;
        }

        [JsonRequired]
        public byte[] ZipEncryptedData { get; private set; }
        [JsonRequired]
        public string AssemblyQualifiedName { get; private set; }

        public byte[] GetBytes()
        {
            if (_bytes == null)
                _bytes = Encoding.UTF8.GetBytes(this.Json());
            return _bytes;
        }

        public static ZipEncryptedEvent From(byte[] data)
        {
            var json = Encoding.UTF8.GetString(data);
            return json.Parse<ZipEncryptedEvent>();
        }

        public LocalEvent GetEvent(string secretKey)
        {
            if (_event == null)
            {
                var decrypted = BytesCrypto.DecryptToBytes(secretKey, ZipEncryptedData);
                var decompressed = decrypted.Decompress();
                var json = Encoding.UTF8.GetString(decompressed);
                _event = (LocalEvent)json.Parse(AssemblyQualifiedName);
            }
            return _event;
        }

        public static ZipEncryptedEvent For(LocalEvent e, string secretKey)
            => new ZipEncryptedEvent(ZipEncrypt(e, secretKey), e.GetType().AssemblyQualifiedName);

        static byte[] ZipEncrypt(LocalEvent e, string secretKey)
        {
            var bytes = Encoding.UTF8.GetBytes(e.Json());
            var compressed = bytes.Compress();
            var encrypted = BytesCrypto.EncryptFromBytes(secretKey, compressed);
            return encrypted;
        }
    }
}
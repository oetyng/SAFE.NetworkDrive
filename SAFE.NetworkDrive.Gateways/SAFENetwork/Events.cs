using Newtonsoft.Json;
using SAFE.NetworkDrive.Encryption;
using SAFE.NetworkDrive.Gateways.Utils;
using System;
using System.Text;

namespace SAFE.NetworkDrive.Gateways.Obsolete.Events
{
    public class ZipEncryptedEvent
    {
        [NonSerialized]
        byte[] _bytes;
        [NonSerialized]
        Event _event;

        [JsonConstructor]
        ZipEncryptedEvent() { }

        ZipEncryptedEvent(byte[] zipEncryptedData, string eventName)
        {
            ZipEncryptedData = zipEncryptedData;
            EventName = eventName;
        }

        [JsonRequired]
        public byte[] ZipEncryptedData { get; private set; }
        [JsonRequired]
        public string EventName { get; private set; }

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

        public Event GetEvent(string secretKey)
        {
            if (_event == null)
            {
                var decrypted = BytesCrypto.DecryptToBytes(secretKey, ZipEncryptedData);
                var decompressed = decrypted.Decompress();
                var json = Encoding.UTF8.GetString(decompressed);
                _event = (Event)json.Parse(EventName);
            }
            return _event;
        }

        public static ZipEncryptedEvent For(Event e, string secretKey)
        {
            return new ZipEncryptedEvent(ZipEncrypt(e, secretKey), 
                e.GetType().Name);
        }

        static byte[] ZipEncrypt(Event e, string secretKey)
        {
            var bytes = Encoding.UTF8.GetBytes(e.Json());
            var compressed = bytes.Compress();
            var encrypted = BytesCrypto.EncryptFromBytes(secretKey, compressed);
            return encrypted;
        }
    }

    public class StoredEvent
    {
        [NonSerialized]
        byte[] _bytes;
        [NonSerialized]
        Event _event;

        [JsonConstructor]
        StoredEvent() { }

        StoredEvent(string payload, string assemblyQualifiedName)
        {
            Payload = payload;
            AssemblyQualifiedName = assemblyQualifiedName;
        }

        public string Payload { get; }
        public string AssemblyQualifiedName { get; }

        public Event GetEvent()
        {
            if (_event == null)
                _event = (Event)Payload.Parse(AssemblyQualifiedName);
            return _event;
        }

        public byte[] GetBytes()
        {
            if (_bytes == null)
                _bytes = Encoding.UTF8.GetBytes(this.Json());
            return _bytes;
        }

        public static StoredEvent From(string json)
            => json.Parse<StoredEvent>();

        public static StoredEvent For(Event e)
        {
            var json = JsonConvert.SerializeObject(e);
            return new StoredEvent(json, e.GetType().AssemblyQualifiedName);
        }
    }

    public abstract class Event
    {
        protected Event(long sequenceNr)
        {
            Id = SequentialGuid.NewGuid();
            TimeStamp = DateTime.UtcNow;
            SequenceNr = sequenceNr;
        }

        public Guid Id { get; private set; }
        public DateTime TimeStamp { get; private set; }
        public long SequenceNr { get; private set; }
    }

    class FileContentCleared : Event
    {
        public FileContentCleared(long sequenceNr, string fileId)
            : base(sequenceNr)
        {
            FileId = fileId;
        }

        public string FileId { get; }
    }

    class FileContentSet : Event
    {
        public FileContentSet(long sequenceNr, string fileId, byte[] content)
            : base(sequenceNr)
        {
            FileId = fileId;
            Content = content;
        }

        public string FileId { get; }
        public byte[] Content { get; }
    }

    class ItemCopied : Event
    {
        public ItemCopied(long sequenceNr, string fileSystemId, FSType fSType, 
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
    }
    
    public enum FSType
    {
        File,
        Directory
    }

    class ItemMoved : Event
    {
        public ItemMoved(long sequenceNr, string fileSystemId, FSType fSType, string moveName, string destDirId)
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
    }

    class DirectoryItemCreated : Event
    {
        public DirectoryItemCreated(long sequenceNr, string parentDirId, string name)
            : base(sequenceNr)
        {
            ParentDirId = parentDirId;
            Name = name;
        }

        public string ParentDirId { get; }
        public string Name { get; }
    }

    class FileItemCreated : Event
    {
        public FileItemCreated(long sequenceNr, string parentDirId, string name, byte[] content)
            : base(sequenceNr)
        {
            ParentDirId = parentDirId;
            Name = name;
            Content = content;
        }

        public string ParentDirId { get; }
        public string Name { get; }
        public byte[] Content { get; }
    }

    class ItemRemoved : Event
    {
        public ItemRemoved(long sequenceNr, string fileSystemId, FSType fSType, bool recursive)
            : base(sequenceNr)
        {
            FileSystemId = fileSystemId;
            FSType = fSType;
            Recursive = recursive;
        }

        public string FileSystemId { get; }
        public FSType FSType { get; }
        public bool Recursive { get; }
    }

    class ItemRenamed : Event
    {
        public ItemRenamed(long sequenceNr, string fileSystemId, FSType fSType, string newName)
            : base(sequenceNr)
        {
            FileSystemId = fileSystemId;
            FSType = fSType;
            NewName = newName;
        }

        public string FileSystemId { get; }
        public FSType FSType { get; }
        public string NewName { get; }
    }
}
using Newtonsoft.Json;
using SAFE.NetworkDrive.Gateways.Utils;
using System;
using System.Text;

namespace SAFE.NetworkDrive.Replication.Events
{
    public abstract class NetworkEvent
    {
        protected NetworkEvent(ulong sequenceNr)
        {
            TimeStamp = DateTime.UtcNow;
            SequenceNr = sequenceNr;
        }

        public DateTime TimeStamp { get; private set; }
        public ulong SequenceNr { get; private set; }
    }

    public class NetworkContentLocator
    {
        [NonSerialized]
        byte[] _bytes;

        public ulong ContentId { get; set; } // SequenceNr
        public byte[] MapOrContent { get; set; }
        public bool IsMap { get; set; }

        public byte[] GetBytes()
        {
            if (_bytes == null)
                _bytes = Encoding.UTF8.GetBytes(this.Json()).Compress();
            return _bytes;
        }

        public static NetworkContentLocator FromBytes(byte[] bytes)
            => Encoding.UTF8.GetString(bytes.Decompress()).Parse<NetworkContentLocator>();
    }

    public class NetworkFileContentSet : NetworkEvent
    {
        public NetworkFileContentSet(ulong sequenceNr, string fileId, byte[] mapOrContent, bool isMap)
            : base(sequenceNr)
        {
            FileId = fileId;
            MapOrContent = mapOrContent;
            IsMap = isMap;
        }

        public string FileId { get; }
        public byte[] MapOrContent { get; }
        public bool IsMap { get; }
    }

    class NetworkFileItemCreated : NetworkEvent
    {
        public NetworkFileItemCreated(ulong sequenceNr, string parentDirId, string name, byte[] mapOrContent, bool isMap)
            : base(sequenceNr)
        {
            ParentDirId = parentDirId;
            Name = name;
            MapOrContent = mapOrContent;
            IsMap = isMap;
        }

        public string ParentDirId { get; }
        public string Name { get; }
        public byte[] MapOrContent { get; }
        public bool IsMap { get; }
    }

    class NetworkFileContentCleared : NetworkEvent
    {
        public NetworkFileContentCleared(ulong sequenceNr, string fileId)
            : base(sequenceNr)
        {
            FileId = fileId;
        }

        public string FileId { get; }
    }

    class NetworkItemCopied : NetworkEvent
    {
        public NetworkItemCopied(ulong sequenceNr, string fileSystemId, FSType fSType, 
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
    
    class NetworkItemMoved : NetworkEvent
    {
        public NetworkItemMoved(ulong sequenceNr, string fileSystemId, FSType fSType, string moveName, string destDirId)
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

    class NetworkDirectoryItemCreated : NetworkEvent
    {
        public NetworkDirectoryItemCreated(ulong sequenceNr, string parentDirId, string name)
            : base(sequenceNr)
        {
            ParentDirId = parentDirId;
            Name = name;
        }

        public string ParentDirId { get; }
        public string Name { get; }
    }

    class NetworkItemRemoved : NetworkEvent
    {
        public NetworkItemRemoved(ulong sequenceNr, string fileSystemId, FSType fSType, bool recursive)
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

    class NetworkItemRenamed : NetworkEvent
    {
        public NetworkItemRenamed(ulong sequenceNr, string fileSystemId, FSType fSType, string newName)
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

    public class StoredEvent
    {
        [NonSerialized]
        byte[] _bytes;
        [NonSerialized]
        NetworkEvent _event;

        [JsonConstructor]
        StoredEvent() { }

        StoredEvent(string payload, string assemblyQualifiedName)
        {
            Payload = payload;
            AssemblyQualifiedName = assemblyQualifiedName;
        }

        public string Payload { get; }
        public string AssemblyQualifiedName { get; }

        public NetworkEvent GetEvent()
        {
            if (_event == null)
                _event = (NetworkEvent)Payload.Parse(AssemblyQualifiedName);
            return _event;
        }

        public byte[] GetBytes()
        {
            if (_bytes == null)
                _bytes = Encoding.UTF8.GetBytes(this.Json()).Compress();
            return _bytes;
        }

        public static StoredEvent From(string json)
        {
            return json.Parse<StoredEvent>();
        }

        public static StoredEvent For(NetworkEvent e)
        {
            var json = JsonConvert.SerializeObject(e);
            return new StoredEvent(json, e.GetType().AssemblyQualifiedName);
        }
    }
}

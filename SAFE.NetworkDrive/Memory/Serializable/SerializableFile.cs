﻿
using Newtonsoft.Json;

namespace SAFE.NetworkDrive.SerializableFS
{
    public class SerializableFile : SerializableItem
    {
        [JsonConstructor]
        SerializableFile() { }

        public SerializableFile(SerializableFolder parent, string name, TimeComponent time)
            : base(parent, name, time)
        { }

        public byte[] ContentOrMap { get; set; } = new byte[0];

        [JsonIgnore]
        public long Size => ContentOrMap.LongLength;
    }
}
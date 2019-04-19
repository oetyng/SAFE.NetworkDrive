using SAFE.Data.Utils;

namespace SAFE.NetworkDrive.SerializableFS
{
    public class SerializedFS
    {
        public SerializedFS(SerializableFolder root)
            => Data = root.GetBytes();

        public SerializedFS(byte[] data)
            => Data = data;

        public byte[] Data { get; }

        public SerializableFolder Deserialize() => Data.Parse<SerializableFolder>();
    }
}
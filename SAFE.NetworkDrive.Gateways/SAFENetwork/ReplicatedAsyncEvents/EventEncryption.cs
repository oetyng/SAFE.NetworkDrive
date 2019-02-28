using Newtonsoft.Json;
using SAFE.NetworkDrive.Encryption;
using SAFE.NetworkDrive.Gateways.Events;

namespace SAFE.NetworkDrive.Gateways.AsyncWAL
{
    class EventEncryption
    {
        string _password;

        public EventEncryption(string password)
        {
            _password = password;
        }

        public byte[] Encrypt(Event e)
        {
            var json = JsonConvert.SerializeObject(e);
            return BytesCrypto.Encrypt(_password, json);
        }

        public Event Decrypt(byte[] encryptedLog)
        {
            var json = BytesCrypto.Decrypt(_password, encryptedLog);
            var stored = StoredEvent.From(json);
            return stored.GetEvent();
        }
    }
}

using SAFE.AppendOnlyDb;
using SAFE.AppendOnlyDb.Snapshots;
using SAFE.Data;
using SAFE.Data.Client;
using SAFE.Data.Utils;
using SAFE.NetworkDrive.MemoryFS;
using SAFE.NetworkDrive.Replication.Events;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SAFE.NetworkDrive.Gateways.AsyncEvents
{
    class SAFENetworkEventService
    {
        readonly IStreamAD _stream;
        readonly IImDStore _imdStore;
        readonly string _pwd;

        public SAFENetworkEventService(IStreamAD stream, IImDStore imdStore, string pwd)
        {
            _stream = stream;
            _imdStore = imdStore;
            _pwd = pwd;
        }

        // Pass in data that was encrypted at rest
        // for upload to SAFENetwork.
        public async Task<(NetworkEvent, Result<Pointer>)> Upload(WALContent walContent)
        {
            var data = ZipEncryptedEvent.From(walContent.EncryptedContent);
            var localEvt = data.GetEvent(_pwd);
            var networkEvt = await GetNetworkEvent(localEvt);
            var expectedVersion = walContent.SequenceNr == 0 ? ExpectedVersion.None : ExpectedVersion.Specific(walContent.SequenceNr - 1);
            var result = await _stream.TryAppendAsync(new StoredValue(networkEvt), expectedVersion);
            return (networkEvt, result);
        }

        public Task<(MemoryGateway, SequenceNr)> LoadStateAsync(Func<IStreamAD, IImDStore, Task<(MemoryGateway, SequenceNr)>> restoration)
            => restoration(_stream, _imdStore);

        public IAsyncEnumerable<NetworkEvent> LoadAsync(ulong fromVersion)
        {
            var data = _stream.ReadForwardFromAsync(fromVersion);
            return data
                .Select(c => c.Item2)
                .Where(c => c != null)
                .Select(c => c.Parse<NetworkEvent>());
        }

        public Task<byte[]> LoadContent(byte[] map)
            => _imdStore.GetImDAsync(map);

        async Task<NetworkEvent> GetNetworkEvent(LocalEvent e)
        {
            switch (e)
            {
                // upload content
                case LocalFileContentSet ev:
                    var (isMap_0, data_0) = await GetMapOrContent(ev.Content);
                    return new NetworkFileContentSet(ev.SequenceNr, ev.FileId, data_0, isMap_0, e.TimeStamp);
                case LocalFileItemCreated ev:
                    var (isMap_1, data_1) = await GetMapOrContent(ev.Content);
                    return new NetworkFileItemCreated(ev.SequenceNr, ev.ParentDirId, ev.Name, data_1, isMap_1, e.TimeStamp);
                case null:
                    throw new ArgumentNullException(nameof(e));
                default:
                    return e.ToNetworkEvent();
            }
        }

        async Task<(bool, byte[])> GetMapOrContent(byte[] data)
        {
            var isMap = data.Length >= 1000;
            var mapOrcontent = isMap ? await _imdStore.StoreImDAsync(data) : data;
            return (isMap, mapOrcontent);
        }
    }
}
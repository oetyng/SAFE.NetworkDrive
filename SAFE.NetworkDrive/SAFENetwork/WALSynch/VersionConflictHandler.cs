using SAFE.AppendOnlyDb;
using SAFE.Data;
using SAFE.NetworkDrive.Replication.Events;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace SAFE.NetworkDrive.Gateways.AsyncEvents
{
    // TODO
    class VersionConflictHandler
    {
        readonly SAFENetworkEventService _service;
        readonly DriveMaterializer _materializer;

        public VersionConflictHandler(SAFENetworkEventService service, DriveMaterializer materializer)
        {
            _service = service;
            _materializer = materializer;
        }

        public async Task<bool> Upload(WALContent walContent)
        {
            var (e, result) = await _service.Upload(walContent);
            switch (result)
            {
                case VersionMismatch<Pointer> mismatch:
                    return await TryMergeAsync(e, walContent);
                case var r when r.HasValue:
                    Debug.WriteLine($"Uploaded version {e.SequenceNr} at {DateTime.Now.ToLongTimeString()}");
                    return r.HasValue;
                default:
                    return result.HasValue;
            }
        }

        // new data from network.. this effectively means we have a fork..
        // can we implement merge rules?
        // Temporarily: only appending with new version nr at end.
        // this will very likely lead to corrupted stream if any real
        // concurrent usage of the drive occurs.
        // TODO: Needs A LOT of work.
        async Task<bool> TryMergeAsync(NetworkEvent evt, WALContent walContent)
        {
            var newData = await _service
                .LoadAsync(evt.SequenceNr - 1)
                .ToListAsync();

            switch (evt)
            {
                case NetworkFileContentSet e when IsUnresolvableConflict(e, newData):
                    return false;
                case NetworkFileItemCreated e:
                    break;
                case NetworkFileContentCleared e:
                    break;
                case NetworkItemCopied e:
                    break;
                case NetworkItemMoved e:
                    break;
                case NetworkDirectoryItemCreated e:
                    break;
                case NetworkItemRemoved e:
                    break;
                case NetworkItemRenamed e:
                    break;
                case null:
                    throw new ArgumentNullException(nameof(evt));
                default:
                    throw new NotImplementedException(evt.GetType().Name);
            }

            // naively just try apply the remote changes
            var caughtUp = await _materializer.Materialize(newData.ToAsyncEnumerable());
            if (!caughtUp)
                return false;

            walContent.SequenceNr = newData.Max(c => c.SequenceNr) + 1;
            return await Upload(walContent);
        }

        // this will be complicated
        // might be better to lock changes, load all pending from local db
        // and look both of the chains of events together.
        bool IsUnresolvableConflict(NetworkFileContentSet e, List<NetworkEvent> newEvents)
        {
            var same = newEvents.OfType<NetworkFileContentSet>().ToList();
            if (same.Any(c => c.FileId == e.FileId))
                return true;

            var removed = newEvents.OfType<NetworkItemRemoved>().ToList();
            if (removed.Any(c => c.FileSystemId == e.FileId))
                return true;

            return false;
        }
    }
}
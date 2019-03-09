﻿using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using SAFE.NetworkDrive.Replication.Events;

namespace SAFE.NetworkDrive.Gateways.AsyncEvents
{
    // Background job
    // Receives data to enqueue on disk
    // Continuously reads logs from disk queue and passes to handler function.
    class DiskQueueWorker
    {
        public const int MIN_DELAY_SECONDS = 3;

        static bool _isRunning;
        static Mutex _mutex;
        readonly Stopwatch _sw = new Stopwatch();
        readonly Func<WALContent, Task<bool>> _onDequeued;
        readonly TimeSpan _minWorkDelay = TimeSpan.FromSeconds(MIN_DELAY_SECONDS);
        TimeSpan _currentWorkDelay = TimeSpan.FromSeconds(MIN_DELAY_SECONDS);

        /// <summary>
        /// Only one instance per machine can be created.
        /// </summary>
        /// <param name="storagePath">Where logs will be stored on the machine.</param>
        /// <param name="onDequeued">Operation for handling dequeued logs.</param>
        public DiskQueueWorker(Func<WALContent, Task<bool>> onDequeued)
        {
            if (_mutex != null)
                throw new ApplicationException("Only one instance of log synch can be running.");
            _mutex = new Mutex(true, nameof(DiskQueueWorker), out bool firstCaller);
            if (!firstCaller)
                throw new ApplicationException("Only one instance of log synch can be running.");
            _onDequeued = onDequeued;
            _sw.Start();
        }

        public static bool AnyInQueue()
        {
            using (var db = new SqlNado.SQLiteDatabase("content.db"))
            {
                if (!db.TableExists<WALContent>())
                    db.SynchronizeSchema<WALContent>();
                return db.Query<WALContent>()
                    .Where(c => !c.Persisted)
                    .Take(1)
                    .FirstOrDefault() != null;
            }
        }

        public static long GetVersion()
        {
            using (var db = new SqlNado.SQLiteDatabase("content.db"))
            {
                var data = db.Query<WALContent>()
                    .OrderBy(c => c.SequenceNr)
                    .Take(1)
                    .FirstOrDefault();
                if (data == null)
                    return -1;
                return (long)data.SequenceNr;
            }
        }

        /// <summary>
        /// Any attempt to enqueue data, will delay
        /// synch runner by MIN_DELAY_SECONDS
        /// </summary>
        /// <param name="data"></param>
        /// <param name="onEnqueued">
        /// Operation to be performed within the enqueue transaction.
        /// If "onEnqueued" fails, the transaction is rolled back.
        /// </param>
        public (bool, T) Enqueue<T>(WALContent data, Func<(bool, object)> onEnqueued)
        {
            _sw.Reset();

            using (var db = new SqlNado.SQLiteDatabase("content.db"))
            {
                try
                {
                    db.BeginTransaction();
                    db.Save(data);
                    var res = onEnqueued();
                    if (res.Item1) db.Commit();
                    else db.Rollback();
                    return (res.Item1, (T)res.Item2);
                }
                catch { db.Rollback(); throw; }
                finally { _sw.Restart(); }
            }
        }

        public void Start(CancellationToken cancellation)
        {
            Task.Factory.StartNew(() => RunQueueSynch(cancellation), TaskCreationOptions.LongRunning);
        }

        async Task RunQueueSynch(CancellationToken cancellation)
        {
            lock (_mutex)
            {
                if (_isRunning)
                    return;
                _isRunning = true;
            }

            while (!cancellation.IsCancellationRequested)
            {
                try
                {
                    if (await EnqueueingIsActive())
                        continue;
                    using (var db = new SqlNado.SQLiteDatabase("content.db"))
                    {
                        var data = db.Query<WALContent>()
                            .Where(c => !c.Persisted)
                            .OrderBy(c => c.SequenceNr)
                            .Take(1)
                            .FirstOrDefault();
                        if (data == null)
                            Delay();
                        else if (await _onDequeued(data))
                        {
                            data.Persisted = true;
                            db.RunTransaction(() => db.Save(data));
                        }
                    }
                }
                catch
                {
                    // 
                }
            }
        }

        // Only start synching if enqueueing has been inactive for MIN_DELAY_SECONDS
        // Then synch faster and faster, dividing wait time by two for every time.
        async Task<bool> EnqueueingIsActive()
        {
            await Task.Delay(_currentWorkDelay);

            if (_minWorkDelay > _sw.Elapsed) // less than MIN_DELAY_SECONDS since last access
            {
                _currentWorkDelay = _minWorkDelay; // reset delay time to MIN_DELAY_SECONDS
                return true; // enqueueing is active
            }
            if (_currentWorkDelay.Ticks > 0) // more than MIN_DELAY_SECONDS since last access
                _currentWorkDelay = new TimeSpan(_currentWorkDelay.Ticks / 2); // synch again in half previous wait time
            return false; // enqueueing is not active
        }

        void Delay()
            => _currentWorkDelay = new TimeSpan(_currentWorkDelay.Ticks * 2);
    }
}
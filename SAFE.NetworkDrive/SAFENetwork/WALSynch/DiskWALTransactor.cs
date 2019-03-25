using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using SAFE.NetworkDrive.Replication.Events;

namespace SAFE.NetworkDrive.Gateways.AsyncEvents
{
    /// <summary>
    /// Work Ahead Logs persisted on disk.
    /// Background job, single instance (machine wide).
    /// Receives WAL content to persist on disk.
    /// Continuously reads logs from disk queue and passes to handler function.
    /// </summary>
    class DiskWALTransactor
    {
        public const int MIN_DELAY_SECONDS = 3;

        bool _isRunning;
        readonly Mutex _mutex;
        readonly string _dbName;
        readonly Stopwatch _sw = new Stopwatch();
        readonly Func<WALContent, Task<bool>> _onDequeued;
        readonly TimeSpan _minWorkDelay = TimeSpan.FromSeconds(MIN_DELAY_SECONDS);
        TimeSpan _currentWorkDelay = TimeSpan.FromSeconds(MIN_DELAY_SECONDS);

        /// <summary>
        /// Only one instance per machine can be created.
        /// </summary>
        /// <param name="storagePath">Where logs will be stored on the machine.</param>
        /// <param name="onDequeued">Operation for handling dequeued logs.</param>
        public DiskWALTransactor(string dbName, Func<WALContent, Task<bool>> onDequeued)
        {
            _mutex = new Mutex(true, $"{dbName}_{nameof(DiskWALTransactor)}", out bool firstCaller);
            if (!firstCaller)
                throw new ApplicationException("Only one instance of log synch can be running.");
            _dbName = dbName;
            _onDequeued = onDequeued;
            _sw.Start();
        }

        public static bool AnyInQueue(string dbName)
        {
            using (var db = new SqlNado.SQLiteDatabase($"{dbName}.db"))
            {
                if (!db.TableExists<WALContent>())
                    db.SynchronizeSchema<WALContent>();
                return db.Query<WALContent>()
                    .Where(c => !c.Persisted)
                    .Take(1)
                    .FirstOrDefault() != null;
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
        public (bool, T) Enqueue<T>(WALContent data, Func<(bool Succeeded, object Data)> onEnqueued)
        {
            _sw.Reset();

            using (var db = new SqlNado.SQLiteDatabase($"{_dbName}.db"))
            {
                try
                {
                    db.BeginTransaction();
                    db.Save(data);
                    var res = onEnqueued();
                    if (res.Succeeded) db.Commit();
                    else db.Rollback();
                    return (res.Succeeded, (T)res.Data);
                }
                catch { db.Rollback(); throw; }
                finally { _sw.Restart(); }
            }
        }

        public void StartDequeueing(CancellationToken cancellation)
        {
            Task.Factory.StartNew(() => RunDequeueing(cancellation), TaskCreationOptions.LongRunning);
        }

        async Task RunDequeueing(CancellationToken cancellation)
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
                    if (await EnqueueingIsActive(cancellation))
                        continue;
                    using (var db = new SqlNado.SQLiteDatabase($"{_dbName}.db"))
                    {
                        var data = db.Query<WALContent>()
                            .Where(c => !c.Persisted)
                            .OrderBy(c => c.SequenceNr)
                            .Take(1)
                            .FirstOrDefault();
                        if (data == null && HighSpeed())
                            SetDelay(1 + _currentWorkDelay.Ticks * 2);
                        else if (await _onDequeued(data))
                        {
                            data.Persisted = true;
                            db.RunTransaction(() => db.Save(data));
                            if (_currentWorkDelay.Ticks > 0) // more than MIN_DELAY_SECONDS since last access
                                SetDelay(_currentWorkDelay.Ticks / 2); // synch again in half previous wait time
                        }
                    }

                    try { await Task.Delay(_currentWorkDelay, cancellation); }
                    catch (TaskCanceledException) { break; }
                }
                catch
                {
                    // 
                }
            }

            Cleanup();
            _mutex.Dispose();
        }

        void Cleanup()
        {
            using (var db = new SqlNado.SQLiteDatabase($"{_dbName}.db"))
            {
                try
                {
                    var data = db.Query<WALContent>()
                        .Where(c => c.Persisted)
                        .ToList();
                    if (data.Count > 0)
                    {
                        db.BeginTransaction();
                        foreach (var entry in data)
                            db.Delete(entry);
                        db.Commit();
                    }
                    db.Vacuum();
                }
                catch
                { }
            }
        }

        // Only start synching if enqueueing has been inactive for MIN_DELAY_SECONDS
        // Then synch faster and faster, dividing wait time by two for every time.
        async Task<bool> EnqueueingIsActive(CancellationToken cancellation)
        {
            if (_minWorkDelay > _sw.Elapsed) // less than MIN_DELAY_SECONDS since last access
            {
                _currentWorkDelay = _minWorkDelay; // reset delay time to MIN_DELAY_SECONDS
                return true; // enqueueing is active
            }
            
            return false; // enqueueing is not active
        }

        void SetDelay(long ticks)
            => _currentWorkDelay = new TimeSpan(ticks);

        bool HighSpeed()
            => _minWorkDelay.Ticks > _currentWorkDelay.Ticks;
    }
}
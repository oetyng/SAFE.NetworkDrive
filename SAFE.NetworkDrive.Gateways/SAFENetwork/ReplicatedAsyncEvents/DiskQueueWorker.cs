using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using DiskQueue;

namespace SAFE.NetworkDrive.Gateways.AsyncEvents
{
    // Background job
    // Receives data to enqueue on disk
    // Continuously reads logs from disk queue and passes to handler function.
    class DiskQueueWorker
    {
        public const int MIN_DELAY_SECONDS = 8;

        static bool _isRunning;
        readonly Mutex _mutex;
        readonly string _storagePath;
        readonly Func<byte[], Task<bool>> _onDequeued;
        readonly TimeSpan _minWorkDelay = TimeSpan.FromSeconds(MIN_DELAY_SECONDS);
        TimeSpan _currentWorkDelay = TimeSpan.FromSeconds(MIN_DELAY_SECONDS);
        Stopwatch _sw = new Stopwatch();

        /// <summary>
        /// Only one instance per machine can be created.
        /// </summary>
        /// <param name="storagePath">Where logs will be stored on the machine.</param>
        /// <param name="onDequeued">Operation for handling dequeued logs.</param>
        public DiskQueueWorker(string storagePath, Func<byte[], Task<bool>> onDequeued)
        {
            _mutex = new Mutex(true, storagePath, out bool firstCaller);
            if (!firstCaller)
                throw new ApplicationException("Only one instance of log synch can be running.");
            _onDequeued = onDequeued;
            _storagePath = storagePath;
            _sw.Start();
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
        public (bool, T) Enqueue<T>(byte[] data, Func<(bool, object)> onEnqueued)
        {
            try
            {
                _sw.Reset();
                using (var queue = PersistentQueue.WaitFor(_storagePath, TimeSpan.FromSeconds(30)))
                using (var session = queue.OpenSession())
                {
                    session.Enqueue(data);
                    var res = onEnqueued();
                    if (res.Item1)
                        session.Flush();
                    return (res.Item1, (T)res.Item2);
                }
            }
            finally
            {
                _sw.Restart();
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
                if (await EnqueueingIsActive())
                    continue;
                try
                {
                    using (var queue = PersistentQueue.WaitFor(_storagePath, TimeSpan.FromSeconds(30)))
                    using (var session = queue.OpenSession())
                    {
                        var data = session.Dequeue();
                        if (data == null) continue;
                        if (await _onDequeued(data))
                            session.Flush();
                    }
                }
                //catch (RetryException)
                //{
                //    Thread.Sleep(1000);
                //    continue;
                //}
                catch
                {
                    throw;
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
    }
}
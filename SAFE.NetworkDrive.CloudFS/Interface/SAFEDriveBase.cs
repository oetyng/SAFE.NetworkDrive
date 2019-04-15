using SAFE.NetworkDrive.Interface;
using System;
using System.Threading;

namespace SAFE.NetworkDrive
{
    internal abstract class SAFEDriveBase : IDisposable
    {
        protected DriveInfoContract _drive;

        SemaphoreSlim _semaphore = new SemaphoreSlim(1);

        public string DisplayRoot { get; }

        public long? Free => ExecuteInSemaphore(() => GetDrive().FreeSpace);
        public long? Used => ExecuteInSemaphore(() => GetDrive().UsedSpace);

        protected SAFEDriveBase(RootName root)
            => DisplayRoot = root.Value;

        protected void ExecuteInSemaphore(Action action, bool invalidateDrive = false)
        {
            if (action == null)
                throw new ArgumentNullException(nameof(action));

            _semaphore.Wait();
            try
            {
                action();
            }
            catch (AggregateException ex) when (ex.InnerExceptions.Count == 1)
            {
                throw ex.InnerExceptions[0];
            }
            finally
            {
                if (invalidateDrive)
                    _drive = null;
                _semaphore.Release();
            }
        }

        protected T ExecuteInSemaphore<T>(Func<T> func, bool invalidateDrive = false)
        {
            if (func == null)
                throw new ArgumentNullException(nameof(func));

            _semaphore.Wait();
            try
            {
                return func();
            }
            catch (AggregateException ex) when (ex.InnerExceptions.Count == 1)
            {
                throw ex.InnerExceptions[0];
            }
            finally
            {
                if (invalidateDrive)
                    _drive = null;
                _semaphore.Release();
            }
        }

        protected abstract DriveInfoContract GetDrive();

        public void Dispose()
        {
            _semaphore.Dispose();
            _semaphore = null;
            GC.SuppressFinalize(this);
        }
    }
}
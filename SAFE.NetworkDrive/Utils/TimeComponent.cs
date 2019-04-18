using System;

namespace SAFE.NetworkDrive
{
    public struct TimeComponent
    {
        public static TimeComponent Now => new TimeComponent
        {
            CreationTime = DateTime.Now,
            LastAccessTime = DateTime.Now,
            LastWriteTime = DateTime.Now
        };

        public TimeComponent(DateTime creationTime, DateTime lastAccessTime, DateTime lastWriteTime)
        {
            CreationTime = creationTime;
            LastAccessTime = lastAccessTime;
            LastWriteTime = lastWriteTime;
        }

        public DateTime CreationTime { get; private set; }
        public DateTime LastAccessTime { get; private set; }
        public DateTime LastWriteTime { get; private set; }

        internal static TimeComponent From(DateTime timeStamp) => new TimeComponent
        {
            CreationTime = timeStamp,
            LastAccessTime = timeStamp,
            LastWriteTime = timeStamp
        };

        internal TimeComponent CloneForCopy(DateTime copiedAt) => new TimeComponent
        {
            CreationTime = copiedAt,
            LastAccessTime = copiedAt,
            LastWriteTime = LastWriteTime
        };

        internal TimeComponent CloneForWrite(DateTime writtenAt) => new TimeComponent
        {
            CreationTime = CreationTime,
            LastAccessTime = writtenAt,
            LastWriteTime = writtenAt
        };
    }
}
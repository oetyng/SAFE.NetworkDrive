using System;
using SAFE.NetworkDrive.Interface;

namespace SAFE.NetworkDrive.Tests
{
    public sealed partial class SAFEDriveBaseTests
    {
        static class Fixture
        {
            class FakeCloudDrive : SAFEDriveBase
            {
                public FakeCloudDrive(RootName rootName) 
                    : base(rootName)
                { }

                protected override DriveInfoContract GetDrive() => throw new NotImplementedException();

                public void ExecuteInSemaphore(Action action) => base.ExecuteInSemaphore(action);
                public T ExecuteInSemaphore<T>(Func<T> func) => base.ExecuteInSemaphore(func);
            }

            static FakeCloudDrive CreateCloudDrive() => new FakeCloudDrive(new RootName("fake", "00000000000000000000000000000000", "FakeRoot"));

            public static void ExecuteInSemaphore(Action action)
            {
                using (var drive = CreateCloudDrive())
                    drive.ExecuteInSemaphore(action);
            }

            public static T ExecuteInSemaphore<T>(Func<T> func)
            {
                using (var drive = CreateCloudDrive())
                    return drive.ExecuteInSemaphore(func);
            }
        }
    }
}
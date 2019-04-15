using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace SAFE.NetworkDrive.Tests
{
    [TestClass]
    public sealed partial class SAFEDriveBaseTests
    {
        [TestMethod, TestCategory(nameof(TestCategories.Offline))]
        [ExpectedException(typeof(ArgumentNullException))]
        public void ExecuteInSemaphor_WhereActionIsNull_Throws()
            => Fixture.ExecuteInSemaphore(null);

        [TestMethod, TestCategory(nameof(TestCategories.Offline))]
        public void ExecuteInSemaphor_WhereActionSucceeds_Succeeds()
        {
            var executed = false;
            void action() => executed = true;
            Fixture.ExecuteInSemaphore(action);
            Assert.IsTrue(executed, "Expected Action not executed");
        }

        [TestMethod, TestCategory(nameof(TestCategories.Offline))]
        [ExpectedException(typeof(ApplicationException))]
        public void ExecuteInSemaphor_WhereActionThrowsAggregateException_ThrowsInnerException()
        {
            void action() => throw new AggregateException(new ApplicationException());
            Fixture.ExecuteInSemaphore(action);
        }

        [TestMethod, TestCategory(nameof(TestCategories.Offline))]
        [ExpectedException(typeof(ArgumentNullException))]
        public void ExecuteInSemaphor_WhereFuncIsNull_Throws()
            => Fixture.ExecuteInSemaphore((Func<object>)null);

        [TestMethod, TestCategory(nameof(TestCategories.Offline))]
        public void ExecuteInSemaphor_WhereFuncSucceeds_ReturnsFunctionResult()
        {
            var @object = new object();
            object func() => @object;
            var result = Fixture.ExecuteInSemaphore(func);
            Assert.AreSame(@object, result, "Expected result not returned");
        }

        [TestMethod, TestCategory(nameof(TestCategories.Offline))]
        [ExpectedException(typeof(ApplicationException))]
        public void ExecuteInSemaphor_WhereFuncThrowsAggregateException_ThrowsInnerException()
        {
            object func() => throw new AggregateException(new ApplicationException());
            var result = Fixture.ExecuteInSemaphore(func);
        }
    }
}
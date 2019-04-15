using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using SAFE.NetworkDrive.Parameters;

namespace SAFE.NetworkDrive.Tests
{
    [TestClass]
    public sealed partial class SAFEDriveFactoryTests
    {
        const string _schema = "test";
        const string _root = "Z";
        const string _volumeId = "00000000000000000000000000000000";

        Fixture _fixture;

        [TestInitialize]
        public void Initialize() => _fixture = Fixture.Initialize();

        [TestMethod, TestCategory(nameof(TestCategories.Offline))]
        [ExpectedException(typeof(InvalidOperationException))]
        public void CreateSAFEDrive_WhereGatewayManagerIsNotInitialized_Throws()
        {
            var sut = new SAFEDriveFactory();
            sut.CreateDrive(_schema, _volumeId, _root, null);
        }

        [TestMethod, TestCategory(nameof(TestCategories.Offline))]
        [ExpectedException(typeof(KeyNotFoundException))]
        public void CreateSAFEDrive_WhereGatewayIsNotRegistered_Throws()
        {
            var sut = new SAFEDriveFactory();
            sut.CreateDrive(_schema, _volumeId, _root, new SAFEDriveParameters());
        }

        [TestMethod, TestCategory(nameof(TestCategories.Offline))]
        public void CreateSAFEDrive_WhereAsyncGatewayIsRegistered_Succeeds()
        {
            var sut = new SAFEDriveFactory();

            using (var result = sut.CreateDrive(_schema, _volumeId, _root, new SAFEDriveParameters()))
                Assert.IsInstanceOfType(result, typeof(SAFEDrive), "Unexpected result type");
        }

        [TestMethod, TestCategory(nameof(TestCategories.Offline))]
        public void CreateSAFEDrive_WhereGatewayIsRegistered_Succeeds()
        {
            var sut = new SAFEDriveFactory();

            using (var result = sut.CreateDrive(_schema, _volumeId, _root, new SAFEDriveParameters()))
                Assert.IsInstanceOfType(result, typeof(SAFEDrive), "Unexpected result type");
        }
    }
}
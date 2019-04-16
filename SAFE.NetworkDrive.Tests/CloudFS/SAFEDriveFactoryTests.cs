using Microsoft.VisualStudio.TestTools.UnitTesting;

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
        public void CreateSAFEDrive_Succeeds()
        {
            var sut = new SAFEDriveFactory();

            using (var result = sut.CreateDrive(_schema, _volumeId, _root, _fixture.Parameters))
                Assert.IsInstanceOfType(result, typeof(SAFEDrive), "Unexpected result type");
        }
    }
}
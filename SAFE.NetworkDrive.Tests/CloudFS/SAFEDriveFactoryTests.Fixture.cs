
using SAFE.NetworkDrive.Parameters;

namespace SAFE.NetworkDrive.Tests
{
    public sealed partial class SAFEDriveFactoryTests
    {
        internal class Fixture
        {
            public SAFEDriveParameters Parameters { get; }

            internal static Fixture Initialize() => new Fixture();

            Fixture()
            {
                Parameters = new SAFEDriveParameters
                {
                    Locator = "Locator",
                    Secret = "Secret",
                };
            }
        }
    }
}
using SAFE.AppendOnlyDb;
using SAFE.AppendOnlyDb.Factories;
using SAFE.Data.Client;
using SAFE.Data.Client.Auth;
using System.Threading.Tasks;

namespace SAFE.NetworkDrive.Gateways.AsyncEvents
{
    class DbFactory
    {
        static readonly ClientFactory _factory;

        static DbFactory()
        {
            var appInfo = new AppInfo
            {
                Id = "safe.networkdrive",
                Name = "SAFE.NetworkDrive",
                Vendor = "oetyng"
            };
            SAFEClient.SetFactory(async (sess, app, db) => (object)await StreamDbFactory.CreateForApp(sess, app, db));
            _factory = new ClientFactory(appInfo, (session, appId) => new SAFEClient(session, appId));
        }

        public static async Task<(IStreamAD, IImDStore)> GetDriveDbsAsync(string volumeId, string apiKey, string secretKey)
        {
            var client = await _factory.GetMockNetworkClient(new Credentials(apiKey, secretKey), inMem: false);
            var db = await client.GetOrAddDbAsync<IStreamDb>(volumeId);
            var store = client.GetImDStore();
            var stream = await db.GetOrAddStreamAsync(volumeId);
            return (stream.Value, store);
        }
    }
}
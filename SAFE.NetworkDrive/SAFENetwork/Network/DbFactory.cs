using SAFE.AppendOnlyDb;
using SAFE.AppendOnlyDb.Factories;
using SAFE.AppendOnlyDb.Network;
using SAFE.AppendOnlyDb.Snapshots;
using SAFE.AuthClient;
using SAFE.Data.Client;
using SAFE.MockAuthClient;
using SAFE.NetworkDrive.Parameters;
using SafeApp;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SAFE.NetworkDrive.Gateways.AsyncEvents
{
    public class DbFactory
    {
        const string APP_ID = "safe.networkdrive";
        readonly SAFEDriveParameters _parameters;
        StreamDbFactory _streamDbFactory;
        INetworkDataOps _networkDataOps;

        public DbFactory(SAFEDriveParameters parameters)
            => _parameters = parameters;

        public async Task<(IStreamAD, IImDStore)> GetDriveDbsAsync(string volumeId, Func<IImDStore, Snapshotter> snapShotterFactory)
        {
            if (_streamDbFactory == null)
            {
                if (_parameters.Live) await InitLiveSession(snapShotterFactory);
                else await InitMockSession(snapShotterFactory);
            }
            var db = await _streamDbFactory.CreateForApp(APP_ID, volumeId);
            var store = GetImdStore();
            var stream = await db.Value.GetOrAddStreamAsync(volumeId);
            return (stream.Value, store);
        }

        async Task InitMockSession(Func<IImDStore, Snapshotter> snapShotterFactory, bool inMem = true)
        {
            var mockClient = new CredentialAuth(APP_ID, inMem);
            var session = (await mockClient.AuthenticateAsync()).Value;
            Set(snapShotterFactory, session);
        }

        async Task InitLiveSession(Func<IImDStore, Snapshotter> snapShotterFactory)
        {
            var credentials = new AuthClient.Credentials(_parameters.Locator, _parameters.Secret);
            var config = new AuthSessionConfig(credentials);
            using (var client = await AuthClient.AuthClient.InitSessionAsync(config))
            {
                var session = await client.CreateAppSessionAsync(GetAuthReq());
                Set(snapShotterFactory, session);
            }
        }

        void Set(Func<IImDStore, Snapshotter> snapShotterFactory, Session session)
        {
            _networkDataOps = new NetworkDataOps(session);
            var snapshotter = snapShotterFactory == null ? null : snapShotterFactory(GetImdStore());
            _streamDbFactory = new StreamDbFactory(_networkDataOps, snapshotter);
        }

        SafeApp.Utilities.AuthReq GetAuthReq()
        {
            return new SafeApp.Utilities.AuthReq
            {
                App = new SafeApp.Utilities.AppExchangeInfo
                {
                    Id = APP_ID,
                    Name = "SAFE.NetworkDrive",
                    Scope = null,
                    Vendor = "oetyng",
                },
                AppContainer = true,
                Containers = new List<SafeApp.Utilities.ContainerPermissions>()
            };
        }

        internal IImDStore GetImdStore()
            => new ImDStore(_networkDataOps);
    }
}
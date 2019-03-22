/*
The MIT License(MIT)

Copyright(c) 2015 IgorSoft

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.
*/

using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using SAFE.NetworkDrive.Parameters;

namespace SAFE.NetworkDrive.Tests
{
    [TestClass]
    public sealed partial class CloudDriveFactoryTests
    {
        const string _schema = "test";
        const string _user = "testUser";
        const string _root = "Z";

        Fixture _fixture;

        [TestInitialize]
        public void Initialize()
        {
            _fixture = Fixture.Initialize();
        }

        [TestMethod, TestCategory(nameof(TestCategories.Offline))]
        [ExpectedException(typeof(InvalidOperationException))]
        public void CreateCloudDrive_WhereGatewayManagerIsNotInitialized_Throws()
        {
            var sut = new CloudDriveFactory();
            sut.CreateCloudDrive(_schema, _user, _root, null);
        }

        [TestMethod, TestCategory(nameof(TestCategories.Offline))]
        [ExpectedException(typeof(KeyNotFoundException))]
        public void CreateCloudDrive_WhereGatewayIsNotRegistered_Throws()
        {
            _fixture.SetupTryGetAsyncCloudGatewayForSchema(_schema, false);
            _fixture.SetupTryGetCloudGatewayForSchema(_schema, false);
            var sut = new CloudDriveFactory();// { GatewayManager = _fixture.GetGatewayManager() };

            sut.CreateCloudDrive(_schema, _user, _root, new CloudDriveParameters());
        }

        [TestMethod, TestCategory(nameof(TestCategories.Offline))]
        public void CreateCloudDrive_WhereAsyncGatewayIsRegistered_Succeeds()
        {
            _fixture.SetupTryGetAsyncCloudGatewayForSchema(_schema);

            var sut = new CloudDriveFactory();// { GatewayManager = _fixture.GetGatewayManager() };

            using (var result = sut.CreateCloudDrive(_schema, _user, _root, new CloudDriveParameters())) {
                Assert.IsInstanceOfType(result, typeof(AsyncCloudDrive), "Unexpected result type");
            }
        }

        [TestMethod, TestCategory(nameof(TestCategories.Offline))]
        public void CreateCloudDrive_WhereGatewayIsRegistered_Succeeds()
        {
            const string schema = "test";
            _fixture.SetupTryGetAsyncCloudGatewayForSchema(schema, false);
            _fixture.SetupTryGetCloudGatewayForSchema(schema);

            var sut = new CloudDriveFactory();// { GatewayManager = _fixture.GetGatewayManager() };

            using (var result = sut.CreateCloudDrive(schema, _user, _root, new CloudDriveParameters())) {
                Assert.IsInstanceOfType(result, typeof(CloudDrive), "Unexpected result type");
            }
        }
    }
}

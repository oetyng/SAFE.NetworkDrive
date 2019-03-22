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
using System.Collections.Generic;
using SAFE.NetworkDrive.Interface.Composition;

namespace SAFE.NetworkDrive
{
    internal sealed class GatewayManager : IGatewayManager
    {
        readonly IDictionary<string, ExportFactory<IAsyncCloudGateway, CloudGatewayMetadata>> _asyncGateways = new Dictionary<string, ExportFactory<IAsyncCloudGateway, CloudGatewayMetadata>>();
        readonly IDictionary<string, ExportFactory<ICloudGateway, CloudGatewayMetadata>> _gateways = new Dictionary<string, ExportFactory<ICloudGateway, CloudGatewayMetadata>>();

        public GatewayManager(IEnumerable<ExportFactory<IAsyncCloudGateway, CloudGatewayMetadata>> asyncGateways, IEnumerable<ExportFactory<ICloudGateway, CloudGatewayMetadata>> syncGateways)
        {
            foreach (var asyncGateway in asyncGateways)
                _asyncGateways.Add(asyncGateway.Metadata.CloudService, asyncGateway);
            foreach (var gateway in syncGateways)
                _gateways.Add(gateway.Metadata.CloudService, gateway);
        }

        public bool TryGetAsyncCloudGatewayForSchema(string cloudService, out IAsyncCloudGateway asyncGateway)
        {
            if (_asyncGateways.TryGetValue(cloudService, out ExportFactory<IAsyncCloudGateway, CloudGatewayMetadata> result))
            {
                using (var export = result.CreateExport())
                {
                    asyncGateway = export.Value;
                    return true;
                }
            }
            else
            {
                asyncGateway = null;
                return false;
            }
        }

        public bool TryGetCloudGatewayForSchema(string cloudService, out ICloudGateway gateway)
        {
            if (_gateways.TryGetValue(cloudService, out ExportFactory<ICloudGateway, CloudGatewayMetadata> result))
            {
                using (var export = result.CreateExport())
                {
                    gateway = export.Value;
                    return true;
                }
            }
            else
            {
                gateway = null;
                return false;
            }
        }
    }

    public class ExportFactory<T1, T2>
    {
        readonly Func<Tuple<T1, Action>> _asyncGateFunc;

        public ExportFactory(Func<Tuple<T1, Action>> asyncGateFunc, T2 cloudGatewayMetadata)
        {
            _asyncGateFunc = asyncGateFunc;
            Metadata = cloudGatewayMetadata;
        }

        public T2 Metadata { get; set; }

        public ExportResult<T1> CreateExport()
        {
            return new ExportResult<T1>(_asyncGateFunc());
        }
    }

    public class ExportResult<T> : IDisposable
    {
        Tuple<T, Action> _tuple;

        public ExportResult(Tuple<T, Action> tuple)
        {
            _tuple = tuple;
            Value = _tuple.Item1;
        }

        public T Value { get; set; }

        public void Dispose()
        {
            
        }
    }
}
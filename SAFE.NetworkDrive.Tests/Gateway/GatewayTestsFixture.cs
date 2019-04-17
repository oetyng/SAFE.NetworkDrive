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
using System.Collections.Concurrent;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SAFE.NetworkDrive.Interface;
using SAFE.NetworkDrive.Tests.Gateway.Config;
using SAFE.NetworkDrive.Gateways;
using SAFE.NetworkDrive.Interface.IO;
using SAFE.Filesystem.Interface.IO;
using System.IO;
using Microsoft.Extensions.Configuration;

namespace SAFE.NetworkDrive.Tests.Gateway
{
    [TestClass]
    public class GatewayTestsFixture
    {
        static IConfiguration _config;
        static Dictionary<string, GatewaySection> _gatewaySections;
        static readonly ConcurrentDictionary<string, byte[]> _cache = new ConcurrentDictionary<string, byte[]>();

        public Dictionary<string, Interfaces.IMemoryGateway> Gateways { get; } = new Dictionary<string, Interfaces.IMemoryGateway>();
        
        [AssemblyInitialize]
        public static void Initialize(TestContext context)
        {
            _config = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", true, true)
                .Build();

            _gatewaySections = _config.GetSection(GatewaySection.Name).Get<Dictionary<string, GatewaySection>>();
            if (_gatewaySections == null)
                throw new ArgumentNullException("Test configuration missing"); // ConfigurationErrorsException
        }

        static void Log(string message) => Console.WriteLine(message);

        public static IEnumerable<GatewaySection> GetGatewayConfigurations(GatewayType type, GatewayCapabilities capability)
        {
            return _gatewaySections.Values.Where(g => 
                g.Type == type && (capability == GatewayCapabilities.None || 
                !g.Exclusions.HasFlag(capability))) ?? Enumerable.Empty<GatewaySection>();
        }

        public Interfaces.IMemoryGateway GetGateway(GatewaySection config)
        {
            if (!Gateways.ContainsKey(config.Schema))
            {
                switch (config.Schema)
                {
                    //case "file":
                    //    Gateways[config.Schema] = new Gateways.File.FileGateway();
                    //    break;
                    case "memory":
                        Gateways[config.Schema] = new MemoryFS.MemoryGateway(new RootName(config.Schema, config.VolumeId, config.Mount));
                        break;
                }
            }
            return Gateways[config.Schema];
        }

        public RootName GetRootName(GatewaySection config)
        {
            if (config == null)
                throw new ArgumentNullException(nameof(config));

            return new RootName(config.Schema, config.VolumeId, config.Mount);
        }

        public IDictionary<string, string> GetParameters(GatewaySection config)
        {
            if (config == null)
                throw new ArgumentNullException(nameof(config));

            var parameters = config.Parameters;
            if (string.IsNullOrEmpty(parameters))
                return null;

            var result = new Dictionary<string, string>();
            foreach (var parameter in parameters.Split('|'))
            {
                var components = parameter.Split(new[] { '=' }, 2);
                result.Add(components[0], components.Length == 2 ? components[1] : null);
            }

            return result;
        }

        void CallTimedTestOnConfig<TGateway>(Action<TGateway, RootName, GatewaySection> test, GatewaySection config, Func<GatewaySection, TGateway> getGateway, IDictionary<string, Exception> failures)
        {
            try
            {
                var startedAt = DateTime.Now;
                var gateway = getGateway(config);
                var rootName = GetRootName(config);
                test(gateway, rootName, config);
                var completedAt = DateTime.Now;
                Log($"Test for schema '{config.Schema}' completed in {completedAt - startedAt}".ToString(CultureInfo.CurrentCulture));
            }
            catch (Exception ex)
            {
                var aggregateException = ex as AggregateException;
                var message = aggregateException != null ? string.Join(", ", aggregateException.InnerExceptions.Select(e => e.Message)) : ex.Message;
                Log($"Test for schema '{config.Schema}' failed:\n\t {message}".ToString(CultureInfo.CurrentCulture));
                failures.Add(config.Schema, ex);
            }
        }

        void ExecuteByConfiguration<TGateway>(Action<TGateway, RootName, GatewaySection> test, GatewayType type, Func<GatewaySection, TGateway> getGateway, int maxDegreeOfParallelism)
        {
            var configurations = GetGatewayConfigurations(type, GatewayCapabilities.None);
            var failures = default(IDictionary<string, Exception>);

            if (maxDegreeOfParallelism > 1)
            {
                failures = new ConcurrentDictionary<string, Exception>();
                Parallel.ForEach(configurations, new ParallelOptions() { MaxDegreeOfParallelism = maxDegreeOfParallelism }, config => 
                {
                    CallTimedTestOnConfig(test, config, getGateway, failures);
                });
            }
            else if (maxDegreeOfParallelism == 1)
            {
                failures = new Dictionary<string, Exception>();
                foreach (var config in configurations)
                    CallTimedTestOnConfig(test, config, getGateway, failures);
            }
            else
            {
                throw new ArgumentException("Degree of parallelism must be positive", nameof(maxDegreeOfParallelism));
            }

            if (failures.Any())
                throw new AggregateException("Test failed in " + string.Join(", ", failures.Select(t => t.Key)), failures.Select(t => t.Value));
        }

        public void ExecuteByConfiguration(Action<Interfaces.IMemoryGateway, RootName, GatewaySection> test, int maxDegreeOfParallelism = int.MaxValue)
            => ExecuteByConfiguration(test, GatewayType.Sync, config => GetGateway(config), maxDegreeOfParallelism);

        public IProgress<ProgressValue> GetProgressReporter() => new NullProgressReporter();

        public void OnCondition(GatewaySection config, GatewayCapabilities capability, Action action)
        {
            if (config == null)
                throw new ArgumentNullException(nameof(config));
            if (action == null)
                throw new ArgumentNullException(nameof(action));

            var capabilityExcluded = config.Exclusions.HasFlag(capability);

            try
            {
                action();
                Assert.IsFalse(capabilityExcluded, $"Unexpected capability {capability}".ToString(CultureInfo.CurrentCulture));
            }
            catch (NotSupportedException) when (capabilityExcluded)
            {
                // Ignore NotSupportedException
            }
            catch (AggregateException ex) when (capabilityExcluded && ex.InnerExceptions.Count == 1 && ex.InnerException is NotSupportedException)
            {
                // Ignore AggregateException containing a single NotSupportedException
            }
        }

        public byte[] GetArbitraryBytes(FileSize bytes)
        {
            if (bytes == null)
                throw new ArgumentNullException(nameof(bytes));

            return _cache.GetOrAdd(bytes.ToString(), (s) => Enumerable.Range(0, (int)bytes).Select(i => (byte)(i % 251 + 1)).ToArray());
        }
    }
}
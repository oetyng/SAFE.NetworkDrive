﻿/*
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
using System.Globalization;
using System.Linq;
using System.Reflection;

namespace SAFE.NetworkDrive.Interface.Composition
{
    /// <summary>
    /// Describes a cloud service.
    /// </summary>
    [System.Diagnostics.DebuggerDisplay("{DebuggerDisplay(),nq}")]
    public sealed class CloudGatewayMetadata
    {
        readonly IDictionary<string, object> _values;

        /// <summary>
        /// Gets the cloud service.
        /// </summary>
        /// <value>A <see cref="string"/> representing the cloud service.</value>
        public string CloudService => _values.TryGetValue(nameof(CloudService), out object result) ? 
            result as string : null;

        /// <summary>
        /// Gets the cloud service capabilities.
        /// </summary>
        /// <value>The cloud service capabilities.</value>
        public GatewayCapabilities Capabilities => _values.TryGetValue(nameof(Capabilities), out object result) ? 
            (GatewayCapabilities)result : GatewayCapabilities.None;

        /// <summary>
        /// Gets the cloud service URI.
        /// </summary>
        /// <value>The cloud service URI.</value>
        public Uri ServiceUri => _values.TryGetValue(nameof(ServiceUri), out object result) ? 
            new Uri(result as string) : null;

        /// <summary>
        /// Gets the name of the cloud service API assembly.
        /// </summary>
        /// <value>The name of the API assembly.</value>
        public AssemblyName ApiAssembly => _values.TryGetValue(nameof(ApiAssembly), out object result) ? 
            AppDomain.CurrentDomain.GetAssemblies().FirstOrDefault(a => a.GetName().Name == result as string)?.GetName() : null;

        /// <summary>
        /// Initializes a new instance of the <see cref="CloudGatewayMetadata"/> class.
        /// </summary>
        /// <param name="values">The initialization values.</param>
        /// <exception cref="ArgumentNullException">values is <c>null</c>.</exception>
        /// <remarks>Supported keys in <paramref name="values"/> are: <see cref="CloudService"/>, <see cref="Capabilities"/>, <see cref="ServiceUri"/>, or <see cref="ApiAssembly"/>.</remarks>
        public CloudGatewayMetadata(IDictionary<string, object> values)
        {
            _values = values ?? throw new ArgumentNullException(nameof(values));
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Justification = "Debugger Display")]
        [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
        string DebuggerDisplay() => $"{nameof(CloudGatewayMetadata)} '{CloudService}' Capabilities={Capabilities} ServiceUri='{ServiceUri}' ApiAssembly='{ApiAssembly}'".ToString(CultureInfo.CurrentCulture);
    }
}
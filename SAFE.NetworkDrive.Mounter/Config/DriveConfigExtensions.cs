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

namespace SAFE.NetworkDrive.Mounter.Config
{
    public static class DriveConfigExtensions
    {
        public static IDictionary<string, string> GetParameters(this DriveConfig config)
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
    }
}
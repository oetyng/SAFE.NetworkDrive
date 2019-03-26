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
using System.Globalization;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using SemanticTypes;

namespace SAFE.NetworkDrive.Interface
{
    /// <summary>
    /// The root of a cloud file system.
    /// </summary>
    /// <seealso cref="SemanticType{string}" />
    [System.Diagnostics.DebuggerDisplay("{DebuggerDisplay(),nq}")]
    public sealed class RootName : SemanticType<string>
    {
        const string rootNamePattern = @"^(?<Schema>[a-z][_a-z0-9]*)(@(?<VolumeId>[a-f0-9]{8}([a-f0-9]{4}){3}[a-f0-9]{12}))?(\|(?<Root>.+))?$";
        static readonly Regex validationRegex = new Regex(rootNamePattern, RegexOptions.Compiled);

        //  @"([a-f0-9]{8}([a-f0-9]{4}){3}[a-f0-9]{12})$"

        /// <summary>
        /// Gets the schema of cloud service providing the storage space.
        /// </summary>
        /// <value>The schema.</value>
        public string Schema { get; }

        ///// <summary>
        ///// Gets the user name of the the storage account.
        ///// </summary>
        ///// <value>The user name.</value>
        //public string UserName { get; }

        /// <summary>
        /// Gets the root volume for the mounted cloud file system.
        /// </summary>
        /// <value>The root volume.</value>
        public string Root { get; }

        /// <summary>
        /// Gets the volume ID for the cloud file system.
        /// </summary>
        /// <value>The volume ID.</value>
        public string VolumeId { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="RootName"/> class from a formatted string.
        /// </summary>
        /// <param name="name">The formatted root name.</param>
        public RootName(string name)
            : base(validationRegex.IsMatch, name)
        {
            var groups = validationRegex.Match(name).Groups;
            Schema = groups[nameof(Schema)].Value;
            VolumeId = groups[nameof(VolumeId)].Value;
            Root = groups[nameof(Root)].Value;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="RootName"/> class.
        /// </summary>
        /// <param name="schema">The cloud service schema.</param>
        /// <param name="userName">The user name.</param>
        /// <param name="root">The root volume.</param>
        public RootName(string schema, string volumeId, string root) 
            : this(Format(schema, volumeId, root))
        { }

        static string Format(string schema, string volumeId, string root)
        {
            var builder = new StringBuilder(schema);
            if (!string.IsNullOrEmpty(volumeId))
            {
                builder.Append("@");
                builder.Append(volumeId);
            }
            if (!string.IsNullOrEmpty(root) && root != Path.DirectorySeparatorChar.ToString())
            {
                builder.Append("|");
                builder.Append(root);
            }
            return builder.ToString();
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Justification = "Used for DebuggerDisplay")]
        [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
        string DebuggerDisplay() => $"{nameof(RootName)} {Format(Schema, VolumeId, Root)}".ToString(CultureInfo.CurrentCulture);
    }
}
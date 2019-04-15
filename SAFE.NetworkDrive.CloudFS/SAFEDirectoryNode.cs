using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using SAFE.NetworkDrive.Interface;

namespace SAFE.NetworkDrive
{
    [System.Diagnostics.DebuggerDisplay("{DebuggerDisplay,nq}")]
    internal class SAFEDirectoryNode : SAFEItemNode
    {
        internal IDictionary<string, SAFEItemNode> children;

        public new DirectoryInfoContract Contract => (DirectoryInfoContract)base.Contract;

        public SAFEDirectoryNode(DirectoryInfoContract contract) 
            : base(contract)
        { }

        public override void SetParent(SAFEDirectoryNode parent)
        {
            base.SetParent(parent);
            Contract.Parent = parent?.Contract;
        }

        public IEnumerable<SAFEItemNode> GetChildItems(ISAFEDrive drive)
        {
            if (drive == null)
                throw new ArgumentNullException(nameof(drive));

            if (children == null)
            {
                lock (Contract)
                {
                    if (children == null)
                    {
                        children = drive
                            .GetChildItem(Contract)
                            .Select(f => CreateNew(f)).ToDictionary(i => i.Name);

                        foreach (var child in children.Values)
                            child.SetParent(this);
                    }
                }
            }

            return children.Values;
        }

        public SAFEItemNode GetChildItemByName(ISAFEDrive drive, string fileName)
        {
            GetChildItems(drive);
            children.TryGetValue(fileName, out SAFEItemNode result);
            return result;
        }

        public SAFEDirectoryNode NewDirectoryItem(ISAFEDrive drive, string directoryName)
        {
            if (drive == null)
                throw new ArgumentNullException(nameof(drive));

            var newItem = new SAFEDirectoryNode(drive.NewDirectoryItem(Contract, directoryName));
            children.Add(newItem.Name, newItem);
            newItem.SetParent(this);
            return newItem;
        }

        public SAFEFileNode NewFileItem(ISAFEDrive drive, string fileName)
        {
            if (drive == null)
                throw new ArgumentNullException(nameof(drive));

            var newItem = new SAFEFileNode(drive.NewFileItem(Contract, fileName, Stream.Null));
            children.Add(newItem.Name, newItem);
            newItem.SetParent(this);
            return newItem;
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Justification = "Debugger Display")]
        [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
        string DebuggerDisplay => $"{nameof(SAFEDirectoryNode)} {Name} [{children?.Count ?? 0}]".ToString(CultureInfo.CurrentCulture);
    }
}
using System;
using System.IO;
using System.Globalization;
using SAFE.NetworkDrive.Interface;

namespace SAFE.NetworkDrive
{
    [System.Diagnostics.DebuggerDisplay("{DebuggerDisplay,nq}")]
    internal class SAFEFileNode : SAFEItemNode
    {
        public new FileInfoContract Contract => (FileInfoContract)base.Contract;

        public SAFEFileNode(FileInfoContract contract) 
            : base(contract)
        { }

        public override void SetParent(SAFEDirectoryNode parent)
        {
            base.SetParent(parent);
            Contract.Directory = parent?.Contract;
        }

        public Stream GetContent(ISAFEDrive drive)
        {
            if (drive == null)
                throw new ArgumentNullException(nameof(drive));

            return drive.GetContent(Contract);
        }

        public void SetContent(ISAFEDrive drive, Stream stream)
        {
            if (drive == null)
                throw new ArgumentNullException(nameof(drive));

            var proxyFileInfoContract = Contract as ProxyFileInfoContract;
            if (proxyFileInfoContract != null)
                ResolveContract(drive.NewFileItem(Parent.Contract, proxyFileInfoContract.Name, stream));
            else
                drive.SetContent(Contract, stream);
        }

        public void Truncate(ISAFEDrive drive)
        {
            if (drive == null)
                throw new ArgumentNullException(nameof(drive));

            drive.SetContent(Contract, Stream.Null);
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Justification = "Debugger Display")]
        [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
        string DebuggerDisplay => $"{nameof(SAFEFileNode)} {Name} Size={Contract.Size}".ToString(CultureInfo.CurrentCulture);
    }
}
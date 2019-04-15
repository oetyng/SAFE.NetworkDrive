using System;
using System.Globalization;
using SAFE.NetworkDrive.Interface;

namespace SAFE.NetworkDrive
{
    internal abstract class SAFEItemNode
    {
        public FileSystemInfoContract Contract { get; private set; }
        protected SAFEDirectoryNode Parent { get; private set; }
        public string Name => Contract.Name;
        public bool IsResolved => !(Contract is ProxyFileInfoContract);

        protected SAFEItemNode(FileSystemInfoContract contract)
            => Contract = contract ?? throw new ArgumentNullException(nameof(contract));

        public static SAFEItemNode CreateNew(FileSystemInfoContract fileSystemInfo)
        {
            var fileInfoContract = fileSystemInfo as FileInfoContract;
            if (fileInfoContract != null)
                return new SAFEFileNode(fileInfoContract);

            var directoryInfoContract = fileSystemInfo as DirectoryInfoContract;
            if (directoryInfoContract != null)
                return new SAFEDirectoryNode(directoryInfoContract);

            throw new InvalidOperationException(string.Format(CultureInfo.InvariantCulture, "Unknown item type '{0}'", fileSystemInfo.GetType().Name));
        }

        protected void ResolveContract(FileInfoContract contract)
        {
            if (IsResolved)
                throw new InvalidOperationException(string.Format(CultureInfo.CurrentCulture, "{0} '{1}' is not a resolvable FileSystemInfo type", Contract.GetType().Name, Contract.Name));
            if (Contract.Name != contract.Name)
                throw new InvalidOperationException(string.Format(CultureInfo.CurrentCulture, "Cannot resolve ProxyFileInfo '{0}' with FileInfo '{1}'", Contract.Name, contract.Name));

            Contract = contract;
        }

        public virtual void SetParent(SAFEDirectoryNode parent)
            => Parent = parent;

        public void Move(ISAFEDrive drive, string newName, SAFEDirectoryNode destinationDirectory)
        {
            if (drive == null)
                throw new ArgumentNullException(nameof(drive));
            if (string.IsNullOrEmpty(newName))
                throw new ArgumentNullException(nameof(newName));
            if (destinationDirectory == null)
                throw new ArgumentNullException(nameof(destinationDirectory));
            if (Parent == null)
                throw new InvalidOperationException($"{nameof(Parent)} of {GetType().Name} '{Name}' is null".ToString(CultureInfo.CurrentCulture));

            var moveItem = CreateNew(drive.MoveItem(Contract, newName, destinationDirectory.Contract));
            if (destinationDirectory.children != null)
            {
                destinationDirectory.children.Add(moveItem.Name, moveItem);
                moveItem.SetParent(destinationDirectory);
            }
            else
                destinationDirectory.GetChildItems(drive);

            Parent.children.Remove(Name);
            SetParent(null);
        }

        public void Remove(ISAFEDrive drive)
        {
            if (drive == null)
                throw new ArgumentNullException(nameof(drive));

            Parent.children.Remove(Name);
            drive.RemoveItem(Contract, false);
            SetParent(null);
        }
    }
}
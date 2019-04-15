using SAFE.NetworkDrive.Interface.IO;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace SAFE.NetworkDrive.Interface
{
    /// <summary>
    /// An asynchronous gateway to a safe network file system.
    /// </summary>
    public interface ISAFEGateway
    {
        /// <summary>
        /// Gets the drive associated with a cloud file system asynchronously.
        /// </summary>
        /// <returns>A <see cref="Task{DriveInfoContract}"/> representing the drive.</returns>
        DriveInfoContract GetDrive();

        /// <summary>
        /// Gets the root directory asynchronously.
        /// </summary>
        /// <returns>A <see cref="Task{RootDirectoryInfoContract}"/> representing the root directory.</returns>
        RootDirectoryInfoContract GetRoot();

        /// <summary>
        /// Gets the child items of the specified parent directory asynchronously.
        /// </summary>
        /// <param name="parent">The parent directory info.</param>
        /// <returns>An enumeration of <see cref="Task{IEnumerable<FileSystemInfoContract>}"/> representing the child items.</returns>
        IEnumerable<FileSystemInfoContract> GetChildItem(DirectoryId parent);

        /// <summary>
        /// Clears the content of the specified file asynchronously.
        /// </summary>
        /// <param name="target">The target file ID.</param>
        /// <returns>A <see cref="Task{bool}"/> representing the operation result.</returns>
        bool ClearContent(FileId target);

        /// <summary>
        /// Creates a read-only <see cref="Stream"/> object for the the content of the specified file asynchronously.
        /// </summary>
        /// <param name="source">The source file ID.</param>
        /// <returns>A <see cref="Task"/> representing a read-only <see cref="Stream"/> object for the the content of the specified file.</returns>
        Stream GetContent(FileId source);

        /// <summary>
        /// Sets the content of the specified file from the specified <see cref="Stream" /> asynchronously.
        /// </summary>
        /// <param name="target">The target file ID.</param>
        /// <param name="content">A <see cref="Stream" /> object providing the content.</param>
        /// <param name="progress">A provider for progress updates.</param>
        /// <returns>A <see cref="Task{bool}"/> representing the operation result.</returns>
        bool SetContent(FileId target, Stream content, IProgress<ProgressValue> progress);

        /// <summary>
        /// Copies and optionally renames the specified cloud file system object to the specified destination directory asynchronously.
        /// </summary>
        /// <param name="source">The source file system object ID.</param>
        /// <param name="copyName">The name of the copy.</param>
        /// <param name="destination">The destination directory ID.</param>
        /// <param name="recurse">if set to <c>true</c>, copies child items recursively.</param>
        /// <returns>A <see cref="Task{FileSystemInfoContract}"/> representing the copied cloud file system object.</returns>
        /// <remarks>A file system object cannot be copied to a different cloud drive.</remarks>
        FileSystemInfoContract CopyItem(FileSystemId source, string copyName, DirectoryId destination, bool recurse);

        /// <summary>
        /// Moves and optionally renames the specified cloud file system object to the specified destination directory asynchronously.
        /// </summary>
        /// <param name="source">The source file system object ID.</param>
        /// <param name="moveName">The name of the moved file system object.</param>
        /// <param name="destination">The destination directory ID.</param>
        /// <param name="locatorResolver">Provides a <see cref="FileSystemInfoLocator"/> instance.</param>
        /// <returns>A <see cref="Task{FileSystemInfoContract}"/> representing the moved cloud file system object.</returns>
        /// <remarks>A file system object cannot be moved to a different cloud drive.</remarks>
        FileSystemInfoContract MoveItem(FileSystemId source, string moveName, DirectoryId destination);

        /// <summary>
        /// Creates a new cloud directory asynchronously.
        /// </summary>
        /// <param name="parent">The parent directory ID.</param>
        /// <param name="name">The name.</param>
        /// <returns>A <see cref="Task{DirectoryInfoContract}"/> representing the new directory.</returns>
        DirectoryInfoContract NewDirectoryItem(DirectoryId parent, string name);

        /// <summary>
        /// Creates a new cloud file asynchronously.
        /// </summary>
        /// <param name="parent">The parent directory ID.</param>
        /// <param name="name">The name.</param>
        /// <param name="content">A <see cref="Stream"/> object providing the content.</param>
        /// <param name="progress">A provider for progress updates.</param>
        /// <returns>A <see cref="Task{FileInfoContract}"/> representing the new file.</returns>
        FileInfoContract NewFileItem(DirectoryId parent, string name, Stream content, IProgress<ProgressValue> progress);

        /// <summary>
        /// Removes the specified cloud file system object asynchronously.
        /// </summary>
        /// <param name="target">The target file system object ID.</param>
        /// <param name="recurse">if set to <c>true</c>, removes a non-empty directory with its child items recursively.</param>
        /// <returns>A <see cref="Task{bool}"/> representing the operation result.</returns>
        bool RemoveItem(FileSystemId target, bool recurse);

        /// <summary>
        /// Renames the specified cloud file system object asynchronously.
        /// </summary>
        /// <param name="target">The target file system object ID.</param>
        /// <param name="newName">The new name.</param>
        /// <returns>A <see cref="Task{FileSystemInfoContract}"/> representing the renamed file system object.</returns>
        FileSystemInfoContract RenameItem(FileSystemId target, string newName);
    }
}
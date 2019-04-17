using SAFE.Filesystem.Interface.IO;
using SAFE.NetworkDrive.Interface;
using SAFE.NetworkDrive.Interface.IO;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace SAFE.NetworkDrive.MemoryFS
{
    [System.Diagnostics.DebuggerDisplay("{DebuggerDisplay(),nq}")]
    public sealed class MemoryGateway : Interfaces.IMemoryGateway
    {
        readonly RootName _root;
        readonly MemoryFolder _rootFolder = new MemoryFolder(null, string.Empty);

        public const string PARAMETER_ROOT = "root";
        const string PATH_NOT_FOUND = "Path '{0}' does not exist";
        const string DUPLICATE_PATH = "'{0}' is already present";
        string _rootPath;

        public MemoryGateway(RootName root)
        { 
            _root = root ?? throw new ArgumentNullException(nameof(root));
            _rootPath = System.IO.Path.DirectorySeparatorChar.ToString();
        }

        public DriveInfoContract GetDrive()
        {
            //if (parameters?.TryGetValue(PARAMETER_ROOT, out _rootPath) != true)
            //    throw new ArgumentException($"Required {PARAMETER_ROOT} missing in {nameof(parameters)}".ToString(CultureInfo.CurrentCulture));
            //if (string.IsNullOrEmpty(_rootPath))
            //    throw new ArgumentException($"{PARAMETER_ROOT} cannot be empty".ToString(CultureInfo.CurrentCulture));

            var drive = new MemDrive(_rootFolder);
            return new DriveInfoContract(_root.Value,
                drive.AvailableFreeSpace,
                drive.TotalSize - drive.AvailableFreeSpace);
        }

        class MemDrive
        {
            readonly MemoryFolder _root;

            public MemDrive(MemoryFolder root)
                => _root = root;

            public long TotalSize => long.MaxValue; // System.Diagnostics.Process.GetCurrentProcess().VirtualMemorySize64;
            public long AvailableFreeSpace => System.Diagnostics.Process.GetCurrentProcess().VirtualMemorySize64; //long.MaxValue;//TotalSize - (long)_root.UsedSize;
        }

        public RootDirectoryInfoContract GetRoot()
        {
            if (string.IsNullOrEmpty(_rootPath))
                throw new InvalidOperationException($"{nameof(_rootPath)} not initialized".ToString(CultureInfo.CurrentCulture));

            var id = System.IO.Path.DirectorySeparatorChar.ToString();

            return new RootDirectoryInfoContract(id, _rootFolder.CreationTime, _rootFolder.LastWriteTime);
        }

        static string GetFullPath(string rootPath, string path)
        {
            if (System.IO.Path.IsPathRooted(path))
                return path;
            return System.IO.Path.Combine(rootPath, path);
        }

        static string GetRelativePath(string rootPath, string path)
        {
            var fullRootPath = rootPath;
            if (path.StartsWith(fullRootPath, StringComparison.Ordinal))
                path = path.Remove(0, fullRootPath.Length);
            return path.TrimEnd(System.IO.Path.DirectorySeparatorChar);
        }

        public IEnumerable<FileSystemInfoContract> GetChildItem(DirectoryId parent)
        {
            if (parent == null)
                throw new ArgumentNullException(nameof(parent));
            if (string.IsNullOrEmpty(_rootPath))
                throw new InvalidOperationException($"{nameof(_rootPath)} not initialized".ToString(CultureInfo.CurrentCulture));

            var effectivePath = GetFullPath(_rootPath, parent.Value);

            var directory = _rootFolder.GetFolderByPath(effectivePath);

            if (directory.Exists())
                return directory.EnumerateDirectories()
                    .Select(d => new DirectoryInfoContract(
                        GetRelativePath(_rootPath, d.FullName),
                        d.Name,
                        d.CreationTime,
                        d.LastWriteTime))
                    .Cast<FileSystemInfoContract>()
                    .Concat(directory.EnumerateFiles()
                        .Select(f => new FileInfoContract(
                            GetRelativePath(_rootPath, f.FullName),
                            f.Name,
                            f.CreationTime,
                            f.LastWriteTime,
                            (FileSize)f.Size, null))
                        .Cast<FileSystemInfoContract>());
            else
                return Array.Empty<FileSystemInfoContract>();
        }

        public void ClearContent(FileId target)
        {
            if (target == null)
                throw new ArgumentNullException(nameof(target));
            if (string.IsNullOrEmpty(_rootPath))
                throw new InvalidOperationException($"{nameof(_rootPath)} not initialized".ToString(CultureInfo.CurrentCulture));

            var file = GetFile(target);

            file.Clear();
        }

        MemoryFile GetFile(FileId fileId)
        {
            var file = TryGetFile(fileId);
            if (!file.Exists())
                throw new System.IO.FileNotFoundException(string.Empty, fileId.Value);
            return file;
        }

        MemoryFile TryGetFile(FileId fileId)
        {
            var effectivePath = GetFullPath(_rootPath, fileId.Value);
            var directory = _rootFolder.GetFolderByPath(effectivePath.GetPathPart());
            return directory.FetchFile(fileId.Value.GetFilenamePart());
        }

        public System.IO.Stream GetContent(FileId source)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));
            if (string.IsNullOrEmpty(_rootPath))
                throw new InvalidOperationException($"{nameof(_rootPath)} not initialized".ToString(CultureInfo.CurrentCulture));

            var file = GetFile(source);

            return new System.IO.BufferedStream(file.OpenRead());
        }

        public void SetContent(FileId target, System.IO.Stream content, IProgress<ProgressValue> progress)
        {
            if (target == null)
                throw new ArgumentNullException(nameof(target));
            if (content == null)
                throw new ArgumentNullException(nameof(content));
            if (string.IsNullOrEmpty(_rootPath))
                throw new InvalidOperationException($"{nameof(_rootPath)} not initialized".ToString(CultureInfo.CurrentCulture));

            var file = GetFile(target);
            file.SetContent(content);
        }

        public FileSystemInfoContract CopyItem(FileSystemId source, string copyName, DirectoryId destination, bool recurse)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));
            if (string.IsNullOrEmpty(copyName))
                throw new ArgumentNullException(nameof(copyName));
            if (destination == null)
                throw new ArgumentNullException(nameof(destination));
            if (string.IsNullOrEmpty(_rootPath))
                throw new InvalidOperationException($"{nameof(_rootPath)} not initialized".ToString(CultureInfo.CurrentCulture));

            var effectivePath = GetFullPath(_rootPath, source.Value);
            var destinationPath = destination.Value;
            if (System.IO.Path.IsPathRooted(destinationPath))
                destinationPath = destinationPath.Remove(0, System.IO.Path.GetPathRoot(destinationPath).Length);
            var effectiveCopyPath = GetFullPath(_rootPath, System.IO.Path.Combine(destinationPath, copyName));

            var directory = _rootFolder.GetFolderByPath(effectivePath);
            if (directory.Exists())
            {
                var directoryCopy = _rootFolder.GetFolderByPath(effectiveCopyPath);
                if (!directoryCopy.Exists())
                {
                    _rootFolder.CreatePath(effectiveCopyPath);
                    directoryCopy = _rootFolder.GetFolderByPath(effectiveCopyPath);
                }
                directory.CopyTo(directoryCopy, recurse);
                return new DirectoryInfoContract(
                    GetRelativePath(_rootPath, directoryCopy.FullName),
                    directoryCopy.Name,
                    directoryCopy.CreationTime,
                    directoryCopy.LastWriteTime);
            }

            var file = TryGetFile(new FileId(source.Value));
            if (file.Exists())
            {
                var parentFolderPath = effectiveCopyPath.GetPathPart();
                var parentFolder = _rootFolder.GetFolderByPath(parentFolderPath); // destinationPath
                if (!parentFolder.Exists())
                {
                    _rootFolder.CreatePath(parentFolderPath); // destinationPath
                    parentFolder = _rootFolder.GetFolderByPath(parentFolderPath); // destinationPath
                }
                var fileCopy = file.CopyTo(parentFolder, copyName);
                return new FileInfoContract(GetRelativePath(_rootPath, fileCopy.FullName),
                    fileCopy.Name,
                    fileCopy.CreationTime,
                    fileCopy.LastWriteTime,
                    (FileSize)fileCopy.Size, null);
            }

            throw new NotSupportedException(string.Format(CultureInfo.CurrentCulture, PATH_NOT_FOUND, source.Value));
        }

        public FileSystemInfoContract MoveItem(FileSystemId source, string moveName, DirectoryId destination)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));
            if (string.IsNullOrEmpty(moveName))
                throw new ArgumentNullException(nameof(moveName));
            if (destination == null)
                throw new ArgumentNullException(nameof(destination));
            if (string.IsNullOrEmpty(_rootPath))
                throw new InvalidOperationException($"{nameof(_rootPath)} not initialized".ToString(CultureInfo.CurrentCulture));

            var effectiveSourcePath = GetFullPath(_rootPath, source.Value);
            var destinationPath = destination.Value;
            if (System.IO.Path.IsPathRooted(destinationPath))
                destinationPath = destinationPath.Remove(0, System.IO.Path.GetPathRoot(destinationPath).Length);
            var effectiveMovePath = GetFullPath(_rootPath, System.IO.Path.Combine(destinationPath, moveName));

            var parentPath = effectiveMovePath.GetPathPart();
            MemoryFolder newParent = _rootFolder.GetFolderByPath(parentPath);
            if (!newParent.Exists())
                throw new System.IO.DirectoryNotFoundException($"Directory does not exist: {parentPath}");

            var directory = _rootFolder.GetFolderByPath(effectiveSourcePath);
            if (directory.Exists())
            {
                directory.MoveTo(newParent, moveName);
                return new DirectoryInfoContract(GetRelativePath(_rootPath, directory.FullName),
                    directory.Name,
                    directory.CreationTime,
                    directory.LastWriteTime);
            }

            var file = TryGetFile(new FileId(source.Value));
            if (file.Exists())
            {
                file.MoveTo(newParent, moveName);
                return new FileInfoContract(GetRelativePath(_rootPath, file.FullName),
                    file.Name,
                    file.CreationTime,
                    file.LastWriteTime,
                    (FileSize)file.Size, null);
            }

            throw new NotSupportedException(string.Format(CultureInfo.CurrentCulture, PATH_NOT_FOUND, source.Value));
        }

        public DirectoryInfoContract NewDirectoryItem(DirectoryId parent, string name)
        {
            if (parent == null)
                throw new ArgumentNullException(nameof(parent));
            if (string.IsNullOrEmpty(name))
                throw new ArgumentNullException(nameof(name));
            if (string.IsNullOrEmpty(_rootPath))
                throw new InvalidOperationException($"{nameof(_rootPath)} not initialized".ToString(CultureInfo.CurrentCulture));

            var effectivePath = GetFullPath(_rootPath, System.IO.Path.Combine(parent.Value, name));

            var directory = _rootFolder.GetFolderByPath(effectivePath);
            if (directory.Exists())
                throw new NotSupportedException(string.Format(CultureInfo.CurrentCulture, DUPLICATE_PATH, effectivePath));

            _rootFolder.CreatePath(effectivePath);
            directory = _rootFolder.GetFolderByPath(effectivePath);

            return new DirectoryInfoContract(GetRelativePath(_rootPath, directory.FullName),
                directory.Name,
                directory.CreationTime,
                directory.LastWriteTime);
        }

        public FileInfoContract NewFileItem(DirectoryId parent,
            string name, System.IO.Stream content, IProgress<ProgressValue> progress)
        {
            if (parent == null)
                throw new ArgumentNullException(nameof(parent));
            if (string.IsNullOrEmpty(name))
                throw new ArgumentNullException(nameof(name));
            if (string.IsNullOrEmpty(_rootPath))
                throw new InvalidOperationException($"{nameof(_rootPath)} not initialized".ToString(CultureInfo.CurrentCulture));

            var file = TryGetFile(new FileId(System.IO.Path.Combine(parent.Value, name)));
            if (file.Exists())
                throw new NotSupportedException(string.Format(CultureInfo.CurrentCulture, DUPLICATE_PATH, parent.Value));

            // var parentPath = parent.Value; 
            var parentPath = GetFullPath(_rootPath, parent.Value);
            var parentDir = _rootFolder.GetFolderByPath(parentPath);
            file = MemoryFile.New(parentDir, name);

            if (content != null)
                content.CopyTo(file);

            return new FileInfoContract(GetRelativePath(_rootPath, file.FullName),
                file.Name,
                file.CreationTime,
                file.LastWriteTime,
                (FileSize)file.Size, null);
        }

        public void RemoveItem(FileSystemId target, bool recurse)
        {
            if (target == null)
                throw new ArgumentNullException(nameof(target));
            if (string.IsNullOrEmpty(_rootPath))
                throw new InvalidOperationException($"{nameof(_rootPath)} not initialized".ToString(CultureInfo.CurrentCulture));

            var effectivePath = GetFullPath(_rootPath, target.Value);

            var directory = _rootFolder.GetFolderByPath(effectivePath);
            if (directory.Exists())
            {
                //directory.Delete(recurse);
                directory.Parent.Children.Remove(directory);
                return;
            }

            var file = TryGetFile(new FileId(target.Value));
            if (file.Exists())
            {
                file.Parent.Children.Remove(file);
                return;
            }

            throw new NotSupportedException(string.Format(CultureInfo.CurrentCulture, PATH_NOT_FOUND, target.Value));
        }

        public FileSystemInfoContract RenameItem(FileSystemId target, string newName)
        {
            if (target == null)
                throw new ArgumentNullException(nameof(target));
            if (string.IsNullOrEmpty(newName))
                throw new ArgumentNullException(nameof(newName));
            if (string.IsNullOrEmpty(_rootPath))
                throw new InvalidOperationException($"{nameof(_rootPath)} not initialized".ToString(CultureInfo.CurrentCulture));

            var effectivePath = GetFullPath(_rootPath, target.Value);
            var newPath = GetFullPath(_rootPath, System.IO.Path.Combine(System.IO.Path.GetDirectoryName(target.Value), newName));

            var destination = _rootFolder.GetFolderByPath(newPath.GetPathPart());

            var directory = _rootFolder.GetFolderByPath(effectivePath);
            if (directory.Exists())
            {
                directory.MoveTo(destination, newName);
                return new DirectoryInfoContract(GetRelativePath(_rootPath, directory.FullName),
                    directory.Name,
                    directory.CreationTime,
                    directory.LastWriteTime);
            }

            var file = TryGetFile(new FileId(target.Value));
            if (file.Exists())
            {
                file.MoveTo(destination, newName);
                return new FileInfoContract(GetRelativePath(_rootPath, file.FullName),
                    file.Name,
                    file.CreationTime,
                    file.LastWriteTime,
                    (FileSize)file.Size, null);
            }

            throw new NotSupportedException(string.Format(CultureInfo.CurrentCulture, PATH_NOT_FOUND, target.Value));
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Justification = "Debugger Display")]
        [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
        string DebuggerDisplay() => $"{nameof(MemoryGateway)} rootPath='{_rootPath}'".ToString(CultureInfo.CurrentCulture);
    }
}
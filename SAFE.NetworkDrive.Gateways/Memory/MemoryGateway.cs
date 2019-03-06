using SAFE.Filesystem.Interface.IO;
using SAFE.NetworkDrive.Interface;
using SAFE.NetworkDrive.Interface.Composition;
using SAFE.NetworkDrive.Interface.IO;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace SAFE.NetworkDrive.Gateways.Memory
{
    [System.Diagnostics.DebuggerDisplay("{DebuggerDisplay(),nq}")]
    public sealed class MemoryGateway : ICloudGateway
    {
        static readonly MemoryFolder _root = new MemoryFolder(null, string.Empty);
        

        public const string PARAMETER_ROOT = "root";
        const string PATH_NOT_FOUND = "Path '{0}' does not exist";
        const string DUPLICATE_PATH = "'{0}' is already present";
        string _rootPath;

        public bool TryAuthenticate(RootName root, string apiKey, IDictionary<string, string> parameters) => true;

        // DONE
        public DriveInfoContract GetDrive(RootName root, string apiKey, IDictionary<string, string> parameters)
        {
            if (root == null)
                throw new ArgumentNullException(nameof(root));
            if (parameters?.TryGetValue(PARAMETER_ROOT, out _rootPath) != true)
                throw new ArgumentException($"Required {PARAMETER_ROOT} missing in {nameof(parameters)}".ToString(CultureInfo.CurrentCulture));
            if (string.IsNullOrEmpty(_rootPath))
                throw new ArgumentException($"{PARAMETER_ROOT} cannot be empty".ToString(CultureInfo.CurrentCulture));

            var drive = new MemDrive(_root);
            return new DriveInfoContract(root.Value, 
                drive.AvailableFreeSpace, 
                drive.TotalSize - drive.AvailableFreeSpace);
        }

        class MemDrive
        {
            readonly MemoryFolder _root;

            public MemDrive(MemoryFolder root)
            {
                _root = root;
            }
            public long TotalSize => System.Diagnostics.Process.GetCurrentProcess().VirtualMemorySize64;// Environment.WorkingSet;
            public long AvailableFreeSpace => long.MaxValue;//TotalSize - (long)_root.UsedSize;
        }

        // SEMI DONE
        public RootDirectoryInfoContract GetRoot(RootName root, string apiKey, IDictionary<string, string> parameters)
        {
            if (root == null)
                throw new ArgumentNullException(nameof(root));
            if (string.IsNullOrEmpty(_rootPath))
                throw new InvalidOperationException($"{nameof(_rootPath)} not initialized".ToString(CultureInfo.CurrentCulture));

            var id = System.IO.Path.DirectorySeparatorChar.ToString();

            return new RootDirectoryInfoContract(id, _root.CreationTime, _root.LastWriteTime);
        }

        // NOT DONE
        static string GetFullPath(string rootPath, string path)
        {
            //if (System.IO.Path.IsPathRooted(path))
            //    path = path.Remove(0, System.IO.Path.GetPathRoot(path).Length);
            //return System.IO.Path.Combine(System.IO.Path.GetFullPath(rootPath), path);
            if (System.IO.Path.IsPathRooted(path))
                return path;
            return System.IO.Path.Combine(rootPath, path);
        }

        // MAYBE DONE
        static string GetRelativePath(string rootPath, string path)
        {
            //var fullRootPath = System.IO.Path.GetFullPath(rootPath);
            var fullRootPath = rootPath;
            if (path.StartsWith(fullRootPath, StringComparison.Ordinal))
                path = path.Remove(0, fullRootPath.Length);
            return path.TrimEnd(System.IO.Path.DirectorySeparatorChar);
        }

        // DONE
        public IEnumerable<FileSystemInfoContract> GetChildItem(RootName root, DirectoryId parent)
        {
            if (root == null)
                throw new ArgumentNullException(nameof(root));
            if (parent == null)
                throw new ArgumentNullException(nameof(parent));
            if (string.IsNullOrEmpty(_rootPath))
                throw new InvalidOperationException($"{nameof(_rootPath)} not initialized".ToString(CultureInfo.CurrentCulture));

            var effectivePath = GetFullPath(_rootPath, parent.Value);

            var directory = _root.GetFolderByPath(effectivePath);

            if (directory.Exists())
                return directory.EnumerateDirectories().Select(d => new DirectoryInfoContract(GetRelativePath(_rootPath, d.FullName), d.Name, d.CreationTime, d.LastWriteTime)).Cast<FileSystemInfoContract>().Concat(
                    directory.EnumerateFiles().Select(f => new FileInfoContract(GetRelativePath(_rootPath, f.FullName), f.Name, f.CreationTime, f.LastWriteTime, (FileSize)f.Size, null)).Cast<FileSystemInfoContract>());
            else
                return Array.Empty<FileSystemInfoContract>();
        }

        // DONE
        public void ClearContent(RootName root, FileId target)
        {
            if (root == null)
                throw new ArgumentNullException(nameof(root));
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
            var directory = _root.GetFolderByPath(effectivePath.GetPathPart());
            return directory.FetchFile(fileId.Value.GetFilenamePart());
        }

        // DONE
        public System.IO.Stream GetContent(RootName root, FileId source)
        {
            if (root == null)
                throw new ArgumentNullException(nameof(root));
            if (source == null)
                throw new ArgumentNullException(nameof(source));
            if (string.IsNullOrEmpty(_rootPath))
                throw new InvalidOperationException($"{nameof(_rootPath)} not initialized".ToString(CultureInfo.CurrentCulture));

            var file = GetFile(source);
            
            return new System.IO.BufferedStream(file.OpenRead());
        }

        // MAYBE DONE
        public void SetContent(RootName root, FileId target, System.IO.Stream content, IProgress<ProgressValue> progress)
        {
            if (root == null)
                throw new ArgumentNullException(nameof(root));
            if (target == null)
                throw new ArgumentNullException(nameof(target));
            if (content == null)
                throw new ArgumentNullException(nameof(content));
            if (string.IsNullOrEmpty(_rootPath))
                throw new InvalidOperationException($"{nameof(_rootPath)} not initialized".ToString(CultureInfo.CurrentCulture));

            var file = GetFile(target);
            file.SetContent(content);
        }

        // MAYBE DONE
        public FileSystemInfoContract CopyItem(RootName root, FileSystemId source, string copyName, DirectoryId destination, bool recurse)
        {
            if (root == null)
                throw new ArgumentNullException(nameof(root));
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

            //var directory = new DirectoryInfo(effectivePath);
            var directory = _root.GetFolderByPath(effectivePath);
            if (directory.Exists())
            {
                var directoryCopy = _root.GetFolderByPath(effectiveCopyPath);
                if (!directoryCopy.Exists())
                {
                    _root.CreatePath(effectiveCopyPath);
                    directoryCopy = _root.GetFolderByPath(effectiveCopyPath);
                }
                directory.CopyTo(directoryCopy, recurse);
                return new DirectoryInfoContract(
                    GetRelativePath(_rootPath, directoryCopy.FullName),
                    directoryCopy.Name, 
                    directoryCopy.CreationTime, 
                    directoryCopy.LastWriteTime);
            }

            //var file = new FileInfo(effectivePath);
            var file = TryGetFile(new FileId(source.Value));
            if (file.Exists())
            {
                var parentFolderPath = effectiveCopyPath.GetPathPart();
                var parentFolder = _root.GetFolderByPath(parentFolderPath); // destinationPath
                if (!parentFolder.Exists())
                {
                    _root.CreatePath(parentFolderPath); // destinationPath
                    parentFolder = _root.GetFolderByPath(parentFolderPath); // destinationPath
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

        // DONE
        public FileSystemInfoContract MoveItem(RootName root, FileSystemId source, string moveName, DirectoryId destination)
        {
            if (root == null)
                throw new ArgumentNullException(nameof(root));
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
            MemoryFolder newParent = _root.GetFolderByPath(parentPath);
            if (!newParent.Exists())
                throw new System.IO.DirectoryNotFoundException($"Directory does not exist: {parentPath}");

            //var directory = new DirectoryInfo(effectivePath);
            var directory = _root.GetFolderByPath(effectiveSourcePath);
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

        // DONE
        public DirectoryInfoContract NewDirectoryItem(RootName root, DirectoryId parent, string name)
        {
            if (root == null)
                throw new ArgumentNullException(nameof(root));
            if (parent == null)
                throw new ArgumentNullException(nameof(parent));
            if (string.IsNullOrEmpty(name))
                throw new ArgumentNullException(nameof(name));
            if (string.IsNullOrEmpty(_rootPath))
                throw new InvalidOperationException($"{nameof(_rootPath)} not initialized".ToString(CultureInfo.CurrentCulture));

            var effectivePath = GetFullPath(_rootPath, System.IO.Path.Combine(parent.Value, name));

            //var directory = new DirectoryInfo(effectivePath);
            var directory = _root.GetFolderByPath(effectivePath);
            if (directory.Exists())
                throw new NotSupportedException(string.Format(CultureInfo.CurrentCulture, DUPLICATE_PATH, effectivePath));

            _root.CreatePath(effectivePath);
            directory = _root.GetFolderByPath(effectivePath);

            return new DirectoryInfoContract(GetRelativePath(_rootPath, directory.FullName), 
                directory.Name, 
                directory.CreationTime, 
                directory.LastWriteTime);
        }

        // DONE
        public FileInfoContract NewFileItem(RootName root, DirectoryId parent, 
            string name, System.IO.Stream content, IProgress<ProgressValue> progress)
        {
            if (root == null)
                throw new ArgumentNullException(nameof(root));
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
            var parentDir = _root.GetFolderByPath(parentPath);
            file = MemoryFile.New(parentDir, name);
            
            if (content != null)
                content.CopyTo(file);

            return new FileInfoContract(GetRelativePath(_rootPath, file.FullName), 
                file.Name, 
                file.CreationTime, 
                file.LastWriteTime, 
                (FileSize)file.Size, null);
        }

        // DONE
        public void RemoveItem(RootName root, FileSystemId target, bool recurse)
        {
            if (root == null)
                throw new ArgumentNullException(nameof(root));
            if (target == null)
                throw new ArgumentNullException(nameof(target));
            if (string.IsNullOrEmpty(_rootPath))
                throw new InvalidOperationException($"{nameof(_rootPath)} not initialized".ToString(CultureInfo.CurrentCulture));

            var effectivePath = GetFullPath(_rootPath, target.Value);

            var directory = _root.GetFolderByPath(effectivePath);
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

        // DONE
        public FileSystemInfoContract RenameItem(RootName root, FileSystemId target, string newName)
        {
            if (root == null)
                throw new ArgumentNullException(nameof(root));
            if (target == null)
                throw new ArgumentNullException(nameof(target));
            if (string.IsNullOrEmpty(newName))
                throw new ArgumentNullException(nameof(newName));
            if (string.IsNullOrEmpty(_rootPath))
                throw new InvalidOperationException($"{nameof(_rootPath)} not initialized".ToString(CultureInfo.CurrentCulture));

            var effectivePath = GetFullPath(_rootPath, target.Value);
            var newPath = GetFullPath(_rootPath, System.IO.Path.Combine(System.IO.Path.GetDirectoryName(target.Value), newName));

            var destination = _root.GetFolderByPath(newPath.GetPathPart());

            var directory = _root.GetFolderByPath(effectivePath);
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
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
using System.IO;
using System.Linq;
using System.Security.AccessControl;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Mono.Unix.Native;
using SAFE.Filesystem.Interface.IO;
using SAFE.NetworkDrive.IO;

namespace SAFE.NetworkDrive
{
    [System.Diagnostics.DebuggerDisplay("{DebuggerDisplay,nq}")]
    internal partial class CloudOperations
    {
        [System.Diagnostics.DebuggerDisplay("{DebuggerDisplay,nq}")]
        class StreamContext : IDisposable
        {
            public CloudFileNode File { get; }
            public FileAccess Access { get; }
            public Stream Stream { get; set; }
            public Task Task { get; set; }
            public bool IsLocked { get; set; }
            public bool CanWriteDelayed => Access.HasFlag(FileAccess.WriteData) && (Stream?.CanRead ?? false) && Task == null;

            public StreamContext(CloudFileNode file, FileAccess access)
            {
                File = file;
                Access = access;
            }

            public void Dispose() => Stream?.Dispose();
            public override string ToString() => DebuggerDisplay;

            [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Justification = "Debugger Display")]
            [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
            string DebuggerDisplay => $"{nameof(StreamContext)} {File.Name} [{Access}] [{nameof(Stream.Length)}={((Stream?.CanSeek ?? false) ? Stream.Length : 0)}] [{nameof(Task.Status)}={Task?.Status}] {nameof(IsLocked)}={IsLocked}".ToString(CultureInfo.CurrentCulture);
        }

        ICloudDrive _drive;
        CloudDirectoryNode _root;
        ILogger _logger;

        static readonly IList<FileInformation> emptyDirectoryDefaultFiles = new[] { ".", ".." }.Select(fileName =>
            new FileInformation() { FileName = fileName, Attributes = FileAttributes.Directory, CreationTime = DateTime.Today, LastWriteTime = DateTime.Today, LastAccessTime = DateTime.Today }
        ).ToList();

        public CloudOperations(ICloudDrive drive, ILogger logger)
        {
            _drive = drive ?? throw new ArgumentNullException(nameof(drive));
            _logger = logger;
        }

        CloudItemNode GetItem(string fileName)
        {
            var result = _root ?? (_root = new CloudDirectoryNode(_drive.GetRoot())) as CloudItemNode;

            var pathSegments = new Queue<string>(fileName.Split(new[] { Path.DirectorySeparatorChar }, StringSplitOptions.RemoveEmptyEntries));

            while (result != null && pathSegments.Count > 0)
                result = (result as CloudDirectoryNode)?.GetChildItemByName(_drive, pathSegments.Dequeue());

            return result;
        }

        public void Cleanup(string fileName, FuseFileInfo info)
        {
            if (info == null)
                throw new ArgumentNullException(nameof(info));

            if (info.DeleteOnClose)
            {
                (GetItem(fileName) as CloudFileNode)?.Remove(_drive);
            }
            else if (!info.IsDirectory)
            {
                var context = info.Context as StreamContext;
                if (context?.CanWriteDelayed ?? false)
                {
                    context.Stream.Seek(0, SeekOrigin.Begin);
                    context.Task = Task.Run(() => {
                        try
                        {
                            context.File.SetContent(_drive, context.Stream);
                        }
                        catch (Exception ex)
                        {
                            if (!(ex is UnauthorizedAccessException) && !((uint)((ex as IOException)?.HResult ?? 0) == 0x80070020))
                                context.File.Remove(_drive);
                            _logger?.Error($"{nameof(context.File.SetContent)} failed on file '{fileName}' with {ex.GetType().Name} '{ex.Message}'".ToString(CultureInfo.CurrentCulture));
                            throw;
                        }
                    })
                    .ContinueWith(t => _logger?.Debug($"{nameof(context.File.SetContent)} finished on file '{fileName}'".ToString(CultureInfo.CurrentCulture)), TaskContinuationOptions.OnlyOnRanToCompletion);
                }

                if (context?.Task != null)
                {
                    context.Task.Wait();

                    if (context.Task.IsCompleted)
                        AsTrace(nameof(Cleanup), fileName, info, FuseResult.Success);
                    else
                        AsError(nameof(Cleanup), fileName, info, FuseResult.Error);
                    context.Dispose();
                    info.Context = null;
                    return;
                }
            }

            AsTrace(nameof(Cleanup), fileName, info, FuseResult.Success);
        }

        public void CloseFile(string fileName, FuseFileInfo info)
        {
            if (info == null)
                throw new ArgumentNullException(nameof(info));

            AsTrace(nameof(CloseFile), fileName, info, FuseResult.Success);

            var context = info.Context as StreamContext;
            context?.Dispose();
        }

        public Errno CreateFile(string fileName, FileAccess access, FileShare share, FileMode mode, FileOptions options, FileAttributes attributes, FuseFileInfo info)
        {
            if (info == null)
                throw new ArgumentNullException(nameof(info));

            // HACK: Fix for Bug in Dokany related to a missing trailing slash for directory names
            if (string.IsNullOrEmpty(fileName))
                fileName = @"\";
            // HACK: Fix for Bug in Dokany related to a call to CreateFile with a fileName of '\*'
            else if (fileName == @"\*" && access == FileAccess.ReadAttributes)
                return AsTrace(nameof(CreateFile), fileName, info, access, share, mode, options, attributes, FuseResult.Success);

            if (fileName == @"\")
            {
                info.IsDirectory = true;
                return AsTrace(nameof(CreateFile), fileName, info, access, share, mode, options, attributes, FuseResult.Success);
            }

            fileName = fileName.TrimEnd(Path.DirectorySeparatorChar);

            var parent = GetItem(Path.GetDirectoryName(fileName)) as CloudDirectoryNode;
            if (parent == null)
                return AsDebug(nameof(CreateFile), fileName, info, access, share, mode, options, attributes, FuseResult.PathNotFound);

            var itemName = Path.GetFileName(fileName);
            var item = parent.GetChildItemByName(_drive, itemName);
            CloudFileNode fileItem;
            switch (mode)
            {
                case FileMode.Create:
                    fileItem = item as CloudFileNode;
                    if (fileItem != null)
                        fileItem.Truncate(_drive);
                    else
                        fileItem = parent.NewFileItem(_drive, itemName);

                    info.Context = new StreamContext(fileItem, FileAccess.WriteData);

                    return AsTrace(nameof(CreateFile), fileName, info, access, share, mode, options, attributes, FuseResult.Success);
                case FileMode.Open:
                    fileItem = item as CloudFileNode;
                    if (fileItem != null)
                    {
                        if (access.HasFlag(FileAccess.ReadData) || access.HasFlag(FileAccess.GenericRead) || access.HasFlag(FileAccess.ReadAttributes))
                            info.Context = new StreamContext(fileItem, FileAccess.ReadData);
                        else if (access.HasFlag(FileAccess.WriteData))
                            info.Context = new StreamContext(fileItem, FileAccess.WriteData);
                        else if (access.HasFlag(FileAccess.Delete))
                            info.Context = new StreamContext(fileItem, FileAccess.Delete);
                        //                        else if (!access.HasFlag(FileAccess.ReadAttributes))
                        else if (!access.HasFlag(FileAccess.ReadAttributes) && !access.HasFlag(FileAccess.ReadPermissions))
                            return AsDebug(nameof(CreateFile), fileName, info, access, share, mode, options, attributes, FuseResult.NotImplemented);
                    }
                    else
                        info.IsDirectory = item != null;

                    if (item != null)
                        return AsTrace(nameof(CreateFile), fileName, info, access, share, mode, options, attributes, FuseResult.Success);
                    else
                        return AsError(nameof(CreateFile), fileName, info, access, share, mode, options, attributes, FuseResult.FileNotFound);
                case FileMode.OpenOrCreate:
                    fileItem = item as CloudFileNode ?? parent.NewFileItem(_drive, itemName);

                    if (access.HasFlag(FileAccess.ReadData) && !access.HasFlag(FileAccess.WriteData))
                        info.Context = new StreamContext(fileItem, FileAccess.ReadData);
                    else
                        info.Context = new StreamContext(fileItem, FileAccess.WriteData);

                    return AsTrace(nameof(CreateFile), fileName, info, access, share, mode, options, attributes, FuseResult.Success);
                case FileMode.CreateNew:
                    if (item != null)
                        return AsDebug(nameof(CreateFile), fileName, info, access, share, mode, options, attributes, info.IsDirectory ? FuseResult.AlreadyExists : FuseResult.FileExists);

                    if (info.IsDirectory)
                        parent.NewDirectoryItem(_drive, itemName);
                    else
                    {
                        fileItem = parent.NewFileItem(_drive, itemName);
                        info.Context = new StreamContext(fileItem, FileAccess.WriteData);
                    }
                    return AsTrace(nameof(CreateFile), fileName, info, access, share, mode, options, attributes, FuseResult.Success);
                case FileMode.Append:
                    return AsError(nameof(CreateFile), fileName, info, access, share, mode, options, attributes, FuseResult.NotImplemented);
                case FileMode.Truncate:
                    fileItem = item as CloudFileNode;
                    if (fileItem == null)
                        return AsDebug(nameof(CreateFile), fileName, info, access, share, mode, options, attributes, FuseResult.FileNotFound);

                    fileItem.Truncate(_drive);
                    info.Context = new StreamContext(fileItem, FileAccess.WriteData);

                    return AsTrace(nameof(CreateFile), fileName, info, access, share, mode, options, attributes, FuseResult.Success);
                    //return AsError(nameof(CreateFile), fileName, info, access, share, mode, options, attributes, DokanResult.NotImplemented);
                default:
                    return AsError(nameof(CreateFile), fileName, info, access, share, mode, options, attributes, FuseResult.NotImplemented);
            }
        }

        public Errno DeleteDirectory(string fileName, FuseFileInfo info)
        {
            var item = GetItem(fileName) as CloudDirectoryNode;
            if (item == null)
                return AsDebug(nameof(DeleteDirectory), fileName, info, FuseResult.PathNotFound);
            if (item.GetChildItems(_drive).Any())
                return AsDebug(nameof(DeleteDirectory), fileName, info, FuseResult.DirectoryNotEmpty);

            item.Remove(_drive);

            return AsTrace(nameof(DeleteDirectory), fileName, info, FuseResult.Success);
        }

        public Errno DeleteFile(string fileName, FuseFileInfo info)
        {
            if (info == null)
                throw new ArgumentNullException(nameof(info));

            ((StreamContext)info.Context).File.Remove(_drive);

            return AsTrace(nameof(DeleteFile), fileName, info, FuseResult.Success);
        }

        public Errno FindFiles(string fileName, out IList<FileInformation> files, FuseFileInfo info)
        {
            var parent = GetItem(fileName) as CloudDirectoryNode;

            var childItems = parent.GetChildItems(_drive).Where(i => i.IsResolved).ToList();
            files = childItems.Any()
                ? childItems.Select(i => new FileInformation()
                {
                    FileName = i.Name,
                    Length = (i as CloudFileNode)?.Contract.Size ?? FileSize.Empty,
                    Attributes = i is CloudDirectoryNode ? FileAttributes.Directory : FileAttributes.NotContentIndexed,
                    CreationTime = i.Contract.Created.DateTime,
                    LastWriteTime = i.Contract.Updated.DateTime,
                    LastAccessTime = i.Contract.Updated.DateTime
                }).ToList()
                : emptyDirectoryDefaultFiles;

            return AsTrace(nameof(FindFiles), fileName, info, FuseResult.Success, $"out [{files.Count}]".ToString(CultureInfo.CurrentCulture));
        }

        public Errno FindFilesWithPattern(string fileName, string searchPattern, out IList<FileInformation> files, FuseFileInfo info)
        {
            if (searchPattern == null)
                throw new ArgumentNullException(nameof(searchPattern));

            var parent = GetItem(fileName) as CloudDirectoryNode;

            var childItems = parent
                .GetChildItems(_drive)
                //.Where(i => i.IsResolved)
                .ToList();

            files = childItems.Any()
                ? childItems
                    .Where(i => Regex.IsMatch(i.Name, searchPattern.Contains('?') || searchPattern.Contains('*') ? searchPattern.Replace('?', '.').Replace("*", ".*") : "^" + searchPattern + "$"))
                    .Select(i => new FileInformation()
                    {
                        FileName = i.Name,
                        Length = (i as CloudFileNode)?.Contract.Size ?? FileSize.Empty,
                        Attributes = i is CloudDirectoryNode ? FileAttributes.Directory : FileAttributes.NotContentIndexed,
                        CreationTime = i.Contract.Created.DateTime,
                        LastWriteTime = i.Contract.Updated.DateTime,
                        LastAccessTime = i.Contract.Updated.DateTime
                    }).ToList()
                : emptyDirectoryDefaultFiles;

            return AsTrace(nameof(FindFilesWithPattern), fileName, info, FuseResult.Success, searchPattern, $"out [{files.Count}]".ToString(CultureInfo.CurrentCulture));
        }

        public Errno FindStreams(string fileName, out IList<FileInformation> streams, FuseFileInfo info)
        {
            streams = Enumerable.Empty<FileInformation>().ToList();
            return AsWarn(nameof(FindStreams), fileName, info, FuseResult.NotImplemented, $"out [{streams.Count}]".ToString(CultureInfo.CurrentCulture));
        }

        public Errno FlushFileBuffers(string fileName, FuseFileInfo info)
        {
            if (info == null)
                throw new ArgumentNullException(nameof(info));

            try
            {
                ((StreamContext)info.Context).Stream?.Flush();

                return AsTrace(nameof(FlushFileBuffers), fileName, info, FuseResult.Success);
            }
            catch (IOException)
            {
                return AsError(nameof(FlushFileBuffers), fileName, info, FuseResult.DiskFull);
            }
        }

        public Errno GetDiskFreeSpace(out long free, out long total, out long used, FuseFileInfo info)
        {
            free = _drive.Free ?? 0;
            used = _drive.Used ?? 0;
            total = free + used;

            return AsTrace(nameof(GetDiskFreeSpace), null, info, FuseResult.Success, $"out {free}", $"out {total}", $"out {used}".ToString(CultureInfo.CurrentCulture));
        }

        public Errno GetFileInformation(string fileName, out FileInformation fileInfo, FuseFileInfo info)
        {
            if (info == null)
                throw new ArgumentNullException(nameof(info));

            var item = GetItem(fileName);
            if (item == null)
            {
                fileInfo = default;
                return AsTrace(nameof(GetFileInformation), fileName, info, FuseResult.PathNotFound);
            }

            fileInfo = new FileInformation()
            {
                FileName = fileName,
                Length = (info.Context as StreamContext)?.Stream?.Length ?? (item as CloudFileNode)?.Contract.Size ?? FileSize.Empty,
                Attributes = item is CloudDirectoryNode ? FileAttributes.Directory : FileAttributes.NotContentIndexed,
                CreationTime = item.Contract.Created.DateTime,
                LastWriteTime = item.Contract.Updated.DateTime,
                LastAccessTime = item.Contract.Updated.DateTime
            };

            return AsTrace(nameof(GetFileInformation), fileName, info, FuseResult.Success, $"out {{{fileInfo.FileName}, [{fileInfo.Length}], [{fileInfo.Attributes}], {fileInfo.CreationTime}, {fileInfo.LastWriteTime}, {fileInfo.LastAccessTime}}}".ToString(CultureInfo.CurrentCulture));
        }

        //public Errno GetFileSecurity(string fileName, out FileSystemSecurity security, AccessControlSections sections, FuseFileInfo info)
        //{
        //    if (info == null)
        //        throw new ArgumentNullException(nameof(info));

        //    security = info.IsDirectory
        //        ? new DirectorySecurity() as FileSystemSecurity
        //        : new FileSecurity() as FileSystemSecurity;
        //    security.AddAccessRule(new FileSystemAccessRule(new System.Security.Principal.SecurityIdentifier(System.Security.Principal.WellKnownSidType.WorldSid, null), FileSystemRights.FullControl, AccessControlType.Allow));

        //    return AsTrace(nameof(GetFileSecurity), fileName, info, FuseResult.Success, $"out {security}", $"{sections}".ToString(CultureInfo.CurrentCulture));
        //}

        //public Errno GetVolumeInformation(out string volumeLabel, out FileSystemFeatures features, out string fileSystemName, out uint maximumComponentLength, FuseFileInfo info)
        //{
        //    volumeLabel = _drive.DisplayRoot;
        //    features = FileSystemFeatures.CaseSensitiveSearch | FileSystemFeatures.CasePreservedNames | FileSystemFeatures.UnicodeOnDisk |
        //               FileSystemFeatures.PersistentAcls | FileSystemFeatures.SupportsRemoteStorage;
        //    fileSystemName = "SAFE.NetworkDrive";
        //    maximumComponentLength = 256;

        //    return AsTrace(nameof(GetVolumeInformation), null, info, FuseResult.Success, $"out {volumeLabel}", $"out {features}", $"out {fileSystemName}".ToString(CultureInfo.CurrentCulture));
        //}

        public Errno LockFile(string fileName, long offset, long length, FuseFileInfo info)
        {
            if (info == null)
                throw new ArgumentNullException(nameof(info));

            var context = ((StreamContext)info.Context);

            if (context.IsLocked)
                return AsWarn(nameof(LockFile), fileName, info, FuseResult.AccessDenied, offset.ToString(CultureInfo.InvariantCulture), length.ToString(CultureInfo.InvariantCulture));

            context.IsLocked = true;
            return AsTrace(nameof(LockFile), fileName, info, FuseResult.Success, offset.ToString(CultureInfo.InvariantCulture), length.ToString(CultureInfo.InvariantCulture));
        }

        public Errno Mounted(FuseFileInfo info) => AsTrace(nameof(Mounted), null, info, FuseResult.Success);

        public Errno MoveFile(string oldName, string newName, bool replace, FuseFileInfo info)
        {
            var item = GetItem(oldName);
            if (item == null)
                return AsWarn(nameof(MoveFile), oldName, info, FuseResult.FileNotFound, newName, replace.ToString(CultureInfo.InvariantCulture));

            var destinationDirectory = GetItem(Path.GetDirectoryName(newName)) as CloudDirectoryNode;
            if (destinationDirectory == null)
                return AsWarn(nameof(MoveFile), oldName, info, FuseResult.PathNotFound, newName, replace.ToString(CultureInfo.InvariantCulture));

            item.Move(_drive, Path.GetFileName(newName), destinationDirectory);

            return AsTrace(nameof(MoveFile), oldName, info, FuseResult.Success, newName, replace.ToString(CultureInfo.InvariantCulture));
        }

        public Errno OpenDirectory(string fileName, FuseFileInfo info)
        {
            var item = GetItem(fileName) as CloudDirectoryNode;
            if (item == null)
                return AsDebug(nameof(OpenDirectory), fileName, info, FuseResult.PathNotFound);

            return AsTrace(nameof(OpenDirectory), fileName, info, FuseResult.Success);
        }

        public Errno ReadFile(string fileName, byte[] buffer, out int bytesRead, long offset, FuseFileInfo info)
        {
            if (buffer == null)
                throw new ArgumentNullException(nameof(buffer));
            if (offset < 0)
                throw new ArgumentOutOfRangeException(nameof(offset), string.Format(CultureInfo.CurrentCulture, "{0} must be non-negative", offset));
            if (info == null)
                throw new ArgumentNullException(nameof(info));

            var context = (StreamContext)info.Context;

            lock (context)
            {
                if (context.Stream == null)
                    try
                    {
                        context.Stream = Stream.Synchronized(context.File.GetContent(_drive));
                    }
                    catch (Exception ex)
                    {
                        bytesRead = 0;
                        return AsError(nameof(ReadFile), fileName, info, FuseResult.Error, offset.ToString(CultureInfo.InvariantCulture), $"out {bytesRead}".ToString(CultureInfo.InvariantCulture), $"{ex.GetType().Name} '{ex.Message}'".ToString(CultureInfo.CurrentCulture));
                    }

                context.Stream.Position = offset;
                bytesRead = context.Stream.Read(buffer, 0, buffer.Length);
            }

            return AsDebug(nameof(ReadFile), fileName, info, FuseResult.Success, offset.ToString(CultureInfo.InvariantCulture), $"out {bytesRead}".ToString(CultureInfo.InvariantCulture));
        }

        public Errno SetAllocationSize(string fileName, long length, FuseFileInfo info)
        {
            if (info == null)
                throw new ArgumentNullException(nameof(info));

            var context = (StreamContext)info.Context;
            if (length > 0)
            {
                if (context.Stream == null)
                {
                    var gatherStreams = new Stream[2];
                    ScatterGatherStreamFactory.CreateScatterGatherStreams((int)length, out Stream scatterStream, gatherStreams);

                    context.Stream = new ReadWriteSegregatingStream(scatterStream, gatherStreams[1]);

                    context.Task = Task.Run(() => {
                        try
                        {
                            context.File.SetContent(_drive, gatherStreams[0]);
                        }
                        catch (Exception ex)
                        {
                            if (!(ex is UnauthorizedAccessException))
                                context.File.Remove(_drive);
                            _logger.Error($"{nameof(context.File.SetContent)} failed on file '{fileName}' with {ex.GetType().Name} '{ex.Message}'".ToString(CultureInfo.CurrentCulture));
                            throw;
                        }
                    })
                    .ContinueWith(t => _logger.Debug($"{nameof(context.File.SetContent)} finished on file '{fileName}'".ToString(CultureInfo.CurrentCulture)), TaskContinuationOptions.OnlyOnRanToCompletion);
                }
                else
                {
                    if ((context.Stream as ReadWriteSegregatingStream)?.WriteStream is ScatterStream scatterStream)
                        scatterStream.Capacity = (int)length;
                }
            }

            return AsDebug(nameof(SetAllocationSize), fileName, info, FuseResult.Success, length.ToString(CultureInfo.InvariantCulture));
        }

        public Errno SetEndOfFile(string fileName, long length, FuseFileInfo info)
        {
            return AsDebug(nameof(SetEndOfFile), fileName, info, FuseResult.Success, length.ToString(CultureInfo.InvariantCulture));
        }

        public Errno SetFileAttributes(string fileName, FileAttributes attributes, FuseFileInfo info)
        {
            // TODO: Possibly return NotImplemented here
            return AsDebug(nameof(SetFileAttributes), fileName, info, FuseResult.Success, attributes.ToString());
        }

        //public Errno SetFileSecurity(string fileName, FileSystemSecurity security, AccessControlSections sections, FuseFileInfo info)
        //    => AsDebug(nameof(SetFileAttributes), fileName, info, FuseResult.NotImplemented, sections.ToString());

        public Errno SetFileTime(string fileName, DateTime? creationTime, DateTime? lastAccessTime, DateTime? lastWriteTime, FuseFileInfo info)
        {
            // TODO: Possibly return NotImplemented here
            return AsDebug(nameof(SetFileTime), fileName, info, FuseResult.Success, creationTime.ToString(), lastAccessTime.ToString(), lastWriteTime.ToString());
        }

        public Errno UnlockFile(string fileName, long offset, long length, FuseFileInfo info)
        {
            if (info == null)
                throw new ArgumentNullException(nameof(info));

            var context = ((StreamContext)info.Context);
            if (!context.IsLocked)
                return AsWarn(nameof(UnlockFile), fileName, info, FuseResult.AccessDenied, offset.ToString(CultureInfo.InvariantCulture), length.ToString(CultureInfo.InvariantCulture));

            context.IsLocked = false;
            return AsTrace(nameof(UnlockFile), fileName, info, FuseResult.Success, offset.ToString(CultureInfo.InvariantCulture), length.ToString(CultureInfo.InvariantCulture));
        }

        public Errno Unmounted(FuseFileInfo info)
        {
            if (info == null)
                throw new ArgumentNullException(nameof(info));

            var result = AsTrace(nameof(Unmounted), null, info, FuseResult.Success);

            _drive = null;
            _logger = null;

            return result;
        }

        public Errno WriteFile(string fileName, byte[] buffer, out int bytesWritten, long offset, FuseFileInfo info)
        {
            if (buffer == null)
                throw new ArgumentNullException(nameof(buffer));
            if (offset < 0)
                throw new ArgumentException(string.Format(CultureInfo.CurrentCulture, "{0} must be non-negative", offset), nameof(offset));
            if (info == null)
                throw new ArgumentNullException(nameof(info));

            var context = ((StreamContext)info.Context);

            lock (context)
            {
                if (context.Stream == null)
                    context.Stream = Stream.Synchronized(new MemoryStream());

                context.Stream.Position = offset;
                context.Stream.Write(buffer, 0, buffer.Length);
                bytesWritten = (int)(context.Stream.Position - offset);
            }

            return AsDebug(nameof(WriteFile), fileName, info, FuseResult.Success, offset.ToString(CultureInfo.InvariantCulture), $"out {bytesWritten}".ToString(CultureInfo.InvariantCulture));
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Justification = "Debugger Display")]
        [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
        string DebuggerDisplay => $"{nameof(CloudOperations)} drive={_drive} root={_root}".ToString(CultureInfo.CurrentCulture);
    }
}
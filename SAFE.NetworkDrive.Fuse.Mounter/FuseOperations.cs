using Mono.Fuse.NETStandard;
using Mono.Unix.Native;
using System;
using System.Collections.Generic;

namespace SAFE.NetworkDrive.Fuse
{
    public class FuseOperations : FileSystem
    {
        readonly CloudOperations _operations;

        public string BaseDir { get; set; }

        public FuseOperations(ISAFEDrive drive, ILogger logger)
            => _operations = new CloudOperations(drive, logger);

        // CONVERTED
        protected override Errno OnGetPathStatus(string path, out Stat buf)
        {
            var res = _operations.GetFileInformation(BaseDir + path, out var fileInfo, new FuseFileInfo());

            buf = fileInfo.GetStat();
            return res;

            //int r = Syscall.lstat(BaseDir + path, out buf);
            //return GetResult(r);
        }

        // CONVERTED
        protected override Errno OnAccessPath(string path, AccessModes mask)
        {
            return 0;
            //int r = Syscall.access(BaseDir + path, mask);
            //return GetResult(r);
        }

        // CONVERTED
        protected override Errno OnReadSymbolicLink(string path, out string target)
        {
            target = path;
            return 0;

            //target = null;
            //var buf = new StringBuilder(256);
            //do
            //{
            //    int r = Syscall.readlink(BaseDir + path, buf);
            //    if (r < 0)
            //        return Stdlib.GetLastError();
            //    else if (r == buf.Capacity)
            //        buf.Capacity *= 2;
            //    else
            //    {
            //        target = buf.ToString(0, r);
            //        return 0;
            //    }
            //} while (true);
        }

        // CONVERTED
        protected override Errno OnReadDirectory(string path, OpenedPathInfo fi,
                out IEnumerable<DirectoryEntry> paths)
        {
            _operations.FindFiles(BaseDir + path, out var files, new FuseFileInfo());

            var entries = new List<DirectoryEntry>();
            foreach (var result in files)
            {
                var entry = new DirectoryEntry(result.FileName);
                entry.Stat.st_ino = (ulong)result.FileName.GetHashCode();// de.d_ino;
                //entry.Stat.st_mode = (FilePermissions)(de.d_type << 12);
                entries.Add(entry);
            }

            paths = entries;
            return 0;

            //IntPtr dp = Syscall.opendir(BaseDir + path);
            //if (dp == IntPtr.Zero)
            //{
            //    paths = null;
            //    return Stdlib.GetLastError();
            //}

            //Dirent de;
            //var entries = new List<DirectoryEntry>();
            //while ((de = Syscall.readdir(dp)) != null)
            //{
            //    var entry = new DirectoryEntry(de.d_name);
            //    entry.Stat.st_ino = de.d_ino;
            //    entry.Stat.st_mode = (FilePermissions)(de.d_type << 12);
            //    entries.Add(entry);
            //}
            //Syscall.closedir(dp);

            //paths = entries;
            //return 0;
        }

        // CONVERTED
        protected override Errno OnCreateSpecialFile(string path, FilePermissions mode, ulong rdev)
        {
            return _operations.CreateFile(BaseDir + path,
                FileAccess.WriteData,
                System.IO.FileShare.ReadWrite,
                System.IO.FileMode.CreateNew,
                System.IO.FileOptions.None,
                System.IO.FileAttributes.NotContentIndexed, new FuseFileInfo());

            //int r;

            //// On Linux, this could just be `mknod(basedir+path, mode, rdev)' but this is
            //// more portable.
            //if ((mode & FilePermissions.S_IFMT) == FilePermissions.S_IFREG)
            //{
            //    r = Syscall.open(BaseDir + path, OpenFlags.O_CREAT | OpenFlags.O_EXCL |
            //            OpenFlags.O_WRONLY, mode);
            //    if (r >= 0)
            //        r = Syscall.close(r);
            //}
            //else if ((mode & FilePermissions.S_IFMT) == FilePermissions.S_IFIFO)
            //    r = Syscall.mkfifo(BaseDir + path, mode);
            //else
            //    r = Syscall.mknod(BaseDir + path, mode, rdev);

            //return GetResult(r);
        }

        // CONVERTED
        protected override Errno OnCreateDirectory(string path, FilePermissions mode)
        {
            return _operations.CreateFile(BaseDir + path,
                FileAccess.WriteData,
                System.IO.FileShare.ReadWrite,
                System.IO.FileMode.CreateNew,
                System.IO.FileOptions.None,
                System.IO.FileAttributes.Directory, new FuseFileInfo());

            //int r = Syscall.mkdir(BaseDir + path, mode);
            //return GetResult(r);
        }

        // CONVERTED
        protected override Errno OnRemoveFile(string path)
        {
            return _operations.DeleteFile(BaseDir + path, new FuseFileInfo());

            //int r = Syscall.unlink(BaseDir + path);
            //return GetResult(r);
        }

        // CONVERTED
        protected override Errno OnRemoveDirectory(string path)
        {
            return _operations.DeleteDirectory(BaseDir + path, new FuseFileInfo());

            //int r = Syscall.rmdir(BaseDir + path);
            //return GetResult(r);
        }

        protected override Errno OnCreateSymbolicLink(string from, string to)
        {
            // naively => read + create. Better => create with pointer
            int r = Syscall.symlink(from, BaseDir + to);
            return GetResult(r);
        }

        protected override Errno OnRenamePath(string from, string to)
        {
            // naively => read + create + delete. Better => rename evt
            int r = Syscall.rename(BaseDir + from, BaseDir + to);
            return GetResult(r);
        }

        protected override Errno OnCreateHardLink(string from, string to)
        {
            // naively => read + create. Better => create with pointer
            int r = Syscall.link(BaseDir + from, BaseDir + to);
            return GetResult(r);
        }

        // CONVERTED
        protected override Errno OnChangePathPermissions(string path, FilePermissions mode)
        {
            return 0;
            //int r = Syscall.chmod(BaseDir + path, mode);
            //return GetResult(r);
        }

        // -- UNKNOWN --
        protected override Errno OnChangePathOwner(string path, long uid, long gid)
        {
            int r = Syscall.lchown(BaseDir + path, (uint)uid, (uint)gid);
            return GetResult(r);
        }

        // CONVERTED
        protected override Errno OnTruncateFile(string path, long size)
        {
            return _operations.SetEndOfFile(BaseDir + path, size, new FuseFileInfo());

            //int r = Syscall.truncate(BaseDir + path, size);
            //return GetResult(r);
        }

        // CONVERTED
        protected override Errno OnChangePathTimes(string path, ref Utimbuf buf)
        {
            return _operations.SetFileTime(BaseDir + path, default, 
                DateTimeOffset.FromUnixTimeSeconds(buf.actime).UtcDateTime, 
                DateTimeOffset.FromUnixTimeSeconds(buf.modtime).UtcDateTime, 
                new FuseFileInfo());
            //int r = Syscall.utime(BaseDir + path, ref buf);
            //return GetResult(r);
        }

        // CONVERTED
        protected override Errno OnOpenHandle(string path, OpenedPathInfo info)
            => ProcessFile(BaseDir + path, info.OpenFlags, delegate (int fd) { return 0; });

        delegate int FdCb(int fd);

        // CONVERTED
        static Errno ProcessFile(string path, OpenFlags flags, FdCb cb)
        {
            return 0;

            //int fd = Syscall.open(path, flags);
            //if (fd == -1)
            //    return Stdlib.GetLastError();
            //int r = cb(fd);
            //Errno res = 0;
            //if (r == -1)
            //    res = Stdlib.GetLastError();
            //Syscall.close(fd);
            //return res;
        }

        // CONVERTED
        protected override unsafe Errno OnReadHandle(string path, OpenedPathInfo info, byte[] buf,
                long offset, out int bytesRead)
        {
            return _operations.ReadFile(BaseDir + path, buf, out bytesRead, offset, new FuseFileInfo());

            //int br = 0;
            //Errno e = ProcessFile(BaseDir + path, OpenFlags.O_RDONLY, delegate (int fd)
            //{
            //    fixed (byte* pb = buf)
            //    {
            //        return br = (int)Syscall.pread(fd, pb, (ulong)buf.Length, offset);
            //    }
            //});
            //bytesRead = br;
            //return e;
        }
        
        // CONVERTED
        protected override unsafe Errno OnWriteHandle(string path, OpenedPathInfo info,
                byte[] buf, long offset, out int bytesWritten)
        {
            return _operations.WriteFile(BaseDir + path, buf, out bytesWritten, offset, new FuseFileInfo());

            //int bw = 0;
            //Errno e = ProcessFile(BaseDir + path, OpenFlags.O_WRONLY, delegate (int fd)
            //{
            //    fixed (byte* pb = buf)
            //    {
            //        return bw = (int)Syscall.pwrite(fd, pb, (ulong)buf.Length, offset);
            //    }
            //});
            //bytesWritten = bw;
            //return e;
        }

        // CONVERTED
        protected override Errno OnGetFileSystemStatus(string path, out Statvfs stbuf)
        {
            _operations.GetDiskFreeSpace(out long free, out long total, out long used, new FuseFileInfo());

            ulong block_size = 4096;
            ulong fragment_size = 512;

            stbuf = new Statvfs
            {
                f_bsize = block_size,
                f_bavail = (ulong)total / block_size,
                f_bfree = (ulong)free / block_size,
                f_blocks = (ulong)used / block_size,
                f_frsize = fragment_size,
                f_favail = (ulong)total / fragment_size,
                f_ffree = (ulong)free / fragment_size,
                f_flag = MountFlags.ST_WRITE,
                f_namemax = 512,
                f_fsid = (ulong)BaseDir.GetHashCode(),
                f_files = byte.MaxValue // TODO
            };

            return 0;

            //int r = Syscall.statvfs(BaseDir + path, out stbuf);
            //return GetResult(r);
        }

        // CONVERTED
        protected override Errno OnReleaseHandle(string path, OpenedPathInfo info)
            => 0;

        // CONVERTED
        protected override Errno OnSynchronizeHandle(string path, OpenedPathInfo info, bool onlyUserData)
            => 0;

        // CONVERTED
        protected override Errno OnSetPathExtendedAttribute(string path, string name, byte[] value, XattrFlags flags)
        {
            return 0;
            
            //int r = Syscall.lsetxattr(BaseDir + path, name, value, (ulong)value.Length, flags);
            //return GetResult(r);
        }

        // CONVERTED
        protected override Errno OnGetPathExtendedAttribute(string path, string name, byte[] value, out int bytesWritten)
        {
            bytesWritten = 0;
            return 0;
            //int r = bytesWritten = (int)Syscall.lgetxattr(BaseDir + path, name, value, (ulong)value.Length);
            //return GetResult(r);
        }

        // CONVERTED
        protected override Errno OnListPathExtendedAttributes(string path, out string[] names)
        {
            names = new string[0];
            return 0;
            //int r = (int)Syscall.llistxattr(BaseDir + path, out names);
            //return GetResult(r);
        }

        // CONVERTED
        protected override Errno OnRemovePathExtendedAttribute(string path, string name)
        {
            return 0;
            //int r = Syscall.lremovexattr(BaseDir + path, name);
            //return GetResult(r);
        }

        // CONVERTED
        protected override Errno OnLockHandle(string file, OpenedPathInfo info, FcntlCommand cmd, ref Flock @lock)
        {
            return _operations.LockFile(BaseDir + file, 0, 0, new FuseFileInfo());

            //Flock _lock = @lock;
            //Errno e = ProcessFile(BaseDir + file, info.OpenFlags, fd => Syscall.fcntl(fd, cmd, ref _lock));
            //@lock = _lock;
            //return e;
        }

        protected override Errno OnCreateHandle(string file, OpenedPathInfo info, FilePermissions mode)
        {
            return base.OnCreateHandle(file, info, mode);
        }
        protected override Errno OnFlushHandle(string file, OpenedPathInfo info)
        {
            return base.OnFlushHandle(file, info);
        }

        protected override Errno OnGetHandleStatus(string file, OpenedPathInfo info, out Stat buf)
        {
            return base.OnGetHandleStatus(file, info, out buf);
        }

        protected override void OnInit(ConnectionInformation connection)
        {
            base.OnInit(connection);
        }

        // CONVERTED
        protected override Errno OnMapPathLogicalToPhysicalIndex(string path, ulong logical, out ulong physical)
            => base.OnMapPathLogicalToPhysicalIndex(path, logical, out physical);

        protected override Errno OnOpenDirectory(string directory, OpenedPathInfo info)
        {
            return base.OnOpenDirectory(directory, info);
        }
        protected override Errno OnReleaseDirectory(string directory, OpenedPathInfo info)
        {
            return base.OnReleaseDirectory(directory, info);
        }
        protected override Errno OnSynchronizeDirectory(string directory, OpenedPathInfo info, bool onlyUserData)
        {
            return base.OnSynchronizeDirectory(directory, info, onlyUserData);
        }
        
        // CONVERTED
        protected override Errno OnTruncateHandle(string file, OpenedPathInfo info, long length)
            => base.OnTruncateHandle(file, info, length);

        Errno GetResult(int result)
        {
            if (result == -1)
                return Stdlib.GetLastError();
            return 0;
        }
    }
}
using Mono.Fuse.NETStandard;
using Mono.Unix.Native;
using System;
using System.Collections.Generic;
using System.Text;

namespace SAFE.NetworkDrive.Fuse
{
    public class FuseFS : FileSystem
    {
        public string BaseDir { get; set; }

        public FuseFS()
        { }

        protected override Errno OnGetPathStatus(string path, out Stat buf)
        {
            int r = Syscall.lstat(BaseDir + path, out buf);
            return GetResult(r);
        }

        protected override Errno OnAccessPath(string path, AccessModes mask)
        {
            int r = Syscall.access(BaseDir + path, mask);
            return GetResult(r);
        }

        protected override Errno OnReadSymbolicLink(string path, out string target)
        {
            target = null;
            var buf = new StringBuilder(256);
            do
            {
                int r = Syscall.readlink(BaseDir + path, buf);
                if (r < 0)
                    return Stdlib.GetLastError();
                else if (r == buf.Capacity)
                    buf.Capacity *= 2;
                else
                {
                    target = buf.ToString(0, r);
                    return 0;
                }
            } while (true);
        }

        protected override Errno OnReadDirectory(string path, OpenedPathInfo fi,
                out IEnumerable<DirectoryEntry> paths)
        {
            IntPtr dp = Syscall.opendir(BaseDir + path);
            if (dp == IntPtr.Zero)
            {
                paths = null;
                return Stdlib.GetLastError();
            }

            Dirent de;
            var entries = new List<DirectoryEntry>();
            while ((de = Syscall.readdir(dp)) != null)
            {
                var entry = new DirectoryEntry(de.d_name);
                entry.Stat.st_ino = de.d_ino;
                entry.Stat.st_mode = (FilePermissions)(de.d_type << 12);
                entries.Add(entry);
            }
            Syscall.closedir(dp);

            paths = entries;
            return 0;
        }

        protected override Errno OnCreateSpecialFile(string path, FilePermissions mode, ulong rdev)
        {
            int r;

            // On Linux, this could just be `mknod(basedir+path, mode, rdev)' but this is
            // more portable.
            if ((mode & FilePermissions.S_IFMT) == FilePermissions.S_IFREG)
            {
                r = Syscall.open(BaseDir + path, OpenFlags.O_CREAT | OpenFlags.O_EXCL |
                        OpenFlags.O_WRONLY, mode);
                if (r >= 0)
                    r = Syscall.close(r);
            }
            else if ((mode & FilePermissions.S_IFMT) == FilePermissions.S_IFIFO)
                r = Syscall.mkfifo(BaseDir + path, mode);
            else
                r = Syscall.mknod(BaseDir + path, mode, rdev);

            return GetResult(r);
        }

        protected override Errno OnCreateDirectory(string path, FilePermissions mode)
        {
            int r = Syscall.mkdir(BaseDir + path, mode);
            return GetResult(r);
        }

        protected override Errno OnRemoveFile(string path)
        {
            int r = Syscall.unlink(BaseDir + path);
            return GetResult(r);
        }

        protected override Errno OnRemoveDirectory(string path)
        {
            int r = Syscall.rmdir(BaseDir + path);
            return GetResult(r);
        }

        protected override Errno OnCreateSymbolicLink(string from, string to)
        {
            int r = Syscall.symlink(from, BaseDir + to);
            return GetResult(r);
        }

        protected override Errno OnRenamePath(string from, string to)
        {
            int r = Syscall.rename(BaseDir + from, BaseDir + to);
            return GetResult(r);
        }

        protected override Errno OnCreateHardLink(string from, string to)
        {
            int r = Syscall.link(BaseDir + from, BaseDir + to);
            return GetResult(r);
        }

        protected override Errno OnChangePathPermissions(string path, FilePermissions mode)
        {
            int r = Syscall.chmod(BaseDir + path, mode);
            return GetResult(r);
        }

        protected override Errno OnChangePathOwner(string path, long uid, long gid)
        {
            int r = Syscall.lchown(BaseDir + path, (uint)uid, (uint)gid);
            return GetResult(r);
        }

        protected override Errno OnTruncateFile(string path, long size)
        {
            int r = Syscall.truncate(BaseDir + path, size);
            return GetResult(r);
        }

        protected override Errno OnChangePathTimes(string path, ref Utimbuf buf)
        {
            int r = Syscall.utime(BaseDir + path, ref buf);
            return GetResult(r);
        }

        protected override Errno OnOpenHandle(string path, OpenedPathInfo info)
            => ProcessFile(BaseDir + path, info.OpenFlags, delegate (int fd) { return 0; });

        delegate int FdCb(int fd);

        static Errno ProcessFile(string path, OpenFlags flags, FdCb cb)
        {
            int fd = Syscall.open(path, flags);
            if (fd == -1)
                return Stdlib.GetLastError();
            int r = cb(fd);
            Errno res = 0;
            if (r == -1)
                res = Stdlib.GetLastError();
            Syscall.close(fd);
            return res;
        }

        protected override unsafe Errno OnReadHandle(string path, OpenedPathInfo info, byte[] buf,
                long offset, out int bytesRead)
        {
            int br = 0;
            Errno e = ProcessFile(BaseDir + path, OpenFlags.O_RDONLY, delegate (int fd)
            {
                fixed (byte* pb = buf)
                {
                    return br = (int)Syscall.pread(fd, pb, (ulong)buf.Length, offset);
                }
            });
            bytesRead = br;
            return e;
        }

        protected override unsafe Errno OnWriteHandle(string path, OpenedPathInfo info,
                byte[] buf, long offset, out int bytesWritten)
        {
            int bw = 0;
            Errno e = ProcessFile(BaseDir + path, OpenFlags.O_WRONLY, delegate (int fd)
            {
                fixed (byte* pb = buf)
                {
                    return bw = (int)Syscall.pwrite(fd, pb, (ulong)buf.Length, offset);
                }
            });
            bytesWritten = bw;
            return e;
        }

        protected override Errno OnGetFileSystemStatus(string path, out Statvfs stbuf)
        {
            int r = Syscall.statvfs(BaseDir + path, out stbuf);
            return GetResult(r);
        }

        protected override Errno OnReleaseHandle(string path, OpenedPathInfo info)
            => 0;

        protected override Errno OnSynchronizeHandle(string path, OpenedPathInfo info, bool onlyUserData)
            => 0;

        protected override Errno OnSetPathExtendedAttribute(string path, string name, byte[] value, XattrFlags flags)
        {
            int r = Syscall.lsetxattr(BaseDir + path, name, value, (ulong)value.Length, flags);
            return GetResult(r);
        }

        protected override Errno OnGetPathExtendedAttribute(string path, string name, byte[] value, out int bytesWritten)
        {
            int r = bytesWritten = (int)Syscall.lgetxattr(BaseDir + path, name, value, (ulong)value.Length);
            return GetResult(r);
        }

        protected override Errno OnListPathExtendedAttributes(string path, out string[] names)
        {
            int r = (int)Syscall.llistxattr(BaseDir + path, out names);
            return GetResult(r);
        }

        protected override Errno OnRemovePathExtendedAttribute(string path, string name)
        {
            int r = Syscall.lremovexattr(BaseDir + path, name);
            return GetResult(r);
        }

        protected override Errno OnLockHandle(string file, OpenedPathInfo info, FcntlCommand cmd, ref Flock @lock)
        {
            Flock _lock = @lock;
            Errno e = ProcessFile(BaseDir + file, info.OpenFlags, fd => Syscall.fcntl(fd, cmd, ref _lock));
            @lock = _lock;
            return e;
        }

        Errno GetResult(int result)
        {
            if (result == -1)
                return Stdlib.GetLastError();
            return 0;
        }
    }
}
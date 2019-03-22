//using DokanMem;
//using System;
//using System.Collections.Generic;

//namespace SAFE.NetworkDrive.Gateways.Memory
//{
//    class MemoryGateway
//    {
//        const string ROOT_FOLDER = "\\";
//        static object _locker = new object(); // we'll have to make this threadsafe sooner or later
//        static MemoryFolder _root = new MemoryFolder(null, string.Empty);

//        public int Cleanup(string filename, DokanFileInfo info) => DokanNet.DOKAN_SUCCESS;

//        public int CloseFile(string filename, DokanFileInfo info) => DokanNet.DOKAN_SUCCESS;

//        public int CreateDirectory(string filename, DokanFileInfo info)
//        {
//            // Get parent-folder 
//            // (where this new directory should be created)
//            string parentFolderPath = filename.GetPathPart();
//            MemoryFolder parentFolder = _root.GetFolderByPath(parentFolderPath);

//            // return an error if the parent-path doesn't exist
//            if (!parentFolder.Exists())
//                return -DokanNet.ERROR_PATH_NOT_FOUND;

//            // Make sure the new directory has a valid filename
//            string newName = filename.GetFilenamePart();
//            if (newName.IndexOfAny(Path.GetInvalidFileNameChars()) > 0)
//                return -DokanNet.ERROR_INVALID_NAME;
//            if (string.IsNullOrEmpty(newName))
//                return -DokanNet.ERROR_INVALID_NAME;

//            // check if already exists;
//            MemoryFolder testFolder = _root.GetFolderByPath(filename);
//            if (testFolder.Exists())
//                return -DokanNet.ERROR_ALREADY_EXISTS;

//            // make a folder :)
//            var newFolder = new MemoryFolder(parentFolder, newName);

//            // Inform Dokan
//            return newFolder.Exists() ? DokanNet.DOKAN_SUCCESS : DokanNet.DOKAN_ERROR;
//        }

//        public int CreateFile(
//            string filename,
//            FileAccess access,
//            FileShare share,
//            FileMode mode,
//            FileOptions options,
//            DokanFileInfo info)
//        {
//            if (filename == ROOT_FOLDER)
//                return DokanNet.DOKAN_SUCCESS;

//            // get parent folder where this file is to be created;
//            MemoryFolder parentFolder = _root.GetFolderByPath(filename.GetPathPart());

//            // does the parent exist?
//            if (!parentFolder.Exists())
//                return -DokanNet.ERROR_PATH_NOT_FOUND;

//            // get the name of the file to be created/opened;
//            string newName = filename.GetFilenamePart();

//            // Make sure the new directory has a valid filename
//            if (newName.IndexOfAny(Path.GetInvalidFileNameChars()) > 0)
//                return -DokanNet.ERROR_INVALID_NAME;
//            if (string.IsNullOrEmpty(newName))
//                return -DokanNet.ERROR_INVALID_NAME;

//            // we'll need this file later on;
//            MemoryFile thisFile = parentFolder.FetchFile(newName);

//            // this is called when we should create a new file;
//            // so raise an error if it's a directory;
//            MemoryFolder testFolder = _root.GetFolderByPath(filename);
//            if (testFolder.Exists())
//            {
//                //this is a folder?
//                info.IsDirectory = true;
//                if (mode == FileMode.Open || mode == FileMode.OpenOrCreate)
//                {
//                    //info.IsDirectory = true;
//                    return DokanNet.DOKAN_SUCCESS;
//                }

//                // you can't make a file with the same name as a folder;
//                return -DokanNet.ERROR_ALREADY_EXISTS;
//            }

//            // there's no folder with this name, the parent exists;
//            // attempt to use the file
//            switch (mode)
//            {
//                // Opens the file if it exists and seeks to the end of the file, 
//                // or creates a new file
//                case FileMode.Append:
//                    if (!thisFile.Exists())
//                        MemoryFile.New(parentFolder, newName);
//                    return DokanNet.DOKAN_SUCCESS;

//                // Specifies that the operating system should create a new file. 
//                // If the file already exists, it will be overwritten. 
//                case FileMode.Create:
//                    //if (!thisFile.Exists())
//                    MemoryFile.New(parentFolder, newName);
//                    //else
//                    //	thisFile.Content = new Thought.Research.AweBuffer(1024); //MemoryStream();
//                    return DokanNet.DOKAN_SUCCESS;

//                // Specifies that the operating system should create a new file. 
//                // If the file already exists, an IOException is thrown.
//                case FileMode.CreateNew:
//                    if (thisFile.Exists())
//                        return -DokanNet.ERROR_ALREADY_EXISTS;
//                    MemoryFile.New(parentFolder, newName);
//                    return DokanNet.DOKAN_SUCCESS;

//                // Specifies that the operating system should open an existing file. 
//                // A System.IO.FileNotFoundException is thrown if the file does not exist.
//                case FileMode.Open:
//                    if (!thisFile.Exists())
//                        return -DokanNet.ERROR_FILE_NOT_FOUND;
//                    else
//                        return DokanNet.DOKAN_SUCCESS;

//                // Specifies that the operating system should open a file if it exists; 
//                // otherwise, a new file should be created.
//                case FileMode.OpenOrCreate:
//                    if (!thisFile.Exists())
//                        MemoryFile.New(parentFolder, newName);
//                    return DokanNet.DOKAN_SUCCESS;

//                // Specifies that the operating system should open an existing file. 
//                // Once opened, the file should be truncated so that its size is zero bytes
//                case FileMode.Truncate:
//                    if (!thisFile.Exists())
//                        thisFile = MemoryFile.New(parentFolder, newName);
//                    thisFile.Size = 0;
//                    return DokanNet.DOKAN_SUCCESS;
//            }

//            return DokanNet.DOKAN_ERROR;
//        }

//        public int DeleteDirectory(string filename, DokanFileInfo info)
//        {
//            // find target;
//            MemoryFolder folder = _root.GetFolderByPath(filename);

//            if (!folder.Exists())
//                return -DokanNet.ERROR_PATH_NOT_FOUND;

//            // unregister folder from it's parent
//            folder.Parent.Children.Remove(folder);
//            return DokanNet.DOKAN_SUCCESS;
//        }

//        public int DeleteFile(string filename, DokanFileInfo info)
//        {
//            // get parent folder
//            MemoryFolder parentFolder = _root.GetFolderByPath(
//                filename.GetPathPart());

//            // exists?
//            if (!parentFolder.Exists())
//                return -DokanNet.ERROR_PATH_NOT_FOUND;

//            // fetch file;
//            MemoryFile file = parentFolder.FetchFile(
//                filename.GetFilenamePart());

//            // exists?
//            if (!file.Exists())
//                return -DokanNet.ERROR_FILE_NOT_FOUND;

//            // delete it;
//            parentFolder.Children.Remove(file);

//            return DokanNet.DOKAN_SUCCESS;
//        }

//        public int FlushFileBuffers(string filename, DokanFileInfo info)
//            => DokanNet.DOKAN_SUCCESS;

//        public int FindFiles(
//            string filename,
//            System.Collections.ArrayList files,
//            DokanFileInfo info)
//        {
//            // do we have this folder?
//            MemoryFolder folder = filename == ROOT_FOLDER ? _root : _root.GetFolderByPath(filename);
//            if (!folder.Exists())
//                return -DokanNet.ERROR_FILE_NOT_FOUND;

//            // we have this folder, list all it's children;
//            foreach (MemoryItem item in folder.Children)
//            {
//                var fileinfo = new FileInformation
//                {
//                    FileName = item.Name,
//                    Attributes = item.Attributes,
//                    LastAccessTime = item.LastAccessTime,
//                    LastWriteTime = item.LastWriteTime,
//                    CreationTime = item.CreationTime
//                };

//                // if it's a file, then also report a size;
//                if (item is MemoryFile)
//                    fileinfo.Length = (item as MemoryFile).Size;

//                files.Add(fileinfo);
//            }
//            return DokanNet.DOKAN_SUCCESS;
//        }

//        public int GetFileInformation(
//            string filename,
//            FileInformation fileinfo,
//            DokanFileInfo info)
//        {
//            if (filename == ROOT_FOLDER || info.IsDirectory)
//            {
//                // ..about a folder?
//                MemoryFolder folder = (filename == ROOT_FOLDER) ? _root : _root.GetFolderByPath(filename);
//                if (!folder.Exists())
//                    return -DokanNet.ERROR_PATH_NOT_FOUND;

//                fileinfo.FileName = folder.Name;
//                fileinfo.Attributes = folder.Attributes;
//                fileinfo.LastAccessTime = folder.LastAccessTime;
//                fileinfo.LastWriteTime = folder.LastWriteTime;
//                fileinfo.CreationTime = folder.CreationTime;
//                return DokanNet.DOKAN_SUCCESS;
//            }
//            else
//            {
//                // ..about a file?
//                string name = filename.GetFilenamePart();
//                MemoryFolder parentFolder = _root.GetFolderByPath(filename.GetPathPart());

//                // at least the parent should exist;
//                if (!parentFolder.Exists())
//                    return DokanNet.DOKAN_ERROR;

//                // does it exist?
//                MemoryFile file = parentFolder.FetchFile(name);
//                if (!file.Exists())
//                    return -DokanNet.ERROR_FILE_NOT_FOUND;

//                fileinfo.FileName = file.Name;
//                fileinfo.Attributes = file.Attributes;
//                fileinfo.LastAccessTime = file.LastAccessTime;
//                fileinfo.LastWriteTime = file.LastWriteTime;
//                fileinfo.CreationTime = file.CreationTime;
//                fileinfo.Length = file.Size;
//                return DokanNet.DOKAN_SUCCESS;
//            }
//        }

//        public int LockFile(
//            string filename,
//            long offset,
//            long length,
//            DokanFileInfo info)
//                => DokanNet.DOKAN_SUCCESS;

//        public int MoveFile(
//            string filename,
//            string newname,
//            bool replace,
//            DokanFileInfo info)
//        {
//            // find new parent 
//            MemoryFolder newParent = _root.GetFolderByPath(newname.GetPathPart());

//            // does it exist?
//            if (!newParent.Exists())
//                return -DokanNet.ERROR_PATH_NOT_FOUND;

//            // Make sure that there's not already a folder with this name;
//            MemoryFolder testNewFolder1 = _root.GetFolderByPath(newname);
//            if (testNewFolder1.Exists())
//                return -DokanNet.ERROR_ALREADY_EXISTS;

//            // Make sure that there's not a already a file with this name;
//            MemoryFile testNewFile = newParent.FetchFile(newname.GetFilenamePart());
//            if (testNewFile.Exists())
//                return -DokanNet.ERROR_FILE_EXISTS;

//            // Make sure we have a valid name
//            string newName = newname.GetFilenamePart();
//            if (string.IsNullOrEmpty(newName))
//                return -DokanNet.ERROR_INVALID_NAME;

//            if (info.IsDirectory)
//            {
//                // find folder that needs to be renamed
//                MemoryFolder sourceFolder = _root.GetFolderByPath(filename);

//                if (!sourceFolder.Exists())
//                    return -DokanNet.ERROR_FILE_NOT_FOUND;

//                // we found a folder with this name, attempt to rename it;

//                // set the new parent
//                sourceFolder.Parent = newParent;

//                // rename the actual item
//                sourceFolder.Name = newName;

//                // we did it!
//                return DokanNet.DOKAN_SUCCESS;
//            }
//            else
//            {
//                // perhaps we need to rename a file?
//                string name = filename.GetFilenamePart();

//                //find parentfolder of file
//                MemoryFolder parentFolder = _root.GetFolderByPath(filename.GetPathPart());
//                if (!parentFolder.Exists())
//                    return -DokanNet.ERROR_PATH_NOT_FOUND;

//                // has file?
//                MemoryFile thisFile = parentFolder.FetchFile(name);
//                if (!thisFile.Exists())
//                    return -DokanNet.ERROR_FILE_NOT_FOUND;

//                // update the parent-reference on the child;
//                thisFile.Parent = newParent;

//                // rename file
//                thisFile.Name = newName;

//                // say we succeeded!
//                return DokanNet.DOKAN_SUCCESS;
//            }
//        }

//        public int OpenDirectory(string filename, DokanFileInfo info)
//        {
//            if (filename == ROOT_FOLDER)
//                return DokanNet.DOKAN_SUCCESS;

//            MemoryFolder testFolder = _root.GetFolderByPath(filename);
//            if (!testFolder.Exists())
//                return -DokanNet.ERROR_FILE_NOT_FOUND; //#46

//            return DokanNet.DOKAN_SUCCESS;
//        }

//        public int ReadFile(
//            string filename,
//            byte[] buffer,
//            ref uint readBytes,
//            long offset,
//            DokanFileInfo info)
//        {
//            // find the parentfolder where this file should be;
//            MemoryFolder parentFolder = _root.GetFolderByPath(filename.GetPathPart());

//            // does it exist?
//            if (!parentFolder.Exists())
//                return -DokanNet.ERROR_PATH_NOT_FOUND;

//            // get the file;
//            string name = filename.GetFilenamePart();
//            MemoryFile file = parentFolder.FetchFile(name);

//            // does it exist?
//            if (!file.Exists())
//                return -DokanNet.ERROR_FILE_NOT_FOUND;

//            // read;
//            readBytes = file.Read(offset, buffer);
//            return DokanNet.DOKAN_SUCCESS;
//        }

//        public int WriteFile(string filename, byte[] buffer, ref uint writtenBytes,
//                long offset, DokanFileInfo info)
//        {
//            // find the parentfolder where this file should be;
//            MemoryFolder parentFolder = _root.GetFolderByPath(filename.GetPathPart());

//            // does it exist?
//            if (!parentFolder.Exists())
//                return -DokanNet.ERROR_PATH_NOT_FOUND;

//            // get the file;
//            string name = filename.GetFilenamePart();
//            MemoryFile file = parentFolder.FetchFile(name);

//            // does it exist?
//            if (!file.Exists())
//                return -DokanNet.ERROR_FILE_NOT_FOUND;

//            // Some apps try to write beyond the allocated size.
//            // We could return an error, or we can adjust the size
//            // of the buffer. 
//            //
//            // Through experimenting; it's possible to build
//            // projects located on the RAM-disk from the
//            // SharpDevelop IDE and to debug them :)
//            if (offset + buffer.Length > file.Size)
//                file.Size = offset + buffer.Length;

//            // write; 
//            writtenBytes = file.Write(offset, buffer);
//            return DokanNet.DOKAN_SUCCESS;
//        }

//        public int SetEndOfFile(string filename, long length, DokanFileInfo info)
//        {
//            // get parentfolder
//            MemoryFolder parentFolder = _root.GetFolderByPath(filename.GetPathPart());

//            // exists?
//            if (!parentFolder.Exists())
//                return -DokanNet.ERROR_PATH_NOT_FOUND;

//            // get file
//            string name = filename.GetFilenamePart();
//            MemoryFile file = parentFolder.FetchFile(name);

//            // exists?
//            if (!file.Exists())
//                return -DokanNet.ERROR_FILE_NOT_FOUND;

//            file.Size = length;
//            return DokanNet.DOKAN_SUCCESS;
//        }

//        public int SetAllocationSize(string filename, long length, DokanFileInfo info)
//        {
//            // get parentfolder
//            MemoryFolder parentFolder = _root.GetFolderByPath(filename.GetPathPart());

//            // exists?
//            if (!parentFolder.Exists())
//                return -DokanNet.ERROR_PATH_NOT_FOUND;

//            // get file
//            string name = filename.GetFilenamePart();
//            MemoryFile file = parentFolder.FetchFile(name);

//            // exists?
//            if (!file.Exists())
//                return -DokanNet.ERROR_FILE_NOT_FOUND;

//            file.Size = length;
//            return DokanNet.DOKAN_SUCCESS;
//        }

//        public int SetFileAttributes(
//            string filename,
//            FileAttributes attr,
//            DokanFileInfo info) => -DokanNet.DOKAN_ERROR;

//        public int SetFileTime(
//            string filename,
//            DateTime ctime,
//            DateTime atime,
//            DateTime mtime,
//            DokanFileInfo info) => -DokanNet.DOKAN_ERROR;

//        public int UnlockFile(string filename, long offset, long length, DokanFileInfo info) => DokanNet.DOKAN_SUCCESS;
//        public int Unmount(DokanFileInfo info) => DokanNet.DOKAN_SUCCESS;

//        /// <summary>
//        /// GetDiskFreeSpace calculates and returns the space of
//        /// the in-memory filesystem
//        /// </summary>
//        /// <param name="freeBytesAvailable">how many space there's left</param>
//        /// <param name="totalBytes">the total size of your drive</param>
//        /// <param name="totalFreeBytes">[ignored]</param>
//        /// <returns>DokanNet.DOKAN_SUCCESS</returns>
//        public int GetDiskFreeSpace(
//            ref ulong freeBytesAvailable,
//            ref ulong totalBytes,
//            ref ulong totalFreeBytes,
//            DokanFileInfo info)
//        {
//            totalBytes = (ulong)Environment.WorkingSet;

//            // The total number free bytes amounts to the total, minus what's used;
//            freeBytesAvailable = totalBytes - _root.UsedSize;

//            // The Dokan-interface seems to ignore this one;
//            totalFreeBytes = int.MaxValue;

//            return DokanNet.DOKAN_SUCCESS;
//        }
//    }
//}

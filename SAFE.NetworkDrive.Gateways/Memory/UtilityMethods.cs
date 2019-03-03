using System.Linq;
using System.IO;
using System;

namespace DokanMem
{
    internal static class UtilityMethods
    {
        /// <summary>
        /// Makes the code easier to read by substituting 
        /// (thisFolder == null) for thisFolder.Exists()
        /// </summary>
        /// <param name="item">An MemoryItem</param>
        /// <returns>False if the item is "null", true otherwise</returns>
        internal static bool Exists(this MemoryItem item)
        {
            return item != null;
        }

        internal static void CopyTo(this Stream content, MemoryFile file)
        {
            var ms = new MemoryStream();
            content.Seek(0, SeekOrigin.Begin);
            content.CopyTo(ms);
            file.Write(0, ms.ToArray());
            file.LastAccessTime = DateTime.Now;
            file.LastWriteTime = DateTime.Now;
        }

        /// <summary>
        /// Returns the parent-path of a file or directory,
        /// similar to Path.GetDirectoryName
        /// </summary>
        /// <param name="sourcePath">the full sourcepath</param>
        /// <returns>a path to it's parent</returns>
        internal static string GetPathPart(this string sourcePath)
        {
            return sourcePath.Substring(0, sourcePath.LastIndexOf('\\'));
        }

        /// <summary>
        /// Returns the filename-part of a string that contains a full path,
        /// similar to Path.GetFileName()
        /// </summary>
        /// <param name="sourcePath">a folder or file, with a full path</param>
        /// <returns>The item's name, without the path</returns>
        internal static string GetFilenamePart(this string sourcePath)
        {
            return sourcePath.Substring(sourcePath.LastIndexOf('\\') + 1);
        }

        /// <summary>
        /// Used to retrieve the first available driveletter
        /// </summary>
        /// <returns>A driveletter that's not in use yet</returns>
        internal static char GetFirstAvailableDriveLetter()
        {
            // these are the driveletters that are in use;
            var usedDriveLetters =
                from drive
                in DriveInfo.GetDrives()
                select drive.Name.ToUpperInvariant()[0];

            // these are all possible driveletters [D..Z] that
            // we can choose from (don't want "B" as drive);
            string allDrives = string.Empty;
            for (char c = 'D'; c < 'Z'; c++)
                allDrives += c.ToString();

            // these are the ones that are available;
            var availableDriveLetters = allDrives.Except(usedDriveLetters);

            if (availableDriveLetters.Count() == 0)
                throw new DriveNotFoundException("No drives available!");

            return availableDriveLetters.First();
        }
    }
}
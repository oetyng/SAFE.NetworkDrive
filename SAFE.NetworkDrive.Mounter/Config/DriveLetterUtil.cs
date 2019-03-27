using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace SAFE.NetworkDrive.Mounter.Config
{
    public static class DriveLetterUtil
    {
        /// <summary>
        /// Used to retrieve the first available driveletter
        /// </summary>
        /// <returns>A driveletter that's not in use yet</returns>
        public static char GetFirstAvailableDriveLetter()
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
            var availableDriveLetters = allDrives
                .Except(usedDriveLetters)
                .Reverse()
                .ToList();

            if (availableDriveLetters.Count == 0)
                throw new DriveNotFoundException("No drives available!");

            return availableDriveLetters.First();
        }

        public static List<char> GetAvailableDriveLetters(IEnumerable<char> reservedInConfig)
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
            var availableDriveLetters = allDrives
                .Except(reservedInConfig)
                .Except(usedDriveLetters)
                .Reverse()
                .ToList();

            if (availableDriveLetters.Count == 0)
                throw new DriveNotFoundException("No drives available!");

            return availableDriveLetters;
        }
    }
}
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using static System.Console;
using SAFE.NetworkDrive.Mounter.Config;

namespace SAFE.NetworkDrive.ConsoleApp
{
    class DriveConfigReader : StringReader
    {
        internal List<DriveConfig> ConfigureDrives(UserConfig user)
        {
            var drives = new List<DriveConfig>();
            
            do
            {
                WriteLine("Press Enter to add a new drive, or Esc to continue.");
                ConsoleKeyInfo key = ReadKey(true);
                if (key.Key == ConsoleKey.Enter)
                    drives.Add(GetDrive(user));
                else if (key.Key == ConsoleKey.Escape)
                    break;
            }
            while (true);

            return drives;
        }

        DriveConfig GetDrive(UserConfig user)
        {
            var csReader = new CredentialsReader();
            var driveLetter = GetDriveLetter().ToString();
            var remoteExists = user.Drives.Any(c => c.Root == driveLetter);
            while (remoteExists)
            {
                WriteLine($"Drive {driveLetter} already exists");
                driveLetter = GetDriveLetter().ToString();
            }

            var locator = csReader.GetLocator();
            var secret = csReader.GetSecret();

            var dirPath = Path.DirectorySeparatorChar.ToString();
            
            return new DriveConfig
            {
                Locator = locator,
                Secret = secret,
                Root = driveLetter,
                Schema = "safenetworkdrive_v1",
                Parameters = $"root={dirPath}"
            };
        }

        char GetDriveLetter()
        {
            WriteLine($"Please enter drive letter:");
            char val = char.MinValue;

            do
            {
                ConsoleKeyInfo key = ReadKey(true);
                if (key.Key != ConsoleKey.Backspace && key.Key != ConsoleKey.Enter)
                {
                    val = key.KeyChar;

                    if (!char.IsLetter(val))
                    {
                        WriteLine("Only letters allowed.");
                        continue;
                    }
                    if (!DriveLetterUtil.GetAvailableDriveLetters().Contains(val))
                    {
                        WriteLine("Drive letter is already used locally.");
                        continue;
                    }
                    if (val != char.MinValue)
                        Write("\b \b");
                    
                    Write(key.KeyChar);
                }
                else
                {
                    if (key.Key == ConsoleKey.Backspace)
                        Write("\b \b");
                    else if (key.Key == ConsoleKey.Enter && char.IsLetter(val))
                    {
                        WriteLine();
                        return char.ToUpperInvariant(val);
                    }
                }
            }
            while (true);
        }
    }
}
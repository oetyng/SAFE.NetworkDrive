using System;
using System.Collections.Generic;
using System.IO;
using Scrambler = SAFE.NetworkDrive.Gateways.Utils.PathScrambler;

namespace SAFE.NetworkDrive.Mounter.Config
{
    class DriveConfigReader : StringReader
    {
        internal List<DriveConfig> ConfigureDrives(string username)
        {
            var drives = new List<DriveConfig>();
            
            do
            {
                Console.WriteLine("Press Enter to add a new drive, or Esc to exit.");
                ConsoleKeyInfo key = Console.ReadKey(true);
                if (key.Key == ConsoleKey.Enter)
                    drives.Add(GetDrive(username));
                else if (key.Key == ConsoleKey.Escape)
                    break;
            }
            while (true);

            return drives;
        }

        DriveConfig GetDrive(string username)
        {
            var csReader = new CredentialsReader();
            var driveLetter = GetDriveLetter();
            var locator = csReader.GetLocator();
            var secret = csReader.GetSecret();
            var userFolder = Scrambler.Obfuscate(username, secret);
            var driveFolder = Scrambler.Obfuscate(driveLetter, secret);
            var dirPath = $"../snd/{userFolder}/{driveFolder}".ToLowerInvariant();
            if (!Directory.Exists(dirPath))
                Directory.CreateDirectory(dirPath);
            return new DriveConfig
            {
                Locator = locator,
                Secret = secret,
                Root = driveLetter,
                Schema = "safenetwork",
                Parameters = $"root={dirPath}"
            };
        }

        string GetDriveLetter()
        {
            Console.WriteLine($"Please enter drive letter:");
            string val = string.Empty;

            do
            {
                ConsoleKeyInfo key = Console.ReadKey(true);
                if (key.Key != ConsoleKey.Backspace && key.Key != ConsoleKey.Enter)
                {
                    if (val.Length == 1)
                    {
                        val = val.Substring(0, val.Length - 1);
                        Console.Write("\b \b");
                    }
                    val += key.KeyChar;
                    Console.Write(key.KeyChar);
                }
                else
                {
                    if (key.Key == ConsoleKey.Backspace && val.Length > 0)
                    {
                        // val = val[0..^1]; // c# 8.0
                        val = val.Substring(0, val.Length - 1);
                        Console.Write("\b \b");
                    }
                    else if (key.Key == ConsoleKey.Enter && val.Length == 1)
                    {
                        Console.WriteLine();
                        return val.ToUpperInvariant();
                    }
                }
            }
            while (true);
        }
    }
}

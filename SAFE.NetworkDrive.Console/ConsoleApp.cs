using NLog;
using SAFE.NetworkDrive.Mounter;
using SAFE.NetworkDrive.Mounter.Config;
using System;
using static System.Console;

namespace SAFE.NetworkDrive.Console
{
    class ConsoleApp
    {
        public UserConfig GetUserConfig()
        {
            var (user, pwd) = GetUserLogin();
            var handler = new UserConfigHandler(user, pwd);
            var userConfig = handler.CreateOrDecrypUserConfig();

            var dReader = new DriveConfigReader();
            var drives = dReader.ConfigureDrives(userConfig);
            handler.AddDrives(drives);
            return userConfig;
        }

        (string user, string pwd) GetUserLogin()
        {
            var uReader = new UserReader();
            return (uReader.GetUserName(), uReader.GetPassword());
        }

        public int Mount(UserConfig config)
        {
            DriveMountManager mounter = default;

            try
            {
                using (var logFactory = new LogFactory())
                {
                    var logger = logFactory.GetCurrentClassLogger();
                    mounter = new DriveMountManager(config, logger);

                    mounter.MountAll();

                    WriteLine("Press CTRL-BACKSPACE to clear log, 'U' key to unmount all drives");
                    while (true)
                    {
                        var keyInfo = ReadKey(true);
                        if (keyInfo.Modifiers.HasFlag(ConsoleModifiers.Control) && keyInfo.Key == ConsoleKey.Backspace)
                            Clear();
                        else if (keyInfo.Key == ConsoleKey.U)
                            break;
                    }

                    return 0;
                }
            }
            catch (Exception ex)
            {
                Error.WriteLine($"{ex.GetType().Name}: {ex.Message}");
                return -1;
            }
            finally
            {
                mounter?.UnmountAll();
            }
        }
    }
}
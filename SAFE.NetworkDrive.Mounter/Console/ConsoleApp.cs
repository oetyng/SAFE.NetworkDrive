using SAFE.NetworkDrive.Mounter;
using SAFE.NetworkDrive.Mounter.Config;
using System;
using static System.Console;

namespace SAFE.NetworkDrive.ConsoleApp
{
    public class ConsoleApp
    {
        readonly Func<DriveConfig, IDriveMounter> _mounter;
        
        public ConsoleApp(Func<DriveConfig, IDriveMounter> mounter)
            => _mounter = mounter;

        public UserConfig GetUserConfig()
        {
            var pwd = GetUserLogin();
            var handler = new UserConfigHandler(pwd);
            var userConfig = handler.CreateOrDecrypUserConfig();

            var dReader = new DriveConfigReader();
            var drives = dReader.ConfigureDrives(userConfig);
            handler.AddDrives(drives);
            return userConfig;
        }

        string GetUserLogin()
        {
            var uReader = new PasswordReader();
            return uReader.GetPassword();
        }

        public int Mount(UserConfig config)
        {
            DriveManager mounter = default;

            try
            {
                var logger = Utils.LogFactory.GetLogger("logger");
                mounter = new DriveManager(_mounter, config, logger);

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
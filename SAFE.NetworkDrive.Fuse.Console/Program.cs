using static System.Console;

namespace SAFE.NetworkDrive.Fuse.Console
{
    class Program
    {
        static void Main(string[] args)
        {
            using (var fs = new FuseFS())
            {
                string[] unhandled = fs.ParseFuseArguments(args);

                foreach (string key in fs.FuseOptions.Keys)
                    WriteLine("Option: {0}={1}", key, fs.FuseOptions[key]);

                if (!ParseArguments(unhandled, fs))
                    return;
                // fs.MountAt("path" /* , args? */);
                fs.Start();
            }
        }

        static bool ParseArguments(string[] args, FuseFS fs)
        {
            for (int i = 0; i < args.Length; ++i)
            {
                switch (args[i])
                {
                    case "-h":
                    case "--help":
                        ShowHelp();
                        return false;
                    default:
                        if (fs.MountPoint == null)
                            fs.MountPoint = args[i];
                        else
                            fs.BaseDir = args[i];
                        break;
                }
            }
            if (fs.MountPoint == null)
                return ShowError("missing mountpoint");
            if (fs.BaseDir == null)
                return ShowError("missing basedir");
            return true;
        }

        static void ShowHelp()
        {
            Error.WriteLine("usage: redirectfs [options] mountpoint basedir:");
            FuseFS.ShowFuseHelp("redirectfs");
            Error.WriteLine();
            Error.WriteLine("redirectfs options:");
            Error.WriteLine("    basedir                Directory to mirror");
        }

        static bool ShowError(string message)
        {
            Error.WriteLine("redirectfs: error: {0}", message);
            return false;
        }
    }
}
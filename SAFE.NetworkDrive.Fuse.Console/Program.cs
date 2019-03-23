using System;
using static System.Console;
using App = SAFE.NetworkDrive.ConsoleApp.ConsoleApp;

namespace SAFE.NetworkDrive.Fuse.Console
{
    internal sealed class Program
    {
        internal static void Main()
        {
            try
            {
                var app = new App((c) => new FuseMounter(c));
                var user = app.GetUserConfig();
                app.Mount(user);
            }
            catch(Exception ex)
            {
                WriteLine(ex.Message);
            }
            WriteLine("Press any key to exit.");
            ReadKey();
        }
    }
}
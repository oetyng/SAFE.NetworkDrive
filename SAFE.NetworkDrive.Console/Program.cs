using System;
using static System.Console;

namespace SAFE.NetworkDrive.Console
{
    internal sealed class Program
    {
        internal static void Main()
        {
            try
            {
                var console = new ConsoleApp();
                var user = console.GetUserConfig();
                console.Mount(user);
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
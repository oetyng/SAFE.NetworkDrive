using System;

namespace SAFE.NetworkDrive.Mounter
{
    public abstract class StringReader
    {
        protected string GetSecretString(string type)
        {
            Console.WriteLine($"Please enter {type}:");
            string pass = string.Empty;
            do
            {
                ConsoleKeyInfo key = Console.ReadKey(true);

                // Backspace Should Not Work
                if (key.Key != ConsoleKey.Backspace && key.Key != ConsoleKey.Enter)
                {
                    pass += key.KeyChar;
                    Console.Write("*");
                }
                else
                {
                    if (key.Key == ConsoleKey.Backspace && pass.Length > 0)
                    {
                        // pass = pass[0..^1]; // c# 8.0
                        pass = pass.Substring(0, pass.Length - 1);
                        Console.Write("\b \b");
                    }
                    else if (key.Key == ConsoleKey.Enter)
                    {
                        break;
                    }
                }
            }
            while (true);

            Console.WriteLine();
            if (pass.Length > 5)
                Console.WriteLine($"Oh lala, very strong {type}..!");
            Console.WriteLine();

            return pass;
        }

        protected string GetString(string type)
        {
            Console.WriteLine($"Please enter {type}:");
            string pass = string.Empty;
            do
            {
                ConsoleKeyInfo key = Console.ReadKey(true);

                // Backspace Should Not Work
                if (key.Key != ConsoleKey.Backspace && key.Key != ConsoleKey.Enter)
                {
                    pass += key.KeyChar;
                    Console.Write(key.KeyChar);
                }
                else
                {
                    if (key.Key == ConsoleKey.Backspace && pass.Length > 0)
                    {
                        // pass = pass[0..^1]; // c# 8.0
                        pass = pass.Substring(0, pass.Length - 1);
                        Console.Write("\b \b");
                    }
                    else if (key.Key == ConsoleKey.Enter)
                    {
                        break;
                    }
                }
            }
            while (true);

            Console.WriteLine();
            if (pass.Length > 5)
                Console.WriteLine($"Oh lala, very strong {type}..!");
            Console.WriteLine();

            return pass;
        }
    }
}
using System;

namespace SAFE.NetworkDrive.Mounter
{
    public abstract class StringReader
    {
        protected string GetSecretString(string type)
        {
            Console.WriteLine($"Please enter {type}:");
            string secret = string.Empty;
            do
            {
                ConsoleKeyInfo key = Console.ReadKey(true);

                // Backspace Should Not Work
                if (key.Key != ConsoleKey.Backspace && key.Key != ConsoleKey.Enter)
                {
                    secret += key.KeyChar;
                    Console.Write("*");
                }
                else
                {
                    if (key.Key == ConsoleKey.Backspace && secret.Length > 0)
                    {
                        // secret = secret[0..^1]; // c# 8.0
                        secret = secret.Substring(0, secret.Length - 1);
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

            return secret;
        }

        protected string GetString(string type)
        {
            Console.WriteLine($"Please enter {type}:");
            string val = string.Empty;
            do
            {
                ConsoleKeyInfo key = Console.ReadKey(true);

                if (key.Key != ConsoleKey.Backspace && key.Key != ConsoleKey.Enter)
                {
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
                    else if (key.Key == ConsoleKey.Enter)
                    {
                        break;
                    }
                }
            }
            while (true);

            Console.WriteLine();

            return val;
        }
    }
}
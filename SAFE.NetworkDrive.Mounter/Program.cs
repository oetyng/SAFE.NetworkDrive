/*
The MIT License(MIT)

Copyright(c) 2015 IgorSoft

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.
*/

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using DokanNet;
using NLog;
using SAFE.NetworkDrive.Parameters;
using SAFE.NetworkDrive.Mounter.Config;

namespace SAFE.NetworkDrive.Mounter
{
    internal sealed class Program
    {
        static ILogger _logger;
        
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA1801:ReviewUnusedParameters", MessageId = "args")]
        internal static void Main(string[] args)
        {
            try
            {
                var console = new ConsoleApp();
                var user = console.GetUserConfig();
                new Program().Mount(user);
            }
            catch(Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
            Console.WriteLine("Press any key to exit.");
            Console.ReadKey();
        }

        int Mount(UserConfig config)
        {
            try
            {
                using (var logFactory = new LogFactory())
                {
                    logFactory.Configuration.AddTarget(new NLog.Targets.FileTarget("logs"));
                    _logger = logFactory.GetCurrentClassLogger();
                    var factory = new CloudDriveFactory();
                    using (var tokenSource = new CancellationTokenSource())
                    {
                        var tasks = new List<Task>();
                        foreach (var driveConfig in config.Drives)
                        {
                            var drive = factory.CreateCloudDrive(
                                driveConfig.Schema,
                                config.UserName,
                                driveConfig.Root,
                                new CloudDriveParameters()
                                {
                                    ApiKey = driveConfig.Locator,
                                    EncryptionKey = driveConfig.Secret,
                                    Logger = _logger,
                                    Cancellation = tokenSource.Token,
                                    Parameters = driveConfig.GetParameters()
                                }
                            );

                            if (!drive.TryAuthenticate())
                            {
                                var displayRoot = drive.DisplayRoot;
                                drive.Dispose();
                                _logger.Warn($"Authentication failed for drive '{displayRoot}'");
                                continue;
                            }

                            var operations = new CloudOperations(drive, _logger);

                            // HACK: handle non-unique parameter set of DokanOperations.Mount() by explicitely specifying AllocationUnitSize and SectorSize
                            tasks.Add(Task.Run(() => operations.Mount(driveConfig.Root, 
                                DokanOptions.NetworkDrive | DokanOptions.MountManager | DokanOptions.CurrentSession, 
                                threadCount: 5,
                                121, 
                                TimeSpan.FromSeconds(driveConfig.Timeout != 0 ? driveConfig.Timeout : 20), 
                                null, 512, 512), 
                                tokenSource.Token));

                            var driveInfo = new DriveInfo(driveConfig.Root);
                            while (!driveInfo.IsReady)
                                Thread.Sleep(10);
                            _logger.Info($"Drive '{drive.DisplayRoot}' mounted successfully.");
                        }

                        Console.WriteLine("Press CTRL-BACKSPACE to clear log, 'U' key to unmount drives");
                        while (true)
                        {
                            var keyInfo = Console.ReadKey(true);
                            if (keyInfo.Modifiers.HasFlag(ConsoleModifiers.Control) && keyInfo.Key == ConsoleKey.Backspace)
                                Console.Clear();
                            else if (keyInfo.Key == ConsoleKey.U)
                                break;
                        }

                        tokenSource.Cancel();

                        return 0;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"{ex.GetType().Name}: {ex.Message}");
                return -1;
            }
            finally
            {
                foreach (var drive in config.Drives)
                    Dokan.Unmount(drive.Root[0]);
            }
        }
    }
}
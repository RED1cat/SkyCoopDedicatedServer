using Microsoft.Extensions.Logging;
using NLog.Extensions.Logging;
using System.Diagnostics;

namespace SkyCoopDedicatedServerKickstarter
{
    class Program
    {
        public static ILogger Logger = LoggerFactory.Create(builder => builder.AddNLog()).CreateLogger<Program>();
        public static string ServerFile = string.Empty;
        public static string AppPath = AppDomain.CurrentDomain.BaseDirectory;

        public static void Main()
        {
            switch (Environment.OSVersion.Platform)
            {
                case PlatformID.Win32NT:
                    ServerFile = @$"{AppPath}../SkyCoopDedicatedServer.exe";
                    break;
                case PlatformID.Unix:
                    ServerFile = @$"{AppPath}../SkyCoopDedicatedServer";
                    break;
                default:
                    return;
            }
            Process process = new Process();
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.FileName = ServerFile;

            if (File.Exists(ServerFile) && ServerFile != string.Empty)
            {
                process.Start();
                process.WaitForExit();

                if (process.ExitCode == 1)
                {
                    Environment.Exit(0);
                }
                while (true)
                {
                    if (process.HasExited)
                    {
                        if (process.ExitCode == 0)
                        {
                            Logger.LogError("The server is rebooting");
                        }
                        else
                        {
                            Logger.LogError("The server has an error, an attempt to restart it");
                        }
                        process.Start();
                        process.WaitForExit();

                        if(process.ExitCode == 1)
                        {
                            Logger.LogError("The server is shutting down");
                            Environment.Exit(0);
                        }
                    }
                }
            }
            else
            {
                Logger.LogInformation("server file not found");
            }
        }
    }
}

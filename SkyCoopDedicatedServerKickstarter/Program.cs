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

                while (true)
                {
                    if (process.HasExited)
                    {
                        Logger.LogError("otval dedicted");
                        process.Start();
                        process.WaitForExit();
                    }
                }
            }
            else
            {
                Logger.LogInformation("otval");
            }
        }
    }
}

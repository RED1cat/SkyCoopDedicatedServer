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
            if (Type.GetType("Mono.Runtime") != null)
            {
                ServerFile = @$"{AppPath}../SkyCoopDedicatedServer"; //linux
            }
            else
            {
                ServerFile = @$"{AppPath}../SkyCoopDedicatedServer.exe"; //windows
            }
            Process process = new Process();
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.FileName = ServerFile;

            if (File.Exists(ServerFile) && ServerFile != string.Empty)
            {
                process.Start();

                while (true)
                {
                    if (process.HasExited)
                    {
                        Logger.LogError("otval dedicted");
                        process.Start();
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

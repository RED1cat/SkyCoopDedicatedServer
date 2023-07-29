using GameServer;
using Microsoft.Extensions.Logging;
using NLog.Extensions.Logging;
using System;

namespace SkyCoopDedicatedServer
{
    class Program
    {
        public static ConsoleMain consoleMain;
        public static ILogger logger = LoggerFactory.Create(builder => builder.AddNLog()).CreateLogger<Program>();
        public static bool ServerGettingError = false;

        public static void Main(string[] args)
        {
            if (args.Length > 0)
            {
                if (args[0] == "Error")
                {
                    ServerGettingError = true;
                }
            }
            consoleMain = new ConsoleMain();
            consoleMain.Initialize();
        }
    }
}
using Microsoft.Extensions.Logging;
using NLog.Extensions.Logging;

namespace SkyCoopDedicatedServer
{
    class Program
    {
        public static ConsoleMain consoleMain;
        public static ILogger logger = LoggerFactory.Create(builder => builder.AddNLog()).CreateLogger<Program>();

        public static void Main()
        {
            consoleMain = new ConsoleMain();
            consoleMain.Initialize();
        }
    }
}
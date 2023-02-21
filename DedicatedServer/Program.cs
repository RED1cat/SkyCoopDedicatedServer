using Microsoft.Extensions.Logging;
using NLog.Extensions.Logging;

namespace DedicatedServer
{
    class Program
    {
        public static bool NoGraphics;
        public static XnaMain xnaMain;
        public static ConsoleMain consoleMain;
        public static ILogger logger = LoggerFactory.Create(builder => builder.AddNLog()).CreateLogger<Program>();

        public static void Main(string[] arg)
        {
#if DEBUG
            arg = new string[1] { "-NoGraphics" };
#endif
            if (arg.Length > 0 && arg[0] == "-NoGraphics")
            {
                NoGraphics = true;
                consoleMain = new ConsoleMain();
                consoleMain.Initialize();
            }
            else
            {
                NoGraphics = false;
                xnaMain = new XnaMain();
                xnaMain.Run();
            }
        }
    }
}
using NLog;
using System;

namespace SkyCoopDedicatedServer
{
    public class Logger
    {
        static ILogger FileLogger = LogManager.GetCurrentClassLogger();
        
        public static void Log(ConsoleColor color, string message)
        {
            Console.ForegroundColor = color;
            Console.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] {message}");
            Console.ForegroundColor = ConsoleColor.White;

            FileLogger.Log(LogLevel.Info, message);
        }
        
        public static void Log(string message)
        {
            Console.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] {message}");

            FileLogger.Log(LogLevel.Info, message);
        }

        public static void HandleServerLog(SkyCoopServer.Logger.LogData Data)
        {
            Log(Data.m_Color, Data.m_Message);
        }
    }
}

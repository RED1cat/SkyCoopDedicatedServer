using SkyCoopDedicatedServer;
using Microsoft.Extensions.Logging;
using NLog.Fluent;
using System;
using static SkyCoop.Shared;

namespace SkyCoop
{
    public static class Logger
    {
        public static void Log(string message, LoggerColor lColor = LoggerColor.White)
        {
            ConsoleColor consoleColor = ColorToConsoleColor(lColor);
            string consoleLog = $"[{DateTime.Now.ToString() + '.' + DateTime.Now.Millisecond.ToString()}] {message}";

            Program.logger.Log(LogLevel.Information, message);

            Console.ForegroundColor = consoleColor;
            Console.WriteLine(consoleLog);
            Console.ResetColor();
        }

        public static ConsoleColor ColorToConsoleColor(LoggerColor lColor)
        {
            switch (lColor)
            {
                case LoggerColor.Red:
                    return ConsoleColor.Red;
                case LoggerColor.Green:
                    return ConsoleColor.Green;
                case LoggerColor.Blue:
                    return ConsoleColor.Blue;
                case LoggerColor.Yellow:
                    return ConsoleColor.Yellow;
                case LoggerColor.Magenta:
                    return ConsoleColor.Magenta;
                case LoggerColor.White:
                    return ConsoleColor.White;
                default:
                    return ConsoleColor.Gray;
            }
        }
    }
}
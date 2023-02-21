using DedicatedServer;
using Microsoft.Extensions.Logging;
using Microsoft.Xna.Framework;
using NLog.Fluent;
using System;
using System.Collections.Generic;
using System.IO;
using static SkyCoop.Shared;

namespace SkyCoop
{
    public static class Logger
    {
        public static void Log(string message, LoggerColor lColor = LoggerColor.White)
        {

            Color xnaColor = ColorToXna(lColor);
            ConsoleColor consoleColor = ColorToConsoleColor(lColor);
            string consoleLog = $"[{DateTime.Now.ToString() + '.' + DateTime.Now.Millisecond.ToString()}] {message}";

            Program.logger.Log(LogLevel.Information, message);
            if (Program.NoGraphics)
            {
                Console.ForegroundColor = consoleColor;
                Console.WriteLine(consoleLog);
                Console.ResetColor();
            }
            else
            {
                CustomConsole.AddLine(consoleLog, xnaColor);
            }
        }

        public static Color ColorToXna(LoggerColor lColor)
        {
            switch (lColor)
            {
                case LoggerColor.Red:
                    return Color.Red;
                case LoggerColor.Green:
                    return Color.Green;
                case LoggerColor.Blue:
                    return Color.Blue;
                case LoggerColor.Yellow:
                    return Color.Yellow;
                case LoggerColor.Magenta:
                    return Color.Magenta;
                case LoggerColor.White:
                    return Color.White;
                default:
                    return Color.White;
            }
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
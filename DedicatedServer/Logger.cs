using DedicatedServer;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using static SkyCoop.Shared;

namespace SkyCoop
{
    public static class Logger
    {
#if DEBUG
        static bool ForceXNA = true;
#else
        static bool ForceXNA = false;
#endif
        private static List<string> logBuffer = new List<string>();
        public static void Log(string message, LoggerColor lColor = LoggerColor.White)
        {

            Color xnaColor = ColorToXna(lColor);
            ConsoleColor consoleColor = ColorToConsoleColor(lColor);
            string log = $"[{DateTime.Now.ToString() + '.' + DateTime.Now.Millisecond.ToString()}] " + message;
            
            if (ForceXNA)
            {
                CustomConsole.AddLine(log, xnaColor);
                return;
            }

            if (File.Exists("log.txt") == false)
            {
                File.Create("log.txt").Close();
            }

            if (logBuffer.Count > 0)
            {
                try
                {
                    File.AppendAllText("log.txt", logBuffer[0] + Environment.NewLine);
                }
                finally
                {
                    logBuffer.RemoveAt(0);
                } 
            }

            try
            {
                File.AppendAllText("log.txt", log + Environment.NewLine);
            }
            catch
            {
                if (Program.NoGraphics)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("Unable to access log.txt");
                    Console.ResetColor();
                }
                else
                {
                    CustomConsole.AddLine("Unable to access log.txt", Color.Red);
                }
                logBuffer.Add(log);
            }
            finally
            {
                if (Program.NoGraphics)
                {
                    Console.ForegroundColor = consoleColor;
                    Console.WriteLine(log);
                    Console.ResetColor();
                }
                else
                {
                    CustomConsole.AddLine(log, xnaColor);
                }
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
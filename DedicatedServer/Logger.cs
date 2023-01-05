using DedicatedServer;
using Microsoft.Xna.Framework;
using System;
using System.IO;

namespace SkyCoop
{
    public static class Logger
    {
        public static void Log(string message, LoggerColor lColor = LoggerColor.White)
        {
            Color color = ColorToXna(lColor);
            string log = $"[{DateTime.Now.ToString() + '.' + DateTime.Now.Millisecond.ToString()}] " + message + Environment.NewLine;
            if (File.Exists("log.txt"))
            {
                try
                {
                    File.AppendAllText("log.txt", log);
                }
                catch
                {
                    CustomConsole.AddLine("Unable to access log.txt", Color.Red);
                }
                CustomConsole.AddLine(log, color);
            }
            else
            {
                File.Create("log.txt").Close();
                try
                {
                    File.AppendAllText("log.txt", log);
                }
                catch
                {
                    CustomConsole.AddLine("Unable to access log.txt", Color.Red);
                }
                CustomConsole.AddLine(log, color);
            }
        }
        public enum LoggerColor
        {
            Red,
            Green,
            Blue,
            Yellow,
            Magenta,
            White,
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
    }
}

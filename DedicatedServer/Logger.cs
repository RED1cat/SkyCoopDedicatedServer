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
        private static List<string> logBuffer = new List<string>();
        public static void Log(string message, LoggerColor lColor = LoggerColor.White)
        {
            Color color = ColorToXna(lColor);
            string log = $"[{DateTime.Now.ToString() + '.' + DateTime.Now.Millisecond.ToString()}] " + message;

            if(File.Exists("log.txt") == false)
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
                CustomConsole.AddLine("Unable to access log.txt", Color.Red);
                logBuffer.Add(log);
            }
            CustomConsole.AddLine(log, color);
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

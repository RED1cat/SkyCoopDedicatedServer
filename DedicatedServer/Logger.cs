using DedicatedServer;
using System;
using System.IO;

namespace SkyCoop
{
    public static class Logger
    {
        public static void Log(string message)
        {
            string log = $"[{DateTime.Now.ToString() + '.' + DateTime.Now.Millisecond.ToString()}] " + message + Environment.NewLine;
            if (File.Exists("log.txt"))
            {
                File.AppendAllText("log.txt", log);
                CustomConsole.AddLine(log);
            }
            else
            {
                File.Create("log.txt").Close();
                File.AppendAllText("log.txt", log);
                CustomConsole.AddLine(log);
            }
        }
    }
}

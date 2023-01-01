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
                try
                {
                    File.AppendAllText("log.txt", log);
                }
                catch
                {
                    CustomConsole.AddLine("Unable to access log.txt");
                }
                CustomConsole.AddLine(log);
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
                    CustomConsole.AddLine("Unable to access log.txt");
                }
                CustomConsole.AddLine(log);
            }
        }
    }
}

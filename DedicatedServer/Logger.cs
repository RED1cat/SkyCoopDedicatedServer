using System;
using System.IO;

namespace DedicatedServer
{
    internal class Logger
    {
        
        public void Log(string message)
        {
            string time = DateTime.Now.ToString() + '.' + DateTime.Now.Millisecond.ToString();
            if (File.Exists("log.txt"))
            {
                File.AppendAllText("log.txt", $"[{time}] " + message + Environment.NewLine);
            }
            else
            {
                File.Create("log.txt");
                File.AppendAllText("log.txt", $"[{time}] " + message + Environment.NewLine);
            }
        }
    }
}

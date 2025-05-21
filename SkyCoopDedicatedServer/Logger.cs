using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SkyCoopDedicatedServer
{
    public class Logger
    {
        public static void Log(ConsoleColor color, string message)
        {
            Console.ForegroundColor = color;
            Console.WriteLine($"[{DateTime.Now.ToString()}] {message}");
            Console.ResetColor();
        }
        
        public static void Log(string message)
        {
            Console.WriteLine($"[{DateTime.Now.ToString()}] {message}");
        }
    }
}

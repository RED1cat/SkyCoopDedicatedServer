using System;

#if RELEASE
using Terminal.Gui.App;
using Terminal.Gui.Views;
#endif

namespace SkyCoopDedicatedServer
{
    public class Logger
    {
#if RELEASE
        public static TextView LogView = null;
#endif
        public static void Log(ConsoleColor color, string message)
        {
#if DEBUG
            Console.ForegroundColor = color;
            Console.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] {message}");
            Console.ForegroundColor = ConsoleColor.White;
#elif RELEASE
            if (LogView != null)
            {
                Application.Invoke(() =>
                {
                    LogView.Text += $"[{DateTime.Now.ToString("HH:mm:ss")}] {message}\n";
                    LogView.MoveEnd();
                });
            }
#endif
        }
        
        public static void Log(string message)
        {
#if DEBUG
            Console.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] {message}");
#elif RELEASE
            if( LogView != null)
            {
                Application.Invoke(() =>
                {
                    LogView.Text += $"[{DateTime.Now.ToString("HH:mm:ss")}] {message}\n";
                    LogView.MoveEnd();
                });
            }
#endif
        }

        public static void HandleServerLog(SkyCoopServer.Logger.LogData Data)
        {
            Log(Data.m_Color, Data.m_Message);
        }
    }
}

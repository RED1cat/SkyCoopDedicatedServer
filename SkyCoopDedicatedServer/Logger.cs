using System;
using Terminal.Gui.App;
using Terminal.Gui.Views;

namespace SkyCoopDedicatedServer
{
    public class Logger
    {
        public static TextView LogView = null;
        public static void Log(ConsoleColor color, string message)
        {
            if (LogView != null)
            {
                Application.Invoke(() =>
                {
                    LogView.Text += $"[{DateTime.Now.ToString("HH:mm:ss")}] {message}\n";
                    LogView.MoveEnd();
                });
            }
        }
        
        public static void Log(string message)
        {
            if( LogView != null)
            {
                Application.Invoke(() =>
                {
                    LogView.Text += $"[{DateTime.Now.ToString("HH:mm:ss")}] {message}\n";
                    LogView.MoveEnd();
                });
            }
        }
    }
}

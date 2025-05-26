using System;
using SkyCoopServer;
using System.Threading.Tasks;
using System.Threading;
using static SkyCoopDedicatedServer.Logger;

namespace SkyCoopDedicatedServer
{
    class Program
    {
        public static Server Server;
        public static bool Ready;
        public static Task ConsoleTask;

        public static void Main(string[] args)
        {
            while (true) 
            {
                try
                {
                    if (!Ready)
                    {
                        Thread.Sleep(5000);

                        Ready = true;
                        Server = new Server();
                        Server.StartServer();
                        ConsoleTask = Task.Factory.StartNew(ConsoleWork);
                    }

                    if (Ready)
                    {
                        Server.Update();

                        if (SkyCoopServer.Logger.Logsbuffer.Count > 0)
                        {
                            SkyCoopServer.Logger.LogData log = SkyCoopServer.Logger.Logsbuffer[0];
                            SkyCoopServer.Logger.Logsbuffer.Remove(log);

                            Log(log.m_Color, log.m_Message);
                        }
                    }
                }
                catch (Exception e)
                {
                    Server.m_Instance.Stop();
                    Server.Dispose();
                    Server = null;
                    Ready = false;
                    Log(ConsoleColor.Red, $"Server get error:\n{e.ToString()}");
                    Log(ConsoleColor.DarkRed, "Trying restart server");
                }
            }
        }

        public static void ConsoleWork()
        {
            while (Ready) 
            {
                string command = string.Empty;
                command = Console.ReadLine().ToLower();
                if (!string.IsNullOrEmpty(command))
                {
                    switch (command)
                    {
                        case "quit":
                        case "exit":
                        case "stop":
                        case "shutdown":
                            Server.Dispose();
                            Environment.Exit(0);
                            break;
                        case "reboot":
                        case "restart":
                            Server.Dispose();
                            Server = null;
                            Ready = false;
                            break;
                        default:
                            Console.WriteLine($"Unknown command: {command}");
                            break;
                    }
                }
            }
        }
    }
}
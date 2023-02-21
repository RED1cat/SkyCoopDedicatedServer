using System;
using System.Threading.Tasks;
using System.Runtime.InteropServices;
using SkyCoop;
using GameServer;
using System.Threading;
using static SkyCoop.Shared;

namespace DedicatedServer
{
    public class ConsoleMain
    {
#if DEDICATED_WINDOWS
        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool AllocConsole();

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool FreeConsole();
#endif
        public void Initialize()
        {
#if DEDICATED_WINDOWS
            AllocConsole();
#endif
            Task.Factory.StartNew(ReadConsole);
            MyMod.Initialize();

            Timer timer1 = new Timer(EverySecond, null, 1000, 1000);
            Timer timer2 = new Timer(EveryMinute, null, 5000, 5000);
            Timer timer3 = new Timer(DsSave, null, MyMod.DsSavePerioud * 1000, MyMod.DsSavePerioud * 1000);

            while (true)
            {
                Update();
                if (Shared.DSQuit)
                {
                    break;
                }
            }
#if DEDICATED_WINDOWS
            FreeConsole();
#endif
        }
        private void Update()
        {
            Shared.OnUpdate();
            ThreadManager.UpdateMain();

        }
        private void ReadConsole()
        {
            while (true)
            {
                string command = Console.ReadLine();
                if (command != null)
                {
                    if (command.Contains('\r'))
                    {
                        command = command.Replace("\r", "");
                    }

                    if (command.Contains('/'))
                    {
                        Logger.Log("[XNAConsole] " + command, LoggerColor.Yellow);
                        Logger.Log("[XNAConsole] " + MyMod.ConsoleCommandExec(command, true), LoggerColor.Yellow);
                    }
                    else
                    {
                        Logger.Log("[ServerConsole] " + command, LoggerColor.Yellow);
                        Logger.Log("[ServerConsole] " + Shared.ExecuteCommand(command), LoggerColor.Yellow);
                    }
                }
            }
        }
        private void EverySecond(object obj)
        {
            Shared.EverySecond();
        }
        private void EveryMinute(object obj)
        {
            Shared.EveryInGameMinute();
        }
        private void DsSave(object obj)
        {
            MPSaveManager.SaveGlobalData();
        }
    }
}

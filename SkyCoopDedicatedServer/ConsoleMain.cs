using System;
using System.Threading.Tasks;
using SkyCoop;
using GameServer;
using System.Threading;
using static SkyCoop.Shared;

namespace SkyCoopDedicatedServer
{
    public class ConsoleMain
    {
        public void Initialize()
        {
            Task.Factory.StartNew(ReadConsole);
            MyMod.Initialize();

            Timer timer1 = new Timer(EverySecond, null, 1000, 1000);
            Timer timer2 = new Timer(EveryGameMinute, null, 5000, 5000);
            Timer timer3 = new Timer(DsSave, null, MyMod.DsSavePerioud * 1000, MyMod.DsSavePerioud * 1000);
            Timer timer4 = new Timer(EveryMinute, null, 60000, 60000);

            while (true)
            {
                Update();
                if (Shared.DSQuit)
                {
                    OnExiting();
                    Environment.Exit(1);
                }
            }
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

                    Logger.Log("[ServerConsole] " + command, LoggerColor.Yellow);
                    Logger.Log("[ServerConsole] " + Shared.ExecuteCommand(command), LoggerColor.Yellow);
                }
            }
        }
        private void EverySecond(object obj)
        {
            Shared.EverySecond();
        }
        private void EveryGameMinute(object obj)
        {
            Shared.EveryInGameMinute();
        }
        private void DsSave(object obj)
        {
            MPSaveManager.SaveGlobalData();
        }
        private void EveryMinute(object obj)
        {
           NetworkHelper.СheckingInternetConnection();
        }
        private void OnExiting()
        {
            NetworkHelper.TryClosePort();
        }
    }
}

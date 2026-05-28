using System;
using SkyCoopServer;
using System.Threading.Tasks;
using System.Threading;
using Terminal.Gui.App;
using Terminal.Gui.Views;
using Terminal.Gui.ViewBase;
using Terminal.Gui.Drivers;
using static SkyCoopDedicatedServer.Logger;
using LiteNetLib;

namespace SkyCoopDedicatedServer
{
    class Program
    {
        public static Server Server;
        public static bool Ready;
        public static Label TopInfoLabel;
        public static Window TopWindow;

        public static void Main(string[] args)
        {
            Window top = GuiInit();
            Task.Run(ServerWorker);

            Application.Run(top);
        }

        public static void ExecuteCommand(string cmd)
        {
            switch (cmd)
            {
                case "quit":
                case "exit":
                case "stop":
                case "shutdown":
                    if(Server != null)
                        Server.Dispose();
                    Ready = false;
                    Application.Shutdown();
                    Environment.Exit(0);
                    break;
                case "reboot":
                case "restart":
                    if(Server != null)
                        Server.Dispose();
                    Server = null;
                    Ready = false;
                    break;
                default:
                    Log($"Unknown command: {cmd}");
                    break;
            }
        }

        public static void ServerWorker()
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

                        if(TopInfoLabel != null)
                        {
                            string statmsg = string.Empty;
                            foreach (NetPeer peer in Server.m_Instance.ConnectedPeerList.ToArray())
                            {
                                DataStr.PlayerData player = Server.m_PlayersData.GetPlayer(peer.Id);
                                statmsg += $"[{peer.Id}] Name:{player.m_PlayerName} GameState:{player.m_GamePlayState.ToString()} Scene:{player.m_Scene} Ping:{peer.Ping} PacketLost:{peer.Statistics.PacketLossPercent}%\n";
                            }
                            TopInfoLabel.Text = statmsg;
                            TopWindow.Height = 2 + Server.m_Instance.ConnectedPeersCount;
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

        public static Window GuiInit()
        {
            Application.Init();
            Window top = new Window();

            Window topWindow = new Window()
            {
                Title = "Server Info",
                X = 0,
                Y = 0,
                Width = Dim.Fill(), 
                Height = 3
            };
            Label topInfoLabel = new Label()
            {
                X = 0,
                Y = 0
            };
            topWindow.Add(topInfoLabel);

            Window centerWindow = new Window()
            {
                Title = "Logs",
                X = 0,
                Y = Pos.Bottom(topWindow),
                Width = Dim.Fill(),
                Height = Dim.Fill() - 3
            };
            TextView centerTextView = new TextView()
            {
                X = 0,
                Y = 0,
                Width = Dim.Fill(),
                Height = Dim.Fill(),
                ReadOnly = true,
                WordWrap = true
            };
            centerWindow.Add(centerTextView);

            FrameView bottomFrameView = new FrameView()
            {
                Title = "Input Command",
                X = 0,
                Y = Pos.Bottom(centerWindow),
                Width = Dim.Fill(),
                Height = 3
            };
            TextField bottomTextField = new TextField()
            {
                X = 0,
                Y = 0,
                Width = Dim.Fill()
            };
            bottomFrameView.Add(bottomTextField);

            bottomTextField.KeyDown += (sender, args) =>
            {
                if (args.KeyCode == KeyCode.Enter)
                {
                    string cmd = bottomTextField.Text.ToString();
                    if (!string.IsNullOrWhiteSpace(cmd))
                    {
                        Log(cmd);
                        ExecuteCommand(cmd);
                    }
                    bottomTextField.Text = "";
                    args.Handled = true;
                }
            };

            top.Add(topWindow, centerWindow,  bottomFrameView);
            LogView = centerTextView;
            TopInfoLabel = topInfoLabel;
            TopWindow = topWindow;

            return top;
        }
    }
}
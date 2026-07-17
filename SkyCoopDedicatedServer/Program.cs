using System;
using SkyCoopServer;
using System.Threading.Tasks;
using System.Threading;
using static SkyCoopDedicatedServer.Logger;
using LiteNetLib;
using System.Collections.Generic;


#if RELEASE
using Terminal.Gui.App;
using Terminal.Gui.Views;
using Terminal.Gui.ViewBase;
using Terminal.Gui.Drivers;
using Terminal.Gui.Drawing;
#endif

namespace SkyCoopDedicatedServer
{
    class Program
    {
        public static Server Server;
        public static bool Ready;

#if RELEASE
        public static Label TopInfoLabel;
        public static FrameView TopWindow;
#endif

        public static void Main(string[] args)
        {
#if RELEASE
            Window top = GuiInit();
#endif

            Server.OnLogEvent += Logger.HandleServerLog;

            Task.Run(ServerWorker);

#if DEBUG
            while (true)
            {
                string cmd = Console.ReadLine();
                if(cmd != null)
                    ExecuteCommand(cmd);
            }
#elif RELEASE
            Application.Run(top);
#endif
        }

        public static void ExecuteCommand(string cmd)
        {
            List<string> Args = new List<string>();
            
            if(cmd.Contains(' ')) // ' ' дешелве чем " " ибо так мы обявляем не стринг а один символ если ты не знал.
            {
                string[] CommandAndArgs = cmd.Split(' ');
                cmd = CommandAndArgs[0];

                if(CommandAndArgs.Length > 1)
                {
                    Args.AddRange(CommandAndArgs);
                    Args.RemoveAt(0);
                }
            }

            cmd.ToLower();
            switch (cmd)
            {
                case "quit":
                case "exit":
                case "stop":
                case "shutdown":
                    if (Server != null && Server.m_IsReady)
                    {
                        Server.DisconnectAllPlayers("Server shutdown", true);
                    }
#if RELEASE
                    Application.Shutdown();
#endif
                    Environment.Exit(0);
                    break;
                case "reboot":
                case "restart":
                    if (Server != null && Server.m_IsReady)
                    {
                        Server.DisconnectAllPlayers("Server restarting", true);
                    }
                    Server = null;
                    Ready = false;
                    break;
                case "players":
                    if(Server != null & Ready)
                    {
                        string statmsg = string.Empty;
                        List<NetPeer> peers = new List<NetPeer>();
                        Server.m_Instance.GetConnectedPeers(peers);
                        foreach (NetPeer peer in peers.ToArray())
                        {
                            DataStr.PlayerData player = Server.m_PlayersData.GetPlayer(peer.Id);
                            statmsg += $"[{peer.Id}] Name:{player.m_PlayerName} GameState:{player.m_GamePlayState.ToString()} Scene:{player.m_Scene} Ping:{peer.Ping} PacketLost:{peer.Statistics.PacketLossPercent}%\n";
                        }
                        Log($"Players info:\n{statmsg}");
                    }
                    break;
                case "kick":
                case "disconnect":
                    if (Server != null && Server.m_IsReady)
                    {
                        if(Args.Count == 0)
                        {
                            Log(ConsoleColor.Red, $"Input NAME of the player! {cmd} NameOfPlayer");
                        }
                        if (Args.Count >= 2)
                        {
                            Server.DisconnectPlayer(Args[0], Args[1].Replace('_', ' '));
                        }
                        else
                        {
                            Server.DisconnectPlayer(Args[0]);
                        }
                        Server.DisconnectPlayer(Args[0]);
                    }
                    break;
                case "kickid":
                case "disconnectid":
                    if (Server != null && Server.m_IsReady)
                    {
                        if (Args.Count == 0)
                        {
                            Log(ConsoleColor.Red, $"Input ID of the player! {cmd} 0");
                            return;
                        }
                        if(Args.Count >= 2)
                        {
                            Server.DisconnectPlayer(int.Parse(Args[0]), Args[1].Replace('_',' '));
                        }
                        else
                        {
                            Server.DisconnectPlayer(int.Parse(Args[0]));
                        }
                    }
                    break;
                default:
                    if (Server != null & Ready)
                    {
                        ServerHandle.ProcessCMD(Server, cmd);
                    }
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

#if RELEASE
                        Server.m_Listener.NetworkLatencyUpdateEvent += (NetPeer npeer, int latency) => 
                        {
                            if (TopInfoLabel != null)
                            {
                                string statmsg = string.Empty;
                                List<NetPeer> peers = new List<NetPeer>();
                                Server.m_Instance.GetConnectedPeers(peers);
                                foreach (NetPeer peer in peers.ToArray())
                                {
                                    DataStr.PlayerData player = Server.m_PlayersData.GetPlayer(peer.Id);
                                    statmsg += $"[{peer.Id}] Name:{player.m_PlayerName} GameState:{player.m_GamePlayState.ToString()} Scene:{player.m_Scene} Ping:{peer.Ping} PacketLost:{peer.Statistics.PacketLossPercent}%\n";
                                }
                                TopInfoLabel.Text = statmsg;
                                TopWindow.Height = 2 + Server.m_Instance.ConnectedPeersCount;
                            }
                        };
#endif
                    }

                    if (Ready)
                    {
                        Server.Update();
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

#if RELEASE
        public static Window GuiInit()
        {
            Application.Init();
            Window top = new Window();
            top.SetScheme(new Scheme(new Terminal.Gui.Drawing.Attribute(Color.BrightBlue, Color.Black)));

            FrameView topWindow = new FrameView()
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

            FrameView centerWindow = new FrameView()
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
#endif
    }
}
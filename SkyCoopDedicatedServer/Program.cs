using LiteNetLib;
using SkyCoopServer;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using static SkyCoopDedicatedServer.Logger;


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

        public static int ServerSeed = 0;
        public static string ServerExp = string.Empty;

#if RELEASE
        public static Label TopInfoLabel;
        public static FrameView TopWindow;
#endif

        public static void Main(string[] args)
        {
#if RELEASE
            Window top = GuiInit();
#endif
            for(int i = 0; i < args.Length; i++)
            {
                if (args[i] == "-seed" && i != args.Length - 1) 
                {
                    int.TryParse(args[i + 1], out ServerSeed);
                }

                if(args[i] == "-exp" && i != args.Length - 1)
                {
                    ServerExp = args[i + 1];
                }
            }

            Server.OnLogEvent += Logger.HandleServerLog;

            Task.Run(ServerWorker);

            AppDomain.CurrentDomain.UnhandledException += (s, e) => { //И почему я об этой штуки узнаю так поздно(
                NLog.LogManager.GetLogger("Application").Error(e.ExceptionObject as Exception, $"DedicatedServer has exception: {(e.ExceptionObject as Exception).Message}.\nTrace:{(e.ExceptionObject as Exception).StackTrace}");
                NLog.LogManager.Flush();
            };
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
            if(Server == null || Ready == false)
            {
                Log(ConsoleColor.Red, "Server not Ready!");
                return;
            }

            List<string> Args = new List<string>();
            if(cmd.Contains(' ')) // ' ' дешелве чем " " ибо так мы объявляем не стринг а один символ если ты не знал. ЗНАЛ, ПРИЧЕМ ОЧЕНЬ ДАВНО.
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
                        Server.SaveToFile();
                        Server.DisconnectAllPlayers("Server shutdown", true);
                    }
#if RELEASE
                    Application.Shutdown();
#endif
                    NLog.LogManager.Shutdown();
                    Environment.Exit(0);
                    break;

                case "reboot":
                case "restart":
                    if (Server != null && Server.m_IsReady)
                    {
                        Server.SaveToFile();
                        Server.DisconnectAllPlayers("Server restarting", true);
                    }
                    Server = null;
                    Ready = false;
                    break;
                case "save":
                    if (Server != null && Server.m_IsReady)
                    {
                        Server.SaveToFile();
                    }
                    break;
                case "players":
                    string statmsg = string.Empty;
                    List<NetPeer> peers = new List<NetPeer>();
                    Server.m_Instance.GetConnectedPeers(peers);
                    peers.Sort();
                    foreach (NetPeer peer in peers.ToArray())
                    {
                        DataStr.PlayerData player = Server.m_PlayersData.GetPlayer(peer.Id);
                        statmsg += $"[{peer.Id}] Name:{player.m_PlayerName} GameState:{player.m_GamePlayState.ToString()} Scene:{player.m_Scene} Ping:{peer.Ping} PacketLost:{peer.Statistics.PacketLossPercent}%\n";
                    }
                    Log($"Players info:\n{statmsg}");
                    break;

                case "showstats":
                case "stats":
                    if (Args.Count == 1)
                    {
                        bool state;
                        if(bool.TryParse(Args[0], out state))
                        {
                            Server.m_Instance.EnableStatistics = state;
                            Log($"Server Statistics is {Server.m_Instance.EnableStatistics}");
                        }
                        else
                        {
                            Log(ConsoleColor.Red, "Wrong argument");
                        }
                    }
                    else if(Server.m_Instance.EnableStatistics == false)
                    {
                        Server.m_Instance.EnableStatistics = true;
                        Log($"Server Statistics is {Server.m_Instance.EnableStatistics}");
                    }

                    if(Server.m_Instance.EnableStatistics == true)
                    {
                        NetStatistics netstats = Server.m_Instance.Statistics;
                        Log(ConsoleColor.DarkYellow, $"[ServerStatistics] PacketsSent:{netstats.PacketsSent}({netstats.BytesSent}|Bytes) PacketsReceived:{netstats.PacketsReceived}({netstats.BytesReceived}|Bytes) PacketLoss:{netstats.PacketLoss}({netstats.PacketLossPercent}%)");
                    }
                    break;

                case "simlatency":
                    if(Args.Count == 1)
                    {
                        int latency;
                        if(int.TryParse(Args[0], out latency))
                        {
                            Server.m_Instance.SimulationMaxLatency = latency;
                            Log($"Server Simulate Max Latency is {Server.m_Instance.SimulationMaxLatency}");

                            if(Server.m_Instance.SimulateLatency == false)
                            {
                                Server.m_Instance.SimulateLatency = true;
                                Log($"Server Simulate Latency is {Server.m_Instance.SimulateLatency}");
                            }
                        }
                        else
                        {
                            Log(ConsoleColor.Red, "Wrong argument");
                        }
                        break;
                    }
                    Server.m_Instance.SimulateLatency = !Server.m_Instance.SimulateLatency;
                    Log($"Server Simulate Latency is {Server.m_Instance.SimulateLatency}");
                    break;

                case "simpacketloss":
                    if (Args.Count == 1)
                    {
                        int packetLossChance;
                        if (int.TryParse(Args[0], out packetLossChance) && (packetLossChance >= 1 & packetLossChance <= 100))
                        {
                            Server.m_Instance.SimulationPacketLossChance = packetLossChance;
                            Log($"Server Simulate PacketLossChance is {Server.m_Instance.SimulationPacketLossChance}");

                            if (Server.m_Instance.SimulatePacketLoss == false)
                            {
                                Server.m_Instance.SimulatePacketLoss = true;
                                Log($"Server Simulate PacketLossChance is {Server.m_Instance.SimulatePacketLoss}");
                            }
                        }
                        else
                        {
                            Log(ConsoleColor.Red, "Wrong argument");
                        }
                        break;
                    }
                    Server.m_Instance.SimulatePacketLoss = !Server.m_Instance.SimulatePacketLoss;
                    Log($"Server Simulate PacketLoss is {Server.m_Instance.SimulatePacketLoss}");
                    break;

                case "kick":
                case "disconnect":
                    if(Args.Count == 0)
                    {
                        Log(ConsoleColor.Red, $"Input NAME of the player! {cmd} NameOfPlayer");
                        break;
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
                    break;

                case "kickid":
                case "disconnectid":
                    if (Args.Count == 0)
                    {
                        Log(ConsoleColor.Red, $"Input ID of the player! {cmd} 0");
                        break;
                    }
                    if(Args.Count >= 2)
                    {
                        Server.DisconnectPlayer(int.Parse(Args[0]), Args[1].Replace('_',' '));
                    }
                    else
                    {
                        Server.DisconnectPlayer(int.Parse(Args[0]));
                    }
                    break;

                default:
                    ServerHandle.ProcessCMD(Server, cmd);
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
                        Log(ConsoleColor.DarkGreen, "Starting server!");
                        Thread.Sleep(5000);

                        Ready = true;
                        Server = new Server(ServerSeed, ServerExp);
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
                    Server.SaveToFile();
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
using LiteNetLib;
using SkyCoopServer;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using static SkyCoopDedicatedServer.Logger;

namespace SkyCoopDedicatedServer
{
    class Program
    {
        public static Server Server;
        public static bool Ready;

        public static void Main(string[] args)
        {
            FilesManager.InitFolders();
            
            Server.OnLogEvent += Logger.HandleServerLog;
            
            Task.Run(ServerWorker);

            AppDomain.CurrentDomain.ProcessExit += (S, e) => {
                Log($"{Environment.ExitCode}");
            };
            AppDomain.CurrentDomain.UnhandledException += (s, e) => { //И почему я об этой штуки узнаю так поздно(
                NLog.LogManager.GetLogger("Application").Error(e.ExceptionObject as Exception, $"Server exception: {(e.ExceptionObject as Exception).Message}.\nTrace:{(e.ExceptionObject as Exception).StackTrace}");
                NLog.LogManager.Flush();
            };
#if DEBUG
            while (true)
            {
                string cmd = Console.ReadLine();
                if(cmd != null)
                    ExecuteCommand(cmd);
            }
        }

        public static void ExecuteCommand(string cmd)
        {
            if(Server == null || Ready == false) //это типа шутка да?
            {
                Log(ConsoleColor.Red, "Server is not Ready!");
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
                    Server.SaveToFile();
                    Server.DisconnectAllPlayers("Server restarting", true);

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

                case "sceneloaded":
                case "scenesloaded":
                case "loadedscenes":
                case "scenes":
                    Log(ConsoleColor.DarkYellow, "Scenes loaded:");
                    foreach(var scene in Server.m_ScenesData.m_LoadedScenes.Values.ToList())
                    {
                        Log(ConsoleColor.DarkYellow, scene.m_SceneName);
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
                        Log(ConsoleColor.DarkGreen, "Starting server...");
                        Thread.Sleep(5000);

                        Ready = true;
                        Server = new Server(FilesManager.LoadServerCFG());
                        Server.StartServer();
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
                    Log(ConsoleColor.Red, $"Server error:\n{e.ToString()}");
                    Log(ConsoleColor.DarkRed, "Trying to restart server");
                }
            }
        }
    }
}
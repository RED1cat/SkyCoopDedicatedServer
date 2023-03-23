using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace SkyCoopDedicatedServerGraphBuilder
{
    internal class XnaMain : Game
    {
        public static SpriteFont font;
        public static Texture2D fontBg;
        public static GameWindow gw;

        public List<float> StatsGraphPoints = new List<float>();
        public List<string> StatsGraphDates = new List<string>();
        public static Vector2 GraphBgPossition = new Vector2(0, 0);
        public static Rectangle GraphBgRect = new Rectangle(0, 0, 0, 0);
        public Graph StatsGraph;

        private GraphicsDeviceManager _graphics;
        public static GraphicsDevice _graphicsDevice;
        private SpriteBatch _spriteBatch;
        private static SpriteBatch _spriteBatchStatic;

        public int m_MaxPlayers;
        public int m_Seed;
        public static string m_ConfigPath = $"{AppDomain.CurrentDomain.BaseDirectory}../server.json";
        public static Day m_TodayStats = new Day();
        public static Day m_AllTimeStats = new Day();
        public XnaMain()
        {
            _graphics = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";
            IsMouseVisible = true;
            InactiveSleepTime = new TimeSpan(0);
        }
        public class PlayerOnlineTime
        {
            public int TotalPlayers = 0;
            public int Days = 0;
            public float Average()
            {
                return TotalPlayers / Days;
            }
            public PlayerOnlineTime(int p, int d)
            {
                TotalPlayers = p;
                Days = d;
            }
        }
        protected override void Initialize()
        {
            LoadConfigData();

            m_AllTimeStats = LoadDayStats("AllTime");
            //DateTime DT = System.DateTime.Now;
            //string FileName = DT.Day + "_" + DT.Month + "_" + DT.Year;
            //m_TodayStats = LoadDayStats(FileName);

            base.Initialize();
            CustomConsole.Logger.Log("[Console] For help, enter /help", CustomConsole.Logger.LoggerColor.Yellow);
        }
        protected override void LoadContent()
        {
            _spriteBatch = new SpriteBatch(GraphicsDevice);
            _spriteBatchStatic = _spriteBatch;
            _graphicsDevice = GraphicsDevice;
            gw = Window;
            font = Content.Load<SpriteFont>("font");
            fontBg = Content.Load<Texture2D>("fontBg");
        }
        public static void BuildGraph()
        {
            Program.xnaMain.StatsGraph = new Graph(_graphicsDevice, _spriteBatchStatic, new Point(740, 200));
            Program.xnaMain.StatsGraph.Position = new Vector2(30, 250);
            Program.xnaMain.StatsGraph.MaxValue = Program.xnaMain.m_MaxPlayers;
            Program.xnaMain.StatsGraph.Type = Graph.GraphType.Line;
            GraphBgPossition = new Vector2(Program.xnaMain.StatsGraph.Position.X, 50);
            GraphBgRect = new Rectangle(0, 0, Program.xnaMain.StatsGraph.Size.X, Program.xnaMain.StatsGraph.Size.Y + 198);
        }
        public void NewGraph(Dictionary<string, int> DayStat)
        {
            BuildGraph();
            StatsGraphPoints.Clear();
            StatsGraphDates.Clear();
            foreach (var item in DayStat)
            {
                StatsGraphPoints.Add(item.Value);
                StatsGraphDates.Add(item.Key);
            }
        }
        public void NewGraph(Dictionary<string, Dictionary<string, int>> DaysStat)
        {
            BuildGraph();
            Program.xnaMain.StatsGraphPoints.Clear();
            Program.xnaMain.StatsGraphDates.Clear();
            Dictionary<string, PlayerOnlineTime> Buffer = new Dictionary<string, PlayerOnlineTime>();
            foreach (var item in DaysStat)
            {
                Dictionary<string, int> Day = item.Value;
                string DayKey = item.Key;

                foreach (var item2 in Day)
                {
                    string TimeKey = item2.Key;
                    int Players = item2.Value;
                    if (Buffer.ContainsKey(TimeKey))
                    {
                        Buffer[TimeKey].TotalPlayers += Players;
                    }
                    else
                    {
                        Buffer.Add(TimeKey, new PlayerOnlineTime(Players, 1));
                    }
                }
            }
            foreach (var item in Buffer)
            {
                Program.xnaMain.StatsGraphPoints.Add(item.Value.Average());
                Program.xnaMain.StatsGraphDates.Add(item.Key);
            }
        }
        protected override void Update(GameTime gameTime)
        {
            CustomConsole.Updata(gameTime);

            base.Update(gameTime);
        }
        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(Color.DarkGray);
            _spriteBatch.Begin();


            CustomConsole.Draw(_spriteBatch, gameTime);


            _spriteBatch.End();

            if (StatsGraph != null)
            {
                _spriteBatch.Begin();
                _spriteBatch.Draw(fontBg, GraphBgPossition, GraphBgRect, Color.White);
                _spriteBatch.Draw(fontBg, new Vector2(StatsGraph.Position.X, StatsGraph.Position.Y + 5), new Rectangle(0, 0, GraphBgRect.Width, 5), Color.Gray);
                _spriteBatch.End();
                StatsGraph.Draw(StatsGraphPoints, Color.Orange, StatsGraphDates);
            }

            base.Draw(gameTime);
        }
        protected override void OnExiting(object sender, EventArgs args)
        {
            base.OnExiting(sender, args);
        }

        public static string ExecuteCommand(string command)
        {
            switch (command)
            {
                case "graph":
                    {
                        if (m_TodayStats.ActivitySnapshots.Count > 0)
                        {
                            if (Program.xnaMain.StatsGraph == null)
                            {
                                Program.xnaMain.NewGraph(m_TodayStats.ActivitySnapshots.First().Value);
                            }
                            else
                            {
                                Program.xnaMain.StatsGraph = null;
                                Program.xnaMain.StatsGraphDates.Clear();
                                Program.xnaMain.StatsGraphPoints.Clear();
                            }
                            return "Graph Done";
                        }
                        else
                        {
                            return "ActivitySnapshots empty";
                        }
                    }
                default:
                    {
                        return "Unknown command";
                    }

            }
        }
        private static void LoadConfigData()
        {
            if(File.Exists(m_ConfigPath))
            {
                CustomConsole.Logger.Log("Reading server.json...", CustomConsole.Logger.LoggerColor.Blue);
                string readText = string.Empty;
                try
                {
                    readText = System.IO.File.ReadAllText(m_ConfigPath);
                }
                catch
                {
                    return;
                }
                finally
                {
                    DedicatedServerData ServerData = TinyJSON.JSON.Load(readText).Make<DedicatedServerData>();
                    CustomConsole.Logger.Log("Server settings: ", CustomConsole.Logger.LoggerColor.Blue);
                    CustomConsole.Logger.Log("SaveSlot: " + ServerData.SaveSlot, CustomConsole.Logger.LoggerColor.Blue);
                    CustomConsole.Logger.Log("ItemDupes: " + ServerData.ItemDupes, CustomConsole.Logger.LoggerColor.Blue);
                    CustomConsole.Logger.Log("ContainersDupes: " + ServerData.ContainersDupes, CustomConsole.Logger.LoggerColor.Blue);
                    CustomConsole.Logger.Log("SpawnStyle: " + ServerData.SpawnStyle, CustomConsole.Logger.LoggerColor.Blue);
                    CustomConsole.Logger.Log("MaxPlayers: " + ServerData.MaxPlayers, CustomConsole.Logger.LoggerColor.Blue);
                    CustomConsole.Logger.Log("UsingSteam: " + ServerData.UsingSteam, CustomConsole.Logger.LoggerColor.Blue);
                    CustomConsole.Logger.Log("Ports: " + ServerData.Ports, CustomConsole.Logger.LoggerColor.Blue);
                    CustomConsole.Logger.Log("Cheats: " + ServerData.Cheats, CustomConsole.Logger.LoggerColor.Blue);
                    CustomConsole.Logger.Log("SteamServerAccessibility: " + ServerData.SteamServerAccessibility, CustomConsole.Logger.LoggerColor.Blue);
                    CustomConsole.Logger.Log("RCON: (SECURED)", CustomConsole.Logger.LoggerColor.Blue);
                    CustomConsole.Logger.Log("DropUnloadPeriod: " + ServerData.DropUnloadPeriod, CustomConsole.Logger.LoggerColor.Blue);
                    CustomConsole.Logger.Log("SaveScamProtection: " + ServerData.SaveScamProtection, CustomConsole.Logger.LoggerColor.Blue);
                    CustomConsole.Logger.Log("ModValidationCheck: " + ServerData.ModValidationCheck, CustomConsole.Logger.LoggerColor.Blue);
                    CustomConsole.Logger.Log("ExperienceMode: " + ServerData.ExperienceMode, CustomConsole.Logger.LoggerColor.Blue);
                    CustomConsole.Logger.Log("StartRegion: " + ServerData.StartRegion, CustomConsole.Logger.LoggerColor.Blue);

                    Program.xnaMain.m_MaxPlayers = ServerData.MaxPlayers;
                    Program.xnaMain.m_Seed = ServerData.Seed;
                }
            }
        }
        public class DedicatedServerData
        {
            public string SaveSlot = "UnspecifiedSave";
            public bool ItemDupes = false;
            public bool ContainersDupes = false;
            public int SpawnStyle = 0;
            public int MaxPlayers = 2;
            public bool UsingSteam = false;
            public int Ports = 26950;
            public string[] WhiteList;
            public string ServerName = "";
            public int Cheats = 2;
            public int SteamServerAccessibility = 2;
            public string RCON = "12345";
            public int DropUnloadPeriod = 5;
            public bool SaveScamProtection = false;
            public bool ModValidationCheck = false;
            public int ExperienceMode = 0;
            public int StartRegion = 0;
            public int Seed = 0;
            public bool PVP = false;
            public int SavingPeriod = 60;
            public int RestartPerioud = 10800;
        }
        public class Day
        {
            public int Visits = 0;
            public int UniqueVisits = 0;
            public int ExpeditionsCompleted = 0;
            public Dictionary<string, int> VisitsHistory = new Dictionary<string, int>();
            public PlayTime OnlineTime = new PlayTime();
            public PlayTime EmptyTime = new PlayTime();
            public Dictionary<string, Dictionary<string, int>> ActivitySnapshots = new Dictionary<string, Dictionary<string, int>>();
            public int Deaths = 0;
            public ResourcesStatistic Looted = new ResourcesStatistic();
            public Dictionary<string, PlayerStatistic> Players = new Dictionary<string, PlayerStatistic>();
            public Dictionary<int, PlayTime> RegionsHistory = new Dictionary<int, PlayTime>();

            //public string GetString(bool IncludePlayersStats = false, bool HideMAC = true, bool HideRegionPlayTime = true)
            //{
            //    string Info = "Visits " + Visits +
            //        "\nUnique Visits " + UniqueVisits +
            //        "\nServer Works " + OnlineTime.GetString() +
            //        "\nEmpty Time " + EmptyTime.GetString() +
            //        "\nPlayers Died " + Deaths +
            //        "\nPicked Gears " + Looted.GearsPicked +
            //        "\nLooted Containers " + Looted.ContainersLooted +
            //        "\nPlants Harvested " + Looted.PlantsHarvested +
            //        "\nAnimals Killed " + Looted.AnimalsKilled +
            //        "\nExpeditions Completed " + ExpeditionsCompleted;

            //    if (!HideRegionPlayTime && RegionsHistory.Count > 0)
            //    {
            //        Info += "\nRegions Play Time:";

            //        foreach (var item in RegionsHistory)
            //        {
            //            Info += "\n" + GetRegionName(item.Key) + ": " + item.Value.GetString();
            //        }
            //    }

            //    if (IncludePlayersStats && Players.Count > 0)
            //    {
            //        Info += "\nToday Statistic Of Players:";
            //        foreach (var item in Players)
            //        {
            //            Info += "\n" + item.Value.GetString(HideMAC, HideRegionPlayTime);
            //        }
            //    }
            //    return Info;
            //}
        }
        public class PlayTime
        {
            public int Seconds = 0;
            public int Minutes = 0;
            public int Hours = 0;
            public int Days = 0;

            //public string GetString()
            //{
            //    string Info = "";
            //    if (Seconds > 1)
            //    {
            //        Info = Seconds + " Seconds";
            //    }
            //    else
            //    {
            //        Info = "1 Second";
            //    }
            //    if (Minutes > 0)
            //    {
            //        if (Minutes == 1)
            //        {
            //            Info = "1 Minute " + Info;
            //        }
            //        else
            //        {
            //            Info = Minutes + " Minutes " + Info;
            //        }
            //    }
            //    if (Hours > 0)
            //    {
            //        if (Hours == 1)
            //        {
            //            Info = "1 Hour " + Info;
            //        }
            //        else
            //        {
            //            Info = Hours + " Hours " + Info;
            //        }
            //    }
            //    if (Days > 0)
            //    {
            //        if (Days == 1)
            //        {
            //            Info = "1 Day " + Info;
            //        }
            //        else
            //        {
            //            Info = Days + " Days " + Info;
            //        }
            //    }
            //    return Info;
            //}
        }
        public class ResourcesStatistic
        {
            public int GearsPicked = 0;
            public int ContainersLooted = 0;
            public int AnimalsKilled = 0;
            public int PlantsHarvested = 0;
        }
        public class PlayerStatistic
        {
            public string Name = "";
            public string MAC = "";
            public int Visits = 1;
            public int Deaths = 0;
            public int ExpeditionsCompleted = 0;
            public PlayTime TotalPlayTime = new PlayTime();
            public Dictionary<int, PlayTime> RegionsHistory = new Dictionary<int, PlayTime>();
            public ResourcesStatistic Looted = new ResourcesStatistic();
            public PlayerStatistic(string mac = "", string name = "")
            {
                MAC = mac;
                Name = name;
            }
            public PlayerStatistic()
            {

            }

            //public string GetString(bool HideMAC = true, bool HideRegionPlayTime = true)
            //{
            //    string Info = "Name " + Name;
            //    if (!HideMAC)
            //    {
            //        Info += "\nMAC " + MAC;
            //    }
            //    Info += "\nTotal Play Time " + TotalPlayTime.GetString() +
            //        "\nServer Visits " + Visits +
            //        "\nDeaths " + Deaths +
            //        "\nPicked Gears " + Looted.GearsPicked +
            //        "\nLooted Containers " + Looted.ContainersLooted +
            //        "\nPlants Harvested " + Looted.PlantsHarvested +
            //        "\nAnimals Killed " + Looted.AnimalsKilled +
            //        "\nExpeditions Completed " + ExpeditionsCompleted;

            //    if (!HideRegionPlayTime && RegionsHistory.Count > 0)
            //    {
            //        Info += "\nRegions Play Time:";
            //        foreach (var item in RegionsHistory)
            //        {
            //            Info += "\n" + GetRegionName(item.Key) + ": " + item.Value.GetString();
            //        }
            //    }
            //    return Info;
            //}
        }
        public static Day LoadDayStats(string TodayFileName)
        {
            string Data;
            if (Directory.Exists($"{AppDomain.CurrentDomain.BaseDirectory}../{Program.xnaMain.m_Seed}"))
            {
                try
                {
                    Data = System.IO.File.ReadAllText($"{AppDomain.CurrentDomain.BaseDirectory}../{Program.xnaMain.m_Seed}/Statistic/{TodayFileName}");
                }
                catch
                {
                    return new Day();
                }
                return TinyJSON.JSON.Load(Data).Make<Day>();
            }
            return new Day();
        }
    }
}

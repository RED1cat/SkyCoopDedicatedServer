using DedicatedServer;
using GameServer;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SkyCoop
{
    public class MyMod : Game
    {
        public static class BuildInfo
        {
            public const string Name = "Sky Co-op LTS";
            public const string Description = "Multiplayer mod";
            public const string Author = "Filigrani";
            public const string Company = null;
            public const string Version = "0.11.2";
            public const string DownloadLink = null;
            public const int RandomGenVersion = 5;
        }
        
        public static bool DedicatedServerAppMode = true;
        public static bool IsCycleSkiping = false;
        public static bool iAmHost = true;
        public static bool IsDead = true;
        public static bool AnimalsController = false;
        public static bool DebugTrafficCheck = false;
        public static bool PVP = false;

        public static int MinutesFromStartServer = 0;
        public static int MaxPlayers = 2;
        public static int OverridedMinutes = 0;
        public static int OverridedHourse = 12;
        public static int PlayedHoursInOnline = 0;
        public static int MyTicksOnScene = 0;
        public static int levelid = 0;
        public static int TimeOutSeconds = 300;
        public static int TimeOutSecondsForLoaders = 300;
        public static int PlayersOnServer = 0;
        public static int NoHostResponceSeconds = 0;
        public static int RestartPerioud = -1;
        public static int SecondsWithoutSaving = 0;
        public static int DsSavePerioud = 60;

        public static string OveridedTime = "12:0";
        public static string level_guid = "";
        public static string NotificationString = "";
        public static string RCON = "12345";
        public static string SavedSceneForSpawn = "";

        public static System.Numerics.Vector3 SavedPositionForSpawn = System.Numerics.Vector3.Zero;
        public static DataStr.CustomChallengeData CurrentCustomChalleng = new DataStr.CustomChallengeData();
        public static DataStr.CustomChallengeRules CurrentChallengeRules = new DataStr.CustomChallengeRules();
        public static DataStr.ServerConfigData ServerConfig = new DataStr.ServerConfigData();

        public static List<DataStr.DeathContainerData> DeathCreates = new List<DataStr.DeathContainerData>();
        public static List<DataStr.BrokenFurnitureSync> BrokenFurniture = new List<DataStr.BrokenFurnitureSync>();
        public static List<DataStr.ClimbingRopeSync> DeployedRopes = new List<DataStr.ClimbingRopeSync>();
        public static List<DataStr.ContainerOpenSync> LootedContainers = new List<DataStr.ContainerOpenSync>();
        public static List<string> HarvestedPlants = new List<string>();
        public static List<DataStr.ShowShelterByOther> ShowSheltersBuilded = new List<DataStr.ShowShelterByOther>();
        public static List<DataStr.MultiPlayerClientData> playersData = new List<DataStr.MultiPlayerClientData>();
        public static List<DataStr.PickedGearSync> RecentlyPickedGears = new List<DataStr.PickedGearSync>();

        public static Dictionary<string, int> BannedSpawnRegions = new Dictionary<string, int>();
        public static Dictionary<int, bool> FoundCairns = new Dictionary<int, bool>();
        public static Dictionary<string, bool> OpenableThings = new Dictionary<string, bool>();
        public static Dictionary<int, string> SlicedJsonDataBuffer = new Dictionary<int, string>();

        static float currentTime0 = 0f;
        static float CurrentTime1 = 0f;
        static float CurrentTime2 = 0f;
        public static SpriteFont font;
        public static Texture2D fontBg;
        public static GameWindow gw;
        NetworkHelper networkPort;
        static bool portOpen = false;
        public static List<float> StatsGraphPoints = new List<float>();
        public static List<string> StatsGraphDates = new List<string>();
        public static Vector2 GraphBgPossition = new Vector2(0, 0);
        public static Rectangle GraphBgRect = new Rectangle(0, 0, 0, 0);
        public static Graph StatsGraph;

        public static string GetGearNameByID(int index)
        {
            return "";
        }
        public static int GetGearIDByName(string name)
        {
            return -1;
        }
        private GraphicsDeviceManager _graphics;
        public static GraphicsDevice _graphicsDevice;
        private SpriteBatch _spriteBatch;
        private static SpriteBatch _spriteBatchStatic;
        private FrameCounter _frameCounter = new FrameCounter();

        public MyMod()
        {
            _graphics = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";
            IsMouseVisible = true;
            InactiveSleepTime = new TimeSpan(0);
        }

        protected override void Initialize()
        {
            Supporters.GetSupportersList(false);
            ResourceIndependent.Init();

            DataStr.DedicatedServerData config = Shared.LoadDedicatedServerConfig();
            MPSaveManager.Seed = config.Seed;
            MPSaveManager.LoadGlobalData();

            Shared.HostAServer(config.Ports);
            networkPort = new NetworkHelper(config.Ports);
            portOpen= true;

            ModsValidation.GetModsHash();

            base.Initialize();
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
            StatsGraph = new Graph(_graphicsDevice, _spriteBatchStatic, new Point(740, 200));
            StatsGraph.Position = new Vector2(30, 250);
            StatsGraph.MaxValue = Server.MaxPlayers;
            StatsGraph.Type = Graph.GraphType.Line;
            GraphBgPossition = new Vector2(StatsGraph.Position.X, 50);
            GraphBgRect = new Rectangle(0, 0, StatsGraph.Size.X, StatsGraph.Size.Y + 198);
        }

        public static void NewGraph(Dictionary<string, int> DayStat)
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


        public static void NewGraph(Dictionary<string, Dictionary<string, int>> DaysStat)
        {
            BuildGraph();
            StatsGraphPoints.Clear();
            StatsGraphDates.Clear();
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
                    } else
                    {
                        Buffer.Add(TimeKey, new PlayerOnlineTime(Players, 1));
                    }
                }
            }
            foreach (var item in Buffer)
            {
                StatsGraphPoints.Add(item.Value.Average());
                StatsGraphDates.Add(item.Key);
            }
        }

        protected override void Update(GameTime gameTime)
        {

            Shared.OnUpdate();
            ThreadManager.UpdateMain();

            currentTime0 += (float)gameTime.ElapsedGameTime.TotalSeconds;
            CurrentTime1 += (float)gameTime.ElapsedGameTime.TotalSeconds;
            CurrentTime2 += (float)gameTime.ElapsedGameTime.TotalSeconds;

            if (currentTime0 >= 1f)
            {
                currentTime0 -= 1f;
                Shared.EverySecond();
            }
            if (CurrentTime1 >= 5f)
            {
                CurrentTime1 -= 5f;
                Shared.EveryInGameMinute();
            }
            if (CurrentTime2 >= (float)DsSavePerioud)
            {
                CurrentTime2 -= (float)DsSavePerioud;
                MPSaveManager.SaveGlobalData();
            }

            CustomConsole.Updata(gameTime);

            if (Shared.DSQuit)
            {
                this.Exit();
            }


            base.Update(gameTime);
        }

        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(Color.DarkGray);
            _spriteBatch.Begin();


            CustomConsole.Draw(_spriteBatch, gameTime);


            DrawServerInfo(_spriteBatch, gameTime);
            
            _spriteBatch.End();

            if(StatsGraph != null)
            {
                _spriteBatch.Begin();
                _spriteBatch.Draw(fontBg, GraphBgPossition, GraphBgRect, Color.White);
                _spriteBatch.Draw(fontBg, new Vector2(StatsGraph.Position.X, StatsGraph.Position.Y+5), new Rectangle(0,0, GraphBgRect.Width, 5), Color.Gray);
                _spriteBatch.End();
                StatsGraph.Draw(StatsGraphPoints, Color.Orange, StatsGraphDates);
            }

            base.Draw(gameTime);
        }
        protected override void OnExiting(object sender, EventArgs args)
        {
            if (portOpen)
            {
                networkPort.TryClosePort();
            }
            base.OnExiting(sender, args);
        }
        private void DrawServerInfo(SpriteBatch _spriteBatch, GameTime gameTime)
        {
            float deltaTime = (float)gameTime.ElapsedGameTime.TotalSeconds;
            _frameCounter.Update(deltaTime);

            _spriteBatch.Draw(fontBg, new Vector2(0, 0), new Rectangle(0, 0, 100, 44), Color.White);

            _spriteBatch.DrawString(font, "Players: " + PlayersOnServer.ToString(), new Vector2(5, 5), Color.Coral);
            _spriteBatch.DrawString(font, $"FPS: {Math.Round(_frameCounter.AverageFramesPerSecond, 3)}", new Vector2(5, 25), Color.Coral);
        }
        public static string ConsoleCommandExec(string command)
        {
            command = command.Replace("/", "");
            switch (command)
            {
                case "test":
                    return "test123\n124test";
                case "graph":
                    if(MPStats.TodayStats.ActivitySnapshots.Count > 0)
                    {
                        if(StatsGraph == null)
                        {
                            NewGraph(MPStats.TodayStats.ActivitySnapshots.First().Value);
                        } else
                        {
                            StatsGraph = null;
                            StatsGraphDates.Clear();
                            StatsGraphPoints.Clear();
                        }
                        return "Graph Done";
                    } else
                    {
                        return "ActivitySnapshots empty";
                    }
                case "globalgraph":
                    if (MPStats.AllTimeStats.ActivitySnapshots.Count > 0)
                    {
                        if (StatsGraph == null)
                        {
                            NewGraph(MPStats.AllTimeStats.ActivitySnapshots);
                        } else
                        {
                            StatsGraph = null;
                            StatsGraphDates.Clear();
                            StatsGraphPoints.Clear();
                        }
                        return "Graph Done";
                    } else
                    {
                        return "ActivitySnapshots empty";
                    }
                default:
                    return "Unknown command";
            }
        }
    }
}
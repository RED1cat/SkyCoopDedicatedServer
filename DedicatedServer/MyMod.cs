using DedicatedServer;
using GameServer;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;
using System.Diagnostics.Metrics;
using System.Text;

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
            public const string Version = "0.10.5";
            public const string DownloadLink = null;
            public const int RandomGenVersion = 4;
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
        public static List<DataStr.PickedGearSync> PickedGears = new List<DataStr.PickedGearSync>();
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

        public static string GetGearNameByID(int index)
        {
            return "";
        }
        public static int GetGearIDByName(string name)
        {
            return -1;
        }
        private GraphicsDeviceManager _graphics;
        private SpriteBatch _spriteBatch;

        public MyMod()
        {
            _graphics = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";
            IsMouseVisible = true;
        }

        protected override void Initialize()
        {
            Supporters.GetSupportersList(true);
            ResourceIndependent.Init();

            DataStr.DedicatedServerData config = Shared.LoadDedicatedServerConfig();
            MPSaveManager.Seed = config.Seed;
            MPSaveManager.LoadGlobalData();
            Shared.HostAServer(config.Ports);

            base.Initialize();
        }

        protected override void LoadContent()
        {
            _spriteBatch = new SpriteBatch(GraphicsDevice);
            gw = Window;
            font = Content.Load<SpriteFont>("File");
            fontBg = Content.Load<Texture2D>("fontBg");
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

            CustomConsole.Updata();


            base.Update(gameTime);
        }

        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(Color.DarkGray);
            _spriteBatch.Begin();

            CustomConsole.Draw(_spriteBatch);

            _spriteBatch.End();

            base.Draw(gameTime);
        }


    }
}
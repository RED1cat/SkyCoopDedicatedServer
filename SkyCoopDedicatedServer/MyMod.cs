using SkyCoopDedicatedServer;
using System;
using System.Collections.Generic;

namespace SkyCoop
{
    public class MyMod
    {
        public static class BuildInfo
        {
            public const string Name = "Sky Co-op LTS";
            public const string Description = "Multiplayer mod";
            public const string Author = "Filigrani";
            public const string Company = null;
            public const string Version = "0.11.6";
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
        public static string CustomServerName = "Dedicated Server";

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



        public static string GetGearNameByID(int index)
        {
            return "";
        }
        public static int GetGearIDByName(string name)
        {
            return -1;
        }


        public static void Initialize()
        {
            Supporters.GetSupportersList(false);
            ResourceIndependent.Init();

            DataStr.DedicatedServerData config = Shared.LoadDedicatedServerConfig();
            MPSaveManager.Seed = config.Seed;
            MPSaveManager.LoadGlobalData();

            Shared.HostAServer(config.Ports);
            Program.networkPort = new NetworkHelper(config.Ports);

            DiscordManager.Init();


            //SteamMaster.PingSteamMaster();
            //SteamMaster.SendSignalToMaster();
        }

        public static string ExecuteCommand(string CMD, int _fromClient = -1)
        {
            if(CMD.StartsWith("webhook "))
            {
                string Message = CMD.Replace("webhook ", "");
                DiscordManager.SendMessage(Message);
                return "Webhook Message: "+ Message+", sent";
            }
            if (CMD.StartsWith("webstats"))
            {
                string Message = CMD.Replace("webhook ", "");
                DiscordManager.TodayStats(MPStats.TodayStats.GetString(false, true, true));
                return "Sent";
            }
            if (CMD.StartsWith("crashsite") && !CMD.StartsWith("crashsite "))
            {
                ExpeditionManager.StartCrashSite();
                return "Random Crashsite!";
            } else if (CMD.StartsWith("crashsite "))
            {
                int Index = int.Parse(CMD.Replace("crashsite ", ""));
                ExpeditionManager.StartCrashSite(Index);
                return "Crashsite by index " + Index + "!";
            }

            if (CMD.StartsWith("whencrashsite"))
            {
                string Result = "Next Crashsite in ";
                int Seconds = ExpeditionManager.NextCrashSiteIn;
                ExpeditionManager.Expedition Crashsite = ExpeditionManager.GetActiveCrashSite();
                if (Crashsite != null)
                {
                    Seconds += Crashsite.m_TimeLeft;
                    Result += Seconds + " seconds later, or faster, if someone find current crashsite, cooldown is " + ExpeditionManager.NextCrashSiteIn;
                } else
                {
                    Result += Seconds + " seconds later, no active crashsite";
                }

                
                return Result;
            }
            if (CMD == "canclecrashsite")
            {
                ExpeditionManager.Expedition Crashsite = ExpeditionManager.GetActiveCrashSite();
                if (Crashsite != null)
                {
                    ExpeditionManager.CompleteCrashsite(-2, Crashsite.m_GUID);
                    ExpeditionManager.m_ActiveCrashSiteGUID = "";
                    return "Crashsite cancled";
                }
                return "No active crashsites";
            }
            if (CMD == "reboot")
            {
                Environment.Exit(0);
            }
            return "";
        }
    }
}
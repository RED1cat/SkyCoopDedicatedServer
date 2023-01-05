using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Diagnostics;
using GameServer;
using static SkyCoop.DataStr;
using System.Globalization;
#if (DEDICATED)
using System.Numerics;
using TinyJSON;
#else
using MelonLoader.TinyJSON;
using UnityEngine;
#endif


namespace SkyCoop
{
    public class MPSaveManager
    {
        public static bool NoSaveAndLoad = false;
        public static void Log(string LOG)
        {
#if (DEDICATED)
            Logger.Log("[MPSaveManager] " +LOG, Shared.LoggerColor.Blue);
#else
            MelonLoader.MelonLogger.Msg(ConsoleColor.Blue, "[MPSaveManager] " + LOG);
            #endif
        }
        public static void Error(string LOG)
        {
#if (DEDICATED)
            Logger.Log("[MPSaveManager] " +LOG, Shared.LoggerColor.Red);
#else
            MelonLoader.MelonLogger.Msg(ConsoleColor.Red, "[MPSaveManager] " + LOG);
            #endif
        }
        public static int GetSeed()
        {
#if (DEDICATED)
            return Seed;
#else
            return GameManager.m_SceneTransitionData.m_GameRandomSeed;
#endif

        }
        public static Dictionary<string, Dictionary<int, DataStr.DroppedGearItemDataPacket>> RecentVisual = new Dictionary<string, Dictionary<int, DataStr.DroppedGearItemDataPacket>>();
        public static Dictionary<string, Dictionary<int, DataStr.SlicedJsonDroppedGear>> RecentData = new Dictionary<string, Dictionary<int, DataStr.SlicedJsonDroppedGear>>();
        public static Dictionary<string, Dictionary<string, bool>> RecentOpenableThings = new Dictionary<string, Dictionary<string, bool>>();
        public static Dictionary<string, int> UsersSaveHashs = new Dictionary<string, int>();
        public static Dictionary<string, Dictionary<string, string>> LockedDoors = new Dictionary<string, Dictionary<string, string>>();
        public static Dictionary<string, bool> UsedKeys = new Dictionary<string, bool>();
        public static Dictionary<int, LocksmithBlank> Blanks = new Dictionary<int, LocksmithBlank>();
        public static bool SaveHashsChanged = false;
        public static bool LockedDoorsChanged = false;
        public static int SaveRecentTimer = 5;
        public static bool Diagnostic = false;
        public static int Seed = 0;


        public class KnockData
        {
            public Vector3 m_Position = new Vector3();
            public string m_Scene = "";
            public int m_ClientID = 0;
            public string m_ToScene = "";
            public int m_Timeout = 30;

        }

        public static List<KnockData> DoorEnterRequested = new List<KnockData>();

        public static void AddKnockDoorRequest(int ClientID, string ToScene)
        {
            Log("AddKnockDoorRequest ClinetID " + ClientID + " ToScene " + ToScene);
            foreach (KnockData Knock in DoorEnterRequested)
            {
                if(Knock.m_ClientID == ClientID && Knock.m_ToScene == ToScene)
                {
                    Knock.m_Timeout = 30;
                    return;
                }
            }

            KnockData AddKnock = new KnockData();
            AddKnock.m_ClientID = ClientID;
            AddKnock.m_ToScene = ToScene;

#if (!DEDICATED)
            if (ClientID != 0)
            {
                if (MyMod.playersData[ClientID] == null)
                {
                    return;
                }

                AddKnock.m_Scene = MyMod.playersData[ClientID].m_LevelGuid;
                AddKnock.m_Position = MyMod.playersData[ClientID].m_Position;
            }else{
                AddKnock.m_Scene = MyMod.level_guid;
                AddKnock.m_Position = GameManager.GetPlayerTransform().position;
            }
#else
            if (MyMod.playersData[ClientID] == null)
            {
                return;
            }

            AddKnock.m_Scene = MyMod.playersData[ClientID].m_LevelGuid;
            AddKnock.m_Position = MyMod.playersData[ClientID].m_Position;
#endif


            DoorEnterRequested.Add(AddKnock);

            Log("AddKnock.m_Scene " + AddKnock.m_Scene);
            Log("AddKnock.m_ToScene " + AddKnock.m_ToScene);

            ServerSend.KNOCKKNOCK(ToScene);

#if (!DEDICATED)
            if (MyMod.iAmHost && MyMod.level_guid == ToScene) 
            {
                HUDMessage.AddMessage("Somebody's knocking on the door");
            }
#endif
        }


        public static void UpdateKnockDoorRequests()
        {
            for (int i = DoorEnterRequested.Count-1; i > -1; i--)
            {
                KnockData Knock = DoorEnterRequested[i];
                int PID = Knock.m_ClientID;
                string Scene = MyMod.level_guid;
                Vector3 V3 = new Vector3(0,0,0);

                if(PID != 0)
                {
                    if (!Server.clients[PID].IsBusy())
                    {
                        DoorEnterRequested.RemoveAt(i);
                    }else{
                        Scene = MyMod.playersData[PID].m_LevelGuid;
                        V3 = MyMod.playersData[PID].m_Position;
                    }
                }
#if (!DEDICATED)
                else
                {
                    V3 = GameManager.GetPlayerTransform().position;
                }
#endif

                if (Knock.m_Timeout <= 0 || Knock.m_Scene != Scene || Vector3.Distance(V3, Knock.m_Position) > 15)
                {
                    DoorEnterRequested.RemoveAt(i);
                    Log("Knock Removed");
                } else
                {
                    Knock.m_Timeout--;
                }
            }
        }

        public static List<int> GetKnocksOnScene(string Scene)
        {
            List<int> Knocks = new List<int>();
            foreach (KnockData Knock in DoorEnterRequested)
            {
                if (Knock.m_ToScene == Scene)
                {
                    Knocks.Add(Knock.m_ClientID);
                }
            }
            Log("GetKnocksOnScene "+ Scene+"  Knockers "+ Knocks.Count);
            return Knocks;
        }
        public static void ApplyEnterFromKnock(int ClientID, string ToScene)
        {
            Log("ApplyEnterFromKnock ClientID " + ClientID + "  ToScene " + ToScene);
            foreach (KnockData Knock in DoorEnterRequested)
            {
                if (Knock.m_ClientID == ClientID && Knock.m_ToScene == ToScene)
                {
                    DoorEnterRequested.Remove(Knock);
#if (!DEDICATED)
                    if (ClientID != 0)
                    {
                        ServerSend.KNOCKENTER(ClientID, ToScene);
                    }else{
                        MyMod.EnterDoorsByScene(ToScene);
                    }
#else
                    ServerSend.KNOCKENTER(ClientID, ToScene);
#endif
                    return;
                }
            }
        }

        public class LocksmithBlank
        {
            public int m_State = 0;
            public string m_GearName = "";
            public string m_Scene = "";
            public string m_Dropper = "";

            public LocksmithBlank(int State, string Name, string Scene, string Dropper)
            {
                m_State = State;
                m_GearName = Name;
                m_Scene = Scene;
                m_Dropper = Dropper;
            }
        }
        public static void ChangeBlankState(int hash, int newState)
        {
            if (Blanks.ContainsKey(hash))
            {
                LocksmithBlank Blank = GetBlank(hash);

                if(Blank != null)
                {
                    Blank.m_State = newState;
                    Blanks.Remove(hash);
                    Blanks.Add(hash, Blank);
                }
            }
        }
#if (!DEDICATED)
        public static void AlignKey(GearItem key, string KeySeed, string Name)
        {
            if (key)
            {
                if (key.m_ObjectGuid == null)
                {
                    key.m_ObjectGuid = key.gameObject.AddComponent<ObjectGuid>();
                }

                if (Shared.HasNonASCIIChars(Name) || Name.Contains("_"))
                {
                    Name = "Incorrectly named Key";
                } else if (string.IsNullOrEmpty(Name))
                {
                    Name = "Nameless key";
                }

                if (Shared.HasNonASCIIChars(KeySeed) || KeySeed.Contains("_") || string.IsNullOrEmpty(KeySeed))
                {
                    KeySeed = "Broken";
                }

                key.m_ObjectGuid.m_Guid = Name + "_" + KeySeed;
                key.m_LocalizedDisplayName.m_LocalizationID = Name;
            }
        }
#endif

        public static void ApplyToolOnBlank(int hash, int tool, string KeyName = "", string KeySeed = "")
        {
            LocksmithBlank Blank = GetBlank(hash);
            if (Blank != null)
            {
                string Result = Shared.GetLockSmithProduct(Blank.m_GearName.ToLower(), tool);
                Log("[ApplyToolOnBlank] "+ hash+" "+ Blank.m_GearName+" Tool "+tool);
                Vector3 PlaceV3 = new Vector3(0,0,0);
                Quaternion Rotation = new Quaternion(0,0,0,0);
                string Scene = Blank.m_Scene;

                if(Result != Blank.m_GearName)
                {
                    Log("[ApplyToolOnBlank] Going to replace " + Blank.m_GearName + " on " + Result);
                    Dictionary<int, DataStr.SlicedJsonDroppedGear> LoadedData = LoadDropData(Scene);
                    Dictionary<int, DataStr.DroppedGearItemDataPacket> LoadedVisual = LoadDropVisual(Scene);

                    if(LoadedData == null || LoadedVisual == null)
                    {
                        Log("Wasn't able to load drop directory");
                        return;
                    }

                    DataStr.DroppedGearItemDataPacket oldVisual;
                    if(LoadedVisual.TryGetValue(hash, out oldVisual))
                    {
                        PlaceV3 = oldVisual.m_Position;
                        Rotation = oldVisual.m_Rotation;
                    }else{
                        Log("Wasn't able to find old item");
                        return;
                    }

                    DataStr.SlicedJsonDroppedGear NewGear = new DataStr.SlicedJsonDroppedGear();
                    NewGear.m_GearName = Result.ToLower();
                    NewGear.m_Extra.m_DroppedTime = MyMod.MinutesFromStartServer;
                    NewGear.m_Extra.m_Dropper = Blank.m_Dropper;
                    NewGear.m_Extra.m_GearName = NewGear.m_GearName;
                    NewGear.m_Extra.m_Variant = 4;
                    string GearJson;
                    int SearchKey;
#if (!DEDICATED)
                    GameObject reference = MyMod.GetGearItemObject(NewGear.m_GearName);
                    

                    if (reference != null)
                    {
                        GameObject obj = UnityEngine.Object.Instantiate<GameObject>(reference, PlaceV3, Rotation);
                        GearItem gi = obj.GetComponent<GearItem>();
                        if(!string.IsNullOrEmpty(KeyName) && !string.IsNullOrEmpty(KeySeed))
                        {
                            AlignKey(gi, KeySeed, KeyName);
                        }
                        gi.SkipSpawnChanceRollInitialDecayAndAutoEvolve();
                        obj.name = Blank.m_GearName;

                        GearJson = obj.GetComponent<GearItem>().Serialize();

                        int hashV3 = Shared.GetVectorHash(PlaceV3);
                        int hashRot = Shared.GetQuaternionHash(Rotation);
                        int hashLevelKey = Scene.GetHashCode();
                        SearchKey = hashV3 + hashRot + hashLevelKey;
                        UnityEngine.Object.Destroy(obj);
                    }else{
                        Log("Can't load reference for blank");
                        ChangeBlankState(hash, 0);
                        return;
                    }
#else
                    if (!string.IsNullOrEmpty(KeyName) && !string.IsNullOrEmpty(KeySeed))
                    {
                        GearJson = ResourceIndependent.GetLocksmithGear(NewGear.m_GearName, PlaceV3, Rotation, KeyName, KeySeed);
                    } else
                    {
                        GearJson = ResourceIndependent.GetLocksmithGear(NewGear.m_GearName, PlaceV3, Rotation);
                    }

                    if (string.IsNullOrEmpty(GearJson))
                    {
                        Log("Can't load reference for blank");
                        ChangeBlankState(hash, 0);
                        return;
                    }

                    int hashV3 = Shared.GetVectorHash(PlaceV3);
                    int hashRot = Shared.GetQuaternionHash(Rotation);
                    int hashLevelKey = Scene.GetHashCode();
                    SearchKey = hashV3 + hashRot + hashLevelKey;

#endif

                    DataStr.DroppedGearItemDataPacket GearVisual = new DataStr.DroppedGearItemDataPacket();
                    GearVisual.m_Extra = NewGear.m_Extra;
                    GearVisual.m_GearID = -1;
                    GearVisual.m_Hash = SearchKey;
                    GearVisual.m_LevelGUID = Scene;
                    GearVisual.m_Position = PlaceV3;
                    GearVisual.m_Rotation = Rotation;
                    NewGear.m_Json = GearJson;
                    RemovSpecificGear(hash, Scene); // Remove Data and visual on host
                    ServerSend.PICKDROPPEDGEAR(0, hash, true); // Remove visual data on client
#if (!DEDICATED)
                    if (!MyMod.DedicatedServerAppMode)
                    {
                        GameObject gearObj;
                        MyMod.DroppedGearsObjs.TryGetValue(hash, out gearObj);
                        if (gearObj != null)
                        {
                            MyMod.DroppedGearsObjs.Remove(hash);
                            MyMod.TrackableDroppedGearsObjs.Remove(hash);
                            UnityEngine.Object.Destroy(gearObj); // Remove visual object on host
                        }
                    }
#endif
                    Log("Removed " + hash);

                    AddGearData(Scene, SearchKey, NewGear);
                    AddGearVisual(Scene, GearVisual);
                    AddBlank(SearchKey, Result, Scene, Blank.m_Dropper);
                    Shared.FakeDropItem(GearVisual, true);
                    ServerSend.DROPITEM(0, GearVisual, true);
                    Log("Added " + SearchKey);
                }else{
                    ChangeBlankState(hash, 0);
                }
            }
        }

        public static void AddBlank(int hash, string name, string scene, string Dropper)
        {
            if (!Blanks.ContainsKey(hash))
            {
                Blanks.Add(hash, new LocksmithBlank(0, name, scene, Dropper));
            }
        }

        public static LocksmithBlank GetBlank(int hash)
        {
            if (Blanks.ContainsKey(hash))
            {
                LocksmithBlank B;
                if (Blanks.TryGetValue(hash, out B))
                {
                    return B;
                }
            }
            return null;
        }

        public static bool CanWorkOnBlank(int hash)
        {
            LocksmithBlank Blank = GetBlank(hash);
            if (Blank != null)
            {
                if(Blank.m_State != -1)
                {
                    return true;
                }
            }
            return false;
        }

        public static void LoadNonUnloadables()
        {
            int SaveSeed = GetSeed();
            Log("LoadNonUnloadables Seed "+ SaveSeed);
            string LockedDoorsJSON = LoadData("LockedDoors", SaveSeed);
            string UsersSavesHashJSON = LoadData("UsersSaveHashes", SaveSeed);
            if (!string.IsNullOrEmpty(LockedDoorsJSON))
            {
                LockedDoors = JSON.Load(LockedDoorsJSON).Make<Dictionary<string, Dictionary<string, string>>>();
                foreach (var Dict in LockedDoors)
                {
                    foreach (var item in Dict.Value)
                    {
                        UsedKeys.Add(item.Value, true);
                    }
                }
            }
            if (!string.IsNullOrEmpty(UsersSavesHashJSON))
            {
                UsersSaveHashs = JSON.Load(UsersSavesHashJSON).Make<Dictionary<string, int>>();
            }
        }

        public static string GenerateSeededGUID(int gameSeed, Vector3 v3)
        {
#if (!DEDICATED)
            int _x = (int)v3.x;
            int _y = (int)v3.y;
            int _z = (int)v3.z;
#else
            int _x = (int)v3.X;
            int _y = (int)v3.Y;
            int _z = (int)v3.Z;
#endif
            int v3Int = _x + _y + _z;
            int newSeed = gameSeed + v3Int;
            string _chars = "abcdefghijklmnopqrstuvwxyz1234567890ABCDEFGHIJKLMNOPQRSTUVWXYZ";
            System.Random newRNG = new System.Random(newSeed);
            string newGUID = "";
            for (int i = 1; i < 36; i++)
            {
                if (i == 9 || i == 14 || i == 19 || i == 24)
                {
                    newGUID = newGUID + "-";
                }
                int charIndex = newRNG.Next(0, _chars.Length);
                newGUID = newGUID + _chars[charIndex];
            }
            return newGUID;
        }

        public static string GetNewUGUID()
        {
            System.Random RNG = new System.Random();
            int GUIDseed = RNG.Next(1, 999999);
            int GUIDx = RNG.Next(-7000, 999999);
            int GUIDy = RNG.Next(-3000, 999999);
            int GUIDz = RNG.Next(-5370, 999999);

            return GenerateSeededGUID(GUIDseed, new Vector3(GUIDx, GUIDy, GUIDz));
        }

        public static void SetSaveHash(string UGUID, int hash)
        {
            if (UsersSaveHashs.ContainsKey(UGUID))
            {
                UsersSaveHashs.Remove(UGUID);
            }

            UsersSaveHashs.Add(UGUID, hash);
            SaveHashsChanged = true;
        }

        public static bool VerifySaveHash(string UGUID, int hash)
        {
            if (UsersSaveHashs.ContainsKey(UGUID))
            {
                int LastHash;
                if(UsersSaveHashs.TryGetValue(UGUID, out LastHash))
                {
                    Log("GUID " + UGUID+" Provided  hash: "+ hash+" expected "+ LastHash);
                    if(LastHash == hash)
                    {
                        Log("Verified GUID " + UGUID);
                        return true;
                    }else{
                        Log("Incorrect hash");
                        UsersSaveHashs.Remove(UGUID);
                        return false;
                    }
                }
            }else{
                Log("There no saves for GUID "+ UGUID);
                return false;
            }
            Log("No");
            return false;
        }

        public static Dictionary<string, string> GetDoorsOnScene(string Scene)
        {
            Dictionary<string, string> Dict;
            if (!LockedDoors.ContainsKey(Scene))
            {
                Dict = new Dictionary<string, string>();
                LockedDoors.Add(Scene, Dict);
                LockedDoorsChanged = true;
            }else{
                LockedDoors.TryGetValue(Scene, out Dict);
            }
            return Dict;
        }

        public static bool TryUseKey(string Scene, string DoorKey, string KeySeed)
        {
            Dictionary<string, string> Dict = GetDoorsOnScene(Scene);
            if (Dict.ContainsKey(DoorKey))
            {
                string Seed = "";
                Dict.TryGetValue(DoorKey, out Seed);
                if (Seed == KeySeed)
                {
                    return true;
                }else{
                    return false;
                }
            }else{
                return true;
            }
        }

        public enum UseKeyStatus
        {
            KeyUsed,
            DoorAlreadyLocked,
            Done,
        }

        public static UseKeyStatus AddLockedDoor(string Scene, string DoorKey, string KeySeed)
        {
            Dictionary<string, string> Dict = GetDoorsOnScene(Scene);

            if (UsedKeys.ContainsKey(KeySeed))
            {
                return UseKeyStatus.KeyUsed;
            }

            if (!Dict.ContainsKey(DoorKey))
            {
                Dict.Add(DoorKey, KeySeed);
                UsedKeys.Add(KeySeed, true);
                LockedDoorsChanged = true;
                return UseKeyStatus.Done;
            }else{
                return UseKeyStatus.DoorAlreadyLocked;
            }
        }
        public static void RemoveLockedDoor(string Scene, string DoorKey)
        {
            Dictionary<string, string> Dict = GetDoorsOnScene(Scene);
            if (Dict.ContainsKey(DoorKey))
            {
                string KeySeed;
                if(Dict.TryGetValue(DoorKey, out KeySeed))
                {
                    UsedKeys.Remove(KeySeed);
                }
                
                Dict.Remove(DoorKey);
                LockedDoorsChanged = true;
            }
        }
#if (!DEDICATED)
        public static void TryLockPick(string Scene, string DoorKey, int Picker)
        {
            System.Random RNG = new System.Random();
            bool Swear = true;
            if (RNG.Next(0, 100) <= 47)
            {
                Swear = false;
                RemoveLockedDoor(Scene, DoorKey);
                string GUID = DoorKey.Split('_')[1];
                ServerSend.REMOVEDOORLOCK(-1, GUID, Scene);

                if (!MyMod.DedicatedServerAppMode)
                {
                    if (MyMod.level_guid == Scene)
                    {
                        MyMod.RemoveLocksFromDoorsByGUID(GUID);
                    }
                }
            }
            if (Picker == 0)
            {
                MyMod.SwearOnLockpickingDone = Swear;
            } else
            {
                ServerSend.LOCKPICK(Picker, Swear);
            }
        }
#else
        public static void TryLockPick(string Scene, string DoorKey, int Picker)
        {
            System.Random RNG = new System.Random();
            bool Swear = true;
            if (RNG.Next(0, 100) <= 47)
            {
                Swear = false;
                RemoveLockedDoor(Scene, DoorKey);
                string GUID = DoorKey.Split('_')[1];
                ServerSend.REMOVEDOORLOCK(-1, GUID, Scene);
            }
            ServerSend.LOCKPICK(Picker, Swear);
        }
#endif

        public static void SaveRecentStuff()
        {
            Stopwatch watch = null;
            if (Diagnostic)
            {
                watch = Stopwatch.StartNew();
            }

            int SaveSeed = GetSeed();
            ValidateRootExits();

            if (SaveHashsChanged)
            {
                SaveHashsChanged = false;
                SaveData("UsersSaveHashes", JSON.Dump(UsersSaveHashs), SaveSeed);
            }
            if (LockedDoorsChanged)
            {
                LockedDoorsChanged = false;
                SaveData("LockedDoors", JSON.Dump(LockedDoors), SaveSeed);
            }

            foreach (var item in RecentVisual)
            {
                SaveData(GetKeyTemplate(SaveKeyTemplateType.DropsVisual, item.Key), JSON.Dump(item.Value), SaveSeed);
            }
            foreach (var item in RecentData)
            {
                SaveData(GetKeyTemplate(SaveKeyTemplateType.DropsData, item.Key), JSON.Dump(item.Value), SaveSeed);
            }
            foreach (var item in RecentOpenableThings)
            {
                SaveData(GetKeyTemplate(SaveKeyTemplateType.Openables, item.Key), JSON.Dump(item.Value), SaveSeed);
            }
            RecentVisual = new Dictionary<string, Dictionary<int, DataStr.DroppedGearItemDataPacket>>();
            RecentData = new Dictionary<string, Dictionary<int, DataStr.SlicedJsonDroppedGear>>();
            RecentOpenableThings = new Dictionary<string, Dictionary<string, bool>>();

            if (watch != null)
            {
                watch.Stop();
                Log("SaveRecentStuff() Took "+ watch.ElapsedMilliseconds+"ms");
            }
        }

        public static string LoadData(string name, int Seed = 0, bool Compressed = false)
        {
            if (NoSaveAndLoad)
            {
                return "";
            }
            Log("Attempt to load " + name);
            Stopwatch watch = null;
            if (Diagnostic)
            {
                watch = Stopwatch.StartNew();
            }
            string Result = "";
            string fullPath = GetPathForName(name, Seed);
            if (!File.Exists(fullPath))
            {
                if (watch != null)
                {
                    watch.Stop();
                    Log("LoadData() Took " + watch.ElapsedMilliseconds + "ms");
                }
                Log("File " + fullPath+" not exist");
            }else{
                byte[] FileData = File.ReadAllBytes(fullPath);
                Result = UTF8Encoding.UTF8.GetString(FileData);
                if(watch != null)
                {
                    watch.Stop();
                    Log("LoadData() Took " + watch.ElapsedMilliseconds + "ms");
                }
                Log("Loaded with no problems");
            }

            if (!string.IsNullOrEmpty(Result))
            {
                Result = UpgradeOldJsonFile(Result);
            }

            return Result;
        }

#if (DEDICATED)
        
        public static string AppPath = "";
        public static string PathSeparator = @"\";
        public static string GetPathForName(string name, int Seed = 0)
        {
            if (NoSaveAndLoad)
            {
                return "";
            }
            if (string.IsNullOrEmpty(AppPath))
            {
                AppPath = AppDomain.CurrentDomain.BaseDirectory;
            }

            if (Seed != 0)
            {
                return AppPath + Seed + PathSeparator + name;
            }

            return AppPath + name;
        }
#else
        public static string GetPathForName(string name, int Seed = 0)
        {
            if (NoSaveAndLoad)
            {
                return "";
            }
            if (string.IsNullOrEmpty(PersistentDataPath.m_Path))
            {
                PersistentDataPath.Init();
            }

            if (Seed != 0)
            {
                return PersistentDataPath.m_Path + PersistentDataPath.m_PathSeparator + Seed + PersistentDataPath.m_PathSeparator + name;
            }

            return PersistentDataPath.m_Path + PersistentDataPath.m_PathSeparator + name;
        }
#endif

        public static bool SaveData(string name, string content, int Seed = 0, string CustomPath = "")
        {
            if (NoSaveAndLoad)
            {
                return false;
            }
            Stopwatch watch = null;
            if (Diagnostic)
            {
                watch = Stopwatch.StartNew();
            }
            Log("Attempt to save " + name);
            string pathAndFilename = GetPathForName(name, Seed);

            if (!string.IsNullOrEmpty(CustomPath))
            {
                pathAndFilename = CustomPath;
            }
            string tempFile = pathAndFilename + "_temp";
            if (File.Exists(tempFile))
                File.Delete(tempFile);
            Stream stream = new FileStream(tempFile, FileMode.OpenOrCreate, FileAccess.Write, FileShare.None);
            if (stream == null)
            {
                Error("Save failed, writing stream wasn't created");
                return false;
            }
            byte[] data = new UTF8Encoding(true).GetBytes(content);
            stream.Write(data, 0, data.Length);
            stream.Dispose();
            if (File.Exists(pathAndFilename))
                File.Delete(pathAndFilename);
            File.Copy(tempFile, pathAndFilename, true);
            if (File.Exists(tempFile))
                File.Delete(tempFile);
            if (watch != null)
            {
                watch.Stop();
                Log("SaveData() Took " + watch.ElapsedMilliseconds + "ms");
            }
            Log("Everything alright! File saved!");


            return true;
        }
        public static void DeleteData(string name, int Seed = 0)
        {
            Log("Attempt to delete " + name);
            try
            {
                string fullPath = GetPathForName(name, Seed);
                if (File.Exists(fullPath))
                    File.Delete(fullPath);
                Log("File deleted!");
            }
            catch (Exception ex)
            {
                Error(ex.ToString());
            }
        }

        public enum SaveKeyTemplateType
        {
            Container = 0,
            DropsVisual = 1,
            DropsData = 2,
            Openables = 3,
        }

        public static string GetKeyTemplate(SaveKeyTemplateType T, string Scene, string GUID = "")
        {            
            switch (T)
            {
                case SaveKeyTemplateType.Container:
                    return Scene + "_"+ GUID;
                case SaveKeyTemplateType.DropsVisual:
                    return Scene+ "_DropVisual";
                case SaveKeyTemplateType.Openables:
                    return Scene + "_Open";
                case SaveKeyTemplateType.DropsData:
                    return Scene + "_DropsData";
                default:
                    return "_UNKNOWN";
            }
        }
        public static bool IsFileExist(string name, int Seed = 0)
        {
            if (NoSaveAndLoad)
            {
                return false;
            }
            bool exists = File.Exists(GetPathForName(name, Seed));
            return exists;
        }

        public static void CreateFolderIfNotExist(string path)
        {
            if (NoSaveAndLoad)
            {
                return;
            }
            bool exists = Directory.Exists(path);
            if (!exists)
            {
                Directory.CreateDirectory(path);
            }
        }

        public static void ValidateRootExits()
        {
            if (NoSaveAndLoad)
            {
                return;
            }
            int SaveSeed = GetSeed();
            CreateFolderIfNotExist(GetPathForName(SaveSeed + ""));
        }

        public static void SaveContainer(string scene, string GUID, string Content)
        {
            int SaveSeed = GetSeed();
            string Key = GetKeyTemplate(SaveKeyTemplateType.Container, scene, GUID);
            ValidateRootExits();
            SaveData(Key, Content, SaveSeed);
        }
        public static string LoadContainer(string scene, string GUID)
        {
            int SaveSeed = GetSeed();
            string Key = GetKeyTemplate(SaveKeyTemplateType.Container, scene, GUID);
            ValidateRootExits();
            return LoadData(Key, SaveSeed, true);
        }
        public static void RemoveContainer(string scene, string GUID)
        {
            Log("Got request to remove "+ GUID);
            int SaveSeed = GetSeed();
            string Key = GetKeyTemplate(SaveKeyTemplateType.Container, scene, GUID);
            DeleteData(Key, SaveSeed);
        }
        public static Dictionary<string, bool> LoadOpenableThings(string scene)
        {
            int SaveSeed = GetSeed();
            string Key = GetKeyTemplate(SaveKeyTemplateType.Openables, scene);

            Dictionary<string, bool> Dict;
            if (RecentOpenableThings.TryGetValue(scene, out Dict))
            {
                return Dict;
            }

            string LoadedContent = LoadData(Key, SaveSeed);
            if (LoadedContent != "")
            {
                return JSON.Load(LoadedContent).Make< Dictionary<string, bool>>();
            }
            return null;
        }
        public static void ChangeOpenableThingState(string scene, string GUID, bool state)
        {
            int SaveSeed = GetSeed();
            string Key = GetKeyTemplate(SaveKeyTemplateType.Openables, scene);
            Dictionary<string, bool> Dict;
            if (RecentOpenableThings.TryGetValue(scene, out Dict))
            {
                if (Dict.ContainsKey(GUID))
                {
                    Dict.Remove(GUID);
                }
                Dict.Add(GUID, state);
                RecentOpenableThings.Remove(scene);
                RecentOpenableThings.Add(scene, Dict);
                return;
            }else{
                Dict = LoadOpenableThings(scene);
            }

            if (Dict == null)
            {
                Dict = new Dictionary<string, bool>();
            }
            Dict.Remove(GUID);
            Dict.Add(GUID, state);
            RecentOpenableThings.Add(scene, Dict);
            ValidateRootExits();
            SaveData(Key, JSON.Dump(Dict), SaveSeed);
        }


        public static Dictionary<int, DataStr.DroppedGearItemDataPacket> LoadDropVisual(string scene)
        {
            int SaveSeed = GetSeed();
            string Key = GetKeyTemplate(SaveKeyTemplateType.DropsVisual, scene);

            Dictionary<int, DataStr.DroppedGearItemDataPacket> Dict;
            if(RecentVisual.TryGetValue(scene, out Dict))
            {
                return Dict;
            }

            string LoadedContent = LoadData(Key, SaveSeed);
            if (LoadedContent != "")
            {
                return JSON.Load(LoadedContent).Make<Dictionary<int, DataStr.DroppedGearItemDataPacket>>();
            }
            return null;
        }
        public static Dictionary<int, DataStr.SlicedJsonDroppedGear> LoadDropData(string scene)
        {
            int SaveSeed = GetSeed();
            string Key = GetKeyTemplate(SaveKeyTemplateType.DropsData, scene);

            Dictionary<int, DataStr.SlicedJsonDroppedGear> Dict;
            if(RecentData.TryGetValue(scene, out Dict))
            {
                return Dict;
            }

            string LoadedContent = LoadData(Key, SaveSeed);
            if (LoadedContent != "")
            {
                return JSON.Load(LoadedContent).Make<Dictionary<int, DataStr.SlicedJsonDroppedGear>>();
            }
            return null;
        }
        public static void AddGearVisual(string scene, DataStr.DroppedGearItemDataPacket gear)
        {
            int SaveSeed = GetSeed();
            string Key = GetKeyTemplate(SaveKeyTemplateType.DropsVisual, scene);
            Dictionary<int, DataStr.DroppedGearItemDataPacket> Dict;
            if (RecentVisual.TryGetValue(scene, out Dict))
            {
                Dict.Remove(gear.m_Hash);
                Dict.Add(gear.m_Hash, gear);
                RecentVisual.Remove(scene);
                RecentVisual.Add(scene, Dict);
                return;
            }else{
                Dict = LoadDropVisual(scene);
            }

            if (Dict == null)
            {
                Dict = new Dictionary<int, DataStr.DroppedGearItemDataPacket>();
            }
            Dict.Remove(gear.m_Hash);
            Dict.Add(gear.m_Hash, gear);
            ValidateRootExits();
            SaveData(Key, JSON.Dump(Dict), SaveSeed);
            RecentVisual.Add(scene, Dict);
        }
        public static void AddGearData(string scene, int hash, DataStr.SlicedJsonDroppedGear GearData)
        {
            int SaveSeed = GetSeed();
            string Key = GetKeyTemplate(SaveKeyTemplateType.DropsData, scene);
            Dictionary<int, DataStr.SlicedJsonDroppedGear> Dict;

            if (RecentData.TryGetValue(scene, out Dict))
            {
                Dict.Remove(hash);
                Dict.Add(hash, GearData);
                RecentData.Remove(scene);
                RecentData.Add(scene, Dict);
                return;
            }else{
                Dict = LoadDropData(scene);
            }

            if (Dict == null)
            {
                Dict = new Dictionary<int, DataStr.SlicedJsonDroppedGear>();
            }
            Dict.Remove(hash);
            Dict.Add(hash, GearData);
            ValidateRootExits();
            SaveData(Key, JSON.Dump(Dict), SaveSeed);
            RecentData.Add(scene, Dict);
        }

        public static void RemovSpecificGear(int Hash, string Scene)
        {
            Log("[RemovSpecificGear] Trying to remove " + Hash);
            Dictionary<int, DataStr.SlicedJsonDroppedGear> Dict;
            if (!RecentData.TryGetValue(Scene, out Dict))
            {
                Dict = LoadDropData(Scene);
            }

            if(Dict != null)
            {
                Dict.Remove(Hash);
                RecentData.Remove(Scene);
                RecentData.Add(Scene, Dict);
            }

            Dictionary<int, DataStr.DroppedGearItemDataPacket> Dict2;
            if (!RecentVisual.TryGetValue(Scene, out Dict2))
            {
                Dict2 = LoadDropVisual(Scene);
            }

            if(Dict2 != null)
            {
                Dict2.Remove(Hash);
                RecentVisual.Remove(Scene);
                RecentVisual.Add(Scene, Dict2);
            }

            if (Blanks.ContainsKey(Hash))
            {
                Blanks.Remove(Hash);
            }
        }

        public static DataStr.SlicedJsonDroppedGear RequestSpecificGear(int Hash, string Scene, bool Remove = true)
        {
            Dictionary<int, DataStr.SlicedJsonDroppedGear> Dict = LoadDropData(Scene);
            DataStr.SlicedJsonDroppedGear Gear = null;
            if (Dict != null)
            {
                if(Dict.TryGetValue(Hash, out Gear))
                {
                    if (Remove)
                    {
                        RemovSpecificGear(Hash, Scene);
                    }
                }
            }
            return Gear;
        }

        public static bool SaveServerCFG(DataStr.ServerSettingsData CFG)
        {
            return SaveData("ServerSettingsData", JSON.Dump(CFG));
        }
        public static DataStr.ServerSettingsData RequestServerCFG()
        {
            string Data = LoadData("ServerSettingsData");
            if (Data != "")
            {
                if (Data.Contains("SkyCoop.MyMod+ServerSettingsData")) // 10.4 ServerSettings file.
                {
                    return null;
                }
                return JSON.Load(Data).Make<DataStr.ServerSettingsData>();
            }else{
                return null;
            }
        }
        public static void SaveMyName(string Name)
        {
            SaveData("MultiplayerNickName", Name);
        }
        public static string LoadMyName()
        {
            string Name = LoadData("MultiplayerNickName");
            return Name;
        }
        public static void SaveGlobalData()
        {
            Log("Dedicated server saving...");
            Dictionary<string, string> GlobalData = new Dictionary<string, string>();
            GlobalData.Add("furns", JSON.Dump(MyMod.BrokenFurniture));
            GlobalData.Add("pickedgears", JSON.Dump(MyMod.PickedGears));
            GlobalData.Add("ropes", JSON.Dump(MyMod.DeployedRopes));
            GlobalData.Add("containers", JSON.Dump(MyMod.LootedContainers));
            GlobalData.Add("plants", JSON.Dump(MyMod.HarvestedPlants));
            GlobalData.Add("shelters", JSON.Dump(MyMod.ShowSheltersBuilded));
            int[] saveProxy = { MyMod.MinutesFromStartServer };
            GlobalData.Add("rtt", JSON.Dump(saveProxy));
            GlobalData.Add("killedanimals", JSON.Dump(Shared.AnimalsKilled));
            GlobalData.Add("deathcreates", JSON.Dump(MyMod.DeathCreates));
            string[] saveProxy2 = { MyMod.OveridedTime };
            GlobalData.Add("gametime", JSON.Dump(saveProxy2));
            string Jonny = JSON.Dump(GlobalData);
            SaveData("GlobalServerData", Jonny, GetSeed());
            Log("Save is done! Next save "+MyMod.DsSavePerioud+" seconds later");
        }
        public static string GetDictionaryString(Dictionary<string, string> Dict, string Key)
        {
            string Val;
            if(Dict.TryGetValue(Key, out Val))
            {
                return Val;
            }
            return "";
        }
        public static void LoadGlobalData()
        {
            string Data = LoadData("GlobalServerData", GetSeed());
            Dictionary<string, string> GlobalData = new Dictionary<string, string>();
            if (Data != "")
            {
                GlobalData = JSON.Load(Data).Make<Dictionary<string, string>>();
            } else
            {
                return;
            }
            MyMod.BrokenFurniture = JSON.Load(GetDictionaryString(GlobalData, "furns")).Make<List<BrokenFurnitureSync>>();
            MyMod.PickedGears = JSON.Load(GetDictionaryString(GlobalData, "pickedgears")).Make<List<PickedGearSync>>();
            MyMod.DeployedRopes = JSON.Load(GetDictionaryString(GlobalData, "ropes")).Make<List<ClimbingRopeSync>>();
            MyMod.LootedContainers = JSON.Load(GetDictionaryString(GlobalData, "containers")).Make<List<ContainerOpenSync>>();
            MyMod.HarvestedPlants = JSON.Load(GetDictionaryString(GlobalData, "plants")).Make<List<string>>();
            MyMod.ShowSheltersBuilded = JSON.Load(GetDictionaryString(GlobalData, "shelters")).Make<List<ShowShelterByOther>>();
            int[] saveProxy = JSON.Load(GetDictionaryString(GlobalData, "rtt")).Make<int[]>();
            MyMod.MinutesFromStartServer = saveProxy[0];
            Shared.AnimalsKilled = JSON.Load(GetDictionaryString(GlobalData, "killedanimals")).Make<Dictionary<string, AnimalKilled>>();
            MyMod.DeathCreates = JSON.Load(GetDictionaryString(GlobalData, "deathcreates")).Make<List<DeathContainerData>>();
            string[] saveProxy2 = JSON.Load(GetDictionaryString(GlobalData, "gametime")).Make<string[]>();
            MyMod.OveridedTime = saveProxy2[0];
        }
        public static void SaveJsonSnapshot(string Alias, string JSON)
        {
            CreateFolderIfNotExist(GetPathForName("Snapshots"));
            CreateFolderIfNotExist(GetPathForName(@"Snapshots\"+ Alias));

            DateTime DT = System.DateTime.Now;
            string FileName = DT.Hour.ToString() + "_" + DT.Minute.ToString() + "_" + DT.Second.ToString() + "_" + DT.Millisecond.ToString();
            SaveData(FileName, JSON, 0, GetPathForName(@"Snapshots\" + Alias+@"\"+FileName));
        }
        public static string UpgradeOldJsonFile(string Json, bool Decompress = false)
        {
            if (Decompress)
            {
                Json = Shared.DecompressString(Json);
            }
            Json = Json.Replace("SkyCoop.MyMod", "SkyCoop.DataStr");
            if (Decompress)
            {
                Json = Shared.CompressString(Json);
            }

            return Json;
        }
    }
}
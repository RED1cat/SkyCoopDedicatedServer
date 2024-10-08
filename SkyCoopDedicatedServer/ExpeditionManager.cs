﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Threading.Tasks;
using GameServer;
using System.Security.Policy;
using static SkyCoop.DataStr;
using static SkyCoop.ExpeditionBuilder;
using System.Net.Sockets;
using static SkyCoop.ExpeditionManager;
#if (DEDICATED)
using System.Numerics;
using TinyJSON;
#else
using MelonLoader.TinyJSON;
using MelonLoader;
using UnityEngine;
#endif
namespace SkyCoop
{
    public class ExpeditionManager
    {
        public static List<Expedition> m_ActiveExpeditions = new List<Expedition>();
        public static string m_ActiveCrashSiteGUID = "";
        public static Dictionary<string, int> m_UnavailableGearSpawners = new Dictionary<string, int>();
        public static Dictionary<int, string> m_GearSpawnerGears = new Dictionary<int, string>();
        public static List<ExpeditionInvite> m_Invites = new List<ExpeditionInvite>();
        public static int NextCrashSiteIn = 3600 *2;
        public static bool Debug = true;
        public static List<SpecialExpeditionItem> m_SpecialItems = new List<SpecialExpeditionItem>();
        public static Dictionary<string, List<string>> m_SpecialItemsOwners = new Dictionary<string, List<string>>();

        public static SpecialExpeditionItem GetSpecialItem(string ReferenceName)
        {
            for (int i = 0; i < m_SpecialItems.Count; i++)
            {
                SpecialExpeditionItem Item = m_SpecialItems[i];
                if (Item.m_GearReferenceName == ReferenceName)
                {
                    return Item;
                }
            }
            return null;
        }

        public static void AddSpecialItem(string JSONString)
        {
            JSONString = MPSaveManager.VectorsFixUp(JSONString);
            AddSpecialItem(JSON.Load(JSONString).Make<SpecialExpeditionItem>());
        }
        public static void AddSpecialItem(SpecialExpeditionItem NewItem)
        {
            if (GetSpecialItem(NewItem.m_GearReferenceName) == null)
            {
                m_SpecialItems.Add(NewItem);
            }
            Log("Registered Special Expedition Item "+ NewItem.m_GearReferenceName);
        }

        public static void InitClues()
        {
            m_SpecialItems.Clear();


            MPSaveManager.CreateFolderIfNotExist(MPSaveManager.GetBaseDirectory() + "Mods");
            MPSaveManager.CreateFolderIfNotExist(MPSaveManager.GetBaseDirectory() + "Mods" + MPSaveManager.GetSeparator() + "ExpeditionTemplates");
            MPSaveManager.CreateFolderIfNotExist(MPSaveManager.GetBaseDirectory() + "Mods" + MPSaveManager.GetSeparator() + "ExpeditionTemplates" + MPSaveManager.GetSeparator() + "SpecialGears");

            DirectoryInfo d = new DirectoryInfo(MPSaveManager.GetBaseDirectory() + "Mods" + MPSaveManager.GetSeparator() + "ExpeditionTemplates" + MPSaveManager.GetSeparator() + "SpecialGears");
            FileInfo[] Files = d.GetFiles("*.json");
            foreach (FileInfo file in Files)
            {
                byte[] FileData = File.ReadAllBytes(file.FullName);

                string JSONString = UTF8Encoding.UTF8.GetString(FileData);
                if (string.IsNullOrEmpty(JSONString))
                {
                    continue;
                }
                AddSpecialItem(JSONString);
            }
        }
        public static bool IsClueGear(string GearName)
        {
            string SearchName = GearName.ToLower();
            foreach (SpecialExpeditionItem SpecailItem in m_SpecialItems)
            {
                if(SearchName == SpecailItem.m_GearReferenceName.ToLower())
                {
                    return true;
                }
            }
            return false;
        }

        public class SaveData
        {
            public string m_ActiveExpeditions = "";
            public string m_ActiveCrashSiteGUID = "";
            public string m_UnavailableGearSpawners = "";
            public string m_GearSpawnerGears = "";
            public int NextCrashSiteIn = 3600 * 2;
            public Dictionary<string, List<string>> m_SpecialItemsOwners = new Dictionary<string, List<string>>();
        }

        public static string Save()
        {
            SaveData Data = new SaveData();
            Data.m_ActiveExpeditions = JSON.Dump(m_ActiveExpeditions);

            if(!string.IsNullOrEmpty(m_ActiveCrashSiteGUID))
            {
                Data.m_ActiveCrashSiteGUID = m_ActiveCrashSiteGUID;
            }
            Data.m_UnavailableGearSpawners = JSON.Dump(m_UnavailableGearSpawners);
            Data.m_GearSpawnerGears = JSON.Dump(m_GearSpawnerGears);
            Data.NextCrashSiteIn = NextCrashSiteIn;
            Data.m_SpecialItemsOwners = m_SpecialItemsOwners;
            return JSON.Dump(Data);
        }

        public static void Load(string JSONString)
        {
            ApplyWorldEdits();
            if (string.IsNullOrEmpty(JSONString))
            {
                return;
            }
            DebugLog("[ExpeditionManager] Loading SaveData...");

            JSONString = MPSaveManager.VectorsFixUp(JSONString);

            SaveData Data = JSON.Load(JSONString).Make<SaveData>();
            if (Data != null && !string.IsNullOrEmpty(Data.m_ActiveExpeditions))
            {
                m_ActiveExpeditions = JSON.Load(Data.m_ActiveExpeditions).Make<List<Expedition>>();
                foreach (Expedition Exp in m_ActiveExpeditions)
                {
                    DebugLog("[ExpeditionManager] " + Exp.m_Alias);
                    DebugLog("[ExpeditionManager] GUID " + Exp.m_GUID);
                    DebugLog("[ExpeditionManager] m_TimeLeft " + Exp.m_TimeLeft);
                    DebugLog("[ExpeditionManager] m_Players: ");
                    foreach (string MAC in Exp.m_Players)
                    {
                        DebugLog("[ExpeditionManager] Player "+ MAC);
                    }
                    DebugLog("[ExpeditionManager] m_Tasks:");
                    foreach (ExpeditionTask Task in Exp.m_Tasks)
                    {
                        DebugLog("[ExpeditionManager] Task " + Task.m_Alias);
                        DebugLog("[ExpeditionManager] m_ExpeditionGUID " + Task.m_ExpeditionGUID);
                        DebugLog("[ExpeditionManager] m_IsComplete " + Task.m_IsComplete);
                    }
                }
            }
            if (!string.IsNullOrEmpty(Data.m_ActiveCrashSiteGUID))
            {
                m_ActiveCrashSiteGUID = Data.m_ActiveCrashSiteGUID;
                foreach (Expedition Exp in m_ActiveExpeditions)
                {
                    if(Exp.m_GUID == m_ActiveCrashSiteGUID)
                    {
                        DebugLog("[ExpeditionManager] m_ActiveCrashSite " + Exp.m_Alias);
                    }
                }
            }
            if (!string.IsNullOrEmpty(Data.m_UnavailableGearSpawners))
            {
                m_UnavailableGearSpawners = JSON.Load(Data.m_UnavailableGearSpawners).Make<Dictionary<string, int>>();
            }
            if (!string.IsNullOrEmpty(Data.m_GearSpawnerGears))
            {
                m_GearSpawnerGears = JSON.Load(Data.m_GearSpawnerGears).Make<Dictionary<int, string>>();
            }
            if (Data.m_SpecialItemsOwners != null)
            {
                m_SpecialItemsOwners = Data.m_SpecialItemsOwners;
            }

            NextCrashSiteIn = Data.NextCrashSiteIn;
        }

        public static void Log(string LOG)
        {
#if (DEDICATED)
            Logger.Log("[MPSaveManager] " +LOG, Shared.LoggerColor.Blue);
#else
            MelonLoader.MelonLogger.Msg(ConsoleColor.Blue, "[ExpeditionManager] " + LOG);
#endif
        }
        public static void DebugLog(string LOG)
        {
            if (!Debug)
            {
                return;
            }
#if (DEDICATED)
            Logger.Log("[MPSaveManager] " +LOG, Shared.LoggerColor.Blue);
#else
            MelonLoader.MelonLogger.Msg(ConsoleColor.Blue, "[ExpeditionManager] " + LOG);
#endif
        }

        public enum ExpeditionTaskType
        {
            ENTERSCENE, // Visit scene.
            ENTERZONE, // Visit zone on scene.
            COLLECT, // Collect gear on scene that spawns once you in zone.
            FLAREGUNSHOT, // Shoot with flaregun being in zone of this scene.
            CHARCOAL, // Draw with coal on scene in the zone.
            STAYINZONE, // Just being in zone on the scene.
            CRASHSITE, // Some settings to make it work for crash sites.
            AUTOCOMPLETE, // Completes once selected time is over.
            INTERACT, // Interact with object on the scene that spawns once you in zone.
        }

        public enum ExpeditionInteractiveImpact
        {
            EVERY,
            ONEOF,
            NONE,
        }

        public enum ExpeditionCompleteOrder
        {
            LINEAL, // Next and previous task will be visible, can be done if previous task is completed.
            LINEALHIDDEN, // Next tasks hidden, previous visible, can be done if previous task is completed.
            LINEALLAST, // Only this task will be visible, can be done if previous task is completed.
            ANYORDER, // Next tasks will be visible, can be done in any time.
        }

        public class ExpeditionInvite
        {
            public string m_InviterMAC = "";
            public string m_PersonToInviteMAC = "";
            public string m_InviterName = "";
            public int m_Timeout = 60;
        }
        public class SpecialExpeditionItem
        {
            public string m_GearReferenceName = "";

            public string m_GearName = "";
            public string m_GearDescription = "";
            
            public string m_ExpeditionAlias = "";

            public string m_ModelPrefab = "";
            public string m_GearIcon = "";

            public Vector3 m_Position = new Vector3(0, 0, 0);
            public Quaternion m_Rotation = new Quaternion(0, 0, 0, 0);
            public string m_Scene = "";
        }
        public static Expedition GetExpeditionByGUID(string GUID)
        {
            foreach (Expedition Exp in m_ActiveExpeditions)
            {
                if (Exp.m_GUID == GUID)
                {
                    return Exp;
                }
            }
            return null;
        }
        public static bool PlayerInExpedition(string MAC)
        {
            if (string.IsNullOrEmpty(MAC))
            {
                return false;
            }
            
            for (int i = 0; i < m_ActiveExpeditions.Count; i++)
            {
                if (m_ActiveExpeditions[i].m_Players.Contains(MAC))
                {
                    return true;
                }
            }
            return false;
        }
        public static bool PlayerInExpedition(int PlayerID)
        {
            return PlayerInExpedition(Server.GetMACByID(PlayerID));
        }
        public static bool StartNewExpedition(string LeaderMAC, int Region, string Alias = "", bool NoMessage = false, bool Special = false)
        {
            DebugLog("Client with MAC "+LeaderMAC+" trying start expedition on region "+Region);

            int LeaderID = Server.GetIDByMAC(LeaderMAC);

            for (int i = 0; i < m_ActiveExpeditions.Count; i++)
            {
                if (m_ActiveExpeditions[i].m_Players.Contains(LeaderMAC))
                {
                    if (!NoMessage)
                    {
                        ServerSend.ADDHUDMESSAGE(LeaderID, "You are already in expedition!");
                    }
                    return false;
                }
            }

            if((Shared.GameRegion)Region == Shared.GameRegion.RandomRegion)
            {
                if (!NoMessage)
                {
                    ServerSend.ADDHUDMESSAGE(LeaderID, "You can't start expedition here, try to move to another region.");
                }
                return false;
            }

            Expedition Exp = BuildBasicExpedition(Region, Alias, false, true);

            if(Exp == null)
            {
                if (!NoMessage)
                {
                    if (!Special)
                    {
                        ServerSend.ADDHUDMESSAGE(LeaderID, "No available expeditions, try to move to another region.");
                    } else
                    {
                        ServerSend.ADDHUDMESSAGE(LeaderID, "You can't start this expedition right now, please try later.");
                    }
                }

                return false;
            }

            DebugLog("Expedition created");
            DebugLog("Expedition Tasks:");
            for (int i = 0; i < Exp.m_Tasks.Count; i++)
            {
                DebugLog("Expedition Task["+i+"] " + Exp.m_Tasks[i].m_Alias+" Type " + Exp.m_Tasks[i].m_Type.ToString());
            }
            
            Exp.m_Players.Add(LeaderMAC);
            m_ActiveExpeditions.Add(Exp);

            if (LeaderID != 0)
            {
                ServerSend.EXPEDITIONRESULT(LeaderID, 2);
            } else
            {
#if (!DEDICATED)
                MyMod.DoExpeditionState(2);
#endif
            }
            return true;
        }

        public static Expedition GetActiveCrashSite()
        {
            if (string.IsNullOrEmpty(m_ActiveCrashSiteGUID))
            {
                return null;
            } else
            {
                foreach (Expedition Exp in m_ActiveExpeditions)
                {
                    if(Exp.m_GUID == m_ActiveCrashSiteGUID)
                    {
                        return Exp;
                    }
                }
            }
            return null;
        }

        public static void MayNotifyAboutCrashSite(int ClientID)
        {
            if (!string.IsNullOrEmpty(m_ActiveCrashSiteGUID))
            {
                ServerSend.EXPEDITIONRESULT(ClientID, 5);
            }
        }

        public static void StartCrashSite(int CrashSiteID = -1)
        {
            Expedition m_ActiveCrashSite = GetActiveCrashSite();
            if (m_ActiveCrashSite != null)
            {
                return;
            }

            string CrashSiteAlias = GetRandomCrashSiteName(CrashSiteID);

            if (string.IsNullOrEmpty(CrashSiteAlias))
            {
                if (CrashSiteID == -1)
                {
                    DebugLog("Wasn't able to find any valid crashsites!");
                } else
                {
                    DebugLog("Wasn't able to find crashsites with index " + CrashSiteID);
                }

                return;
            }
            DebugLog("CrashSite " + CrashSiteAlias + " going to be loaded!");

            Expedition Exp = BuildBasicExpedition(0, CrashSiteAlias);

            if (Exp == null)
            {
                return;
            }

            DebugLog("CrashSite created");
            DebugLog("CrashSite Tasks:");
            for (int i = 0; i < Exp.m_Tasks.Count; i++)
            {
                DebugLog("CrashSite Task[" + i + "] " + Exp.m_Tasks[i].m_Alias + " Type " + Exp.m_Tasks[i].m_Type.ToString());
            }

            for (int i = 0; i < MyMod.playersData.Count; ++i)
            {
                ServerSend.EXPEDITIONRESULT(i, 5);
            }

            m_ActiveExpeditions.Add(Exp);
            m_ActiveCrashSiteGUID = Exp.m_GUID;
            MultiplayerChatMessage Message = new MultiplayerChatMessage();
            Message.m_Message = "Plane with a valuable cargo crashed somewhere on " + GetRegionString(Exp.m_RegionBelong) + ", find the crash site before other players do.";
            Message.m_Type = 0;
            Message.m_By = "[Server]";
            Shared.SendMessageToChat(Message, true);
            Shared.WebhookCrashSiteSpawn(Message.m_Message);
        }

        public static void AcceptInvite(string Accepter, string Inviter)
        {
            int AccepterID = Server.GetIDByMAC(Accepter);
            string AccepterName = "";

#if (!DEDICATED)
            if (AccepterID == 0)
            {
                AccepterName = MyMod.MyChatName;
            }
#endif
            if (AccepterID != 0 && AccepterID != -1)
            {
                if (MyMod.playersData[AccepterID] != null)
                {
                    AccepterName = MyMod.playersData[AccepterID].m_Name;
                }
            }

            for (int i = 0; i < m_ActiveExpeditions.Count; i++)
            {
                if (m_ActiveExpeditions[i].m_Players.Contains(Accepter))
                {
                    ServerSend.ADDHUDMESSAGE(AccepterID, "You already in expedition!");
                    return;
                }
            }
            Expedition ActiveExpedition = null;

            for (int i = 0; i < m_ActiveExpeditions.Count; i++)
            {
                if (m_ActiveExpeditions[i].m_Players.Contains(Inviter))
                {
                    ActiveExpedition = m_ActiveExpeditions[i];
                    break;
                }
            }
            if (ActiveExpedition == null)
            {
                ServerSend.ADDHUDMESSAGE(AccepterID, "This invite expired!");
            }

            for (int i = m_Invites.Count-1; i >= 0; i--)
            {
                ExpeditionInvite CurInv = m_Invites[i];
                if (CurInv.m_InviterMAC == Inviter && CurInv.m_PersonToInviteMAC == Accepter)
                {
                    if (ActiveExpedition != null)
                    {
                        List<int> Players = ActiveExpedition.GetExpeditionPlayersIDs();
                        for (int i2 = 0; i2 < Players.Count; i2++)
                        { 
#if (!DEDICATED)
                            if(Players[i2] == 0)
                            {
                                MyMod.NewPlayerInExpedition(AccepterName);
                            } else
                            {
                                ServerSend.NEWPLAYEREXPEDITION(Players[i2], AccepterName);
                            }
#else
                            ServerSend.NEWPLAYEREXPEDITION(Players[i2], AccepterName);
#endif
                        }
                        ActiveExpedition.m_Players.Add(Accepter);

                        if (AccepterID != 0)
                        {
                            ServerSend.EXPEDITIONRESULT(AccepterID, 2);
                        } else
                        {
#if (!DEDICATED)
                            MyMod.DoExpeditionState(2);
#endif
                        }
                    }
                    m_Invites.RemoveAt(i);
                    return;
                }
            }
        }

        public static List<ExpeditionInvite> GetInviteForClient(string MAC)
        {
            List<ExpeditionInvite> Invites = new List<ExpeditionInvite>();
            foreach (ExpeditionInvite CurInv in m_Invites)
            {
                if (CurInv.m_PersonToInviteMAC == MAC)
                {
                    Invites.Add(CurInv);
                }
            }
            return Invites;
        }

        public static bool SendInvite(string Inviter, string ToInvite, int InviterID, int ToInviteID)
        {
            foreach (ExpeditionInvite CurInv in m_Invites)
            {
                if(CurInv.m_InviterMAC == Inviter && CurInv.m_PersonToInviteMAC == ToInvite)
                {
                    return false;
                }
            }
            ExpeditionInvite Invite = new ExpeditionInvite();
            Invite.m_InviterMAC = Inviter;
            Invite.m_PersonToInviteMAC = ToInvite;


#if (!DEDICATED)
            if(InviterID == 0)
            {
                Invite.m_InviterName = MyMod.MyChatName;
            }
#endif
            if(InviterID != 0 && InviterID != -1 && MyMod.playersData[InviterID] != null)
            {
                Invite.m_InviterName = MyMod.playersData[InviterID].m_Name;
            }

            m_Invites.Add(Invite);
            //ServerSend.ADDHUDMESSAGE(ToInviteID, "You got a new expedition invite.");
#if (!DEDICATED)
            if (ToInviteID == 0)
            {
                MyMod.NewExpeditionInvite(Invite.m_InviterName);
            } else
            {
                ServerSend.NEWEXPEDITIONINVITE(ToInviteID, Invite.m_InviterName);
            }
#else
            ServerSend.NEWEXPEDITIONINVITE(ToInviteID, Invite.m_InviterName);
#endif
            return true;
        }

        public static void CreateInviteToExpedition(string LeaderMAC, string InviteMAC)
        {
            Expedition MyExpedition = null;
            int LeaderID = Server.GetIDByMAC(LeaderMAC);
            int InviteID = Server.GetIDByMAC(InviteMAC);

            for (int i = 0; i < m_ActiveExpeditions.Count; i++)
            {
                if (m_ActiveExpeditions[i].m_Players.Contains(LeaderMAC))
                {
                    MyExpedition = m_ActiveExpeditions[i];
                }
            }

            if (MyExpedition != null)
            {
                bool AlreadyInExpedition = false;
                for (int i = 0; i < m_ActiveExpeditions.Count; i++)
                {
                    if (m_ActiveExpeditions[i].m_Players.Contains(InviteMAC))
                    {
                        AlreadyInExpedition = false;
                    }
                }

                if (!AlreadyInExpedition)
                {
                    if (!MyExpedition.m_Players.Contains(InviteMAC))
                    {
                        bool Ok = SendInvite(LeaderMAC, InviteMAC, LeaderID, InviteID);

                        if (Ok)
                        {
                            ServerSend.ADDHUDMESSAGE(LeaderID, "Invite sent!");
                        } else
                        {
                            ServerSend.ADDHUDMESSAGE(LeaderID, "You already invited this player to expedition");
                        }
                    } else
                    {
                        ServerSend.ADDHUDMESSAGE(LeaderID, "This player already in your expedition!");
                        return;
                    }
                } else{
                    ServerSend.ADDHUDMESSAGE(LeaderID, "This player already in another expedition!");
                    return;
                }
            } else
            {
                ServerSend.ADDHUDMESSAGE(LeaderID, "You are not in expedition!");
                return;
            }
        }

        public static void CompleteCrashsite(int FinishState = -2, string GUID = "")
        {
            int RemoveID = -1;
            if (string.IsNullOrEmpty(GUID) && m_ActiveCrashSiteGUID != "")
            {
                GUID = m_ActiveCrashSiteGUID;
            }
            for (int i = 0; i < m_ActiveExpeditions.Count; i++)
            {
                if (m_ActiveExpeditions[i].m_GUID == GUID)
                {
                    RemoveID = i;
                    break;
                }
            }
            if (RemoveID != -1)
            {
                CompleteCrashSite(RemoveID, new List<int>(), FinishState);
            }
        }

        public static void CompleteExpedition(string GUID, int FinishState = 1)
        {
            int RemoveID = -1;
            for (int i = 0; i < m_ActiveExpeditions.Count; i++)
            {
                if (m_ActiveExpeditions[i].m_GUID == GUID)
                {
                    RemoveID = i; 
                    break;
                }
            }
            if(RemoveID != -1)
            {
                CompleteExpedition(RemoveID, FinishState);
            }
        }
        public static void CompleteExpedition(int RemoveID, int FinishState = 1)
        {
            Expedition Exp = m_ActiveExpeditions[RemoveID];
            List<int> PlayersIDs = Exp.GetExpeditionPlayersIDs();
            if (RemoveID != -1)
            {
                foreach (int ClientID in PlayersIDs)
                {
                    if (ClientID == 0)
                    {
#if (!DEDICATED)
                        MyMod.DoExpeditionState(FinishState);
#endif
                    } else
                    {
                        ServerSend.EXPEDITIONRESULT(ClientID, FinishState);
                    }

                    if(FinishState == 1)
                    {
                        string MAC = Server.GetMACByID(ClientID);
                        if (!string.IsNullOrEmpty(MAC))
                        {
                            MPStats.AddExpedition(MAC, Exp.m_Alias);
                        }
                    }
                }
                if (FinishState == 0)
                {
                    foreach (ExpeditionTask Task in m_ActiveExpeditions[RemoveID].m_Tasks)
                    {
                        Task.RemoveAllObjects();
                    }
                }
                m_ActiveExpeditions.RemoveAt(RemoveID);
            }
        }

        public static void CompleteCrashSite(int RemoveID, List<int> ClosePlayers, int DefaultFinishState = -1)
        {
            List<int> PlayersIDs = m_ActiveExpeditions[RemoveID].GetExpeditionPlayersIDs(true);

            if (RemoveID != -1)
            {
                foreach (int ClientID in PlayersIDs)
                {
                    int FinishState = DefaultFinishState;
                    if (ClosePlayers.Contains(ClientID))
                    {
                        FinishState = 4;
                    }

                    if (ClientID == 0)
                    {
#if (!DEDICATED)
                        MyMod.DoExpeditionState(FinishState);
#endif
                    } else
                    {
                        ServerSend.EXPEDITIONRESULT(ClientID, FinishState);
                    }

                    if (FinishState == 4)
                    {
                        string MAC = Server.GetMACByID(ClientID);
                        if (!string.IsNullOrEmpty(MAC))
                        {
                            MPStats.AddCrashSite(MAC);
                        }
                    }
                }

                if(DefaultFinishState == -2)
                {
                    foreach (ExpeditionTask Task in m_ActiveExpeditions[RemoveID].m_Tasks)
                    {
                        Task.RemoveAllObjects();
                    }
                }

                m_ActiveExpeditions.RemoveAt(RemoveID);
                m_ActiveCrashSiteGUID = "";
            }
            MultiplayerChatMessage Message = new MultiplayerChatMessage();

            if(DefaultFinishState == -1)
            {
                Message.m_Message = "Crash site has been found!";
                Shared.WebhookCrashSiteFound();
            } else if(DefaultFinishState == -2)
            {
                Message.m_Message = "Time is up, no one has found the crash site.";
                Shared.WebhookCrashSiteTimeOver();
            }

            
            Message.m_Type = 0;
            Message.m_By = "[Server]";
            Shared.SendMessageToChat(Message, true);
            

        }

        public static void UpdateExpeditions()
        {
            //DebugLog("UpdateExpeditions() m_ActiveExpeditions.Count "+ m_ActiveExpeditions.Count);
            for (int i = m_ActiveExpeditions.Count - 1; i > -1; i--)
            {
                Expedition Exp = m_ActiveExpeditions[i];
                Exp.UpdateTasks();
                if (Exp.m_Completed)
                {
                    if (Exp.m_Tasks.Count > 0)
                    {
                        ExpeditionTask Task = Exp.m_Tasks[Exp.m_Tasks.Count - 1];
                        if (Task.m_Type == ExpeditionTaskType.CRASHSITE)
                        {
                            CompleteCrashSite(i, Task.GetCrashSiteNearPlayers());
                        } else
                        {
                            CompleteExpedition(i);
                        }
                    } else
                    {
                        CompleteExpedition(i);
                    }
                }
            }
            for (int i = m_Invites.Count - 1; i > -1; i--)
            {
                m_Invites[i].m_Timeout--;
                if (m_Invites[i].m_Timeout <= 0)
                {
                    m_Invites.RemoveAt(i);
                }
            }
            if(string.IsNullOrEmpty(m_ActiveCrashSiteGUID))
            {
                NextCrashSiteIn--;
                if (NextCrashSiteIn <= 0)
                {
                    int OneHour = 3600;
                    System.Random RNG = new System.Random();
                    NextCrashSiteIn = RNG.Next(OneHour * 3, OneHour * 6);
                    StartCrashSite();
                }
            }
        }

        public class Expedition
        {
            public string m_Name = "";
            public string m_Alias = "";
            public List<ExpeditionTask> m_Tasks = new List<ExpeditionTask>();
            public int m_RegionBelong = 0;
            public List<string> m_Players = new List<string>();
            public string m_GUID = "";
            public bool m_Completed = false;
            public int m_TasksCompleted = 0;
            public int m_TimeLeft = 7200;

            public List<DataStr.MultiPlayerClientData> GetExpeditionPlayersData(bool Everyone = false)
            {
                List<DataStr.MultiPlayerClientData> Data = new List<DataStr.MultiPlayerClientData>();

                if (Everyone)
                {
                    List<int> PlayersIDs = GetExpeditionPlayersIDs(Everyone);
                    foreach (int ClientID in PlayersIDs)
                    {
                        if (ClientID != -1 && ClientID != 0)
                        {
                            if (MyMod.playersData[ClientID] != null)
                            {
                                Data.Add(MyMod.playersData[ClientID]);
                            }
                        } else if (ClientID == 0)
                        {
#if (!DEDICATED)
                            MultiPlayerClientData P = new MultiPlayerClientData();
                            P.m_LevelGuid = MyMod.level_guid;
                            P.m_Position = GameManager.GetPlayerTransform().position;
                            Data.Add(P);
#endif
                        }
                    }
                    return Data;
                }
                foreach (string MAC in m_Players)
                {
                    int ClientID = Server.GetIDByMAC(MAC);
                    if (ClientID != -1 && ClientID != 0)
                    {
                        if (MyMod.playersData[ClientID] != null)
                        {
                            Data.Add(MyMod.playersData[ClientID]);
                        }
                    }else if(ClientID == 0)
                    {
#if (!DEDICATED)
                        MultiPlayerClientData P = new MultiPlayerClientData();
                        P.m_LevelGuid = MyMod.level_guid;
                        P.m_Position = GameManager.GetPlayerTransform().position;
                        Data.Add(P);
#endif
                    }
                }

                return Data;
            }
            public List<int> GetExpeditionPlayersIDs(bool Everyone = false)
            {
                List<int> IDs = new List<int>();
                bool IsCS = IsCrashSite();
                if (!IsCS)
                {
                    foreach (string MAC in m_Players)
                    {
                        int ClientID = Server.GetIDByMAC(MAC);
                        if (ClientID != -1)
                        {
                            if (MyMod.playersData[ClientID] != null)
                            {
                                IDs.Add(ClientID);
                            }
                        }
                    }
                } else
                {
                    if (Everyone)
                    {
                        for (int i = 0; i < MyMod.playersData.Count; i++)
                        {
                            if (MyMod.playersData[i] != null)
                            {
                                IDs.Add(i);
                            }
                        }
                    } else
                    {
                        for (int i = 0; i < MyMod.playersData.Count; i++)
                        {
                            if (MyMod.playersData[i] != null)
                            {
                                if (!PlayerInExpedition(i))
                                {
                                    IDs.Add(i);
                                }
                            }
                        }
                    }
                }
                return IDs;
            }

            public void OnTaskCompletedNotification()
            {
                foreach (int Client in GetExpeditionPlayersIDs())
                {
                    if (Client == 0)
                    {
#if (!DEDICATED)
                        MyMod.DoExpeditionState(3);
#endif
                    } else
                    {
                        ServerSend.EXPEDITIONRESULT(Client, 3);
                    }
                }
            }
            public void OnTaskCompleted(ExpeditionTask TaskCompleted)
            {
                if (TaskCompleted.m_TimeAdd)
                {
                    m_TimeLeft += TaskCompleted.m_Time;
                } else
                {
                    m_TimeLeft = TaskCompleted.m_Time;
                }
            }

            public bool IsCrashSite()
            {
                if (m_TimeLeft <= 0)
                {
                    if (m_Tasks.Count > 0)
                    {
                        ExpeditionTask Task = m_Tasks[m_Tasks.Count - 1];
                        if (Task.m_Type == ExpeditionTaskType.CRASHSITE)
                        {
                            return true;
                        } else
                        {
                            return false;
                        }
                    }
                }
                if (!string.IsNullOrEmpty(m_ActiveCrashSiteGUID) && m_ActiveCrashSiteGUID == m_GUID)
                {
                    return true;
                } else
                {
                    return false;
                }
            }

            public void UpdateTasks()
            {
                int NeedToComplete = m_Tasks.Count;
                int Completed = 0;
                m_TimeLeft--;
                if(m_TimeLeft <= 0)
                { 
                    if (m_Tasks.Count > 0)
                    {
                        ExpeditionTask Task = m_Tasks[m_Tasks.Count - 1];
                        if (IsCrashSite())
                        {
                            CompleteCrashsite(-2, m_GUID);
                        } else
                        {
                            CompleteExpedition(m_GUID, 0);
                        }
                    } else
                    {
                        CompleteExpedition(m_GUID, 0);
                    }
                    return;
                }

                List<DataStr.MultiPlayerClientData> PlayersData = GetExpeditionPlayersData(IsCrashSite());
                ExpeditionCompleteOrder CompleteOrder = ExpeditionCompleteOrder.LINEAL;
                bool CanUseNextCompleteOrder = true;
                bool DontUpdateLaterTasks = false;
                string Text = "";
                bool HideNextOnes = false;
                bool AllPreviousIsComplete = true;
                for (int i = 0; i < m_Tasks.Count; i++)
                {
                    ExpeditionTask Task = m_Tasks[i];
                    ExpeditionTask NextTask = null;
                    bool LastAnyOrder = false;
                    if (i + 1 < m_Tasks.Count)
                    {
                        NextTask = m_Tasks[i + 1];
                    }
                    if(Task.m_CompleteOrder == ExpeditionCompleteOrder.ANYORDER && NextTask != null && NextTask.m_CompleteOrder != ExpeditionCompleteOrder.ANYORDER)
                    {
                        LastAnyOrder = true;
                    }
                    if (AllPreviousIsComplete && !Task.m_IsComplete)
                    {
                        AllPreviousIsComplete = false;
                    }

                    if (!DontUpdateLaterTasks)
                    {
                        if (Task.m_IsComplete)
                        {
                            Completed++;
                        } else // If not completed
                        {
                            Task.Update(PlayersData);

                            if (CanUseNextCompleteOrder)
                            {
                                CompleteOrder = Task.m_CompleteOrder;
                                CanUseNextCompleteOrder = false;
                            }
                        }
                        if (((CompleteOrder == ExpeditionCompleteOrder.LINEAL || CompleteOrder == ExpeditionCompleteOrder.LINEALHIDDEN || CompleteOrder == ExpeditionCompleteOrder.LINEALLAST) && !Task.m_IsComplete) || (LastAnyOrder && !AllPreviousIsComplete)) // If Lineal do not update later tasks. And if AnyOrder is last, dont update later tasks.
                        {
                            DontUpdateLaterTasks = true;
                        }
                    }
                    string ColorPrefix = "[FFFFFF]";
                    string ColorFinishPrefix = "[707070]";
                    string ColorAffix = "[-]";
                    string ProgressBar = "";
                    if(Task.m_Type == ExpeditionTaskType.STAYINZONE)
                    {
                        if (!Task.m_IsComplete && Task.m_SecondsInZone > 0)
                        {
                            int Procent = Task.GetCompleteProcent();
                            ProgressBar = "Progress: [";

                            int TotalLines = 18;
                            int Stages = (TotalLines * Procent) / 100;
                            if (Stages > 0)
                            {
                                ProgressBar += "[00FF00]";
                            }
                            for (int i2 = 1; i2 <= TotalLines; i2++)
                            {
                                ProgressBar += "■";
                                if(Stages == i2)
                                {
                                    ProgressBar += ColorAffix;
                                }
                            }
                            ProgressBar += "] " + +Procent + "%";
                            ProgressBar = "\n" + ProgressBar;

                        }
                    }

                    if (CompleteOrder == ExpeditionCompleteOrder.LINEALLAST)
                    {
                        if (!Task.m_IsComplete && !HideNextOnes)
                        {
                            Text = ColorPrefix + Task.m_Text + ColorAffix + ProgressBar;
                            HideNextOnes = true;
                        }
                    } else if (CompleteOrder == ExpeditionCompleteOrder.LINEAL || CompleteOrder == ExpeditionCompleteOrder.ANYORDER || CompleteOrder == ExpeditionCompleteOrder.LINEALHIDDEN)
                    {
                        if (m_Tasks.Count > 1)
                        {
                            if(!HideNextOnes)
                            {
                                string TaskText = ColorPrefix + "- ";
                                if (Task.m_IsComplete)
                                {
                                    TaskText = ColorFinishPrefix + "- ";
                                }

                                if (string.IsNullOrEmpty(Text))
                                {
                                    TaskText += Task.m_Text + ColorAffix + ProgressBar;
                                } else
                                {
                                    TaskText = "\n" + TaskText + Task.m_Text + ColorAffix + ProgressBar;
                                }
                                Text += TaskText;
                            }
                        } else
                        {
                            Text = ColorPrefix + "- " + Task.m_Text + ColorAffix + ProgressBar;
                        }
                    }
                    if (!Task.m_IsComplete && CompleteOrder == ExpeditionCompleteOrder.LINEALHIDDEN)
                    {
                        HideNextOnes = true;
                    }
                    if (LastAnyOrder && !AllPreviousIsComplete && NextTask != null && (NextTask.m_CompleteOrder == ExpeditionCompleteOrder.LINEALHIDDEN || NextTask.m_CompleteOrder == ExpeditionCompleteOrder.LINEALLAST))
                    {
                        HideNextOnes = true;
                    }
                }

                if(m_TasksCompleted < Completed) // Completed Updated
                {
                    m_TasksCompleted = Completed;
                    if(NeedToComplete > Completed) // This is not last task
                    {
                        OnTaskCompletedNotification();
                    }
                }

                foreach (int Client in GetExpeditionPlayersIDs())
                {
                    if(Client == 0)
                    {
#if (!DEDICATED)
                        MyMod.OnExpedition = true;
                        MyMod.ExpeditionLastName = m_Name;
                        MyMod.ExpeditionLastTaskText = Text;
                        MyMod.ExpeditionLastTime = m_TimeLeft;
                        MyMod.LastExpeditionAlias = m_Alias;
#endif
                    } else
                    {
                        ServerSend.EXPEDITIONSYNC(Client, m_Name, Text, m_TimeLeft, m_Alias);
                    }
                }

                
                //DebugLog("UpdateTasks() " + Completed + "/" + NeedToComplete);

                if (NeedToComplete == Completed)
                {
                    m_Completed = true;
                }
            }
        }
        public static void RegisterInteractionDone(string GUID)
        {
            DebugLog("[RegisterInteractionDone] " + GUID);
            for (int i = m_ActiveExpeditions.Count - 1; i > -1; i--)
            {
                for (int i2 = m_ActiveExpeditions[i].m_Tasks.Count - 1; i2 > -1; i2--)
                {
                    if (m_ActiveExpeditions[i].m_Tasks[i2].m_Type == ExpeditionTaskType.INTERACT)
                    {
                        m_ActiveExpeditions[i].m_Tasks[i2].CheckInteractiveDone(GUID);
                    }
                }
            }
        }
        public static void RegisterFlaregunShot(string Scene, Vector3 Position)
        {
            DebugLog("[RegisterFlaregunShot] "+ Scene);
            for (int i = m_ActiveExpeditions.Count - 1; i > -1; i--)
            {
                for (int i2 = m_ActiveExpeditions[i].m_Tasks.Count - 1; i2 > -1; i2--)
                {
                    if (m_ActiveExpeditions[i].m_Tasks[i2].m_Type == ExpeditionTaskType.FLAREGUNSHOT)
                    {
                        m_ActiveExpeditions[i].m_Tasks[i2].CheckFlaregunShot(Scene, Position);
                    }
                }
            }
        }
        public static void RegisterCharcoalDrawing(string Scene, Vector3 Position)
        {
            DebugLog("[RegisterCharcoalDrawing] " + Scene);
            for (int i = m_ActiveExpeditions.Count - 1; i > -1; i--)
            {
                for (int i2 = m_ActiveExpeditions[i].m_Tasks.Count - 1; i2 > -1; i2--)
                {
                    if (m_ActiveExpeditions[i].m_Tasks[i2].m_Type == ExpeditionTaskType.CHARCOAL)
                    {
                        m_ActiveExpeditions[i].m_Tasks[i2].CheckCharcoalSpot(Scene, Position);
                    }
                }
            }
        }

        public static void SpawnObjectsFromSpawners( List<UniversalSyncableObjectSpawner> m_ObjectSpawners, string m_ExpeditionGUID, string m_Scene, bool AddContainersLoot, ExpeditionTask Task = null)
        {
            if (m_ObjectSpawners.Count > 0)
            {
                foreach (UniversalSyncableObjectSpawner Spawner in m_ObjectSpawners)
                {
                    if (Spawner.m_Prefab == "Expedition3DAudioEvent" || Spawner.m_Prefab == "Expedition2DAudioEvent" || Spawner.m_Prefab == "ExpeditionAudioEvent")
                    {
                        if (Spawner.m_Prefab == "ExpeditionAudioEvent")
                        {
                            Expedition exp = GetExpeditionByGUID(m_ExpeditionGUID);
                            if (exp != null)
                            {
                                List<int> Players = exp.GetExpeditionPlayersIDs(true);
                                foreach (int PlayerID in Players)
                                {
#if (!DEDICATED)
                                    if (PlayerID == 0)
                                    {
                                        MyMod.PlayCustomSoundEvent(Spawner.m_Position, Spawner.m_Content, Spawner.m_Prefab);
                                    } else
                                    {
                                        ServerSend.CUSTOMSOUNDEVENT(Spawner.m_Position, Spawner.m_Content, Spawner.m_Prefab, PlayerID);
                                    }
#else
                                    ServerSend.CUSTOMSOUNDEVENT(Spawner.m_Position, Spawner.m_Content, Spawner.m_Prefab, PlayerID);
#endif
                                }
                            }
                            continue;
                        }
#if (!DEDICATED)
                        if (MyMod.level_guid == m_Scene)
                        {
                            MyMod.PlayCustomSoundEvent(Spawner.m_Position, Spawner.m_Content, Spawner.m_Prefab);
                        }
#endif
                        ServerSend.CUSTOMSOUNDEVENT(Spawner.m_Position, Spawner.m_Content, Spawner.m_Prefab, m_Scene);
                        continue;
                    }

                    if (Spawner.m_Prefab == "RockCache")
                    {
                        FakeRockCacheVisualData FRCVD = new FakeRockCacheVisualData();
                        FRCVD.m_GUID = Spawner.m_GUID;
                        FRCVD.m_Position = Spawner.m_Position;
                        FRCVD.m_Rotation = Spawner.m_Rotation;
                        FRCVD.m_Owner = "Unknown";
#if (!DEDICATED)
                        if (MyMod.level_guid == m_Scene)
                        {
                            MyMod.AddRockCache(FRCVD);
                        }
#endif
                        MPSaveManager.AddRockCach(FRCVD, 0);
                        MPSaveManager.SaveContainer(m_Scene, Spawner.m_GUID, Spawner.m_Content);
                        continue;
                    }
                    UniversalSyncableObject Obj = new UniversalSyncableObject();
                    Obj.m_Prefab = Spawner.m_Prefab;
                    Obj.m_GUID = Spawner.m_GUID;
                    Obj.m_Position = Spawner.m_Position;
                    Obj.m_Rotation = Spawner.m_Rotation;

                    Obj.m_ExpeditionBelong = m_ExpeditionGUID;
                    Obj.m_Scene = m_Scene;
                    Obj.m_CreationTime = MyMod.MinutesFromStartServer;
                    Obj.m_RemoveTime = 0;
                    Obj.m_InteractiveData = Spawner.m_InteractiveData;
                    Obj.m_ObjectGroup = Spawner.m_ObjectGroup;

                    if (Obj.m_Prefab == "ExpeditionInteractive")
                    {
                        if (Task != null)
                        {
                            if (Obj.m_InteractiveData.m_Impact == ExpeditionInteractiveImpact.EVERY)
                            {
                                Task.m_RequiredInteractionsEvery.Add(Obj.m_GUID, false);
                                Task.m_RequiredInteractionsEveryDone = false;
                            } else if (Obj.m_InteractiveData.m_Impact == ExpeditionInteractiveImpact.ONEOF)
                            {
                                Task.m_RequiredInteractionsAny.Add(Obj.m_GUID);
                                Task.m_RequiredInteractionsAnyDone = false;
                            }
                        }
                    }
                    // Add
                    MPSaveManager.AddUniversalSyncableObject(Obj);
                    if (AddContainersLoot)
                    {
                        // Wipe previous data.
                        MPSaveManager.RemoveContainer(m_Scene, Spawner.m_GUID);
                        for (int i = 1; i < 10; i++)
                        {
                            MPSaveManager.RemoveContainer(m_Scene, Spawner.m_GUID + i);
                        }
                        // Add new loot
                        if (!string.IsNullOrEmpty(Spawner.m_Content))
                        {
                            MPSaveManager.SaveContainer(m_Scene, Spawner.m_GUID, Spawner.m_Content);
                            MPSaveManager.SetConstainerState(m_Scene, Spawner.m_GUID, 1, true);
                        } else
                        {
                            MPSaveManager.SetConstainerState(m_Scene, Spawner.m_GUID, 2, true);
                        }
                    }

                    // Spawn and sync
#if (!DEDICATED)
                    if (MyMod.level_guid == m_Scene)
                    {
                        MyMod.SpawnUniversalSyncableObject(Obj);
                    }
#endif
                    ServerSend.ADDUNIVERSALSYNCABLE(Obj);
                }
            }
        }

        public class ExpeditionTask
        {
            public ExpeditionTaskType m_Type = ExpeditionTaskType.ENTERSCENE;
            public ExpeditionCompleteOrder m_CompleteOrder = ExpeditionCompleteOrder.LINEAL;
            public string m_Text = "";
            public string m_Scene = "";
            public string m_Alias = "";
            public Vector3 m_ZoneCenter = new Vector3(0, 0, 0);
            public float m_ZoneRadius = 0;
            public List<string> m_SpecificContrainers = new List<string>();
            public List<ExpeditionGearSpawner> m_GearSpawners = new List<ExpeditionGearSpawner>();
            public List<string> m_SpecificPlants = new List<string>();
            public List<string> m_SpecificBreakdowns = new List<string>();
            public List<UniversalSyncableObjectSpawner> m_ObjectSpawners = new List<UniversalSyncableObjectSpawner>();
            public bool m_ObjectsSpawned = false;
            public bool m_RestockSceneContainers = false;
            public bool m_IsComplete = false;
            public string m_ObjectiveGearSpawnerGUID = "";
            public bool m_ObjectiveGearSpawned = false;
            public bool m_Debug = false;
            public bool m_RewardAlreadySpawned = false;
            public string m_ExpeditionGUID = null;
            public int m_LastPlayersAmout = 1;
            public bool m_CanCheckFlaregun = false;
            public bool m_DidFlareShots = false;
            public bool m_CanCheckCharcoal = false;
            public int m_Time = 3600;
            public bool m_TimeAdd = true;
            public int m_SecondsInZone = 0;
            public int m_StayInZoneSeconds = 300;
            public Dictionary<string, bool> m_RequiredInteractionsEvery = new Dictionary<string, bool>();
            public List<string> m_RequiredInteractionsAny = new List<string>();
            public bool m_RequiredInteractionsAnyDone = true;
            public bool m_RequiredInteractionsEveryDone = true;
            public void StartDespawnOfAllObjects()
            {
                if (m_ObjectsSpawned)
                {
                    foreach (UniversalSyncableObjectSpawner Spawner in m_ObjectSpawners)
                    {
                        UniversalSyncableObject Obj = new UniversalSyncableObject();
                        Obj.m_Prefab = Spawner.m_Prefab;
                        Obj.m_GUID = Spawner.m_GUID;
                        Obj.m_Position = Spawner.m_Position;
                        Obj.m_Rotation = Spawner.m_Rotation;

                        Obj.m_ExpeditionBelong = m_ExpeditionGUID;
                        Obj.m_Scene = m_Scene;
                        Obj.m_CreationTime = MyMod.MinutesFromStartServer;
                        Obj.m_RemoveTime = MyMod.MinutesFromStartServer + 1440;
                        Obj.m_ObjectGroup = Spawner.m_ObjectGroup;
                        MPSaveManager.AddUniversalSyncableObject(Obj);
                    }
                }
            }

            public void RemoveAllObjects()
            {
                if (m_ObjectsSpawned)
                {
                    foreach (UniversalSyncableObjectSpawner Spawner in m_ObjectSpawners)
                    {
                        MPSaveManager.RemoveUniversalSyncableObject(m_Scene, Spawner.m_GUID);
#if (!DEDICATED)
                        if (MyMod.level_guid == m_Scene)
                        {
                            MyMod.RemoveObjectByGUID(Spawner.m_GUID);
                        }
#endif
                    }
                }
            }
            public void SpawnObjects()
            {
                if (!m_ObjectsSpawned)
                {
                    DebugLog("Task " + m_Alias + " SpawnObjects");
                    SpawnObjectsFromSpawners(m_ObjectSpawners, m_ExpeditionGUID, m_Scene, true, this);
                }
                m_ObjectsSpawned = true;
            }
            public void SpawnReward()
            {
                if (m_RewardAlreadySpawned)
                {
                    return;
                }
                DebugLog("Task "+ m_Alias + " SpawnReward");
                
                m_RewardAlreadySpawned = true;
                //DebugLog("m_RestockSceneContainers "+ m_RestockSceneContainers);
                if (m_RestockSceneContainers)
                {
                    MPSaveManager.AddLootToScene(m_Scene);
                }
                //DebugLog("m_SpecificContrainers.Count " + m_SpecificContrainers.Count);
                if (m_SpecificContrainers.Count > 0)
                {
                    foreach (string ContainerGUID in m_SpecificContrainers)
                    {
                        MPSaveManager.AddLootToContainerOnScene(ContainerGUID, m_Scene);
                    }
                }
                //DebugLog("m_GearSpawnerGears.Count " + m_GearSpawners.Count);
                if (m_GearSpawners.Count > 0)
                {
                    foreach (ExpeditionGearSpawner spawn in m_GearSpawners)
                    {
                        CreateRewardGear(spawn, m_Scene, m_LastPlayersAmout, m_Debug);
                    }
                }
                if (!string.IsNullOrEmpty(m_ObjectiveGearSpawnerGUID))
                {
                    if (m_UnavailableGearSpawners.ContainsKey(m_ObjectiveGearSpawnerGUID))
                    {
                        m_ObjectiveGearSpawned = true;
                        DebugLog("Objective Gear Spawned!");
                    }
                }
                //DebugLog("m_SpecificPlants.Count " + m_SpecificPlants.Count);
                if (m_SpecificPlants.Count > 0)
                {
                    MPSaveManager.ForceGrowPlants(m_Scene, m_SpecificPlants);
                }
                //DebugLog("m_SpecificBreakdowns.Count " + m_SpecificBreakdowns.Count);
                if (m_SpecificBreakdowns.Count > 0)
                {
                    MPSaveManager.RepairBreakdowns(m_Scene, m_SpecificBreakdowns);
                }
            }

            public int GetCompleteProcent()
            {
                return (100 * m_SecondsInZone) / m_StayInZoneSeconds;
            }

            public void OnCompleted()
            {
                if (m_IsComplete)
                {
                    return;
                }
                DebugLog("Task " + m_Alias + " OnCompleted");

                m_IsComplete = true;


                if(m_Type != ExpeditionTaskType.CRASHSITE)
                {
                    foreach (Expedition Exp in m_ActiveExpeditions)
                    {
                        if(Exp.m_GUID == m_ExpeditionGUID)
                        {
                            Exp.OnTaskCompleted(this);
                            break;
                        }
                    }
                }
                SpawnReward();
                StartDespawnOfAllObjects();
            }

            public void CheckInteractiveDone(string GUID)
            {
                if (!m_IsComplete)
                {
                    bool CanDelete = false;
                    bool WipeAnylist = false;
                    
                    if(!m_RequiredInteractionsAnyDone && m_RequiredInteractionsAny.Count > 0)
                    {
                        for (int i = 0; i < m_RequiredInteractionsAny.Count; i++)
                        {
                            if (m_RequiredInteractionsAny[i] == GUID)
                            {
                                m_RequiredInteractionsAnyDone = true;
                                WipeAnylist = true;
                                break;
                            }
                        }
                    }
                    if (!m_RequiredInteractionsEveryDone && m_RequiredInteractionsEvery.Count > 0)
                    {
                        if (m_RequiredInteractionsEvery.ContainsKey(GUID))
                        {
                            m_RequiredInteractionsEvery.Remove(GUID);
                            m_RequiredInteractionsEvery.Add(GUID, true);
                            CanDelete = true;
                        }
                        foreach (var item in m_RequiredInteractionsEvery)
                        {
                            if (item.Value)
                            {
                                m_RequiredInteractionsEveryDone = true;
                            } else
                            {
                                break;   
                            }
                        }
                    }

                    if (WipeAnylist)
                    {
                        for (int i = 0; i < m_RequiredInteractionsAny.Count; i++)
                        {
                            MPSaveManager.RemoveUniversalSyncableObject(m_Scene, m_RequiredInteractionsAny[i]);
#if (!DEDICATED)
                            if (MyMod.level_guid == m_Scene)
                            {
                                MyMod.RemoveObjectByGUID(m_RequiredInteractionsAny[i]);
                            }
#endif
                        }
                    }
                    if(CanDelete)
                    {
                        MPSaveManager.RemoveUniversalSyncableObject(m_Scene, GUID);
#if (!DEDICATED)
                        if (MyMod.level_guid == m_Scene)
                        {
                            MyMod.RemoveObjectByGUID(GUID);
                        }
#endif
                    }
                }
            }

            public void CheckFlaregunShot(string Scene, Vector3 Position)
            {
                if (!m_IsComplete && m_CanCheckFlaregun)
                {
                    if (m_Type == ExpeditionTaskType.FLAREGUNSHOT)
                    {
                        if (Scene == m_Scene && Vector3.Distance(Position, m_ZoneCenter) <= m_ZoneRadius)
                        {
                            OnCompleted();
                            return;
                        }
                    }
                }
            }
            public void CheckCharcoalSpot(string Scene, Vector3 Position)
            {
                if (!m_IsComplete && m_CanCheckCharcoal)
                {
                    if (m_Type == ExpeditionTaskType.CHARCOAL)
                    {
                        if (Scene == m_Scene && Vector3.Distance(Position, m_ZoneCenter) <= m_ZoneRadius)
                        {
                            OnCompleted();
                            return;
                        }
                    }
                }
            }

            public List<int> GetCrashSiteNearPlayers()
            {
                List<int> PlayersIDs = new List<int>();

#if (!DEDICATED)
                if(MyMod.level_guid == m_Scene && Vector3.Distance(GameManager.GetPlayerTransform().position, m_ZoneCenter) <= m_ZoneRadius * 2)
                {
                    PlayersIDs.Add(0);
                }
#endif
                for (int i = 0; i < MyMod.playersData.Count; i++)
                {
                    if (MyMod.playersData[i] != null)
                    {
                        MultiPlayerClientData PlayerData = MyMod.playersData[i];

                        if(PlayerData.m_LevelGuid == m_Scene && Vector3.Distance(PlayerData.m_Position, m_ZoneCenter) <= m_ZoneRadius *2)
                        {
                            PlayersIDs.Add(i);
                        }
                    }
                }
                return PlayersIDs;
            }

            public void Update(List<DataStr.MultiPlayerClientData> PlayersData)
            {
                m_LastPlayersAmout = PlayersData.Count;

                if (!m_ObjectsSpawned)
                {
                    SpawnObjects();
                }

                if (!m_DidFlareShots)
                {
                    DoFlareShots(m_GearSpawners, m_Scene);
                    m_DidFlareShots = true;
                }

                if(m_Type == ExpeditionTaskType.FLAREGUNSHOT)
                {
                    m_CanCheckFlaregun = true;
                }
                if (m_Type == ExpeditionTaskType.CHARCOAL)
                {
                    m_CanCheckCharcoal = true;
                }

                if (m_Type == ExpeditionTaskType.COLLECT)
                {
                    if (m_ObjectiveGearSpawned)
                    {
                        if (!m_UnavailableGearSpawners.ContainsKey(m_ObjectiveGearSpawnerGUID))
                        {
                            OnCompleted();
                            return;
                        }
                    } else {
                        foreach (DataStr.MultiPlayerClientData PlayerData in PlayersData)
                        {
                            if (PlayerData.m_LevelGuid == m_Scene && Vector3.Distance(PlayerData.m_Position, m_ZoneCenter) <= m_ZoneRadius)
                            {
                                SpawnReward();
                                return;
                            }
                        }
                    }
                } else if(m_Type == ExpeditionTaskType.STAYINZONE)
                {
                    foreach (DataStr.MultiPlayerClientData PlayerData in PlayersData)
                    {
                        if (PlayerData.m_LevelGuid == m_Scene && Vector3.Distance(PlayerData.m_Position, m_ZoneCenter) <= m_ZoneRadius)
                        {
                            m_SecondsInZone++;
                        }
                    }
                    if(m_SecondsInZone >= m_StayInZoneSeconds)
                    {
                        OnCompleted();
                        return;
                    }
                } else if (m_Type == ExpeditionTaskType.ENTERSCENE)
                {
                    foreach (DataStr.MultiPlayerClientData PlayerData in PlayersData)
                    {
                        if (PlayerData.m_LevelGuid == m_Scene)
                        {
                            OnCompleted();
                            return;
                        }
                    }
                } else if(m_Type == ExpeditionTaskType.ENTERZONE)
                {
                    foreach (DataStr.MultiPlayerClientData PlayerData in PlayersData)
                    {
                        if (PlayerData.m_LevelGuid == m_Scene && Vector3.Distance(PlayerData.m_Position, m_ZoneCenter) <= m_ZoneRadius)
                        {
                            OnCompleted();
                            return;
                        }
                    }
                } else if (m_Type == ExpeditionTaskType.CRASHSITE)
                {
                    foreach (DataStr.MultiPlayerClientData PlayerData in PlayersData)
                    {
                        if (PlayerData.m_LevelGuid == m_Scene && Vector3.Distance(PlayerData.m_Position, m_ZoneCenter) <= m_ZoneRadius)
                        {
                            OnCompleted();
                            return;
                        }
                    }
                } else if (m_Type == ExpeditionTaskType.AUTOCOMPLETE)
                {
                    m_SecondsInZone++;
                    if (m_SecondsInZone >= m_StayInZoneSeconds)
                    {
                        OnCompleted();
                        return;
                    }
                } else if(m_Type == ExpeditionTaskType.INTERACT)
                {
                    if (!m_IsComplete)
                    {
                        if (m_ObjectsSpawned && m_RequiredInteractionsEveryDone && m_RequiredInteractionsAnyDone)
                        {
                            OnCompleted();
                        }
                    }
                }
            }
        }

        public static void RemoveGearSpawnerGear(int SearchKey)
        {
            if (m_GearSpawnerGears.ContainsKey(SearchKey))
            {
                string SpawnerGUID = m_GearSpawnerGears[SearchKey];
                if (m_UnavailableGearSpawners.ContainsKey(SpawnerGUID))
                {
                    m_UnavailableGearSpawners.Remove(SpawnerGUID);
                }
                m_GearSpawnerGears.Remove(SearchKey);
            }
        }

        public static void DoFlareShots(List<ExpeditionGearSpawner> Spawners, string Scene)
        {
            foreach (ExpeditionGearSpawner Spawner in Spawners)
            {
                string Prefab = Spawner.PickGear();
                Vector3 PlaceV3 = Spawner.m_Possition;
                Quaternion Rotation = Spawner.m_Rotation;

                if (Prefab == "GEAR_FlareGunShoot")
                {
                    ShootSync shot = new ShootSync();
                    shot.m_position = PlaceV3;
                    shot.m_rotation = Rotation;
                    shot.m_camera_up = new Vector3(0, 1, 0);
                    shot.m_lookat = true;
                    shot.m_projectilename = "GEAR_FlareGunAmmoSingle";
                    shot.m_sceneguid = Scene;
                    ServerSend.SHOOTSYNC(0, shot, true);
#if (!DEDICATED)
                    MyMod.DoShootSync(shot, 0);
#endif
                }
            }
        }


        public static void CreateRewardGear(ExpeditionGearSpawner Spawner, string Scene, int PlayersInExpedition = 1, bool DebugFlag = false)
        {
            if (!m_UnavailableGearSpawners.ContainsKey(Spawner.m_GUID))
            {
                if (Spawner.RollChance(PlayersInExpedition) || DebugFlag)
                {
                    string Prefab = Spawner.PickGear();

                    if (string.IsNullOrEmpty(Prefab))
                    {
                        return;
                    }


                    Vector3 PlaceV3 = Spawner.m_Possition;
                    Quaternion Rotation = Spawner.m_Rotation;
                    int SearchKey;

                    if(Prefab == "GEAR_FlareGunShoot")
                    {
                        return;
                    }

                    SlicedJsonDroppedGear NewGear = new SlicedJsonDroppedGear();
                    NewGear.m_GearName = Prefab.ToLower();
                    NewGear.m_Extra.m_DroppedTime = MyMod.MinutesFromStartServer;
                    NewGear.m_Extra.m_Dropper = Spawner.m_Extra.m_Dropper;
                    NewGear.m_Extra.m_GearName = NewGear.m_GearName;
                    NewGear.m_Extra.m_Variant = 0;
                    NewGear.m_Extra.m_PhotoGUID = Spawner.m_Extra.m_PhotoGUID;
                    NewGear.m_Extra.m_ExpeditionNote = Spawner.m_Extra.m_ExpeditionNote;

                    int hashV3 = Shared.GetVectorHash(PlaceV3);
                    int hashRot = Shared.GetQuaternionHash(Rotation);
                    int hashLevelKey = Scene.GetHashCode();
                    SearchKey = hashV3 + hashRot + hashLevelKey;

                    DroppedGearItemDataPacket GearVisual = new DroppedGearItemDataPacket();
                    GearVisual.m_Extra = NewGear.m_Extra;
                    GearVisual.m_GearID = -1;
                    GearVisual.m_Hash = SearchKey;
                    GearVisual.m_LevelGUID = Scene;
                    GearVisual.m_Position = PlaceV3;
                    GearVisual.m_Rotation = Rotation;
                    NewGear.m_Json = "";
                    MPSaveManager.AddGearData(Scene, SearchKey, NewGear);
                    MPSaveManager.AddGearVisual(Scene, GearVisual);
                    DebugLog("[CreateRewardGear] NewGear.m_GearName " + NewGear.m_GearName);

                    m_UnavailableGearSpawners.Add(Spawner.m_GUID, SearchKey);

                    if (m_GearSpawnerGears.ContainsKey(SearchKey)) // Who knows, shit happends.
                    {
                        m_GearSpawnerGears.Remove(SearchKey);
                    }
                    m_GearSpawnerGears.Add(SearchKey, Spawner.m_GUID);
#if (!DEDICATED)
                    Shared.FakeDropItem(GearVisual, true);
#endif
                    ServerSend.DROPITEM(0, GearVisual, true);
                }
            }
        }
        public static void ApplyWorldEdits()
        {
            List<string> WorldEdits = GetWorldEdits();
            foreach (string Alias in WorldEdits)
            {
                ExpeditionTaskTemplate TaksTemp = JSON.Load(GetExpeditionJsonByAlias(Alias)).Make<ExpeditionTaskTemplate>();
                SpawnObjectsFromSpawners(TaksTemp.m_Objects, "WorldEdit", TaksTemp.m_SceneName, false);
            }
        }

        public static void RemoveObjectGroup(string group)
        {
            foreach (var Dict in MPSaveManager.UniversalSyncableObjects.ToList())
            {
                string Scene = Dict.Key;
                foreach (var Obj in Dict.Value.ToList())
                {
                    if(Obj.Value.m_ObjectGroup == group)
                    {
                        MPSaveManager.RemoveUniversalSyncableObject(Scene, Obj.Value.m_GUID);
#if (!DEDICATED)
                        if(MyMod.level_guid == Scene)
                        {
                            MyMod.RemoveObjectByGUID(Obj.Value.m_GUID);
                        }
#endif
                    }
                }
            }
        }

        public static List<string> GetAllSpeicalItemsOfPlayer(string MAC)
        {
            List<string> Belongings = new List<string>();
            if (m_SpecialItemsOwners.TryGetValue(MAC, out Belongings))
            {
                return Belongings;
            }
            return new List<string>();
        }

        public static bool PlayerHasSpecialItem(string MAC, string Item)
        {
            List<string> Belongings = GetAllSpeicalItemsOfPlayer(MAC);
            foreach (string CheckItem in Belongings)
            {
                if (CheckItem == Item)
                {
                    return true;
                }
            }
            return false;
        }

        public static bool PlayerCanRequestSepcialExpedition(string MAC, string ExpeditionName)
        {
            foreach (SpecialExpeditionItem item in m_SpecialItems)
            {
                if(item.m_ExpeditionAlias == ExpeditionName)
                {
                    return PlayerHasSpecialItem(MAC, item.m_GearReferenceName);
                }
            }
            return false;
        }

        public static void MaySpawnSpeicalExpeditionItem(int ClientID, string Scene)
        {
            foreach (SpecialExpeditionItem item in m_SpecialItems)
            {
                if(item.m_Scene == Scene)
                {
                    if (PlayerHasSpecialItem(Server.GetMACByID(ClientID), item.m_GearReferenceName) == false)
                    {
                        DroppedGearItemDataPacket Data = new DroppedGearItemDataPacket();
                        Data.m_Position = item.m_Position;
                        Data.m_Rotation = item.m_Rotation;
                        Data.m_LevelGUID = item.m_Scene;
                        Data.m_Extra.m_GearName = item.m_ModelPrefab;
                        Data.m_Extra.m_Variant = -2;
                        Data.m_Extra.m_ExpeditionNote = item.m_GearName;
                        Data.m_Extra.m_PhotoGUID = item.m_GearReferenceName;
                        if (ClientID == 0)
                        {
#if (!DEDICATED)
                            MyMod.FakeDropItem(-1, Data.m_Position, Data.m_Rotation, 0, Data.m_Extra);
#endif
                        } else
                        {
                            ServerSend.DROPITEM(0, Data, false, ClientID);
                        }
                        
                        Log("Created special item "+ item.m_GearName + " for client "+ ClientID);
                    }
                }
            }
        }

        public static void GivePlayerSpeicalItem(int ClientID, string Item)
        {
            string MAC = Server.GetMACByID(ClientID);
            if(m_SpecialItemsOwners.ContainsKey(MAC))
            {
                m_SpecialItemsOwners[MAC].Add(Item);
            } else
            {
                m_SpecialItemsOwners.Add(MAC, new List<string>() { Item });
            }
        }
    }
}

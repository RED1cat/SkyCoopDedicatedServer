using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GameServer;
using System.Security.Policy;
using static SkyCoop.DataStr;
using static SkyCoop.ExpeditionBuilder;
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
        public static Dictionary<string, bool> m_TrackableGears = new Dictionary<string, bool>();
        public static List<Expedition> m_ActiveExpeditions = new List<Expedition>();
        public static Dictionary<string, int> m_UnavailableGearSpawners = new Dictionary<string, int>();
        public static Dictionary<int, string> m_GearSpawnerGears = new Dictionary<int, string>();
        public static List<ExpeditionInvite> m_Invites = new List<ExpeditionInvite>();

        public static bool Debug = true;

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
            ENTERSCENE,
            ENTERZONE,
            COLLECT,
            FLAREGUNSHOT,
            CHARCOAL,
            STAYINZONE,
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

        public static void StartNewExpedition(string LeaderMAC, int Region, string Alias = "")
        {
            DebugLog("Client with MAC "+LeaderMAC+" trying start expedition on region "+Region);

            int LeaderID = Server.GetIDByMAC(LeaderMAC);

            for (int i = 0; i < m_ActiveExpeditions.Count; i++)
            {
                if (m_ActiveExpeditions[i].m_Players.Contains(LeaderMAC))
                {
                    ServerSend.ADDHUDMESSAGE(LeaderID, "You are already in expedition!");
                    return;
                }
            }

            if((Shared.GameRegion)Region == Shared.GameRegion.RandomRegion)
            {
                ServerSend.ADDHUDMESSAGE(LeaderID, "You can't start expedition here, try to move to another region.");
                return;
            }

            Expedition Exp = BuildBasicExpedition(Region, Alias);

            if(Exp == null)
            {
                ServerSend.ADDHUDMESSAGE(LeaderID, "No available expeditions, try to move to another region.");
                return;
            }

            DebugLog("Expedition created");
            DebugLog("Expedition Tasks:");
            for (int i = 0; i < Exp.m_Tasks.Count; i++)
            {
                DebugLog("Expedition Task["+i+"] " + Exp.m_Tasks[i].m_Alias+" Type " + Exp.m_Tasks[i].m_Type.ToString());
            }
            
            Exp.m_Players.Add(LeaderMAC);
            Exp.m_GUID = MPSaveManager.GetNewUGUID();
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
            List<int> PlayersIDs = m_ActiveExpeditions[RemoveID].GetExpeditionPlayersIDs();
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
                            MPStats.AddExpedition(MAC);
                        }
                    }
                }
                m_ActiveExpeditions.RemoveAt(RemoveID);
            }
        }

        public static void UpdateExpeditions()
        {
            //DebugLog("UpdateExpeditions() m_ActiveExpeditions.Count "+ m_ActiveExpeditions.Count);
            for (int i = m_ActiveExpeditions.Count - 1; i > -1; i--)
            {
                m_ActiveExpeditions[i].UpdateTasks();
                if (m_ActiveExpeditions[i].m_Completed)
                {
                    CompleteExpedition(i);
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

            public List<DataStr.MultiPlayerClientData> GetExpeditionPlayersData()
            {
                List<DataStr.MultiPlayerClientData> Data = new List<DataStr.MultiPlayerClientData>();
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
            public List<int> GetExpeditionPlayersIDs()
            {
                List<int> IDs = new List<int>();
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

            public void UpdateTasks()
            {
                int NeedToComplete = m_Tasks.Count;
                int Completed = 0;
                m_TimeLeft--;
                if(m_TimeLeft <= 0)
                {
                    CompleteExpedition(m_GUID, 0);
                }

                List<DataStr.MultiPlayerClientData> PlayersData = GetExpeditionPlayersData();
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
                            ProgressBar = "Progress: [[00FF00]";
                            int Stages = Procent / 20;
                            for (int i2 = 1; i2 <= 20; i2++)
                            {
                                if (Stages > i2)
                                {
                                    ProgressBar += "|";
                                } else
                                {
                                    ProgressBar += " ";
                                }
                            }
                            ProgressBar += ColorAffix + "] " + +Procent + "%";
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
#endif
                    } else
                    {
                        ServerSend.EXPEDITIONSYNC(Client, m_Name, Text, m_TimeLeft);
                    }
                }

                
                //DebugLog("UpdateTasks() " + Completed + "/" + NeedToComplete);

                if (NeedToComplete == Completed)
                {
                    m_Completed = true;
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
            public bool m_RestockSceneContainers = false;
            public bool m_IsComplete = false;
            public string m_ObjectiveGearSpawnerGUID = "";
            public bool m_ObjectiveGearSpawned = false;
            public bool m_Debug = false;
            public bool m_RewardAlreadySpawned = false;
            public Expedition m_Expedition = null;
            public int m_LastPlayersAmout = 1;
            public bool m_CanCheckFlaregun = false;
            public bool m_DidFlareShots = false;
            public bool m_CanCheckCharcoal = false;
            public int m_Time = 3600;
            public bool m_TimeAdd = true;
            public int m_SecondsInZone = 0;
            public int m_StayInZoneSeconds = 300;
            public void SpawnReward()
            {
                if (m_RewardAlreadySpawned)
                {
                    return;
                }
                DebugLog("Task "+ m_Alias + " SpawnReward");
                
                m_RewardAlreadySpawned = true;
                DebugLog("m_RestockSceneContainers "+ m_RestockSceneContainers);
                if (m_RestockSceneContainers)
                {
                    MPSaveManager.AddLootToScene(m_Scene);
                    DebugLog("Containers Restocked");
                }
                DebugLog("m_SpecificContrainers.Count " + m_SpecificContrainers.Count);
                if (m_SpecificContrainers.Count > 0)
                {
                    foreach (string ContainerGUID in m_SpecificContrainers)
                    {
                        MPSaveManager.AddLootToContainerOnScene(ContainerGUID, m_Scene);
                    }
                }
                DebugLog("m_GearSpawnerGears.Count " + m_GearSpawners.Count);
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
                DebugLog("m_SpecificPlants.Count " + m_SpecificPlants.Count);
                if (m_SpecificPlants.Count > 0)
                {
                    MPSaveManager.ForceGrowPlants(m_Scene, m_SpecificPlants);
                }
                DebugLog("m_SpecificBreakdowns.Count " + m_SpecificBreakdowns.Count);
                if (m_SpecificBreakdowns.Count > 0)
                {
                    MPSaveManager.RepairBreakdowns(m_Scene, m_SpecificBreakdowns);
                }
                DebugLog("m_ObjectSpawners.Count " + m_ObjectSpawners.Count);
                if (m_ObjectSpawners.Count > 0)
                {
                    foreach (UniversalSyncableObjectSpawner Spawner in m_ObjectSpawners)
                    {
                        UniversalSyncableObject Obj = new UniversalSyncableObject();
                        Obj.m_Prefab = Spawner.m_Prefab;
                        Obj.m_GUID = Spawner.m_GUID;
                        Obj.m_Position = Spawner.m_Position;
                        Obj.m_Rotation = Spawner.m_Rotation;

                        MPSaveManager.RemoveContainer(m_Scene, Spawner.m_GUID);
                        if (!string.IsNullOrEmpty(Spawner.m_Content))
                        {
                            MPSaveManager.SaveContainer(m_Scene, Spawner.m_GUID, Spawner.m_Content);
                            MPSaveManager.SetConstainerState(m_Scene, Spawner.m_GUID, 1);
                        } else
                        {
                            MPSaveManager.SetConstainerState(m_Scene, Spawner.m_GUID, 2);
                        }

                        Obj.m_ExpeditionBelong = m_Expedition.m_GUID;
                        Obj.m_Scene = m_Scene;
                        Obj.m_CreationTime = MyMod.MinutesFromStartServer;
                        Obj.m_RemoveTime = MyMod.MinutesFromStartServer + 1440;

                        MPSaveManager.AddUniversalSyncableObject(Obj);
#if (!DEDICATED)
                        MyMod.SpawnUniversalSyncableObject(Obj);
#endif
                        ServerSend.ADDUNIVERSALSYNCABLE(Obj);
                    }
                }
            }

            public int GetCompleteProcent()
            {
                return (int)(0.5f + ((100f * m_SecondsInZone) / m_StayInZoneSeconds));
            }

            public void OnCompleted()
            {
                if (m_IsComplete)
                {
                    return;
                }
                DebugLog("Task " + m_Alias + " OnCompleted");

                m_IsComplete = true;

                m_Expedition.OnTaskCompleted(this);

                SpawnReward();
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
            public void Update(List<DataStr.MultiPlayerClientData> PlayersData)
            {
                m_LastPlayersAmout = PlayersData.Count;

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
                } else{
                    foreach (DataStr.MultiPlayerClientData PlayerData in PlayersData)
                    {
                        if (m_Type == ExpeditionTaskType.ENTERSCENE)
                        {
                            if (PlayerData.m_LevelGuid == m_Scene)
                            {
                                OnCompleted();
                                return;
                            }
                        } else if (m_Type == ExpeditionTaskType.ENTERZONE)
                        {
                            if (PlayerData.m_LevelGuid == m_Scene && Vector3.Distance(PlayerData.m_Position, m_ZoneCenter) <= m_ZoneRadius)
                            {
                                OnCompleted();
                                return;
                            }
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
    }
}

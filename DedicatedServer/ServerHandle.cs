﻿using System;
using System.Collections.Generic;
using System.Text;
using SkyCoop;
#if (!DEDICATED)
using MelonLoader;
using UnityEngine;
#else
using System.Numerics;
#endif

namespace GameServer
{
    class ServerHandle
    {
        public static void Log(string TXT)
        {
#if (!DEDICATED)
            MelonLogger.Msg(TXT);
#else
            Console.WriteLine(TXT);
#endif
        }
        
        public static void WelcomeReceived(int _fromClient, Packet _packet)
        {
            int _clientIdCheck = _packet.ReadInt();
            string _username = _packet.ReadString();
            string ModVersion = "";
            if (_packet.Length() > 1)
            {
                ModVersion = _packet.ReadString();
            }

            if (_fromClient != _clientIdCheck)
            {
                Log($"Player \"{_username}\" (ID: {_fromClient}) has assumed the wrong client ID ({_clientIdCheck})!");
                return;
            }

            if(ModVersion != "RCON")
            {
                if (ModVersion != MyMod.BuildInfo.Version)
                {
                    ServerSend.KICKMESSAGE(_fromClient, "Wrong mod version! Server using version " + MyMod.BuildInfo.Version);
                    DataStr.MultiplayerChatMessage DisconnectMessage = new DataStr.MultiplayerChatMessage();
                    DisconnectMessage.m_Type = 0;
                    DisconnectMessage.m_By = _username;
                    DisconnectMessage.m_Message = _username + " can't join because has different mod version!";
                    Shared.SendMessageToChat(DisconnectMessage, true);
                    Log("Client " + _fromClient + " has different version! Processing disconnect!");
                    Server.clients[_fromClient].udp.Disconnect();
                    return;
                }
            }else{
                Log("RCON request from client " + _fromClient);

                string RCONpass = _packet.ReadString();
                if(RCONpass == MyMod.RCON)
                {
                    Log("Correct RCON password, client registered as operator.");
                    Server.clients[_fromClient].RCON = true;
                    ServerSend.RCONCONNECTED(_fromClient);
                }else{
                    Log("Incorrect RCON password, client kicked.");
                    ServerSend.KICKMESSAGE(_fromClient, "Wrong RCON password!");
                    Server.clients[_fromClient].udp.Disconnect();
                }

                return;
            }

            string SupporterID = _packet.ReadString();
            Supporters.SupporterBenefits ConfiguratedBenefits = _packet.ReadSupporterBenefits();
            MyMod.playersData[_fromClient].m_SupporterBenefits = Supporters.VerifyBenefitsWithConfig(SupporterID, ConfiguratedBenefits);
#if (!DEDICATED)
            Supporters.ApplyFlairsForModel(_fromClient, MyMod.playersData[_fromClient].m_SupporterBenefits.m_Flairs);
#endif

            int ClientModsHash = _packet.ReadInt();

            if (MyMod.ServerConfig.m_CheckModsValidation)
            {
                ModsValidation.ModValidationData ValiData = ModsValidation.GetModsHash();

                if (ValiData.m_Hash != ClientModsHash)
                {
                    ServerSend.MODSLIST(_fromClient, ValiData.m_FullStringBase64);
                    ServerSend.KICKMESSAGE(_fromClient, "Wrong mods installed! You have that different from server's mods, or you missing some mods that installed on the server, check logs!");
                    DataStr.MultiplayerChatMessage DisconnectMessage = new DataStr.MultiplayerChatMessage();
                    DisconnectMessage.m_Type = 0;
                    DisconnectMessage.m_By = _username;
                    DisconnectMessage.m_Message = _username + " can't join because has diferent mods installed!";
                    Shared.SendMessageToChat(DisconnectMessage, true);
                    Log("Client " + _fromClient + " can't join because has diferent mods installed!");
                    Server.clients[_fromClient].udp.Disconnect();
                    return;
                }
            }

            ServerSend.GAMETIME(MyMod.OveridedTime);

            MyMod.playersData[_fromClient].m_Name = _username;

            Log("Client "+ _fromClient+" with user name "+ _username+" connected!");
            Log("Sending init data to new client...");

            
            ServerSend.SERVERCFG(_fromClient);
            ServerSend.GEARPICKUPLIST(_fromClient);
            ServerSend.FURNBROKENLIST(_fromClient);
            ServerSend.ROPELIST(_fromClient);
            ServerSend.LOOTEDCONTAINERLIST(_fromClient);
            ServerSend.LOOTEDHARVESTABLEALL(_fromClient);
            ServerSend.ALLSHELTERS(_fromClient);

#if (!DEDICATED)
            if (!MyMod.DedicatedServerAppMode)
            {
                Log("[Init data] Client 0 -> Client " + _fromClient + " Data from host player object");
                int character = (int)GameManager.GetPlayerManagerComponent().m_VoicePersona;

                ServerSend.SELECTEDCHARACTER(0, character, false, _fromClient);

                ServerSend.XYZ(0, GameManager.GetPlayerTransform().position, false, _fromClient);
                ServerSend.XYZW(0, GameManager.GetPlayerTransform().rotation, false, _fromClient);
                ServerSend.LEVELID(0, MyMod.levelid, false, _fromClient);
                ServerSend.LEVELGUID(0, MyMod.level_guid, false, _fromClient);
                ServerSend.LIGHTSOURCE(0, MyMod.MyLightSource, false, _fromClient);
                ServerSend.LIGHTSOURCENAME(0, MyMod.MyLightSourceName, false, _fromClient);
                ServerSend.BENEFITINIT(0, Supporters.ConfiguratedBenefits, _fromClient);

                DataStr.PlayerEquipmentData Edata = new DataStr.PlayerEquipmentData();
                Edata.m_HasAxe = MyMod.MyHasAxe;
                Edata.m_HasMedkit = MyMod.MyHasMedkit;
                Edata.m_HasRevolver = MyMod.MyHasRevolver;
                Edata.m_HasRifle = MyMod.MyHasRifle;
                Edata.m_Arrows = MyMod.MyArrows;
                ServerSend.EQUIPMENT(0, Edata, false, _fromClient);
                DataStr.PlayerClothingData Cdata = new DataStr.PlayerClothingData();
                Cdata.m_Hat = MyMod.MyHat;
                Cdata.m_Top = MyMod.MyTop;
                Cdata.m_Bottom = MyMod.MyBottom;
                ServerSend.CLOTH(0, Cdata, false, _fromClient);
            }
#endif

            for (int i = 1; i <= Server.MaxPlayers; i++)
            {
                if (Server.clients[i].IsBusy() == true)
                {
                    if (MyMod.playersData[i] != null && i != _fromClient)
                    {
                        int PlayerIndex = i;
                        Log("[Init data] Client " + i + " -> Client " + _fromClient + " Data from playersData[" + PlayerIndex + "]");
                        int _FromId = i;
                        int _ForId = _fromClient;
                        DataStr.MultiPlayerClientData pD = MyMod.playersData[i];

                        ServerSend.XYZ(_FromId, pD.m_Position, false, _ForId);
                        ServerSend.XYZW(_FromId, pD.m_Rotation, false, _ForId);
                        ServerSend.LEVELID(_FromId, pD.m_Levelid, false, _ForId);
                        ServerSend.LEVELGUID(_FromId, pD.m_LevelGuid, true);
                        ServerSend.LIGHTSOURCE(_FromId, pD.m_PlayerEquipmentData.m_LightSourceOn, false, _ForId);
                        ServerSend.LIGHTSOURCENAME(_FromId, pD.m_PlayerEquipmentData.m_HoldingItem, false, _ForId);
                        ServerSend.EQUIPMENT(_FromId, pD.m_PlayerEquipmentData, false, _ForId);
                        ServerSend.CLOTH(_FromId, pD.m_PlayerClothingData, false, _ForId);
                        ServerSend.SELECTEDCHARACTER(_FromId, pD.m_Character, false, _ForId);
                        ServerSend.BENEFITINIT(_FromId, pD.m_SupporterBenefits, _fromClient);
                    }
                }
            }

            Shared.SendSlotData(_fromClient);
#if (!DEDICATED)
            MyMod.NoHostResponceSeconds = 0;
            MyMod.NeedTryReconnect = false;
            MyMod.TryingReconnect = false;
#endif

            DataStr.MultiplayerChatMessage joinMessage = new DataStr.MultiplayerChatMessage();
            joinMessage.m_Type = 0;
            joinMessage.m_By = _username;
            joinMessage.m_Message = _username + " join the server";

            if (!MyMod.DedicatedServerAppMode)
            {
                Shared.SendMessageToChat(joinMessage, false);
            }

            ServerSend.CHAT(_fromClient, joinMessage, false);


            if (MyMod.CurrentCustomChalleng.m_Started)
            {
                ServerSend.CHALLENGEINIT(MyMod.CurrentChallengeRules.m_ID, MyMod.CurrentCustomChalleng.m_CurrentTask);
            }
        }
        public static void XYZ(int _fromClient, Packet _packet)
        {
            Vector3 V3 = _packet.ReadVector3();

#if (!DEDICATED)
     Vector3 NewV3 = new Vector3(V3.x, V3.y + 0.03f, V3.z);
#else
     Vector3 NewV3 = new Vector3(V3.X, V3.Y + 0.03f, V3.Z);
#endif




            if (MyMod.playersData[_fromClient] != null)
            {
                MyMod.playersData[_fromClient].m_Position = NewV3;


#if (!DEDICATED)

                if (!MyMod.DedicatedServerAppMode)
                {
                    if (MyMod.players[_fromClient] != null && MyMod.players[_fromClient].GetComponent<Comps.MultiplayerPlayer>() != null)
                    {
                        MyMod.LongActionCancleCauseMoved(MyMod.players[_fromClient].GetComponent<Comps.MultiplayerPlayer>());
                    }
                }
#endif
            }
            ServerSend.XYZ(_fromClient, NewV3, false);
        }
        public static void XYZDW(int _fromClient, Packet _packet)
        {
      
        }
        public static void XYZW(int _fromClient, Packet _packet)
        {
            Quaternion Rot = _packet.ReadQuaternion();

            if (MyMod.playersData[_fromClient] != null)
            {
                MyMod.playersData[_fromClient].m_Rotation = Rot;
            }

            ServerSend.XYZW(_fromClient, Rot, false);
        }
        public static void BLOCK(int _fromClient, Packet _packet)
        {
            Vector3 V3 = _packet.ReadVector3();

            if (MyMod.playersData[_fromClient] != null)
            {
                if (MyMod.playersData[_fromClient].m_Levelid == MyMod.levelid)
                {
#if (!DEDICATED)
                    GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    cube.transform.position = V3;
#endif
                }
            }
            ServerSend.BLOCK(_fromClient, V3, false);
        }
        public static void LEVELID(int _fromClient, Packet _packet)
        {
            int lel = _packet.ReadInt();

            if (MyMod.playersData[_fromClient] != null)
            {
                MyMod.playersData[_fromClient].m_Levelid = lel;
                if(MyMod.playersData[_fromClient].m_Levelid != lel)
                {
                    Log("Player " + _fromClient + "  transition to level " + lel);
                }
                if(lel == 0)
                {
                    Shared.AddLoadingClient(_fromClient);
                }
            }
            ServerSend.LEVELID(_fromClient, lel, false);
        }
        public static void LEVELGUID(int _fromClient, Packet _packet)
        {
            string lel = _packet.ReadString();
            
            if (MyMod.playersData[_fromClient] != null)
            {
                MyMod.playersData[_fromClient].m_LevelGuid = lel;
                if (MyMod.playersData[_fromClient].m_LevelGuid != lel)
                {
                    Log("Player " + _fromClient + "  transition to level with GUID " + lel);
                }
            }
            ServerSend.LEVELGUID(_fromClient, lel, false);
        }
        public static void GOTITEM(int _fromClient, Packet _packet)
        {
            DataStr.GearItemDataPacket got = _packet.ReadGearData();

#if (!DEDICATED)
            if (got.m_SendedTo == 0)
            {
                MyMod.GiveRecivedItem(got);
            }else{
                ServerSend.GOTITEM(got.m_SendedTo, got);
            }
#else
            ServerSend.GOTITEM(got.m_SendedTo, got);
#endif


        }
        public static void GOTITEMSLICE(int _fromClient, Packet _packet)
        {
            DataStr.SlicedJsonData got = _packet.ReadSlicedGear();

#if (!DEDICATED)
            if (got.m_SendTo == 0)
            {
                MyMod.AddSlicedJsonData(got);
            }else{
                ServerSend.GOTITEMSLICE(got.m_SendTo, got);
            }
#else
            ServerSend.GOTITEMSLICE(got.m_SendTo, got);
#endif



        }
        public static void GAMETIME(int _fromClient, Packet _packet)
        {

        }
        public static void LIGHTSOURCENAME(int _fromClient, Packet _packet)
        {
            string Item = _packet.ReadString();
            if (MyMod.playersData[_fromClient] != null)
            {
                MyMod.playersData[_fromClient].m_PlayerEquipmentData.m_HoldingItem = Item;
            }

            ServerSend.LIGHTSOURCENAME(_fromClient, Item, false);
        }
        public static void LIGHTSOURCE(int _fromClient, Packet _packet)
        {
            bool On = _packet.ReadBool();
            if (MyMod.playersData[_fromClient] != null)
            {
                MyMod.playersData[_fromClient].m_PlayerEquipmentData.m_LightSourceOn = On;
            }
            ServerSend.LIGHTSOURCE(_fromClient, On, false);
        }
        public static void ANIMSTATE(int _fromClient, Packet _packet)
        {
            string anim = _packet.ReadString();

            if (MyMod.playersData[_fromClient] != null)
            {
                MyMod.playersData[_fromClient].m_AnimState = anim;
            }

            ServerSend.ANIMSTATE(_fromClient, anim, false);
        }
        public static void REVIVE(int _fromClient, Packet _packet)
        {

        }
        public static void REVIVEDONE(int _fromClient, Packet _packet)
        {
            
        }
        public static void SLEEPHOURS(int _fromClient, Packet _packet)
        {
            if (MyMod.playersData[_fromClient] != null)
            {
                MyMod.playersData[_fromClient].m_SleepHours = _packet.ReadInt();
            }
        }
        public static void DARKWALKERREADY(int _fromClient, Packet _packet)
        {

        }
        public static void WARDISACTIVE(int _fromClient, Packet _packet)
        {

        }
        public static void REQUESTDWREADYSTATE(int _fromClient, Packet _packet)
        {

        }
        public static void DWCOUNTDOWN(int _fromClient, Packet _packet)
        {

        }
        public static void SHOOTSYNC(int _fromClient, Packet _packet)
        {
            DataStr.ShootSync shoot = _packet.ReadShoot();
#if (!DEDICATED)
            MyMod.DoShootSync(shoot, _fromClient);
#endif


            ServerSend.SHOOTSYNC(_fromClient, shoot, false);
        }
        public static void PIMPSKILL(int _fromClient, Packet _packet)
        {
#if (!DEDICATED)
            int SkillTypeId = _packet.ReadInt();

            if (SkillTypeId == 1)
            {
                GameManager.GetSkillsManager().IncrementPointsAndNotify(SkillType.Rifle, 1, SkillsManager.PointAssignmentMode.AssignOnlyInSandbox);
                Log("Got remote skill upgrade Rifle");
            }
            else if (SkillTypeId == 2)
            {
                GameManager.GetSkillsManager().IncrementPointsAndNotify(SkillType.Revolver, 1, SkillsManager.PointAssignmentMode.AssignOnlyInSandbox);
                Log("Got remote skill upgrade Revolver");
            }
#endif
        }
        public static void HARVESTINGANIMAL(int _fromClient, Packet _packet)
        {
            string GUID = _packet.ReadString();
            if (MyMod.playersData[_fromClient] != null)
            {
                MyMod.playersData[_fromClient].m_HarvestingAnimal = GUID;
            }
            ServerSend.HARVESTINGANIMAL(_fromClient, GUID, false);
        }
        public static void DONEHARVASTING(int _fromClient, Packet _packet)
        {
            DataStr.HarvestStats Harvey = _packet.ReadHarvest();
            Shared.OnAnimalCorpseChanged(Harvey.m_Guid, Harvey.m_Meat, Harvey.m_Guts, Harvey.m_Hide);
        }
        public static void ANIMALTEST(int _fromClient, Packet _packet)
        {
            DataStr.AnimalCompactData dat = _packet.ReadAnimalCompactData();
            DataStr.AnimalAnimsSync anim = _packet.ReadAnimalAnim();
            int _for = _packet.ReadInt();

#if (!DEDICATED)
            if (_for == 0)
            {
                MyMod.DoAnimalSync(dat, anim);
            }else{
                ServerSend.ANIMALTEST(_fromClient, dat, anim, _for);
            }
#else
            ServerSend.ANIMALTEST(_fromClient, dat, anim, _for);
#endif
        }
        public static void ANIMALSYNCTRIGG(int _fromClient, Packet _packet)
        {
            DataStr.AnimalTrigger got = _packet.ReadAnimalTrigger();

#if (!DEDICATED)

            MyMod.SetAnimalTriggers(got);
#endif
            ServerSend.ANIMALSYNCTRIGG(_fromClient, got, true);
        }
        public static void BULLETDAMAGE(int _fromClient, Packet _packet)
        {
            if (!MyMod.PVP)
            {
                ServerSend.DOORLOCKEDMSG(_fromClient, "You can't attack players on this server.");
                return;
            }
            
            float damage = _packet.ReadFloat();
            int BodyPart = _packet.ReadInt();
            int _for = _packet.ReadInt();
            bool Melee = _packet.ReadBool();
            string MeleeWeapon = "";
            if (Melee)
            {
                MeleeWeapon = _packet.ReadString();
            }

#if (!DEDICATED)
            if (_for == 0)
            {
                MyMod.DamageByBullet(damage, _fromClient, BodyPart, Melee, MeleeWeapon);
            }else{
                ServerSend.BULLETDAMAGE(_for, damage, BodyPart, _fromClient, Melee, MeleeWeapon);
            }
#else
            ServerSend.BULLETDAMAGE(_for, damage, BodyPart, _fromClient, Melee, MeleeWeapon);
#endif



        }
        public static void MULTISOUND(int _fromClient, Packet _packet)
        {
            string sound = _packet.ReadString();

#if (!DEDICATED)
            MyMod.PlayMultiplayer3dAduio(sound, _fromClient);
#endif



            ServerSend.MULTISOUND(_fromClient, sound, false);
        }
        public static void CONTAINEROPEN(int _fromClient, Packet _packet)
        {
            DataStr.ContainerOpenSync box = _packet.ReadContainer();

#if (!DEDICATED)
            MyMod.DoSyncContainer(box);
#endif

            ServerSend.CONTAINEROPEN(_fromClient, box, false);
        }
        public static void LUREPLACEMENT(int _fromClient, Packet _packet)
        {

        }
        public static void LUREISACTIVE(int _fromClient, Packet _packet)
        {

        }
        public static void ALIGNANIMAL(int _fromClient, Packet _packet)
        {

        }
        public static void ASKFORANIMALPROXY(int _fromClient, Packet _packet)
        {

        }
        public static void CARRYBODY(int _fromClient, Packet _packet)
        {
        }
        public static void BODYWARP(int _fromClient, Packet _packet)
        {

        }
        public static void ANIMALDELETE(int _fromClient, Packet _packet)
        {
            string AnimalGuid = _packet.ReadString();
#if (!DEDICATED)
            MyMod.DeleteAnimal(AnimalGuid);
#endif
            ServerSend.ANIMALDELETE(_fromClient, AnimalGuid);
        }
        public static void KEEPITALIVE(int _fromClient, Packet _packet)
        {
            MyMod.NoHostResponceSeconds = 0;

            int Been = Server.clients[_fromClient].TimeOutTime;

            Server.clients[_fromClient].TimeOutTime = 0;

            if(Been > 10)
            {
                Log("Last request from client "+_fromClient+ " took longer than expected");
                Log("Client[" + _fromClient + "] KeepItAlive took "+ Been+"s");
            }
        }
        public static void SYNCWEATHER(int _fromClient, Packet _packet)
        {

        }
        public static void EQUIPMENT(int _fromClient, Packet _packet)
        {
            DataStr.PlayerEquipmentData item = _packet.ReadEQ();

            if (MyMod.playersData[_fromClient] != null)
            {
                MyMod.playersData[_fromClient].m_PlayerEquipmentData.m_Arrows = item.m_Arrows;
                MyMod.playersData[_fromClient].m_PlayerEquipmentData.m_HasAxe = item.m_HasAxe;
                MyMod.playersData[_fromClient].m_PlayerEquipmentData.m_HasMedkit = item.m_HasMedkit;
                MyMod.playersData[_fromClient].m_PlayerEquipmentData.m_HasRevolver = item.m_HasRevolver;
                MyMod.playersData[_fromClient].m_PlayerEquipmentData.m_HasRifle = item.m_HasRifle;
                MyMod.playersData[_fromClient].m_PlayerEquipmentData.m_Flares = item.m_Flares;
                MyMod.playersData[_fromClient].m_PlayerEquipmentData.m_BlueFlares = item.m_BlueFlares;
            }

            ServerSend.EQUIPMENT(_fromClient, item, false);
        }
        public static void CHAT(int _fromClient, Packet _packet)
        {
            DataStr.MultiplayerChatMessage message = _packet.ReadChat();
            Shared.SendMessageToChat(message, false);
            ServerSend.CHAT(_fromClient, message, false);
        }
        public static void CHANGENAME(int _fromClient, Packet _packet)
        {
            string newName = _packet.ReadString();

            DataStr.MultiplayerChatMessage message = new DataStr.MultiplayerChatMessage();
            message.m_Type = 0;
            message.m_By = MyMod.playersData[_fromClient].m_Name;
            message.m_Message = MyMod.playersData[_fromClient].m_Name + " changed name to "+ newName;

            MyMod.playersData[_fromClient].m_Name = newName;

            Shared.SendMessageToChat(message, true);
            ServerSend.CHANGENAME(_fromClient, newName, false);
        }

        public static void CLOTH(int _fromClient, Packet _packet)
        {
            DataStr.PlayerClothingData ClotchData = _packet.ReadClothingData();
            MyMod.playersData[_fromClient].m_PlayerClothingData = ClotchData;
            ServerSend.CLOTH(_fromClient, ClotchData, false);
        }

        public static void CONNECTSTEAM(int _fromClient, Packet _packet)
        {
            string sid = _packet.ReadString();
            int freeSlot = 0;

            bool ReConnection = false;

            for (int i = 1; i <= Server.MaxPlayers; i++)
            {
                if (Server.clients[i].udp != null && Server.clients[i].udp.sid == sid)
                {
                    ReConnection = true;
                    Log("[SteamWorks.NET] Reconnecting " + sid + " as client " + i);
                    Server.clients[i].TimeOutTime = 0;
                    ServerSend.Welcome(freeSlot, Server.MaxPlayers);
                    freeSlot = i;
                    break;
                }
            }

            if(ReConnection == false)
            {
                for (int i = 1; i <= Server.MaxPlayers; i++)
                {
                    if (Server.clients[i].IsBusy() == false)
                    {
                        Log("[SteamWorks.NET] Here an empty slot " + i + " for " + sid);
                        freeSlot = i;
                        Server.clients[i].udp.sid = sid;
                        ServerSend.Welcome(freeSlot, Server.MaxPlayers);
                        break;
                    }
                }
            }
        }
        public static void ASKSPAWNDATA(int _fromClient, Packet _packet)
        {
            int lvl = _packet.ReadInt();

#if (!DEDICATED)

            if(lvl == MyMod.levelid)
            {
                MyMod.SendSpawnData(false);
            }
#endif

            ServerSend.ASKSPAWNDATA(_fromClient, lvl, false);
        }
        public static void FURNBROKEN(int _fromClient, Packet _packet)
        {
            DataStr.BrokenFurnitureSync furn = _packet.ReadFurn();

#if (!DEDICATED)

            MyMod.OnFurnitureDestroyed(furn.m_Guid, furn.m_ParentGuid, furn.m_LevelID, furn.m_LevelGUID, false);
#endif

            ServerSend.FURNBROKEN(_fromClient, furn, false);
        }
        public static void FURNBREAKINGGUID(int _fromClient, Packet _packet)
        {
            DataStr.BrokenFurnitureSync furn = _packet.ReadFurn();

            if(MyMod.playersData[_fromClient] != null)
            {
                MyMod.playersData[_fromClient].m_BrakingObject = furn;

#if (!DEDICATED)

                if(MyMod.playersData[_fromClient].m_Levelid == MyMod.levelid && MyMod.playersData[_fromClient].m_LevelGuid == MyMod.level_guid)
                {
                    MyMod.playersData[_fromClient].m_BrakingSounds = MyMod.GetBreakDownSound(furn);
                }
#endif
            }

            ServerSend.FURNBREAKINGGUID(_fromClient, furn, false);
        }
        public static void FURNBREAKINSTOP(int _fromClient, Packet _packet)
        {
            bool broken = _packet.ReadBool();

            if (MyMod.playersData[_fromClient] != null)
            {
                MyMod.playersData[_fromClient].m_BrakingObject = new DataStr.BrokenFurnitureSync();
                MyMod.playersData[_fromClient].m_BrakingSounds = "";
            }

            ServerSend.FURNBREAKINSTOP(_fromClient, true, false);
        }
        public static void GEARPICKUP(int _fromClient, Packet _packet)
        {
            DataStr.PickedGearSync gear = _packet.ReadPickedGear();

#if (!DEDICATED)


            if (MyMod.playersData[_fromClient] != null && MyMod.playersData[_fromClient].m_Levelid == MyMod.levelid && MyMod.playersData[_fromClient].m_LevelGuid == MyMod.level_guid)
            {
                if (!MyMod.DedicatedServerAppMode)
                {
                    if (MyMod.players[_fromClient] != null && MyMod.players[_fromClient].GetComponent<Comps.MultiplayerPlayerAnimator>() != null)
                    {
                        MyMod.players[_fromClient].GetComponent<Comps.MultiplayerPlayerAnimator>().Pickup();
                    }
                }
            }
#endif

            Shared.AddPickedGear(gear.m_Spawn, gear.m_LevelID, gear.m_LevelGUID, _fromClient, gear.m_MyInstanceID, false);

            ServerSend.GEARPICKUP(_fromClient, gear, false);
        }
        public static void ROPE(int _fromClient, Packet _packet)
        {
            DataStr.ClimbingRopeSync rope = _packet.ReadRope();

            Shared.AddDeployedRopes(rope.m_Position, rope.m_Deployed, rope.m_Snapped, rope.m_LevelID, rope.m_LevelGUID, false);

            ServerSend.ROPE(_fromClient, rope, false);
        }
        public static void CONSUME(int _fromClient, Packet _packet)
        {
            bool IsDrink = _packet.ReadBool();

#if (!DEDICATED)

            if (MyMod.playersData[_fromClient] != null && MyMod.playersData[_fromClient].m_Levelid == MyMod.levelid && MyMod.playersData[_fromClient].m_LevelGuid == MyMod.level_guid)
            {
                if (!MyMod.DedicatedServerAppMode)
                {
                    if (MyMod.players[_fromClient] != null && MyMod.players[_fromClient].GetComponent<Comps.MultiplayerPlayerAnimator>() != null)
                    {
                        MyMod.players[_fromClient].GetComponent<Comps.MultiplayerPlayerAnimator>().m_IsDrink = IsDrink;
                        MyMod.players[_fromClient].GetComponent<Comps.MultiplayerPlayerAnimator>().Consumption();
                    }
                }
            }
#endif

            ServerSend.CONSUME(_fromClient, IsDrink, false);
        }
        public static void STOPCONSUME(int _fromClient, Packet _packet)
        {
            string LastAnim = _packet.ReadString();
            
            if (MyMod.playersData[_fromClient] != null)
            {
#if (!DEDICATED)

                if (!MyMod.DedicatedServerAppMode)
                {
                    if (MyMod.playersData[_fromClient].m_Levelid == MyMod.levelid && MyMod.playersData[_fromClient].m_LevelGuid == MyMod.level_guid)
                    {
                        if (MyMod.players[_fromClient] != null && MyMod.players[_fromClient].GetComponent<Comps.MultiplayerPlayerAnimator>() != null)
                        {
                            MyMod.players[_fromClient].GetComponent<Comps.MultiplayerPlayerAnimator>().StopConsumption();
                        }
                    }
                }
#endif
                MyMod.playersData[_fromClient].m_AnimState = LastAnim;
            }
            ServerSend.STOPCONSUME(_fromClient, LastAnim, false);
        }
        public static void HEAVYBREATH(int _fromClient, Packet _packet)
        {
            bool IsHeavyBreath = _packet.ReadBool();

            if (MyMod.playersData[_fromClient] != null)
            {
                MyMod.playersData[_fromClient].m_HeavyBreath = IsHeavyBreath;
            }
            ServerSend.HEAVYBREATH(_fromClient, IsHeavyBreath, false);
        }
        public static void BLOODLOSTS(int _fromClient, Packet _packet)
        {
            int BloodCount = _packet.ReadInt();

            if (MyMod.playersData[_fromClient] != null)
            {
                MyMod.playersData[_fromClient].m_BloodLosts = BloodCount;
            }
            ServerSend.BLOODLOSTS(_fromClient, BloodCount, false);
        }
        public static void APPLYACTIONONPLAYER(int _fromClient, Packet _packet)
        {
            string ActionType = _packet.ReadString();
            int ForWho = _packet.ReadInt();

#if (!DEDICATED)
            if(ForWho == 0)
            {
                MyMod.OtherPlayerApplyActionOnMe(ActionType, _fromClient);
            }else{
                ServerSend.APPLYACTIONONPLAYER(_fromClient, ActionType, false, ForWho);
            }
#else
            ServerSend.APPLYACTIONONPLAYER(_fromClient, ActionType, false, ForWho);
#endif
        }
        public static void DONTMOVEWARNING(int _fromClient, Packet _packet)
        {
            bool ok = _packet.ReadBool();
            int ForWho = _packet.ReadInt();

#if (!DEDICATED)

            if(ForWho == 0)
            {
                if (MyMod.playersData[_fromClient] != null)
                {
                    HUDMessage.AddMessage("PLEASE DON'T MOVE, " + MyMod.playersData[_fromClient].m_Name + " IS TENDING YOU");
                    GameManager.GetVpFPSPlayer().Controller.Stop();
                }
            }else{
                ServerSend.DONTMOVEWARNING(_fromClient, true, false, ForWho);
            }
#else
            ServerSend.DONTMOVEWARNING(_fromClient, true, false, ForWho);
#endif
        }
        public static void INFECTIONSRISK(int _fromClient, Packet _packet)
        {
            bool InfRisk = _packet.ReadBool();

            if (MyMod.playersData[_fromClient] != null)
            {
                MyMod.playersData[_fromClient].m_NeedAntiseptic = InfRisk;
            }
            ServerSend.INFECTIONSRISK(_fromClient, InfRisk, false);
        }
        public static void CONTAINERINTERACT(int _fromClient, Packet _packet)
        {
            DataStr.ContainerOpenSync box = _packet.ReadContainer();

            if (MyMod.playersData[_fromClient] != null)
            {
                if(box.m_Guid == "NULL")
                {
                    MyMod.playersData[_fromClient].m_Container = null;
                }else{
                    MyMod.playersData[_fromClient].m_Container = box;
                }
            }
#if (!DEDICATED)
            if(box.m_Guid != "NULL")
            {
                Shared.AddLootedContainer(box, true, _fromClient);
                if (box.m_LevelID == MyMod.levelid && box.m_LevelGUID == MyMod.level_guid)
                {
                    MyMod.ApplyLootedContainers();
                }
            }
#endif
            ServerSend.CONTAINERINTERACT(_fromClient, box, false);
        }
        public static void HARVESTPLANT(int _fromClient, Packet _packet)
        {
            DataStr.HarvestableSyncData harveData = _packet.ReadHarvestablePlant();

            if (MyMod.playersData[_fromClient] != null)
            {
                if (harveData.m_State == "Start")
                {
                    MyMod.playersData[_fromClient].m_Plant = harveData.m_Guid;
                }else{
                    MyMod.playersData[_fromClient].m_Plant = "";
                }
            }
#if (!DEDICATED)
            if(harveData.m_State == "Done")
            {
                MyMod.AddHarvastedPlant(harveData.m_Guid, _fromClient);
            }
#endif
            ServerSend.HARVESTPLANT(_fromClient, harveData, false);
        }

        public static void SELECTEDCHARACTER(int _fromClient, Packet _packet)
        {
            int character = _packet.ReadInt();

            if (MyMod.playersData[_fromClient] != null)
            {
                MyMod.playersData[_fromClient].m_Character = character;
                if (character == 0)
                {
                    MyMod.playersData[_fromClient].m_Female = false;
                }
                if (character == 1)
                {
                    MyMod.playersData[_fromClient].m_Female = true;
                }
            }
            ServerSend.SELECTEDCHARACTER(_fromClient, character, false);
        }

        public static void ADDSHELTER(int _fromClient, Packet _packet)
        {
            DataStr.ShowShelterByOther shelter = _packet.ReadShelter();
            Shared.ShelterCreated(shelter.m_Position, shelter.m_Rotation, shelter.m_LevelID, shelter.m_LevelGUID, false);
            ServerSend.ADDSHELTER(_fromClient, shelter,  false);
        }

        public static void REMOVESHELTER(int _fromClient, Packet _packet)
        {
            DataStr.ShowShelterByOther shelter = _packet.ReadShelter();
            Shared.ShelterRemoved(shelter.m_Position, shelter.m_LevelID, shelter.m_LevelGUID, false);
            ServerSend.REMOVESHELTER(_fromClient, shelter, false);
        }
        public static void USESHELTER(int _fromClient, Packet _packet)
        {
            DataStr.ShowShelterByOther shelter = _packet.ReadShelter();
            if (MyMod.playersData[_fromClient] != null)
            {
                if (shelter.m_Position == new Vector3(0, 0, 0))
                {
                    MyMod.playersData[_fromClient].m_Shelter = null;
                }else{
                    MyMod.playersData[_fromClient].m_Shelter = shelter;
                }
            }
            ServerSend.USESHELTER(_fromClient, shelter, false);
        }
        public static void FIRE(int _fromClient, Packet _packet)
        {
            DataStr.FireSourcesSync FireSource = _packet.ReadFire();
#if (!DEDICATED)
            MyMod.MayAddFireSources(FireSource);
#endif
            ServerSend.FIRE(_fromClient, FireSource, false);
        }
        public static void FIREFUEL(int _fromClient, Packet _packet)
        {
            DataStr.FireSourcesSync FireSource = _packet.ReadFire();
#if (!DEDICATED)
            if (FireSource.m_LevelId == MyMod.levelid && FireSource.m_LevelGUID == MyMod.level_guid)
            {
                MyMod.AddOtherFuel(FireSource, FireSource.m_FuelName);
            }
#endif
            ServerSend.FIREFUEL(_fromClient, FireSource, false);
        }
        public static void CUSTOM(int _fromClient, Packet _packet)
        {
#if (!DEDICATED)
            API.CustomEventCallback(_packet, _fromClient);
#endif
        }
        public static void VOICECHAT(int _fromClient, Packet _packet)
        {
            int BytesWritten = _packet.ReadInt();
            int ReadLength = _packet.ReadInt();
            byte[] CompressedData = _packet.ReadBytes(ReadLength);
            float RecordTime = _packet.ReadFloat();
            int Sender = _packet.ReadInt();

            if(MyMod.playersData[_fromClient] != null)
            {
                string SourceLevel = MyMod.playersData[_fromClient].m_LevelGuid;
                float Radio = -66;

                if(_fromClient == Sender && MyMod.playersData[_fromClient].m_PlayerEquipmentData.m_HoldingItem == "GEAR_HandheldShortwave")
                {
                    Radio = MyMod.playersData[_fromClient].m_RadioFrequency;
                }

#if (!DEDICATED)

                if (SourceLevel == MyMod.level_guid)
                {
                    bool IsRadio = false;
                    if (_fromClient != Sender)
                    {
                        IsRadio = true;
                    }
                    MyMod.ProcessVoiceChatData(_fromClient, CompressedData, uint.Parse(BytesWritten.ToString()), RecordTime, IsRadio);
                }

                if(Radio == MyMod.RadioFrequency && !MyMod.DoingRecord)
                {
                    MyMod.ProcessRadioChatData(CompressedData, uint.Parse(BytesWritten.ToString()), RecordTime);
                    ServerSend.VOICECHAT(0, CompressedData, BytesWritten, RecordTime, MyMod.level_guid, -66, Sender);
                }
#endif
                ServerSend.VOICECHAT(_fromClient, CompressedData, BytesWritten, RecordTime, SourceLevel, Radio, Sender);
            }
        }
        public static void SLICEDBYTES(int _fromClient, Packet _packet)
        {
        }
        public static void ANIMALDAMAGE(int _fromClient, Packet _packet)
        {
            string guid = _packet.ReadString();
            float damage = _packet.ReadFloat();
#if (!DEDICATED)
            MyMod.DoAnimalDamage(guid, damage);
#endif

            ServerSend.ANIMALDAMAGE(_fromClient, guid, damage);
        }
        public static void DROPITEM(int _fromClient, Packet _packet)
        {
            DataStr.DroppedGearItemDataPacket GearData = _packet.ReadDroppedGearData();
#if (!DEDICATED)
            MyMod.FakeDropItem(GearData);
#endif
            ServerSend.DROPITEM(_fromClient, GearData, true);
        }
        public static void GOTDROPSLICE(int _fromClient, Packet _packet)
        {
            DataStr.SlicedJsonData got = _packet.ReadSlicedGear();
            Shared.AddSlicedJsonDataForDrop(got);
        }
        public static void REQUESTPICKUP(int _fromClient, Packet _packet)
        {
            int Hash = _packet.ReadInt();
            string Scene = _packet.ReadString();
            Log("Client "+ _fromClient+" trying to pickup gear with hash "+ Hash);
            Shared.ClientTryPickupItem(Hash, _fromClient, Scene, false);
        }
        public static void REQUESTPLACE(int _fromClient, Packet _packet)
        {
            int Hash = _packet.ReadInt();
            string lvlKey = _packet.ReadString();
            Log("Client " + _fromClient + " trying to place gear with hash " + Hash);
            Shared.ClientTryPickupItem(Hash, _fromClient, lvlKey, true);
        }

        public static void SendAllOpenables(int _fromClient, string scene)
        {
            Dictionary<string, bool> Opens = MPSaveManager.LoadOpenableThings(scene);
            if(Opens == null)
            {
                return;
            }

            foreach (var cur in Opens)
            {
                string currentKey = cur.Key;
                bool currentValue = cur.Value;
                ServerSend.USEOPENABLE(_fromClient, currentKey, currentValue);
            }
        }

        public static void SendAnimalCorpse(DataStr.AnimalKilled Animal, int forWho = -1)
        {
            if(forWho != -1)
            {
                ServerSend.ANIMALCORPSE(forWho, Animal);
            }else{
                ServerSend.ANIMALCORPSE(forWho, Animal, true);
            }
        }

        public static void RequestAnimalCorpses(int _fromClient, string LevelGUID)
        {
            List<string> ToRemove = new List<string>();

            foreach (var item in Shared.AnimalsKilled)
            {
                int DespawnTime = item.Value.m_CreatedTime+14400;
                if (DespawnTime < MyMod.MinutesFromStartServer)
                {
                    ToRemove.Add(item.Key);
                }else{
                    if(item.Value.m_LevelGUID == LevelGUID)
                    {
                        SendAnimalCorpse(item.Value, _fromClient);
                    }
                }
            }
            foreach (var item in ToRemove)
            {
                Shared.AnimalsKilled.Remove(item);
            }
        }

        public static void REQUESTDROPSFORSCENE(int _fromClient, Packet _packet)
        {
            int lvl = _packet.ReadInt();
            string Scene = _packet.ReadString();
            Log("Client "+ _fromClient+" request all drops for scene "+ Scene);
            if(MyMod.playersData[_fromClient] != null)
            {
                MyMod.playersData[_fromClient].m_Levelid = lvl;
                MyMod.playersData[_fromClient].m_LevelGuid = Scene;
            }
            SendAllOpenables(_fromClient, Scene);
            RequestAnimalCorpses(_fromClient, Scene);


#if (!DEDICATED)
            if(MyMod.CurrentCustomChalleng.m_Started && MyMod.CurrentChallengeRules.m_Name == "Lost in action")
            {
                ServerSend.CAIRNS(_fromClient);
            }
#endif

            foreach (DataStr.DeathContainerData create in MyMod.DeathCreates)
            {
                if(create.m_LevelKey == Scene)
                {
                    ServerSend.ADDDEATHCONTAINER(create, _fromClient);
                }
            }
            Dictionary<string, string> Doors = MPSaveManager.GetDoorsOnScene(Scene);
            foreach (var item in Doors)
            {
                string GUID = item.Key.Split('_')[1];
                ServerSend.ADDDOORLOCK(_fromClient, GUID, Scene);
            }

            Shared.ModifyDynamicGears(Scene);
            Dictionary<int, DataStr.DroppedGearItemDataPacket> Visuals = MPSaveManager.LoadDropVisual(Scene);
            Dictionary<int, DataStr.SlicedJsonDroppedGear> Drops = MPSaveManager.LoadDropData(Scene);


            if(Drops == null || Visuals == null)
            {
                Log("Requested scene has no drops " + Scene);
                ServerSend.LOADINGSCENEDROPSDONE(_fromClient, true);
            }else{
                int index = 0;
                foreach (var cur in Visuals)
                {
                    index++;
                    ServerSend.DROPITEM(0, cur.Value, false, _fromClient);
                }
                Log("Sending done, "+ index+" packets has been sent");                
                
                ServerSend.LOADINGSCENEDROPSDONE(_fromClient, true);
            }

            Shared.RemoveLoadingClient(_fromClient);
        }
        public static void GOTCONTAINERSLICE(int _fromClient, Packet _packet)
        {
            DataStr.SlicedJsonData got = _packet.ReadSlicedGear();
            Shared.AddSlicedJsonDataForContainer(got, _fromClient);
        }
        public static void REQUESTOPENCONTAINER(int _fromClient, Packet _packet)
        {
            string Scene = _packet.ReadString();
            string boxGUID = _packet.ReadString();
            Log("Client " + _fromClient + " request container data for " + boxGUID);
            string CompressedData = MPSaveManager.LoadContainer(Scene, boxGUID);

            if (CompressedData == "")
            {
                Log("Send to client this is empty");
                ServerSend.OPENEMPTYCONTAINER(_fromClient, true);
            }else{
                Log("Send to client data about container");
                Shared.SendContainerData(CompressedData, Scene, boxGUID, _fromClient);
            }
        }
        public static void CHANGEAIM(int _fromClient, Packet _packet)
        {
            bool IsAiming = _packet.ReadBool();
            if (MyMod.playersData[_fromClient] != null)
            {
                MyMod.playersData[_fromClient].m_Aiming = IsAiming;
            }
            ServerSend.CHANGEAIM(_fromClient, IsAiming, false);
        }
        public static void USEOPENABLE(int _fromClient, Packet _packet)
        {
            string Scene = _packet.ReadString();
            string _GUID = _packet.ReadString();
            bool state = _packet.ReadBool();
            Shared.ChangeOpenableThingState(Scene, _GUID, state);
        }
        public static void TRYDIAGNISISPLAYER(int _fromClient, Packet _packet)
        {
            int ForWho = _packet.ReadInt();
#if (!DEDICATED)
            if (ForWho == 0)
            {
                MyMod.SendMyAffictions(_fromClient, GameManager.GetConditionComponent().m_CurrentHP);
            }else{
                ServerSend.TRYDIAGNISISPLAYER(ForWho, _fromClient);
            }
#else
            ServerSend.TRYDIAGNISISPLAYER(ForWho, _fromClient);
#endif
        }
        public static void CUREAFFLICTION(int _fromClient, Packet _packet)
        {
            DataStr.AffictionSync toCure = _packet.ReadAffiction();
            int ForWho = _packet.ReadInt();

#if (!DEDICATED)
            if (ForWho == 0)
            {
                MyMod.OtherPlayerCuredMyAffiction(toCure);
            }else{
                ServerSend.CUREAFFLICTION(ForWho, toCure);
            }
#else
            ServerSend.CUREAFFLICTION(ForWho, toCure);
#endif
        }
        public static void ANIMALKILLED(int _fromClient, Packet _packet)
        {
            DataStr.AnimalKilled Data = _packet.ReadAnimalCorpse();
            Shared.OnAnimalKilled(Data.m_PrefabName, Data.m_Position, Data.m_Rotation, Data.m_GUID, Data.m_LevelGUID, Data.m_RegionGUID, Data.m_Knocked);

            ServerSend.ANIMALCORPSE(0, Data, true);
        }
        public static void PICKUPRABBIT(int _fromClient, Packet _packet)
        {
            string GUID = _packet.ReadString();
            int result = Shared.PickUpRabbit(GUID);
            ServerSend.GOTRABBIT(_fromClient, result);
        }
        public static void HITRABBIT(int _fromClient, Packet _packet)
        {
            string GUID = _packet.ReadString();
            int For = _packet.ReadInt();
#if (!DEDICATED)
            if (For != 0)
            {
                ServerSend.HITRABBIT(For, GUID);
            }else{
                MyMod.OnHitRabbit(GUID);
            }
#else
            ServerSend.HITRABBIT(For, GUID);
#endif

        }
        public static void RELEASERABBIT(int _fromClient, Packet _packet)
        {
            string levelGUID = _packet.ReadString();
#if (!DEDICATED)
            MyMod.OnReleaseRabbit(_fromClient);
#endif
            ServerSend.RELEASERABBIT(_fromClient, levelGUID);
        }
        public static void SENDMYAFFLCTIONS(int _fromClient, Packet _packet)
        {
            int forWho = _packet.ReadInt();
            int Count = _packet.ReadInt();
            float hp = _packet.ReadFloat();
            List<DataStr.AffictionSync> Affs = new List<DataStr.AffictionSync>();

            for (int index = 0; index < Count; ++index)
            {
                DataStr.AffictionSync newElement = _packet.ReadAffiction();
                Affs.Add(newElement);
            }

            Log("Client " + _fromClient + " sent " + Count + " afflictions, for "+ forWho);

#if (!DEDICATED)
            if (forWho == 0)
            {
                MyMod.CheckOtherPlayer(Affs, _fromClient, hp);
            } else
            {
                ServerSend.SENDMYAFFLCTIONS(forWho, Affs, hp, _fromClient);
            }
#else
            ServerSend.SENDMYAFFLCTIONS(forWho, Affs, hp, _fromClient);
#endif

        }
        public static void REQUESTANIMALCORPSE(int _fromClient, Packet _packet)
        {
            string GUID = _packet.ReadString();
            Log("Client "+_fromClient+" requested animal corpse "+GUID);
            DataStr.AnimalKilled Animal;
            if (Shared.AnimalsKilled.TryGetValue(GUID, out Animal))
            {
                float Meat = Animal.m_Meat;
                int Guts = Animal.m_Guts;
                int Hide = Animal.m_Hide;
                Log("Sending responce");
                ServerSend.REQUESTANIMALCORPSE(_fromClient, Meat, Guts, Hide);
            }else{
                ServerSend.REQUESTANIMALCORPSE(_fromClient, -1, 0, 0);
            }
        }
        public static void QUARTERANIMAL(int _fromClient, Packet _packet)
        {
            string GUID = _packet.ReadString();
            Log("QUARTERANIMAL " + GUID);
            Shared.OnAnimalQuarted(GUID);
#if (!DEDICATED)

            MyMod.SpawnQuartedMess(GUID);
#endif
            ServerSend.QUARTERANIMAL(_fromClient, GUID, false);
        }
        public static void ANIMALAUDIO(int _fromClient, Packet _packet)
        {
            bool InInt = _packet.ReadBool();
            string GUID = _packet.ReadString();
            if (InInt)
            {
                int soundID = _packet.ReadInt();
                string SenderLevelGUID = "";
                if (MyMod.playersData[_fromClient] != null)
                {
                    SenderLevelGUID = MyMod.playersData[_fromClient].m_LevelGuid;
#if (!DEDICATED)
                    if (MyMod.level_guid == MyMod.playersData[_fromClient].m_LevelGuid)
                    {
                        Pathes.Play3dAudioOnAnimal(GUID, soundID);
                    }
#endif
                }
                ServerSend.ANIMALAUDIO(_fromClient, soundID, GUID, SenderLevelGUID);
            }else{
                string soundID = _packet.ReadString();
                string SenderLevelGUID = "";
                if (MyMod.playersData[_fromClient] != null)
                {
                    SenderLevelGUID = MyMod.playersData[_fromClient].m_LevelGuid;
#if (!DEDICATED)
                    if (MyMod.level_guid == MyMod.playersData[_fromClient].m_LevelGuid)
                    {
                        Pathes.Play3dAudioOnAnimal(GUID, soundID);
                    }
#endif
                }
                ServerSend.ANIMALAUDIO(_fromClient, soundID, GUID, SenderLevelGUID);
            }
        }
        public static void CHANGEDFREQUENCY(int _fromClient, Packet _packet)
        {
            float FQ = _packet.ReadFloat();
            double FixedFloat = System.Math.Round((double)FQ * 10.0f) * 0.1f;


            if (MyMod.playersData[_fromClient] != null)
            {
                MyMod.playersData[_fromClient].m_RadioFrequency = (float)FixedFloat;
            }
        }
        public static void MELEESTART(int _fromClient, Packet _packet)
        {
#if (!DEDICATED)
            if (MyMod.players[_fromClient] != null && MyMod.players[_fromClient].GetComponent<Comps.MultiplayerPlayerAnimator>() != null)
            {
                MyMod.players[_fromClient].GetComponent<Comps.MultiplayerPlayerAnimator>().MeleeAttack();
            }
#endif
            ServerSend.MELEESTART(_fromClient);
        }
        public static void TRYBORROWGEAR(int _fromClient, Packet _packet)
        {

        }
        public static void CHALLENGETRIGGER(int _fromClient, Packet _packet)
        {
            string TRIGGER = _packet.ReadString();
#if (!DEDICATED)
            MyMod.ProcessCustomChallengeTrigger(TRIGGER);
#endif
            ServerSend.CHALLENGETRIGGER(TRIGGER);
        }
        public static void ADDDEATHCONTAINER(int _fromClient, Packet _packet)
        {
            DataStr.DeathContainerData Con = _packet.ReadDeathContainer();
            if (!MyMod.DeathCreates.Contains(Con))
            {
                MyMod.DeathCreates.Add(Con);
            }
#if (!DEDICATED)
            if (Con.m_LevelKey == MyMod.level_guid)
            {
                MyMod.MakeDeathCreate(Con);
            }
#endif

            ServerSend.ADDDEATHCONTAINER(Con, Con.m_LevelKey, _fromClient);
        }
        public static void DEATHCREATEEMPTYNOW(int _fromClient, Packet _packet)
        {
            string GUID = _packet.ReadString();
            string Scene = _packet.ReadString();
#if (!DEDICATED)
            MyMod.RemoveDeathContainer(GUID, Scene);
#endif
            ServerSend.DEATHCREATEEMPTYNOW(GUID, Scene, _fromClient);
            MPSaveManager.RemoveContainer(Scene, GUID);
        }
        public static void SPAWNREGIONBANCHECK(int _fromClient, Packet _packet)
        {
            string GUID = _packet.ReadString();
            bool Result = Shared.CheckSpawnRegionBanned(GUID);
            ServerSend.SPAWNREGIONBANCHECK(GUID, Result, _fromClient);
        }
        public static void ADDDOORLOCK(int _fromClient, Packet _packet)
        {
            string DoorKey = _packet.ReadString();
            string KeySeed = _packet.ReadString();
            string Scene = _packet.ReadString();
            Shared.ClientTryingLockDoor(DoorKey, KeySeed, Scene, _fromClient);
        }
        public static void LOCKPICK(int _fromClient, Packet _packet)
        {
            string DoorKey = _packet.ReadString();
            string Scene = _packet.ReadString();
            MPSaveManager.TryLockPick(Scene, DoorKey, _fromClient);
        }
        public static void TRYOPENDOOR(int _fromClient, Packet _packet)
        {
            string DoorKey = _packet.ReadString();
            string KeySeed = _packet.ReadString();
            string Scene = _packet.ReadString();
            bool Correct = MPSaveManager.TryUseKey(Scene, DoorKey, KeySeed);

            if (Correct)
            {
                ServerSend.ENTERDOOR(_fromClient, DoorKey.Split('_')[1]);
            }else{
                ServerSend.DOORLOCKEDMSG(_fromClient, "Incorrect key!");
            }
        }
        public static void VERIFYSAVE(int _fromClient, Packet _packet)
        {
            string UGUID = _packet.ReadString();
            int SaveHash = _packet.ReadInt();

            Log("[VERIFYSAVE] Client "+ _fromClient+" UGUID "+ UGUID+" Hash "+ SaveHash);

            if (MyMod.ServerConfig.m_SaveScamProtection)
            {
                if(UGUID == "" && SaveHash == 0)
                {
                    ServerSend.VERIFYSAVE(_fromClient, MPSaveManager.GetNewUGUID(), false);
                }else{
                    if (MPSaveManager.VerifySaveHash(UGUID, SaveHash))
                    {
                        ServerSend.VERIFYSAVE(_fromClient, UGUID, true);
                    }else{
                        ServerSend.VERIFYSAVE(_fromClient, MPSaveManager.GetNewUGUID(), false);
                    }
                }
            }else{
                ServerSend.VERIFYSAVE(_fromClient, UGUID, true);
            }
        }
        public static void SAVEHASH(int _fromClient, Packet _packet)
        {
            string UGUID = _packet.ReadString();
            int SaveHash = _packet.ReadInt();
            bool DisconnectMe = _packet.ReadBool();

            if (MyMod.ServerConfig.m_SaveScamProtection)
            {
                Log("[SAVEHASH] " + _fromClient + " UGUID " + UGUID + " Hash " + SaveHash);
                if (UGUID != "" && SaveHash != 0)
                {
                    MPSaveManager.SetSaveHash(UGUID, SaveHash);
                }
            }
            if (DisconnectMe)
            {
                if (Server.clients[_fromClient] != null)
                {
                    Server.clients[_fromClient].TimeOutTime = MyMod.TimeOutSecondsForLoaders + 1;
                }
            }
        }

        public static void RCONCOMMAND(int _fromClient, Packet _packet)
        {
            if(Server.clients[_fromClient] != null && Server.clients[_fromClient].RCON)
            {
                Server.clients[_fromClient].TimeOutTime = 0;
                string CMD = _packet.ReadString();
                string Low = CMD.ToLower();
                Log("[RCON] Operator Execute: "+ CMD);
                if(Low == "disconnect" || Low == "exit" || Low == "quit")
                {
                    Server.clients[_fromClient].RCON = false;
                    Shared.ResetDataForSlot(_fromClient);
                    Log("RCON process disconnect");
                    Server.clients[_fromClient].udp.Disconnect();
                    return;
                }else if(Low == "trafficdebug" || Low == "traffic" || Low == "trafictrace" || Low == "trafficcheck")
                {
                    MyMod.DebugTrafficCheck = !MyMod.DebugTrafficCheck;
                    ServerSend.RCONCALLBACK(_fromClient, "DebugTrafficCheck = " + MyMod.DebugTrafficCheck);
                    return;
                }
                else if(Low == "savediag" || Low == "unloaddiag")
                {
                    MPSaveManager.Diagnostic = !MPSaveManager.Diagnostic;
                    ServerSend.RCONCALLBACK(_fromClient, "MPSaveManager.Diagnostic = "+ MPSaveManager.Diagnostic);
                    return;
                }
                else if (Low == "reset" || Low == "restart")
                {
                    ServerSend.RCONCALLBACK(_fromClient, "Server going to restart in 30 seconds!");
                    MyMod.RestartPerioud = 0;
                    MyMod.SecondsWithoutSaving = MyMod.DsSavePerioud;
                    return;
                }
                else if(Low == "shutdown")
                {
                    ServerSend.RCONCALLBACK(_fromClient, "Server will shutdown");
#if (DEDICATED)
                    Environment.Exit(0);
#else
                    Application.Quit();
#endif
                    return;
                }else if(Low == "players" || Low == "playerslist" || Low == "clients")
                {
                    string List = "Players:";
                    foreach (var c in Server.clients)
                    {
                        if (c.Value.IsBusy())
                        {
                            if (!c.Value.RCON)
                            {
                                List = List + "\n" + c.Key + ". " + MyMod.playersData[c.Key].m_Name;
                            }else{
                                List = List + "\n" + c.Key + ". RCON";
                            }
                        }
                    }
                    ServerSend.RCONCALLBACK(_fromClient, List);
                    return;
                }else if(Low.StartsWith("say "))
                {
                    string Message = CMD.Remove(0, 4);
                    ServerSend.RCONCALLBACK(_fromClient, "Message sent to chat");
                    DataStr.MultiplayerChatMessage MSG = new DataStr.MultiplayerChatMessage();
                    MSG.m_Type = 0;
                    MSG.m_By = "";
                    MSG.m_Message = "[SERVER] " + Message;
                    Shared.SendMessageToChat(MSG, true);
                    return;
                }else if(Low.StartsWith("skip "))
                {
                    int Skip = int.Parse(CMD.Split(' ')[1]);
                    Shared.SkipRTTime(Skip);
                    ServerSend.RCONCALLBACK(_fromClient, "Skipped "+Skip+" hour(s)");
                    return;
                }
                else if(Low == "skip")
                {
                    Shared.SkipRTTime(1);
                    ServerSend.RCONCALLBACK(_fromClient, "Skipped 1 hour");
                    return;
                }else if(Low.StartsWith("rpc "))
                {
                    string[] Things = CMD.Split(' ');
                    int Client = int.Parse(Things[1]);
                    string RPCDATA = "";
                    for (int i = 2; i < Things.Length; i++)
                    {
                        if(RPCDATA == "")
                        {
                            RPCDATA = Things[i];
                        }else{
                            RPCDATA = RPCDATA+ " "+ Things[i];
                        }
                    }

                    if(Client < MyMod.playersData.Count)
                    {
                        if(MyMod.playersData[Client] != null)
                        {
                            if (Server.clients[Client].IsBusy())
                            {
                                ServerSend.RPC(Client, RPCDATA);
                                ServerSend.RCONCALLBACK(_fromClient, "Sent "+ RPCDATA+" to client "+ Client);
                                return;
                            }
                        }
                    }
                    ServerSend.RCONCALLBACK(_fromClient, "There no Client " + Client);
                    return;
                }
#if (!DEDICATED)
                uConsole.RunCommand(CMD);
                string Responce = uConsoleLog.m_Log[uConsoleLog.m_Log.Count - 1];
                ServerSend.RCONCALLBACK(_fromClient, Responce);
#else
                ServerSend.RCONCALLBACK(_fromClient, "Unknown command");
#endif

            }
        }
        public static void FORCELOADING(int _fromClient, Packet _packet)
        {
            Shared.AddLoadingClient(_fromClient);
        }
        public static void REQUESTLOCKSMITH(int _fromClient, Packet _packet)
        {
            int Hash = _packet.ReadInt();
            if (MPSaveManager.CanWorkOnBlank(Hash))
            {
                ServerSend.REQUESTLOCKSMITH(_fromClient, 0);
                MPSaveManager.ChangeBlankState(Hash, -1);
            }else{
                ServerSend.REQUESTLOCKSMITH(_fromClient, -1);
            }
        }
        public static void APPLYTOOLONBLANK(int _fromClient, Packet _packet)
        {
            int Hash = _packet.ReadInt();
            int Tool = _packet.ReadInt();
            bool IsKey = _packet.ReadBool();

            if (!IsKey)
            {
                MPSaveManager.ApplyToolOnBlank(Hash, Tool);
            }else{
                string Name = _packet.ReadString();
                string Seed = _packet.ReadString();
                MPSaveManager.ApplyToolOnBlank(Hash, Tool, Name, Seed);
            }
        }
        public static void LETENTER(int _fromClient, Packet _packet)
        {
            int ClientID = _packet.ReadInt();
            string ToScene = _packet.ReadString();
            MPSaveManager.ApplyEnterFromKnock(ClientID, ToScene);
        }
        public static void KNOCKKNOCK(int _fromClient, Packet _packet)
        {
            string ToScene = _packet.ReadString();
            MPSaveManager.AddKnockDoorRequest(_fromClient, ToScene);
        }
        public static void PEEPHOLE(int _fromClient, Packet _packet)
        {
            string Scene = _packet.ReadString();
            List<int> Knockers = MPSaveManager.GetKnocksOnScene(Scene);
            ServerSend.PEEPHOLE(_fromClient, Knockers);
        }
        public static void RESTART(int _fromClient, Packet _packet)
        {
            Log("Incomming reconnect");
        }
    }
}

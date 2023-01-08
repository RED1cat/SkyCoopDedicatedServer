using Harmony;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static SkyCoop.DataStr;
#if (!DEDICATED)
using UnityEngine;
#else
using System.Numerics;
#endif

namespace SkyCoop
{
    public class SafeZoneManager
    {
        public static List<string> SafeScenes = new List<string>();
        public static Dictionary<string, List<SafeZoneSpace>> SafeZones = new Dictionary<string, List<SafeZoneSpace>>();

        public class SafeZoneSpace
        {
            public Vector3 m_Center = new Vector3(0,0,0);
            public float m_Radius = 1;

            public SafeZoneSpace(Vector3 center, float radius)
            {
                m_Center = center;
                m_Radius = radius;
            }
        }

        public static bool SceneIsSafe(string Scene)
        {
            return SafeScenes.Contains(Scene);
        }
        public static bool InsideSafeZone(string Scene, Vector3 Position)
        {
            List<SafeZoneSpace> Zones;
            if (SafeZones.TryGetValue(Scene, out Zones))
            {
                foreach (SafeZoneSpace Zone in Zones)
                {
                    if(Vector3.Distance(Position, Zone.m_Center) <= Zone.m_Radius)
                    {
                        return true;
                    }
                }
            }

            return false;
        }
        public static void UpdatePlayersSafeZoneStatus()
        {
            foreach (MultiPlayerClientData player in MyMod.playersData)
            {
                bool SafeStatus = false;
                if (SceneIsSafe(player.m_LevelGuid) || InsideSafeZone(player.m_LevelGuid, player.m_Position))
                {
                    SafeStatus = true;
                }

                if(player.m_IsSafe != SafeStatus)
                {
                    player.m_IsSafe = SafeStatus;
                    //TODO: Send UI Message
                }
            }
        }

        public static void AddSafeZone(string Scene, Vector3 Center, float Radius)
        {
            SafeZoneSpace NewZone = new SafeZoneSpace(Center, Radius);
            List<SafeZoneSpace> Zones;
            if (!SafeZones.ContainsKey(Scene))
            {
                Zones = new List<SafeZoneSpace>();
                Zones.Add(NewZone);
                SafeZones.Add(Scene, Zones);
                return;
            } else
            {
                if (SafeZones.TryGetValue(Scene, out Zones))
                {
                    Zones.Add(NewZone);
                    SafeZones.Remove(Scene);
                    SafeZones.Add(Scene, Zones);
                }
            }
        }
        public static void AddSafeScene(string Scene)
        {
            if(!SceneIsSafe(Scene))
            {
                SafeScenes.Add(Scene);
            }
        }
    }
}

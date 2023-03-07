using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using TinyJSON;

namespace SkyCoop
{
    public class ModsValidation
    {
        public static ModValidationData LastRequested = null;
        public static List<string> ServerSideOnlyFiles = new List<string>();
        public static List<string> WhitelistFiles = new List<string>();

        public static string SHA256CheckSum(string filePath)
        {
            if (!File.Exists(filePath))
                return "null";

            byte[] byteHash;
            using (SHA256 SHA256 = SHA256Managed.Create())
            {
                using (FileStream fileStream = File.OpenRead(filePath))
                {
                    byteHash = SHA256.ComputeHash(fileStream);
                }
            }
            string finalHash = string.Empty;
            foreach (byte b in byteHash)
                finalHash += b.ToString("x2");

            return finalHash;
        }

        public class ModHashPair
        {
            public string m_Name = "";
            public string m_Hash = "";
            public ModHashPair(string n, string h)
            {
                m_Name = n;
                m_Hash = h;
            }
        }

        public class ModValidationData 
        {
            public long m_Hash = 0;
            public List<ModHashPair> m_Files = new List<ModHashPair>();
            public string m_FullString = "";
            public string m_FullStringBase64 = "";
            public List<string> m_WhiteList = new List<string>();
        }

        public static bool ServerSideOnly(string Name)
        {
            return ServerSideOnlyFiles.Contains(Name) || WhitelistFiles.Contains(Name);
        }

        public static ModValidationData GetModsHash(bool Force = false)
        {
            ModValidationData Valid = new ModValidationData();
            if (!Force && LastRequested != null)
            {
                return LastRequested;
            }

            string AppPath = MPSaveManager.GetBaseDirectory();

            if (MyMod.DedicatedServerAppMode)
            {
                if (File.Exists(AppPath + "serversideonly.json"))
                {
                    Logger.Log("[ModsValidation][Info] Found Server Side Files List!");
                    string FilterJson = System.IO.File.ReadAllText(AppPath + "serversideonly.json");
                    ServerSideOnlyFiles = JSON.Load(FilterJson).Make<List<string>>();
                }
                if (File.Exists(AppPath + "modswhitelist.json"))
                {
                    Logger.Log("[ModsValidation][Info] Found Mods White List!");
                    string FilterJson = System.IO.File.ReadAllText(AppPath + "modswhitelist.json");
                    WhitelistFiles = JSON.Load(FilterJson).Make<List<string>>();
                }
            }
            if (Directory.Exists(AppPath + "Mods"))
            {
                foreach (string mod in Directory.GetFiles(AppPath + "Mods"))
                {
                    if (mod.Contains(".dll"))
                    {
                        string Hash = SHA256CheckSum(mod);
                        string FileName = mod;
                        if (!ServerSideOnly(FileName))
                        {
                            Valid.m_Files.Add(new ModHashPair(mod, Hash));
                        }
                        else
                        {
                            Logger.Log("[ModsValidation][Info] Ignore " + FileName);
                        }
                        if (WhitelistFiles.Contains(FileName))
                        {
                            Valid.m_WhiteList.Add(Hash);
                        }
                    }
                }

                foreach (string mod in Directory.GetFiles(AppPath + "Mods"))
                {
                    if (mod.Contains(".modcomponent"))
                    {
                        string Hash = SHA256CheckSum(mod);
                        string FileName = mod;
                        if (!ServerSideOnly(FileName))
                        {
                            Valid.m_Files.Add(new ModHashPair(mod, Hash));
                        }
                        else
                        {
                            Logger.Log("[ModsValidation][Info] Ignore " + FileName);
                        }
                        if (WhitelistFiles.Contains(FileName))
                        {
                            Valid.m_WhiteList.Add(Hash);
                        }
                    }
                }
            }
            else
            {
                Directory.CreateDirectory(AppPath + "Mods");
            }
            if (Directory.Exists(AppPath + "Plugins"))
            {
                foreach (string mod in Directory.GetFiles(AppPath + "Plugins"))
                {
                    if (mod.Contains(".dll"))
                    {
                        string Hash = SHA256CheckSum(mod);
                        string FileName = mod;
                        if (!ServerSideOnly(FileName))
                        {
                            Valid.m_Files.Add(new ModHashPair(mod, Hash));
                        }
                        else
                        {
                            Logger.Log("[ModsValidation][Info] Ignore " + FileName);
                        }
                        if (WhitelistFiles.Contains(FileName))
                        {
                            Valid.m_WhiteList.Add(Hash);
                        }
                    }
                }
            }
            else
            {
                Directory.CreateDirectory(AppPath + "Plugins");
            }

            Valid.m_Files.Sort(delegate (ModHashPair x, ModHashPair y) {
                return x.m_Name.CompareTo(y.m_Name);
            });

            string MainHash = "";
            string FullString = "";
            foreach (ModHashPair mod in Valid.m_Files)
            {
                if (string.IsNullOrEmpty(MainHash))
                {
                    MainHash = mod.m_Hash;
                    FullString = mod.m_Name;
                }
                else
                {
                    MainHash = MainHash + mod.m_Hash;
                    FullString = FullString + "\n" + mod.m_Name;
                }

                Logger.Log("[ModsValidation][Info] " + mod.m_Name + " Hash: " + mod.m_Hash);
            }

            Valid.m_Hash = Shared.GetDeterministicId(MainHash);
            Valid.m_FullString = FullString;
            Valid.m_FullStringBase64 = Shared.CompressString(FullString);
            Logger.Log("[ModsValidation][Info] Main Hash: " + Valid.m_Hash);
            Logger.Log("[ModsValidation][Info] Stock: " + Encoding.UTF8.GetBytes(Valid.m_FullString).Length);
            Logger.Log("[ModsValidation][Info] Compressed: " + Shared.CompressString(Valid.m_FullStringBase64).Length);
            LastRequested = Valid;
            return Valid;
        }
    }
}

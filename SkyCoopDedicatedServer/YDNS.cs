using SkyCoop;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace SkyCoopDedicatedServer
{
    public static class YDNS
    {
        public static string URL = "";
        public static WebClient Web;
        public static bool Init = false;
        public static void UpdateIP()
        {
            if (!Init)
            {
                string Path = MPSaveManager.GetBaseDirectory() + MPSaveManager.GetSeparator() + "ydns.txt";
                if (System.IO.File.Exists(Path))
                {
                    string readText = System.IO.File.ReadAllText(Path);
                    URL = readText;
                    if (Web == null)
                    {
                        Web = new WebClient();
                        Web.DownloadStringCompleted += new DownloadStringCompletedEventHandler(Completed);
                    }
                    Init = true;
                } else
                {
                    Logger.Log("[YDNS] File not exist ", Shared.LoggerColor.Red);
                }
            }
            
            if(Init && !string.IsNullOrEmpty(URL) && Web != null)
            {
                Logger.Log("[YDNS] "+ URL, Shared.LoggerColor.Magenta);
                Web.DownloadStringAsync(new Uri(URL));
            }
        }

        private static void Completed(object sender, DownloadStringCompletedEventArgs e)
        {
            if (e.Cancelled || e.Error != null)
            {
                Logger.Log("[YDNS] Failed to update IP!", Shared.LoggerColor.Red);
                return;
            }

            Logger.Log("[YDNS] "+e.Result, Shared.LoggerColor.Yellow);
        }
    }
}

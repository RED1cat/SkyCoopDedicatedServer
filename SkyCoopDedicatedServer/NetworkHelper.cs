using Mono.Nat;
using SkyCoop;
using System.Net;
using System;
using System.Collections.Generic;

namespace SkyCoopDedicatedServer
{
    public static class NetworkHelper
    {
        static INatDevice device;
        public static string externalIp;
        public static int port;
        public static bool upnpIsEnable = false;
        static bool tryingRestoreConnection = false;
        static bool internetAvailability = true;

        public static void OpenPort(int portToOpen)
        {
            port = portToOpen;

            SkyCoop.Logger.Log("[NetworkHelper] Try open port for upnp", SkyCoop.Shared.LoggerColor.Green);
            NatUtility.DeviceFound += DeviceFound;
            NatUtility.StartDiscovery();
        }
        private static void DeviceFound(object sender, DeviceEventArgs args)
        {
            device = args.Device;
            externalIp = device.GetExternalIP().ToString();
            try
            {
                device.CreatePortMap(new Mapping(Protocol.Udp, port, port));
            }
            catch
            {
                SkyCoop.Logger.Log($"[NetworkHelper] Port {port} could not be opened, it may already be busy by someone, or your hardware does not support upnp", SkyCoop.Shared.LoggerColor.Red);
                return;
            }
            SkyCoop.Logger.Log($"[NetworkHelper] {port} port is open", SkyCoop.Shared.LoggerColor.Green);
            SkyCoop.Logger.Log($"[NetworkHelper] External ip= {externalIp}", SkyCoop.Shared.LoggerColor.Green);
            YDNS.UpdateIP();
            upnpIsEnable = true;
            internetAvailability = true;
            tryingRestoreConnection = false;
            NatUtility.StopDiscovery();
        }
        public static void TryClosePort()
        {
            if (upnpIsEnable == true)
            {
                if (!tryingRestoreConnection)
                {
                    SkyCoop.Logger.Log("[NetworkHelper] Try close port", SkyCoop.Shared.LoggerColor.Green);
                }
                try
                {
                    device.DeletePortMap(new Mapping(Protocol.Udp, port, port));
                }
                catch
                {
                    if (!tryingRestoreConnection)
                    {
                        SkyCoop.Logger.Log("[NetworkHelper] Can't close port", SkyCoop.Shared.LoggerColor.Red);
                    }
                }
            }
        }
        public static void СheckingInternetConnection()
        {
            string ip = GetActualExternalIp();
            if(ip != "0.0.0.0")
            {
                if(ip != externalIp || internetAvailability == false)
                {
                    if (!tryingRestoreConnection)
                    {
                        SkyCoop.Logger.Log("[NetworkHelper] Attempt to reopen the port", SkyCoop.Shared.LoggerColor.Green);
                    }

                    TryClosePort();
                    tryingRestoreConnection = true;
                    OpenPort(port);
                }
            }
            else
            {
                internetAvailability = false;
            }
        }
        public static string GetActualExternalIp()
        {
            List<string> services = new List<string>()
        {
            "https://ipv4.icanhazip.com",
            "https://api.ipify.org",
            "https://ipinfo.io/ip",
            "https://checkip.amazonaws.com",
            "https://wtfismyip.com/text",
            "http://icanhazip.com"
        };
            using (var webclient = new WebClient())
                foreach (var service in services)
                {
                    try { return IPAddress.Parse(webclient.DownloadString(service)).ToString(); } catch { }
                }
            return "0.0.0.0";
        }
    }
    public static class YDNS
    {
        private static string URL = "";
        private static WebClient Web;
        private static bool Init = false;
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
                }
                else
                {
                    Logger.Log("[YDNS] File not exist ", Shared.LoggerColor.Red);
                }
            }

            if (Init && !string.IsNullOrEmpty(URL) && Web != null)
            {
                Logger.Log("[YDNS] " + URL, Shared.LoggerColor.Magenta);
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

            Logger.Log("[YDNS] " + e.Result, Shared.LoggerColor.Yellow);
        }
    }
}

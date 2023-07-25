using Mono.Nat;
using SkyCoop;
using System.Net;
using System;
using System.Net.NetworkInformation;
using System.Text;

namespace SkyCoopDedicatedServer
{
    class NetworkHelper
    {
        INatDevice device;
        private string externalIp;
        private int port;
        bool upnpIsEnable = false;
        static string addressForCheckingInternetConnection = "8.8.8.8";
        static int attemptsToEstablishAConnection = 0;

        public string GetExternalIp
        {
            get
            {
                return externalIp;
            }
        }
        public int GetPort
        {
            get
            {
                return port;
            }
        }
        public bool UpnpIsEnable
        {
            get
            {
                return upnpIsEnable;
            }
        }
        public NetworkHelper(int port)
        {
            this.port = port;

            SkyCoop.Logger.Log("[NetworkHelper] Try open port for upnp", SkyCoop.Shared.LoggerColor.Green);
            NatUtility.DeviceFound += DeviceFound;
            NatUtility.StartDiscovery();
        }
        private void DeviceFound(object sender, DeviceEventArgs args)
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
            NatUtility.StopDiscovery();
        }
        public void TryClosePort()
        {
            if (upnpIsEnable == true)
            {
                SkyCoop.Logger.Log("[NetworkHelper] Try close port", SkyCoop.Shared.LoggerColor.Green);
                try
                {
                    device.DeletePortMap(new Mapping(Protocol.Udp, port, port));
                }
                catch
                {
                    SkyCoop.Logger.Log("[NetworkHelper] Can't close port", SkyCoop.Shared.LoggerColor.Red);
                }
            }
        }
        public void СheckingInternetConnection()
        {
            string ip = GetActualExternalIp();
            if(ip != GetExternalIp || ip == "0.0.0.0")
            {
                attemptsToEstablishAConnection = 5;
            }
            
            PingOptions options = new PingOptions();
            options.DontFragment = true;

            PingReply reply = new Ping().Send(addressForCheckingInternetConnection, 5000, Encoding.ASCII.GetBytes("aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa"), options);
            if(reply.Status == IPStatus.Success)
            {
                if(attemptsToEstablishAConnection == 5 || attemptsToEstablishAConnection > 5)
                {
                    SkyCoop.Logger.Log("[NetworkHelper] Attempt to reopen the port", SkyCoop.Shared.LoggerColor.Green);

                    TryClosePort();
                    Program.networkPort = new NetworkHelper(GameServer.Server.Port);

                    attemptsToEstablishAConnection= 0;
                }
            }
            if(reply.Status != IPStatus.Success)
            {
                SkyCoop.Logger.Log($"[NetworkHelper] It was not possible to establish a connection with this IP: {addressForCheckingInternetConnection}", SkyCoop.Shared.LoggerColor.Red);
                attemptsToEstablishAConnection++;
            }
        }
        public string GetActualExternalIp()
        {
            try
            {
                return Dns.GetHostEntry("myip.opendns.com").AddressList[0].ToString();
            }
            catch
            {
                return "0.0.0.0";
            }
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

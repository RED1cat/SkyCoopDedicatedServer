using Mono.Nat;
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
            PingOptions options = new PingOptions();
            options.DontFragment= true;

            //SkyCoop.Logger.Log($"[NetworkHelper] Attempt to check the Internet connection with the IP address: {addressForCheckingInternetConnection}", SkyCoop.Shared.LoggerColor.Blue);

            PingReply reply = new Ping().Send(addressForCheckingInternetConnection, 5000, Encoding.ASCII.GetBytes("aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa"), options);
            if(reply.Status == IPStatus.Success)
            {
                //SkyCoop.Logger.Log("[NetworkHelper] Internet connection is established", SkyCoop.Shared.LoggerColor.Green);

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
    }
}

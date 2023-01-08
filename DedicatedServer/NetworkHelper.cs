using Mono.Nat;

namespace SkyCoop
{
    class NetworkHelper
    {
        INatDevice device;
        private string externalIp;
        private int port;
        bool upnpIsEnable = false;

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

            Logger.Log("[upnp] Try open port for upnp", Shared.LoggerColor.Green);
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
                Logger.Log("[upnp] Error", Shared.LoggerColor.Red);
                return;
            }
            Logger.Log($"[upnp] {port} port is open", Shared.LoggerColor.Green);
            Logger.Log($"[upnp] External ip= {externalIp}", Shared.LoggerColor.Green);
            upnpIsEnable = true;
            NatUtility.StopDiscovery();
        }
        public void TryClosePort()
        {
            if (upnpIsEnable == true)
            {
                Logger.Log("[upnp] Try close port", Shared.LoggerColor.Green);
                try
                {
                    device.DeletePortMap(new Mapping(Protocol.Udp, port, port));
                }
                catch
                {
                    Logger.Log("[upnp] Can't close port", Shared.LoggerColor.Red);
                }
            }
        }
    }
}

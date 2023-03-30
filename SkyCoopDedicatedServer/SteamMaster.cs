using SkyCoop;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace SkyCoopDedicatedServer
{
    public class SteamMaster
    {
        public static UdpClient MasterPing = null;
        public static List<UdpClient> MasterSend = new List<UdpClient>();
        public static string MasterDomain = "25.61.16.231";
        public static int MasterPort = 27010;
        public static List<IPAddress> MasterAddress = new List<IPAddress>();
        public static List<IPEndPoint> MasterEndPoint = new List<IPEndPoint>();
        const string ZeroIP = "0.0.0.0:0";

        public static void HandleMasterCallBack(byte[] BytesGot, int iMas = 0)
        {
            int IntChallenge = -1;
            int Type = -1;
            string LastIpAddress = "";
            if (BytesGot.Length > 0)
            {
                int i = 0;
                while (i < BytesGot.Length)
                {
                    if (i == 0)
                    {
                        string Header = BitConverter.ToString(new byte[6] {
                        BytesGot[i],
                        BytesGot[i+1],
                        BytesGot[i+2],
                        BytesGot[i+3],
                        BytesGot[i+4],
                        BytesGot[i+5]});

                        if (Header == "FF-FF-FF-FF-73-0A")
                        {
                            Logger.Log("[SteamMaster] MasterSend header! ", Shared.LoggerColor.Magenta);
                            Type = 1;
                        } else if (Header == "FF-FF-FF-FF-66-0A" || Header == "FF-FF-FF-FF-66-0D")
                        {
                            Logger.Log("[SteamMaster] MasterPing header! ", Shared.LoggerColor.Magenta);
                            Type = 0;
                        } else
                        {
                            Logger.Log("[SteamMaster] Wrong header! ", Shared.LoggerColor.Red);
                            return;
                        }
                        i += 6;
                    } else
                    {
                        if (Type == 0)
                        {
                            byte[] IpAddress = new byte[4] { BytesGot[i], BytesGot[i + 1], BytesGot[i + 2], BytesGot[i + 3] };
                            ushort Port = 0;

                            if (BytesGot.Length - 1 > i + 6)
                            {
                                Port = BitConverter.ToUInt16(BytesGot, i + 4);
                            }

                            LastIpAddress = IpAddress[0] + "." + IpAddress[1] + "." + IpAddress[2] + "." + IpAddress[3] + ":" + Port;

                            if (LastIpAddress != ZeroIP)
                            {
                                Logger.Log("[SteamMaster] " + LastIpAddress, Shared.LoggerColor.Magenta);
                            }
                            i += 6;
                        } else if (Type == 1)
                        {
                            IntChallenge = BitConverter.ToInt32(BytesGot, i);
                            Logger.Log("[SteamMaster] Got Challenge " + IntChallenge, Shared.LoggerColor.Magenta);
                            i += 4;
                        }
                    }
                }
            } else
            {
                return;
            }

            if (Type == 1 && IntChallenge != -1)
            {
                List<byte> Buffer = new List<byte>();
                Buffer.Add(0x30);
                Buffer.Add(0x0a);
                //Buffer.AddRange(Encoding.ASCII.GetBytes("\\protocol\\7\\challenge\\" + IntChallenge + "\\players\\0\\max\\2\\bots\\0\\gamedir\\tld_data\\map\\gratebear_island\\password\\0\\os\\1\\lan\\0\\region\\255\\type\\d\\secure\\0\\version\\2.01\\product\\tld"));
                Buffer.AddRange(Encoding.ASCII.GetBytes(@"\protocol\47\challenge\" + IntChallenge + @"\players\0\max\2\bots\0\gamedir\cstrike\map\de_dust\type\d\password\0\os\l\secure\0\lan\0\version\1.1.2.5/Stdio\region\255\product\cstrike"));

                Buffer.Add(0x0a);
                byte[] BytesToSend;
                BytesToSend = Buffer.ToArray();

                string Hex = BitConverter.ToString(BytesToSend);

                Logger.Log(Hex.Replace("-", " ").ToLower(), Shared.LoggerColor.Yellow);

                MasterSend[iMas].Send(BytesToSend, BytesToSend.Length);
                Logger.Log("[SteamMaster] Waiting for next responce", Shared.LoggerColor.Magenta);

                //SendSignalToMaster();
            } else if (Type == 0)
            {
                if (LastIpAddress == ZeroIP)
                {
                    Logger.Log("[SteamMaster] List Finished!", Shared.LoggerColor.Magenta);
                    //MasterPing.Close();
                    //MasterPing = null;
                } else
                {
                    Logger.Log("[SteamMaster] Request next part!", Shared.LoggerColor.Green);
                    //PingSteamMaster(LastIpAddress);
                }
            }
        }

        private static void MasterCallback(IAsyncResult _result)
        {
            try
            {
                IPEndPoint _clientEndPoint = new IPEndPoint(IPAddress.Any, 0);
                byte[] _data = null;
                try
                {
                    _data = MasterSend[0].EndReceive(_result, ref _clientEndPoint);
                }
                catch (System.Net.Sockets.SocketException)
                {
                    MasterSend[0].BeginReceive(MasterCallback, null);
                    Logger.Log("[SteamMaster] Expection", Shared.LoggerColor.Red);
                    return;
                }
                if (_data == null)
                {
                    Logger.Log("[SteamMaster] _data == null", Shared.LoggerColor.Red);
                    return;
                }
                Logger.Log("[SteamMaster] HandleMasterCallBack " + BitConverter.ToString(_data), Shared.LoggerColor.Magenta);
                MasterSend[0].BeginReceive(MasterCallback, null);
                HandleMasterCallBack(_data, 0);
            }
            catch (Exception)
            {

                throw;
            }
        }
        private static void MasterPingCallback(IAsyncResult _result)
        {
            try
            {
                IPEndPoint _clientEndPoint = new IPEndPoint(IPAddress.Any, 0);
                byte[] _data = null;
                try
                {
                    _data = MasterPing.EndReceive(_result, ref _clientEndPoint);
                }
                catch (System.Net.Sockets.SocketException)
                {
                    MasterPing.BeginReceive(MasterPingCallback, null);
                    Logger.Log("[SteamMaster] Error", Shared.LoggerColor.Red);
                    return;
                }
                if (_data == null)
                {
                    Logger.Log("[SteamMaster] _data == null " + BitConverter.ToString(_data), Shared.LoggerColor.Red);
                    return;
                }
                Logger.Log("[SteamMaster] HandleMasterCallBack " + BitConverter.ToString(_data), Shared.LoggerColor.Magenta);
                MasterPing.BeginReceive(MasterPingCallback, null);
                HandleMasterCallBack(_data, 0);
            }
            catch (Exception)
            {

                throw;
            }
        }

        public static void SendSignalToMaster()
        {
            List<byte> Buffer = new List<byte>();
            Buffer.Add(0x71); // "q"
            byte[] BytesToSend = Buffer.ToArray();


            if (MasterAddress.Count == 0)
            {
                IPAddress[] MasterAddresses = Dns.GetHostAddresses(MasterDomain);

                foreach (IPAddress address in MasterAddresses)
                {
                    MasterAddress.Add(address);
                    IPEndPoint End = new System.Net.IPEndPoint(address, MasterPort);
                    MasterEndPoint.Add(End);
                    UdpClient cl = new UdpClient();
                    cl.Connect(End);
                    MasterSend.Add(cl);
                    break;
                }
                if (MasterAddress.Count == 0)
                {
                    return;
                }
            }

            UdpClient SendCl = MasterSend[0];
            IPEndPoint EndSend = MasterEndPoint[0];
            SendCl.Send(BytesToSend, BytesToSend.Length);
            Logger.Log("[SteamMaster] MasterSend Client " + EndSend.Address, Shared.LoggerColor.Blue);

            SendCl.BeginReceive(MasterCallback, null);
        }

        public static void PingSteamMaster(string Seed = ZeroIP)
        {
            List<byte> Buffer = new List<byte>();
            Buffer.Add(0x31); // Message Type "1"
            Buffer.Add(0x03); // Region Europe
            Buffer.AddRange(Encoding.ASCII.GetBytes(Seed + char.MinValue)); // Seed Ip:Port
            Buffer.AddRange(Encoding.ASCII.GetBytes(@"\gamedir\cstrike" + char.MinValue)); // Filter
            byte[] BytesToSend = Buffer.ToArray();

            Logger.Log("[SteamMaster] Seed " + Seed, Shared.LoggerColor.Blue);

            if (MasterPing == null)
            {
                MasterPing = new UdpClient();
                MasterPing.Connect(MasterDomain, MasterPort);
                Logger.Log("[SteamMaster] MasterPing Client Created", Shared.LoggerColor.Blue);
            }

            MasterPing.Send(BytesToSend, BytesToSend.Length);
            if (MasterAddress.Count == 0)
            {
                IPAddress[] MasterAddresses = Dns.GetHostAddresses(MasterDomain);

                foreach (IPAddress address in MasterAddresses)
                {
                    MasterAddress.Add(address);
                    MasterEndPoint.Add(new System.Net.IPEndPoint(address, MasterPort));
                }
                if (MasterAddress.Count == 0)
                {
                    return;
                }
            }
            MasterPing.BeginReceive(MasterPingCallback, null);
        }
    }
}

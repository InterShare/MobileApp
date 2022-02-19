using System;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using Xamarin.Forms;

namespace InterShareMobile.Helper
{
    public class IpAddress
    {
        public static string GetIpAddress()
        {
            try
            {
                foreach (NetworkInterface item in NetworkInterface.GetAllNetworkInterfaces())
                {
                    bool networkInterface = Device.RuntimePlatform != Device.iOS || item.Name == "en0";

                    if (item.OperationalStatus == OperationalStatus.Up && networkInterface)
                    {
                        IPInterfaceProperties adapterProperties = item.GetIPProperties();

                        if (adapterProperties.GatewayAddresses.FirstOrDefault() != null)
                        {
                            foreach (UnicastIPAddressInformation ip in adapterProperties.UnicastAddresses)
                            {
                                if (ip.Address.AddressFamily == AddressFamily.InterNetwork && ip.Address.ToString() != "127.0.0.1")
                                {
                                    return ip.Address.ToString();
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception)
            {
                // ignored
            }

            try
            {
                string hostName = Dns.GetHostName();
                IPHostEntry host = Dns.GetHostEntry(hostName);

                foreach (IPAddress ip in host.AddressList)
                {
                    if (ip.AddressFamily == AddressFamily.InterNetwork && ip.ToString() != "127.0.0.1")
                    {
                        return ip.ToString();
                    }
                }

                return "";
            }
            catch (Exception)
            {
                return "";
            }
        }
    }
}
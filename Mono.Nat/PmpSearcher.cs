using System;
using System.Collections.Generic;
using System.Text;
using System.Net;
using Mono.Nat.Pmp;
using System.Net.NetworkInformation;
using System.Net.Sockets;

namespace Mono.Nat
{
    internal class PmpSearcher : ISearcher
    {
		static PmpSearcher instance = new PmpSearcher();
        public static List<UdpClient> sockets;
        static Dictionary<UdpClient, List<IPEndPoint>> gatewayLists;
		

		public static PmpSearcher Instance
		{
			get { return instance; }
		}

        private int timeout;
        private DateTime nextSearch;
        public event EventHandler<DeviceEventArgs> DeviceFound;

        static PmpSearcher()
        {
            CreateSocketsAndAddGateways();
        }

        PmpSearcher()
        {
            timeout = 250;
        }

		private static void CreateSocketsAndAddGateways()
		{
            sockets = new List<UdpClient>();
            gatewayLists = new Dictionary<UdpClient,List<IPEndPoint>>();

            try
            {
                foreach (NetworkInterface n in NetworkInterface.GetAllNetworkInterfaces())
                {
                    IPInterfaceProperties properties = n.GetIPProperties();
                    List<IPEndPoint> gatewayList = new List<IPEndPoint>();

                    foreach (GatewayIPAddressInformation gateway in properties.GatewayAddresses)
                    {
                        if (gateway.Address.AddressFamily == AddressFamily.InterNetwork)
                        {
                            gatewayList.Add(new IPEndPoint(gateway.Address, PmpConstants.ServerPort));
                        }
                    }

                    if (gatewayList.Count > 0)
                    {
                        foreach (UnicastIPAddressInformation address in properties.UnicastAddresses)
                        {
                            if (address.Address.AddressFamily == AddressFamily.InterNetwork)
                            {
                                UdpClient client;

                                try
                                {
                                    client = new UdpClient(new IPEndPoint(address.Address, 0));
                                }
                                catch (SocketException)
                                {
                                    continue; // Move on to the next address.
                                }

                                gatewayLists.Add(client, gatewayList); sockets.Add(client);
                            }
                        }
                    }
                }
            }
            catch (Exception)
            {
                // NAT-PMP does not use multicast, so there isn't really a good fallback.
            }
		}

        public void Search()
		{
			foreach (UdpClient s in sockets)
			{
				try
				{
					Search(s);
				}
				catch
				{
					// Ignore any search errors
				}
			}
		}

		void Search (UdpClient client)
        {
            // Sort out the time for the next search first. The spec says the 
            // timeout should double after each attempt. Once it reaches 64 seconds
            // (and that attempt fails), assume no devices available
            nextSearch = DateTime.Now.AddMilliseconds(timeout);
            timeout *= 2;

            // We've tried 9 times as per spec, try searching again in 5 minutes
            if (timeout == 128 * 1000)
            {
                timeout = 250;
                nextSearch = DateTime.Now.AddMinutes(10);
                return;
            }

            // The nat-pmp search message. Must be sent to GatewayIP:53531
            byte[] buffer = new byte[] { PmpConstants.Version, PmpConstants.OperationCode };
            foreach (IPEndPoint gatewayEndpoint in gatewayLists[client])
                client.Send(buffer, buffer.Length, gatewayEndpoint);
        }

        bool IsSearchAddress(IPAddress address)
        {
            foreach (List<IPEndPoint> gatewayList in gatewayLists.Values)
                foreach (IPEndPoint gatewayEndpoint in gatewayList)
                    if (gatewayEndpoint.Address.Equals(address))
                        return true;
            return false;
        }

        public void Handle(IPAddress localAddress, byte[] response, IPEndPoint endpoint)
        {
            if (!IsSearchAddress(endpoint.Address))
                return;
            if (response.Length != 12)
                return;
            if (response[0] != PmpConstants.Version)
                return;
            if (response[1] != PmpConstants.ServerNoop)
                return;
            int errorcode = IPAddress.NetworkToHostOrder(BitConverter.ToInt16(response, 2));
            if (errorcode != 0)
                NatUtility.Log("Non zero error: {0}", errorcode);

            IPAddress publicIp = new IPAddress(new byte[] { response[8], response[9], response[10], response[11] });
            nextSearch = DateTime.Now.AddMinutes(5);
            timeout = 250;
            OnDeviceFound(new DeviceEventArgs(new PmpNatDevice(endpoint.Address, publicIp)));
        }

        public DateTime NextSearch
        {
            get { return nextSearch; }
        }
        private void OnDeviceFound(DeviceEventArgs args)
        {
            if (DeviceFound != null)
                DeviceFound(this, args);
        }
    }
}

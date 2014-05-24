using System;
using System.Collections.Generic;
using System.Text;
using System.Net;
using Mono.Nat.Upnp;
using System.Diagnostics;
using System.Net.Sockets;
using System.Net.NetworkInformation;
using System.Threading;

namespace Mono.Nat
{
	public class UpnpSearcher : ISearcher
	{
		internal const string WanIPUrn = "urn:schemas-upnp-org:service:WANIPConnection:1";
        internal const string WanPPPUrn = "urn:schemas-upnp-org:service:WANPPPConnection:1";

		private const int SearchPeriod = 5 * 60; // The time in seconds between each search
		static UpnpSearcher instance = new UpnpSearcher();
		public static List<UdpClient> sockets = CreateSockets();

		public event EventHandler<DeviceEventArgs> DeviceFound;

		private List<INatDevice> devices;
		private Dictionary<IPAddress, DateTime> lastFetched;
		private DateTime nextSearch;
		private IPEndPoint searchEndpoint;

		public UpnpSearcher()
		{
			devices = new List<INatDevice>();
			lastFetched = new Dictionary<IPAddress, DateTime>();
			searchEndpoint = new IPEndPoint(IPAddress.Parse("239.255.255.250"), 1900);
		}

		static List<UdpClient> CreateSockets()
		{
			List<UdpClient> clients = new List<UdpClient>();
			try
			{
				foreach(NetworkInterface n in NetworkInterface.GetAllNetworkInterfaces())
				{
					foreach(UnicastIPAddressInformation address in n.GetIPProperties().UnicastAddresses)
					{
						if(address.Address.AddressFamily == AddressFamily.InterNetwork)
						{
							try
							{
								clients.Add(new UdpClient(new IPEndPoint(address.Address, 0)));
							}
							catch
							{
								continue; // Move on to the next address.
							}
						}
					}
				}
			}
			catch(Exception)
			{
				clients.Add(new UdpClient(0));
			}
			return clients;
		}

		public void Search()
		{
			foreach(UdpClient s in sockets)
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

		void Search(UdpClient client)
		{
			nextSearch = DateTime.Now.AddSeconds(SearchPeriod);
			byte[] data = DiscoverDeviceMessage.Encode();

			// UDP is unreliable, so send 3 requests at a time (per Upnp spec, sec 1.1.2)
			for(int i = 0 ; i < 3 ; i++)
				client.Send(data, data.Length, searchEndpoint);
		}

		public IPEndPoint SearchEndpoint
		{
			get
			{
				return searchEndpoint;
			}
		}

		public void Handle(IPAddress localAddress, byte[] response, IPEndPoint endpoint)
		{
			// Convert it to a string for easy parsing
			string dataString = null;

			// No matter what, this method should never throw an exception. If something goes wrong
			// we should still be in a position to handle the next reply correctly.
			try
			{
				dataString = Encoding.UTF8.GetString(response);

				if(NatUtility.Verbose)
					NatUtility.Log("UPnP Response: {0}", dataString);
				// If this device does not have a WANIPConnection service, then ignore it
				// Technically i should be checking for WANIPConnection:1 and InternetGatewayDevice:1
				// but there are some routers missing the '1'.
				string log = "UPnP Response: Router advertised a '{0}' service";
				StringComparison c = StringComparison.OrdinalIgnoreCase;
			    string st = "";
                if(dataString.IndexOf("urn:schemas-upnp-org:service:WANIPConnection:", c) != -1)
                {
                    NatUtility.Log(log, "urn:schemas-upnp-org:service:WANIPConnection:");
                    st = WanIPUrn;
                }
                else if(dataString.IndexOf("urn:schemas-upnp-org:device:InternetGatewayDevice:", c) != -1)
					NatUtility.Log(log, "urn:schemas-upnp-org:device:InternetGatewayDevice:");
				else if(dataString.IndexOf("urn:schemas-upnp-org:service:WANPPPConnection:", c) != -1)
				{
				    NatUtility.Log(log, "urn:schemas-upnp-org:service:WANPPPConnection:");
				    st = WanPPPUrn;
				}

				else
					return;

				// We have an internet gateway device now
				UpnpNatDevice d = new UpnpNatDevice(localAddress, dataString, st);

				if(this.devices.Contains(d))
				{
					// We already have found this device, so we just refresh it to let people know it's
					// Still alive. If a device doesn't respond to a search, we dump it.
					this.devices[this.devices.IndexOf(d)].LastSeen = DateTime.Now;
				}
				else
				{

					// If we send 3 requests at a time, ensure we only fetch the services list once
					// even if three responses are received
					if(lastFetched.ContainsKey(endpoint.Address))
					{
						DateTime last = lastFetched[endpoint.Address];
						if((DateTime.Now - last) < TimeSpan.FromSeconds(20))
							return;
					}
					lastFetched[endpoint.Address] = DateTime.Now;

					// Once we've parsed the information we need, we tell the device to retrieve it's service list
					// Once we successfully receive the service list, the callback provided will be invoked.
					NatUtility.Log("Fetching service list: {0}", d.HostEndPoint);
					d.GetServicesList(new NatDeviceCallback(DeviceSetupComplete));
				}
			}
			catch(Exception ex)
			{
				Trace.WriteLine("Unhandled exception when trying to decode a device's response Send me the following data: ");
				Trace.WriteLine("ErrorMessage:");
				Trace.WriteLine(ex.Message);
				Trace.WriteLine("Data string:");
				Trace.WriteLine(dataString);
			}
		}

		public DateTime NextSearch
		{
			get
			{
				return nextSearch;
			}
		}

		private void DeviceSetupComplete(INatDevice device)
		{
			lock(this.devices)
			{
				// We don't want the same device in there twice
				if(devices.Contains(device))
					return;

				devices.Add(device);
			}

			OnDeviceFound(new DeviceEventArgs(device));
		}

		private void OnDeviceFound(DeviceEventArgs args)
		{
			if(DeviceFound != null)
				DeviceFound(this, args);
		}

		/// <summary>
		/// Search for a nat device every 10 seconds for 35 seconds
		/// </summary>
		/// <param name="cancellation"></param>
		/// <returns>the nat device or null if nothing found after 35 seconds</returns>
		public INatDevice SearchAndReceive(CancellationToken cancellation = default(CancellationToken))
		{
			Search();
			var maxSearchTime = new CancellationTokenSource();
			maxSearchTime.CancelAfter(TimeSpan.FromSeconds(35));

			IPEndPoint received = new IPEndPoint(IPAddress.Parse("192.168.0.1"), 5351);

			CancellationTokenSource resendSearch = new CancellationTokenSource();
			resendSearch.CancelAfter(TimeSpan.FromSeconds(10));
			while(devices.Count == 0)
			{
				cancellation.ThrowIfCancellationRequested();
				if(resendSearch.IsCancellationRequested)
				{
					Search();
					resendSearch = new CancellationTokenSource();
					resendSearch.CancelAfter(TimeSpan.FromSeconds(10));
				}
				if(maxSearchTime.IsCancellationRequested)
					return null;
				foreach(UdpClient client in sockets)
				{
					if(client.Available > 0)
					{
						IPAddress localAddress = ((IPEndPoint)client.Client.LocalEndPoint).Address;
						byte[] data = client.Receive(ref received);
						Handle(localAddress, data, received);
						Thread.Sleep(10);
					}
				}
			}
			return devices[0];
		}
	}
}

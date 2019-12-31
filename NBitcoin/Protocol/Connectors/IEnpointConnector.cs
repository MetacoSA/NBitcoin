#if !NOSOCKET
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace NBitcoin.Protocol.Connectors
{
	public interface IEnpointConnector
	{
		Task ConnectSocket(Socket socket, EndPoint endPoint, NodeConnectionParameters nodeConnectionParameters, CancellationToken cancellationToken);
		IEnpointConnector Clone();
	}
}
#endif
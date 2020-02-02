#if !NOSOCKET
using System;
using System.Collections.Generic;
using System.Text;

namespace NBitcoin.Socks
{
	public enum SocksErrorCode
	{
		Success = 0,
		GeneralServerFailure = 1,
		ConnectionNotAllowed = 2,
		NetworkUnreachable = 3,
		HostUnreachable = 4,
		ConnectionRefused = 5,
		TTLExpired = 6,
		CommandNotSupported = 7,
		AddressTypeNotSupported = 8,
	}
	public class SocksException : Exception
	{
		public SocksException(SocksErrorCode errorCode) : base(GetMessageForCode((int)errorCode))
		{
			SocksErrorCode = errorCode;
		}

		public bool IsTransient
		{
			get
			{
				return SocksErrorCode == SocksErrorCode.GeneralServerFailure ||
					   SocksErrorCode == SocksErrorCode.TTLExpired;
			}
		}

		public SocksErrorCode SocksErrorCode
		{
			get; set;
		}

		private static string GetMessageForCode(int errorCode)
		{
			switch (errorCode)
			{
				case 0:
					return "Success";
				case 1:
					return "general SOCKS server failure";
				case 2:
					return "connection not allowed by ruleset";
				case 3:
					return "Network unreachable";
				case 4:
					return "Host unreachable";
				case 5:
					return "Connection refused";
				case 6:
					return "TTL expired";
				case 7:
					return "Command not supported";
				case 8:
					return "Address type not supported";
				default:
					return "Unknown code";
			}
		}

		public SocksException(string message) : base(message)
		{

		}
	}
}
#endif
using NBitcoin.Crypto;
using System;

namespace NBitcoin.Protocol
{
	public class ProtocolCapabilities
	{
		/// <summary>
		/// Disconnect from peers older than this protocol version
		/// </summary>
		public bool PeerTooOld
		{
			get; set;
		}

		/// <summary>
		/// nTime field added to CAddress, starting with this version;
		/// if possible, avoid requesting addresses nodes older than this
		/// </summary>
		public bool SupportTimeAddress
		{
			get; set;
		}

		public bool SupportGetBlock
		{
			get; set;
		}
		/// <summary>
		/// BIP 0031, pong message, is enabled for all versions AFTER this one
		/// </summary>
		public bool SupportPingPong
		{
			get; set;
		}

		/// <summary>
		/// "mempool" command, enhanced "getdata" behavior starts with this version
		/// </summary>
		public bool SupportMempoolQuery
		{
			get; set;
		}

		/// <summary>
		/// ! "filter*" commands are disabled without NODE_BLOOM after and including this version
		/// </summary>
		public bool SupportNodeBloom
		{
			get; set;
		}

		/// <summary>
		/// ! "sendheaders" command and announcing blocks with headers starts with this version
		/// </summary>
		public bool SupportSendHeaders
		{
			get; set;
		}

		/// <summary>
		/// ! Version after which witness support potentially exists
		/// </summary>
		public bool SupportWitness
		{
			get; set;
		}

		/// <summary>
		/// short-id-based block download starts with this version
		/// </summary>
		public bool SupportCompactBlocks
		{
			get; set;
		}

		/// <summary>
		/// Support checksum at p2p message level
		/// </summary>
		public bool SupportCheckSum
		{
			get;
			set;
		}
		public virtual HashStreamBase GetChecksumHashStream()
		{
			return new HashStream();
		}
		public virtual HashStreamBase GetChecksumHashStream(int hintSize)
		{
			return GetChecksumHashStream();
		}


		public bool SupportUserAgent
		{
			get;
			set;
		}

		public bool SupportAddrv2
		{ 
			get; 
			set;
		}

		public static ProtocolCapabilities CreateSupportAll()
		{
			return new ProtocolCapabilities()
			{
				PeerTooOld = false,
				SupportCheckSum = true,
				SupportCompactBlocks = true,
				SupportGetBlock = true,
				SupportMempoolQuery = true,
				SupportNodeBloom = true,
				SupportPingPong = true,
				SupportSendHeaders = true,
				SupportTimeAddress = true,
				SupportUserAgent = true,
				SupportWitness = true,
				SupportAddrv2 = true
			};
		}

		public bool IsSupersetOf(ProtocolCapabilities capabilities)
		{
			return (!capabilities.SupportCheckSum || SupportCheckSum) &&
				(!capabilities.SupportCompactBlocks || SupportCompactBlocks) &&
				(!capabilities.SupportGetBlock || SupportGetBlock) &&
				(!capabilities.SupportMempoolQuery || SupportMempoolQuery) &&
				(!capabilities.SupportNodeBloom || SupportNodeBloom) &&
				(!capabilities.SupportPingPong || SupportPingPong) &&
				(!capabilities.SupportSendHeaders || SupportSendHeaders) &&
				(!capabilities.SupportTimeAddress || SupportTimeAddress) &&
				(!capabilities.SupportWitness || SupportWitness) &&
				(!capabilities.SupportUserAgent || SupportUserAgent) &&
				(!capabilities.SupportCheckSum || SupportCheckSum) &&
				(!capabilities.SupportAddrv2 || SupportAddrv2);
		}
	}
}

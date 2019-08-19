using NBitcoin.Protobuf;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
#if !NOHTTPCLIENT
using System.Net.Http;
using System.Net.Http.Headers;
#endif
using System.Text;
using System.Threading.Tasks;

namespace NBitcoin.Payment
{
	[Obsolete("BIP70 is obsolete")]
	public class PaymentACK
	{
		public const int MaxLength = 60000;

		public static PaymentACK Load(byte[] data, Network network)
		{
			return Load(new MemoryStream(data), network);
		}

		[Obsolete("Use Load(byte[] data, Network network) instead")]
		public static PaymentACK Load(byte[] data)
		{
			return Load(new MemoryStream(data));
		}

		public static PaymentACK Load(Stream source, Network network)
		{
			if (source.CanSeek && source.Length > MaxLength)
				throw new ArgumentOutOfRangeException("PaymentACK messages larger than " + MaxLength + " bytes should be rejected", "source");

			PaymentACK ack = new PaymentACK();
			Protobuf.ProtobufReaderWriter reader = new Protobuf.ProtobufReaderWriter(source);
			int key;
			while (reader.TryReadKey(out key))
			{
				switch (key)
				{
					case 1:
						var bytes = reader.ReadBytes();
						ack.Payment = PaymentMessage.Load(bytes, network);
						break;
					case 2:
						ack.Memo = reader.ReadString();
						break;
					default:
						break;
				}
			}
			return ack;
		}

		[Obsolete("Use Load(Stream source, Network network) instead")]
		public static PaymentACK Load(Stream source)
		{
			return Load(source, null);
		}
		public PaymentACK()
		{

		}
		public PaymentACK(PaymentMessage payment)
		{
			Payment = payment;
		}

		public readonly static string MediaType = "application/bitcoin-paymentack";
		public PaymentMessage Payment
		{
			get;
			set;
		}

		public string Memo
		{
			get;
			set;
		}

		public byte[] ToBytes()
		{
			MemoryStream ms = new MemoryStream();
			WriteTo(ms);
			return ms.ToArray();
		}

		public void WriteTo(Stream output)
		{
			Protobuf.ProtobufReaderWriter proto = new ProtobufReaderWriter(output);
			proto.WriteKey(1, ProtobufReaderWriter.PROTOBUF_LENDELIM);
			proto.WriteBytes(Payment.ToBytes());
			if (Memo != null)
			{
				proto.WriteKey(2, ProtobufReaderWriter.PROTOBUF_LENDELIM);
				proto.WriteString(Memo);
			}
		}
#if !NOFILEIO
		public static PaymentACK Load(string file, Network network)
		{
			using (var fs = File.OpenRead(file))
			{
				return Load(fs, network);
			}
		}

		[Obsolete("Use Load(string file, Network network) instead")]
		public static PaymentACK Load(string file)
		{
			return Load(file, null);
		}
#endif
	}
	[Obsolete("BIP70 is obsolete")]
	public class PaymentMessage
	{
		public const int MaxLength = 50000;
		public static PaymentMessage Load(byte[] data, Network network)
		{
			return Load(new MemoryStream(data), network);
		}

		[Obsolete("Use Load(byte[] data, Network network)")]
		public static PaymentMessage Load(byte[] data)
		{
			return Load(new MemoryStream(data), null);
		}

		public PaymentACK CreateACK(string memo = null)
		{
			return new PaymentACK(this)
			{
				Memo = memo
			};
		}


		public static PaymentMessage Load(Stream source, Network network)
		{
			if (source.CanSeek && source.Length > MaxLength)
				throw new ArgumentException("Payment messages larger than " + MaxLength + " bytes should be rejected by the merchant's server", "source");
			network = network ?? Network.Main;
			PaymentMessage message = new PaymentMessage();
			ProtobufReaderWriter proto = new ProtobufReaderWriter(source);
			int key;
			while (proto.TryReadKey(out key))
			{
				switch (key)
				{
					case 1:
						message.MerchantData = proto.ReadBytes();
						break;
					case 2:
						var tx = network.Consensus.ConsensusFactory.CreateTransaction();
						tx.ReadWrite(proto.ReadBytes(), network);
						message.Transactions.Add(tx);
						break;
					case 3:
						message.RefundTo.Add(PaymentOutput.Load(proto.ReadBytes()));
						break;
					case 4:
						message.Memo = proto.ReadString();
						break;
					default:
						break;
				}
			}
			message.Network = network;
			return message;
		}

		[Obsolete("Use Load(Stream, Network)")]
		public static PaymentMessage Load(Stream source)
		{
			return Load(source, null);
		}

		public string Memo
		{
			get;
			set;
		}
		public byte[] MerchantData
		{
			get;
			set;
		}

		private readonly List<PaymentOutput> _RefundTo = new List<PaymentOutput>();
		public List<PaymentOutput> RefundTo
		{
			get
			{
				return _RefundTo;
			}
		}

		private readonly List<Transaction> _Transactions = new List<Transaction>();
		public readonly static string MediaType = "application/bitcoin-payment";
		public PaymentMessage()
		{

		}

		public PaymentMessage(PaymentRequest request)
		{
			this.MerchantData = request.Details.MerchantData;
		}
		public List<Transaction> Transactions
		{
			get
			{
				return _Transactions;
			}
		}

		public Uri ImplicitPaymentUrl
		{
			get;
			set;
		}
		public Network Network
		{
			get;
			private set;
		}

		public byte[] ToBytes()
		{
			MemoryStream ms = new MemoryStream();
			WriteTo(ms);
			return ms.ToArray();
		}

		public void WriteTo(Stream output)
		{
			var proto = new ProtobufReaderWriter(output);
			if (MerchantData != null)
			{
				proto.WriteKey(1, ProtobufReaderWriter.PROTOBUF_LENDELIM);
				proto.WriteBytes(MerchantData);
			}

			foreach (var tx in Transactions)
			{
				proto.WriteKey(2, ProtobufReaderWriter.PROTOBUF_LENDELIM);
				proto.WriteBytes(tx.ToBytes());
			}

			foreach (var txout in RefundTo)
			{
				proto.WriteKey(3, ProtobufReaderWriter.PROTOBUF_LENDELIM);
				proto.WriteBytes(txout.ToBytes());
			}

			if (Memo != null)
			{
				proto.WriteKey(4, ProtobufReaderWriter.PROTOBUF_LENDELIM);
				proto.WriteString(Memo);
			}
		}
#if !NOHTTPCLIENT
		/// <summary>
		/// Send the payment to given address
		/// </summary>
		/// <param name="paymentUrl">ImplicitPaymentUrl if null</param>
		/// <returns>The PaymentACK</returns>
		public PaymentACK SubmitPayment(Uri paymentUrl = null)
		{
			if (paymentUrl == null)
				paymentUrl = ImplicitPaymentUrl;
			if (paymentUrl == null)
				throw new ArgumentNullException(nameof(paymentUrl));
			try
			{
				return SubmitPaymentAsync(paymentUrl, null).Result;
			}
			catch (AggregateException ex)
			{
				throw ex.InnerException;
			}
		}

		public async Task<PaymentACK> SubmitPaymentAsync(Uri paymentUrl, HttpClient httpClient)
		{
			bool own = false;
			if (paymentUrl == null)
				paymentUrl = ImplicitPaymentUrl;
			if (paymentUrl == null)
				throw new ArgumentNullException(nameof(paymentUrl));
			if (httpClient == null)
			{
				httpClient = new HttpClient();
				own = true;
			}

			try
			{
				var request = new HttpRequestMessage(HttpMethod.Post, paymentUrl.OriginalString);
				request.Headers.Clear();
				request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue(PaymentACK.MediaType));
				request.Content = new ByteArrayContent(this.ToBytes());
				request.Content.Headers.ContentType = new MediaTypeHeaderValue(PaymentMessage.MediaType);

				var result = await httpClient.SendAsync(request).ConfigureAwait(false);
				if (!result.IsSuccessStatusCode)
					throw new WebException(result.StatusCode + "(" + (int)result.StatusCode + ")");

				if (result.Content.Headers.ContentType == null || !result.Content.Headers.ContentType.MediaType.Equals(PaymentACK.MediaType, StringComparison.OrdinalIgnoreCase))
				{
					throw new WebException("Invalid contenttype received, expecting " + PaymentACK.MediaType + ", but got " + result.Content.Headers.ContentType);
				}
				var response = await result.Content.ReadAsStreamAsync().ConfigureAwait(false);
				return PaymentACK.Load(response, Network);
			}
			finally
			{
				if (own)
					httpClient.Dispose();
			}
		}
#endif
#if !NOFILEIO
		public static PaymentMessage Load(string file, Network network)
		{
			using (var fs = File.OpenRead(file))
			{
				return Load(fs, network);
			}
		}
		[Obsolete("Use Load(String file, Network network)")]
		public static PaymentMessage Load(string file)
		{
			using (var fs = File.OpenRead(file))
			{
				return Load(fs);
			}
		}
#endif
	}
}
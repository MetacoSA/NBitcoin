using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NBitcoin.Payment
{
	public class PaymentACK
	{
		public static PaymentACK Load(byte[] data)
		{
			return Load(new MemoryStream(data));
		}

		public static PaymentACK Load(Stream source)
		{
			var data = PaymentRequest.Serializer.Deserialize<Proto.PaymentACK>(source);
			return new PaymentACK(data);
		}
		public PaymentACK()
		{

		}
		public PaymentACK(PaymentMessage payment)
		{
			_Payment = payment;
		}
		internal PaymentACK(Proto.PaymentACK data)
		{
			_Payment = new PaymentMessage(data.payment);
			Memo = data.memoSpecified ? data.memo : null;
			OriginalData = data;
		}

		private readonly PaymentMessage _Payment = new PaymentMessage();
		public PaymentMessage Payment
		{
			get
			{
				return _Payment;
			}
		}

		public string Memo
		{
			get;
			set;
		}
		
		internal Proto.PaymentACK OriginalData
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
			var data = OriginalData == null ? new Proto.PaymentACK() : (Proto.PaymentACK)PaymentRequest.Serializer.DeepClone(OriginalData);
			data.memo = Memo;
			data.payment = Payment.ToData();
			PaymentRequest.Serializer.Serialize(output, data);
		}
	}
	public class PaymentMessage
	{
		public static PaymentMessage Load(byte[] data)
		{
			return Load(new MemoryStream(data));
		}

		public PaymentACK CreateACK(string memo = null)
		{
			return new PaymentACK(this)
			{
				Memo = memo
			};
		}

		public static PaymentMessage Load(Stream source)
		{
			var data = PaymentRequest.Serializer.Deserialize<Proto.Payment>(source);
			return new PaymentMessage(data);
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
		public PaymentMessage()
		{

		}
		internal PaymentMessage(Proto.Payment data)
		{
			Memo = data.memoSpecified ? data.memo : null;
			MerchantData = data.merchant_data;
			foreach(var tx in data.transactions)
			{
				Transactions.Add(new Transaction(tx));
			}
			foreach(var refund in data.refund_to)
			{
				RefundTo.Add(new PaymentOutput(refund));
			}
			OriginalData = data;
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

		internal Proto.Payment OriginalData
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
			PaymentRequest.Serializer.Serialize(output, this.ToData());
		}

		internal Proto.Payment ToData()
		{
			var data = OriginalData == null ? new Proto.Payment() : (Proto.Payment)PaymentRequest.Serializer.DeepClone(OriginalData);
			data.memo = Memo;
			data.merchant_data = MerchantData;

			foreach(var refund in RefundTo)
			{
				data.refund_to.Add(refund.ToData());
			}
			foreach(var transaction in Transactions)
			{
				data.transactions.Add(transaction.ToBytes());
			}

			return data;
		}
	}
}

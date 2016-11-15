using NBitcoin.Crypto;
using NBitcoin.DataEncoders;
using NBitcoin.Protocol;
using NBitcoin.RPC;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NBitcoin
{
	public class WitScript
	{
		byte[][] _Pushes;
		public WitScript(string script)
		{
			var parts = script.Split(new[] { '\t', ' ' }, StringSplitOptions.RemoveEmptyEntries);
			_Pushes = new byte[parts.Length][];
			for(int i = 0; i < parts.Length; i++)
			{
				_Pushes[i] = Encoders.Hex.DecodeData(parts[i]);
			}
		}

		/// <summary>
		/// Create a new WitnessScript
		/// </summary>
		/// <param name="script">Scripts</param>
		/// <param name="unsafe">If false, make a copy of the input script array</param>
		public WitScript(byte[][] script, bool @unsafe = false)
		{
			if(@unsafe)
				_Pushes = script;
			else
			{
				_Pushes = script.ToArray();
				for(int i = 0; i < _Pushes.Length; i++)
					_Pushes[i] = script[i].ToArray();
			}
		}

		/// <summary>
		/// Create a new WitnessScript
		/// </summary>
		/// <param name="script">Scripts</param>
		public WitScript(IEnumerable<byte[]> script, bool @unsafe = false)
			: this(script.ToArray(), @unsafe)
		{

		}

		public WitScript(params Op[] ops)
		{
			List<byte[]> pushes = new List<byte[]>();
			foreach(var op in ops)
			{
				if(op.PushData == null)
					throw new ArgumentException("Non push operation unsupported in WitScript", "ops");
				pushes.Add(op.PushData);
			}
			_Pushes = pushes.ToArray();
		}

		public WitScript(byte[] script)
		{
			if(script == null)
				throw new ArgumentNullException("script");
			var ms = new MemoryStream(script);
			BitcoinStream stream = new BitcoinStream(ms, false);
			ReadCore(stream);
		}
		WitScript()
		{

		}

		public WitScript(Script scriptSig)
		{
			List<byte[]> pushes = new List<byte[]>();
			foreach(var op in scriptSig.ToOps())
			{
				if(op.PushData == null)
					throw new ArgumentException("A WitScript can only contains push operations", "script");
				pushes.Add(op.PushData);
			}
			_Pushes = pushes.ToArray();
		}

		public static WitScript Load(BitcoinStream stream)
		{
			WitScript script = new WitScript();
			script.ReadCore(stream);
			return script;
		}
		void ReadCore(BitcoinStream stream)
		{
			List<byte[]> pushes = new List<byte[]>();
			uint pushCount = 0;
			stream.ReadWriteAsVarInt(ref pushCount);
			for(int i = 0; i < (int)pushCount; i++)
			{
				byte[] push = ReadPush(stream);
				pushes.Add(push);
			}
			_Pushes = pushes.ToArray();
		}
		private static byte[] ReadPush(BitcoinStream stream)
		{
			byte[] push = null;
			stream.ReadWriteAsVarString(ref push);
			return push;
		}

		public byte[] this[int index]
		{
			get
			{
				return _Pushes[index];
			}
		}

		public IEnumerable<byte[]> Pushes
		{
			get
			{
				return _Pushes;
			}
		}

		static WitScript _Empty = new WitScript(new byte[0][], true);

		public static WitScript Empty
		{
			get
			{
				return _Empty;
			}
		}

		public override bool Equals(object obj)
		{
			WitScript item = obj as WitScript;
			if(item == null)
				return false;
			return EqualsCore(item);
		}

		private bool EqualsCore(WitScript item)
		{
			if(_Pushes.Length != item._Pushes.Length)
				return false;
			for(int i = 0; i < _Pushes.Length; i++)
			{
				if(!Utils.ArrayEqual(_Pushes[i], item._Pushes[i]))
					return false;
			}
			return true;
		}
		public static bool operator ==(WitScript a, WitScript b)
		{
			if(System.Object.ReferenceEquals(a, b))
				return true;
			if(((object)a == null) || ((object)b == null))
				return false;
			return a.EqualsCore(b);
		}

		public static bool operator !=(WitScript a, WitScript b)
		{
			return !(a == b);
		}
		public static WitScript operator +(WitScript a, WitScript b)
		{
			if(a == null)
				return b;
			if(b == null)
				return a;
			return new WitScript(a._Pushes.Concat(b._Pushes).ToArray());
		}
		public static implicit operator Script(WitScript witScript)
		{
			if(witScript == null)
				return null;
			return witScript.ToScript();
		}
		public override int GetHashCode()
		{
			return Utils.GetHashCode(ToBytes());
		}

		public byte[] ToBytes()
		{
			var ms = new MemoryStream();
			BitcoinStream stream = new BitcoinStream(ms, true);
			uint pushCount = (uint)_Pushes.Length;
			stream.ReadWriteAsVarInt(ref pushCount);
			foreach(var push in Pushes)
			{
				var localpush = push;
				stream.ReadWriteAsVarString(ref localpush);
			}
			return ms.ToArrayEfficient();
		}

		public override string ToString()
		{
			return ToScript().ToString();
		}

		public Script ToScript()
		{
			return new Script(_Pushes.Select(p => Op.GetPushOp(p)).ToArray());
		}

		public int PushCount
		{
			get
			{
				return _Pushes.Length;
			}
		}

		public byte[] GetUnsafePush(int i)
		{
			return _Pushes[i];
		}

		public WitScript Clone()
		{
			return new WitScript(ToBytes());
		}
	}
}

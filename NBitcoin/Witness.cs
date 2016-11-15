using System;
using System.Linq;

namespace NBitcoin
{
	class Witness
	{
		TxInList _Inputs;
		public Witness(TxInList inputs)
		{
			_Inputs = inputs;
		}

		internal bool IsNull()
		{
			return _Inputs.All(i => i.WitScript.PushCount == 0);
		}

		internal void ReadWrite(BitcoinStream stream)
		{
			for(int i = 0; i < _Inputs.Count; i++)
			{
				if(stream.Serializing)
				{
					var bytes = (_Inputs[i].WitScript ?? WitScript.Empty).ToBytes();
					stream.ReadWrite(ref bytes);
				}
				else
				{
					_Inputs[i].WitScript = WitScript.Load(stream);
				}
			}

			if(IsNull())
				throw new FormatException("Superfluous witness record");
		}
	}
}

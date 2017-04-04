using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace NBitcoin.Tests
{
	public class sigopcount_tests
	{
		[Fact]
		[Trait("Core", "Core")]
		public void GetSigOpCount()
		{
			// Test CScript::GetSigOpCount()
			Script s1 = new Script();
			Assert.Equal(s1.GetSigOpCount(false), 0U);
			Assert.Equal(s1.GetSigOpCount(true), 0U);

			uint160 dummy = new uint160(0);
			s1 = s1 + OpcodeType.OP_1 + dummy.ToBytes() + dummy.ToBytes() + OpcodeType.OP_2 + OpcodeType.OP_CHECKMULTISIG;
			Assert.Equal(s1.GetSigOpCount(true), 2U);
			s1 = s1 + OpcodeType.OP_IF + OpcodeType.OP_CHECKSIG + OpcodeType.OP_ENDIF;
			Assert.Equal(s1.GetSigOpCount(true), 3U);
			Assert.Equal(s1.GetSigOpCount(false), 21U);

			Script p2sh = PayToScriptHashTemplate.Instance.GenerateScriptPubKey(s1);
			Script scriptSig = PayToScriptHashTemplate.Instance.GenerateScriptSig(new[] { (Op)OpcodeType.OP_0 }, s1);
			Assert.Equal(p2sh.GetSigOpCount(scriptSig), 3U);

			PubKey[] keys = Enumerable.Range(0, 3).Select(_ => new Key(true).PubKey).ToArray();

			Script s2 = PayToMultiSigTemplate.Instance.GenerateScriptPubKey(1, keys);
			Assert.Equal(s2.GetSigOpCount(true), 3U);
			Assert.Equal(s2.GetSigOpCount(false), 20U);

			p2sh = PayToScriptHashTemplate.Instance.GenerateScriptPubKey(s2);
			Assert.Equal(p2sh.GetSigOpCount(true), 0U);
			Assert.Equal(p2sh.GetSigOpCount(false), 0U);
			Script scriptSig2 = new Script();
			scriptSig2 = scriptSig2 + OpcodeType.OP_1 + dummy.ToBytes() + dummy.ToBytes() + s2.ToBytes();
			Assert.Equal(p2sh.GetSigOpCount(scriptSig2), 3U);
		}

	}
}

using BenchmarkDotNet.Attributes;
using NBitcoin.DataEncoders;
using System.Collections.Generic;

namespace NBitcoin.Bench;

[MemoryDiagnoser]
public class UtilsBench
{
	private static readonly byte[] taprootPubKeyA = Encoders.Hex.DecodeData("54c9e50dde41d5fc0b3dc0ff1ed4f6603a493d0a50dcc1badc3a5d959da7e750");
	private static readonly byte[] taprootPubKeyB = Encoders.Hex.DecodeData("7ca556cf2fc5ba848c5ede3c8f0605dc78c005244915ad42b4b15076eb4b2c43");

	public static IEnumerable<object[]> ArrayEqualArgs
	{
		get
		{
			yield return new byte[][] { [], [] }; // No bytes.
			yield return new byte[][] { [], [0x01] }; // Empty array against non-empty array.
			yield return new byte[][] { [0x01], [0x02] }; // 1-byte arrays which are not equal.
			yield return new byte[][] { [0x01, 0x02], [0x01, 0x03] }; // 2-byte arrays which are not equal.
			yield return new byte[][] { [0x01, 0x02, 0x03], [0x02, 0x03, 0x04] }; // 3-byte arrays which are not equal.
			yield return new byte[][] { [0x01, 0x02, 0x03, 0x04, 0x05], [0x01, 0x02, 0x03, 0x04, 0x05] }; // 5-byte arrays which are equal.
			yield return new byte[][] { [0x01, 0x02, 0x03, 0x04, 0x06, 0x05], [0x01, 0x02, 0x03, 0x04, 0x05, 0x06] }; // 6-byte arrays which are not equal.
			yield return new byte[][] { [.. taprootPubKeyA], [.. taprootPubKeyA] }; // Create a copy to avoid reference equality
			yield return new byte[][] { [.. taprootPubKeyA], [.. taprootPubKeyB] };
		}
	}

	[Benchmark]
	[ArgumentsSource(nameof(ArrayEqualArgs))]
	public bool ArrayEqual(byte[] a, byte[] b)
	{
		return Utils.ArrayEqual(a, b);
	}
}

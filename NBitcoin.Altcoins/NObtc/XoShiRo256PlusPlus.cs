namespace NBitcoin.Altcoins
{
    public class XoShiRo256PlusPlus
		{

			private ulong[] s = new ulong[4];
			// uint256 seed -> parent block hash
			public XoShiRo256PlusPlus(uint256 seed) => Reset(seed);

			public void Reset(uint256 seed) {
				s[0] = seed.pn0;
				s[1] = seed.pn1;
				s[2] = seed.pn2;
				s[3] = seed.pn3;
			}

			public ulong GetNext() {
				ulong res = RotateLeft64(s[0] + s[3], 23) + s[0];

				ulong t = s[1] << 17;

				s[2] ^= s[0];
				s[3] ^= s[1];
				s[1] ^= s[2];
				s[0] ^= s[3];

				s[2] ^= t;

				s[3] = RotateLeft64(s[3], 45);

				return res;
			}

			private static ulong RotateLeft64(ulong x, int k){
				return (x << k) | (x >> (64 - k));
			}
		}
}
﻿using System;

using NBitcoin.BouncyCastle.Crypto.Macs;
using NBitcoin.BouncyCastle.Crypto.Parameters;
using NBitcoin.BouncyCastle.Math;
using NBitcoin.BouncyCastle.Security;
using NBitcoin.BouncyCastle.Utilities;

namespace NBitcoin.BouncyCastle.Crypto.Signers
{
	/**
     * A deterministic K calculator based on the algorithm in section 3.2 of RFC 6979.
     */
	internal class HMacDsaKCalculator
		: IDsaKCalculator
	{
		private readonly HMac hMac;
		private readonly byte[] K;
		private readonly byte[] V;

		private BigInteger n;

		/**
         * Base constructor.
         *
         * @param digest digest to build the HMAC on.
         */
		public HMacDsaKCalculator(IDigest digest)
		{
			this.hMac = new HMac(digest);
			this.V = new byte[hMac.GetMacSize()];
			this.K = new byte[hMac.GetMacSize()];
		}

		public virtual bool IsDeterministic
		{
			get
			{
				return true;
			}
		}

		public virtual void Init(BigInteger n, SecureRandom random)
		{
			throw new InvalidOperationException("Operation not supported");
		}

		public void Init(BigInteger n, BigInteger d, byte[] message)
		{
			this.n = n;

			Arrays.Fill(V, (byte)0x01);
			Arrays.Fill(K, (byte)0);

			byte[] x = new byte[(n.BitLength + 7) / 8];
			byte[] dVal = BigIntegers.AsUnsignedByteArray(d);

			Array.Copy(dVal, 0, x, x.Length - dVal.Length, dVal.Length);

			byte[] m = new byte[(n.BitLength + 7) / 8];

			BigInteger mInt = BitsToInt(message);

			if(mInt.CompareTo(n) >= 0)
			{
				mInt = mInt.Subtract(n);
			}

			byte[] mVal = BigIntegers.AsUnsignedByteArray(mInt);

			Array.Copy(mVal, 0, m, m.Length - mVal.Length, mVal.Length);

			hMac.Init(new KeyParameter(K));

			hMac.BlockUpdate(V, 0, V.Length);
			hMac.Update((byte)0x00);
			hMac.BlockUpdate(x, 0, x.Length);
			hMac.BlockUpdate(m, 0, m.Length);

			hMac.DoFinal(K, 0);

			hMac.Init(new KeyParameter(K));

			hMac.BlockUpdate(V, 0, V.Length);

			hMac.DoFinal(V, 0);

			hMac.BlockUpdate(V, 0, V.Length);
			hMac.Update((byte)0x01);
			hMac.BlockUpdate(x, 0, x.Length);
			hMac.BlockUpdate(m, 0, m.Length);

			hMac.DoFinal(K, 0);

			hMac.Init(new KeyParameter(K));

			hMac.BlockUpdate(V, 0, V.Length);

			hMac.DoFinal(V, 0);
		}

		public virtual BigInteger NextK()
		{
			byte[] t = new byte[((n.BitLength + 7) / 8)];

			for(;;)
			{
				int tOff = 0;

				while(tOff < t.Length)
				{
					hMac.BlockUpdate(V, 0, V.Length);

					hMac.DoFinal(V, 0);

					int len = System.Math.Min(t.Length - tOff, V.Length);
					Array.Copy(V, 0, t, tOff, len);
					tOff += len;
				}

				BigInteger k = BitsToInt(t);

				if(k.SignValue > 0 && k.CompareTo(n) < 0)
				{
					return k;
				}

				hMac.BlockUpdate(V, 0, V.Length);
				hMac.Update((byte)0x00);

				hMac.DoFinal(K, 0);

				hMac.Init(new KeyParameter(K));

				hMac.BlockUpdate(V, 0, V.Length);

				hMac.DoFinal(V, 0);
			}
		}

		private BigInteger BitsToInt(byte[] t)
		{
			BigInteger v = new BigInteger(1, t);

			if(t.Length * 8 > n.BitLength)
			{
				v = v.ShiftRight(t.Length * 8 - n.BitLength);
			}

			return v;
		}
	}
}

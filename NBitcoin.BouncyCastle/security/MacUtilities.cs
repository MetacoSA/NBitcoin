using System.Collections;

using NBitcoin.BouncyCastle.Asn1;
using NBitcoin.BouncyCastle.Asn1.Iana;
using NBitcoin.BouncyCastle.Asn1.Pkcs;
using NBitcoin.BouncyCastle.Crypto;
using NBitcoin.BouncyCastle.Crypto.Engines;
using NBitcoin.BouncyCastle.Crypto.Macs;
using NBitcoin.BouncyCastle.Crypto.Paddings;
using NBitcoin.BouncyCastle.Utilities;
using System;

namespace NBitcoin.BouncyCastle.Security
{
    /// <remarks>
    ///  Utility class for creating HMac object from their names/Oids
    /// </remarks>
    public sealed class MacUtilities
    {
        private MacUtilities()
        {
        }

        private static readonly IDictionary algorithms = Platform.CreateHashtable();
        //private static readonly IDictionary oids = Platform.CreateHashtable();

        static MacUtilities()
        {
            algorithms[IanaObjectIdentifiers.HmacMD5.Id] = "HMAC-MD5";
            algorithms[IanaObjectIdentifiers.HmacRipeMD160.Id] = "HMAC-RIPEMD160";
            algorithms[IanaObjectIdentifiers.HmacSha1.Id] = "HMAC-SHA1";
            algorithms[IanaObjectIdentifiers.HmacTiger.Id] = "HMAC-TIGER";

            algorithms[PkcsObjectIdentifiers.IdHmacWithSha1.Id] = "HMAC-SHA1";
            algorithms[PkcsObjectIdentifiers.IdHmacWithSha224.Id] = "HMAC-SHA224";
            algorithms[PkcsObjectIdentifiers.IdHmacWithSha256.Id] = "HMAC-SHA256";
            algorithms[PkcsObjectIdentifiers.IdHmacWithSha384.Id] = "HMAC-SHA384";
            algorithms[PkcsObjectIdentifiers.IdHmacWithSha512.Id] = "HMAC-SHA512";

            // TODO AESMAC?

            algorithms["DES"] = "DESMAC";
            algorithms["DES/CFB8"] = "DESMAC/CFB8";
            algorithms["DES64"] = "DESMAC64";
            algorithms["DESEDE"] = "DESEDEMAC";
            algorithms[PkcsObjectIdentifiers.DesEde3Cbc.Id] = "DESEDEMAC";
            algorithms["DESEDE/CFB8"] = "DESEDEMAC/CFB8";
            algorithms["DESISO9797MAC"] = "DESWITHISO9797";
            algorithms["DESEDE64"] = "DESEDEMAC64";

            algorithms["DESEDE64WITHISO7816-4PADDING"] = "DESEDEMAC64WITHISO7816-4PADDING";
            algorithms["DESEDEISO9797ALG1MACWITHISO7816-4PADDING"] = "DESEDEMAC64WITHISO7816-4PADDING";
            algorithms["DESEDEISO9797ALG1WITHISO7816-4PADDING"] = "DESEDEMAC64WITHISO7816-4PADDING";

            algorithms["ISO9797ALG3"] = "ISO9797ALG3MAC";
            algorithms["ISO9797ALG3MACWITHISO7816-4PADDING"] = "ISO9797ALG3WITHISO7816-4PADDING";

            algorithms["SKIPJACK"] = "SKIPJACKMAC";
            algorithms["SKIPJACK/CFB8"] = "SKIPJACKMAC/CFB8";
            algorithms["IDEA"] = "IDEAMAC";
            algorithms["IDEA/CFB8"] = "IDEAMAC/CFB8";
            algorithms["RC2"] = "RC2MAC";
            algorithms["RC2/CFB8"] = "RC2MAC/CFB8";
            algorithms["RC5"] = "RC5MAC";
            algorithms["RC5/CFB8"] = "RC5MAC/CFB8";
            algorithms["GOST28147"] = "GOST28147MAC";
            algorithms["VMPC"] = "VMPCMAC";
            algorithms["VMPC-MAC"] = "VMPCMAC";
            algorithms["SIPHASH"] = "SIPHASH-2-4";

            algorithms["PBEWITHHMACSHA"] = "PBEWITHHMACSHA1";
            algorithms["1.3.14.3.2.26"] = "PBEWITHHMACSHA1";
        }

//		/// <summary>
//		/// Returns a ObjectIdentifier for a given digest mechanism.
//		/// </summary>
//		/// <param name="mechanism">A string representation of the digest meanism.</param>
//		/// <returns>A DerObjectIdentifier, null if the Oid is not available.</returns>
//		public static DerObjectIdentifier GetObjectIdentifier(
//			string mechanism)
//		{
//			mechanism = (string) algorithms[Platform.ToUpperInvariant(mechanism)];
//
//			if (mechanism != null)
//			{
//				return (DerObjectIdentifier)oids[mechanism];
//			}
//
//			return null;
//		}

//		public static ICollection Algorithms
//		{
//			get { return oids.Keys; }
//		}

        public static IMac GetMac(
            DerObjectIdentifier id)
        {
            return GetMac(id.Id);
        }

        public static IMac GetMac(
            string algorithm)
        {
            string upper = Platform.ToUpperInvariant(algorithm);

            string mechanism = (string) algorithms[upper];

            if (mechanism == null)
            {
                mechanism = upper;
            }

            if (mechanism.StartsWith("PBEWITH", StringComparison.OrdinalIgnoreCase))
            {
                mechanism = mechanism.Substring("PBEWITH".Length);
            }

			if(mechanism.StartsWith("HMAC", StringComparison.OrdinalIgnoreCase))
            {
                string digestName;
                if (mechanism.StartsWith("HMAC-") || mechanism.StartsWith("HMAC/"))
                {
                    digestName = mechanism.Substring(5);
                }
                else
                {
                    digestName = mechanism.Substring(4);
                }

                return new HMac(DigestUtilities.GetDigest(digestName));
            }

            if (mechanism.Equals("AESCMAC", StringComparison.OrdinalIgnoreCase))
            {
                return new CMac(new AesFastEngine());
            }
            if (mechanism.Equals("DESMAC", StringComparison.OrdinalIgnoreCase))
            {
                return new CbcBlockCipherMac(new DesEngine());
            }
            if (mechanism.Equals("DESMAC/CFB8", StringComparison.OrdinalIgnoreCase))
            {
                return new CfbBlockCipherMac(new DesEngine());
            }
            if (mechanism.Equals("DESMAC64", StringComparison.OrdinalIgnoreCase))
            {
                return new CbcBlockCipherMac(new DesEngine(), 64);
            }
            if (mechanism.Equals("DESEDECMAC", StringComparison.OrdinalIgnoreCase))
            {
                return new CMac(new DesEdeEngine());
            }
            if (mechanism.Equals("DESEDEMAC", StringComparison.OrdinalIgnoreCase))
            {
                return new CbcBlockCipherMac(new DesEdeEngine());
            }
            if (mechanism.Equals("DESEDEMAC/CFB8", StringComparison.OrdinalIgnoreCase))
            {
                return new CfbBlockCipherMac(new DesEdeEngine());
            }
            if (mechanism.Equals("DESEDEMAC64", StringComparison.OrdinalIgnoreCase))
            {
                return new CbcBlockCipherMac(new DesEdeEngine(), 64);
            }
            if (mechanism.Equals("DESEDEMAC64WITHISO7816-4PADDING", StringComparison.OrdinalIgnoreCase))
            {
                return new CbcBlockCipherMac(new DesEdeEngine(), 64, new ISO7816d4Padding());
            }
            if (mechanism.Equals("DESWITHISO9797", StringComparison.OrdinalIgnoreCase)
                || mechanism.Equals("ISO9797ALG3MAC", StringComparison.OrdinalIgnoreCase))
            {
                return new ISO9797Alg3Mac(new DesEngine());
            }
            if (mechanism.Equals("ISO9797ALG3WITHISO7816-4PADDING", StringComparison.OrdinalIgnoreCase))
            {
                return new ISO9797Alg3Mac(new DesEngine(), new ISO7816d4Padding());
            }
            if (mechanism.Equals("SKIPJACKMAC", StringComparison.OrdinalIgnoreCase))
            {
                return new CbcBlockCipherMac(new SkipjackEngine());
            }
            if (mechanism.Equals("SKIPJACKMAC/CFB8", StringComparison.OrdinalIgnoreCase))
            {
                return new CfbBlockCipherMac(new SkipjackEngine());
            }
            if (mechanism.Equals("IDEAMAC", StringComparison.OrdinalIgnoreCase))
            {
                return new CbcBlockCipherMac(new IdeaEngine());
            }
            if (mechanism.Equals("IDEAMAC/CFB8", StringComparison.OrdinalIgnoreCase))
            {
                return new CfbBlockCipherMac(new IdeaEngine());
            }
            if (mechanism.Equals("RC2MAC", StringComparison.OrdinalIgnoreCase))
            {
                return new CbcBlockCipherMac(new RC2Engine());
            }
            if (mechanism.Equals("RC2MAC/CFB8", StringComparison.OrdinalIgnoreCase))
            {
                return new CfbBlockCipherMac(new RC2Engine());
            }
            if (mechanism.Equals("RC5MAC", StringComparison.OrdinalIgnoreCase))
            {
                return new CbcBlockCipherMac(new RC532Engine());
            }
            if (mechanism.Equals("RC5MAC/CFB8", StringComparison.OrdinalIgnoreCase))
            {
                return new CfbBlockCipherMac(new RC532Engine());
            }
            if (mechanism.Equals("GOST28147MAC", StringComparison.OrdinalIgnoreCase))
            {
                return new Gost28147Mac();
            }
            if (mechanism.Equals("VMPCMAC", StringComparison.OrdinalIgnoreCase))
            {
                return new VmpcMac();
            }
            if (mechanism.Equals("SIPHASH-2-4", StringComparison.OrdinalIgnoreCase))
            {
                return new SipHash();
            }
            throw new SecurityUtilityException("Mac " + mechanism + " not recognised.");
        }

        public static string GetAlgorithmName(
            DerObjectIdentifier oid)
        {
            return (string) algorithms[oid.Id];
        }

        public static byte[] DoFinal(IMac mac)
        {
            byte[] b = new byte[mac.GetMacSize()];
            mac.DoFinal(b, 0);
            return b;
        }

        public static byte[] DoFinal(IMac mac, byte[] input)
        {
            mac.BlockUpdate(input, 0, input.Length);
            return DoFinal(mac);
        }
    }
}

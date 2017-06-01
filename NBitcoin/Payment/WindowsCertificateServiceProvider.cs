#if WIN
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace NBitcoin.Payment
{
	public class WindowsCertificateServiceProvider : ICertificateServiceProvider
	{
		public class WindowsHashChecker : ISignatureChecker
		{
			#region IHashChecker Members

			public bool VerifySignature(byte[] certificate, byte[] hash, string hashOID, byte[] signature)
			{
				try
				{
					return ((RSACryptoServiceProvider)new X509Certificate2(certificate).PublicKey.Key).VerifyHash(hash, hashOID, signature);
				}
				catch(CryptographicException)
				{
					return false;
				}
			}

			#endregion
		}
		public class WindowsSigner : ISigner
		{
			#region ISigner Members

			public byte[] Sign(byte[] certificate, byte[] hash, string hashOID)
			{
				return Sign(new X509Certificate2(certificate), hash, hashOID);
			}

			public static byte[] Sign(X509Certificate2 certificate, byte[] hash, string hashOID)
			{
				var privateKey = certificate.PrivateKey as RSACryptoServiceProvider;
				if(privateKey == null)
					throw new ArgumentException("Private key not present in the certificate, impossible to sign");
				return privateKey.SignHash(hash, hashOID);
			}

			public byte[] StripPrivateKey(byte[] certificate)
			{
				return StripPrivateKey(new X509Certificate2(certificate));
			}
			public byte[] StripPrivateKey(X509Certificate2 certificate)
			{
				return new X509Certificate2(certificate.Export(X509ContentType.Cert)).GetRawCertData();
			}

			#endregion

			#region ISigner Members

			public byte[] Sign(object certificate, byte[] hash, string hashOID)
			{
				if(certificate is byte[])
					return Sign((byte[])certificate, hash, hashOID);
				if(certificate is X509Certificate2)
					return Sign((X509Certificate2)certificate, hash, hashOID);
				throw new NotSupportedException("Certificate object's type is not supported");
			}

			public byte[] StripPrivateKey(object certificate)
			{
				if(certificate is byte[])
					return StripPrivateKey((byte[])certificate);
				if(certificate is X509Certificate2)
					return StripPrivateKey((X509Certificate2)certificate);
				throw new NotSupportedException("Certificate object's type is not supported");
			}

			#endregion
		}
		public class WindowsChainChecker : IChainChecker
		{
			public WindowsChainChecker()
			{
				VerificationFlags = X509VerificationFlags.NoFlag;
				RevocationMode = X509RevocationMode.Online;
			}
			public X509VerificationFlags VerificationFlags
			{
				get;
				set;
			}

			public X509RevocationMode RevocationMode
			{
				get;
				set;
			}

			#region IChainChecker Members

			public bool VerifyChain(byte[] certificate, byte[][] additionalCertificates)
			{
				X509Chain chain;
				return VerifyChain(out chain, new X509Certificate2(certificate), additionalCertificates.Select(c => new X509Certificate2(c)).ToArray());
			}

			public bool VerifyChain(out X509Chain chain, X509Certificate2 certificate, X509Certificate2[] additionalCertificates)
			{
				chain = new X509Chain();
				chain.ChainPolicy.VerificationFlags = VerificationFlags;
				chain.ChainPolicy.RevocationMode = RevocationMode;
				foreach(var additional in additionalCertificates)
					chain.ChainPolicy.ExtraStore.Add(additional);
				return chain.Build(certificate);
			}

			#endregion
		}


		/// <summary>
		/// Get the certificate name from the certificate subject
		/// </summary>
		public static string GetCertificateName(X509Certificate2 cert)
		{
			if(cert == null)
				return null;
			if(!string.IsNullOrEmpty(cert.FriendlyName))
				return cert.FriendlyName;
			else
			{
				var match = Regex.Match(cert.Subject, "^(CN=)?(?<Name>[^,]*)", RegexOptions.IgnoreCase);
				if(!match.Success)
					return cert.Subject;
				return match.Groups["Name"].Value.Trim();
			}
		}

		readonly X509VerificationFlags _VerificationFlags;
		public X509VerificationFlags VerificationFlags
		{
			get
			{
				return _VerificationFlags;
			}
		}
		private readonly X509RevocationMode _RevocationMode;
		public X509RevocationMode RevocationMode
		{
			get
			{
				return _RevocationMode;
			}
		}
		public WindowsCertificateServiceProvider(X509VerificationFlags verificationFlags = X509VerificationFlags.NoFlag,
												 X509RevocationMode revocationMode = X509RevocationMode.Online)
		{
			_VerificationFlags = verificationFlags;
			_RevocationMode = revocationMode;
		}
		#region ICertificateServiceProvider Members

		public IChainChecker GetChainChecker()
		{
			return new WindowsChainChecker()
			{
				VerificationFlags = _VerificationFlags,
				RevocationMode = _RevocationMode
			};
		}

		public ISignatureChecker GetSignatureChecker()
		{
			return new WindowsHashChecker();
		}

		public ISigner GetSigner()
		{
			return new WindowsSigner();
		}

		#endregion
	}
}
#endif
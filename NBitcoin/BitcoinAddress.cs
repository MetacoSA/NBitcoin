using System;
using System.Linq;
using NBitcoin.DataEncoders;

namespace NBitcoin
{
    /// <summary>
    /// Base58 representaiton of a script hash
    /// </summary>
    public class BitcoinScriptAddress : BitcoinAddress
    {
        public BitcoinScriptAddress(string address, Network expectedNetwork)
            : base(address, expectedNetwork)
        {
        }

        public BitcoinScriptAddress(ScriptId scriptId, Network network)
            : base(scriptId, network)
        {
        }

        protected override bool IsValid
        {
            get
            {
                return vchData.Length == 20;
            }
        }

        public ScriptId Hash
        {
            get
            {
                return new ScriptId(vchData);
            }
        }

        public override Base58Type Type
        {
            get
            {
                return Base58Type.SCRIPT_ADDRESS;
            }
        }

        protected override Script GeneratePaymentScript()
        {
            return PayToScriptHashTemplate.Instance.GenerateScriptPubKey((ScriptId)Hash);
        }
    }

    /// <summary>
    /// Base58 representation of a bitcoin address
    /// </summary>
    public abstract class BitcoinAddress : IDestination, IWalletData
    {
        /// <summary>
        /// Detect whether the input base58 is a pubkey hash or a script hash
        /// </summary>
        /// <param name="base58">The Base58 string to parse</param>
        /// <param name="expectedNetwork">The expected network to which it belongs</param>
        /// <returns>A BitcoinAddress or BitcoinScriptAddress</returns>
        /// <exception cref="System.FormatException">Invalid format</exception>
        public static BitcoinAddress Create(string base58, Network expectedNetwork = null)
        {
            if (base58 == null)
                throw new ArgumentNullException("base58");
            return Network.CreateFromBase58Data<BitcoinAddress>(base58, expectedNetwork);
        }

        protected byte[] vchData = new byte[0];
        protected byte[] vchVersion = new byte[0];
        protected string wifData = "";
        private Network _Network;
        public Network Network
        {
            get
            {
                return _Network;
            }
        }

        protected virtual bool IsValid
        {
            get
            {
                return true;
            }
        }

        public abstract Base58Type Type
        {
            get;
        }

        public string ToWif()
        {
            return wifData;
        }
        public byte[] ToBytes()
        {
            return vchData.ToArray();
        }
        public override string ToString()
        {
            return wifData;
        }

        public override bool Equals(object obj)
        {
            Base58Data item = obj as Base58Data;
            if (item == null)
                return false;
            return ToString().Equals(item.ToString());
        }

        public static bool operator ==(BitcoinAddress a, BitcoinAddress b)
        {
            if (System.Object.ReferenceEquals(a, b))
                return true;
            if (((object)a == null) || ((object)b == null))
                return false;
            return a.ToString() == b.ToString();
        }

        public static bool operator !=(BitcoinAddress a, BitcoinAddress b)
        {
            return !(a == b);
        }

        public override int GetHashCode()
        {
            return ToString().GetHashCode();
        }

        private void SetString(string psz)
        {
            if (_Network == null)
            {
                _Network = Network.GetNetworkFromBase58Data(psz, Type);
                if (_Network == null)
                    throw new FormatException("Invalid " + this.GetType().Name);
            }

            byte[] vchTemp = Encoders.Base58Check.DecodeData(psz);
            var expectedVersion = _Network.GetVersionBytes(Type);


            vchVersion = vchTemp.SafeSubarray(0, expectedVersion.Length);
            if (!Utils.ArrayEqual(vchVersion, expectedVersion))
                throw new FormatException("The version prefix does not match the expected one " + String.Join(",", expectedVersion));

            vchData = vchTemp.SafeSubarray(expectedVersion.Length);
            wifData = psz;

            if (!IsValid)
                throw new FormatException("Invalid " + this.GetType().Name);
        }

        private void SetData(byte[] vchData)
        {
            this.vchData = vchData;
            this.vchVersion = _Network.GetVersionBytes(Type);
            wifData = Encoders.Base58Check.EncodeData(vchVersion.Concat(vchData).ToArray());

            if (!IsValid)
                throw new FormatException("Invalid " + this.GetType().Name);
        }

        protected BitcoinAddress(string base58, Network expectedNetwork = null)
        {
            _Network = expectedNetwork;
            SetString(base58);
        }

        protected BitcoinAddress(TxDestination id, Network network)
            : this(id.ToBytes(), network)
        {
        }

        protected BitcoinAddress(byte[] rawBytes, Network network)
        {
            if (network == null)
                throw new ArgumentNullException("network");

            _Network = network;
            SetData(rawBytes);
        }

        Script _ScriptPubKey;
        public Script ScriptPubKey
        {
            get
            {
                if (_ScriptPubKey == null)
                {
                    _ScriptPubKey = GeneratePaymentScript();
                }
                return _ScriptPubKey;
            }
        }

        protected abstract Script GeneratePaymentScript();

        public BitcoinScriptAddress GetScriptAddress()
        {
            var bitcoinScriptAddress = this as BitcoinScriptAddress;
            if (bitcoinScriptAddress != null)
                return bitcoinScriptAddress;

            return new BitcoinScriptAddress(this.ScriptPubKey.Hash, Network);
        }

        public BitcoinColoredAddress ToColoredAddress()
        {
            return new BitcoinColoredAddress(this);
        }
    }
}

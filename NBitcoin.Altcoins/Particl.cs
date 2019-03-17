using NBitcoin;
using NBitcoin.DataEncoders;
using NBitcoin.Protocol;
using NBitcoin.RPC;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;

namespace NBitcoin.Altcoins
{
    public class Particl : NetworkSetBase
    {
        public static Particl Instance { get; } = new Particl();

        public override string CryptoCode => "PART";

        private Particl()
        {

        }

        static uint ANON_MARKER = 0xffffffa0;

        //Format visual studio
        //{({.*?}), (.*?)}
        //Tuple.Create(new byte[]$1, $2)
        static Tuple<byte[], int>[] pnSeed6_main = {
    Tuple.Create(new byte[]{0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0xff,0xff,0x0d,0x4e,0xbc,0x09}, 51738),
    Tuple.Create(new byte[]{0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0xff,0xff,0x0d,0x5d,0x73,0x84}, 51738),
    Tuple.Create(new byte[]{0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0xff,0xff,0x12,0xdd,0x72,0x07}, 51738),
    Tuple.Create(new byte[]{0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0xff,0xff,0x17,0x66,0xac,0x83}, 51738),
    Tuple.Create(new byte[]{0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0xff,0xff,0x23,0xc4,0x30,0x74}, 51738),
    Tuple.Create(new byte[]{0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0xff,0xff,0x23,0xc4,0x58,0x7f}, 51738),
    Tuple.Create(new byte[]{0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0xff,0xff,0x23,0xe3,0x11,0xc4}, 51738),
    Tuple.Create(new byte[]{0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0xff,0xff,0x25,0xbb,0x65,0x4b}, 51738),
    Tuple.Create(new byte[]{0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0xff,0xff,0x28,0x41,0x74,0xb1}, 51738),
    Tuple.Create(new byte[]{0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0xff,0xff,0x2d,0x4c,0xed,0x7e}, 51738),
    Tuple.Create(new byte[]{0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0xff,0xff,0x2d,0x4d,0x96,0x48}, 51738),
    Tuple.Create(new byte[]{0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0xff,0xff,0x2e,0x26,0xf0,0x20}, 51738),
    Tuple.Create(new byte[]{0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0xff,0xff,0x34,0x0e,0xa1,0x96}, 51738),
    Tuple.Create(new byte[]{0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0xff,0xff,0x34,0xe0,0xa2,0xa9}, 51738),
    Tuple.Create(new byte[]{0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0xff,0xff,0x34,0xe9,0xc7,0x24}, 51738),
    Tuple.Create(new byte[]{0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0xff,0xff,0x36,0x25,0x0d,0x02}, 51738),
    Tuple.Create(new byte[]{0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0xff,0xff,0x44,0x6d,0x45,0x94}, 51738),
    Tuple.Create(new byte[]{0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0xff,0xff,0x4b,0xbc,0x32,0x9f}, 51738),
    Tuple.Create(new byte[]{0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0xff,0xff,0x50,0xd0,0xe6,0x67}, 51738),
    Tuple.Create(new byte[]{0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0xff,0xff,0x52,0x12,0xfa,0x7d}, 51738),
    Tuple.Create(new byte[]{0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0xff,0xff,0x55,0xbb,0xb7,0xd8}, 51738),
    Tuple.Create(new byte[]{0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0xff,0xff,0x59,0x46,0xe2,0xce}, 51738),
    Tuple.Create(new byte[]{0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0xff,0xff,0x59,0x67,0x2c,0x33}, 51738),
    Tuple.Create(new byte[]{0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0xff,0xff,0x59,0xdd,0xd8,0x1c}, 51738),
    Tuple.Create(new byte[]{0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0xff,0xff,0x5c,0xf3,0x03,0x3b}, 51738),
    Tuple.Create(new byte[]{0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0xff,0xff,0x68,0xa8,0x8f,0x78}, 51738),
    Tuple.Create(new byte[]{0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0xff,0xff,0x81,0xaa,0x1c,0x6f}, 51738),
    Tuple.Create(new byte[]{0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0xff,0xff,0x8e,0x69,0xc9,0xee}, 51738),
    Tuple.Create(new byte[]{0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0xff,0xff,0x90,0xca,0x44,0x93}, 51738),
    Tuple.Create(new byte[]{0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0xff,0xff,0x90,0xd9,0x82,0x21}, 51738),
    Tuple.Create(new byte[]{0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0xff,0xff,0x93,0x4b,0x52,0x59}, 51738),
    Tuple.Create(new byte[]{0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0xff,0xff,0x93,0x87,0xbf,0xa2}, 51738),
    Tuple.Create(new byte[]{0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0xff,0xff,0x93,0xaf,0xbb,0x6f}, 51738),
    Tuple.Create(new byte[]{0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0xff,0xff,0x9f,0x59,0x04,0x32}, 51738),
    Tuple.Create(new byte[]{0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0xff,0xff,0xa3,0xac,0x2a,0x43}, 51738),
    Tuple.Create(new byte[]{0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0xff,0xff,0xb0,0x09,0x69,0x1b}, 51738),
    Tuple.Create(new byte[]{0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0xff,0xff,0xc6,0xc7,0x6f,0x7f}, 51738),
    Tuple.Create(new byte[]{0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0xff,0xff,0xc6,0xcc,0xfb,0x4a}, 51738),
    Tuple.Create(new byte[]{0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0xff,0xff,0xc7,0xf7,0x11,0xb2}, 51738),
    Tuple.Create(new byte[]{0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0xff,0xff,0xcf,0x9a,0xdc,0x6a}, 51738),
    Tuple.Create(new byte[]{0x20,0x01,0x19,0xf0,0x64,0x01,0x0b,0x6d,0x54,0x00,0x01,0xff,0xfe,0x63,0xcc,0xd4}, 51738),
    Tuple.Create(new byte[]{0x20,0x01,0x41,0xd0,0x00,0x0a,0x25,0x4b,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x01}, 51738),
    Tuple.Create(new byte[]{0x20,0x01,0x4b,0x98,0x0d,0xc0,0x00,0x41,0x02,0x16,0x3e,0xff,0xfe,0xa0,0xba,0x2a}, 51738),
    Tuple.Create(new byte[]{0x20,0x01,0x4b,0x98,0x0d,0xc0,0x00,0x41,0x02,0x16,0x3e,0xff,0xfe,0xa3,0xad,0x23}, 51738),
    Tuple.Create(new byte[]{0x20,0x02,0x68,0xa8,0x8f,0x78,0x00,0x00,0x00,0x00,0x00,0x00,0x68,0xa8,0x8f,0x78}, 51738),
    Tuple.Create(new byte[]{0x26,0x04,0x13,0x80,0x20,0x00,0x4d,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x03}, 51738),
    Tuple.Create(new byte[]{0x26,0x05,0x60,0x00,0xf3,0x84,0x1a,0x01,0x1c,0x54,0x07,0x02,0x75,0xfd,0x79,0xc4}, 51738),
    Tuple.Create(new byte[]{0x26,0x07,0xfc,0xc8,0xff,0xc0,0x00,0xc1,0x41,0x21,0xc9,0x8d,0x69,0xf3,0x3d,0x33}, 51738),
    Tuple.Create(new byte[]{0x2a,0x01,0x0e,0x35,0x8a,0xaf,0x84,0x80,0xb5,0xa8,0xef,0x50,0x30,0xde,0x5f,0x44}, 51738),
    Tuple.Create(new byte[]{0x2a,0x02,0x7b,0x40,0x50,0xd0,0xe6,0x67,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x01}, 51738),
    Tuple.Create(new byte[]{0x2a,0x02,0x80,0x84,0x02,0x02,0xcc,0x00,0x1f,0xe7,0xc3,0x67,0xd0,0x63,0xeb,0xca}, 51738),
    Tuple.Create(new byte[]{0xfd,0x87,0xd8,0x7e,0xeb,0x43,0xe4,0x0e,0x3a,0x27,0xd7,0x62,0xb7,0x69,0x53,0xaa}, 51738),
    Tuple.Create(new byte[]{0xfd,0x87,0xd8,0x7e,0xeb,0x43,0xe4,0x70,0xd2,0x61,0xbd,0xae,0xf1,0x93,0xfc,0x2e}, 51738),
    Tuple.Create(new byte[]{0xfd,0x87,0xd8,0x7e,0xeb,0x43,0xfd,0x69,0x22,0x5f,0xc6,0xaf,0x48,0x48,0xc1,0x1a}, 51738),
    Tuple.Create(new byte[]{0xfd,0x87,0xd8,0x7e,0xeb,0x43,0x2c,0x56,0x53,0x1d,0x68,0x22,0xef,0x0d,0x34,0x58}, 51738)
};
        static Tuple<byte[], int>[] pnSeed6_test = {
    Tuple.Create(new byte[]{0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0xff,0xff,0x0d,0x4e,0xbc,0x09}, 51938),
    Tuple.Create(new byte[]{0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0xff,0xff,0x0d,0x5d,0x73,0x84}, 51938),
    Tuple.Create(new byte[]{0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0xff,0xff,0x0d,0x5e,0x95,0x16}, 51938),
    Tuple.Create(new byte[]{0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0xff,0xff,0x17,0x66,0xac,0x83}, 51938),
    Tuple.Create(new byte[]{0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0xff,0xff,0x34,0xe0,0xa2,0xa9}, 51938),
    Tuple.Create(new byte[]{0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0xff,0xff,0x34,0xe9,0xc7,0x24}, 51938),
    Tuple.Create(new byte[]{0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0xff,0xff,0x4c,0x4a,0xaa,0x43}, 51938),
    Tuple.Create(new byte[]{0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0xff,0xff,0xb9,0x53,0xda,0x97}, 51938)
};

#pragma warning disable CS0618 // Type or member is obsolete
        public class ParticlConsensusFactory : ConsensusFactory
        {
            private ParticlConsensusFactory()
            {
            }

            public static ParticlConsensusFactory Instance { get; } = new ParticlConsensusFactory();

            public override BlockHeader CreateBlockHeader()
            {
                return new ParticlBlockHeader();
            }
            public override Block CreateBlock()
            {
                return new ParticlBlock(new ParticlBlockHeader());
            }

            public override Transaction CreateTransaction()
            {
                return new ParticlTransaction(this);
            }

            public override bool TryCreateNew(Type type, out IBitcoinSerializable result)
            {
                if (typeof(TxIn).GetTypeInfo().IsAssignableFrom(type.GetTypeInfo()))
                {
                    result = new ParticlTxIn();
                    return true;
                }
                if (typeof(TxOut).GetTypeInfo().IsAssignableFrom(type.GetTypeInfo()))
                {
                    result = new ParticlTxOut();
                    return true;
                }
                return base.TryCreateNew(type, out result);
            }
        }

        public class ParticlBlockHeader : BlockHeader
        {
            protected uint256 hashWitnessMerkleRoot;

            public uint256 HashWitnessMerkleRoot
            {
                get
                {
                    return hashWitnessMerkleRoot;
                }
                set
                {
                    hashWitnessMerkleRoot = value;
                }
            }
            public override uint256 GetPoWHash()
            {
                var headerBytes = this.ToBytes();
                var h = NBitcoin.Crypto.SCrypt.ComputeDerivedKey(headerBytes, headerBytes, 1024, 1, 1, null, 32);
                return new uint256(h);
            }

            public override void ReadWrite(BitcoinStream stream)
            {
                stream.ReadWrite(ref nVersion);
                stream.ReadWrite(ref hashPrevBlock);
                stream.ReadWrite(ref hashMerkleRoot);
                stream.ReadWrite(ref hashWitnessMerkleRoot);
                stream.ReadWrite(ref nTime);
                stream.ReadWrite(ref nBits);
                stream.ReadWrite(ref nNonce);
            }
        }

        public class ParticlBlock : Block
        {
            public ParticlBlock(ParticlBlockHeader header) : base(header)
            {

            }

            public override ConsensusFactory GetConsensusFactory()
            {
                return ParticlConsensusFactory.Instance;
            }
        }

        public class ParticlTransaction : Transaction
        {
            public ParticlTransaction(ConsensusFactory consensusFactory)
            {
                _Factory = consensusFactory;
            }

            ConsensusFactory _Factory;
            public override ConsensusFactory GetConsensusFactory()
            {
                return _Factory;
            }
            
            protected new ushort nVersion = 1;

            public new ushort Version
            {
                get
                {
                    return nVersion;
                }
                set
                {
                    nVersion = value;
                }
            }
            public override void ReadWrite(BitcoinStream stream)
            {        
                stream.ReadWrite(ref nVersion);                    
                stream.ReadWriteStruct(ref nLockTime);

                stream.ReadWrite<TxInList, TxIn>(ref vin);
                vin.Transaction = this;

                stream.ReadWrite<TxOutList, TxOut>(ref vout);
                vout.Transaction = this;

                if (stream.Type != SerializationType.Hash) {
                    Witness wit = new Witness(Inputs);
                    try
                    {
                        wit.ReadWrite(stream);
                    } catch (FormatException e) {
                        Console.Out.WriteLine(e.Message);
                    }
                }
                
        
            }
        }

        public class ParticlTxIn : TxIn
        {
            byte[][] data = null;

            public override void ReadWrite(BitcoinStream stream)
            {
                if (!stream.Serializing)
                    prevout = null;
                stream.ReadWrite(ref prevout);
                stream.ReadWrite(ref scriptSig);
                stream.ReadWrite(ref nSequence);
                

                if (prevout.N == ANON_MARKER) {
                    uint stack_size = stream.Serializing ? (uint) data.Length : 0;
                    stream.ReadWriteAsVarInt(ref stack_size);

                    if (!stream.Serializing) {
                        data = new byte[stack_size][];
                    }

                    for (int k = 0; k < stack_size; k++)
                    {
                        uint data_size = stream.Serializing ? (uint) data[k].Length : 0;
                        stream.ReadWriteAsVarInt(ref data_size);

                        byte[] data_stack = stream.Serializing ? data[k] : new byte[data_size];
                        
                        if (data_size != 0) {
                            stream.ReadWrite(ref data_stack);
                        }

                        if (!stream.Serializing) {
                            data[k] = data_stack;
                        }
                    }
                }
            }
        }

        public class ParticlTxOut : TxOut
        {
            enum Type { OUTPUT_NULL, OUTPUT_STANDARD, OUTPUT_CT, OUTPUT_RINGCT, OUTPUT_DATA };

            byte type = 0;

            public override void ReadWrite(BitcoinStream stream)
            {
                stream.ReadWrite(ref type);

                uint data_size = 0;

                switch(type) {
                    case (byte)Type.OUTPUT_STANDARD:
                        long value = Value.Satoshi;
                        stream.ReadWrite(ref value);
                        if (!stream.Serializing)
                            _Value = new Money(value);
                        stream.ReadWrite(ref publicKey);
                        break;
                    case (byte)Type.OUTPUT_CT:
                        byte[] valueCommitment = new byte[33];
                        stream.ReadWrite(ref valueCommitment);

                        stream.ReadWriteAsVarInt(ref data_size);
                        if (data_size != 0) {
                            byte[] data = new byte[data_size];
                            stream.ReadWrite(ref data);
                        }

                        stream.ReadWriteAsVarInt(ref data_size);
                        if (data_size != 0) {
                            byte[] script = new byte[data_size];
                            stream.ReadWrite(ref script);
                        }

                        stream.ReadWriteAsVarInt(ref data_size);
                        if (data_size != 0) {
                            byte[] rangeProof = new byte[data_size];
                            stream.ReadWrite(ref rangeProof);
                        }

                        break;
                    case (byte)Type.OUTPUT_RINGCT:
                        byte[] pubkey = new byte[33];
                        stream.ReadWrite(ref pubkey);

                        byte[] valueCommitment2 = new byte[33];
                        stream.ReadWrite(ref valueCommitment2);

                        stream.ReadWriteAsVarInt(ref data_size);
                        if (data_size != 0) {
                            byte[] data = new byte[data_size];
                            stream.ReadWrite(ref data);
                        }

                        stream.ReadWriteAsVarInt(ref data_size);
                        if (data_size != 0) {
                            byte[] rangeProof = new byte[data_size];
                            stream.ReadWrite(ref rangeProof);
                        }
                        break;
                    case (byte)Type.OUTPUT_DATA:
                        stream.ReadWriteAsVarInt(ref data_size);
                        if (data_size != 0) {
                            byte[] data = new byte[data_size];
                            stream.ReadWrite(ref data);
                        }
                        break;
                    default:
                        break;
                }
            }
        }

        public class ParticlMainnetAddressStringParser : NetworkStringParser
        {
            public override bool TryParse<T>(string str, Network network, out T result)
            {
                if (str.StartsWith("XPAR", StringComparison.OrdinalIgnoreCase) && typeof(T) == typeof(BitcoinExtKey))
                {
                    try
                    {
                        var decoded = Encoders.Base58Check.DecodeData(str);
                        decoded[0] = 0x8f;
                        decoded[1] = 0x1d;
                        decoded[2] = 0xae;
                        decoded[3] = 0xb8;
                        result = (T)(object)new BitcoinExtKey(Encoders.Base58Check.EncodeData(decoded), network);
                        return true;
                    }
                    catch
                    {
                    }
                }
                if (str.StartsWith("PPAR", StringComparison.OrdinalIgnoreCase) && typeof(T) == typeof(BitcoinExtPubKey))
                {
                    try
                    {
                        var decoded = Encoders.Base58Check.DecodeData(str);
                        decoded[0] = 0x69;
                        decoded[1] = 0x6e;
                        decoded[2] = 0x82;
                        decoded[3] = 0xd1;
                        result = (T)(object)new BitcoinExtPubKey(Encoders.Base58Check.EncodeData(decoded), network);
                        return true;
                    }
                    catch
                    {
                    }
                }
                return base.TryParse(str, network, out result);
            }
        }

#pragma warning restore CS0618 // Type or member is obsolete

        protected override void PostInit()
        {
            RegisterDefaultCookiePath("Particl", new FolderName() { TestnetFolder = "testnet" });
        }

        protected override NetworkBuilder CreateMainnet()
        {
            NetworkBuilder builder = new NetworkBuilder();
            builder.SetConsensus(new Consensus()
            {
                SubsidyHalvingInterval = 210000,
                MajorityEnforceBlockUpgrade = 750,
                MajorityRejectBlockOutdated = 950,
                MajorityWindow = 1000,
                BIP34Hash = new uint256("0x000000000000024b89b42a942fe0d9fea3bb44ab7bd1b19115dd6a759c0808b8"),
                PowLimit = new Target(new uint256("00000000ffffffffffffffffffffffffffffffffffffffffffffffffffffffff")),
                PowTargetTimespan = TimeSpan.FromSeconds(24 * 60),
                PowTargetSpacing = TimeSpan.FromSeconds(120),
                PowAllowMinDifficultyBlocks = false,
                PowNoRetargeting = false,
                RuleChangeActivationThreshold = 1916,
                MinerConfirmationWindow = 2016,
                CoinbaseMaturity = 100,
                ConsensusFactory = ParticlConsensusFactory.Instance
            })
            .SetBase58Bytes(Base58Type.PUBKEY_ADDRESS, new byte[] { 0x38 }) // P
            .SetBase58Bytes(Base58Type.SCRIPT_ADDRESS, new byte[] { 0x3c })
            .SetBase58Bytes(Base58Type.SECRET_KEY, new byte[] { 0x6c })
            .SetBase58Bytes(Base58Type.EXT_PUBLIC_KEY, new byte[] { 0x69, 0x6e, 0x82, 0xd1 }) // PPAR
            .SetBase58Bytes(Base58Type.EXT_SECRET_KEY, new byte[] { 0x8f, 0x1d, 0xae, 0xb8 }) // XPAR
            .SetBase58Bytes(Base58Type.STEALTH_ADDRESS, new byte[] { 0x14 })
            .SetNetworkStringParser(new ParticlMainnetAddressStringParser())
            .SetMagic(0xb4eff2fb) 
            .SetPort(51738)
            .SetRPCPort(51735)
            .SetMaxP2PVersion(90007)
            .SetName("part-main")
            .AddAlias("part-mainnet")
            .AddAlias("particl-mainnet")
            .AddAlias("particl-main")
            .AddDNSSeeds(new[]
            {
                new DNSSeedData("mainnet-seed.particl.io", "mainnet-seed.particl.io"),
                new DNSSeedData("dnsseed-mainnet.particl.io", "dnsseed-mainnet.particl.io"),
                new DNSSeedData("mainnet.particl.io", "mainnet.particl.io"),
            })
            .AddSeeds(ToSeed(pnSeed6_main))
            .SetGenesis("a000000000000000000000000000000000000000000000000000000000000000000000003e1e4f55df2e3380279e208366721add3ef5b2e2591aeddf2dc04bcf23b05fc96cd65969e356228751f3d8be1e17233c2dc4d9bcb88e011d8a4cf0f9a7949e61d0b46c59ffff001fc57a000001a00100000000010000000000000000000000000000000000000000000000000000000000000000ffffffff4e04ffff001d01044442544320303030303030303030303030303030303030633637396263323230393637366430353132393833343632376337623163303264313031386232323463366633376a00ffffffff2201d63440a2010000001976a91462a62c80e0b41f2857ba83eb438d5caa46e36bcb88ac01fce41daa330000001976a914c515c636ae215ebba2a98af433a3fa6c74f8441588ac011faf2e07000000001976a914711b5e1fd0b0f4cdf92cb53b00061ef742dda4fb88ac0177cd1301000000001976a91420c17c53337d80408e0b488b5af7781320a0a31188ac01bd479991150000001976a914aba8c6f8dbcf4ecfb598e3c08e12321d884bfe0b88ac01c6b85af4d10200001976a9141f3277a84a18f822171d720f0132f698bcc370ca88ac01b69b0d4b6c0000001976a9148fff14bea695ffa6c8754a3e7d518f8c53c3979a88ac01e4b051c99b0000001976a914e54967b4067d91a777587c9f54ee36dd9f1947c488ac01ac3a95f1ba0000001976a9147744d2ac08f2e1d108b215935215a4e66d0262d288ac018d1387503e0000001976a914a55a17e86246ea21cb883c12c709476a09b4885c88ac018d1387503e0000001976a9144e00dce8ab44fd4cafa34839edf8f68ba783988188ac01fab8e6323b0000001976a914702cae5d2537bfdd5673ac986f910d6adb23510a88ac0164bf8163180100001976a914b19e494b0033c5608a7d153e57d7fdf3dfb51bb788ac01fc192564180100001976a9146909b0f1c94ea1979ed76e10a5a49ec795a8f49888ac0124d82728370000001976a91405a06af3b29dade9f304244d934381ac495646c188ac0174d04b1e240000001976a914557e2b3205719931e22853b27920d2ebd614753188ac01977be006000000001976a914ad16fb301bd21c60c5cb580b322aa2c61b6c5df288ac0189120801000000001976a914182c5cfb9d17aa8d8ff78940135ca8d822022f3288ac01c68943281f0000001976a914b8a374a75f6d44a0bd1bf052da014efe564ae41288ac01e475f8211f0000001976a914fadee7e2878172dad55068c8696621b1788dccb388ac01cf9c695e280000001976a914eacc4b108c28ed73b111ff149909aacffd2cdf7888ac0144c6900d390000001976a914dd87cc0b8e0fc119061f33f161104ce691d2365788ac01867e669e2e0000001976a9141c8b0435eda1d489e9f0a16d3b9d65182f88537788ac017ba2c48a650000001976a91415a724f2bc643041cb35c9475cd67b897d62ca5288ac018c1f5d59240000001976a914626f86e9033026be7afbb2b9dbe4972ef4b3e08588ac01981d055f190000001976a914a4a73d99269639541cb7e845a4c6ef3e3911fcd688ac011f3b5e661d0000001976a91427929b31f11471aa4b77ca74bb66409ff76d24a288ac01ccf8482c080000001976a9142d6248888c7f72cc88e4883e4afd1025c43a7f0e88ac012aa39eb2120000001976a91425d8debc253f5c3f70010f41c53348ed156e7baa88ac0100b401da2324000017a9145766354dcb13caff682ed9451b9fe5bbb786996c8701008a0700ef1a000017a9145766354dcb13caff682ed9451b9fe5bbb786996c870100a3c5df3f0e000017a9146e29c4a11fd54916d024af16ca913cdf8f89cb31870100daa532ad13000017a914727e5e75929bbf26912dd7833971d77e7450a33e870100a09eee955a00001e04004a1f5ab175a9149433643b4fd5de3ebd7fdd68675f978f34585af1870000");
            return builder;
        }

        protected override NetworkBuilder CreateTestnet()
        {
            NetworkBuilder builder = new NetworkBuilder();
            builder.SetConsensus(new Consensus()
            {
                SubsidyHalvingInterval = 210000,
                MajorityEnforceBlockUpgrade = 51,
                MajorityRejectBlockOutdated = 75,
                MajorityWindow = 144,
                PowLimit = new Target(new uint256("000000000005ffffffffffffffffffffffffffffffffffffffffffffffffffff")),
                PowTargetTimespan = TimeSpan.FromSeconds(14 * 24 * 60 * 60),
                PowTargetSpacing = TimeSpan.FromSeconds(10 * 60),
                PowAllowMinDifficultyBlocks = true,
                PowNoRetargeting = false,
                RuleChangeActivationThreshold = 1512,
                MinerConfirmationWindow = 2016,
                CoinbaseMaturity = 100,
                ConsensusFactory = ParticlConsensusFactory.Instance
            })
            .SetBase58Bytes(Base58Type.PUBKEY_ADDRESS, new byte[] { 0x76 }) // p
            .SetBase58Bytes(Base58Type.SCRIPT_ADDRESS, new byte[] { 0x7a })
            .SetBase58Bytes(Base58Type.SECRET_KEY, new byte[] { 0x2e })
            .SetBase58Bytes(Base58Type.EXT_PUBLIC_KEY, new byte[] { 0xe1, 0x42, 0x78, 0x00 }) // ppar
            .SetBase58Bytes(Base58Type.EXT_SECRET_KEY, new byte[] { 0x04, 0x88, 0x94, 0x78 }) // xpar
            .SetBase58Bytes(Base58Type.STEALTH_ADDRESS, new byte[] { 0x15 })
            .SetMagic(0x0b051108) 
            .SetPort(51938)
            .SetRPCPort(51935)
            .SetMaxP2PVersion(90007)
            .SetName("part-test")
            .AddAlias("part-testnet")
            .AddAlias("particl-testnet")
            .AddAlias("particl-test")
            .AddDNSSeeds(new[]
            {
                new DNSSeedData("testnet-seed.particl.io", "testnet-seed.particl.io"),
                new DNSSeedData("dnsseed-testnet.particl.io", "dnsseed-testnet.particl.io"),
            })
            .AddSeeds(ToSeed(pnSeed6_test))
            .SetGenesis("a0000000000000000000000000000000000000000000000000000000000000000000000002c0ee0829a772341d64f3dfe4726166d903631f06029584e3945934884d7f2cda6c7bd1505e1cb877b6d03cd71019b7d5c4826ee3ec6392a1d531955c23e2f9806b8b59ffff001f2417000001a00100000000010000000000000000000000000000000000000000000000000000000000000000ffffffff4f04ffff001d0104455468652054696d65732030332f4a616e2f32303039204368616e63656c6c6f72206f6e206272696e6b206f66207365636f6e64206261696c6f757420666f722062616e6b736a00ffffffff2201d63440a2010000001976a91446a064688dc7beb5f70ef83569a0f15c7abf4f2888ac01fce41daa330000001976a9149c97b561ac186bd3758bf690036296d36b1fd01988ac011faf2e07000000001976a914118a92e28242a73244fb03c96b7e1429c06f979f88ac0177cd1301000000001976a914cae4bf990ce39624e2f77c140c543d4b15428ce788ac01bd479991150000001976a9149d6b7b5874afc100eb82a4883441a73b99d9c30688ac01c6b85af4d10200001976a914f989e2deedb1f09ed10310fc0d7da7ebfb57332688ac01b69b0d4b6c0000001976a9144688d6701fb4ae2893d3ec806e6af966faf6754588ac01e4b051c99b0000001976a91440e07b038941fb2616a54a498f763abae6d4f28088ac01ac3a95f1ba0000001976a914c43f7c57448805a068a440cc51f67379ca94626488ac018d1387503e0000001976a91498b7269dbf0c2e3344fb41cd60e75db16d6743a688ac018d1387503e0000001976a91485dceec8cdbb9e24fe07af783e4d273d1ae39f7588ac0144c6900d390000001976a914ddc05d332b7d1a18a55509f34c786ccb65bbffbc88ac0164bf8163180100001976a9148b04d0b2b582c986975414a01cb6295f1c33d0e988ac01fc192564180100001976a9141e9ff4c3ac6d0372963e92a13f1e47409eb62d3788ac0124d82728370000001976a914687e7cf063cd106c6098f002fa1ea91d8aee302a88ac0174d04b1e240000001976a914dc0be0edcadd4cc97872db40bb8c2db2cebafd1c88ac01977be006000000001976a91421efcbfe37045648180ac68b406794bde77f998388ac0189120801000000001976a914deaf53dbfbc799eed1171269e84c733dec22f51788ac01c68943281f0000001976a914200a0f9dba25e00ea84a4a3a43a7ea6983719d7188ac01e475f8211f0000001976a9142d072fb1a9d1f7dd8df0443e37e9f942eab5868088ac01cf9c695e280000001976a9140850f3b7caf3b822bb41b9619f8edf9b277402d088ac01fab8e6323b0000001976a914ec62fbd782bf6f48e52eea75a3c68a4c3ab824c088ac01867e669e2e0000001976a914c6dcb0065e98f5edda771c594265d61e38cf63a088ac017ba2c48a650000001976a914e5f9a711ccd7cb0d2a70f9710229d0d0d7ef3bda88ac018c1f5d59240000001976a914cae1527d24a91470aeb796f9d024630f301752ef88ac01981d055f190000001976a914604f36860d79a9d72b827c99409118bfe16711bd88ac011f3b5e661d0000001976a914f02e5891cef35c9c5d9a770756b240aba5ba363988ac01ccf8482c080000001976a9148251b4983be1027a17dc3b977502086f08ba891088ac012aa39eb2120000001976a914b991d98acde28455ecb0193fefab06841187c4e788ac0100b401da2324000017a914fc118af69f63d426f61c6a4bf38b56bcdaf8d0698701008a0700ef1a000017a91489ca93e03119d53fd9ad1e65ce22b6f8791f8a49870100a3c5df3f0e000017a91489ca93e03119d53fd9ad1e65ce22b6f8791f8a49870100daa532ad13000017a91489ca93e03119d53fd9ad1e65ce22b6f8791f8a49870100a09eee955a00001e04004a1f5ab175a9149c8c6c8c698f074180ecfdb38e8265c11f2a62cf870000");
            return builder;
        }

        protected override NetworkBuilder CreateRegtest()
        {
            NetworkBuilder builder = new NetworkBuilder();
            builder.SetConsensus(new Consensus()
            {
                SubsidyHalvingInterval = 150,
                MajorityEnforceBlockUpgrade = 51,
                MajorityRejectBlockOutdated = 75,
                MajorityWindow = 1000,
                PowLimit = new Target(new uint256("7fffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff")),
                PowTargetTimespan = TimeSpan.FromSeconds(14 * 24 * 60 * 60),
                PowTargetSpacing = TimeSpan.FromSeconds(10 * 60),
                PowAllowMinDifficultyBlocks = true,
                PowNoRetargeting = true,
                RuleChangeActivationThreshold = 108,
                MinerConfirmationWindow = 144,
                CoinbaseMaturity = 100,
                ConsensusFactory = ParticlConsensusFactory.Instance
            })
            .SetBase58Bytes(Base58Type.PUBKEY_ADDRESS, new byte[] { 0x76 }) // p
            .SetBase58Bytes(Base58Type.SCRIPT_ADDRESS, new byte[] { 0x7a })
            .SetBase58Bytes(Base58Type.SECRET_KEY, new byte[] { 0x2e })
            .SetBase58Bytes(Base58Type.EXT_PUBLIC_KEY, new byte[] { 0xe1, 0x42, 0x78, 0x00 }) // ppar
            .SetBase58Bytes(Base58Type.EXT_SECRET_KEY, new byte[] { 0x04, 0x88, 0x94, 0x78 }) // xpar
            .SetBase58Bytes(Base58Type.STEALTH_ADDRESS, new byte[] { 0x15 }) // T
            .SetMagic(0x0c061209)
            .SetPort(11938)
            .SetRPCPort(51936)
            .SetMaxP2PVersion(90007)
            .SetName("part-reg")
            .AddAlias("part-regnet")
            .AddAlias("particl-regnet")
            .AddAlias("particl-reg")
            .AddSeeds(ToSeed(pnSeed6_test))
            .SetGenesis("a00000000000000000000000000000000000000000000000000000000000000000000000e73c4282995b99070381d55b06bdac82b79f2236d470306ac7f28a20c75396f839963992f2e596ac79190e322a615eef7777000d71da94b74af391ff1a6ab6366bbaac58ffff7f200000000001a00100000000010000000000000000000000000000000000000000000000000000000000000000ffffffff4f04ffff001d0104455468652054696d65732030332f4a616e2f32303039204368616e63656c6c6f72206f6e206272696e6b206f66207365636f6e64206261696c6f757420666f722062616e6b736a00ffffffff0f010010a5d4e80000001976a914585c2b3914d9ee51f8e710304e386531c3abcc8288ac010010a5d4e80000001976a914c33f3603ce7c46b423536f0434155dad8ee2aa1f88ac010010a5d4e80000001976a91472d83540ed1dcf28bfaca3fa2ed77100c280882588ac010010a5d4e80000001976a91469e4cc4c219d8971a253cd5db69a0c99c4a5659d88ac010010a5d4e80000001976a914eab5ed88d97e50c87615a015771e220ab0a0991a88ac010010a5d4e80000001976a914119668a93761a34a4ba1c065794b26733975904f88ac010010a5d4e80000001976a9146da49762a4402d199d41d5778fcb69de19abbe9f88ac010010a5d4e80000001976a91427974d10ff5ba65052be7461d89ef2185acbe41188ac010010a5d4e80000001976a91489ea3129b8dbf1238b20a50211d50d462a988f6188ac010010a5d4e80000001976a9143baab5b42a409b7c6848a95dfd06ff792511d56188ac010088526a740000001976a914649b801848cc0c32993fb39927654969a5af27b088ac010088526a740000001976a914d669de30fa30c3e64a0303cb13df12391a2f725688ac010088526a740000001976a914f0c0e3ebe4a1334ed6a5e9c1e069ef425c52993488ac010088526a740000001976a91427189afe71ca423856de5f17538a069f2238542288ac010088526a740000001976a9140e7f6fe0c4a5a6a9bfd18f7effdd5898b1f40b8088ac0000");
            return builder;
        }
    }
}

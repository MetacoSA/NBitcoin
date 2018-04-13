using System;

namespace NBitcoin.Altcoins.HashX11
{
	internal interface ICrypto : IHash, IBlockHash
    {
    }

    internal interface ICryptoBuildIn : ICrypto
    {
    }

    internal interface ICryptoNotBuildIn : ICrypto
    {
    }

    internal interface IWithKey : IHash
    {
        byte[] Key
        {
            get;
            set;
        }

        int? KeyLength
        {
            get;
        }
    }

    internal interface IHashWithKey : IHash, IWithKey
    {
    }

    public interface IBlockHash
    {
    }

    public interface INonBlockHash
    {
    }

    public interface IChecksum
    {
    }
}

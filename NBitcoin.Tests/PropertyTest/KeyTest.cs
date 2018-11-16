using FsCheck;
using FsCheck.Xunit;
using NBitcoin.Tests.Generators;
using NBitcoin;
using Xunit;

namespace NBitcoin.Tests.PropertyTest
{
  public class KeyTest
  {
    public KeyTest()
    {
      Arb.Register<CryptoGenerator>();
      Arb.Register<ChainParamsGenerator>();
    }

    [Property]
    [Trait("UnitTest", "UnitTest")]
    public bool CanSerializeAsymmetric(Key key, Network network)
    {
      var keyStr = key.ToString();
      string wif = new BitcoinSecret(key, network).ToWif();
      return Key.Parse(wif, network).Equals(key);
    }
  }
}
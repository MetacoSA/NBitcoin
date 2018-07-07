using NBitcoin.BouncyCastle.Math;
using NBitcoin.Crypto;
using NBitcoin.DataEncoders;
using Xunit;

namespace NBitcoin.Tests
{
	public class SchnorrSignerTests
	{

        [Fact]
        public void SingningTest()
        {
            var vectors = new (string Name, string PrivateKey, string PublickKey, string Message, string Signature)[]{
                ("Test vector 1",
                    "0000000000000000000000000000000000000000000000000000000000000001",
                    "0279BE667EF9DCBBAC55A06295CE870B07029BFCDB2DCE28D959F2815B16F81798",
                    "0000000000000000000000000000000000000000000000000000000000000000",
                    "787A848E71043D280C50470E8E1532B2DD5D20EE912A45DBDD2BD1DFBF187EF67031A98831859DC34DFFEEDDA86831842CCD0079E1F92AF177F7F22CC1DCED05"),
                ("Test vector 2",
                    "B7E151628AED2A6ABF7158809CF4F3C762E7160F38B4DA56A784D9045190CFEF",
                    "02DFF1D77F2A671C5F36183726DB2341BE58FEAE1DA2DECED843240F7B502BA659",
                    "243F6A8885A308D313198A2E03707344A4093822299F31D0082EFA98EC4E6C89",
                    "2A298DACAE57395A15D0795DDBFD1DCB564DA82B0F269BC70A74F8220429BA1D1E51A22CCEC35599B8F266912281F8365FFC2D035A230434A1A64DC59F7013FD"),
                ("Test vector 3",
                    "C90FDAA22168C234C4C6628B80DC1CD129024E088A67CC74020BBEA63B14E5C7",
                    "03FAC2114C2FBB091527EB7C64ECB11F8021CB45E8E7809D3C0938E4B8C0E5F84B",
                    "5E2D58D8B3BCDF1ABADEC7829054F90DDA9805AAB56C77333024B9D0A508B75C",
                    "00DA9B08172A9B6F0466A2DEFD817F2D7AB437E0D253CB5395A963866B3574BE00880371D01766935B92D2AB4CD5C8A2A5837EC57FED7660773A05F0DE142380")                
            };
            var signer = new SchnorrSigner();

            foreach(var vector in vectors)
            {
                var privatekey= Encoders.Hex.DecodeData(vector.PrivateKey);
                var message= uint256.Parse(vector.Message);
                var expectedSignature= vector.Signature;

                var signature = signer.Sign(privatekey, message);
                Assert.Equal(expectedSignature, Encoders.Hex.EncodeData(signature).ToUpper());
            }
        }
    }
}
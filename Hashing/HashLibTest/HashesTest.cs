using System;
using HashLib;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Text;
using TomanuExtensions;
using TomanuExtensions.Utils;
using System.Collections.Generic;
using System.Diagnostics;

namespace HashLibTest
{
    [TestClass]
    public class HashesTest : HashesTestBase
    {
        [TestMethod]
        public void HashLib_Crypto_MD5()
        {
            Test(HashFactory.Crypto.CreateMD5());
        }

        [TestMethod]
        public void HashLib_Crypto_Snefru()
        {
            Test(HashFactory.Crypto.CreateSnefru_4_128());
            Test(HashFactory.Crypto.CreateSnefru_4_256());
            Test(HashFactory.Crypto.CreateSnefru_8_128());
            Test(HashFactory.Crypto.CreateSnefru_8_256());
        }

        [TestMethod]
        public void HashLib_Crypto_HAS160()
        {
            Test(HashFactory.Crypto.CreateHAS160());
        }

        [TestMethod]
        public void HashLib_Crypto_BuildIn_MD5CryptoServiceProvider()
        {
            Test(HashFactory.Crypto.BuildIn.CreateMD5CryptoServiceProvider());
        }

        [TestMethod]
        public void HashLib_Crypto_MD2()
        {
            Test(HashFactory.Crypto.CreateMD2());
        }

        [TestMethod]
        public void HashLib_Crypto_MD4()
        {
            Test(HashFactory.Crypto.CreateMD4());
        }

        [TestMethod]
        public void HashLib_Crypto_SHA224()
        {
            Test(HashFactory.Crypto.CreateSHA224());
        }

        [TestMethod]
        public void HashLib_Crypto_RIPEMD128()
        {
            Test(HashFactory.Crypto.CreateRIPEMD128());
        }

        [TestMethod]
        public void HashLib_Crypto_BuildIn_RIPEMD160Managed()
        {
            Test(HashFactory.Crypto.BuildIn.CreateRIPEMD160Managed());
        }

        [TestMethod]
        public void HashLib_Crypto_RIPEMD160()
        {
            Test(HashFactory.Crypto.CreateRIPEMD160());
        }

        [TestMethod]
        public void HashLib_Crypto_Haval()
        {
            Test(HashFactory.Crypto.CreateHaval_3_128());
            Test(HashFactory.Crypto.CreateHaval_4_128());
            Test(HashFactory.Crypto.CreateHaval_5_128());

            Test(HashFactory.Crypto.CreateHaval_3_160());
            Test(HashFactory.Crypto.CreateHaval_4_160());
            Test(HashFactory.Crypto.CreateHaval_5_160());

            Test(HashFactory.Crypto.CreateHaval_3_192());
            Test(HashFactory.Crypto.CreateHaval_4_192());
            Test(HashFactory.Crypto.CreateHaval_5_192());

            Test(HashFactory.Crypto.CreateHaval_3_224());
            Test(HashFactory.Crypto.CreateHaval_4_224());
            Test(HashFactory.Crypto.CreateHaval_5_224());

            Test(HashFactory.Crypto.CreateHaval_3_256());
            Test(HashFactory.Crypto.CreateHaval_4_256());
            Test(HashFactory.Crypto.CreateHaval_5_256());
        }

        [TestMethod]
        public void HashLib_Crypto_SHA3_JH()
        {
            Test(HashFactory.Crypto.SHA3.CreateJH224());
            Test(HashFactory.Crypto.SHA3.CreateJH256());
            Test(HashFactory.Crypto.SHA3.CreateJH384());
            Test(HashFactory.Crypto.SHA3.CreateJH512());
        }

        [TestMethod]
        public void HashLib_Crypto_SHA3_Echo()
        {
            Test(HashFactory.Crypto.SHA3.CreateEcho224());
            Test(HashFactory.Crypto.SHA3.CreateEcho256());
            Test(HashFactory.Crypto.SHA3.CreateEcho384());
            Test(HashFactory.Crypto.SHA3.CreateEcho512());
        }

        [TestMethod]
        public void HashLib_Crypto_SHA3_Fugue()
        {
            Test(HashFactory.Crypto.SHA3.CreateFugue224());
            Test(HashFactory.Crypto.SHA3.CreateFugue256());
            Test(HashFactory.Crypto.SHA3.CreateFugue384());
            Test(HashFactory.Crypto.SHA3.CreateFugue512());
        }

        [TestMethod]
        public void HashLib_Crypto_SHA3_Groestl()
        {
            Test(HashFactory.Crypto.SHA3.CreateGroestl224());
            Test(HashFactory.Crypto.SHA3.CreateGroestl256());
            Test(HashFactory.Crypto.SHA3.CreateGroestl384());
            Test(HashFactory.Crypto.SHA3.CreateGroestl512());
        }

        [TestMethod]
        public void HashLib_Crypto_SHA3_Hamsi()
        {
            Test(HashFactory.Crypto.SHA3.CreateHamsi224());
            Test(HashFactory.Crypto.SHA3.CreateHamsi256());
            Test(HashFactory.Crypto.SHA3.CreateHamsi384());
            Test(HashFactory.Crypto.SHA3.CreateHamsi512());
        }

        [TestMethod]
        public void HashLib_Crypto_SHA3_Keccak()
        {
            Test(HashFactory.Crypto.SHA3.CreateKeccak224());
            Test(HashFactory.Crypto.SHA3.CreateKeccak256());
            Test(HashFactory.Crypto.SHA3.CreateKeccak384());
            Test(HashFactory.Crypto.SHA3.CreateKeccak512());
        }

        [TestMethod]
        public void HashLib_Crypto_SHA3_Luffa()
        {
            Test(HashFactory.Crypto.SHA3.CreateLuffa224());
            Test(HashFactory.Crypto.SHA3.CreateLuffa256());
            Test(HashFactory.Crypto.SHA3.CreateLuffa384());
            Test(HashFactory.Crypto.SHA3.CreateLuffa512());
        }

        [TestMethod]
        public void HashLib_Crypto_SHA3_Shabal()
        {
            Test(HashFactory.Crypto.SHA3.CreateShabal224());
            Test(HashFactory.Crypto.SHA3.CreateShabal256());
            Test(HashFactory.Crypto.SHA3.CreateShabal384());
            Test(HashFactory.Crypto.SHA3.CreateShabal512());
        }

        [TestMethod]
        public void HashLib_Crypto_SHA3_SHAvite3()
        {
            Test(HashFactory.Crypto.SHA3.CreateSHAvite3_224());
            Test(HashFactory.Crypto.SHA3.CreateSHAvite3_256());
            Test(HashFactory.Crypto.SHA3.CreateSHAvite3_384());
            Test(HashFactory.Crypto.SHA3.CreateSHAvite3_512());
        }

        [TestMethod]
        public void HashLib_Crypto_SHA3_SIMD()
        {
            Test(HashFactory.Crypto.SHA3.CreateSIMD224());
            Test(HashFactory.Crypto.SHA3.CreateSIMD256());
            Test(HashFactory.Crypto.SHA3.CreateSIMD384());
            Test(HashFactory.Crypto.SHA3.CreateSIMD512());
        }

        [TestMethod]
        public void HashLib_Crypto_SHA3_Skein()
        {
            Test(HashFactory.Crypto.SHA3.CreateSkein224());
            Test(HashFactory.Crypto.SHA3.CreateSkein256());
            Test(HashFactory.Crypto.SHA3.CreateSkein384());
            Test(HashFactory.Crypto.SHA3.CreateSkein512());
        }

        [TestMethod]
        public void HashLib_Crypto_SHA3_CubeHash()
        {
            Test(HashFactory.Crypto.SHA3.CreateCubeHash224());
            Test(HashFactory.Crypto.SHA3.CreateCubeHash256());
            Test(HashFactory.Crypto.SHA3.CreateCubeHash384());
            Test(HashFactory.Crypto.SHA3.CreateCubeHash512());
        }

        [TestMethod]
        public void HashLib_Crypto_SHA3_Blake()
        {
            Test(HashFactory.Crypto.SHA3.CreateBlake224());
            Test(HashFactory.Crypto.SHA3.CreateBlake256());
            Test(HashFactory.Crypto.SHA3.CreateBlake384());
            Test(HashFactory.Crypto.SHA3.CreateBlake512());
        }

        [TestMethod]
        public void HashLib_Crypto_SHA3_BlueMidnightWish()
        {
            Test(HashFactory.Crypto.SHA3.CreateBlueMidnightWish224());
            Test(HashFactory.Crypto.SHA3.CreateBlueMidnightWish256());
            Test(HashFactory.Crypto.SHA3.CreateBlueMidnightWish384());
            Test(HashFactory.Crypto.SHA3.CreateBlueMidnightWish512());
        }

        [TestMethod]
        public void HashLib_Crypto_RIPEMD256()
        {
            Test(HashFactory.Crypto.CreateRIPEMD256());
        }

        [TestMethod]
        public void HashLib_Crypto_RIPEMD320()
        {
            Test(HashFactory.Crypto.CreateRIPEMD320());
        }

        [TestMethod]
        public void HashLib_Crypto_SHA1()
        {
            Test(HashFactory.Crypto.CreateSHA1());
        }

        [TestMethod]
        public void HashLib_Crypto_BuildIn_SHA1Cng()
        {
            Test(HashFactory.Crypto.BuildIn.CreateSHA1Cng());
        }

        [TestMethod]
        public void HashLib_Crypto_BuildIn_SHA1Managed()
        {
            Test(HashFactory.Crypto.BuildIn.CreateSHA1Managed());
        }

        [TestMethod]
        public void HashLib_Crypto_BuildIn_SHA1CryptoServiceProvider()
        {
            Test(HashFactory.Crypto.BuildIn.CreateSHA1CryptoServiceProvider());
        }

        [TestMethod]
        public void HashLib_Crypto_BuildIn_SHA512Cng()
        {
            Test(HashFactory.Crypto.BuildIn.CreateSHA512Cng());
        }

        [TestMethod]
        public void HashLib_Crypto_BuildIn_SHA512Managed()
        {
            Test(HashFactory.Crypto.BuildIn.CreateSHA512Managed());
        }

        [TestMethod]
        public void HashLib_Crypto_BuildIn_SHA512CryptoServiceProvider()
        {
            Test(HashFactory.Crypto.BuildIn.CreateSHA512CryptoServiceProvider());
        }

        [TestMethod]
        public void HashLib_Crypto_BuildIn_SHA384Cng()
        {
            Test(HashFactory.Crypto.BuildIn.CreateSHA384Cng());
        }

        [TestMethod]
        public void HashLib_Crypto_BuildIn_SHA384Managed()
        {
            Test(HashFactory.Crypto.BuildIn.CreateSHA384Managed());
        }

        [TestMethod]
        public void HashLib_Crypto_BuildIn_SHA384CryptoServiceProvider()
        {
            Test(HashFactory.Crypto.BuildIn.CreateSHA384CryptoServiceProvider());
        }

        [TestMethod]
        public void HashLib_Crypto_BuildIn_SHA256Managed()
        {
            Test(HashFactory.Crypto.BuildIn.CreateSHA256Managed());
        }

        [TestMethod]
        public void HashLib_Crypto_SHA512()
        {
            Test(HashFactory.Crypto.CreateSHA512());
        }

        [TestMethod]
        public void HashLib_Crypto_SHA384()
        {
            Test(HashFactory.Crypto.CreateSHA384());
        }

        [TestMethod]
        public void HashLib_Crypto_BuildIn_SHA256CryptoServiceProvider()
        {
            Test(HashFactory.Crypto.BuildIn.CreateSHA256CryptoServiceProvider());
        }

        [TestMethod]
        public void HashLib_Crypto_BuildIn_SHA256Cng()
        {
            Test(HashFactory.Crypto.BuildIn.CreateSHA256Cng());
        }

        [TestMethod]
        public void HashLib_Crypto_SHA256()
        {
            Test(HashFactory.Crypto.CreateSHA256());
        }

        [TestMethod]
        public void HashLib_Crypto_Whirlpool()
        {
            Test(HashFactory.Crypto.CreateWhirlpool());
        }

        [TestMethod]
        public void Crypto_ExtremelyLong()
        {
            TestExtremelyLong();
        }

        [TestMethod]
        public void HashLib_Crypto_Gost()
        {
            Test(HashFactory.Crypto.CreateGost());
        }

        [TestMethod]
        public void HashLib_Crypto_Tiger()
        {
            Test(HashFactory.Crypto.CreateTiger_3_192());
            Test(HashFactory.Crypto.CreateTiger_4_192());
        }

        [TestMethod]
        public void HashLib_Crypto_Tiger2()
        {
            Test(HashFactory.Crypto.CreateTiger2());
        }

        [TestMethod]
        public void HashLib_32_AP()
        {
            Test(HashFactory.Hash32.CreateAP());
        }

        [TestMethod]
        public void HashLib_Checksum_Adler32()
        {
            Test(HashFactory.Checksum.CreateAdler32());
        }

        [TestMethod]
        public void HashLib_32_Bernstein()
        {
            Test(HashFactory.Hash32.CreateBernstein());
        }

        [TestMethod]
        public void HashLib_32_Bernstein1()
        {
            Test(HashFactory.Hash32.CreateBernstein1());
        }

        [TestMethod]
        public void HashLib_32_BKDR()
        {
            Test(HashFactory.Hash32.CreateBKDR());
        }

        [TestMethod]
        public void HashLib_Checksum_CRC32()
        {
            Test(HashFactory.Checksum.CreateCRC32_IEEE());
            Test(HashFactory.Checksum.CreateCRC32_CASTAGNOLI());
            Test(HashFactory.Checksum.CreateCRC32_KOOPMAN());
            Test(HashFactory.Checksum.CreateCRC32_Q());
        }

        [TestMethod]
        public void HashLib_32_DEK()
        {
            Test(HashFactory.Hash32.CreateDEK());
        }

        [TestMethod]
        public void HashLib_32_DJB()
        {
            Test(HashFactory.Hash32.CreateDJB());
        }

        [TestMethod]
        public void HashLib_32_ELF()
        {
            Test(HashFactory.Hash32.CreateELF());
        }

        [TestMethod]
        public void HashLib_Checksum_FNV()
        {
            Test(HashFactory.Hash32.CreateFNV());
        }

        [TestMethod]
        public void HashLib_32_FNV1a()
        {
            Test(HashFactory.Hash32.CreateFNV1a());
        }

        [TestMethod]
        public void HashLib_32_Jenkins3()
        {
            Test(HashFactory.Hash32.CreateJenkins3());
        }

        [TestMethod]
        public void HashLib_32_JS()
        {
            Test(HashFactory.Hash32.CreateJS());
        }

        [TestMethod]
        public void HashLib_32_Murmur2()
        {
            Test(HashFactory.Hash32.CreateMurmur2());
        }

        [TestMethod]
        public void HashLib_32_Murmur3()
        {
            Test(HashFactory.Hash32.CreateMurmur3());
        }

        [TestMethod]
        public void HashLib_32_OneAtTime()
        {
            Test(HashFactory.Hash32.CreateOneAtTime());
        }

        [TestMethod]
        public void HashLib_32_PJW()
        {
            Test(HashFactory.Hash32.CreatePJW());
        }

        [TestMethod]
        public void HashLib_32_Rotating()
        {
            Test(HashFactory.Hash32.CreateRotating());
        }

        [TestMethod]
        public void HashLib_32_RS()
        {
            Test(HashFactory.Hash32.CreateRS());
        }

        [TestMethod]
        public void HashLib_32_SDBM()
        {
            Test(HashFactory.Hash32.CreateSDBM());
        }

        [TestMethod]
        public void HashLib_32_ShiftAndXor()
        {
            Test(HashFactory.Hash32.CreateShiftAndXor());
        }

        [TestMethod]
        public void HashLib_32_SuperFast()
        {
            Test(HashFactory.Hash32.CreateSuperFast());
        }

        [TestMethod]
        public void HashLib_64_FNV()
        {
            Test(HashFactory.Hash64.CreateFNV());
        }

        [TestMethod]
        public void HashLib_64_FNV1a()
        {
            Test(HashFactory.Hash64.CreateFNV1a());
        }

        [TestMethod]
        public void HashLib_Checksum_CRC64()
        {
            Test(HashFactory.Checksum.CreateCRC64_ISO());
            Test(HashFactory.Checksum.CreateCRC64_ECMA());
        }

        [TestMethod]
        public void HashLib_64_Murmur2()
        {
            Test(HashFactory.Hash64.CreateMurmur2());
        }

        [TestMethod]
        public void HashLib_128_Murmur3()
        {
            Test(HashFactory.Hash128.CreateMurmur3_128());
        }

        [TestMethod]
        public void HashLib_64_SipHash()
        {
            Test(HashFactory.Hash64.CreateSipHash());
        }

        [TestMethod]
        public void HashLib_Crypto_Grindahl256()
        {
            Test(HashFactory.Crypto.CreateGrindahl256());
        }

        [TestMethod]
        public void HashLib_Crypto_Grindahl512()
        {
            Test(HashFactory.Crypto.CreateGrindahl512());
        }

        [TestMethod]
        public void HashLib_Crypto_Panama()
        {
            Test(HashFactory.Crypto.CreatePanama());
        }

        [TestMethod]
        public void HashLib_Crypto_RadioGatun32()
        {
            Test(HashFactory.Crypto.CreateRadioGatun32());
        }

        [TestMethod]
        public void HashLib_Crypto_RadioGatun64()
        {
            Test(HashFactory.Crypto.CreateRadioGatun64());
        }

        [TestMethod]
        public void HashLib_Crypto_RIPEMD()
        {
            Test(HashFactory.Crypto.CreateRIPEMD());
        }

        [TestMethod]
        public void HashLib_Crypto_SHA0()
        {
            Test(HashFactory.Crypto.CreateSHA0());
        }

        [TestMethod]
        public void TestHashes()
        {
            Assert.AreEqual(
                "F3581AE5-027E0B67-5FB9136F-E8C1403E-4DF209FA-2C07CAC5-D8A7BEC8-DC9076C0-63B2DEEC-9DA04A78-DC3DE302-A99ACC2C-B79333E3-FE4C7F16-50691C22-1180354B", 
                HashFactory.Crypto.BuildIn.CreateSHA512Cng().ComputeBytes(
                    Converters.ConvertStringToBytes(
                        Hashes.All.Select(el => el.GetType().FullName).
                            Aggregate("", (acc, val) => acc += val))).ToString());

            Assert.AreEqual(
                "A27A6429-71247FF0-CC8BC923-DEE3E261-6EBAA40A-7872BC00-3B280C7D-5C7D3D2C-D5539A29-9876F73B-AE10CAA4-11AB9A21-B0C7F915-C877B390-C3A6695D-21428416", 
                HashFactory.Crypto.BuildIn.CreateSHA512Cng().ComputeBytes(
                    Converters.ConvertStringToBytes(
                        Hashes.AllUnique.Select(el => el.GetType().FullName).
                            Aggregate("", (acc, val) => acc += val))).ToString());

            Assert.AreEqual(
                "3097E1ED-B168CAFA-2CBCEFBF-6FB4FABE-AD0959DD-E04BE5AF-0E6D5D15-78DB5577-B570AE00-14CC9586-CBDB300D-CBD0CEA5-D4D5E2D5-20E78D08-CE726E58-AE176CBA", 
                HashFactory.Crypto.BuildIn.CreateSHA512Cng().ComputeBytes(
                    Converters.ConvertStringToBytes(
                        Hashes.Checksums.Select(el => el.GetType().FullName).
                            Aggregate("", (acc, val) => acc += val))).ToString());

            Assert.AreEqual(
                "6C1E48E2-74A976FD-7A9F76E1-2F784912-929B939C-19E7B195-65CE29FD-9181D337-2185BB77-F86B16B3-FB6638C2-3A8D130B-BEC517A7-FC639305-28A079D1-D5D115DE", 
                HashFactory.Crypto.BuildIn.CreateSHA512Cng().ComputeBytes(
                    Converters.ConvertStringToBytes(
                        Hashes.CryptoAll.Select(el => el.GetType().FullName).
                            Aggregate("", (acc, val) => acc += val))).ToString());

            Assert.AreEqual(
                "87D9E63B-B68DC8B9-2B970BD8-503D861D-4C406A3A-9BF28A7F-189C7E85-D72F1528-3279F0D7-79CF9B36-396B5F98-DDE3FE0A-A47243FE-912A1E8B-E308CA0A-5A8844F0", 
                HashFactory.Crypto.BuildIn.CreateSHA512Cng().ComputeBytes(
                    Converters.ConvertStringToBytes(
                        Hashes.CryptoBuildIn.Select(el => el.GetType().FullName).
                            Aggregate("", (acc, val) => acc += val))).ToString());

            Assert.AreEqual(
                "37B94613-2D4E7794-6E763DAE-AD5CD38E-5AA88F82-DD96A845-D71DFDC7-A28DC559-DEBD055A-1A154837-9C66C6F7-9BF1B0E6-6EA3263B-E1FAF1BA-C0624119-AC4382DA", 
                HashFactory.Crypto.BuildIn.CreateSHA512Cng().ComputeBytes(
                    Converters.ConvertStringToBytes(
                        Hashes.CryptoNotBuildIn.Select(el => el.GetType().FullName).
                            Aggregate("", (acc, val) => acc += val))).ToString());

            Assert.AreEqual(
                "8F2F5764-A203761B-8B84755B-206C3255-1DD29088-90F5AE37-7879460D-00B2EF42-195C8FB1-9C0DEDD7-153A6E7B-5F447E15-BF6D4B53-7B95F73B-80BE1C34-2972CCF4", 
                HashFactory.Crypto.BuildIn.CreateSHA512Cng().ComputeBytes(
                    Converters.ConvertStringToBytes(
                        Hashes.FastComputes.Select(el => el.GetType().FullName).
                            Aggregate("", (acc, val) => acc += val))).ToString());

            Assert.AreEqual(
                "48DDCBA3-AF1BB119-B1358345-3933130B-D19D756C-A4280134-C3A43180-401F6D72-DB52234D-3CF5232D-1202A332-89B46C77-B8239B28-A4A6A279-D2082CF5-CFE67514", 
                HashFactory.Crypto.BuildIn.CreateSHA512Cng().ComputeBytes(
                    Converters.ConvertStringToBytes(
                        Hashes.Hash32.Select(el => el.GetType().FullName).
                            Aggregate("", (acc, val) => acc += val))).ToString());

            Assert.AreEqual(
                "13F849F0-C5D6743F-B6342FAE-FD101384-72705F8B-412E9C2E-F3B7BD4B-56BAF8EC-F2397B9E-8CDC52FD-5F999591-214E2499-15F24A43-9147FFAB-EDAB01CD-0CA2126D", 
                HashFactory.Crypto.BuildIn.CreateSHA512Cng().ComputeBytes(
                    Converters.ConvertStringToBytes(
                        Hashes.Hash64.Select(el => el.GetType().FullName).
                            Aggregate("", (acc, val) => acc += val))).ToString());

            Assert.AreEqual(
                "7133CF20-EF5E24ED-7B97CA25-AE4E6867-0F9C22AC-B7ECB12C-4D0A482D-0D06D08A-69567E42-24B3440A-224DA240-CC282BF0-A4793F3B-75DBEC35-E7CCF361-3C70F05B", 
                HashFactory.Crypto.BuildIn.CreateSHA512Cng().ComputeBytes(
                    Converters.ConvertStringToBytes(
                        Hashes.Hash128.Select(el => el.GetType().FullName).
                            Aggregate("", (acc, val) => acc += val))).ToString());

            Assert.AreEqual(
                "67F03D8C-380AD001-25719A65-14D96D5F-CBA29A9A-E282977B-C6B39696-70F3E0CF-63FDD4F1-07B35AB7-CB9552D0-28981B10-FD6CDB78-91EB3C33-91D10D03-2EF6C2E2", 
                HashFactory.Crypto.BuildIn.CreateSHA512Cng().ComputeBytes(
                    Converters.ConvertStringToBytes(
                        Hashes.HasHMACBuildIn.Select(el => el.GetType().FullName).
                            Aggregate("", (acc, val) => acc += val))).ToString());

            Assert.AreEqual(
                "A44F5451-85495868-48531E32-8953390D-BEA98D72-CE1A3D9F-72D56E2D-C90D79CE-53E9E384-9636CC43-B9498F19-583F1AF8-9E6F5CA7-10D28145-327A87CC-C3E75113", 
                HashFactory.Crypto.BuildIn.CreateSHA512Cng().ComputeBytes(
                    Converters.ConvertStringToBytes(
                        Hashes.NonBlock.Select(el => el.GetType().FullName).
                            Aggregate("", (acc, val) => acc += val))).ToString());

            Assert.AreEqual(
                "E578F7C4-5CEBA02B-7CC51716-23E46666-CC43875C-4C6288DC-48B1DB75-9A11C6CD-C2B88CE4-96F052A0-A0D8BFDF-F31D6B38-AE57BEB3-9A0310FA-A9ECCC26-DB17FD4A", 
                HashFactory.Crypto.BuildIn.CreateSHA512Cng().ComputeBytes(
                    Converters.ConvertStringToBytes(
                        Hashes.WithKey.Select(el => el.GetType().FullName).
                            Aggregate("", (acc, val) => acc += val))).ToString());
        }

        [TestMethod]
        public void TestConverters()
        {
            {
                var chars = new char[] { '\x1234', '\xABCD' };
                var bytes = Converters.ConvertCharsToBytes(chars);
                CollectionAssert.AreEqual(bytes.ToList(), 
                    Converters.ConvertHexStringToBytes("3412CDAB").ToList());
            }

            {
                var str = "\x1234\xABCD";
                var bytes = Converters.ConvertStringToBytes(str);
                CollectionAssert.AreEqual(bytes.ToList(),
                    Converters.ConvertHexStringToBytes("3412CDAB").ToList());
            }

            {
                var str = "\x1234\xABCD";
                var bytes = Converters.ConvertStringToBytes(str, Encoding.Unicode);
                CollectionAssert.AreEqual(bytes.ToList(),
                    Converters.ConvertHexStringToBytes("3412CDAB").ToList());
            }

            {
                var shorts = new short[] { 0x1234, 0x7BCD };
                var bytes = Converters.ConvertShortsToBytes(shorts);
                CollectionAssert.AreEqual(bytes.ToList(),
                    Converters.ConvertHexStringToBytes("3412CD7B").ToList());
            }

            {
                var ushorts = new ushort[] { 0x1234, 0xABCD };
                var bytes = Converters.ConvertUShortsToBytes(ushorts);
                CollectionAssert.AreEqual(bytes.ToList(),
                    Converters.ConvertHexStringToBytes("3412CDAB").ToList());
            }

            {
                var ints = new int[] { 0x12345678, 0x7BCDEF45 };
                var bytes = Converters.ConvertIntsToBytes(ints);
                CollectionAssert.AreEqual(bytes.ToList(), 
                    Converters.ConvertHexStringToBytes("7856341245EFCD7B").ToList());
            }

            {
                var uints = new uint[] { 0x12345678, 0xABCDEF45 };
                var bytes = Converters.ConvertUIntsToBytes(uints);
                CollectionAssert.AreEqual(bytes.ToList(),
                    Converters.ConvertHexStringToBytes("7856341245EFCDAB").ToList());
            }

            {
                var longs = new long[] { 0x12345678ABCDEF45, 0x6756EEFFBC456783 };
                var bytes = Converters.ConvertLongsToBytes(longs);
                CollectionAssert.AreEqual(bytes.ToList(),
                    Converters.ConvertHexStringToBytes("12345678ABCDEF45").Reverse().Concat(
                        Converters.ConvertHexStringToBytes("6756EEFFBC456783").Reverse()).ToList());
            }

            {
                var ulongs = new ulong[] { 0x12345678ABCDEF45, 0xF756EEFFBC456783 };
                var bytes = Converters.ConvertULongsToBytes(ulongs);
                CollectionAssert.AreEqual(bytes.ToList(),
                    Converters.ConvertHexStringToBytes("12345678ABCDEF45").Reverse().Concat(
                        Converters.ConvertHexStringToBytes("F756EEFFBC456783").Reverse()).ToList());
            }

            {
                var doubles = new double[] { 56.678768, -34.4568768, 10e34, 10e-20, Double.NaN };
                var bytes = Converters.ConvertDoublesToBytes(doubles);

                var b0 = BitConverter.GetBytes(doubles[0]);
                var b1 = BitConverter.GetBytes(doubles[1]);
                var b2 = BitConverter.GetBytes(doubles[2]);
                var b3 = BitConverter.GetBytes(doubles[3]);
                var b4 = BitConverter.GetBytes(doubles[4]);

                CollectionAssert.AreEqual(bytes.ToList(),
                    b0.Concat(b1).Concat(b2).Concat(b3).Concat(b4).ToList());
            }

            {
                var floats = new float[] { 56.678768f, -34.4568768f, 10e34f, 10e-20f, Single.NaN };
                var bytes = Converters.ConvertFloatsToBytes(floats);

                var b0 = BitConverter.GetBytes(floats[0]);
                var b1 = BitConverter.GetBytes(floats[1]);
                var b2 = BitConverter.GetBytes(floats[2]);
                var b3 = BitConverter.GetBytes(floats[3]);
                var b4 = BitConverter.GetBytes(floats[4]);

                CollectionAssert.AreEqual(bytes.ToList(),
                    b0.Concat(b1).Concat(b2).Concat(b3).Concat(b4).ToList());
            }
        }

        [TestMethod]
        public void HashResult()
        {
            for (int i = 0; i < 14; i++)
            {
                HashResult h1 = new HashResult(m_random.NextBytes(i));

                try
                {
                    uint h2 = h1.GetUInt();

                    if (i != 4)
                        Assert.Fail(i.ToString());

                    Assert.IsTrue(Converters.ConvertBytesToUInts(h1.GetBytes())[0] == h2, i.ToString());
                }
                catch
                {
                    if (i == 4)
                        Assert.Fail(i.ToString());
                }

                try
                {
                    ulong h3 = h1.GetULong();

                    if (i != 8)
                        Assert.Fail(i.ToString());

                    Assert.IsTrue(Converters.ConvertBytesToULongs(h1.GetBytes())[0] == h3, i.ToString());
                }
                catch
                {
                    if (i == 8)
                        Assert.Fail(i.ToString());
                }
            }
        }

        [TestMethod()]
        public void Wrappers()

        {
            TestAgainstTestFile(
                HashFactory.Wrappers.HashAlgorithmToHash(
                    HashFactory.Wrappers.HashToHashAlgorithm(
                        HashFactory.Crypto.CreateSHA1()),
                    HashFactory.Crypto.CreateSHA1().BlockSize
                ), TestData.Load(HashFactory.Crypto.CreateSHA1())
            );
        }

        [TestMethod]
        public void HMAC_All()
        {
            TestHMAC(HashFactory.HMAC.CreateHMAC(HashFactory.Crypto.BuildIn.CreateMD5CryptoServiceProvider()),
                HashFactory.HMAC.CreateHMAC(HashFactory.Crypto.CreateMD5()));
            TestHMAC(HashFactory.HMAC.CreateHMAC(HashFactory.Crypto.BuildIn.CreateRIPEMD160Managed()),
                HashFactory.HMAC.CreateHMAC(HashFactory.Crypto.CreateRIPEMD160()));
            TestHMAC(HashFactory.HMAC.CreateHMAC(HashFactory.Crypto.BuildIn.CreateSHA1CryptoServiceProvider()),
                HashFactory.HMAC.CreateHMAC(HashFactory.Crypto.CreateSHA1()));
            TestHMAC(HashFactory.HMAC.CreateHMAC(HashFactory.Crypto.BuildIn.CreateSHA256Managed()),
                HashFactory.HMAC.CreateHMAC(HashFactory.Crypto.CreateSHA256()));
            TestHMAC(HashFactory.HMAC.CreateHMAC(HashFactory.Crypto.BuildIn.CreateSHA384Managed()),
                HashFactory.HMAC.CreateHMAC(HashFactory.Crypto.CreateSHA384()));
            TestHMAC(HashFactory.HMAC.CreateHMAC(HashFactory.Crypto.BuildIn.CreateSHA512Managed()),
                HashFactory.HMAC.CreateHMAC(HashFactory.Crypto.CreateSHA512()));

            TestKey(HashFactory.HMAC.CreateHMAC(HashFactory.Crypto.BuildIn.CreateMD5CryptoServiceProvider()));
            TestKey(HashFactory.HMAC.CreateHMAC(HashFactory.Crypto.CreateMD5()));
        }

        [TestMethod]
        public void hashBlake()
        {
            var paramIn = "01000000000000000000000000000000000000000000000000000000000000000000000018157f44917c2514c1f339346200f8b27d8ffaae9d8205bfae51030bc26ba265b88ba557ffff0f1eddf21b00";
            var paramOut = "042733333794f07574f6ca059eef16bacbfc5d563e5342d64fded94c6f6fbd139db7ebe1d48b962156391383ccb7f6064fe4583c64df954e5418b9a08908a082";
            var result = HashFactory.Crypto.SHA3.CreateBlake512().ComputeBytes(Converters.ConvertHexStringToBytes(paramIn));
            Assert.AreEqual(paramOut, Converters.ConvertBytesToHexString(result.GetBytes(), false).ToLower());
        }

        [TestMethod]
        public void hashBlueMidnightWish()
        {
            var paramIn = "042733333794f07574f6ca059eef16bacbfc5d563e5342d64fded94c6f6fbd139db7ebe1d48b962156391383ccb7f6064fe4583c64df954e5418b9a08908a082";
            var paramOut = "b2e1d72db8a3807d6d929a0e1349250cae0e99475d94bd869d0163755574a89078e08f604ff32833585dc45d28a69c0b269abb3fcd5c4ee09afc8ca32fa7e40d";
            var result = HashFactory.Crypto.SHA3.CreateBlueMidnightWish512().ComputeBytes(Converters.ConvertHexStringToBytes(paramIn));
            Assert.AreEqual(paramOut, Converters.ConvertBytesToHexString(result.GetBytes(), false).ToLower());
        }

        [TestMethod]
        public void hashGroestl()
        {
            var paramIn = "b2e1d72db8a3807d6d929a0e1349250cae0e99475d94bd869d0163755574a89078e08f604ff32833585dc45d28a69c0b269abb3fcd5c4ee09afc8ca32fa7e40d";
            var paramOut = "317024467e25cb6f1014f1b7a98c63b2ccc925b05a72180b0cdf23f42fabe653ddf51d11ce471dca48282b22261bbc7f5a729189c52554443a635889c7d47db6";
            var result = HashFactory.Crypto.SHA3.CreateGroestl512().ComputeBytes(Converters.ConvertHexStringToBytes(paramIn));
            Assert.AreEqual(paramOut, Converters.ConvertBytesToHexString(result.GetBytes(), false).ToLower());
        }

        [TestMethod]
        public void hashSkein()
        {
            var paramIn = "317024467e25cb6f1014f1b7a98c63b2ccc925b05a72180b0cdf23f42fabe653ddf51d11ce471dca48282b22261bbc7f5a729189c52554443a635889c7d47db6";
            var paramOut = "a4d126f16372bd2df3e22bc95f61e696a72a1bee32e62ca90fedc24e94dbdf314446dc00a5e6bc2907d73c7210e6cb780be00b49b26b7a6f2db29249f2bd884b";
            var result = HashFactory.Crypto.SHA3.CreateSkein512().ComputeBytes(Converters.ConvertHexStringToBytes(paramIn));
            Assert.AreEqual(paramOut, Converters.ConvertBytesToHexString(result.GetBytes(), false).ToLower());
        }

        [TestMethod]
        public void hashJH()
        {
            var paramIn = "a4d126f16372bd2df3e22bc95f61e696a72a1bee32e62ca90fedc24e94dbdf314446dc00a5e6bc2907d73c7210e6cb780be00b49b26b7a6f2db29249f2bd884b";
            var paramOut = "c295dd0155177a9104a80ec27b245600f0de17db4aee4a16a1cf386db29b6a8e5ea74c32bb6c317f388f6585d4b338e53959399e75fcaa16045a4094da19cb6d";
            var result = HashFactory.Crypto.SHA3.CreateJH512().ComputeBytes(Converters.ConvertHexStringToBytes(paramIn));
            Assert.AreEqual(paramOut, Converters.ConvertBytesToHexString(result.GetBytes(), false).ToLower());
        }

        [TestMethod]
        public void hashKeccak()
        {
            var paramIn = "c295dd0155177a9104a80ec27b245600f0de17db4aee4a16a1cf386db29b6a8e5ea74c32bb6c317f388f6585d4b338e53959399e75fcaa16045a4094da19cb6d";
            var paramOut = "c4f7a14f01cab51c317b7b0064932004ac72a85d8686a9165e1f8b8a968113cd7a3398554ef1c92a3c296c192f9314a2365bc0f7775d4e478787055a9b2ce897";
            var result = HashFactory.Crypto.SHA3.CreateKeccak512().ComputeBytes(Converters.ConvertHexStringToBytes(paramIn));
            Assert.AreEqual(paramOut, Converters.ConvertBytesToHexString(result.GetBytes(), false).ToLower());
        }

        [TestMethod]
        public void hashLuffa()
        {
            var paramIn = "c4f7a14f01cab51c317b7b0064932004ac72a85d8686a9165e1f8b8a968113cd7a3398554ef1c92a3c296c192f9314a2365bc0f7775d4e478787055a9b2ce897";
            var paramOut = "8bc3589bea395cdd461226ccbea9cfa463edc5d556ff8c60f8053502135781747ae56b521ced7208fcf6c30dc6f9169b51f5452021b6951fa3d8240f3972d740";
            var result = HashFactory.Crypto.SHA3.CreateLuffa512().ComputeBytes(Converters.ConvertHexStringToBytes(paramIn));
            Assert.AreEqual(paramOut, Converters.ConvertBytesToHexString(result.GetBytes(), false).ToLower());
        }

        [TestMethod]
        public void hashCubeHash()
        {
            var paramIn = "8bc3589bea395cdd461226ccbea9cfa463edc5d556ff8c60f8053502135781747ae56b521ced7208fcf6c30dc6f9169b51f5452021b6951fa3d8240f3972d740";
            var paramOut = "50ddc199803de46305083d0852bc4005fc473ed05ec56347ae65e9875c0571da7375bb227678805e7ef868015bd4bf714bae038937538dd7819cc58b6d03ca7b";
            var result = HashFactory.Crypto.SHA3.CreateCubeHash512().ComputeBytes(Converters.ConvertHexStringToBytes(paramIn));
            Assert.AreEqual(paramOut, Converters.ConvertBytesToHexString(result.GetBytes(), false).ToLower());
        }

        [TestMethod]
        public void hashSHAvite3()
        {
            var paramIn = "50ddc199803de46305083d0852bc4005fc473ed05ec56347ae65e9875c0571da7375bb227678805e7ef868015bd4bf714bae038937538dd7819cc58b6d03ca7b";
            var paramOut = "0bb309f45b7ec5b115a3318f0b2f0e431c8e415a3d6848087e7905e4e47c52874b79947e4bdee71668d1b1487716da57ac1f8d87e149ce1eee9080d6cc2827df";
            var result = HashFactory.Crypto.SHA3.CreateSHAvite3_512().ComputeBytes(Converters.ConvertHexStringToBytes(paramIn));
            Assert.AreEqual(paramOut, Converters.ConvertBytesToHexString(result.GetBytes(), false).ToLower());
        }

        [TestMethod]
        public void hashSIMD()
        {
            var paramIn = "0bb309f45b7ec5b115a3318f0b2f0e431c8e415a3d6848087e7905e4e47c52874b79947e4bdee71668d1b1487716da57ac1f8d87e149ce1eee9080d6cc2827df";
            var paramOut = "921ca1f5fc388ff8217e5bc787acb7e5b462063c12dca18b56b8bff0791d5c338b6604b74cd2c77ed7ac3a5a3843deb27e82f077c71a11a7308fc90864a0bd89";
            var result = HashFactory.Crypto.SHA3.CreateSIMD512().ComputeBytes(Converters.ConvertHexStringToBytes(paramIn));
            Assert.AreEqual(paramOut, Converters.ConvertBytesToHexString(result.GetBytes(), false).ToLower());
        }

        [TestMethod]
        public void hashEcho()
        {
            var paramIn = "921ca1f5fc388ff8217e5bc787acb7e5b462063c12dca18b56b8bff0791d5c338b6604b74cd2c77ed7ac3a5a3843deb27e82f077c71a11a7308fc90864a0bd89";
            var paramOut = "ad8f8a4b105ffb83bb7546da799e29caa5bc9f2d0b584bdbf7d3275c65bdaae849e277187321d7d323e827c901530f6073bb967a198f3e3ba52c3a01716a442b";
            var result = HashFactory.Crypto.SHA3.CreateEcho512().ComputeBytes(Converters.ConvertHexStringToBytes(paramIn));
            Assert.AreEqual(paramOut, Converters.ConvertBytesToHexString(result.GetBytes(), false).ToLower());
        }

        [TestMethod]
        public void hashHamsi()
        {
            var paramIn = "ad8f8a4b105ffb83bb7546da799e29caa5bc9f2d0b584bdbf7d3275c65bdaae849e277187321d7d323e827c901530f6073bb967a198f3e3ba52c3a01716a442b";
            var paramOut = "73ed6f3bd1805c003de63ae11f76630d35602c1a1b9504ba3f42233176425213622c9c630c830175b4f8a81f633e8bb98c663e142bcc88b0baaa7dd9e73a6907";
            var result = HashFactory.Crypto.SHA3.CreateHamsi512().ComputeBytes(Converters.ConvertHexStringToBytes(paramIn));
            Assert.AreEqual(paramOut, Converters.ConvertBytesToHexString(result.GetBytes(), false).ToLower());
        }

        [TestMethod]
        public void hashFugue()
        {
            var paramIn = "73ed6f3bd1805c003de63ae11f76630d35602c1a1b9504ba3f42233176425213622c9c630c830175b4f8a81f633e8bb98c663e142bcc88b0baaa7dd9e73a6907";
            var paramOut = "af72d939050259913e440b23bee62e3b9604129ec8424d265a6ee4916e060000e51eead6ded2b584283ac0e04c1ea582e1a757245b5e8c408520216139e17848";
            var result = HashFactory.Crypto.SHA3.CreateFugue512().ComputeBytes(Converters.ConvertHexStringToBytes(paramIn));
            Assert.AreEqual(paramOut, Converters.ConvertBytesToHexString(result.GetBytes(), false).ToLower());
        }
    }
}

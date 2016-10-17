using System;

namespace HashLib.Crypto.SHA3
{
    internal class SHAvite3_224 : SHAvite3_256Base
    {
        public SHAvite3_224()
            : base(HashLib.HashSize.HashSize224)
        {
        }
    }

    internal class SHAvite3_256 : SHAvite3_256Base
    {
        public SHAvite3_256()
            : base(HashLib.HashSize.HashSize256)
        {
        }
    }

    internal class SHAvite3_384 : SHAvite3_512Base
    {
        public SHAvite3_384()
            : base(HashLib.HashSize.HashSize384)
        {
        }
    }

    internal class SHAvite3_512 : SHAvite3_512Base
    {
        public SHAvite3_512()
            : base(HashLib.HashSize.HashSize512)
        {
        }
    }

    internal abstract class SHAvite3Base : BlockHash, ICryptoNotBuildIn
    {
        #region Consts

        protected static readonly uint[] Table0 =
        {
            0xc66363a5U, 0xf87c7c84U, 0xee777799U, 0xf67b7b8dU, 0xfff2f20dU, 0xd66b6bbdU,
            0xde6f6fb1U, 0x91c5c554U, 0x60303050U, 0x02010103U, 0xce6767a9U, 0x562b2b7dU,
            0xe7fefe19U, 0xb5d7d762U, 0x4dababe6U, 0xec76769aU, 0x8fcaca45U, 0x1f82829dU,
            0x89c9c940U, 0xfa7d7d87U, 0xeffafa15U, 0xb25959ebU, 0x8e4747c9U, 0xfbf0f00bU,
            0x41adadecU, 0xb3d4d467U, 0x5fa2a2fdU, 0x45afafeaU, 0x239c9cbfU, 0x53a4a4f7U,
            0xe4727296U, 0x9bc0c05bU, 0x75b7b7c2U, 0xe1fdfd1cU, 0x3d9393aeU, 0x4c26266aU,
            0x6c36365aU, 0x7e3f3f41U, 0xf5f7f702U, 0x83cccc4fU, 0x6834345cU, 0x51a5a5f4U,
            0xd1e5e534U, 0xf9f1f108U, 0xe2717193U, 0xabd8d873U, 0x62313153U, 0x2a15153fU,
            0x0804040cU, 0x95c7c752U, 0x46232365U, 0x9dc3c35eU, 0x30181828U, 0x379696a1U,
            0x0a05050fU, 0x2f9a9ab5U, 0x0e070709U, 0x24121236U, 0x1b80809bU, 0xdfe2e23dU,
            0xcdebeb26U, 0x4e272769U, 0x7fb2b2cdU, 0xea75759fU, 0x1209091bU, 0x1d83839eU,
            0x582c2c74U, 0x341a1a2eU, 0x361b1b2dU, 0xdc6e6eb2U, 0xb45a5aeeU, 0x5ba0a0fbU,
            0xa45252f6U, 0x763b3b4dU, 0xb7d6d661U, 0x7db3b3ceU, 0x5229297bU, 0xdde3e33eU,
            0x5e2f2f71U, 0x13848497U, 0xa65353f5U, 0xb9d1d168U, 0x00000000U, 0xc1eded2cU,
            0x40202060U, 0xe3fcfc1fU, 0x79b1b1c8U, 0xb65b5bedU, 0xd46a6abeU, 0x8dcbcb46U,
            0x67bebed9U, 0x7239394bU, 0x944a4adeU, 0x984c4cd4U, 0xb05858e8U, 0x85cfcf4aU,
            0xbbd0d06bU, 0xc5efef2aU, 0x4faaaae5U, 0xedfbfb16U, 0x864343c5U, 0x9a4d4dd7U,
            0x66333355U, 0x11858594U, 0x8a4545cfU, 0xe9f9f910U, 0x04020206U, 0xfe7f7f81U,
            0xa05050f0U, 0x783c3c44U, 0x259f9fbaU, 0x4ba8a8e3U, 0xa25151f3U, 0x5da3a3feU,
            0x804040c0U, 0x058f8f8aU, 0x3f9292adU, 0x219d9dbcU, 0x70383848U, 0xf1f5f504U,
            0x63bcbcdfU, 0x77b6b6c1U, 0xafdada75U, 0x42212163U, 0x20101030U, 0xe5ffff1aU,
            0xfdf3f30eU, 0xbfd2d26dU, 0x81cdcd4cU, 0x180c0c14U, 0x26131335U, 0xc3ecec2fU,
            0xbe5f5fe1U, 0x359797a2U, 0x884444ccU, 0x2e171739U, 0x93c4c457U, 0x55a7a7f2U,
            0xfc7e7e82U, 0x7a3d3d47U, 0xc86464acU, 0xba5d5de7U, 0x3219192bU, 0xe6737395U,
            0xc06060a0U, 0x19818198U, 0x9e4f4fd1U, 0xa3dcdc7fU, 0x44222266U, 0x542a2a7eU,
            0x3b9090abU, 0x0b888883U, 0x8c4646caU, 0xc7eeee29U, 0x6bb8b8d3U, 0x2814143cU,
            0xa7dede79U, 0xbc5e5ee2U, 0x160b0b1dU, 0xaddbdb76U, 0xdbe0e03bU, 0x64323256U,
            0x743a3a4eU, 0x140a0a1eU, 0x924949dbU, 0x0c06060aU, 0x4824246cU, 0xb85c5ce4U,
            0x9fc2c25dU, 0xbdd3d36eU, 0x43acacefU, 0xc46262a6U, 0x399191a8U, 0x319595a4U,
            0xd3e4e437U, 0xf279798bU, 0xd5e7e732U, 0x8bc8c843U, 0x6e373759U, 0xda6d6db7U,
            0x018d8d8cU, 0xb1d5d564U, 0x9c4e4ed2U, 0x49a9a9e0U, 0xd86c6cb4U, 0xac5656faU,
            0xf3f4f407U, 0xcfeaea25U, 0xca6565afU, 0xf47a7a8eU, 0x47aeaee9U, 0x10080818U,
            0x6fbabad5U, 0xf0787888U, 0x4a25256fU, 0x5c2e2e72U, 0x381c1c24U, 0x57a6a6f1U,
            0x73b4b4c7U, 0x97c6c651U, 0xcbe8e823U, 0xa1dddd7cU, 0xe874749cU, 0x3e1f1f21U,
            0x964b4bddU, 0x61bdbddcU, 0x0d8b8b86U, 0x0f8a8a85U, 0xe0707090U, 0x7c3e3e42U,
            0x71b5b5c4U, 0xcc6666aaU, 0x904848d8U, 0x06030305U, 0xf7f6f601U, 0x1c0e0e12U,
            0xc26161a3U, 0x6a35355fU, 0xae5757f9U, 0x69b9b9d0U, 0x17868691U, 0x99c1c158U,
            0x3a1d1d27U, 0x279e9eb9U, 0xd9e1e138U, 0xebf8f813U, 0x2b9898b3U, 0x22111133U,
            0xd26969bbU, 0xa9d9d970U, 0x078e8e89U, 0x339494a7U, 0x2d9b9bb6U, 0x3c1e1e22U,
            0x15878792U, 0xc9e9e920U, 0x87cece49U, 0xaa5555ffU, 0x50282878U, 0xa5dfdf7aU,
            0x038c8c8fU, 0x59a1a1f8U, 0x09898980U, 0x1a0d0d17U, 0x65bfbfdaU, 0xd7e6e631U,
            0x844242c6U, 0xd06868b8U, 0x824141c3U, 0x299999b0U, 0x5a2d2d77U, 0x1e0f0f11U,
            0x7bb0b0cbU, 0xa85454fcU, 0x6dbbbbd6U, 0x2c16163aU,
        };

        protected static readonly uint[] Table1 =
        {
            0xa5c66363U, 0x84f87c7cU, 0x99ee7777U, 0x8df67b7bU, 0x0dfff2f2U, 0xbdd66b6bU,
            0xb1de6f6fU, 0x5491c5c5U, 0x50603030U, 0x03020101U, 0xa9ce6767U, 0x7d562b2bU,
            0x19e7fefeU, 0x62b5d7d7U, 0xe64dababU, 0x9aec7676U, 0x458fcacaU, 0x9d1f8282U,
            0x4089c9c9U, 0x87fa7d7dU, 0x15effafaU, 0xebb25959U, 0xc98e4747U, 0x0bfbf0f0U,
            0xec41adadU, 0x67b3d4d4U, 0xfd5fa2a2U, 0xea45afafU, 0xbf239c9cU, 0xf753a4a4U,
            0x96e47272U, 0x5b9bc0c0U, 0xc275b7b7U, 0x1ce1fdfdU, 0xae3d9393U, 0x6a4c2626U,
            0x5a6c3636U, 0x417e3f3fU, 0x02f5f7f7U, 0x4f83ccccU, 0x5c683434U, 0xf451a5a5U,
            0x34d1e5e5U, 0x08f9f1f1U, 0x93e27171U, 0x73abd8d8U, 0x53623131U, 0x3f2a1515U,
            0x0c080404U, 0x5295c7c7U, 0x65462323U, 0x5e9dc3c3U, 0x28301818U, 0xa1379696U,
            0x0f0a0505U, 0xb52f9a9aU, 0x090e0707U, 0x36241212U, 0x9b1b8080U, 0x3ddfe2e2U,
            0x26cdebebU, 0x694e2727U, 0xcd7fb2b2U, 0x9fea7575U, 0x1b120909U, 0x9e1d8383U,
            0x74582c2cU, 0x2e341a1aU, 0x2d361b1bU, 0xb2dc6e6eU, 0xeeb45a5aU, 0xfb5ba0a0U,
            0xf6a45252U, 0x4d763b3bU, 0x61b7d6d6U, 0xce7db3b3U, 0x7b522929U, 0x3edde3e3U,
            0x715e2f2fU, 0x97138484U, 0xf5a65353U, 0x68b9d1d1U, 0x00000000U, 0x2cc1ededU,
            0x60402020U, 0x1fe3fcfcU, 0xc879b1b1U, 0xedb65b5bU, 0xbed46a6aU, 0x468dcbcbU,
            0xd967bebeU, 0x4b723939U, 0xde944a4aU, 0xd4984c4cU, 0xe8b05858U, 0x4a85cfcfU,
            0x6bbbd0d0U, 0x2ac5efefU, 0xe54faaaaU, 0x16edfbfbU, 0xc5864343U, 0xd79a4d4dU,
            0x55663333U, 0x94118585U, 0xcf8a4545U, 0x10e9f9f9U, 0x06040202U, 0x81fe7f7fU,
            0xf0a05050U, 0x44783c3cU, 0xba259f9fU, 0xe34ba8a8U, 0xf3a25151U, 0xfe5da3a3U,
            0xc0804040U, 0x8a058f8fU, 0xad3f9292U, 0xbc219d9dU, 0x48703838U, 0x04f1f5f5U,
            0xdf63bcbcU, 0xc177b6b6U, 0x75afdadaU, 0x63422121U, 0x30201010U, 0x1ae5ffffU,
            0x0efdf3f3U, 0x6dbfd2d2U, 0x4c81cdcdU, 0x14180c0cU, 0x35261313U, 0x2fc3ececU,
            0xe1be5f5fU, 0xa2359797U, 0xcc884444U, 0x392e1717U, 0x5793c4c4U, 0xf255a7a7U,
            0x82fc7e7eU, 0x477a3d3dU, 0xacc86464U, 0xe7ba5d5dU, 0x2b321919U, 0x95e67373U,
            0xa0c06060U, 0x98198181U, 0xd19e4f4fU, 0x7fa3dcdcU, 0x66442222U, 0x7e542a2aU,
            0xab3b9090U, 0x830b8888U, 0xca8c4646U, 0x29c7eeeeU, 0xd36bb8b8U, 0x3c281414U,
            0x79a7dedeU, 0xe2bc5e5eU, 0x1d160b0bU, 0x76addbdbU, 0x3bdbe0e0U, 0x56643232U,
            0x4e743a3aU, 0x1e140a0aU, 0xdb924949U, 0x0a0c0606U, 0x6c482424U, 0xe4b85c5cU,
            0x5d9fc2c2U, 0x6ebdd3d3U, 0xef43acacU, 0xa6c46262U, 0xa8399191U, 0xa4319595U,
            0x37d3e4e4U, 0x8bf27979U, 0x32d5e7e7U, 0x438bc8c8U, 0x596e3737U, 0xb7da6d6dU,
            0x8c018d8dU, 0x64b1d5d5U, 0xd29c4e4eU, 0xe049a9a9U, 0xb4d86c6cU, 0xfaac5656U,
            0x07f3f4f4U, 0x25cfeaeaU, 0xafca6565U, 0x8ef47a7aU, 0xe947aeaeU, 0x18100808U,
            0xd56fbabaU, 0x88f07878U, 0x6f4a2525U, 0x725c2e2eU, 0x24381c1cU, 0xf157a6a6U,
            0xc773b4b4U, 0x5197c6c6U, 0x23cbe8e8U, 0x7ca1ddddU, 0x9ce87474U, 0x213e1f1fU,
            0xdd964b4bU, 0xdc61bdbdU, 0x860d8b8bU, 0x850f8a8aU, 0x90e07070U, 0x427c3e3eU,
            0xc471b5b5U, 0xaacc6666U, 0xd8904848U, 0x05060303U, 0x01f7f6f6U, 0x121c0e0eU,
            0xa3c26161U, 0x5f6a3535U, 0xf9ae5757U, 0xd069b9b9U, 0x91178686U, 0x5899c1c1U,
            0x273a1d1dU, 0xb9279e9eU, 0x38d9e1e1U, 0x13ebf8f8U, 0xb32b9898U, 0x33221111U,
            0xbbd26969U, 0x70a9d9d9U, 0x89078e8eU, 0xa7339494U, 0xb62d9b9bU, 0x223c1e1eU,
            0x92158787U, 0x20c9e9e9U, 0x4987ceceU, 0xffaa5555U, 0x78502828U, 0x7aa5dfdfU,
            0x8f038c8cU, 0xf859a1a1U, 0x80098989U, 0x171a0d0dU, 0xda65bfbfU, 0x31d7e6e6U,
            0xc6844242U, 0xb8d06868U, 0xc3824141U, 0xb0299999U, 0x775a2d2dU, 0x111e0f0fU,
            0xcb7bb0b0U, 0xfca85454U, 0xd66dbbbbU, 0x3a2c1616U,
        };

        protected static readonly uint[] Table2 =
        {
            0x63a5c663U, 0x7c84f87cU, 0x7799ee77U, 0x7b8df67bU, 0xf20dfff2U, 0x6bbdd66bU,
            0x6fb1de6fU, 0xc55491c5U, 0x30506030U, 0x01030201U, 0x67a9ce67U, 0x2b7d562bU,
            0xfe19e7feU, 0xd762b5d7U, 0xabe64dabU, 0x769aec76U, 0xca458fcaU, 0x829d1f82U,
            0xc94089c9U, 0x7d87fa7dU, 0xfa15effaU, 0x59ebb259U, 0x47c98e47U, 0xf00bfbf0U,
            0xadec41adU, 0xd467b3d4U, 0xa2fd5fa2U, 0xafea45afU, 0x9cbf239cU, 0xa4f753a4U,
            0x7296e472U, 0xc05b9bc0U, 0xb7c275b7U, 0xfd1ce1fdU, 0x93ae3d93U, 0x266a4c26U,
            0x365a6c36U, 0x3f417e3fU, 0xf702f5f7U, 0xcc4f83ccU, 0x345c6834U, 0xa5f451a5U,
            0xe534d1e5U, 0xf108f9f1U, 0x7193e271U, 0xd873abd8U, 0x31536231U, 0x153f2a15U,
            0x040c0804U, 0xc75295c7U, 0x23654623U, 0xc35e9dc3U, 0x18283018U, 0x96a13796U,
            0x050f0a05U, 0x9ab52f9aU, 0x07090e07U, 0x12362412U, 0x809b1b80U, 0xe23ddfe2U,
            0xeb26cdebU, 0x27694e27U, 0xb2cd7fb2U, 0x759fea75U, 0x091b1209U, 0x839e1d83U,
            0x2c74582cU, 0x1a2e341aU, 0x1b2d361bU, 0x6eb2dc6eU, 0x5aeeb45aU, 0xa0fb5ba0U,
            0x52f6a452U, 0x3b4d763bU, 0xd661b7d6U, 0xb3ce7db3U, 0x297b5229U, 0xe33edde3U,
            0x2f715e2fU, 0x84971384U, 0x53f5a653U, 0xd168b9d1U, 0x00000000U, 0xed2cc1edU,
            0x20604020U, 0xfc1fe3fcU, 0xb1c879b1U, 0x5bedb65bU, 0x6abed46aU, 0xcb468dcbU,
            0xbed967beU, 0x394b7239U, 0x4ade944aU, 0x4cd4984cU, 0x58e8b058U, 0xcf4a85cfU,
            0xd06bbbd0U, 0xef2ac5efU, 0xaae54faaU, 0xfb16edfbU, 0x43c58643U, 0x4dd79a4dU,
            0x33556633U, 0x85941185U, 0x45cf8a45U, 0xf910e9f9U, 0x02060402U, 0x7f81fe7fU,
            0x50f0a050U, 0x3c44783cU, 0x9fba259fU, 0xa8e34ba8U, 0x51f3a251U, 0xa3fe5da3U,
            0x40c08040U, 0x8f8a058fU, 0x92ad3f92U, 0x9dbc219dU, 0x38487038U, 0xf504f1f5U,
            0xbcdf63bcU, 0xb6c177b6U, 0xda75afdaU, 0x21634221U, 0x10302010U, 0xff1ae5ffU,
            0xf30efdf3U, 0xd26dbfd2U, 0xcd4c81cdU, 0x0c14180cU, 0x13352613U, 0xec2fc3ecU,
            0x5fe1be5fU, 0x97a23597U, 0x44cc8844U, 0x17392e17U, 0xc45793c4U, 0xa7f255a7U,
            0x7e82fc7eU, 0x3d477a3dU, 0x64acc864U, 0x5de7ba5dU, 0x192b3219U, 0x7395e673U,
            0x60a0c060U, 0x81981981U, 0x4fd19e4fU, 0xdc7fa3dcU, 0x22664422U, 0x2a7e542aU,
            0x90ab3b90U, 0x88830b88U, 0x46ca8c46U, 0xee29c7eeU, 0xb8d36bb8U, 0x143c2814U,
            0xde79a7deU, 0x5ee2bc5eU, 0x0b1d160bU, 0xdb76addbU, 0xe03bdbe0U, 0x32566432U,
            0x3a4e743aU, 0x0a1e140aU, 0x49db9249U, 0x060a0c06U, 0x246c4824U, 0x5ce4b85cU,
            0xc25d9fc2U, 0xd36ebdd3U, 0xacef43acU, 0x62a6c462U, 0x91a83991U, 0x95a43195U,
            0xe437d3e4U, 0x798bf279U, 0xe732d5e7U, 0xc8438bc8U, 0x37596e37U, 0x6db7da6dU,
            0x8d8c018dU, 0xd564b1d5U, 0x4ed29c4eU, 0xa9e049a9U, 0x6cb4d86cU, 0x56faac56U,
            0xf407f3f4U, 0xea25cfeaU, 0x65afca65U, 0x7a8ef47aU, 0xaee947aeU, 0x08181008U,
            0xbad56fbaU, 0x7888f078U, 0x256f4a25U, 0x2e725c2eU, 0x1c24381cU, 0xa6f157a6U,
            0xb4c773b4U, 0xc65197c6U, 0xe823cbe8U, 0xdd7ca1ddU, 0x749ce874U, 0x1f213e1fU,
            0x4bdd964bU, 0xbddc61bdU, 0x8b860d8bU, 0x8a850f8aU, 0x7090e070U, 0x3e427c3eU,
            0xb5c471b5U, 0x66aacc66U, 0x48d89048U, 0x03050603U, 0xf601f7f6U, 0x0e121c0eU,
            0x61a3c261U, 0x355f6a35U, 0x57f9ae57U, 0xb9d069b9U, 0x86911786U, 0xc15899c1U,
            0x1d273a1dU, 0x9eb9279eU, 0xe138d9e1U, 0xf813ebf8U, 0x98b32b98U, 0x11332211U,
            0x69bbd269U, 0xd970a9d9U, 0x8e89078eU, 0x94a73394U, 0x9bb62d9bU, 0x1e223c1eU,
            0x87921587U, 0xe920c9e9U, 0xce4987ceU, 0x55ffaa55U, 0x28785028U, 0xdf7aa5dfU,
            0x8c8f038cU, 0xa1f859a1U, 0x89800989U, 0x0d171a0dU, 0xbfda65bfU, 0xe631d7e6U,
            0x42c68442U, 0x68b8d068U, 0x41c38241U, 0x99b02999U, 0x2d775a2dU, 0x0f111e0fU,
            0xb0cb7bb0U, 0x54fca854U, 0xbbd66dbbU, 0x163a2c16U,
        };

        protected static readonly uint[] Table3 =
        {
            0x6363a5c6U, 0x7c7c84f8U, 0x777799eeU, 0x7b7b8df6U, 0xf2f20dffU, 0x6b6bbdd6U,
            0x6f6fb1deU, 0xc5c55491U, 0x30305060U, 0x01010302U, 0x6767a9ceU, 0x2b2b7d56U,
            0xfefe19e7U, 0xd7d762b5U, 0xababe64dU, 0x76769aecU, 0xcaca458fU, 0x82829d1fU,
            0xc9c94089U, 0x7d7d87faU, 0xfafa15efU, 0x5959ebb2U, 0x4747c98eU, 0xf0f00bfbU,
            0xadadec41U, 0xd4d467b3U, 0xa2a2fd5fU, 0xafafea45U, 0x9c9cbf23U, 0xa4a4f753U,
            0x727296e4U, 0xc0c05b9bU, 0xb7b7c275U, 0xfdfd1ce1U, 0x9393ae3dU, 0x26266a4cU,
            0x36365a6cU, 0x3f3f417eU, 0xf7f702f5U, 0xcccc4f83U, 0x34345c68U, 0xa5a5f451U,
            0xe5e534d1U, 0xf1f108f9U, 0x717193e2U, 0xd8d873abU, 0x31315362U, 0x15153f2aU,
            0x04040c08U, 0xc7c75295U, 0x23236546U, 0xc3c35e9dU, 0x18182830U, 0x9696a137U,
            0x05050f0aU, 0x9a9ab52fU, 0x0707090eU, 0x12123624U, 0x80809b1bU, 0xe2e23ddfU,
            0xebeb26cdU, 0x2727694eU, 0xb2b2cd7fU, 0x75759feaU, 0x09091b12U, 0x83839e1dU,
            0x2c2c7458U, 0x1a1a2e34U, 0x1b1b2d36U, 0x6e6eb2dcU, 0x5a5aeeb4U, 0xa0a0fb5bU,
            0x5252f6a4U, 0x3b3b4d76U, 0xd6d661b7U, 0xb3b3ce7dU, 0x29297b52U, 0xe3e33eddU,
            0x2f2f715eU, 0x84849713U, 0x5353f5a6U, 0xd1d168b9U, 0x00000000U, 0xeded2cc1U,
            0x20206040U, 0xfcfc1fe3U, 0xb1b1c879U, 0x5b5bedb6U, 0x6a6abed4U, 0xcbcb468dU,
            0xbebed967U, 0x39394b72U, 0x4a4ade94U, 0x4c4cd498U, 0x5858e8b0U, 0xcfcf4a85U,
            0xd0d06bbbU, 0xefef2ac5U, 0xaaaae54fU, 0xfbfb16edU, 0x4343c586U, 0x4d4dd79aU,
            0x33335566U, 0x85859411U, 0x4545cf8aU, 0xf9f910e9U, 0x02020604U, 0x7f7f81feU,
            0x5050f0a0U, 0x3c3c4478U, 0x9f9fba25U, 0xa8a8e34bU, 0x5151f3a2U, 0xa3a3fe5dU,
            0x4040c080U, 0x8f8f8a05U, 0x9292ad3fU, 0x9d9dbc21U, 0x38384870U, 0xf5f504f1U,
            0xbcbcdf63U, 0xb6b6c177U, 0xdada75afU, 0x21216342U, 0x10103020U, 0xffff1ae5U,
            0xf3f30efdU, 0xd2d26dbfU, 0xcdcd4c81U, 0x0c0c1418U, 0x13133526U, 0xecec2fc3U,
            0x5f5fe1beU, 0x9797a235U, 0x4444cc88U, 0x1717392eU, 0xc4c45793U, 0xa7a7f255U,
            0x7e7e82fcU, 0x3d3d477aU, 0x6464acc8U, 0x5d5de7baU, 0x19192b32U, 0x737395e6U,
            0x6060a0c0U, 0x81819819U, 0x4f4fd19eU, 0xdcdc7fa3U, 0x22226644U, 0x2a2a7e54U,
            0x9090ab3bU, 0x8888830bU, 0x4646ca8cU, 0xeeee29c7U, 0xb8b8d36bU, 0x14143c28U,
            0xdede79a7U, 0x5e5ee2bcU, 0x0b0b1d16U, 0xdbdb76adU, 0xe0e03bdbU, 0x32325664U,
            0x3a3a4e74U, 0x0a0a1e14U, 0x4949db92U, 0x06060a0cU, 0x24246c48U, 0x5c5ce4b8U,
            0xc2c25d9fU, 0xd3d36ebdU, 0xacacef43U, 0x6262a6c4U, 0x9191a839U, 0x9595a431U,
            0xe4e437d3U, 0x79798bf2U, 0xe7e732d5U, 0xc8c8438bU, 0x3737596eU, 0x6d6db7daU,
            0x8d8d8c01U, 0xd5d564b1U, 0x4e4ed29cU, 0xa9a9e049U, 0x6c6cb4d8U, 0x5656faacU,
            0xf4f407f3U, 0xeaea25cfU, 0x6565afcaU, 0x7a7a8ef4U, 0xaeaee947U, 0x08081810U,
            0xbabad56fU, 0x787888f0U, 0x25256f4aU, 0x2e2e725cU, 0x1c1c2438U, 0xa6a6f157U,
            0xb4b4c773U, 0xc6c65197U, 0xe8e823cbU, 0xdddd7ca1U, 0x74749ce8U, 0x1f1f213eU,
            0x4b4bdd96U, 0xbdbddc61U, 0x8b8b860dU, 0x8a8a850fU, 0x707090e0U, 0x3e3e427cU,
            0xb5b5c471U, 0x6666aaccU, 0x4848d890U, 0x03030506U, 0xf6f601f7U, 0x0e0e121cU,
            0x6161a3c2U, 0x35355f6aU, 0x5757f9aeU, 0xb9b9d069U, 0x86869117U, 0xc1c15899U,
            0x1d1d273aU, 0x9e9eb927U, 0xe1e138d9U, 0xf8f813ebU, 0x9898b32bU, 0x11113322U,
            0x6969bbd2U, 0xd9d970a9U, 0x8e8e8907U, 0x9494a733U, 0x9b9bb62dU, 0x1e1e223cU,
            0x87879215U, 0xe9e920c9U, 0xcece4987U, 0x5555ffaaU, 0x28287850U, 0xdfdf7aa5U,
            0x8c8c8f03U, 0xa1a1f859U, 0x89898009U, 0x0d0d171aU, 0xbfbfda65U, 0xe6e631d7U,
            0x4242c684U, 0x6868b8d0U, 0x4141c382U, 0x9999b029U, 0x2d2d775aU, 0x0f0f111eU,
            0xb0b0cb7bU, 0x5454fca8U, 0xbbbbd66dU, 0x16163a2cU,
        };

        #endregion

        protected readonly uint[] m_state;

        protected override byte[] GetResult()
        {
            return Converters.ConvertUIntsToBytes(m_state, 0, HashSize / 4);
        }

        public SHAvite3Base(HashLib.HashSize a_hash_size, int a_block_size)
            : base((int)a_hash_size, a_block_size)
        {
            m_state = new uint[BlockSize / 4];
        }
    };

    internal abstract class SHAvite3_256Base : SHAvite3Base
    {
        #region Consts

        private static readonly uint[] IV_224 =  
        {
            0xC4C67795, 0xC0B1817F, 0xEAD88924, 0x1ABB1BB0,
            0xE0C29152, 0xBDE046BA, 0xAEEECF99, 0x58D509D8
        };

        private static readonly uint[] IV_256 =  
        {
            0x3EECF551, 0xBF10819B, 0xE6DC8559, 0xF3E23FD5,
            0x431AEC73, 0x79E3F731, 0x98325F05, 0xA92A31F1
        };

        #endregion

        protected override void TransformBlock(byte[] a_data, int a_index)
        {
            uint x0, x1, x2, x3, y0, y1, y2, y3;

            uint[] rk = new uint[144];
            Converters.ConvertBytesToUInts(a_data, a_index, 64, rk);

            uint cnt0 = (uint)(m_processed_bytes << 3);
            uint cnt1 = (uint)(m_processed_bytes >> 29);

            uint state0 = m_state[0];
            uint state1 = m_state[1];
            uint state2 = m_state[2];
            uint state3 = m_state[3];
            uint state4 = m_state[4];
            uint state5 = m_state[5];
            uint state6 = m_state[6];
            uint state7 = m_state[7];

            x0 = rk[1];
            x1 = rk[2];
            x2 = rk[3];
            x3 = rk[0];
            y0 = Table0[x0 >> 24] ^ Table1[(x1 >> 16) & 0xff] ^ Table2[(x2 >> 8) & 0xff] ^ Table3[x3 & 0xff];
            y1 = Table0[x1 >> 24] ^ Table1[(x2 >> 16) & 0xff] ^ Table2[(x3 >> 8) & 0xff] ^ Table3[x0 & 0xff];
            y2 = Table0[x2 >> 24] ^ Table1[(x3 >> 16) & 0xff] ^ Table2[(x0 >> 8) & 0xff] ^ Table3[x1 & 0xff];
            y3 = Table0[x3 >> 24] ^ Table1[(x0 >> 16) & 0xff] ^ Table2[(x1 >> 8) & 0xff] ^ Table3[x2 & 0xff];
            rk[16] = y0 ^ rk[12] ^ cnt0;
            rk[17] = y1 ^ rk[13] ^ ~cnt1;
            rk[18] = y2 ^ rk[14];
            rk[19] = y3 ^ rk[15];
            x0 = rk[5];
            x1 = rk[6];
            x2 = rk[7];
            x3 = rk[4];
            y0 = Table0[x0 >> 24] ^ Table1[(x1 >> 16) & 0xff] ^ Table2[(x2 >> 8) & 0xff] ^ Table3[x3 & 0xff];
            y1 = Table0[x1 >> 24] ^ Table1[(x2 >> 16) & 0xff] ^ Table2[(x3 >> 8) & 0xff] ^ Table3[x0 & 0xff];
            y2 = Table0[x2 >> 24] ^ Table1[(x3 >> 16) & 0xff] ^ Table2[(x0 >> 8) & 0xff] ^ Table3[x1 & 0xff];
            y3 = Table0[x3 >> 24] ^ Table1[(x0 >> 16) & 0xff] ^ Table2[(x1 >> 8) & 0xff] ^ Table3[x2 & 0xff];
            rk[20] = y0 ^ rk[16];
            rk[21] = y1 ^ rk[17];
            rk[22] = y2 ^ rk[18];
            rk[23] = y3 ^ rk[19];
            x0 = rk[9];
            x1 = rk[10];
            x2 = rk[11];
            x3 = rk[8];
            y0 = Table0[x0 >> 24] ^ Table1[(x1 >> 16) & 0xff] ^ Table2[(x2 >> 8) & 0xff] ^ Table3[x3 & 0xff];
            y1 = Table0[x1 >> 24] ^ Table1[(x2 >> 16) & 0xff] ^ Table2[(x3 >> 8) & 0xff] ^ Table3[x0 & 0xff];
            y2 = Table0[x2 >> 24] ^ Table1[(x3 >> 16) & 0xff] ^ Table2[(x0 >> 8) & 0xff] ^ Table3[x1 & 0xff];
            y3 = Table0[x3 >> 24] ^ Table1[(x0 >> 16) & 0xff] ^ Table2[(x1 >> 8) & 0xff] ^ Table3[x2 & 0xff];
            rk[24] = y0 ^ rk[20];
            rk[25] = y1 ^ rk[21];
            rk[26] = y2 ^ rk[22];
            rk[27] = y3 ^ rk[23];
            x0 = rk[13];
            x1 = rk[14];
            x2 = rk[15];
            x3 = rk[12];
            y0 = Table0[x0 >> 24] ^ Table1[(x1 >> 16) & 0xff] ^ Table2[(x2 >> 8) & 0xff] ^ Table3[x3 & 0xff];
            y1 = Table0[x1 >> 24] ^ Table1[(x2 >> 16) & 0xff] ^ Table2[(x3 >> 8) & 0xff] ^ Table3[x0 & 0xff];
            y2 = Table0[x2 >> 24] ^ Table1[(x3 >> 16) & 0xff] ^ Table2[(x0 >> 8) & 0xff] ^ Table3[x1 & 0xff];
            y3 = Table0[x3 >> 24] ^ Table1[(x0 >> 16) & 0xff] ^ Table2[(x1 >> 8) & 0xff] ^ Table3[x2 & 0xff];
            rk[28] = y0 ^ rk[24];
            rk[29] = y1 ^ rk[25];
            rk[30] = y2 ^ rk[26];
            rk[31] = y3 ^ rk[27];

            y0 = state4 ^ rk[0];
            y1 = state5 ^ rk[0 + 1];
            y2 = state6 ^ rk[0 + 2];
            y3 = state7 ^ rk[0 + 3];
            x0 = Table0[y0 >> 24] ^ Table1[(y1 >> 16) & 0xff] ^ Table2[(y2 >> 8) & 0xff] ^ Table3[y3 & 0xff] ^ rk[0 + 4];
            x1 = Table0[y1 >> 24] ^ Table1[(y2 >> 16) & 0xff] ^ Table2[(y3 >> 8) & 0xff] ^ Table3[y0 & 0xff] ^ rk[0 + 5];
            x2 = Table0[y2 >> 24] ^ Table1[(y3 >> 16) & 0xff] ^ Table2[(y0 >> 8) & 0xff] ^ Table3[y1 & 0xff] ^ rk[0 + 6];
            x3 = Table0[y3 >> 24] ^ Table1[(y0 >> 16) & 0xff] ^ Table2[(y1 >> 8) & 0xff] ^ Table3[y2 & 0xff] ^ rk[0 + 7];
            y0 = Table0[x0 >> 24] ^ Table1[(x1 >> 16) & 0xff] ^ Table2[(x2 >> 8) & 0xff] ^ Table3[x3 & 0xff] ^ rk[0 + 8];
            y1 = Table0[x1 >> 24] ^ Table1[(x2 >> 16) & 0xff] ^ Table2[(x3 >> 8) & 0xff] ^ Table3[x0 & 0xff] ^ rk[0 + 9];
            y2 = Table0[x2 >> 24] ^ Table1[(x3 >> 16) & 0xff] ^ Table2[(x0 >> 8) & 0xff] ^ Table3[x1 & 0xff] ^ rk[0 + 10];
            y3 = Table0[x3 >> 24] ^ Table1[(x0 >> 16) & 0xff] ^ Table2[(x1 >> 8) & 0xff] ^ Table3[x2 & 0xff] ^ rk[0 + 11];
            x0 = Table0[y0 >> 24] ^ Table1[(y1 >> 16) & 0xff] ^ Table2[(y2 >> 8) & 0xff] ^ Table3[y3 & 0xff];
            x1 = Table0[y1 >> 24] ^ Table1[(y2 >> 16) & 0xff] ^ Table2[(y3 >> 8) & 0xff] ^ Table3[y0 & 0xff];
            x2 = Table0[y2 >> 24] ^ Table1[(y3 >> 16) & 0xff] ^ Table2[(y0 >> 8) & 0xff] ^ Table3[y1 & 0xff];
            x3 = Table0[y3 >> 24] ^ Table1[(y0 >> 16) & 0xff] ^ Table2[(y1 >> 8) & 0xff] ^ Table3[y2 & 0xff];
            state0 ^= x0;
            state1 ^= x1;
            state2 ^= x2;
            state3 ^= x3;
            y0 = state0 ^ rk[0 + 12];
            y1 = state1 ^ rk[0 + 13];
            y2 = state2 ^ rk[0 + 14];
            y3 = state3 ^ rk[0 + 15];
            x0 = Table0[y0 >> 24] ^ Table1[(y1 >> 16) & 0xff] ^ Table2[(y2 >> 8) & 0xff] ^ Table3[y3 & 0xff] ^ rk[0 + 16];
            x1 = Table0[y1 >> 24] ^ Table1[(y2 >> 16) & 0xff] ^ Table2[(y3 >> 8) & 0xff] ^ Table3[y0 & 0xff] ^ rk[0 + 17];
            x2 = Table0[y2 >> 24] ^ Table1[(y3 >> 16) & 0xff] ^ Table2[(y0 >> 8) & 0xff] ^ Table3[y1 & 0xff] ^ rk[0 + 18];
            x3 = Table0[y3 >> 24] ^ Table1[(y0 >> 16) & 0xff] ^ Table2[(y1 >> 8) & 0xff] ^ Table3[y2 & 0xff] ^ rk[0 + 19];
            y0 = Table0[x0 >> 24] ^ Table1[(x1 >> 16) & 0xff] ^ Table2[(x2 >> 8) & 0xff] ^ Table3[x3 & 0xff] ^ rk[0 + 20];
            y1 = Table0[x1 >> 24] ^ Table1[(x2 >> 16) & 0xff] ^ Table2[(x3 >> 8) & 0xff] ^ Table3[x0 & 0xff] ^ rk[0 + 21];
            y2 = Table0[x2 >> 24] ^ Table1[(x3 >> 16) & 0xff] ^ Table2[(x0 >> 8) & 0xff] ^ Table3[x1 & 0xff] ^ rk[0 + 22];
            y3 = Table0[x3 >> 24] ^ Table1[(x0 >> 16) & 0xff] ^ Table2[(x1 >> 8) & 0xff] ^ Table3[x2 & 0xff] ^ rk[0 + 23];
            x0 = Table0[y0 >> 24] ^ Table1[(y1 >> 16) & 0xff] ^ Table2[(y2 >> 8) & 0xff] ^ Table3[y3 & 0xff];
            x1 = Table0[y1 >> 24] ^ Table1[(y2 >> 16) & 0xff] ^ Table2[(y3 >> 8) & 0xff] ^ Table3[y0 & 0xff];
            x2 = Table0[y2 >> 24] ^ Table1[(y3 >> 16) & 0xff] ^ Table2[(y0 >> 8) & 0xff] ^ Table3[y1 & 0xff];
            x3 = Table0[y3 >> 24] ^ Table1[(y0 >> 16) & 0xff] ^ Table2[(y1 >> 8) & 0xff] ^ Table3[y2 & 0xff];
            state4 ^= x0;
            state5 ^= x1;
            state6 ^= x2;
            state7 ^= x3;

            for (int i = 0; i < 16; i++)
                rk[32 + i] = rk[i + 32 - 16] ^ rk[i + 32 - 3];

            y0 = state4 ^ rk[24];
            y1 = state5 ^ rk[24 + 1];
            y2 = state6 ^ rk[24 + 2];
            y3 = state7 ^ rk[24 + 3];
            x0 = Table0[y0 >> 24] ^ Table1[(y1 >> 16) & 0xff] ^ Table2[(y2 >> 8) & 0xff] ^ Table3[y3 & 0xff] ^ rk[24 + 4];
            x1 = Table0[y1 >> 24] ^ Table1[(y2 >> 16) & 0xff] ^ Table2[(y3 >> 8) & 0xff] ^ Table3[y0 & 0xff] ^ rk[24 + 5];
            x2 = Table0[y2 >> 24] ^ Table1[(y3 >> 16) & 0xff] ^ Table2[(y0 >> 8) & 0xff] ^ Table3[y1 & 0xff] ^ rk[24 + 6];
            x3 = Table0[y3 >> 24] ^ Table1[(y0 >> 16) & 0xff] ^ Table2[(y1 >> 8) & 0xff] ^ Table3[y2 & 0xff] ^ rk[24 + 7];
            y0 = Table0[x0 >> 24] ^ Table1[(x1 >> 16) & 0xff] ^ Table2[(x2 >> 8) & 0xff] ^ Table3[x3 & 0xff] ^ rk[24 + 8];
            y1 = Table0[x1 >> 24] ^ Table1[(x2 >> 16) & 0xff] ^ Table2[(x3 >> 8) & 0xff] ^ Table3[x0 & 0xff] ^ rk[24 + 9];
            y2 = Table0[x2 >> 24] ^ Table1[(x3 >> 16) & 0xff] ^ Table2[(x0 >> 8) & 0xff] ^ Table3[x1 & 0xff] ^ rk[24 + 10];
            y3 = Table0[x3 >> 24] ^ Table1[(x0 >> 16) & 0xff] ^ Table2[(x1 >> 8) & 0xff] ^ Table3[x2 & 0xff] ^ rk[24 + 11];
            x0 = Table0[y0 >> 24] ^ Table1[(y1 >> 16) & 0xff] ^ Table2[(y2 >> 8) & 0xff] ^ Table3[y3 & 0xff];
            x1 = Table0[y1 >> 24] ^ Table1[(y2 >> 16) & 0xff] ^ Table2[(y3 >> 8) & 0xff] ^ Table3[y0 & 0xff];
            x2 = Table0[y2 >> 24] ^ Table1[(y3 >> 16) & 0xff] ^ Table2[(y0 >> 8) & 0xff] ^ Table3[y1 & 0xff];
            x3 = Table0[y3 >> 24] ^ Table1[(y0 >> 16) & 0xff] ^ Table2[(y1 >> 8) & 0xff] ^ Table3[y2 & 0xff];
            state0 ^= x0;
            state1 ^= x1;
            state2 ^= x2;
            state3 ^= x3;
            y0 = state0 ^ rk[24 + 12];
            y1 = state1 ^ rk[24 + 13];
            y2 = state2 ^ rk[24 + 14];
            y3 = state3 ^ rk[24 + 15];
            x0 = Table0[y0 >> 24] ^ Table1[(y1 >> 16) & 0xff] ^ Table2[(y2 >> 8) & 0xff] ^ Table3[y3 & 0xff] ^ rk[24 + 16];
            x1 = Table0[y1 >> 24] ^ Table1[(y2 >> 16) & 0xff] ^ Table2[(y3 >> 8) & 0xff] ^ Table3[y0 & 0xff] ^ rk[24 + 17];
            x2 = Table0[y2 >> 24] ^ Table1[(y3 >> 16) & 0xff] ^ Table2[(y0 >> 8) & 0xff] ^ Table3[y1 & 0xff] ^ rk[24 + 18];
            x3 = Table0[y3 >> 24] ^ Table1[(y0 >> 16) & 0xff] ^ Table2[(y1 >> 8) & 0xff] ^ Table3[y2 & 0xff] ^ rk[24 + 19];
            y0 = Table0[x0 >> 24] ^ Table1[(x1 >> 16) & 0xff] ^ Table2[(x2 >> 8) & 0xff] ^ Table3[x3 & 0xff] ^ rk[24 + 20];
            y1 = Table0[x1 >> 24] ^ Table1[(x2 >> 16) & 0xff] ^ Table2[(x3 >> 8) & 0xff] ^ Table3[x0 & 0xff] ^ rk[24 + 21];
            y2 = Table0[x2 >> 24] ^ Table1[(x3 >> 16) & 0xff] ^ Table2[(x0 >> 8) & 0xff] ^ Table3[x1 & 0xff] ^ rk[24 + 22];
            y3 = Table0[x3 >> 24] ^ Table1[(x0 >> 16) & 0xff] ^ Table2[(x1 >> 8) & 0xff] ^ Table3[x2 & 0xff] ^ rk[24 + 23];
            x0 = Table0[y0 >> 24] ^ Table1[(y1 >> 16) & 0xff] ^ Table2[(y2 >> 8) & 0xff] ^ Table3[y3 & 0xff];
            x1 = Table0[y1 >> 24] ^ Table1[(y2 >> 16) & 0xff] ^ Table2[(y3 >> 8) & 0xff] ^ Table3[y0 & 0xff];
            x2 = Table0[y2 >> 24] ^ Table1[(y3 >> 16) & 0xff] ^ Table2[(y0 >> 8) & 0xff] ^ Table3[y1 & 0xff];
            x3 = Table0[y3 >> 24] ^ Table1[(y0 >> 16) & 0xff] ^ Table2[(y1 >> 8) & 0xff] ^ Table3[y2 & 0xff];
            state4 ^= x0;
            state5 ^= x1;
            state6 ^= x2;
            state7 ^= x3;

            x0 = rk[33];
            x1 = rk[34];
            x2 = rk[35];
            x3 = rk[32];
            y0 = Table0[x0 >> 24] ^ Table1[(x1 >> 16) & 0xff] ^ Table2[(x2 >> 8) & 0xff] ^ Table3[x3 & 0xff];
            y1 = Table0[x1 >> 24] ^ Table1[(x2 >> 16) & 0xff] ^ Table2[(x3 >> 8) & 0xff] ^ Table3[x0 & 0xff];
            y2 = Table0[x2 >> 24] ^ Table1[(x3 >> 16) & 0xff] ^ Table2[(x0 >> 8) & 0xff] ^ Table3[x1 & 0xff];
            y3 = Table0[x3 >> 24] ^ Table1[(x0 >> 16) & 0xff] ^ Table2[(x1 >> 8) & 0xff] ^ Table3[x2 & 0xff];
            rk[48] = y0 ^ rk[44];
            rk[49] = y1 ^ rk[45];
            rk[50] = y2 ^ rk[46];
            rk[51] = y3 ^ rk[47];
            x0 = rk[37];
            x1 = rk[38];
            x2 = rk[39];
            x3 = rk[36];
            y0 = Table0[x0 >> 24] ^ Table1[(x1 >> 16) & 0xff] ^ Table2[(x2 >> 8) & 0xff] ^ Table3[x3 & 0xff];
            y1 = Table0[x1 >> 24] ^ Table1[(x2 >> 16) & 0xff] ^ Table2[(x3 >> 8) & 0xff] ^ Table3[x0 & 0xff];
            y2 = Table0[x2 >> 24] ^ Table1[(x3 >> 16) & 0xff] ^ Table2[(x0 >> 8) & 0xff] ^ Table3[x1 & 0xff];
            y3 = Table0[x3 >> 24] ^ Table1[(x0 >> 16) & 0xff] ^ Table2[(x1 >> 8) & 0xff] ^ Table3[x2 & 0xff];
            rk[52] = y0 ^ rk[48];
            rk[53] = y1 ^ rk[49];
            rk[54] = y2 ^ rk[50];
            rk[55] = y3 ^ rk[51];
            x0 = rk[41];
            x1 = rk[42];
            x2 = rk[43];
            x3 = rk[40];
            y0 = Table0[x0 >> 24] ^ Table1[(x1 >> 16) & 0xff] ^ Table2[(x2 >> 8) & 0xff] ^ Table3[x3 & 0xff];
            y1 = Table0[x1 >> 24] ^ Table1[(x2 >> 16) & 0xff] ^ Table2[(x3 >> 8) & 0xff] ^ Table3[x0 & 0xff];
            y2 = Table0[x2 >> 24] ^ Table1[(x3 >> 16) & 0xff] ^ Table2[(x0 >> 8) & 0xff] ^ Table3[x1 & 0xff];
            y3 = Table0[x3 >> 24] ^ Table1[(x0 >> 16) & 0xff] ^ Table2[(x1 >> 8) & 0xff] ^ Table3[x2 & 0xff];
            rk[56] = y0 ^ rk[52];
            rk[57] = y1 ^ rk[53] ^ cnt1;
            rk[58] = y2 ^ rk[54] ^ ~cnt0;
            rk[59] = y3 ^ rk[55];
            x0 = rk[45];
            x1 = rk[46];
            x2 = rk[47];
            x3 = rk[44];
            y0 = Table0[x0 >> 24] ^ Table1[(x1 >> 16) & 0xff] ^ Table2[(x2 >> 8) & 0xff] ^ Table3[x3 & 0xff];
            y1 = Table0[x1 >> 24] ^ Table1[(x2 >> 16) & 0xff] ^ Table2[(x3 >> 8) & 0xff] ^ Table3[x0 & 0xff];
            y2 = Table0[x2 >> 24] ^ Table1[(x3 >> 16) & 0xff] ^ Table2[(x0 >> 8) & 0xff] ^ Table3[x1 & 0xff];
            y3 = Table0[x3 >> 24] ^ Table1[(x0 >> 16) & 0xff] ^ Table2[(x1 >> 8) & 0xff] ^ Table3[x2 & 0xff];
            rk[60] = y0 ^ rk[56];
            rk[61] = y1 ^ rk[57];
            rk[62] = y2 ^ rk[58];
            rk[63] = y3 ^ rk[59];

            for (int i = 0; i < 16; i++)
                rk[64 + i] = rk[i + 64 - 16] ^ rk[i + 64 - 3];

            y0 = state4 ^ rk[48];
            y1 = state5 ^ rk[48 + 1];
            y2 = state6 ^ rk[48 + 2];
            y3 = state7 ^ rk[48 + 3];
            x0 = Table0[y0 >> 24] ^ Table1[(y1 >> 16) & 0xff] ^ Table2[(y2 >> 8) & 0xff] ^ Table3[y3 & 0xff] ^ rk[48 + 4];
            x1 = Table0[y1 >> 24] ^ Table1[(y2 >> 16) & 0xff] ^ Table2[(y3 >> 8) & 0xff] ^ Table3[y0 & 0xff] ^ rk[48 + 5];
            x2 = Table0[y2 >> 24] ^ Table1[(y3 >> 16) & 0xff] ^ Table2[(y0 >> 8) & 0xff] ^ Table3[y1 & 0xff] ^ rk[48 + 6];
            x3 = Table0[y3 >> 24] ^ Table1[(y0 >> 16) & 0xff] ^ Table2[(y1 >> 8) & 0xff] ^ Table3[y2 & 0xff] ^ rk[48 + 7];
            y0 = Table0[x0 >> 24] ^ Table1[(x1 >> 16) & 0xff] ^ Table2[(x2 >> 8) & 0xff] ^ Table3[x3 & 0xff] ^ rk[48 + 8];
            y1 = Table0[x1 >> 24] ^ Table1[(x2 >> 16) & 0xff] ^ Table2[(x3 >> 8) & 0xff] ^ Table3[x0 & 0xff] ^ rk[48 + 9];
            y2 = Table0[x2 >> 24] ^ Table1[(x3 >> 16) & 0xff] ^ Table2[(x0 >> 8) & 0xff] ^ Table3[x1 & 0xff] ^ rk[48 + 10];
            y3 = Table0[x3 >> 24] ^ Table1[(x0 >> 16) & 0xff] ^ Table2[(x1 >> 8) & 0xff] ^ Table3[x2 & 0xff] ^ rk[48 + 11];
            x0 = Table0[y0 >> 24] ^ Table1[(y1 >> 16) & 0xff] ^ Table2[(y2 >> 8) & 0xff] ^ Table3[y3 & 0xff];
            x1 = Table0[y1 >> 24] ^ Table1[(y2 >> 16) & 0xff] ^ Table2[(y3 >> 8) & 0xff] ^ Table3[y0 & 0xff];
            x2 = Table0[y2 >> 24] ^ Table1[(y3 >> 16) & 0xff] ^ Table2[(y0 >> 8) & 0xff] ^ Table3[y1 & 0xff];
            x3 = Table0[y3 >> 24] ^ Table1[(y0 >> 16) & 0xff] ^ Table2[(y1 >> 8) & 0xff] ^ Table3[y2 & 0xff];
            state0 ^= x0;
            state1 ^= x1;
            state2 ^= x2;
            state3 ^= x3;
            y0 = state0 ^ rk[48 + 12];
            y1 = state1 ^ rk[48 + 13];
            y2 = state2 ^ rk[48 + 14];
            y3 = state3 ^ rk[48 + 15];
            x0 = Table0[y0 >> 24] ^ Table1[(y1 >> 16) & 0xff] ^ Table2[(y2 >> 8) & 0xff] ^ Table3[y3 & 0xff] ^ rk[48 + 16];
            x1 = Table0[y1 >> 24] ^ Table1[(y2 >> 16) & 0xff] ^ Table2[(y3 >> 8) & 0xff] ^ Table3[y0 & 0xff] ^ rk[48 + 17];
            x2 = Table0[y2 >> 24] ^ Table1[(y3 >> 16) & 0xff] ^ Table2[(y0 >> 8) & 0xff] ^ Table3[y1 & 0xff] ^ rk[48 + 18];
            x3 = Table0[y3 >> 24] ^ Table1[(y0 >> 16) & 0xff] ^ Table2[(y1 >> 8) & 0xff] ^ Table3[y2 & 0xff] ^ rk[48 + 19];
            y0 = Table0[x0 >> 24] ^ Table1[(x1 >> 16) & 0xff] ^ Table2[(x2 >> 8) & 0xff] ^ Table3[x3 & 0xff] ^ rk[48 + 20];
            y1 = Table0[x1 >> 24] ^ Table1[(x2 >> 16) & 0xff] ^ Table2[(x3 >> 8) & 0xff] ^ Table3[x0 & 0xff] ^ rk[48 + 21];
            y2 = Table0[x2 >> 24] ^ Table1[(x3 >> 16) & 0xff] ^ Table2[(x0 >> 8) & 0xff] ^ Table3[x1 & 0xff] ^ rk[48 + 22];
            y3 = Table0[x3 >> 24] ^ Table1[(x0 >> 16) & 0xff] ^ Table2[(x1 >> 8) & 0xff] ^ Table3[x2 & 0xff] ^ rk[48 + 23];
            x0 = Table0[y0 >> 24] ^ Table1[(y1 >> 16) & 0xff] ^ Table2[(y2 >> 8) & 0xff] ^ Table3[y3 & 0xff];
            x1 = Table0[y1 >> 24] ^ Table1[(y2 >> 16) & 0xff] ^ Table2[(y3 >> 8) & 0xff] ^ Table3[y0 & 0xff];
            x2 = Table0[y2 >> 24] ^ Table1[(y3 >> 16) & 0xff] ^ Table2[(y0 >> 8) & 0xff] ^ Table3[y1 & 0xff];
            x3 = Table0[y3 >> 24] ^ Table1[(y0 >> 16) & 0xff] ^ Table2[(y1 >> 8) & 0xff] ^ Table3[y2 & 0xff];
            state4 ^= x0;
            state5 ^= x1;
            state6 ^= x2;
            state7 ^= x3;

            x0 = rk[65];
            x1 = rk[66];
            x2 = rk[67];
            x3 = rk[64];
            y0 = Table0[x0 >> 24] ^ Table1[(x1 >> 16) & 0xff] ^ Table2[(x2 >> 8) & 0xff] ^ Table3[x3 & 0xff];
            y1 = Table0[x1 >> 24] ^ Table1[(x2 >> 16) & 0xff] ^ Table2[(x3 >> 8) & 0xff] ^ Table3[x0 & 0xff];
            y2 = Table0[x2 >> 24] ^ Table1[(x3 >> 16) & 0xff] ^ Table2[(x0 >> 8) & 0xff] ^ Table3[x1 & 0xff];
            y3 = Table0[x3 >> 24] ^ Table1[(x0 >> 16) & 0xff] ^ Table2[(x1 >> 8) & 0xff] ^ Table3[x2 & 0xff];
            rk[80] = y0 ^ rk[76];
            rk[81] = y1 ^ rk[77];
            rk[82] = y2 ^ rk[78];
            rk[83] = y3 ^ rk[79];
            x0 = rk[69];
            x1 = rk[70];
            x2 = rk[71];
            x3 = rk[68];
            y0 = Table0[x0 >> 24] ^ Table1[(x1 >> 16) & 0xff] ^ Table2[(x2 >> 8) & 0xff] ^ Table3[x3 & 0xff];
            y1 = Table0[x1 >> 24] ^ Table1[(x2 >> 16) & 0xff] ^ Table2[(x3 >> 8) & 0xff] ^ Table3[x0 & 0xff];
            y2 = Table0[x2 >> 24] ^ Table1[(x3 >> 16) & 0xff] ^ Table2[(x0 >> 8) & 0xff] ^ Table3[x1 & 0xff];
            y3 = Table0[x3 >> 24] ^ Table1[(x0 >> 16) & 0xff] ^ Table2[(x1 >> 8) & 0xff] ^ Table3[x2 & 0xff];
            rk[84] = y0 ^ rk[80];
            rk[85] = y1 ^ rk[81];
            rk[86] = y2 ^ rk[82] ^ cnt1;
            rk[87] = y3 ^ rk[83] ^ ~cnt0;
            x0 = rk[73];
            x1 = rk[74];
            x2 = rk[75];
            x3 = rk[72];
            y0 = Table0[x0 >> 24] ^ Table1[(x1 >> 16) & 0xff] ^ Table2[(x2 >> 8) & 0xff] ^ Table3[x3 & 0xff];
            y1 = Table0[x1 >> 24] ^ Table1[(x2 >> 16) & 0xff] ^ Table2[(x3 >> 8) & 0xff] ^ Table3[x0 & 0xff];
            y2 = Table0[x2 >> 24] ^ Table1[(x3 >> 16) & 0xff] ^ Table2[(x0 >> 8) & 0xff] ^ Table3[x1 & 0xff];
            y3 = Table0[x3 >> 24] ^ Table1[(x0 >> 16) & 0xff] ^ Table2[(x1 >> 8) & 0xff] ^ Table3[x2 & 0xff];
            rk[88] = y0 ^ rk[84];
            rk[89] = y1 ^ rk[85];
            rk[90] = y2 ^ rk[86];
            rk[91] = y3 ^ rk[87];
            x0 = rk[77];
            x1 = rk[78];
            x2 = rk[79];
            x3 = rk[76];
            y0 = Table0[x0 >> 24] ^ Table1[(x1 >> 16) & 0xff] ^ Table2[(x2 >> 8) & 0xff] ^ Table3[x3 & 0xff];
            y1 = Table0[x1 >> 24] ^ Table1[(x2 >> 16) & 0xff] ^ Table2[(x3 >> 8) & 0xff] ^ Table3[x0 & 0xff];
            y2 = Table0[x2 >> 24] ^ Table1[(x3 >> 16) & 0xff] ^ Table2[(x0 >> 8) & 0xff] ^ Table3[x1 & 0xff];
            y3 = Table0[x3 >> 24] ^ Table1[(x0 >> 16) & 0xff] ^ Table2[(x1 >> 8) & 0xff] ^ Table3[x2 & 0xff];
            rk[92] = y0 ^ rk[88];
            rk[93] = y1 ^ rk[89];
            rk[94] = y2 ^ rk[90];
            rk[95] = y3 ^ rk[91];

            y0 = state4 ^ rk[72];
            y1 = state5 ^ rk[72 + 1];
            y2 = state6 ^ rk[72 + 2];
            y3 = state7 ^ rk[72 + 3];
            x0 = Table0[y0 >> 24] ^ Table1[(y1 >> 16) & 0xff] ^ Table2[(y2 >> 8) & 0xff] ^ Table3[y3 & 0xff] ^ rk[72 + 4];
            x1 = Table0[y1 >> 24] ^ Table1[(y2 >> 16) & 0xff] ^ Table2[(y3 >> 8) & 0xff] ^ Table3[y0 & 0xff] ^ rk[72 + 5];
            x2 = Table0[y2 >> 24] ^ Table1[(y3 >> 16) & 0xff] ^ Table2[(y0 >> 8) & 0xff] ^ Table3[y1 & 0xff] ^ rk[72 + 6];
            x3 = Table0[y3 >> 24] ^ Table1[(y0 >> 16) & 0xff] ^ Table2[(y1 >> 8) & 0xff] ^ Table3[y2 & 0xff] ^ rk[72 + 7];
            y0 = Table0[x0 >> 24] ^ Table1[(x1 >> 16) & 0xff] ^ Table2[(x2 >> 8) & 0xff] ^ Table3[x3 & 0xff] ^ rk[72 + 8];
            y1 = Table0[x1 >> 24] ^ Table1[(x2 >> 16) & 0xff] ^ Table2[(x3 >> 8) & 0xff] ^ Table3[x0 & 0xff] ^ rk[72 + 9];
            y2 = Table0[x2 >> 24] ^ Table1[(x3 >> 16) & 0xff] ^ Table2[(x0 >> 8) & 0xff] ^ Table3[x1 & 0xff] ^ rk[72 + 10];
            y3 = Table0[x3 >> 24] ^ Table1[(x0 >> 16) & 0xff] ^ Table2[(x1 >> 8) & 0xff] ^ Table3[x2 & 0xff] ^ rk[72 + 11];
            x0 = Table0[y0 >> 24] ^ Table1[(y1 >> 16) & 0xff] ^ Table2[(y2 >> 8) & 0xff] ^ Table3[y3 & 0xff];
            x1 = Table0[y1 >> 24] ^ Table1[(y2 >> 16) & 0xff] ^ Table2[(y3 >> 8) & 0xff] ^ Table3[y0 & 0xff];
            x2 = Table0[y2 >> 24] ^ Table1[(y3 >> 16) & 0xff] ^ Table2[(y0 >> 8) & 0xff] ^ Table3[y1 & 0xff];
            x3 = Table0[y3 >> 24] ^ Table1[(y0 >> 16) & 0xff] ^ Table2[(y1 >> 8) & 0xff] ^ Table3[y2 & 0xff];
            state0 ^= x0;
            state1 ^= x1;
            state2 ^= x2;
            state3 ^= x3;
            y0 = state0 ^ rk[72 + 12];
            y1 = state1 ^ rk[72 + 13];
            y2 = state2 ^ rk[72 + 14];
            y3 = state3 ^ rk[72 + 15];
            x0 = Table0[y0 >> 24] ^ Table1[(y1 >> 16) & 0xff] ^ Table2[(y2 >> 8) & 0xff] ^ Table3[y3 & 0xff] ^ rk[72 + 16];
            x1 = Table0[y1 >> 24] ^ Table1[(y2 >> 16) & 0xff] ^ Table2[(y3 >> 8) & 0xff] ^ Table3[y0 & 0xff] ^ rk[72 + 17];
            x2 = Table0[y2 >> 24] ^ Table1[(y3 >> 16) & 0xff] ^ Table2[(y0 >> 8) & 0xff] ^ Table3[y1 & 0xff] ^ rk[72 + 18];
            x3 = Table0[y3 >> 24] ^ Table1[(y0 >> 16) & 0xff] ^ Table2[(y1 >> 8) & 0xff] ^ Table3[y2 & 0xff] ^ rk[72 + 19];
            y0 = Table0[x0 >> 24] ^ Table1[(x1 >> 16) & 0xff] ^ Table2[(x2 >> 8) & 0xff] ^ Table3[x3 & 0xff] ^ rk[72 + 20];
            y1 = Table0[x1 >> 24] ^ Table1[(x2 >> 16) & 0xff] ^ Table2[(x3 >> 8) & 0xff] ^ Table3[x0 & 0xff] ^ rk[72 + 21];
            y2 = Table0[x2 >> 24] ^ Table1[(x3 >> 16) & 0xff] ^ Table2[(x0 >> 8) & 0xff] ^ Table3[x1 & 0xff] ^ rk[72 + 22];
            y3 = Table0[x3 >> 24] ^ Table1[(x0 >> 16) & 0xff] ^ Table2[(x1 >> 8) & 0xff] ^ Table3[x2 & 0xff] ^ rk[72 + 23];
            x0 = Table0[y0 >> 24] ^ Table1[(y1 >> 16) & 0xff] ^ Table2[(y2 >> 8) & 0xff] ^ Table3[y3 & 0xff];
            x1 = Table0[y1 >> 24] ^ Table1[(y2 >> 16) & 0xff] ^ Table2[(y3 >> 8) & 0xff] ^ Table3[y0 & 0xff];
            x2 = Table0[y2 >> 24] ^ Table1[(y3 >> 16) & 0xff] ^ Table2[(y0 >> 8) & 0xff] ^ Table3[y1 & 0xff];
            x3 = Table0[y3 >> 24] ^ Table1[(y0 >> 16) & 0xff] ^ Table2[(y1 >> 8) & 0xff] ^ Table3[y2 & 0xff];
            state4 ^= x0;
            state5 ^= x1;
            state6 ^= x2;
            state7 ^= x3;

            for (int i = 0; i < 16; i++)
                rk[96 + i] = rk[i + 96 - 16] ^ rk[i + 96 - 3];

            x0 = rk[97];
            x1 = rk[98];
            x2 = rk[99];
            x3 = rk[96];
            y0 = Table0[x0 >> 24] ^ Table1[(x1 >> 16) & 0xff] ^ Table2[(x2 >> 8) & 0xff] ^ Table3[x3 & 0xff];
            y1 = Table0[x1 >> 24] ^ Table1[(x2 >> 16) & 0xff] ^ Table2[(x3 >> 8) & 0xff] ^ Table3[x0 & 0xff];
            y2 = Table0[x2 >> 24] ^ Table1[(x3 >> 16) & 0xff] ^ Table2[(x0 >> 8) & 0xff] ^ Table3[x1 & 0xff];
            y3 = Table0[x3 >> 24] ^ Table1[(x0 >> 16) & 0xff] ^ Table2[(x1 >> 8) & 0xff] ^ Table3[x2 & 0xff];
            rk[112] = y0 ^ rk[108];
            rk[113] = y1 ^ rk[109];
            rk[114] = y2 ^ rk[110];
            rk[115] = y3 ^ rk[111];
            x0 = rk[101];
            x1 = rk[102];
            x2 = rk[103];
            x3 = rk[100];
            y0 = Table0[x0 >> 24] ^ Table1[(x1 >> 16) & 0xff] ^ Table2[(x2 >> 8) & 0xff] ^ Table3[x3 & 0xff];
            y1 = Table0[x1 >> 24] ^ Table1[(x2 >> 16) & 0xff] ^ Table2[(x3 >> 8) & 0xff] ^ Table3[x0 & 0xff];
            y2 = Table0[x2 >> 24] ^ Table1[(x3 >> 16) & 0xff] ^ Table2[(x0 >> 8) & 0xff] ^ Table3[x1 & 0xff];
            y3 = Table0[x3 >> 24] ^ Table1[(x0 >> 16) & 0xff] ^ Table2[(x1 >> 8) & 0xff] ^ Table3[x2 & 0xff];
            rk[116] = y0 ^ rk[112];
            rk[117] = y1 ^ rk[113];
            rk[118] = y2 ^ rk[114];
            rk[119] = y3 ^ rk[115];
            x0 = rk[105];
            x1 = rk[106];
            x2 = rk[107];
            x3 = rk[104];
            y0 = Table0[x0 >> 24] ^ Table1[(x1 >> 16) & 0xff] ^ Table2[(x2 >> 8) & 0xff] ^ Table3[x3 & 0xff];
            y1 = Table0[x1 >> 24] ^ Table1[(x2 >> 16) & 0xff] ^ Table2[(x3 >> 8) & 0xff] ^ Table3[x0 & 0xff];
            y2 = Table0[x2 >> 24] ^ Table1[(x3 >> 16) & 0xff] ^ Table2[(x0 >> 8) & 0xff] ^ Table3[x1 & 0xff];
            y3 = Table0[x3 >> 24] ^ Table1[(x0 >> 16) & 0xff] ^ Table2[(x1 >> 8) & 0xff] ^ Table3[x2 & 0xff];
            rk[120] = y0 ^ rk[116];
            rk[121] = y1 ^ rk[117];
            rk[122] = y2 ^ rk[118];
            rk[123] = y3 ^ rk[119];
            x0 = rk[109];
            x1 = rk[110];
            x2 = rk[111];
            x3 = rk[108];
            y0 = Table0[x0 >> 24] ^ Table1[(x1 >> 16) & 0xff] ^ Table2[(x2 >> 8) & 0xff] ^ Table3[x3 & 0xff];
            y1 = Table0[x1 >> 24] ^ Table1[(x2 >> 16) & 0xff] ^ Table2[(x3 >> 8) & 0xff] ^ Table3[x0 & 0xff];
            y2 = Table0[x2 >> 24] ^ Table1[(x3 >> 16) & 0xff] ^ Table2[(x0 >> 8) & 0xff] ^ Table3[x1 & 0xff];
            y3 = Table0[x3 >> 24] ^ Table1[(x0 >> 16) & 0xff] ^ Table2[(x1 >> 8) & 0xff] ^ Table3[x2 & 0xff];
            rk[124] = y0 ^ rk[120] ^ cnt0;
            rk[125] = y1 ^ rk[121];
            rk[126] = y2 ^ rk[122];
            rk[127] = y3 ^ rk[123] ^ ~cnt1;

            y0 = state4 ^ rk[96];
            y1 = state5 ^ rk[96 + 1];
            y2 = state6 ^ rk[96 + 2];
            y3 = state7 ^ rk[96 + 3];
            x0 = Table0[y0 >> 24] ^ Table1[(y1 >> 16) & 0xff] ^ Table2[(y2 >> 8) & 0xff] ^ Table3[y3 & 0xff] ^ rk[96 + 4];
            x1 = Table0[y1 >> 24] ^ Table1[(y2 >> 16) & 0xff] ^ Table2[(y3 >> 8) & 0xff] ^ Table3[y0 & 0xff] ^ rk[96 + 5];
            x2 = Table0[y2 >> 24] ^ Table1[(y3 >> 16) & 0xff] ^ Table2[(y0 >> 8) & 0xff] ^ Table3[y1 & 0xff] ^ rk[96 + 6];
            x3 = Table0[y3 >> 24] ^ Table1[(y0 >> 16) & 0xff] ^ Table2[(y1 >> 8) & 0xff] ^ Table3[y2 & 0xff] ^ rk[96 + 7];
            y0 = Table0[x0 >> 24] ^ Table1[(x1 >> 16) & 0xff] ^ Table2[(x2 >> 8) & 0xff] ^ Table3[x3 & 0xff] ^ rk[96 + 8];
            y1 = Table0[x1 >> 24] ^ Table1[(x2 >> 16) & 0xff] ^ Table2[(x3 >> 8) & 0xff] ^ Table3[x0 & 0xff] ^ rk[96 + 9];
            y2 = Table0[x2 >> 24] ^ Table1[(x3 >> 16) & 0xff] ^ Table2[(x0 >> 8) & 0xff] ^ Table3[x1 & 0xff] ^ rk[96 + 10];
            y3 = Table0[x3 >> 24] ^ Table1[(x0 >> 16) & 0xff] ^ Table2[(x1 >> 8) & 0xff] ^ Table3[x2 & 0xff] ^ rk[96 + 11];
            x0 = Table0[y0 >> 24] ^ Table1[(y1 >> 16) & 0xff] ^ Table2[(y2 >> 8) & 0xff] ^ Table3[y3 & 0xff];
            x1 = Table0[y1 >> 24] ^ Table1[(y2 >> 16) & 0xff] ^ Table2[(y3 >> 8) & 0xff] ^ Table3[y0 & 0xff];
            x2 = Table0[y2 >> 24] ^ Table1[(y3 >> 16) & 0xff] ^ Table2[(y0 >> 8) & 0xff] ^ Table3[y1 & 0xff];
            x3 = Table0[y3 >> 24] ^ Table1[(y0 >> 16) & 0xff] ^ Table2[(y1 >> 8) & 0xff] ^ Table3[y2 & 0xff];
            state0 ^= x0;
            state1 ^= x1;
            state2 ^= x2;
            state3 ^= x3;
            y0 = state0 ^ rk[96 + 12];
            y1 = state1 ^ rk[96 + 13];
            y2 = state2 ^ rk[96 + 14];
            y3 = state3 ^ rk[96 + 15];
            x0 = Table0[y0 >> 24] ^ Table1[(y1 >> 16) & 0xff] ^ Table2[(y2 >> 8) & 0xff] ^ Table3[y3 & 0xff] ^ rk[96 + 16];
            x1 = Table0[y1 >> 24] ^ Table1[(y2 >> 16) & 0xff] ^ Table2[(y3 >> 8) & 0xff] ^ Table3[y0 & 0xff] ^ rk[96 + 17];
            x2 = Table0[y2 >> 24] ^ Table1[(y3 >> 16) & 0xff] ^ Table2[(y0 >> 8) & 0xff] ^ Table3[y1 & 0xff] ^ rk[96 + 18];
            x3 = Table0[y3 >> 24] ^ Table1[(y0 >> 16) & 0xff] ^ Table2[(y1 >> 8) & 0xff] ^ Table3[y2 & 0xff] ^ rk[96 + 19];
            y0 = Table0[x0 >> 24] ^ Table1[(x1 >> 16) & 0xff] ^ Table2[(x2 >> 8) & 0xff] ^ Table3[x3 & 0xff] ^ rk[96 + 20];
            y1 = Table0[x1 >> 24] ^ Table1[(x2 >> 16) & 0xff] ^ Table2[(x3 >> 8) & 0xff] ^ Table3[x0 & 0xff] ^ rk[96 + 21];
            y2 = Table0[x2 >> 24] ^ Table1[(x3 >> 16) & 0xff] ^ Table2[(x0 >> 8) & 0xff] ^ Table3[x1 & 0xff] ^ rk[96 + 22];
            y3 = Table0[x3 >> 24] ^ Table1[(x0 >> 16) & 0xff] ^ Table2[(x1 >> 8) & 0xff] ^ Table3[x2 & 0xff] ^ rk[96 + 23];
            x0 = Table0[y0 >> 24] ^ Table1[(y1 >> 16) & 0xff] ^ Table2[(y2 >> 8) & 0xff] ^ Table3[y3 & 0xff];
            x1 = Table0[y1 >> 24] ^ Table1[(y2 >> 16) & 0xff] ^ Table2[(y3 >> 8) & 0xff] ^ Table3[y0 & 0xff];
            x2 = Table0[y2 >> 24] ^ Table1[(y3 >> 16) & 0xff] ^ Table2[(y0 >> 8) & 0xff] ^ Table3[y1 & 0xff];
            x3 = Table0[y3 >> 24] ^ Table1[(y0 >> 16) & 0xff] ^ Table2[(y1 >> 8) & 0xff] ^ Table3[y2 & 0xff];
            state4 ^= x0;
            state5 ^= x1;
            state6 ^= x2;
            state7 ^= x3;

            for (int i = 0; i < 16; i++)
                rk[128 + i] = rk[i + 128 - 16] ^ rk[i + 128 - 3];

            y0 = state4 ^ rk[120];
            y1 = state5 ^ rk[120 + 1];
            y2 = state6 ^ rk[120 + 2];
            y3 = state7 ^ rk[120 + 3];
            x0 = Table0[y0 >> 24] ^ Table1[(y1 >> 16) & 0xff] ^ Table2[(y2 >> 8) & 0xff] ^ Table3[y3 & 0xff] ^ rk[120 + 4];
            x1 = Table0[y1 >> 24] ^ Table1[(y2 >> 16) & 0xff] ^ Table2[(y3 >> 8) & 0xff] ^ Table3[y0 & 0xff] ^ rk[120 + 5];
            x2 = Table0[y2 >> 24] ^ Table1[(y3 >> 16) & 0xff] ^ Table2[(y0 >> 8) & 0xff] ^ Table3[y1 & 0xff] ^ rk[120 + 6];
            x3 = Table0[y3 >> 24] ^ Table1[(y0 >> 16) & 0xff] ^ Table2[(y1 >> 8) & 0xff] ^ Table3[y2 & 0xff] ^ rk[120 + 7];
            y0 = Table0[x0 >> 24] ^ Table1[(x1 >> 16) & 0xff] ^ Table2[(x2 >> 8) & 0xff] ^ Table3[x3 & 0xff] ^ rk[120 + 8];
            y1 = Table0[x1 >> 24] ^ Table1[(x2 >> 16) & 0xff] ^ Table2[(x3 >> 8) & 0xff] ^ Table3[x0 & 0xff] ^ rk[120 + 9];
            y2 = Table0[x2 >> 24] ^ Table1[(x3 >> 16) & 0xff] ^ Table2[(x0 >> 8) & 0xff] ^ Table3[x1 & 0xff] ^ rk[120 + 10];
            y3 = Table0[x3 >> 24] ^ Table1[(x0 >> 16) & 0xff] ^ Table2[(x1 >> 8) & 0xff] ^ Table3[x2 & 0xff] ^ rk[120 + 11];
            x0 = Table0[y0 >> 24] ^ Table1[(y1 >> 16) & 0xff] ^ Table2[(y2 >> 8) & 0xff] ^ Table3[y3 & 0xff];
            x1 = Table0[y1 >> 24] ^ Table1[(y2 >> 16) & 0xff] ^ Table2[(y3 >> 8) & 0xff] ^ Table3[y0 & 0xff];
            x2 = Table0[y2 >> 24] ^ Table1[(y3 >> 16) & 0xff] ^ Table2[(y0 >> 8) & 0xff] ^ Table3[y1 & 0xff];
            x3 = Table0[y3 >> 24] ^ Table1[(y0 >> 16) & 0xff] ^ Table2[(y1 >> 8) & 0xff] ^ Table3[y2 & 0xff];
            state0 ^= x0;
            state1 ^= x1;
            state2 ^= x2;
            state3 ^= x3;
            y0 = state0 ^ rk[120 + 12];
            y1 = state1 ^ rk[120 + 13];
            y2 = state2 ^ rk[120 + 14];
            y3 = state3 ^ rk[120 + 15];
            x0 = Table0[y0 >> 24] ^ Table1[(y1 >> 16) & 0xff] ^ Table2[(y2 >> 8) & 0xff] ^ Table3[y3 & 0xff] ^ rk[120 + 16];
            x1 = Table0[y1 >> 24] ^ Table1[(y2 >> 16) & 0xff] ^ Table2[(y3 >> 8) & 0xff] ^ Table3[y0 & 0xff] ^ rk[120 + 17];
            x2 = Table0[y2 >> 24] ^ Table1[(y3 >> 16) & 0xff] ^ Table2[(y0 >> 8) & 0xff] ^ Table3[y1 & 0xff] ^ rk[120 + 18];
            x3 = Table0[y3 >> 24] ^ Table1[(y0 >> 16) & 0xff] ^ Table2[(y1 >> 8) & 0xff] ^ Table3[y2 & 0xff] ^ rk[120 + 19];
            y0 = Table0[x0 >> 24] ^ Table1[(x1 >> 16) & 0xff] ^ Table2[(x2 >> 8) & 0xff] ^ Table3[x3 & 0xff] ^ rk[120 + 20];
            y1 = Table0[x1 >> 24] ^ Table1[(x2 >> 16) & 0xff] ^ Table2[(x3 >> 8) & 0xff] ^ Table3[x0 & 0xff] ^ rk[120 + 21];
            y2 = Table0[x2 >> 24] ^ Table1[(x3 >> 16) & 0xff] ^ Table2[(x0 >> 8) & 0xff] ^ Table3[x1 & 0xff] ^ rk[120 + 22];
            y3 = Table0[x3 >> 24] ^ Table1[(x0 >> 16) & 0xff] ^ Table2[(x1 >> 8) & 0xff] ^ Table3[x2 & 0xff] ^ rk[120 + 23];
            x0 = Table0[y0 >> 24] ^ Table1[(y1 >> 16) & 0xff] ^ Table2[(y2 >> 8) & 0xff] ^ Table3[y3 & 0xff];
            x1 = Table0[y1 >> 24] ^ Table1[(y2 >> 16) & 0xff] ^ Table2[(y3 >> 8) & 0xff] ^ Table3[y0 & 0xff];
            x2 = Table0[y2 >> 24] ^ Table1[(y3 >> 16) & 0xff] ^ Table2[(y0 >> 8) & 0xff] ^ Table3[y1 & 0xff];
            x3 = Table0[y3 >> 24] ^ Table1[(y0 >> 16) & 0xff] ^ Table2[(y1 >> 8) & 0xff] ^ Table3[y2 & 0xff];
            state4 ^= x0;
            state5 ^= x1;
            state6 ^= x2;
            state7 ^= x3;

            m_state[0] ^= state0;
            m_state[1] ^= state1;
            m_state[2] ^= state2;
            m_state[3] ^= state3;
            m_state[4] ^= state4;
            m_state[5] ^= state5;
            m_state[6] ^= state6;
            m_state[7] ^= state7;
        }

        protected override void Finish()
        {
            byte[] pad = new byte[64];

            int buf_pos = m_buffer.Pos;

            Array.Copy(m_buffer.GetBytesZeroPadded(), 0, pad, 0, buf_pos);

            pad[buf_pos] = 0x80;

            if (buf_pos >= BlockSize - 10)
            {
                TransformBlock(pad, 0);

                pad.Clear();

                Converters.ConvertULongToBytes(m_processed_bytes * 8, pad, BlockSize - 10);

                int hashsizebits = HashSize * 8;
                pad[BlockSize - 2] = (byte)hashsizebits;
                pad[BlockSize - 1] = (byte)(hashsizebits >> 8);

                m_processed_bytes = 0;
                TransformBlock(pad, 0);
            }
            else
            {
                ulong bits = m_processed_bytes * 8;
                int padindex = BlockSize - 10;

                pad[padindex++] = (byte)bits;
                pad[padindex++] = (byte)(bits >> 8);
                pad[padindex++] = (byte)(bits >> 16);
                pad[padindex++] = (byte)(bits >> 24);
                pad[padindex++] = (byte)(bits >> 32);
                pad[padindex++] = (byte)(bits >> 40);
                pad[padindex++] = (byte)(bits >> 48);
                pad[padindex++] = (byte)(bits >> 56);

                int hashsizebits = HashSize * 8;
                pad[padindex++] = (byte)hashsizebits;
                pad[padindex] = (byte)(hashsizebits >> 8);

                if (buf_pos == 0)
                {
                    m_processed_bytes = 0;
                    TransformBlock(pad, 0);
                }
                else
                    TransformBlock(pad, 0);
            }
        }

        public SHAvite3_256Base(HashLib.HashSize a_hash_size)
            : base(a_hash_size, 64)
        {
            Initialize();
        }

        public override void Initialize()
        {
            if (HashSize == 28)
                Array.Copy(IV_224, 0, m_state, 0, 8);
            else
                Array.Copy(IV_256, 0, m_state, 0, 8);

            base.Initialize();
        }
    };

    internal abstract class SHAvite3_512Base : SHAvite3Base
    {
        #region Consts

        private static readonly uint[] IV_384 =
        {
            0x71F48510, 0xA903A8AC, 0xFE3216DD, 0x0B2D2AD4,
            0x6672900A, 0x41032819, 0x15A7D780, 0xB3CAB8D9,
            0x34EF4711, 0xDE019FE8, 0x4D674DC4, 0xE056D96B,
            0xA35C016B, 0xDD903BA7, 0x8C1B09B4, 0x2C3E9F25
        };

        private static readonly uint[] IV_512 = 
        {
            0xD5652B63, 0x25F1E6EA, 0xB18F48FA, 0xA1EE3A47,
            0xC8B67B07, 0xBDCE48D3, 0xE3937B78, 0x05DB5186,
            0x613BE326, 0xA11FA303, 0x90C833D4, 0x79CEE316,
            0x1E1AF00F, 0x2829B165, 0x23B25F80, 0x21E11499
        };

        #endregion

        protected override void TransformBlock(byte[] a_data, int a_index)
        {
            uint x0, x1, x2, x3, y0, y1, y2, y3;

            uint[] rk = new uint[448];
            Converters.ConvertBytesToUInts(a_data, a_index, 128, rk);

            uint cnt0 = (uint)(m_processed_bytes << 3);
            uint cnt1 = (uint)(m_processed_bytes >> 29);

            uint state0 = m_state[0];
            uint state1 = m_state[1];
            uint state2 = m_state[2];
            uint state3 = m_state[3];
            uint state4 = m_state[4];
            uint state5 = m_state[5];
            uint state6 = m_state[6];
            uint state7 = m_state[7];
            uint state8 = m_state[8];
            uint state9 = m_state[9];
            uint state10 = m_state[10];
            uint state11 = m_state[11];
            uint state12 = m_state[12];
            uint state13 = m_state[13];
            uint state14 = m_state[14];
            uint state15 = m_state[15];

            x0 = rk[1];
            x1 = rk[2];
            x2 = rk[3];
            x3 = rk[0];
            y0 = Table0[x0 >> 24] ^ Table1[(x1 >> 16) & 0xff] ^ Table2[(x2 >> 8) & 0xff] ^ Table3[x3 & 0xff];
            y1 = Table0[x1 >> 24] ^ Table1[(x2 >> 16) & 0xff] ^ Table2[(x3 >> 8) & 0xff] ^ Table3[x0 & 0xff];
            y2 = Table0[x2 >> 24] ^ Table1[(x3 >> 16) & 0xff] ^ Table2[(x0 >> 8) & 0xff] ^ Table3[x1 & 0xff];
            y3 = Table0[x3 >> 24] ^ Table1[(x0 >> 16) & 0xff] ^ Table2[(x1 >> 8) & 0xff] ^ Table3[x2 & 0xff];
            rk[32] = y0 ^ rk[28] ^ cnt0;
            rk[33] = y1 ^ rk[29] ^ cnt1;
            rk[34] = y2 ^ rk[30];
            rk[35] = ~y3 ^ rk[31];
            x0 = rk[5];
            x1 = rk[6];
            x2 = rk[7];
            x3 = rk[4];
            y0 = Table0[x0 >> 24] ^ Table1[(x1 >> 16) & 0xff] ^ Table2[(x2 >> 8) & 0xff] ^ Table3[x3 & 0xff];
            y1 = Table0[x1 >> 24] ^ Table1[(x2 >> 16) & 0xff] ^ Table2[(x3 >> 8) & 0xff] ^ Table3[x0 & 0xff];
            y2 = Table0[x2 >> 24] ^ Table1[(x3 >> 16) & 0xff] ^ Table2[(x0 >> 8) & 0xff] ^ Table3[x1 & 0xff];
            y3 = Table0[x3 >> 24] ^ Table1[(x0 >> 16) & 0xff] ^ Table2[(x1 >> 8) & 0xff] ^ Table3[x2 & 0xff];
            rk[36] = y0 ^ rk[32];
            rk[37] = y1 ^ rk[33];
            rk[38] = y2 ^ rk[34];
            rk[39] = y3 ^ rk[35];
            x0 = rk[9];
            x1 = rk[10];
            x2 = rk[11];
            x3 = rk[8];
            y0 = Table0[x0 >> 24] ^ Table1[(x1 >> 16) & 0xff] ^ Table2[(x2 >> 8) & 0xff] ^ Table3[x3 & 0xff];
            y1 = Table0[x1 >> 24] ^ Table1[(x2 >> 16) & 0xff] ^ Table2[(x3 >> 8) & 0xff] ^ Table3[x0 & 0xff];
            y2 = Table0[x2 >> 24] ^ Table1[(x3 >> 16) & 0xff] ^ Table2[(x0 >> 8) & 0xff] ^ Table3[x1 & 0xff];
            y3 = Table0[x3 >> 24] ^ Table1[(x0 >> 16) & 0xff] ^ Table2[(x1 >> 8) & 0xff] ^ Table3[x2 & 0xff];
            rk[40] = y0 ^ rk[36];
            rk[41] = y1 ^ rk[37];
            rk[42] = y2 ^ rk[38];
            rk[43] = y3 ^ rk[39];
            x0 = rk[13];
            x1 = rk[14];
            x2 = rk[15];
            x3 = rk[12];
            y0 = Table0[x0 >> 24] ^ Table1[(x1 >> 16) & 0xff] ^ Table2[(x2 >> 8) & 0xff] ^ Table3[x3 & 0xff];
            y1 = Table0[x1 >> 24] ^ Table1[(x2 >> 16) & 0xff] ^ Table2[(x3 >> 8) & 0xff] ^ Table3[x0 & 0xff];
            y2 = Table0[x2 >> 24] ^ Table1[(x3 >> 16) & 0xff] ^ Table2[(x0 >> 8) & 0xff] ^ Table3[x1 & 0xff];
            y3 = Table0[x3 >> 24] ^ Table1[(x0 >> 16) & 0xff] ^ Table2[(x1 >> 8) & 0xff] ^ Table3[x2 & 0xff];
            rk[44] = y0 ^ rk[40];
            rk[45] = y1 ^ rk[41];
            rk[46] = y2 ^ rk[42];
            rk[47] = y3 ^ rk[43];

            x0 = rk[48 - 31];
            x1 = rk[48 - 30];
            x2 = rk[48 - 29];
            x3 = rk[48 - 32];
            y0 = Table0[x0 >> 24] ^ Table1[(x1 >> 16) & 0xff] ^ Table2[(x2 >> 8) & 0xff] ^ Table3[x3 & 0xff];
            y1 = Table0[x1 >> 24] ^ Table1[(x2 >> 16) & 0xff] ^ Table2[(x3 >> 8) & 0xff] ^ Table3[x0 & 0xff];
            y2 = Table0[x2 >> 24] ^ Table1[(x3 >> 16) & 0xff] ^ Table2[(x0 >> 8) & 0xff] ^ Table3[x1 & 0xff];
            y3 = Table0[x3 >> 24] ^ Table1[(x0 >> 16) & 0xff] ^ Table2[(x1 >> 8) & 0xff] ^ Table3[x2 & 0xff];
            rk[48] = y0 ^ rk[48 - 4];
            rk[48 + 1] = y1 ^ rk[48 - 3];
            rk[48 + 2] = y2 ^ rk[48 - 2];
            rk[48 + 3] = y3 ^ rk[48 - 1];
            x0 = rk[48 - 27];
            x1 = rk[48 - 26];
            x2 = rk[48 - 25];
            x3 = rk[48 - 28];
            y0 = Table0[x0 >> 24] ^ Table1[(x1 >> 16) & 0xff] ^ Table2[(x2 >> 8) & 0xff] ^ Table3[x3 & 0xff];
            y1 = Table0[x1 >> 24] ^ Table1[(x2 >> 16) & 0xff] ^ Table2[(x3 >> 8) & 0xff] ^ Table3[x0 & 0xff];
            y2 = Table0[x2 >> 24] ^ Table1[(x3 >> 16) & 0xff] ^ Table2[(x0 >> 8) & 0xff] ^ Table3[x1 & 0xff];
            y3 = Table0[x3 >> 24] ^ Table1[(x0 >> 16) & 0xff] ^ Table2[(x1 >> 8) & 0xff] ^ Table3[x2 & 0xff];
            rk[48 + 4] = y0 ^ rk[48];
            rk[48 + 5] = y1 ^ rk[48 + 1];
            rk[48 + 6] = y2 ^ rk[48 + 2];
            rk[48 + 7] = y3 ^ rk[48 + 3];
            x0 = rk[48 - 23];
            x1 = rk[48 - 22];
            x2 = rk[48 - 21];
            x3 = rk[48 - 24];
            y0 = Table0[x0 >> 24] ^ Table1[(x1 >> 16) & 0xff] ^ Table2[(x2 >> 8) & 0xff] ^ Table3[x3 & 0xff];
            y1 = Table0[x1 >> 24] ^ Table1[(x2 >> 16) & 0xff] ^ Table2[(x3 >> 8) & 0xff] ^ Table3[x0 & 0xff];
            y2 = Table0[x2 >> 24] ^ Table1[(x3 >> 16) & 0xff] ^ Table2[(x0 >> 8) & 0xff] ^ Table3[x1 & 0xff];
            y3 = Table0[x3 >> 24] ^ Table1[(x0 >> 16) & 0xff] ^ Table2[(x1 >> 8) & 0xff] ^ Table3[x2 & 0xff];
            rk[48 + 8] = y0 ^ rk[48 + 4];
            rk[48 + 9] = y1 ^ rk[48 + 5];
            rk[48 + 10] = y2 ^ rk[48 + 6];
            rk[48 + 11] = y3 ^ rk[48 + 7];
            x0 = rk[48 - 19];
            x1 = rk[48 - 18];
            x2 = rk[48 - 17];
            x3 = rk[48 - 20];
            y0 = Table0[x0 >> 24] ^ Table1[(x1 >> 16) & 0xff] ^ Table2[(x2 >> 8) & 0xff] ^ Table3[x3 & 0xff];
            y1 = Table0[x1 >> 24] ^ Table1[(x2 >> 16) & 0xff] ^ Table2[(x3 >> 8) & 0xff] ^ Table3[x0 & 0xff];
            y2 = Table0[x2 >> 24] ^ Table1[(x3 >> 16) & 0xff] ^ Table2[(x0 >> 8) & 0xff] ^ Table3[x1 & 0xff];
            y3 = Table0[x3 >> 24] ^ Table1[(x0 >> 16) & 0xff] ^ Table2[(x1 >> 8) & 0xff] ^ Table3[x2 & 0xff];
            rk[48 + 12] = y0 ^ rk[48 + 8];
            rk[48 + 13] = y1 ^ rk[48 + 9];
            rk[48 + 14] = y2 ^ rk[48 + 10];
            rk[48 + 15] = y3 ^ rk[48 + 11];

            for (int i = 0; i < 32; i++)
                rk[64 + i] = rk[i + 64 - 32] ^ rk[i + 64 - 7];

            x0 = rk[96 - 31];
            x1 = rk[96 - 30];
            x2 = rk[96 - 29];
            x3 = rk[96 - 32];
            y0 = Table0[x0 >> 24] ^ Table1[(x1 >> 16) & 0xff] ^ Table2[(x2 >> 8) & 0xff] ^ Table3[x3 & 0xff];
            y1 = Table0[x1 >> 24] ^ Table1[(x2 >> 16) & 0xff] ^ Table2[(x3 >> 8) & 0xff] ^ Table3[x0 & 0xff];
            y2 = Table0[x2 >> 24] ^ Table1[(x3 >> 16) & 0xff] ^ Table2[(x0 >> 8) & 0xff] ^ Table3[x1 & 0xff];
            y3 = Table0[x3 >> 24] ^ Table1[(x0 >> 16) & 0xff] ^ Table2[(x1 >> 8) & 0xff] ^ Table3[x2 & 0xff];
            rk[96] = y0 ^ rk[96 - 4];
            rk[96 + 1] = y1 ^ rk[96 - 3];
            rk[96 + 2] = y2 ^ rk[96 - 2];
            rk[96 + 3] = y3 ^ rk[96 - 1];
            x0 = rk[96 - 27];
            x1 = rk[96 - 26];
            x2 = rk[96 - 25];
            x3 = rk[96 - 28];
            y0 = Table0[x0 >> 24] ^ Table1[(x1 >> 16) & 0xff] ^ Table2[(x2 >> 8) & 0xff] ^ Table3[x3 & 0xff];
            y1 = Table0[x1 >> 24] ^ Table1[(x2 >> 16) & 0xff] ^ Table2[(x3 >> 8) & 0xff] ^ Table3[x0 & 0xff];
            y2 = Table0[x2 >> 24] ^ Table1[(x3 >> 16) & 0xff] ^ Table2[(x0 >> 8) & 0xff] ^ Table3[x1 & 0xff];
            y3 = Table0[x3 >> 24] ^ Table1[(x0 >> 16) & 0xff] ^ Table2[(x1 >> 8) & 0xff] ^ Table3[x2 & 0xff];
            rk[96 + 4] = y0 ^ rk[96];
            rk[96 + 5] = y1 ^ rk[96 + 1];
            rk[96 + 6] = y2 ^ rk[96 + 2];
            rk[96 + 7] = y3 ^ rk[96 + 3];
            x0 = rk[96 - 23];
            x1 = rk[96 - 22];
            x2 = rk[96 - 21];
            x3 = rk[96 - 24];
            y0 = Table0[x0 >> 24] ^ Table1[(x1 >> 16) & 0xff] ^ Table2[(x2 >> 8) & 0xff] ^ Table3[x3 & 0xff];
            y1 = Table0[x1 >> 24] ^ Table1[(x2 >> 16) & 0xff] ^ Table2[(x3 >> 8) & 0xff] ^ Table3[x0 & 0xff];
            y2 = Table0[x2 >> 24] ^ Table1[(x3 >> 16) & 0xff] ^ Table2[(x0 >> 8) & 0xff] ^ Table3[x1 & 0xff];
            y3 = Table0[x3 >> 24] ^ Table1[(x0 >> 16) & 0xff] ^ Table2[(x1 >> 8) & 0xff] ^ Table3[x2 & 0xff];
            rk[96 + 8] = y0 ^ rk[96 + 4];
            rk[96 + 9] = y1 ^ rk[96 + 5];
            rk[96 + 10] = y2 ^ rk[96 + 6];
            rk[96 + 11] = y3 ^ rk[96 + 7];
            x0 = rk[96 - 19];
            x1 = rk[96 - 18];
            x2 = rk[96 - 17];
            x3 = rk[96 - 20];
            y0 = Table0[x0 >> 24] ^ Table1[(x1 >> 16) & 0xff] ^ Table2[(x2 >> 8) & 0xff] ^ Table3[x3 & 0xff];
            y1 = Table0[x1 >> 24] ^ Table1[(x2 >> 16) & 0xff] ^ Table2[(x3 >> 8) & 0xff] ^ Table3[x0 & 0xff];
            y2 = Table0[x2 >> 24] ^ Table1[(x3 >> 16) & 0xff] ^ Table2[(x0 >> 8) & 0xff] ^ Table3[x1 & 0xff];
            y3 = Table0[x3 >> 24] ^ Table1[(x0 >> 16) & 0xff] ^ Table2[(x1 >> 8) & 0xff] ^ Table3[x2 & 0xff];
            rk[96 + 12] = y0 ^ rk[96 + 8];
            rk[96 + 13] = y1 ^ rk[96 + 9];
            rk[96 + 14] = y2 ^ rk[96 + 10];
            rk[96 + 15] = y3 ^ rk[96 + 11];

            x0 = rk[112 - 31];
            x1 = rk[112 - 30];
            x2 = rk[112 - 29];
            x3 = rk[112 - 32];
            y0 = Table0[x0 >> 24] ^ Table1[(x1 >> 16) & 0xff] ^ Table2[(x2 >> 8) & 0xff] ^ Table3[x3 & 0xff];
            y1 = Table0[x1 >> 24] ^ Table1[(x2 >> 16) & 0xff] ^ Table2[(x3 >> 8) & 0xff] ^ Table3[x0 & 0xff];
            y2 = Table0[x2 >> 24] ^ Table1[(x3 >> 16) & 0xff] ^ Table2[(x0 >> 8) & 0xff] ^ Table3[x1 & 0xff];
            y3 = Table0[x3 >> 24] ^ Table1[(x0 >> 16) & 0xff] ^ Table2[(x1 >> 8) & 0xff] ^ Table3[x2 & 0xff];
            rk[112] = y0 ^ rk[112 - 4];
            rk[112 + 1] = y1 ^ rk[112 - 3];
            rk[112 + 2] = y2 ^ rk[112 - 2];
            rk[112 + 3] = y3 ^ rk[112 - 1];
            x0 = rk[112 - 27];
            x1 = rk[112 - 26];
            x2 = rk[112 - 25];
            x3 = rk[112 - 28];
            y0 = Table0[x0 >> 24] ^ Table1[(x1 >> 16) & 0xff] ^ Table2[(x2 >> 8) & 0xff] ^ Table3[x3 & 0xff];
            y1 = Table0[x1 >> 24] ^ Table1[(x2 >> 16) & 0xff] ^ Table2[(x3 >> 8) & 0xff] ^ Table3[x0 & 0xff];
            y2 = Table0[x2 >> 24] ^ Table1[(x3 >> 16) & 0xff] ^ Table2[(x0 >> 8) & 0xff] ^ Table3[x1 & 0xff];
            y3 = Table0[x3 >> 24] ^ Table1[(x0 >> 16) & 0xff] ^ Table2[(x1 >> 8) & 0xff] ^ Table3[x2 & 0xff];
            rk[112 + 4] = y0 ^ rk[112];
            rk[112 + 5] = y1 ^ rk[112 + 1];
            rk[112 + 6] = y2 ^ rk[112 + 2];
            rk[112 + 7] = y3 ^ rk[112 + 3];
            x0 = rk[112 - 23];
            x1 = rk[112 - 22];
            x2 = rk[112 - 21];
            x3 = rk[112 - 24];
            y0 = Table0[x0 >> 24] ^ Table1[(x1 >> 16) & 0xff] ^ Table2[(x2 >> 8) & 0xff] ^ Table3[x3 & 0xff];
            y1 = Table0[x1 >> 24] ^ Table1[(x2 >> 16) & 0xff] ^ Table2[(x3 >> 8) & 0xff] ^ Table3[x0 & 0xff];
            y2 = Table0[x2 >> 24] ^ Table1[(x3 >> 16) & 0xff] ^ Table2[(x0 >> 8) & 0xff] ^ Table3[x1 & 0xff];
            y3 = Table0[x3 >> 24] ^ Table1[(x0 >> 16) & 0xff] ^ Table2[(x1 >> 8) & 0xff] ^ Table3[x2 & 0xff];
            rk[112 + 8] = y0 ^ rk[112 + 4];
            rk[112 + 9] = y1 ^ rk[112 + 5];
            rk[112 + 10] = y2 ^ rk[112 + 6];
            rk[112 + 11] = y3 ^ rk[112 + 7];
            x0 = rk[112 - 19];
            x1 = rk[112 - 18];
            x2 = rk[112 - 17];
            x3 = rk[112 - 20];
            y0 = Table0[x0 >> 24] ^ Table1[(x1 >> 16) & 0xff] ^ Table2[(x2 >> 8) & 0xff] ^ Table3[x3 & 0xff];
            y1 = Table0[x1 >> 24] ^ Table1[(x2 >> 16) & 0xff] ^ Table2[(x3 >> 8) & 0xff] ^ Table3[x0 & 0xff];
            y2 = Table0[x2 >> 24] ^ Table1[(x3 >> 16) & 0xff] ^ Table2[(x0 >> 8) & 0xff] ^ Table3[x1 & 0xff];
            y3 = Table0[x3 >> 24] ^ Table1[(x0 >> 16) & 0xff] ^ Table2[(x1 >> 8) & 0xff] ^ Table3[x2 & 0xff];
            rk[112 + 12] = y0 ^ rk[112 + 8];
            rk[112 + 13] = y1 ^ rk[112 + 9];
            rk[112 + 14] = y2 ^ rk[112 + 10];
            rk[112 + 15] = y3 ^ rk[112 + 11];

            for (int i = 0; i < 32; i++)
                rk[128 + i] = rk[i + 128 - 32] ^ rk[i + 128 - 7];

            x0 = rk[129];
            x1 = rk[130];
            x2 = rk[131];
            x3 = rk[128];
            y0 = Table0[x0 >> 24] ^ Table1[(x1 >> 16) & 0xff] ^ Table2[(x2 >> 8) & 0xff] ^ Table3[x3 & 0xff];
            y1 = Table0[x1 >> 24] ^ Table1[(x2 >> 16) & 0xff] ^ Table2[(x3 >> 8) & 0xff] ^ Table3[x0 & 0xff];
            y2 = Table0[x2 >> 24] ^ Table1[(x3 >> 16) & 0xff] ^ Table2[(x0 >> 8) & 0xff] ^ Table3[x1 & 0xff];
            y3 = Table0[x3 >> 24] ^ Table1[(x0 >> 16) & 0xff] ^ Table2[(x1 >> 8) & 0xff] ^ Table3[x2 & 0xff];
            rk[160] = y0 ^ rk[156];
            rk[161] = y1 ^ rk[157];
            rk[162] = y2 ^ rk[158];
            rk[163] = y3 ^ rk[159];
            x0 = rk[133];
            x1 = rk[134];
            x2 = rk[135];
            x3 = rk[132];
            y0 = Table0[x0 >> 24] ^ Table1[(x1 >> 16) & 0xff] ^ Table2[(x2 >> 8) & 0xff] ^ Table3[x3 & 0xff];
            y1 = Table0[x1 >> 24] ^ Table1[(x2 >> 16) & 0xff] ^ Table2[(x3 >> 8) & 0xff] ^ Table3[x0 & 0xff];
            y2 = Table0[x2 >> 24] ^ Table1[(x3 >> 16) & 0xff] ^ Table2[(x0 >> 8) & 0xff] ^ Table3[x1 & 0xff];
            y3 = Table0[x3 >> 24] ^ Table1[(x0 >> 16) & 0xff] ^ Table2[(x1 >> 8) & 0xff] ^ Table3[x2 & 0xff];
            rk[164] = y0 ^ rk[160];
            rk[165] = y1 ^ rk[161];
            rk[166] = y2 ^ rk[162] ^ cnt1;
            rk[167] = y3 ^ rk[163] ^ ~cnt0;
            x0 = rk[137];
            x1 = rk[138];
            x2 = rk[139];
            x3 = rk[136];
            y0 = Table0[x0 >> 24] ^ Table1[(x1 >> 16) & 0xff] ^ Table2[(x2 >> 8) & 0xff] ^ Table3[x3 & 0xff];
            y1 = Table0[x1 >> 24] ^ Table1[(x2 >> 16) & 0xff] ^ Table2[(x3 >> 8) & 0xff] ^ Table3[x0 & 0xff];
            y2 = Table0[x2 >> 24] ^ Table1[(x3 >> 16) & 0xff] ^ Table2[(x0 >> 8) & 0xff] ^ Table3[x1 & 0xff];
            y3 = Table0[x3 >> 24] ^ Table1[(x0 >> 16) & 0xff] ^ Table2[(x1 >> 8) & 0xff] ^ Table3[x2 & 0xff];
            rk[168] = y0 ^ rk[164];
            rk[169] = y1 ^ rk[165];
            rk[170] = y2 ^ rk[166];
            rk[171] = y3 ^ rk[167];
            x0 = rk[141];
            x1 = rk[142];
            x2 = rk[143];
            x3 = rk[140];
            y0 = Table0[x0 >> 24] ^ Table1[(x1 >> 16) & 0xff] ^ Table2[(x2 >> 8) & 0xff] ^ Table3[x3 & 0xff];
            y1 = Table0[x1 >> 24] ^ Table1[(x2 >> 16) & 0xff] ^ Table2[(x3 >> 8) & 0xff] ^ Table3[x0 & 0xff];
            y2 = Table0[x2 >> 24] ^ Table1[(x3 >> 16) & 0xff] ^ Table2[(x0 >> 8) & 0xff] ^ Table3[x1 & 0xff];
            y3 = Table0[x3 >> 24] ^ Table1[(x0 >> 16) & 0xff] ^ Table2[(x1 >> 8) & 0xff] ^ Table3[x2 & 0xff];
            rk[172] = y0 ^ rk[168];
            rk[173] = y1 ^ rk[169];
            rk[174] = y2 ^ rk[170];
            rk[175] = y3 ^ rk[171];

            x0 = rk[176 - 31];
            x1 = rk[176 - 30];
            x2 = rk[176 - 29];
            x3 = rk[176 - 32];
            y0 = Table0[x0 >> 24] ^ Table1[(x1 >> 16) & 0xff] ^ Table2[(x2 >> 8) & 0xff] ^ Table3[x3 & 0xff];
            y1 = Table0[x1 >> 24] ^ Table1[(x2 >> 16) & 0xff] ^ Table2[(x3 >> 8) & 0xff] ^ Table3[x0 & 0xff];
            y2 = Table0[x2 >> 24] ^ Table1[(x3 >> 16) & 0xff] ^ Table2[(x0 >> 8) & 0xff] ^ Table3[x1 & 0xff];
            y3 = Table0[x3 >> 24] ^ Table1[(x0 >> 16) & 0xff] ^ Table2[(x1 >> 8) & 0xff] ^ Table3[x2 & 0xff];
            rk[176] = y0 ^ rk[176 - 4];
            rk[176 + 1] = y1 ^ rk[176 - 3];
            rk[176 + 2] = y2 ^ rk[176 - 2];
            rk[176 + 3] = y3 ^ rk[176 - 1];
            x0 = rk[176 - 27];
            x1 = rk[176 - 26];
            x2 = rk[176 - 25];
            x3 = rk[176 - 28];
            y0 = Table0[x0 >> 24] ^ Table1[(x1 >> 16) & 0xff] ^ Table2[(x2 >> 8) & 0xff] ^ Table3[x3 & 0xff];
            y1 = Table0[x1 >> 24] ^ Table1[(x2 >> 16) & 0xff] ^ Table2[(x3 >> 8) & 0xff] ^ Table3[x0 & 0xff];
            y2 = Table0[x2 >> 24] ^ Table1[(x3 >> 16) & 0xff] ^ Table2[(x0 >> 8) & 0xff] ^ Table3[x1 & 0xff];
            y3 = Table0[x3 >> 24] ^ Table1[(x0 >> 16) & 0xff] ^ Table2[(x1 >> 8) & 0xff] ^ Table3[x2 & 0xff];
            rk[176 + 4] = y0 ^ rk[176];
            rk[176 + 5] = y1 ^ rk[176 + 1];
            rk[176 + 6] = y2 ^ rk[176 + 2];
            rk[176 + 7] = y3 ^ rk[176 + 3];
            x0 = rk[176 - 23];
            x1 = rk[176 - 22];
            x2 = rk[176 - 21];
            x3 = rk[176 - 24];
            y0 = Table0[x0 >> 24] ^ Table1[(x1 >> 16) & 0xff] ^ Table2[(x2 >> 8) & 0xff] ^ Table3[x3 & 0xff];
            y1 = Table0[x1 >> 24] ^ Table1[(x2 >> 16) & 0xff] ^ Table2[(x3 >> 8) & 0xff] ^ Table3[x0 & 0xff];
            y2 = Table0[x2 >> 24] ^ Table1[(x3 >> 16) & 0xff] ^ Table2[(x0 >> 8) & 0xff] ^ Table3[x1 & 0xff];
            y3 = Table0[x3 >> 24] ^ Table1[(x0 >> 16) & 0xff] ^ Table2[(x1 >> 8) & 0xff] ^ Table3[x2 & 0xff];
            rk[176 + 8] = y0 ^ rk[176 + 4];
            rk[176 + 9] = y1 ^ rk[176 + 5];
            rk[176 + 10] = y2 ^ rk[176 + 6];
            rk[176 + 11] = y3 ^ rk[176 + 7];
            x0 = rk[176 - 19];
            x1 = rk[176 - 18];
            x2 = rk[176 - 17];
            x3 = rk[176 - 20];
            y0 = Table0[x0 >> 24] ^ Table1[(x1 >> 16) & 0xff] ^ Table2[(x2 >> 8) & 0xff] ^ Table3[x3 & 0xff];
            y1 = Table0[x1 >> 24] ^ Table1[(x2 >> 16) & 0xff] ^ Table2[(x3 >> 8) & 0xff] ^ Table3[x0 & 0xff];
            y2 = Table0[x2 >> 24] ^ Table1[(x3 >> 16) & 0xff] ^ Table2[(x0 >> 8) & 0xff] ^ Table3[x1 & 0xff];
            y3 = Table0[x3 >> 24] ^ Table1[(x0 >> 16) & 0xff] ^ Table2[(x1 >> 8) & 0xff] ^ Table3[x2 & 0xff];
            rk[176 + 12] = y0 ^ rk[176 + 8];
            rk[176 + 13] = y1 ^ rk[176 + 9];
            rk[176 + 14] = y2 ^ rk[176 + 10];
            rk[176 + 15] = y3 ^ rk[176 + 11];

            for (int i = 0; i < 32; i++)
                rk[192 + i] = rk[i + 192 - 32] ^ rk[i + 192 - 7];

            x0 = rk[224 - 31];
            x1 = rk[224 - 30];
            x2 = rk[224 - 29];
            x3 = rk[224 - 32];
            y0 = Table0[x0 >> 24] ^ Table1[(x1 >> 16) & 0xff] ^ Table2[(x2 >> 8) & 0xff] ^ Table3[x3 & 0xff];
            y1 = Table0[x1 >> 24] ^ Table1[(x2 >> 16) & 0xff] ^ Table2[(x3 >> 8) & 0xff] ^ Table3[x0 & 0xff];
            y2 = Table0[x2 >> 24] ^ Table1[(x3 >> 16) & 0xff] ^ Table2[(x0 >> 8) & 0xff] ^ Table3[x1 & 0xff];
            y3 = Table0[x3 >> 24] ^ Table1[(x0 >> 16) & 0xff] ^ Table2[(x1 >> 8) & 0xff] ^ Table3[x2 & 0xff];
            rk[224] = y0 ^ rk[224 - 4];
            rk[224 + 1] = y1 ^ rk[224 - 3];
            rk[224 + 2] = y2 ^ rk[224 - 2];
            rk[224 + 3] = y3 ^ rk[224 - 1];
            x0 = rk[224 - 27];
            x1 = rk[224 - 26];
            x2 = rk[224 - 25];
            x3 = rk[224 - 28];
            y0 = Table0[x0 >> 24] ^ Table1[(x1 >> 16) & 0xff] ^ Table2[(x2 >> 8) & 0xff] ^ Table3[x3 & 0xff];
            y1 = Table0[x1 >> 24] ^ Table1[(x2 >> 16) & 0xff] ^ Table2[(x3 >> 8) & 0xff] ^ Table3[x0 & 0xff];
            y2 = Table0[x2 >> 24] ^ Table1[(x3 >> 16) & 0xff] ^ Table2[(x0 >> 8) & 0xff] ^ Table3[x1 & 0xff];
            y3 = Table0[x3 >> 24] ^ Table1[(x0 >> 16) & 0xff] ^ Table2[(x1 >> 8) & 0xff] ^ Table3[x2 & 0xff];
            rk[224 + 4] = y0 ^ rk[224];
            rk[224 + 5] = y1 ^ rk[224 + 1];
            rk[224 + 6] = y2 ^ rk[224 + 2];
            rk[224 + 7] = y3 ^ rk[224 + 3];
            x0 = rk[224 - 23];
            x1 = rk[224 - 22];
            x2 = rk[224 - 21];
            x3 = rk[224 - 24];
            y0 = Table0[x0 >> 24] ^ Table1[(x1 >> 16) & 0xff] ^ Table2[(x2 >> 8) & 0xff] ^ Table3[x3 & 0xff];
            y1 = Table0[x1 >> 24] ^ Table1[(x2 >> 16) & 0xff] ^ Table2[(x3 >> 8) & 0xff] ^ Table3[x0 & 0xff];
            y2 = Table0[x2 >> 24] ^ Table1[(x3 >> 16) & 0xff] ^ Table2[(x0 >> 8) & 0xff] ^ Table3[x1 & 0xff];
            y3 = Table0[x3 >> 24] ^ Table1[(x0 >> 16) & 0xff] ^ Table2[(x1 >> 8) & 0xff] ^ Table3[x2 & 0xff];
            rk[224 + 8] = y0 ^ rk[224 + 4];
            rk[224 + 9] = y1 ^ rk[224 + 5];
            rk[224 + 10] = y2 ^ rk[224 + 6];
            rk[224 + 11] = y3 ^ rk[224 + 7];
            x0 = rk[224 - 19];
            x1 = rk[224 - 18];
            x2 = rk[224 - 17];
            x3 = rk[224 - 20];
            y0 = Table0[x0 >> 24] ^ Table1[(x1 >> 16) & 0xff] ^ Table2[(x2 >> 8) & 0xff] ^ Table3[x3 & 0xff];
            y1 = Table0[x1 >> 24] ^ Table1[(x2 >> 16) & 0xff] ^ Table2[(x3 >> 8) & 0xff] ^ Table3[x0 & 0xff];
            y2 = Table0[x2 >> 24] ^ Table1[(x3 >> 16) & 0xff] ^ Table2[(x0 >> 8) & 0xff] ^ Table3[x1 & 0xff];
            y3 = Table0[x3 >> 24] ^ Table1[(x0 >> 16) & 0xff] ^ Table2[(x1 >> 8) & 0xff] ^ Table3[x2 & 0xff];
            rk[224 + 12] = y0 ^ rk[224 + 8];
            rk[224 + 13] = y1 ^ rk[224 + 9];
            rk[224 + 14] = y2 ^ rk[224 + 10];
            rk[224 + 15] = y3 ^ rk[224 + 11];

            x0 = rk[240 - 31];
            x1 = rk[240 - 30];
            x2 = rk[240 - 29];
            x3 = rk[240 - 32];
            y0 = Table0[x0 >> 24] ^ Table1[(x1 >> 16) & 0xff] ^ Table2[(x2 >> 8) & 0xff] ^ Table3[x3 & 0xff];
            y1 = Table0[x1 >> 24] ^ Table1[(x2 >> 16) & 0xff] ^ Table2[(x3 >> 8) & 0xff] ^ Table3[x0 & 0xff];
            y2 = Table0[x2 >> 24] ^ Table1[(x3 >> 16) & 0xff] ^ Table2[(x0 >> 8) & 0xff] ^ Table3[x1 & 0xff];
            y3 = Table0[x3 >> 24] ^ Table1[(x0 >> 16) & 0xff] ^ Table2[(x1 >> 8) & 0xff] ^ Table3[x2 & 0xff];
            rk[240] = y0 ^ rk[240 - 4];
            rk[240 + 1] = y1 ^ rk[240 - 3];
            rk[240 + 2] = y2 ^ rk[240 - 2];
            rk[240 + 3] = y3 ^ rk[240 - 1];
            x0 = rk[240 - 27];
            x1 = rk[240 - 26];
            x2 = rk[240 - 25];
            x3 = rk[240 - 28];
            y0 = Table0[x0 >> 24] ^ Table1[(x1 >> 16) & 0xff] ^ Table2[(x2 >> 8) & 0xff] ^ Table3[x3 & 0xff];
            y1 = Table0[x1 >> 24] ^ Table1[(x2 >> 16) & 0xff] ^ Table2[(x3 >> 8) & 0xff] ^ Table3[x0 & 0xff];
            y2 = Table0[x2 >> 24] ^ Table1[(x3 >> 16) & 0xff] ^ Table2[(x0 >> 8) & 0xff] ^ Table3[x1 & 0xff];
            y3 = Table0[x3 >> 24] ^ Table1[(x0 >> 16) & 0xff] ^ Table2[(x1 >> 8) & 0xff] ^ Table3[x2 & 0xff];
            rk[240 + 4] = y0 ^ rk[240];
            rk[240 + 5] = y1 ^ rk[240 + 1];
            rk[240 + 6] = y2 ^ rk[240 + 2];
            rk[240 + 7] = y3 ^ rk[240 + 3];
            x0 = rk[240 - 23];
            x1 = rk[240 - 22];
            x2 = rk[240 - 21];
            x3 = rk[240 - 24];
            y0 = Table0[x0 >> 24] ^ Table1[(x1 >> 16) & 0xff] ^ Table2[(x2 >> 8) & 0xff] ^ Table3[x3 & 0xff];
            y1 = Table0[x1 >> 24] ^ Table1[(x2 >> 16) & 0xff] ^ Table2[(x3 >> 8) & 0xff] ^ Table3[x0 & 0xff];
            y2 = Table0[x2 >> 24] ^ Table1[(x3 >> 16) & 0xff] ^ Table2[(x0 >> 8) & 0xff] ^ Table3[x1 & 0xff];
            y3 = Table0[x3 >> 24] ^ Table1[(x0 >> 16) & 0xff] ^ Table2[(x1 >> 8) & 0xff] ^ Table3[x2 & 0xff];
            rk[240 + 8] = y0 ^ rk[240 + 4];
            rk[240 + 9] = y1 ^ rk[240 + 5];
            rk[240 + 10] = y2 ^ rk[240 + 6];
            rk[240 + 11] = y3 ^ rk[240 + 7];
            x0 = rk[240 - 19];
            x1 = rk[240 - 18];
            x2 = rk[240 - 17];
            x3 = rk[240 - 20];
            y0 = Table0[x0 >> 24] ^ Table1[(x1 >> 16) & 0xff] ^ Table2[(x2 >> 8) & 0xff] ^ Table3[x3 & 0xff];
            y1 = Table0[x1 >> 24] ^ Table1[(x2 >> 16) & 0xff] ^ Table2[(x3 >> 8) & 0xff] ^ Table3[x0 & 0xff];
            y2 = Table0[x2 >> 24] ^ Table1[(x3 >> 16) & 0xff] ^ Table2[(x0 >> 8) & 0xff] ^ Table3[x1 & 0xff];
            y3 = Table0[x3 >> 24] ^ Table1[(x0 >> 16) & 0xff] ^ Table2[(x1 >> 8) & 0xff] ^ Table3[x2 & 0xff];
            rk[240 + 12] = y0 ^ rk[240 + 8];
            rk[240 + 13] = y1 ^ rk[240 + 9];
            rk[240 + 14] = y2 ^ rk[240 + 10];
            rk[240 + 15] = y3 ^ rk[240 + 11];

            for (int i = 0; i < 32; i++)
                rk[256 + i] = rk[i + 256 - 32] ^ rk[i + 256 - 7];

            x0 = rk[288 - 31];
            x1 = rk[288 - 30];
            x2 = rk[288 - 29];
            x3 = rk[288 - 32];
            y0 = Table0[x0 >> 24] ^ Table1[(x1 >> 16) & 0xff] ^ Table2[(x2 >> 8) & 0xff] ^ Table3[x3 & 0xff];
            y1 = Table0[x1 >> 24] ^ Table1[(x2 >> 16) & 0xff] ^ Table2[(x3 >> 8) & 0xff] ^ Table3[x0 & 0xff];
            y2 = Table0[x2 >> 24] ^ Table1[(x3 >> 16) & 0xff] ^ Table2[(x0 >> 8) & 0xff] ^ Table3[x1 & 0xff];
            y3 = Table0[x3 >> 24] ^ Table1[(x0 >> 16) & 0xff] ^ Table2[(x1 >> 8) & 0xff] ^ Table3[x2 & 0xff];
            rk[288] = y0 ^ rk[288 - 4];
            rk[288 + 1] = y1 ^ rk[288 - 3];
            rk[288 + 2] = y2 ^ rk[288 - 2];
            rk[288 + 3] = y3 ^ rk[288 - 1];
            x0 = rk[288 - 27];
            x1 = rk[288 - 26];
            x2 = rk[288 - 25];
            x3 = rk[288 - 28];
            y0 = Table0[x0 >> 24] ^ Table1[(x1 >> 16) & 0xff] ^ Table2[(x2 >> 8) & 0xff] ^ Table3[x3 & 0xff];
            y1 = Table0[x1 >> 24] ^ Table1[(x2 >> 16) & 0xff] ^ Table2[(x3 >> 8) & 0xff] ^ Table3[x0 & 0xff];
            y2 = Table0[x2 >> 24] ^ Table1[(x3 >> 16) & 0xff] ^ Table2[(x0 >> 8) & 0xff] ^ Table3[x1 & 0xff];
            y3 = Table0[x3 >> 24] ^ Table1[(x0 >> 16) & 0xff] ^ Table2[(x1 >> 8) & 0xff] ^ Table3[x2 & 0xff];
            rk[288 + 4] = y0 ^ rk[288];
            rk[288 + 5] = y1 ^ rk[288 + 1];
            rk[288 + 6] = y2 ^ rk[288 + 2];
            rk[288 + 7] = y3 ^ rk[288 + 3];
            x0 = rk[288 - 23];
            x1 = rk[288 - 22];
            x2 = rk[288 - 21];
            x3 = rk[288 - 24];
            y0 = Table0[x0 >> 24] ^ Table1[(x1 >> 16) & 0xff] ^ Table2[(x2 >> 8) & 0xff] ^ Table3[x3 & 0xff];
            y1 = Table0[x1 >> 24] ^ Table1[(x2 >> 16) & 0xff] ^ Table2[(x3 >> 8) & 0xff] ^ Table3[x0 & 0xff];
            y2 = Table0[x2 >> 24] ^ Table1[(x3 >> 16) & 0xff] ^ Table2[(x0 >> 8) & 0xff] ^ Table3[x1 & 0xff];
            y3 = Table0[x3 >> 24] ^ Table1[(x0 >> 16) & 0xff] ^ Table2[(x1 >> 8) & 0xff] ^ Table3[x2 & 0xff];
            rk[288 + 8] = y0 ^ rk[288 + 4];
            rk[288 + 9] = y1 ^ rk[288 + 5];
            rk[288 + 10] = y2 ^ rk[288 + 6];
            rk[288 + 11] = y3 ^ rk[288 + 7];
            x0 = rk[288 - 19];
            x1 = rk[288 - 18];
            x2 = rk[288 - 17];
            x3 = rk[288 - 20];
            y0 = Table0[x0 >> 24] ^ Table1[(x1 >> 16) & 0xff] ^ Table2[(x2 >> 8) & 0xff] ^ Table3[x3 & 0xff];
            y1 = Table0[x1 >> 24] ^ Table1[(x2 >> 16) & 0xff] ^ Table2[(x3 >> 8) & 0xff] ^ Table3[x0 & 0xff];
            y2 = Table0[x2 >> 24] ^ Table1[(x3 >> 16) & 0xff] ^ Table2[(x0 >> 8) & 0xff] ^ Table3[x1 & 0xff];
            y3 = Table0[x3 >> 24] ^ Table1[(x0 >> 16) & 0xff] ^ Table2[(x1 >> 8) & 0xff] ^ Table3[x2 & 0xff];
            rk[288 + 12] = y0 ^ rk[288 + 8];
            rk[288 + 13] = y1 ^ rk[288 + 9];
            rk[288 + 14] = y2 ^ rk[288 + 10];
            rk[288 + 15] = y3 ^ rk[288 + 11];

            x0 = rk[273];
            x1 = rk[274];
            x2 = rk[275];
            x3 = rk[272];
            y0 = Table0[x0 >> 24] ^ Table1[(x1 >> 16) & 0xff] ^ Table2[(x2 >> 8) & 0xff] ^ Table3[x3 & 0xff];
            y1 = Table0[x1 >> 24] ^ Table1[(x2 >> 16) & 0xff] ^ Table2[(x3 >> 8) & 0xff] ^ Table3[x0 & 0xff];
            y2 = Table0[x2 >> 24] ^ Table1[(x3 >> 16) & 0xff] ^ Table2[(x0 >> 8) & 0xff] ^ Table3[x1 & 0xff];
            y3 = Table0[x3 >> 24] ^ Table1[(x0 >> 16) & 0xff] ^ Table2[(x1 >> 8) & 0xff] ^ Table3[x2 & 0xff];
            rk[304] = y0 ^ rk[300];
            rk[305] = y1 ^ rk[301];
            rk[306] = y2 ^ rk[302];
            rk[307] = y3 ^ rk[303];
            x0 = rk[277];
            x1 = rk[278];
            x2 = rk[279];
            x3 = rk[276];
            y0 = Table0[x0 >> 24] ^ Table1[(x1 >> 16) & 0xff] ^ Table2[(x2 >> 8) & 0xff] ^ Table3[x3 & 0xff];
            y1 = Table0[x1 >> 24] ^ Table1[(x2 >> 16) & 0xff] ^ Table2[(x3 >> 8) & 0xff] ^ Table3[x0 & 0xff];
            y2 = Table0[x2 >> 24] ^ Table1[(x3 >> 16) & 0xff] ^ Table2[(x0 >> 8) & 0xff] ^ Table3[x1 & 0xff];
            y3 = Table0[x3 >> 24] ^ Table1[(x0 >> 16) & 0xff] ^ Table2[(x1 >> 8) & 0xff] ^ Table3[x2 & 0xff];
            rk[308] = y0 ^ rk[304];
            rk[309] = y1 ^ rk[305];
            rk[310] = y2 ^ rk[306];
            rk[311] = y3 ^ rk[307];
            x0 = rk[281];
            x1 = rk[282];
            x2 = rk[283];
            x3 = rk[280];
            y0 = Table0[x0 >> 24] ^ Table1[(x1 >> 16) & 0xff] ^ Table2[(x2 >> 8) & 0xff] ^ Table3[x3 & 0xff];
            y1 = Table0[x1 >> 24] ^ Table1[(x2 >> 16) & 0xff] ^ Table2[(x3 >> 8) & 0xff] ^ Table3[x0 & 0xff];
            y2 = Table0[x2 >> 24] ^ Table1[(x3 >> 16) & 0xff] ^ Table2[(x0 >> 8) & 0xff] ^ Table3[x1 & 0xff];
            y3 = Table0[x3 >> 24] ^ Table1[(x0 >> 16) & 0xff] ^ Table2[(x1 >> 8) & 0xff] ^ Table3[x2 & 0xff];
            rk[312] = y0 ^ rk[308];
            rk[313] = y1 ^ rk[309];
            rk[314] = y2 ^ rk[310];
            rk[315] = y3 ^ rk[311];
            x0 = rk[285];
            x1 = rk[286];
            x2 = rk[287];
            x3 = rk[284];
            y0 = Table0[x0 >> 24] ^ Table1[(x1 >> 16) & 0xff] ^ Table2[(x2 >> 8) & 0xff] ^ Table3[x3 & 0xff];
            y1 = Table0[x1 >> 24] ^ Table1[(x2 >> 16) & 0xff] ^ Table2[(x3 >> 8) & 0xff] ^ Table3[x0 & 0xff];
            y2 = Table0[x2 >> 24] ^ Table1[(x3 >> 16) & 0xff] ^ Table2[(x0 >> 8) & 0xff] ^ Table3[x1 & 0xff];
            y3 = Table0[x3 >> 24] ^ Table1[(x0 >> 16) & 0xff] ^ Table2[(x1 >> 8) & 0xff] ^ Table3[x2 & 0xff];
            rk[316] = y0 ^ rk[312];
            rk[317] = y1 ^ rk[313];
            rk[318] = y2 ^ rk[314] ^ cnt0;
            rk[319] = y3 ^ rk[315] ^ ~cnt1;

            for (int i = 0; i < 32; i++)
                rk[320 + i] = rk[i + 320 - 32] ^ rk[i + 320 - 7];

            x0 = rk[352 - 31];
            x1 = rk[352 - 30];
            x2 = rk[352 - 29];
            x3 = rk[352 - 32];
            y0 = Table0[x0 >> 24] ^ Table1[(x1 >> 16) & 0xff] ^ Table2[(x2 >> 8) & 0xff] ^ Table3[x3 & 0xff];
            y1 = Table0[x1 >> 24] ^ Table1[(x2 >> 16) & 0xff] ^ Table2[(x3 >> 8) & 0xff] ^ Table3[x0 & 0xff];
            y2 = Table0[x2 >> 24] ^ Table1[(x3 >> 16) & 0xff] ^ Table2[(x0 >> 8) & 0xff] ^ Table3[x1 & 0xff];
            y3 = Table0[x3 >> 24] ^ Table1[(x0 >> 16) & 0xff] ^ Table2[(x1 >> 8) & 0xff] ^ Table3[x2 & 0xff];
            rk[352] = y0 ^ rk[352 - 4];
            rk[352 + 1] = y1 ^ rk[352 - 3];
            rk[352 + 2] = y2 ^ rk[352 - 2];
            rk[352 + 3] = y3 ^ rk[352 - 1];
            x0 = rk[352 - 27];
            x1 = rk[352 - 26];
            x2 = rk[352 - 25];
            x3 = rk[352 - 28];
            y0 = Table0[x0 >> 24] ^ Table1[(x1 >> 16) & 0xff] ^ Table2[(x2 >> 8) & 0xff] ^ Table3[x3 & 0xff];
            y1 = Table0[x1 >> 24] ^ Table1[(x2 >> 16) & 0xff] ^ Table2[(x3 >> 8) & 0xff] ^ Table3[x0 & 0xff];
            y2 = Table0[x2 >> 24] ^ Table1[(x3 >> 16) & 0xff] ^ Table2[(x0 >> 8) & 0xff] ^ Table3[x1 & 0xff];
            y3 = Table0[x3 >> 24] ^ Table1[(x0 >> 16) & 0xff] ^ Table2[(x1 >> 8) & 0xff] ^ Table3[x2 & 0xff];
            rk[352 + 4] = y0 ^ rk[352];
            rk[352 + 5] = y1 ^ rk[352 + 1];
            rk[352 + 6] = y2 ^ rk[352 + 2];
            rk[352 + 7] = y3 ^ rk[352 + 3];
            x0 = rk[352 - 23];
            x1 = rk[352 - 22];
            x2 = rk[352 - 21];
            x3 = rk[352 - 24];
            y0 = Table0[x0 >> 24] ^ Table1[(x1 >> 16) & 0xff] ^ Table2[(x2 >> 8) & 0xff] ^ Table3[x3 & 0xff];
            y1 = Table0[x1 >> 24] ^ Table1[(x2 >> 16) & 0xff] ^ Table2[(x3 >> 8) & 0xff] ^ Table3[x0 & 0xff];
            y2 = Table0[x2 >> 24] ^ Table1[(x3 >> 16) & 0xff] ^ Table2[(x0 >> 8) & 0xff] ^ Table3[x1 & 0xff];
            y3 = Table0[x3 >> 24] ^ Table1[(x0 >> 16) & 0xff] ^ Table2[(x1 >> 8) & 0xff] ^ Table3[x2 & 0xff];
            rk[352 + 8] = y0 ^ rk[352 + 4];
            rk[352 + 9] = y1 ^ rk[352 + 5];
            rk[352 + 10] = y2 ^ rk[352 + 6];
            rk[352 + 11] = y3 ^ rk[352 + 7];
            x0 = rk[352 - 19];
            x1 = rk[352 - 18];
            x2 = rk[352 - 17];
            x3 = rk[352 - 20];
            y0 = Table0[x0 >> 24] ^ Table1[(x1 >> 16) & 0xff] ^ Table2[(x2 >> 8) & 0xff] ^ Table3[x3 & 0xff];
            y1 = Table0[x1 >> 24] ^ Table1[(x2 >> 16) & 0xff] ^ Table2[(x3 >> 8) & 0xff] ^ Table3[x0 & 0xff];
            y2 = Table0[x2 >> 24] ^ Table1[(x3 >> 16) & 0xff] ^ Table2[(x0 >> 8) & 0xff] ^ Table3[x1 & 0xff];
            y3 = Table0[x3 >> 24] ^ Table1[(x0 >> 16) & 0xff] ^ Table2[(x1 >> 8) & 0xff] ^ Table3[x2 & 0xff];
            rk[352 + 12] = y0 ^ rk[352 + 8];
            rk[352 + 13] = y1 ^ rk[352 + 9];
            rk[352 + 14] = y2 ^ rk[352 + 10];
            rk[352 + 15] = y3 ^ rk[352 + 11];

            x0 = rk[368 - 31];
            x1 = rk[368 - 30];
            x2 = rk[368 - 29];
            x3 = rk[368 - 32];
            y0 = Table0[x0 >> 24] ^ Table1[(x1 >> 16) & 0xff] ^ Table2[(x2 >> 8) & 0xff] ^ Table3[x3 & 0xff];
            y1 = Table0[x1 >> 24] ^ Table1[(x2 >> 16) & 0xff] ^ Table2[(x3 >> 8) & 0xff] ^ Table3[x0 & 0xff];
            y2 = Table0[x2 >> 24] ^ Table1[(x3 >> 16) & 0xff] ^ Table2[(x0 >> 8) & 0xff] ^ Table3[x1 & 0xff];
            y3 = Table0[x3 >> 24] ^ Table1[(x0 >> 16) & 0xff] ^ Table2[(x1 >> 8) & 0xff] ^ Table3[x2 & 0xff];
            rk[368] = y0 ^ rk[368 - 4];
            rk[368 + 1] = y1 ^ rk[368 - 3];
            rk[368 + 2] = y2 ^ rk[368 - 2];
            rk[368 + 3] = y3 ^ rk[368 - 1];
            x0 = rk[368 - 27];
            x1 = rk[368 - 26];
            x2 = rk[368 - 25];
            x3 = rk[368 - 28];
            y0 = Table0[x0 >> 24] ^ Table1[(x1 >> 16) & 0xff] ^ Table2[(x2 >> 8) & 0xff] ^ Table3[x3 & 0xff];
            y1 = Table0[x1 >> 24] ^ Table1[(x2 >> 16) & 0xff] ^ Table2[(x3 >> 8) & 0xff] ^ Table3[x0 & 0xff];
            y2 = Table0[x2 >> 24] ^ Table1[(x3 >> 16) & 0xff] ^ Table2[(x0 >> 8) & 0xff] ^ Table3[x1 & 0xff];
            y3 = Table0[x3 >> 24] ^ Table1[(x0 >> 16) & 0xff] ^ Table2[(x1 >> 8) & 0xff] ^ Table3[x2 & 0xff];
            rk[368 + 4] = y0 ^ rk[368];
            rk[368 + 5] = y1 ^ rk[368 + 1];
            rk[368 + 6] = y2 ^ rk[368 + 2];
            rk[368 + 7] = y3 ^ rk[368 + 3];
            x0 = rk[368 - 23];
            x1 = rk[368 - 22];
            x2 = rk[368 - 21];
            x3 = rk[368 - 24];
            y0 = Table0[x0 >> 24] ^ Table1[(x1 >> 16) & 0xff] ^ Table2[(x2 >> 8) & 0xff] ^ Table3[x3 & 0xff];
            y1 = Table0[x1 >> 24] ^ Table1[(x2 >> 16) & 0xff] ^ Table2[(x3 >> 8) & 0xff] ^ Table3[x0 & 0xff];
            y2 = Table0[x2 >> 24] ^ Table1[(x3 >> 16) & 0xff] ^ Table2[(x0 >> 8) & 0xff] ^ Table3[x1 & 0xff];
            y3 = Table0[x3 >> 24] ^ Table1[(x0 >> 16) & 0xff] ^ Table2[(x1 >> 8) & 0xff] ^ Table3[x2 & 0xff];
            rk[368 + 8] = y0 ^ rk[368 + 4];
            rk[368 + 9] = y1 ^ rk[368 + 5];
            rk[368 + 10] = y2 ^ rk[368 + 6];
            rk[368 + 11] = y3 ^ rk[368 + 7];
            x0 = rk[368 - 19];
            x1 = rk[368 - 18];
            x2 = rk[368 - 17];
            x3 = rk[368 - 20];
            y0 = Table0[x0 >> 24] ^ Table1[(x1 >> 16) & 0xff] ^ Table2[(x2 >> 8) & 0xff] ^ Table3[x3 & 0xff];
            y1 = Table0[x1 >> 24] ^ Table1[(x2 >> 16) & 0xff] ^ Table2[(x3 >> 8) & 0xff] ^ Table3[x0 & 0xff];
            y2 = Table0[x2 >> 24] ^ Table1[(x3 >> 16) & 0xff] ^ Table2[(x0 >> 8) & 0xff] ^ Table3[x1 & 0xff];
            y3 = Table0[x3 >> 24] ^ Table1[(x0 >> 16) & 0xff] ^ Table2[(x1 >> 8) & 0xff] ^ Table3[x2 & 0xff];
            rk[368 + 12] = y0 ^ rk[368 + 8];
            rk[368 + 13] = y1 ^ rk[368 + 9];
            rk[368 + 14] = y2 ^ rk[368 + 10];
            rk[368 + 15] = y3 ^ rk[368 + 11];

            for (int i = 0; i < 32; i++)
                rk[384 + i] = rk[i + 384 - 32] ^ rk[i + 384 - 7];

            x0 = rk[416 - 31];
            x1 = rk[416 - 30];
            x2 = rk[416 - 29];
            x3 = rk[416 - 32];
            y0 = Table0[x0 >> 24] ^ Table1[(x1 >> 16) & 0xff] ^ Table2[(x2 >> 8) & 0xff] ^ Table3[x3 & 0xff];
            y1 = Table0[x1 >> 24] ^ Table1[(x2 >> 16) & 0xff] ^ Table2[(x3 >> 8) & 0xff] ^ Table3[x0 & 0xff];
            y2 = Table0[x2 >> 24] ^ Table1[(x3 >> 16) & 0xff] ^ Table2[(x0 >> 8) & 0xff] ^ Table3[x1 & 0xff];
            y3 = Table0[x3 >> 24] ^ Table1[(x0 >> 16) & 0xff] ^ Table2[(x1 >> 8) & 0xff] ^ Table3[x2 & 0xff];
            rk[416] = y0 ^ rk[416 - 4];
            rk[416 + 1] = y1 ^ rk[416 - 3];
            rk[416 + 2] = y2 ^ rk[416 - 2];
            rk[416 + 3] = y3 ^ rk[416 - 1];
            x0 = rk[416 - 27];
            x1 = rk[416 - 26];
            x2 = rk[416 - 25];
            x3 = rk[416 - 28];
            y0 = Table0[x0 >> 24] ^ Table1[(x1 >> 16) & 0xff] ^ Table2[(x2 >> 8) & 0xff] ^ Table3[x3 & 0xff];
            y1 = Table0[x1 >> 24] ^ Table1[(x2 >> 16) & 0xff] ^ Table2[(x3 >> 8) & 0xff] ^ Table3[x0 & 0xff];
            y2 = Table0[x2 >> 24] ^ Table1[(x3 >> 16) & 0xff] ^ Table2[(x0 >> 8) & 0xff] ^ Table3[x1 & 0xff];
            y3 = Table0[x3 >> 24] ^ Table1[(x0 >> 16) & 0xff] ^ Table2[(x1 >> 8) & 0xff] ^ Table3[x2 & 0xff];
            rk[416 + 4] = y0 ^ rk[416];
            rk[416 + 5] = y1 ^ rk[416 + 1];
            rk[416 + 6] = y2 ^ rk[416 + 2];
            rk[416 + 7] = y3 ^ rk[416 + 3];
            x0 = rk[416 - 23];
            x1 = rk[416 - 22];
            x2 = rk[416 - 21];
            x3 = rk[416 - 24];
            y0 = Table0[x0 >> 24] ^ Table1[(x1 >> 16) & 0xff] ^ Table2[(x2 >> 8) & 0xff] ^ Table3[x3 & 0xff];
            y1 = Table0[x1 >> 24] ^ Table1[(x2 >> 16) & 0xff] ^ Table2[(x3 >> 8) & 0xff] ^ Table3[x0 & 0xff];
            y2 = Table0[x2 >> 24] ^ Table1[(x3 >> 16) & 0xff] ^ Table2[(x0 >> 8) & 0xff] ^ Table3[x1 & 0xff];
            y3 = Table0[x3 >> 24] ^ Table1[(x0 >> 16) & 0xff] ^ Table2[(x1 >> 8) & 0xff] ^ Table3[x2 & 0xff];
            rk[416 + 8] = y0 ^ rk[416 + 4];
            rk[416 + 9] = y1 ^ rk[416 + 5];
            rk[416 + 10] = y2 ^ rk[416 + 6];
            rk[416 + 11] = y3 ^ rk[416 + 7];
            x0 = rk[416 - 19];
            x1 = rk[416 - 18];
            x2 = rk[416 - 17];
            x3 = rk[416 - 20];
            y0 = Table0[x0 >> 24] ^ Table1[(x1 >> 16) & 0xff] ^ Table2[(x2 >> 8) & 0xff] ^ Table3[x3 & 0xff];
            y1 = Table0[x1 >> 24] ^ Table1[(x2 >> 16) & 0xff] ^ Table2[(x3 >> 8) & 0xff] ^ Table3[x0 & 0xff];
            y2 = Table0[x2 >> 24] ^ Table1[(x3 >> 16) & 0xff] ^ Table2[(x0 >> 8) & 0xff] ^ Table3[x1 & 0xff];
            y3 = Table0[x3 >> 24] ^ Table1[(x0 >> 16) & 0xff] ^ Table2[(x1 >> 8) & 0xff] ^ Table3[x2 & 0xff];
            rk[416 + 12] = y0 ^ rk[416 + 8];
            rk[416 + 13] = y1 ^ rk[416 + 9];
            rk[416 + 14] = y2 ^ rk[416 + 10];
            rk[416 + 15] = y3 ^ rk[416 + 11];

            x0 = rk[401];
            x1 = rk[402];
            x2 = rk[403];
            x3 = rk[400];
            y0 = Table0[x0 >> 24] ^ Table1[(x1 >> 16) & 0xff] ^ Table2[(x2 >> 8) & 0xff] ^ Table3[x3 & 0xff];
            y1 = Table0[x1 >> 24] ^ Table1[(x2 >> 16) & 0xff] ^ Table2[(x3 >> 8) & 0xff] ^ Table3[x0 & 0xff];
            y2 = Table0[x2 >> 24] ^ Table1[(x3 >> 16) & 0xff] ^ Table2[(x0 >> 8) & 0xff] ^ Table3[x1 & 0xff];
            y3 = Table0[x3 >> 24] ^ Table1[(x0 >> 16) & 0xff] ^ Table2[(x1 >> 8) & 0xff] ^ Table3[x2 & 0xff];
            rk[432] = y0 ^ rk[428];
            rk[433] = y1 ^ rk[429];
            rk[434] = y2 ^ rk[430];
            rk[435] = y3 ^ rk[431];
            x0 = rk[405];
            x1 = rk[406];
            x2 = rk[407];
            x3 = rk[404];
            y0 = Table0[x0 >> 24] ^ Table1[(x1 >> 16) & 0xff] ^ Table2[(x2 >> 8) & 0xff] ^ Table3[x3 & 0xff];
            y1 = Table0[x1 >> 24] ^ Table1[(x2 >> 16) & 0xff] ^ Table2[(x3 >> 8) & 0xff] ^ Table3[x0 & 0xff];
            y2 = Table0[x2 >> 24] ^ Table1[(x3 >> 16) & 0xff] ^ Table2[(x0 >> 8) & 0xff] ^ Table3[x1 & 0xff];
            y3 = Table0[x3 >> 24] ^ Table1[(x0 >> 16) & 0xff] ^ Table2[(x1 >> 8) & 0xff] ^ Table3[x2 & 0xff];
            rk[436] = y0 ^ rk[432];
            rk[437] = y1 ^ rk[433];
            rk[438] = y2 ^ rk[434];
            rk[439] = y3 ^ rk[435];
            x0 = rk[409];
            x1 = rk[410];
            x2 = rk[411];
            x3 = rk[408];
            y0 = Table0[x0 >> 24] ^ Table1[(x1 >> 16) & 0xff] ^ Table2[(x2 >> 8) & 0xff] ^ Table3[x3 & 0xff];
            y1 = Table0[x1 >> 24] ^ Table1[(x2 >> 16) & 0xff] ^ Table2[(x3 >> 8) & 0xff] ^ Table3[x0 & 0xff];
            y2 = Table0[x2 >> 24] ^ Table1[(x3 >> 16) & 0xff] ^ Table2[(x0 >> 8) & 0xff] ^ Table3[x1 & 0xff];
            y3 = Table0[x3 >> 24] ^ Table1[(x0 >> 16) & 0xff] ^ Table2[(x1 >> 8) & 0xff] ^ Table3[x2 & 0xff];
            rk[440] = y0 ^ rk[436] ^ cnt1;
            rk[441] = y1 ^ rk[437] ^ cnt0;
            rk[442] = y2 ^ rk[438];
            rk[443] = ~y3 ^ rk[439];
            x0 = rk[413];
            x1 = rk[414];
            x2 = rk[415];
            x3 = rk[412];
            y0 = Table0[x0 >> 24] ^ Table1[(x1 >> 16) & 0xff] ^ Table2[(x2 >> 8) & 0xff] ^ Table3[x3 & 0xff];
            y1 = Table0[x1 >> 24] ^ Table1[(x2 >> 16) & 0xff] ^ Table2[(x3 >> 8) & 0xff] ^ Table3[x0 & 0xff];
            y2 = Table0[x2 >> 24] ^ Table1[(x3 >> 16) & 0xff] ^ Table2[(x0 >> 8) & 0xff] ^ Table3[x1 & 0xff];
            y3 = Table0[x3 >> 24] ^ Table1[(x0 >> 16) & 0xff] ^ Table2[(x1 >> 8) & 0xff] ^ Table3[x2 & 0xff];
            rk[444] = y0 ^ rk[440];
            rk[445] = y1 ^ rk[441];
            rk[446] = y2 ^ rk[442];
            rk[447] = y3 ^ rk[443];

            x0 = state4 ^ rk[0];
            x1 = state5 ^ rk[0 + 1];
            x2 = state6 ^ rk[0 + 2];
            x3 = state7 ^ rk[0 + 3];
            y0 = Table0[x0 >> 24] ^ Table1[(x1 >> 16) & 0xff] ^ Table2[(x2 >> 8) & 0xff] ^ Table3[x3 & 0xff] ^ rk[0 + 4];
            y1 = Table0[x1 >> 24] ^ Table1[(x2 >> 16) & 0xff] ^ Table2[(x3 >> 8) & 0xff] ^ Table3[x0 & 0xff] ^ rk[0 + 5];
            y2 = Table0[x2 >> 24] ^ Table1[(x3 >> 16) & 0xff] ^ Table2[(x0 >> 8) & 0xff] ^ Table3[x1 & 0xff] ^ rk[0 + 6];
            y3 = Table0[x3 >> 24] ^ Table1[(x0 >> 16) & 0xff] ^ Table2[(x1 >> 8) & 0xff] ^ Table3[x2 & 0xff] ^ rk[0 + 7];
            x0 = Table0[y0 >> 24] ^ Table1[(y1 >> 16) & 0xff] ^ Table2[(y2 >> 8) & 0xff] ^ Table3[y3 & 0xff] ^ rk[0 + 8];
            x1 = Table0[y1 >> 24] ^ Table1[(y2 >> 16) & 0xff] ^ Table2[(y3 >> 8) & 0xff] ^ Table3[y0 & 0xff] ^ rk[0 + 9];
            x2 = Table0[y2 >> 24] ^ Table1[(y3 >> 16) & 0xff] ^ Table2[(y0 >> 8) & 0xff] ^ Table3[y1 & 0xff] ^ rk[0 + 10];
            x3 = Table0[y3 >> 24] ^ Table1[(y0 >> 16) & 0xff] ^ Table2[(y1 >> 8) & 0xff] ^ Table3[y2 & 0xff] ^ rk[0 + 11];
            y0 = Table0[x0 >> 24] ^ Table1[(x1 >> 16) & 0xff] ^ Table2[(x2 >> 8) & 0xff] ^ Table3[x3 & 0xff] ^ rk[0 + 12];
            y1 = Table0[x1 >> 24] ^ Table1[(x2 >> 16) & 0xff] ^ Table2[(x3 >> 8) & 0xff] ^ Table3[x0 & 0xff] ^ rk[0 + 13];
            y2 = Table0[x2 >> 24] ^ Table1[(x3 >> 16) & 0xff] ^ Table2[(x0 >> 8) & 0xff] ^ Table3[x1 & 0xff] ^ rk[0 + 14];
            y3 = Table0[x3 >> 24] ^ Table1[(x0 >> 16) & 0xff] ^ Table2[(x1 >> 8) & 0xff] ^ Table3[x2 & 0xff] ^ rk[0 + 15];
            x0 = Table0[y0 >> 24] ^ Table1[(y1 >> 16) & 0xff] ^ Table2[(y2 >> 8) & 0xff] ^ Table3[y3 & 0xff];
            x1 = Table0[y1 >> 24] ^ Table1[(y2 >> 16) & 0xff] ^ Table2[(y3 >> 8) & 0xff] ^ Table3[y0 & 0xff];
            x2 = Table0[y2 >> 24] ^ Table1[(y3 >> 16) & 0xff] ^ Table2[(y0 >> 8) & 0xff] ^ Table3[y1 & 0xff];
            x3 = Table0[y3 >> 24] ^ Table1[(y0 >> 16) & 0xff] ^ Table2[(y1 >> 8) & 0xff] ^ Table3[y2 & 0xff];
            state0 ^= x0;
            state1 ^= x1;
            state2 ^= x2;
            state3 ^= x3;
            x0 = state12 ^ rk[0 + 16];
            x1 = state13 ^ rk[0 + 17];
            x2 = state14 ^ rk[0 + 18];
            x3 = state15 ^ rk[0 + 19];
            y0 = Table0[x0 >> 24] ^ Table1[(x1 >> 16) & 0xff] ^ Table2[(x2 >> 8) & 0xff] ^ Table3[x3 & 0xff] ^ rk[0 + 20];
            y1 = Table0[x1 >> 24] ^ Table1[(x2 >> 16) & 0xff] ^ Table2[(x3 >> 8) & 0xff] ^ Table3[x0 & 0xff] ^ rk[0 + 21];
            y2 = Table0[x2 >> 24] ^ Table1[(x3 >> 16) & 0xff] ^ Table2[(x0 >> 8) & 0xff] ^ Table3[x1 & 0xff] ^ rk[0 + 22];
            y3 = Table0[x3 >> 24] ^ Table1[(x0 >> 16) & 0xff] ^ Table2[(x1 >> 8) & 0xff] ^ Table3[x2 & 0xff] ^ rk[0 + 23];
            x0 = Table0[y0 >> 24] ^ Table1[(y1 >> 16) & 0xff] ^ Table2[(y2 >> 8) & 0xff] ^ Table3[y3 & 0xff] ^ rk[0 + 24];
            x1 = Table0[y1 >> 24] ^ Table1[(y2 >> 16) & 0xff] ^ Table2[(y3 >> 8) & 0xff] ^ Table3[y0 & 0xff] ^ rk[0 + 25];
            x2 = Table0[y2 >> 24] ^ Table1[(y3 >> 16) & 0xff] ^ Table2[(y0 >> 8) & 0xff] ^ Table3[y1 & 0xff] ^ rk[0 + 26];
            x3 = Table0[y3 >> 24] ^ Table1[(y0 >> 16) & 0xff] ^ Table2[(y1 >> 8) & 0xff] ^ Table3[y2 & 0xff] ^ rk[0 + 27];
            y0 = Table0[x0 >> 24] ^ Table1[(x1 >> 16) & 0xff] ^ Table2[(x2 >> 8) & 0xff] ^ Table3[x3 & 0xff] ^ rk[0 + 28];
            y1 = Table0[x1 >> 24] ^ Table1[(x2 >> 16) & 0xff] ^ Table2[(x3 >> 8) & 0xff] ^ Table3[x0 & 0xff] ^ rk[0 + 29];
            y2 = Table0[x2 >> 24] ^ Table1[(x3 >> 16) & 0xff] ^ Table2[(x0 >> 8) & 0xff] ^ Table3[x1 & 0xff] ^ rk[0 + 30];
            y3 = Table0[x3 >> 24] ^ Table1[(x0 >> 16) & 0xff] ^ Table2[(x1 >> 8) & 0xff] ^ Table3[x2 & 0xff] ^ rk[0 + 31];
            x0 = Table0[y0 >> 24] ^ Table1[(y1 >> 16) & 0xff] ^ Table2[(y2 >> 8) & 0xff] ^ Table3[y3 & 0xff];
            x1 = Table0[y1 >> 24] ^ Table1[(y2 >> 16) & 0xff] ^ Table2[(y3 >> 8) & 0xff] ^ Table3[y0 & 0xff];
            x2 = Table0[y2 >> 24] ^ Table1[(y3 >> 16) & 0xff] ^ Table2[(y0 >> 8) & 0xff] ^ Table3[y1 & 0xff];
            x3 = Table0[y3 >> 24] ^ Table1[(y0 >> 16) & 0xff] ^ Table2[(y1 >> 8) & 0xff] ^ Table3[y2 & 0xff];
            state8 ^= x0;
            state9 ^= x1;
            state10 ^= x2;
            state11 ^= x3;

            x0 = state0 ^ rk[32];
            x1 = state1 ^ rk[32 + 1];
            x2 = state2 ^ rk[32 + 2];
            x3 = state3 ^ rk[32 + 3];
            y0 = Table0[x0 >> 24] ^ Table1[(x1 >> 16) & 0xff] ^ Table2[(x2 >> 8) & 0xff] ^ Table3[x3 & 0xff] ^ rk[32 + 4];
            y1 = Table0[x1 >> 24] ^ Table1[(x2 >> 16) & 0xff] ^ Table2[(x3 >> 8) & 0xff] ^ Table3[x0 & 0xff] ^ rk[32 + 5];
            y2 = Table0[x2 >> 24] ^ Table1[(x3 >> 16) & 0xff] ^ Table2[(x0 >> 8) & 0xff] ^ Table3[x1 & 0xff] ^ rk[32 + 6];
            y3 = Table0[x3 >> 24] ^ Table1[(x0 >> 16) & 0xff] ^ Table2[(x1 >> 8) & 0xff] ^ Table3[x2 & 0xff] ^ rk[32 + 7];
            x0 = Table0[y0 >> 24] ^ Table1[(y1 >> 16) & 0xff] ^ Table2[(y2 >> 8) & 0xff] ^ Table3[y3 & 0xff] ^ rk[32 + 8];
            x1 = Table0[y1 >> 24] ^ Table1[(y2 >> 16) & 0xff] ^ Table2[(y3 >> 8) & 0xff] ^ Table3[y0 & 0xff] ^ rk[32 + 9];
            x2 = Table0[y2 >> 24] ^ Table1[(y3 >> 16) & 0xff] ^ Table2[(y0 >> 8) & 0xff] ^ Table3[y1 & 0xff] ^ rk[32 + 10];
            x3 = Table0[y3 >> 24] ^ Table1[(y0 >> 16) & 0xff] ^ Table2[(y1 >> 8) & 0xff] ^ Table3[y2 & 0xff] ^ rk[32 + 11];
            y0 = Table0[x0 >> 24] ^ Table1[(x1 >> 16) & 0xff] ^ Table2[(x2 >> 8) & 0xff] ^ Table3[x3 & 0xff] ^ rk[32 + 12];
            y1 = Table0[x1 >> 24] ^ Table1[(x2 >> 16) & 0xff] ^ Table2[(x3 >> 8) & 0xff] ^ Table3[x0 & 0xff] ^ rk[32 + 13];
            y2 = Table0[x2 >> 24] ^ Table1[(x3 >> 16) & 0xff] ^ Table2[(x0 >> 8) & 0xff] ^ Table3[x1 & 0xff] ^ rk[32 + 14];
            y3 = Table0[x3 >> 24] ^ Table1[(x0 >> 16) & 0xff] ^ Table2[(x1 >> 8) & 0xff] ^ Table3[x2 & 0xff] ^ rk[32 + 15];
            x0 = Table0[y0 >> 24] ^ Table1[(y1 >> 16) & 0xff] ^ Table2[(y2 >> 8) & 0xff] ^ Table3[y3 & 0xff];
            x1 = Table0[y1 >> 24] ^ Table1[(y2 >> 16) & 0xff] ^ Table2[(y3 >> 8) & 0xff] ^ Table3[y0 & 0xff];
            x2 = Table0[y2 >> 24] ^ Table1[(y3 >> 16) & 0xff] ^ Table2[(y0 >> 8) & 0xff] ^ Table3[y1 & 0xff];
            x3 = Table0[y3 >> 24] ^ Table1[(y0 >> 16) & 0xff] ^ Table2[(y1 >> 8) & 0xff] ^ Table3[y2 & 0xff];
            state12 ^= x0;
            state13 ^= x1;
            state14 ^= x2;
            state15 ^= x3;
            x0 = state8 ^ rk[32 + 16];
            x1 = state9 ^ rk[32 + 17];
            x2 = state10 ^ rk[32 + 18];
            x3 = state11 ^ rk[32 + 19];
            y0 = Table0[x0 >> 24] ^ Table1[(x1 >> 16) & 0xff] ^ Table2[(x2 >> 8) & 0xff] ^ Table3[x3 & 0xff] ^ rk[32 + 20];
            y1 = Table0[x1 >> 24] ^ Table1[(x2 >> 16) & 0xff] ^ Table2[(x3 >> 8) & 0xff] ^ Table3[x0 & 0xff] ^ rk[32 + 21];
            y2 = Table0[x2 >> 24] ^ Table1[(x3 >> 16) & 0xff] ^ Table2[(x0 >> 8) & 0xff] ^ Table3[x1 & 0xff] ^ rk[32 + 22];
            y3 = Table0[x3 >> 24] ^ Table1[(x0 >> 16) & 0xff] ^ Table2[(x1 >> 8) & 0xff] ^ Table3[x2 & 0xff] ^ rk[32 + 23];
            x0 = Table0[y0 >> 24] ^ Table1[(y1 >> 16) & 0xff] ^ Table2[(y2 >> 8) & 0xff] ^ Table3[y3 & 0xff] ^ rk[32 + 24];
            x1 = Table0[y1 >> 24] ^ Table1[(y2 >> 16) & 0xff] ^ Table2[(y3 >> 8) & 0xff] ^ Table3[y0 & 0xff] ^ rk[32 + 25];
            x2 = Table0[y2 >> 24] ^ Table1[(y3 >> 16) & 0xff] ^ Table2[(y0 >> 8) & 0xff] ^ Table3[y1 & 0xff] ^ rk[32 + 26];
            x3 = Table0[y3 >> 24] ^ Table1[(y0 >> 16) & 0xff] ^ Table2[(y1 >> 8) & 0xff] ^ Table3[y2 & 0xff] ^ rk[32 + 27];
            y0 = Table0[x0 >> 24] ^ Table1[(x1 >> 16) & 0xff] ^ Table2[(x2 >> 8) & 0xff] ^ Table3[x3 & 0xff] ^ rk[32 + 28];
            y1 = Table0[x1 >> 24] ^ Table1[(x2 >> 16) & 0xff] ^ Table2[(x3 >> 8) & 0xff] ^ Table3[x0 & 0xff] ^ rk[32 + 29];
            y2 = Table0[x2 >> 24] ^ Table1[(x3 >> 16) & 0xff] ^ Table2[(x0 >> 8) & 0xff] ^ Table3[x1 & 0xff] ^ rk[32 + 30];
            y3 = Table0[x3 >> 24] ^ Table1[(x0 >> 16) & 0xff] ^ Table2[(x1 >> 8) & 0xff] ^ Table3[x2 & 0xff] ^ rk[32 + 31];
            x0 = Table0[y0 >> 24] ^ Table1[(y1 >> 16) & 0xff] ^ Table2[(y2 >> 8) & 0xff] ^ Table3[y3 & 0xff];
            x1 = Table0[y1 >> 24] ^ Table1[(y2 >> 16) & 0xff] ^ Table2[(y3 >> 8) & 0xff] ^ Table3[y0 & 0xff];
            x2 = Table0[y2 >> 24] ^ Table1[(y3 >> 16) & 0xff] ^ Table2[(y0 >> 8) & 0xff] ^ Table3[y1 & 0xff];
            x3 = Table0[y3 >> 24] ^ Table1[(y0 >> 16) & 0xff] ^ Table2[(y1 >> 8) & 0xff] ^ Table3[y2 & 0xff];
            state4 ^= x0;
            state5 ^= x1;
            state6 ^= x2;
            state7 ^= x3;

            x0 = state12 ^ rk[64];
            x1 = state13 ^ rk[64 + 1];
            x2 = state14 ^ rk[64 + 2];
            x3 = state15 ^ rk[64 + 3];
            y0 = Table0[x0 >> 24] ^ Table1[(x1 >> 16) & 0xff] ^ Table2[(x2 >> 8) & 0xff] ^ Table3[x3 & 0xff] ^ rk[64 + 4];
            y1 = Table0[x1 >> 24] ^ Table1[(x2 >> 16) & 0xff] ^ Table2[(x3 >> 8) & 0xff] ^ Table3[x0 & 0xff] ^ rk[64 + 5];
            y2 = Table0[x2 >> 24] ^ Table1[(x3 >> 16) & 0xff] ^ Table2[(x0 >> 8) & 0xff] ^ Table3[x1 & 0xff] ^ rk[64 + 6];
            y3 = Table0[x3 >> 24] ^ Table1[(x0 >> 16) & 0xff] ^ Table2[(x1 >> 8) & 0xff] ^ Table3[x2 & 0xff] ^ rk[64 + 7];
            x0 = Table0[y0 >> 24] ^ Table1[(y1 >> 16) & 0xff] ^ Table2[(y2 >> 8) & 0xff] ^ Table3[y3 & 0xff] ^ rk[64 + 8];
            x1 = Table0[y1 >> 24] ^ Table1[(y2 >> 16) & 0xff] ^ Table2[(y3 >> 8) & 0xff] ^ Table3[y0 & 0xff] ^ rk[64 + 9];
            x2 = Table0[y2 >> 24] ^ Table1[(y3 >> 16) & 0xff] ^ Table2[(y0 >> 8) & 0xff] ^ Table3[y1 & 0xff] ^ rk[64 + 10];
            x3 = Table0[y3 >> 24] ^ Table1[(y0 >> 16) & 0xff] ^ Table2[(y1 >> 8) & 0xff] ^ Table3[y2 & 0xff] ^ rk[64 + 11];
            y0 = Table0[x0 >> 24] ^ Table1[(x1 >> 16) & 0xff] ^ Table2[(x2 >> 8) & 0xff] ^ Table3[x3 & 0xff] ^ rk[64 + 12];
            y1 = Table0[x1 >> 24] ^ Table1[(x2 >> 16) & 0xff] ^ Table2[(x3 >> 8) & 0xff] ^ Table3[x0 & 0xff] ^ rk[64 + 13];
            y2 = Table0[x2 >> 24] ^ Table1[(x3 >> 16) & 0xff] ^ Table2[(x0 >> 8) & 0xff] ^ Table3[x1 & 0xff] ^ rk[64 + 14];
            y3 = Table0[x3 >> 24] ^ Table1[(x0 >> 16) & 0xff] ^ Table2[(x1 >> 8) & 0xff] ^ Table3[x2 & 0xff] ^ rk[64 + 15];
            x0 = Table0[y0 >> 24] ^ Table1[(y1 >> 16) & 0xff] ^ Table2[(y2 >> 8) & 0xff] ^ Table3[y3 & 0xff];
            x1 = Table0[y1 >> 24] ^ Table1[(y2 >> 16) & 0xff] ^ Table2[(y3 >> 8) & 0xff] ^ Table3[y0 & 0xff];
            x2 = Table0[y2 >> 24] ^ Table1[(y3 >> 16) & 0xff] ^ Table2[(y0 >> 8) & 0xff] ^ Table3[y1 & 0xff];
            x3 = Table0[y3 >> 24] ^ Table1[(y0 >> 16) & 0xff] ^ Table2[(y1 >> 8) & 0xff] ^ Table3[y2 & 0xff];
            state8 ^= x0;
            state9 ^= x1;
            state10 ^= x2;
            state11 ^= x3;
            x0 = state4 ^ rk[64 + 16];
            x1 = state5 ^ rk[64 + 17];
            x2 = state6 ^ rk[64 + 18];
            x3 = state7 ^ rk[64 + 19];
            y0 = Table0[x0 >> 24] ^ Table1[(x1 >> 16) & 0xff] ^ Table2[(x2 >> 8) & 0xff] ^ Table3[x3 & 0xff] ^ rk[64 + 20];
            y1 = Table0[x1 >> 24] ^ Table1[(x2 >> 16) & 0xff] ^ Table2[(x3 >> 8) & 0xff] ^ Table3[x0 & 0xff] ^ rk[64 + 21];
            y2 = Table0[x2 >> 24] ^ Table1[(x3 >> 16) & 0xff] ^ Table2[(x0 >> 8) & 0xff] ^ Table3[x1 & 0xff] ^ rk[64 + 22];
            y3 = Table0[x3 >> 24] ^ Table1[(x0 >> 16) & 0xff] ^ Table2[(x1 >> 8) & 0xff] ^ Table3[x2 & 0xff] ^ rk[64 + 23];
            x0 = Table0[y0 >> 24] ^ Table1[(y1 >> 16) & 0xff] ^ Table2[(y2 >> 8) & 0xff] ^ Table3[y3 & 0xff] ^ rk[64 + 24];
            x1 = Table0[y1 >> 24] ^ Table1[(y2 >> 16) & 0xff] ^ Table2[(y3 >> 8) & 0xff] ^ Table3[y0 & 0xff] ^ rk[64 + 25];
            x2 = Table0[y2 >> 24] ^ Table1[(y3 >> 16) & 0xff] ^ Table2[(y0 >> 8) & 0xff] ^ Table3[y1 & 0xff] ^ rk[64 + 26];
            x3 = Table0[y3 >> 24] ^ Table1[(y0 >> 16) & 0xff] ^ Table2[(y1 >> 8) & 0xff] ^ Table3[y2 & 0xff] ^ rk[64 + 27];
            y0 = Table0[x0 >> 24] ^ Table1[(x1 >> 16) & 0xff] ^ Table2[(x2 >> 8) & 0xff] ^ Table3[x3 & 0xff] ^ rk[64 + 28];
            y1 = Table0[x1 >> 24] ^ Table1[(x2 >> 16) & 0xff] ^ Table2[(x3 >> 8) & 0xff] ^ Table3[x0 & 0xff] ^ rk[64 + 29];
            y2 = Table0[x2 >> 24] ^ Table1[(x3 >> 16) & 0xff] ^ Table2[(x0 >> 8) & 0xff] ^ Table3[x1 & 0xff] ^ rk[64 + 30];
            y3 = Table0[x3 >> 24] ^ Table1[(x0 >> 16) & 0xff] ^ Table2[(x1 >> 8) & 0xff] ^ Table3[x2 & 0xff] ^ rk[64 + 31];
            x0 = Table0[y0 >> 24] ^ Table1[(y1 >> 16) & 0xff] ^ Table2[(y2 >> 8) & 0xff] ^ Table3[y3 & 0xff];
            x1 = Table0[y1 >> 24] ^ Table1[(y2 >> 16) & 0xff] ^ Table2[(y3 >> 8) & 0xff] ^ Table3[y0 & 0xff];
            x2 = Table0[y2 >> 24] ^ Table1[(y3 >> 16) & 0xff] ^ Table2[(y0 >> 8) & 0xff] ^ Table3[y1 & 0xff];
            x3 = Table0[y3 >> 24] ^ Table1[(y0 >> 16) & 0xff] ^ Table2[(y1 >> 8) & 0xff] ^ Table3[y2 & 0xff];
            state0 ^= x0;
            state1 ^= x1;
            state2 ^= x2;
            state3 ^= x3;

            x0 = state8 ^ rk[96];
            x1 = state9 ^ rk[96 + 1];
            x2 = state10 ^ rk[96 + 2];
            x3 = state11 ^ rk[96 + 3];
            y0 = Table0[x0 >> 24] ^ Table1[(x1 >> 16) & 0xff] ^ Table2[(x2 >> 8) & 0xff] ^ Table3[x3 & 0xff] ^ rk[96 + 4];
            y1 = Table0[x1 >> 24] ^ Table1[(x2 >> 16) & 0xff] ^ Table2[(x3 >> 8) & 0xff] ^ Table3[x0 & 0xff] ^ rk[96 + 5];
            y2 = Table0[x2 >> 24] ^ Table1[(x3 >> 16) & 0xff] ^ Table2[(x0 >> 8) & 0xff] ^ Table3[x1 & 0xff] ^ rk[96 + 6];
            y3 = Table0[x3 >> 24] ^ Table1[(x0 >> 16) & 0xff] ^ Table2[(x1 >> 8) & 0xff] ^ Table3[x2 & 0xff] ^ rk[96 + 7];
            x0 = Table0[y0 >> 24] ^ Table1[(y1 >> 16) & 0xff] ^ Table2[(y2 >> 8) & 0xff] ^ Table3[y3 & 0xff] ^ rk[96 + 8];
            x1 = Table0[y1 >> 24] ^ Table1[(y2 >> 16) & 0xff] ^ Table2[(y3 >> 8) & 0xff] ^ Table3[y0 & 0xff] ^ rk[96 + 9];
            x2 = Table0[y2 >> 24] ^ Table1[(y3 >> 16) & 0xff] ^ Table2[(y0 >> 8) & 0xff] ^ Table3[y1 & 0xff] ^ rk[96 + 10];
            x3 = Table0[y3 >> 24] ^ Table1[(y0 >> 16) & 0xff] ^ Table2[(y1 >> 8) & 0xff] ^ Table3[y2 & 0xff] ^ rk[96 + 11];
            y0 = Table0[x0 >> 24] ^ Table1[(x1 >> 16) & 0xff] ^ Table2[(x2 >> 8) & 0xff] ^ Table3[x3 & 0xff] ^ rk[96 + 12];
            y1 = Table0[x1 >> 24] ^ Table1[(x2 >> 16) & 0xff] ^ Table2[(x3 >> 8) & 0xff] ^ Table3[x0 & 0xff] ^ rk[96 + 13];
            y2 = Table0[x2 >> 24] ^ Table1[(x3 >> 16) & 0xff] ^ Table2[(x0 >> 8) & 0xff] ^ Table3[x1 & 0xff] ^ rk[96 + 14];
            y3 = Table0[x3 >> 24] ^ Table1[(x0 >> 16) & 0xff] ^ Table2[(x1 >> 8) & 0xff] ^ Table3[x2 & 0xff] ^ rk[96 + 15];
            x0 = Table0[y0 >> 24] ^ Table1[(y1 >> 16) & 0xff] ^ Table2[(y2 >> 8) & 0xff] ^ Table3[y3 & 0xff];
            x1 = Table0[y1 >> 24] ^ Table1[(y2 >> 16) & 0xff] ^ Table2[(y3 >> 8) & 0xff] ^ Table3[y0 & 0xff];
            x2 = Table0[y2 >> 24] ^ Table1[(y3 >> 16) & 0xff] ^ Table2[(y0 >> 8) & 0xff] ^ Table3[y1 & 0xff];
            x3 = Table0[y3 >> 24] ^ Table1[(y0 >> 16) & 0xff] ^ Table2[(y1 >> 8) & 0xff] ^ Table3[y2 & 0xff];
            state4 ^= x0;
            state5 ^= x1;
            state6 ^= x2;
            state7 ^= x3;
            x0 = state0 ^ rk[96 + 16];
            x1 = state1 ^ rk[96 + 17];
            x2 = state2 ^ rk[96 + 18];
            x3 = state3 ^ rk[96 + 19];
            y0 = Table0[x0 >> 24] ^ Table1[(x1 >> 16) & 0xff] ^ Table2[(x2 >> 8) & 0xff] ^ Table3[x3 & 0xff] ^ rk[96 + 20];
            y1 = Table0[x1 >> 24] ^ Table1[(x2 >> 16) & 0xff] ^ Table2[(x3 >> 8) & 0xff] ^ Table3[x0 & 0xff] ^ rk[96 + 21];
            y2 = Table0[x2 >> 24] ^ Table1[(x3 >> 16) & 0xff] ^ Table2[(x0 >> 8) & 0xff] ^ Table3[x1 & 0xff] ^ rk[96 + 22];
            y3 = Table0[x3 >> 24] ^ Table1[(x0 >> 16) & 0xff] ^ Table2[(x1 >> 8) & 0xff] ^ Table3[x2 & 0xff] ^ rk[96 + 23];
            x0 = Table0[y0 >> 24] ^ Table1[(y1 >> 16) & 0xff] ^ Table2[(y2 >> 8) & 0xff] ^ Table3[y3 & 0xff] ^ rk[96 + 24];
            x1 = Table0[y1 >> 24] ^ Table1[(y2 >> 16) & 0xff] ^ Table2[(y3 >> 8) & 0xff] ^ Table3[y0 & 0xff] ^ rk[96 + 25];
            x2 = Table0[y2 >> 24] ^ Table1[(y3 >> 16) & 0xff] ^ Table2[(y0 >> 8) & 0xff] ^ Table3[y1 & 0xff] ^ rk[96 + 26];
            x3 = Table0[y3 >> 24] ^ Table1[(y0 >> 16) & 0xff] ^ Table2[(y1 >> 8) & 0xff] ^ Table3[y2 & 0xff] ^ rk[96 + 27];
            y0 = Table0[x0 >> 24] ^ Table1[(x1 >> 16) & 0xff] ^ Table2[(x2 >> 8) & 0xff] ^ Table3[x3 & 0xff] ^ rk[96 + 28];
            y1 = Table0[x1 >> 24] ^ Table1[(x2 >> 16) & 0xff] ^ Table2[(x3 >> 8) & 0xff] ^ Table3[x0 & 0xff] ^ rk[96 + 29];
            y2 = Table0[x2 >> 24] ^ Table1[(x3 >> 16) & 0xff] ^ Table2[(x0 >> 8) & 0xff] ^ Table3[x1 & 0xff] ^ rk[96 + 30];
            y3 = Table0[x3 >> 24] ^ Table1[(x0 >> 16) & 0xff] ^ Table2[(x1 >> 8) & 0xff] ^ Table3[x2 & 0xff] ^ rk[96 + 31];
            x0 = Table0[y0 >> 24] ^ Table1[(y1 >> 16) & 0xff] ^ Table2[(y2 >> 8) & 0xff] ^ Table3[y3 & 0xff];
            x1 = Table0[y1 >> 24] ^ Table1[(y2 >> 16) & 0xff] ^ Table2[(y3 >> 8) & 0xff] ^ Table3[y0 & 0xff];
            x2 = Table0[y2 >> 24] ^ Table1[(y3 >> 16) & 0xff] ^ Table2[(y0 >> 8) & 0xff] ^ Table3[y1 & 0xff];
            x3 = Table0[y3 >> 24] ^ Table1[(y0 >> 16) & 0xff] ^ Table2[(y1 >> 8) & 0xff] ^ Table3[y2 & 0xff];
            state12 ^= x0;
            state13 ^= x1;
            state14 ^= x2;
            state15 ^= x3;

            x0 = state4 ^ rk[128];
            x1 = state5 ^ rk[128 + 1];
            x2 = state6 ^ rk[128 + 2];
            x3 = state7 ^ rk[128 + 3];
            y0 = Table0[x0 >> 24] ^ Table1[(x1 >> 16) & 0xff] ^ Table2[(x2 >> 8) & 0xff] ^ Table3[x3 & 0xff] ^ rk[128 + 4];
            y1 = Table0[x1 >> 24] ^ Table1[(x2 >> 16) & 0xff] ^ Table2[(x3 >> 8) & 0xff] ^ Table3[x0 & 0xff] ^ rk[128 + 5];
            y2 = Table0[x2 >> 24] ^ Table1[(x3 >> 16) & 0xff] ^ Table2[(x0 >> 8) & 0xff] ^ Table3[x1 & 0xff] ^ rk[128 + 6];
            y3 = Table0[x3 >> 24] ^ Table1[(x0 >> 16) & 0xff] ^ Table2[(x1 >> 8) & 0xff] ^ Table3[x2 & 0xff] ^ rk[128 + 7];
            x0 = Table0[y0 >> 24] ^ Table1[(y1 >> 16) & 0xff] ^ Table2[(y2 >> 8) & 0xff] ^ Table3[y3 & 0xff] ^ rk[128 + 8];
            x1 = Table0[y1 >> 24] ^ Table1[(y2 >> 16) & 0xff] ^ Table2[(y3 >> 8) & 0xff] ^ Table3[y0 & 0xff] ^ rk[128 + 9];
            x2 = Table0[y2 >> 24] ^ Table1[(y3 >> 16) & 0xff] ^ Table2[(y0 >> 8) & 0xff] ^ Table3[y1 & 0xff] ^ rk[128 + 10];
            x3 = Table0[y3 >> 24] ^ Table1[(y0 >> 16) & 0xff] ^ Table2[(y1 >> 8) & 0xff] ^ Table3[y2 & 0xff] ^ rk[128 + 11];
            y0 = Table0[x0 >> 24] ^ Table1[(x1 >> 16) & 0xff] ^ Table2[(x2 >> 8) & 0xff] ^ Table3[x3 & 0xff] ^ rk[128 + 12];
            y1 = Table0[x1 >> 24] ^ Table1[(x2 >> 16) & 0xff] ^ Table2[(x3 >> 8) & 0xff] ^ Table3[x0 & 0xff] ^ rk[128 + 13];
            y2 = Table0[x2 >> 24] ^ Table1[(x3 >> 16) & 0xff] ^ Table2[(x0 >> 8) & 0xff] ^ Table3[x1 & 0xff] ^ rk[128 + 14];
            y3 = Table0[x3 >> 24] ^ Table1[(x0 >> 16) & 0xff] ^ Table2[(x1 >> 8) & 0xff] ^ Table3[x2 & 0xff] ^ rk[128 + 15];
            x0 = Table0[y0 >> 24] ^ Table1[(y1 >> 16) & 0xff] ^ Table2[(y2 >> 8) & 0xff] ^ Table3[y3 & 0xff];
            x1 = Table0[y1 >> 24] ^ Table1[(y2 >> 16) & 0xff] ^ Table2[(y3 >> 8) & 0xff] ^ Table3[y0 & 0xff];
            x2 = Table0[y2 >> 24] ^ Table1[(y3 >> 16) & 0xff] ^ Table2[(y0 >> 8) & 0xff] ^ Table3[y1 & 0xff];
            x3 = Table0[y3 >> 24] ^ Table1[(y0 >> 16) & 0xff] ^ Table2[(y1 >> 8) & 0xff] ^ Table3[y2 & 0xff];
            state0 ^= x0;
            state1 ^= x1;
            state2 ^= x2;
            state3 ^= x3;
            x0 = state12 ^ rk[128 + 16];
            x1 = state13 ^ rk[128 + 17];
            x2 = state14 ^ rk[128 + 18];
            x3 = state15 ^ rk[128 + 19];
            y0 = Table0[x0 >> 24] ^ Table1[(x1 >> 16) & 0xff] ^ Table2[(x2 >> 8) & 0xff] ^ Table3[x3 & 0xff] ^ rk[128 + 20];
            y1 = Table0[x1 >> 24] ^ Table1[(x2 >> 16) & 0xff] ^ Table2[(x3 >> 8) & 0xff] ^ Table3[x0 & 0xff] ^ rk[128 + 21];
            y2 = Table0[x2 >> 24] ^ Table1[(x3 >> 16) & 0xff] ^ Table2[(x0 >> 8) & 0xff] ^ Table3[x1 & 0xff] ^ rk[128 + 22];
            y3 = Table0[x3 >> 24] ^ Table1[(x0 >> 16) & 0xff] ^ Table2[(x1 >> 8) & 0xff] ^ Table3[x2 & 0xff] ^ rk[128 + 23];
            x0 = Table0[y0 >> 24] ^ Table1[(y1 >> 16) & 0xff] ^ Table2[(y2 >> 8) & 0xff] ^ Table3[y3 & 0xff] ^ rk[128 + 24];
            x1 = Table0[y1 >> 24] ^ Table1[(y2 >> 16) & 0xff] ^ Table2[(y3 >> 8) & 0xff] ^ Table3[y0 & 0xff] ^ rk[128 + 25];
            x2 = Table0[y2 >> 24] ^ Table1[(y3 >> 16) & 0xff] ^ Table2[(y0 >> 8) & 0xff] ^ Table3[y1 & 0xff] ^ rk[128 + 26];
            x3 = Table0[y3 >> 24] ^ Table1[(y0 >> 16) & 0xff] ^ Table2[(y1 >> 8) & 0xff] ^ Table3[y2 & 0xff] ^ rk[128 + 27];
            y0 = Table0[x0 >> 24] ^ Table1[(x1 >> 16) & 0xff] ^ Table2[(x2 >> 8) & 0xff] ^ Table3[x3 & 0xff] ^ rk[128 + 28];
            y1 = Table0[x1 >> 24] ^ Table1[(x2 >> 16) & 0xff] ^ Table2[(x3 >> 8) & 0xff] ^ Table3[x0 & 0xff] ^ rk[128 + 29];
            y2 = Table0[x2 >> 24] ^ Table1[(x3 >> 16) & 0xff] ^ Table2[(x0 >> 8) & 0xff] ^ Table3[x1 & 0xff] ^ rk[128 + 30];
            y3 = Table0[x3 >> 24] ^ Table1[(x0 >> 16) & 0xff] ^ Table2[(x1 >> 8) & 0xff] ^ Table3[x2 & 0xff] ^ rk[128 + 31];
            x0 = Table0[y0 >> 24] ^ Table1[(y1 >> 16) & 0xff] ^ Table2[(y2 >> 8) & 0xff] ^ Table3[y3 & 0xff];
            x1 = Table0[y1 >> 24] ^ Table1[(y2 >> 16) & 0xff] ^ Table2[(y3 >> 8) & 0xff] ^ Table3[y0 & 0xff];
            x2 = Table0[y2 >> 24] ^ Table1[(y3 >> 16) & 0xff] ^ Table2[(y0 >> 8) & 0xff] ^ Table3[y1 & 0xff];
            x3 = Table0[y3 >> 24] ^ Table1[(y0 >> 16) & 0xff] ^ Table2[(y1 >> 8) & 0xff] ^ Table3[y2 & 0xff];
            state8 ^= x0;
            state9 ^= x1;
            state10 ^= x2;
            state11 ^= x3;

            x0 = state0 ^ rk[160];
            x1 = state1 ^ rk[160 + 1];
            x2 = state2 ^ rk[160 + 2];
            x3 = state3 ^ rk[160 + 3];
            y0 = Table0[x0 >> 24] ^ Table1[(x1 >> 16) & 0xff] ^ Table2[(x2 >> 8) & 0xff] ^ Table3[x3 & 0xff] ^ rk[160 + 4];
            y1 = Table0[x1 >> 24] ^ Table1[(x2 >> 16) & 0xff] ^ Table2[(x3 >> 8) & 0xff] ^ Table3[x0 & 0xff] ^ rk[160 + 5];
            y2 = Table0[x2 >> 24] ^ Table1[(x3 >> 16) & 0xff] ^ Table2[(x0 >> 8) & 0xff] ^ Table3[x1 & 0xff] ^ rk[160 + 6];
            y3 = Table0[x3 >> 24] ^ Table1[(x0 >> 16) & 0xff] ^ Table2[(x1 >> 8) & 0xff] ^ Table3[x2 & 0xff] ^ rk[160 + 7];
            x0 = Table0[y0 >> 24] ^ Table1[(y1 >> 16) & 0xff] ^ Table2[(y2 >> 8) & 0xff] ^ Table3[y3 & 0xff] ^ rk[160 + 8];
            x1 = Table0[y1 >> 24] ^ Table1[(y2 >> 16) & 0xff] ^ Table2[(y3 >> 8) & 0xff] ^ Table3[y0 & 0xff] ^ rk[160 + 9];
            x2 = Table0[y2 >> 24] ^ Table1[(y3 >> 16) & 0xff] ^ Table2[(y0 >> 8) & 0xff] ^ Table3[y1 & 0xff] ^ rk[160 + 10];
            x3 = Table0[y3 >> 24] ^ Table1[(y0 >> 16) & 0xff] ^ Table2[(y1 >> 8) & 0xff] ^ Table3[y2 & 0xff] ^ rk[160 + 11];
            y0 = Table0[x0 >> 24] ^ Table1[(x1 >> 16) & 0xff] ^ Table2[(x2 >> 8) & 0xff] ^ Table3[x3 & 0xff] ^ rk[160 + 12];
            y1 = Table0[x1 >> 24] ^ Table1[(x2 >> 16) & 0xff] ^ Table2[(x3 >> 8) & 0xff] ^ Table3[x0 & 0xff] ^ rk[160 + 13];
            y2 = Table0[x2 >> 24] ^ Table1[(x3 >> 16) & 0xff] ^ Table2[(x0 >> 8) & 0xff] ^ Table3[x1 & 0xff] ^ rk[160 + 14];
            y3 = Table0[x3 >> 24] ^ Table1[(x0 >> 16) & 0xff] ^ Table2[(x1 >> 8) & 0xff] ^ Table3[x2 & 0xff] ^ rk[160 + 15];
            x0 = Table0[y0 >> 24] ^ Table1[(y1 >> 16) & 0xff] ^ Table2[(y2 >> 8) & 0xff] ^ Table3[y3 & 0xff];
            x1 = Table0[y1 >> 24] ^ Table1[(y2 >> 16) & 0xff] ^ Table2[(y3 >> 8) & 0xff] ^ Table3[y0 & 0xff];
            x2 = Table0[y2 >> 24] ^ Table1[(y3 >> 16) & 0xff] ^ Table2[(y0 >> 8) & 0xff] ^ Table3[y1 & 0xff];
            x3 = Table0[y3 >> 24] ^ Table1[(y0 >> 16) & 0xff] ^ Table2[(y1 >> 8) & 0xff] ^ Table3[y2 & 0xff];
            state12 ^= x0;
            state13 ^= x1;
            state14 ^= x2;
            state15 ^= x3;
            x0 = state8 ^ rk[160 + 16];
            x1 = state9 ^ rk[160 + 17];
            x2 = state10 ^ rk[160 + 18];
            x3 = state11 ^ rk[160 + 19];
            y0 = Table0[x0 >> 24] ^ Table1[(x1 >> 16) & 0xff] ^ Table2[(x2 >> 8) & 0xff] ^ Table3[x3 & 0xff] ^ rk[160 + 20];
            y1 = Table0[x1 >> 24] ^ Table1[(x2 >> 16) & 0xff] ^ Table2[(x3 >> 8) & 0xff] ^ Table3[x0 & 0xff] ^ rk[160 + 21];
            y2 = Table0[x2 >> 24] ^ Table1[(x3 >> 16) & 0xff] ^ Table2[(x0 >> 8) & 0xff] ^ Table3[x1 & 0xff] ^ rk[160 + 22];
            y3 = Table0[x3 >> 24] ^ Table1[(x0 >> 16) & 0xff] ^ Table2[(x1 >> 8) & 0xff] ^ Table3[x2 & 0xff] ^ rk[160 + 23];
            x0 = Table0[y0 >> 24] ^ Table1[(y1 >> 16) & 0xff] ^ Table2[(y2 >> 8) & 0xff] ^ Table3[y3 & 0xff] ^ rk[160 + 24];
            x1 = Table0[y1 >> 24] ^ Table1[(y2 >> 16) & 0xff] ^ Table2[(y3 >> 8) & 0xff] ^ Table3[y0 & 0xff] ^ rk[160 + 25];
            x2 = Table0[y2 >> 24] ^ Table1[(y3 >> 16) & 0xff] ^ Table2[(y0 >> 8) & 0xff] ^ Table3[y1 & 0xff] ^ rk[160 + 26];
            x3 = Table0[y3 >> 24] ^ Table1[(y0 >> 16) & 0xff] ^ Table2[(y1 >> 8) & 0xff] ^ Table3[y2 & 0xff] ^ rk[160 + 27];
            y0 = Table0[x0 >> 24] ^ Table1[(x1 >> 16) & 0xff] ^ Table2[(x2 >> 8) & 0xff] ^ Table3[x3 & 0xff] ^ rk[160 + 28];
            y1 = Table0[x1 >> 24] ^ Table1[(x2 >> 16) & 0xff] ^ Table2[(x3 >> 8) & 0xff] ^ Table3[x0 & 0xff] ^ rk[160 + 29];
            y2 = Table0[x2 >> 24] ^ Table1[(x3 >> 16) & 0xff] ^ Table2[(x0 >> 8) & 0xff] ^ Table3[x1 & 0xff] ^ rk[160 + 30];
            y3 = Table0[x3 >> 24] ^ Table1[(x0 >> 16) & 0xff] ^ Table2[(x1 >> 8) & 0xff] ^ Table3[x2 & 0xff] ^ rk[160 + 31];
            x0 = Table0[y0 >> 24] ^ Table1[(y1 >> 16) & 0xff] ^ Table2[(y2 >> 8) & 0xff] ^ Table3[y3 & 0xff];
            x1 = Table0[y1 >> 24] ^ Table1[(y2 >> 16) & 0xff] ^ Table2[(y3 >> 8) & 0xff] ^ Table3[y0 & 0xff];
            x2 = Table0[y2 >> 24] ^ Table1[(y3 >> 16) & 0xff] ^ Table2[(y0 >> 8) & 0xff] ^ Table3[y1 & 0xff];
            x3 = Table0[y3 >> 24] ^ Table1[(y0 >> 16) & 0xff] ^ Table2[(y1 >> 8) & 0xff] ^ Table3[y2 & 0xff];
            state4 ^= x0;
            state5 ^= x1;
            state6 ^= x2;
            state7 ^= x3;

            x0 = state12 ^ rk[192];
            x1 = state13 ^ rk[192 + 1];
            x2 = state14 ^ rk[192 + 2];
            x3 = state15 ^ rk[192 + 3];
            y0 = Table0[x0 >> 24] ^ Table1[(x1 >> 16) & 0xff] ^ Table2[(x2 >> 8) & 0xff] ^ Table3[x3 & 0xff] ^ rk[192 + 4];
            y1 = Table0[x1 >> 24] ^ Table1[(x2 >> 16) & 0xff] ^ Table2[(x3 >> 8) & 0xff] ^ Table3[x0 & 0xff] ^ rk[192 + 5];
            y2 = Table0[x2 >> 24] ^ Table1[(x3 >> 16) & 0xff] ^ Table2[(x0 >> 8) & 0xff] ^ Table3[x1 & 0xff] ^ rk[192 + 6];
            y3 = Table0[x3 >> 24] ^ Table1[(x0 >> 16) & 0xff] ^ Table2[(x1 >> 8) & 0xff] ^ Table3[x2 & 0xff] ^ rk[192 + 7];
            x0 = Table0[y0 >> 24] ^ Table1[(y1 >> 16) & 0xff] ^ Table2[(y2 >> 8) & 0xff] ^ Table3[y3 & 0xff] ^ rk[192 + 8];
            x1 = Table0[y1 >> 24] ^ Table1[(y2 >> 16) & 0xff] ^ Table2[(y3 >> 8) & 0xff] ^ Table3[y0 & 0xff] ^ rk[192 + 9];
            x2 = Table0[y2 >> 24] ^ Table1[(y3 >> 16) & 0xff] ^ Table2[(y0 >> 8) & 0xff] ^ Table3[y1 & 0xff] ^ rk[192 + 10];
            x3 = Table0[y3 >> 24] ^ Table1[(y0 >> 16) & 0xff] ^ Table2[(y1 >> 8) & 0xff] ^ Table3[y2 & 0xff] ^ rk[192 + 11];
            y0 = Table0[x0 >> 24] ^ Table1[(x1 >> 16) & 0xff] ^ Table2[(x2 >> 8) & 0xff] ^ Table3[x3 & 0xff] ^ rk[192 + 12];
            y1 = Table0[x1 >> 24] ^ Table1[(x2 >> 16) & 0xff] ^ Table2[(x3 >> 8) & 0xff] ^ Table3[x0 & 0xff] ^ rk[192 + 13];
            y2 = Table0[x2 >> 24] ^ Table1[(x3 >> 16) & 0xff] ^ Table2[(x0 >> 8) & 0xff] ^ Table3[x1 & 0xff] ^ rk[192 + 14];
            y3 = Table0[x3 >> 24] ^ Table1[(x0 >> 16) & 0xff] ^ Table2[(x1 >> 8) & 0xff] ^ Table3[x2 & 0xff] ^ rk[192 + 15];
            x0 = Table0[y0 >> 24] ^ Table1[(y1 >> 16) & 0xff] ^ Table2[(y2 >> 8) & 0xff] ^ Table3[y3 & 0xff];
            x1 = Table0[y1 >> 24] ^ Table1[(y2 >> 16) & 0xff] ^ Table2[(y3 >> 8) & 0xff] ^ Table3[y0 & 0xff];
            x2 = Table0[y2 >> 24] ^ Table1[(y3 >> 16) & 0xff] ^ Table2[(y0 >> 8) & 0xff] ^ Table3[y1 & 0xff];
            x3 = Table0[y3 >> 24] ^ Table1[(y0 >> 16) & 0xff] ^ Table2[(y1 >> 8) & 0xff] ^ Table3[y2 & 0xff];
            state8 ^= x0;
            state9 ^= x1;
            state10 ^= x2;
            state11 ^= x3;
            x0 = state4 ^ rk[192 + 16];
            x1 = state5 ^ rk[192 + 17];
            x2 = state6 ^ rk[192 + 18];
            x3 = state7 ^ rk[192 + 19];
            y0 = Table0[x0 >> 24] ^ Table1[(x1 >> 16) & 0xff] ^ Table2[(x2 >> 8) & 0xff] ^ Table3[x3 & 0xff] ^ rk[192 + 20];
            y1 = Table0[x1 >> 24] ^ Table1[(x2 >> 16) & 0xff] ^ Table2[(x3 >> 8) & 0xff] ^ Table3[x0 & 0xff] ^ rk[192 + 21];
            y2 = Table0[x2 >> 24] ^ Table1[(x3 >> 16) & 0xff] ^ Table2[(x0 >> 8) & 0xff] ^ Table3[x1 & 0xff] ^ rk[192 + 22];
            y3 = Table0[x3 >> 24] ^ Table1[(x0 >> 16) & 0xff] ^ Table2[(x1 >> 8) & 0xff] ^ Table3[x2 & 0xff] ^ rk[192 + 23];
            x0 = Table0[y0 >> 24] ^ Table1[(y1 >> 16) & 0xff] ^ Table2[(y2 >> 8) & 0xff] ^ Table3[y3 & 0xff] ^ rk[192 + 24];
            x1 = Table0[y1 >> 24] ^ Table1[(y2 >> 16) & 0xff] ^ Table2[(y3 >> 8) & 0xff] ^ Table3[y0 & 0xff] ^ rk[192 + 25];
            x2 = Table0[y2 >> 24] ^ Table1[(y3 >> 16) & 0xff] ^ Table2[(y0 >> 8) & 0xff] ^ Table3[y1 & 0xff] ^ rk[192 + 26];
            x3 = Table0[y3 >> 24] ^ Table1[(y0 >> 16) & 0xff] ^ Table2[(y1 >> 8) & 0xff] ^ Table3[y2 & 0xff] ^ rk[192 + 27];
            y0 = Table0[x0 >> 24] ^ Table1[(x1 >> 16) & 0xff] ^ Table2[(x2 >> 8) & 0xff] ^ Table3[x3 & 0xff] ^ rk[192 + 28];
            y1 = Table0[x1 >> 24] ^ Table1[(x2 >> 16) & 0xff] ^ Table2[(x3 >> 8) & 0xff] ^ Table3[x0 & 0xff] ^ rk[192 + 29];
            y2 = Table0[x2 >> 24] ^ Table1[(x3 >> 16) & 0xff] ^ Table2[(x0 >> 8) & 0xff] ^ Table3[x1 & 0xff] ^ rk[192 + 30];
            y3 = Table0[x3 >> 24] ^ Table1[(x0 >> 16) & 0xff] ^ Table2[(x1 >> 8) & 0xff] ^ Table3[x2 & 0xff] ^ rk[192 + 31];
            x0 = Table0[y0 >> 24] ^ Table1[(y1 >> 16) & 0xff] ^ Table2[(y2 >> 8) & 0xff] ^ Table3[y3 & 0xff];
            x1 = Table0[y1 >> 24] ^ Table1[(y2 >> 16) & 0xff] ^ Table2[(y3 >> 8) & 0xff] ^ Table3[y0 & 0xff];
            x2 = Table0[y2 >> 24] ^ Table1[(y3 >> 16) & 0xff] ^ Table2[(y0 >> 8) & 0xff] ^ Table3[y1 & 0xff];
            x3 = Table0[y3 >> 24] ^ Table1[(y0 >> 16) & 0xff] ^ Table2[(y1 >> 8) & 0xff] ^ Table3[y2 & 0xff];
            state0 ^= x0;
            state1 ^= x1;
            state2 ^= x2;
            state3 ^= x3;

            x0 = state8 ^ rk[224];
            x1 = state9 ^ rk[224 + 1];
            x2 = state10 ^ rk[224 + 2];
            x3 = state11 ^ rk[224 + 3];
            y0 = Table0[x0 >> 24] ^ Table1[(x1 >> 16) & 0xff] ^ Table2[(x2 >> 8) & 0xff] ^ Table3[x3 & 0xff] ^ rk[224 + 4];
            y1 = Table0[x1 >> 24] ^ Table1[(x2 >> 16) & 0xff] ^ Table2[(x3 >> 8) & 0xff] ^ Table3[x0 & 0xff] ^ rk[224 + 5];
            y2 = Table0[x2 >> 24] ^ Table1[(x3 >> 16) & 0xff] ^ Table2[(x0 >> 8) & 0xff] ^ Table3[x1 & 0xff] ^ rk[224 + 6];
            y3 = Table0[x3 >> 24] ^ Table1[(x0 >> 16) & 0xff] ^ Table2[(x1 >> 8) & 0xff] ^ Table3[x2 & 0xff] ^ rk[224 + 7];
            x0 = Table0[y0 >> 24] ^ Table1[(y1 >> 16) & 0xff] ^ Table2[(y2 >> 8) & 0xff] ^ Table3[y3 & 0xff] ^ rk[224 + 8];
            x1 = Table0[y1 >> 24] ^ Table1[(y2 >> 16) & 0xff] ^ Table2[(y3 >> 8) & 0xff] ^ Table3[y0 & 0xff] ^ rk[224 + 9];
            x2 = Table0[y2 >> 24] ^ Table1[(y3 >> 16) & 0xff] ^ Table2[(y0 >> 8) & 0xff] ^ Table3[y1 & 0xff] ^ rk[224 + 10];
            x3 = Table0[y3 >> 24] ^ Table1[(y0 >> 16) & 0xff] ^ Table2[(y1 >> 8) & 0xff] ^ Table3[y2 & 0xff] ^ rk[224 + 11];
            y0 = Table0[x0 >> 24] ^ Table1[(x1 >> 16) & 0xff] ^ Table2[(x2 >> 8) & 0xff] ^ Table3[x3 & 0xff] ^ rk[224 + 12];
            y1 = Table0[x1 >> 24] ^ Table1[(x2 >> 16) & 0xff] ^ Table2[(x3 >> 8) & 0xff] ^ Table3[x0 & 0xff] ^ rk[224 + 13];
            y2 = Table0[x2 >> 24] ^ Table1[(x3 >> 16) & 0xff] ^ Table2[(x0 >> 8) & 0xff] ^ Table3[x1 & 0xff] ^ rk[224 + 14];
            y3 = Table0[x3 >> 24] ^ Table1[(x0 >> 16) & 0xff] ^ Table2[(x1 >> 8) & 0xff] ^ Table3[x2 & 0xff] ^ rk[224 + 15];
            x0 = Table0[y0 >> 24] ^ Table1[(y1 >> 16) & 0xff] ^ Table2[(y2 >> 8) & 0xff] ^ Table3[y3 & 0xff];
            x1 = Table0[y1 >> 24] ^ Table1[(y2 >> 16) & 0xff] ^ Table2[(y3 >> 8) & 0xff] ^ Table3[y0 & 0xff];
            x2 = Table0[y2 >> 24] ^ Table1[(y3 >> 16) & 0xff] ^ Table2[(y0 >> 8) & 0xff] ^ Table3[y1 & 0xff];
            x3 = Table0[y3 >> 24] ^ Table1[(y0 >> 16) & 0xff] ^ Table2[(y1 >> 8) & 0xff] ^ Table3[y2 & 0xff];
            state4 ^= x0;
            state5 ^= x1;
            state6 ^= x2;
            state7 ^= x3;
            x0 = state0 ^ rk[224 + 16];
            x1 = state1 ^ rk[224 + 17];
            x2 = state2 ^ rk[224 + 18];
            x3 = state3 ^ rk[224 + 19];
            y0 = Table0[x0 >> 24] ^ Table1[(x1 >> 16) & 0xff] ^ Table2[(x2 >> 8) & 0xff] ^ Table3[x3 & 0xff] ^ rk[224 + 20];
            y1 = Table0[x1 >> 24] ^ Table1[(x2 >> 16) & 0xff] ^ Table2[(x3 >> 8) & 0xff] ^ Table3[x0 & 0xff] ^ rk[224 + 21];
            y2 = Table0[x2 >> 24] ^ Table1[(x3 >> 16) & 0xff] ^ Table2[(x0 >> 8) & 0xff] ^ Table3[x1 & 0xff] ^ rk[224 + 22];
            y3 = Table0[x3 >> 24] ^ Table1[(x0 >> 16) & 0xff] ^ Table2[(x1 >> 8) & 0xff] ^ Table3[x2 & 0xff] ^ rk[224 + 23];
            x0 = Table0[y0 >> 24] ^ Table1[(y1 >> 16) & 0xff] ^ Table2[(y2 >> 8) & 0xff] ^ Table3[y3 & 0xff] ^ rk[224 + 24];
            x1 = Table0[y1 >> 24] ^ Table1[(y2 >> 16) & 0xff] ^ Table2[(y3 >> 8) & 0xff] ^ Table3[y0 & 0xff] ^ rk[224 + 25];
            x2 = Table0[y2 >> 24] ^ Table1[(y3 >> 16) & 0xff] ^ Table2[(y0 >> 8) & 0xff] ^ Table3[y1 & 0xff] ^ rk[224 + 26];
            x3 = Table0[y3 >> 24] ^ Table1[(y0 >> 16) & 0xff] ^ Table2[(y1 >> 8) & 0xff] ^ Table3[y2 & 0xff] ^ rk[224 + 27];
            y0 = Table0[x0 >> 24] ^ Table1[(x1 >> 16) & 0xff] ^ Table2[(x2 >> 8) & 0xff] ^ Table3[x3 & 0xff] ^ rk[224 + 28];
            y1 = Table0[x1 >> 24] ^ Table1[(x2 >> 16) & 0xff] ^ Table2[(x3 >> 8) & 0xff] ^ Table3[x0 & 0xff] ^ rk[224 + 29];
            y2 = Table0[x2 >> 24] ^ Table1[(x3 >> 16) & 0xff] ^ Table2[(x0 >> 8) & 0xff] ^ Table3[x1 & 0xff] ^ rk[224 + 30];
            y3 = Table0[x3 >> 24] ^ Table1[(x0 >> 16) & 0xff] ^ Table2[(x1 >> 8) & 0xff] ^ Table3[x2 & 0xff] ^ rk[224 + 31];
            x0 = Table0[y0 >> 24] ^ Table1[(y1 >> 16) & 0xff] ^ Table2[(y2 >> 8) & 0xff] ^ Table3[y3 & 0xff];
            x1 = Table0[y1 >> 24] ^ Table1[(y2 >> 16) & 0xff] ^ Table2[(y3 >> 8) & 0xff] ^ Table3[y0 & 0xff];
            x2 = Table0[y2 >> 24] ^ Table1[(y3 >> 16) & 0xff] ^ Table2[(y0 >> 8) & 0xff] ^ Table3[y1 & 0xff];
            x3 = Table0[y3 >> 24] ^ Table1[(y0 >> 16) & 0xff] ^ Table2[(y1 >> 8) & 0xff] ^ Table3[y2 & 0xff];
            state12 ^= x0;
            state13 ^= x1;
            state14 ^= x2;
            state15 ^= x3;

            x0 = state4 ^ rk[256];
            x1 = state5 ^ rk[256 + 1];
            x2 = state6 ^ rk[256 + 2];
            x3 = state7 ^ rk[256 + 3];
            y0 = Table0[x0 >> 24] ^ Table1[(x1 >> 16) & 0xff] ^ Table2[(x2 >> 8) & 0xff] ^ Table3[x3 & 0xff] ^ rk[256 + 4];
            y1 = Table0[x1 >> 24] ^ Table1[(x2 >> 16) & 0xff] ^ Table2[(x3 >> 8) & 0xff] ^ Table3[x0 & 0xff] ^ rk[256 + 5];
            y2 = Table0[x2 >> 24] ^ Table1[(x3 >> 16) & 0xff] ^ Table2[(x0 >> 8) & 0xff] ^ Table3[x1 & 0xff] ^ rk[256 + 6];
            y3 = Table0[x3 >> 24] ^ Table1[(x0 >> 16) & 0xff] ^ Table2[(x1 >> 8) & 0xff] ^ Table3[x2 & 0xff] ^ rk[256 + 7];
            x0 = Table0[y0 >> 24] ^ Table1[(y1 >> 16) & 0xff] ^ Table2[(y2 >> 8) & 0xff] ^ Table3[y3 & 0xff] ^ rk[256 + 8];
            x1 = Table0[y1 >> 24] ^ Table1[(y2 >> 16) & 0xff] ^ Table2[(y3 >> 8) & 0xff] ^ Table3[y0 & 0xff] ^ rk[256 + 9];
            x2 = Table0[y2 >> 24] ^ Table1[(y3 >> 16) & 0xff] ^ Table2[(y0 >> 8) & 0xff] ^ Table3[y1 & 0xff] ^ rk[256 + 10];
            x3 = Table0[y3 >> 24] ^ Table1[(y0 >> 16) & 0xff] ^ Table2[(y1 >> 8) & 0xff] ^ Table3[y2 & 0xff] ^ rk[256 + 11];
            y0 = Table0[x0 >> 24] ^ Table1[(x1 >> 16) & 0xff] ^ Table2[(x2 >> 8) & 0xff] ^ Table3[x3 & 0xff] ^ rk[256 + 12];
            y1 = Table0[x1 >> 24] ^ Table1[(x2 >> 16) & 0xff] ^ Table2[(x3 >> 8) & 0xff] ^ Table3[x0 & 0xff] ^ rk[256 + 13];
            y2 = Table0[x2 >> 24] ^ Table1[(x3 >> 16) & 0xff] ^ Table2[(x0 >> 8) & 0xff] ^ Table3[x1 & 0xff] ^ rk[256 + 14];
            y3 = Table0[x3 >> 24] ^ Table1[(x0 >> 16) & 0xff] ^ Table2[(x1 >> 8) & 0xff] ^ Table3[x2 & 0xff] ^ rk[256 + 15];
            x0 = Table0[y0 >> 24] ^ Table1[(y1 >> 16) & 0xff] ^ Table2[(y2 >> 8) & 0xff] ^ Table3[y3 & 0xff];
            x1 = Table0[y1 >> 24] ^ Table1[(y2 >> 16) & 0xff] ^ Table2[(y3 >> 8) & 0xff] ^ Table3[y0 & 0xff];
            x2 = Table0[y2 >> 24] ^ Table1[(y3 >> 16) & 0xff] ^ Table2[(y0 >> 8) & 0xff] ^ Table3[y1 & 0xff];
            x3 = Table0[y3 >> 24] ^ Table1[(y0 >> 16) & 0xff] ^ Table2[(y1 >> 8) & 0xff] ^ Table3[y2 & 0xff];
            state0 ^= x0;
            state1 ^= x1;
            state2 ^= x2;
            state3 ^= x3;
            x0 = state12 ^ rk[256 + 16];
            x1 = state13 ^ rk[256 + 17];
            x2 = state14 ^ rk[256 + 18];
            x3 = state15 ^ rk[256 + 19];
            y0 = Table0[x0 >> 24] ^ Table1[(x1 >> 16) & 0xff] ^ Table2[(x2 >> 8) & 0xff] ^ Table3[x3 & 0xff] ^ rk[256 + 20];
            y1 = Table0[x1 >> 24] ^ Table1[(x2 >> 16) & 0xff] ^ Table2[(x3 >> 8) & 0xff] ^ Table3[x0 & 0xff] ^ rk[256 + 21];
            y2 = Table0[x2 >> 24] ^ Table1[(x3 >> 16) & 0xff] ^ Table2[(x0 >> 8) & 0xff] ^ Table3[x1 & 0xff] ^ rk[256 + 22];
            y3 = Table0[x3 >> 24] ^ Table1[(x0 >> 16) & 0xff] ^ Table2[(x1 >> 8) & 0xff] ^ Table3[x2 & 0xff] ^ rk[256 + 23];
            x0 = Table0[y0 >> 24] ^ Table1[(y1 >> 16) & 0xff] ^ Table2[(y2 >> 8) & 0xff] ^ Table3[y3 & 0xff] ^ rk[256 + 24];
            x1 = Table0[y1 >> 24] ^ Table1[(y2 >> 16) & 0xff] ^ Table2[(y3 >> 8) & 0xff] ^ Table3[y0 & 0xff] ^ rk[256 + 25];
            x2 = Table0[y2 >> 24] ^ Table1[(y3 >> 16) & 0xff] ^ Table2[(y0 >> 8) & 0xff] ^ Table3[y1 & 0xff] ^ rk[256 + 26];
            x3 = Table0[y3 >> 24] ^ Table1[(y0 >> 16) & 0xff] ^ Table2[(y1 >> 8) & 0xff] ^ Table3[y2 & 0xff] ^ rk[256 + 27];
            y0 = Table0[x0 >> 24] ^ Table1[(x1 >> 16) & 0xff] ^ Table2[(x2 >> 8) & 0xff] ^ Table3[x3 & 0xff] ^ rk[256 + 28];
            y1 = Table0[x1 >> 24] ^ Table1[(x2 >> 16) & 0xff] ^ Table2[(x3 >> 8) & 0xff] ^ Table3[x0 & 0xff] ^ rk[256 + 29];
            y2 = Table0[x2 >> 24] ^ Table1[(x3 >> 16) & 0xff] ^ Table2[(x0 >> 8) & 0xff] ^ Table3[x1 & 0xff] ^ rk[256 + 30];
            y3 = Table0[x3 >> 24] ^ Table1[(x0 >> 16) & 0xff] ^ Table2[(x1 >> 8) & 0xff] ^ Table3[x2 & 0xff] ^ rk[256 + 31];
            x0 = Table0[y0 >> 24] ^ Table1[(y1 >> 16) & 0xff] ^ Table2[(y2 >> 8) & 0xff] ^ Table3[y3 & 0xff];
            x1 = Table0[y1 >> 24] ^ Table1[(y2 >> 16) & 0xff] ^ Table2[(y3 >> 8) & 0xff] ^ Table3[y0 & 0xff];
            x2 = Table0[y2 >> 24] ^ Table1[(y3 >> 16) & 0xff] ^ Table2[(y0 >> 8) & 0xff] ^ Table3[y1 & 0xff];
            x3 = Table0[y3 >> 24] ^ Table1[(y0 >> 16) & 0xff] ^ Table2[(y1 >> 8) & 0xff] ^ Table3[y2 & 0xff];
            state8 ^= x0;
            state9 ^= x1;
            state10 ^= x2;
            state11 ^= x3;

            x0 = state0 ^ rk[288];
            x1 = state1 ^ rk[288 + 1];
            x2 = state2 ^ rk[288 + 2];
            x3 = state3 ^ rk[288 + 3];
            y0 = Table0[x0 >> 24] ^ Table1[(x1 >> 16) & 0xff] ^ Table2[(x2 >> 8) & 0xff] ^ Table3[x3 & 0xff] ^ rk[288 + 4];
            y1 = Table0[x1 >> 24] ^ Table1[(x2 >> 16) & 0xff] ^ Table2[(x3 >> 8) & 0xff] ^ Table3[x0 & 0xff] ^ rk[288 + 5];
            y2 = Table0[x2 >> 24] ^ Table1[(x3 >> 16) & 0xff] ^ Table2[(x0 >> 8) & 0xff] ^ Table3[x1 & 0xff] ^ rk[288 + 6];
            y3 = Table0[x3 >> 24] ^ Table1[(x0 >> 16) & 0xff] ^ Table2[(x1 >> 8) & 0xff] ^ Table3[x2 & 0xff] ^ rk[288 + 7];
            x0 = Table0[y0 >> 24] ^ Table1[(y1 >> 16) & 0xff] ^ Table2[(y2 >> 8) & 0xff] ^ Table3[y3 & 0xff] ^ rk[288 + 8];
            x1 = Table0[y1 >> 24] ^ Table1[(y2 >> 16) & 0xff] ^ Table2[(y3 >> 8) & 0xff] ^ Table3[y0 & 0xff] ^ rk[288 + 9];
            x2 = Table0[y2 >> 24] ^ Table1[(y3 >> 16) & 0xff] ^ Table2[(y0 >> 8) & 0xff] ^ Table3[y1 & 0xff] ^ rk[288 + 10];
            x3 = Table0[y3 >> 24] ^ Table1[(y0 >> 16) & 0xff] ^ Table2[(y1 >> 8) & 0xff] ^ Table3[y2 & 0xff] ^ rk[288 + 11];
            y0 = Table0[x0 >> 24] ^ Table1[(x1 >> 16) & 0xff] ^ Table2[(x2 >> 8) & 0xff] ^ Table3[x3 & 0xff] ^ rk[288 + 12];
            y1 = Table0[x1 >> 24] ^ Table1[(x2 >> 16) & 0xff] ^ Table2[(x3 >> 8) & 0xff] ^ Table3[x0 & 0xff] ^ rk[288 + 13];
            y2 = Table0[x2 >> 24] ^ Table1[(x3 >> 16) & 0xff] ^ Table2[(x0 >> 8) & 0xff] ^ Table3[x1 & 0xff] ^ rk[288 + 14];
            y3 = Table0[x3 >> 24] ^ Table1[(x0 >> 16) & 0xff] ^ Table2[(x1 >> 8) & 0xff] ^ Table3[x2 & 0xff] ^ rk[288 + 15];
            x0 = Table0[y0 >> 24] ^ Table1[(y1 >> 16) & 0xff] ^ Table2[(y2 >> 8) & 0xff] ^ Table3[y3 & 0xff];
            x1 = Table0[y1 >> 24] ^ Table1[(y2 >> 16) & 0xff] ^ Table2[(y3 >> 8) & 0xff] ^ Table3[y0 & 0xff];
            x2 = Table0[y2 >> 24] ^ Table1[(y3 >> 16) & 0xff] ^ Table2[(y0 >> 8) & 0xff] ^ Table3[y1 & 0xff];
            x3 = Table0[y3 >> 24] ^ Table1[(y0 >> 16) & 0xff] ^ Table2[(y1 >> 8) & 0xff] ^ Table3[y2 & 0xff];
            state12 ^= x0;
            state13 ^= x1;
            state14 ^= x2;
            state15 ^= x3;
            x0 = state8 ^ rk[288 + 16];
            x1 = state9 ^ rk[288 + 17];
            x2 = state10 ^ rk[288 + 18];
            x3 = state11 ^ rk[288 + 19];
            y0 = Table0[x0 >> 24] ^ Table1[(x1 >> 16) & 0xff] ^ Table2[(x2 >> 8) & 0xff] ^ Table3[x3 & 0xff] ^ rk[288 + 20];
            y1 = Table0[x1 >> 24] ^ Table1[(x2 >> 16) & 0xff] ^ Table2[(x3 >> 8) & 0xff] ^ Table3[x0 & 0xff] ^ rk[288 + 21];
            y2 = Table0[x2 >> 24] ^ Table1[(x3 >> 16) & 0xff] ^ Table2[(x0 >> 8) & 0xff] ^ Table3[x1 & 0xff] ^ rk[288 + 22];
            y3 = Table0[x3 >> 24] ^ Table1[(x0 >> 16) & 0xff] ^ Table2[(x1 >> 8) & 0xff] ^ Table3[x2 & 0xff] ^ rk[288 + 23];
            x0 = Table0[y0 >> 24] ^ Table1[(y1 >> 16) & 0xff] ^ Table2[(y2 >> 8) & 0xff] ^ Table3[y3 & 0xff] ^ rk[288 + 24];
            x1 = Table0[y1 >> 24] ^ Table1[(y2 >> 16) & 0xff] ^ Table2[(y3 >> 8) & 0xff] ^ Table3[y0 & 0xff] ^ rk[288 + 25];
            x2 = Table0[y2 >> 24] ^ Table1[(y3 >> 16) & 0xff] ^ Table2[(y0 >> 8) & 0xff] ^ Table3[y1 & 0xff] ^ rk[288 + 26];
            x3 = Table0[y3 >> 24] ^ Table1[(y0 >> 16) & 0xff] ^ Table2[(y1 >> 8) & 0xff] ^ Table3[y2 & 0xff] ^ rk[288 + 27];
            y0 = Table0[x0 >> 24] ^ Table1[(x1 >> 16) & 0xff] ^ Table2[(x2 >> 8) & 0xff] ^ Table3[x3 & 0xff] ^ rk[288 + 28];
            y1 = Table0[x1 >> 24] ^ Table1[(x2 >> 16) & 0xff] ^ Table2[(x3 >> 8) & 0xff] ^ Table3[x0 & 0xff] ^ rk[288 + 29];
            y2 = Table0[x2 >> 24] ^ Table1[(x3 >> 16) & 0xff] ^ Table2[(x0 >> 8) & 0xff] ^ Table3[x1 & 0xff] ^ rk[288 + 30];
            y3 = Table0[x3 >> 24] ^ Table1[(x0 >> 16) & 0xff] ^ Table2[(x1 >> 8) & 0xff] ^ Table3[x2 & 0xff] ^ rk[288 + 31];
            x0 = Table0[y0 >> 24] ^ Table1[(y1 >> 16) & 0xff] ^ Table2[(y2 >> 8) & 0xff] ^ Table3[y3 & 0xff];
            x1 = Table0[y1 >> 24] ^ Table1[(y2 >> 16) & 0xff] ^ Table2[(y3 >> 8) & 0xff] ^ Table3[y0 & 0xff];
            x2 = Table0[y2 >> 24] ^ Table1[(y3 >> 16) & 0xff] ^ Table2[(y0 >> 8) & 0xff] ^ Table3[y1 & 0xff];
            x3 = Table0[y3 >> 24] ^ Table1[(y0 >> 16) & 0xff] ^ Table2[(y1 >> 8) & 0xff] ^ Table3[y2 & 0xff];
            state4 ^= x0;
            state5 ^= x1;
            state6 ^= x2;
            state7 ^= x3;

            x0 = state12 ^ rk[320];
            x1 = state13 ^ rk[320 + 1];
            x2 = state14 ^ rk[320 + 2];
            x3 = state15 ^ rk[320 + 3];
            y0 = Table0[x0 >> 24] ^ Table1[(x1 >> 16) & 0xff] ^ Table2[(x2 >> 8) & 0xff] ^ Table3[x3 & 0xff] ^ rk[320 + 4];
            y1 = Table0[x1 >> 24] ^ Table1[(x2 >> 16) & 0xff] ^ Table2[(x3 >> 8) & 0xff] ^ Table3[x0 & 0xff] ^ rk[320 + 5];
            y2 = Table0[x2 >> 24] ^ Table1[(x3 >> 16) & 0xff] ^ Table2[(x0 >> 8) & 0xff] ^ Table3[x1 & 0xff] ^ rk[320 + 6];
            y3 = Table0[x3 >> 24] ^ Table1[(x0 >> 16) & 0xff] ^ Table2[(x1 >> 8) & 0xff] ^ Table3[x2 & 0xff] ^ rk[320 + 7];
            x0 = Table0[y0 >> 24] ^ Table1[(y1 >> 16) & 0xff] ^ Table2[(y2 >> 8) & 0xff] ^ Table3[y3 & 0xff] ^ rk[320 + 8];
            x1 = Table0[y1 >> 24] ^ Table1[(y2 >> 16) & 0xff] ^ Table2[(y3 >> 8) & 0xff] ^ Table3[y0 & 0xff] ^ rk[320 + 9];
            x2 = Table0[y2 >> 24] ^ Table1[(y3 >> 16) & 0xff] ^ Table2[(y0 >> 8) & 0xff] ^ Table3[y1 & 0xff] ^ rk[320 + 10];
            x3 = Table0[y3 >> 24] ^ Table1[(y0 >> 16) & 0xff] ^ Table2[(y1 >> 8) & 0xff] ^ Table3[y2 & 0xff] ^ rk[320 + 11];
            y0 = Table0[x0 >> 24] ^ Table1[(x1 >> 16) & 0xff] ^ Table2[(x2 >> 8) & 0xff] ^ Table3[x3 & 0xff] ^ rk[320 + 12];
            y1 = Table0[x1 >> 24] ^ Table1[(x2 >> 16) & 0xff] ^ Table2[(x3 >> 8) & 0xff] ^ Table3[x0 & 0xff] ^ rk[320 + 13];
            y2 = Table0[x2 >> 24] ^ Table1[(x3 >> 16) & 0xff] ^ Table2[(x0 >> 8) & 0xff] ^ Table3[x1 & 0xff] ^ rk[320 + 14];
            y3 = Table0[x3 >> 24] ^ Table1[(x0 >> 16) & 0xff] ^ Table2[(x1 >> 8) & 0xff] ^ Table3[x2 & 0xff] ^ rk[320 + 15];
            x0 = Table0[y0 >> 24] ^ Table1[(y1 >> 16) & 0xff] ^ Table2[(y2 >> 8) & 0xff] ^ Table3[y3 & 0xff];
            x1 = Table0[y1 >> 24] ^ Table1[(y2 >> 16) & 0xff] ^ Table2[(y3 >> 8) & 0xff] ^ Table3[y0 & 0xff];
            x2 = Table0[y2 >> 24] ^ Table1[(y3 >> 16) & 0xff] ^ Table2[(y0 >> 8) & 0xff] ^ Table3[y1 & 0xff];
            x3 = Table0[y3 >> 24] ^ Table1[(y0 >> 16) & 0xff] ^ Table2[(y1 >> 8) & 0xff] ^ Table3[y2 & 0xff];
            state8 ^= x0;
            state9 ^= x1;
            state10 ^= x2;
            state11 ^= x3;
            x0 = state4 ^ rk[320 + 16];
            x1 = state5 ^ rk[320 + 17];
            x2 = state6 ^ rk[320 + 18];
            x3 = state7 ^ rk[320 + 19];
            y0 = Table0[x0 >> 24] ^ Table1[(x1 >> 16) & 0xff] ^ Table2[(x2 >> 8) & 0xff] ^ Table3[x3 & 0xff] ^ rk[320 + 20];
            y1 = Table0[x1 >> 24] ^ Table1[(x2 >> 16) & 0xff] ^ Table2[(x3 >> 8) & 0xff] ^ Table3[x0 & 0xff] ^ rk[320 + 21];
            y2 = Table0[x2 >> 24] ^ Table1[(x3 >> 16) & 0xff] ^ Table2[(x0 >> 8) & 0xff] ^ Table3[x1 & 0xff] ^ rk[320 + 22];
            y3 = Table0[x3 >> 24] ^ Table1[(x0 >> 16) & 0xff] ^ Table2[(x1 >> 8) & 0xff] ^ Table3[x2 & 0xff] ^ rk[320 + 23];
            x0 = Table0[y0 >> 24] ^ Table1[(y1 >> 16) & 0xff] ^ Table2[(y2 >> 8) & 0xff] ^ Table3[y3 & 0xff] ^ rk[320 + 24];
            x1 = Table0[y1 >> 24] ^ Table1[(y2 >> 16) & 0xff] ^ Table2[(y3 >> 8) & 0xff] ^ Table3[y0 & 0xff] ^ rk[320 + 25];
            x2 = Table0[y2 >> 24] ^ Table1[(y3 >> 16) & 0xff] ^ Table2[(y0 >> 8) & 0xff] ^ Table3[y1 & 0xff] ^ rk[320 + 26];
            x3 = Table0[y3 >> 24] ^ Table1[(y0 >> 16) & 0xff] ^ Table2[(y1 >> 8) & 0xff] ^ Table3[y2 & 0xff] ^ rk[320 + 27];
            y0 = Table0[x0 >> 24] ^ Table1[(x1 >> 16) & 0xff] ^ Table2[(x2 >> 8) & 0xff] ^ Table3[x3 & 0xff] ^ rk[320 + 28];
            y1 = Table0[x1 >> 24] ^ Table1[(x2 >> 16) & 0xff] ^ Table2[(x3 >> 8) & 0xff] ^ Table3[x0 & 0xff] ^ rk[320 + 29];
            y2 = Table0[x2 >> 24] ^ Table1[(x3 >> 16) & 0xff] ^ Table2[(x0 >> 8) & 0xff] ^ Table3[x1 & 0xff] ^ rk[320 + 30];
            y3 = Table0[x3 >> 24] ^ Table1[(x0 >> 16) & 0xff] ^ Table2[(x1 >> 8) & 0xff] ^ Table3[x2 & 0xff] ^ rk[320 + 31];
            x0 = Table0[y0 >> 24] ^ Table1[(y1 >> 16) & 0xff] ^ Table2[(y2 >> 8) & 0xff] ^ Table3[y3 & 0xff];
            x1 = Table0[y1 >> 24] ^ Table1[(y2 >> 16) & 0xff] ^ Table2[(y3 >> 8) & 0xff] ^ Table3[y0 & 0xff];
            x2 = Table0[y2 >> 24] ^ Table1[(y3 >> 16) & 0xff] ^ Table2[(y0 >> 8) & 0xff] ^ Table3[y1 & 0xff];
            x3 = Table0[y3 >> 24] ^ Table1[(y0 >> 16) & 0xff] ^ Table2[(y1 >> 8) & 0xff] ^ Table3[y2 & 0xff];
            state0 ^= x0;
            state1 ^= x1;
            state2 ^= x2;
            state3 ^= x3;

            x0 = state8 ^ rk[352];
            x1 = state9 ^ rk[352 + 1];
            x2 = state10 ^ rk[352 + 2];
            x3 = state11 ^ rk[352 + 3];
            y0 = Table0[x0 >> 24] ^ Table1[(x1 >> 16) & 0xff] ^ Table2[(x2 >> 8) & 0xff] ^ Table3[x3 & 0xff] ^ rk[352 + 4];
            y1 = Table0[x1 >> 24] ^ Table1[(x2 >> 16) & 0xff] ^ Table2[(x3 >> 8) & 0xff] ^ Table3[x0 & 0xff] ^ rk[352 + 5];
            y2 = Table0[x2 >> 24] ^ Table1[(x3 >> 16) & 0xff] ^ Table2[(x0 >> 8) & 0xff] ^ Table3[x1 & 0xff] ^ rk[352 + 6];
            y3 = Table0[x3 >> 24] ^ Table1[(x0 >> 16) & 0xff] ^ Table2[(x1 >> 8) & 0xff] ^ Table3[x2 & 0xff] ^ rk[352 + 7];
            x0 = Table0[y0 >> 24] ^ Table1[(y1 >> 16) & 0xff] ^ Table2[(y2 >> 8) & 0xff] ^ Table3[y3 & 0xff] ^ rk[352 + 8];
            x1 = Table0[y1 >> 24] ^ Table1[(y2 >> 16) & 0xff] ^ Table2[(y3 >> 8) & 0xff] ^ Table3[y0 & 0xff] ^ rk[352 + 9];
            x2 = Table0[y2 >> 24] ^ Table1[(y3 >> 16) & 0xff] ^ Table2[(y0 >> 8) & 0xff] ^ Table3[y1 & 0xff] ^ rk[352 + 10];
            x3 = Table0[y3 >> 24] ^ Table1[(y0 >> 16) & 0xff] ^ Table2[(y1 >> 8) & 0xff] ^ Table3[y2 & 0xff] ^ rk[352 + 11];
            y0 = Table0[x0 >> 24] ^ Table1[(x1 >> 16) & 0xff] ^ Table2[(x2 >> 8) & 0xff] ^ Table3[x3 & 0xff] ^ rk[352 + 12];
            y1 = Table0[x1 >> 24] ^ Table1[(x2 >> 16) & 0xff] ^ Table2[(x3 >> 8) & 0xff] ^ Table3[x0 & 0xff] ^ rk[352 + 13];
            y2 = Table0[x2 >> 24] ^ Table1[(x3 >> 16) & 0xff] ^ Table2[(x0 >> 8) & 0xff] ^ Table3[x1 & 0xff] ^ rk[352 + 14];
            y3 = Table0[x3 >> 24] ^ Table1[(x0 >> 16) & 0xff] ^ Table2[(x1 >> 8) & 0xff] ^ Table3[x2 & 0xff] ^ rk[352 + 15];
            x0 = Table0[y0 >> 24] ^ Table1[(y1 >> 16) & 0xff] ^ Table2[(y2 >> 8) & 0xff] ^ Table3[y3 & 0xff];
            x1 = Table0[y1 >> 24] ^ Table1[(y2 >> 16) & 0xff] ^ Table2[(y3 >> 8) & 0xff] ^ Table3[y0 & 0xff];
            x2 = Table0[y2 >> 24] ^ Table1[(y3 >> 16) & 0xff] ^ Table2[(y0 >> 8) & 0xff] ^ Table3[y1 & 0xff];
            x3 = Table0[y3 >> 24] ^ Table1[(y0 >> 16) & 0xff] ^ Table2[(y1 >> 8) & 0xff] ^ Table3[y2 & 0xff];
            state4 ^= x0;
            state5 ^= x1;
            state6 ^= x2;
            state7 ^= x3;
            x0 = state0 ^ rk[352 + 16];
            x1 = state1 ^ rk[352 + 17];
            x2 = state2 ^ rk[352 + 18];
            x3 = state3 ^ rk[352 + 19];
            y0 = Table0[x0 >> 24] ^ Table1[(x1 >> 16) & 0xff] ^ Table2[(x2 >> 8) & 0xff] ^ Table3[x3 & 0xff] ^ rk[352 + 20];
            y1 = Table0[x1 >> 24] ^ Table1[(x2 >> 16) & 0xff] ^ Table2[(x3 >> 8) & 0xff] ^ Table3[x0 & 0xff] ^ rk[352 + 21];
            y2 = Table0[x2 >> 24] ^ Table1[(x3 >> 16) & 0xff] ^ Table2[(x0 >> 8) & 0xff] ^ Table3[x1 & 0xff] ^ rk[352 + 22];
            y3 = Table0[x3 >> 24] ^ Table1[(x0 >> 16) & 0xff] ^ Table2[(x1 >> 8) & 0xff] ^ Table3[x2 & 0xff] ^ rk[352 + 23];
            x0 = Table0[y0 >> 24] ^ Table1[(y1 >> 16) & 0xff] ^ Table2[(y2 >> 8) & 0xff] ^ Table3[y3 & 0xff] ^ rk[352 + 24];
            x1 = Table0[y1 >> 24] ^ Table1[(y2 >> 16) & 0xff] ^ Table2[(y3 >> 8) & 0xff] ^ Table3[y0 & 0xff] ^ rk[352 + 25];
            x2 = Table0[y2 >> 24] ^ Table1[(y3 >> 16) & 0xff] ^ Table2[(y0 >> 8) & 0xff] ^ Table3[y1 & 0xff] ^ rk[352 + 26];
            x3 = Table0[y3 >> 24] ^ Table1[(y0 >> 16) & 0xff] ^ Table2[(y1 >> 8) & 0xff] ^ Table3[y2 & 0xff] ^ rk[352 + 27];
            y0 = Table0[x0 >> 24] ^ Table1[(x1 >> 16) & 0xff] ^ Table2[(x2 >> 8) & 0xff] ^ Table3[x3 & 0xff] ^ rk[352 + 28];
            y1 = Table0[x1 >> 24] ^ Table1[(x2 >> 16) & 0xff] ^ Table2[(x3 >> 8) & 0xff] ^ Table3[x0 & 0xff] ^ rk[352 + 29];
            y2 = Table0[x2 >> 24] ^ Table1[(x3 >> 16) & 0xff] ^ Table2[(x0 >> 8) & 0xff] ^ Table3[x1 & 0xff] ^ rk[352 + 30];
            y3 = Table0[x3 >> 24] ^ Table1[(x0 >> 16) & 0xff] ^ Table2[(x1 >> 8) & 0xff] ^ Table3[x2 & 0xff] ^ rk[352 + 31];
            x0 = Table0[y0 >> 24] ^ Table1[(y1 >> 16) & 0xff] ^ Table2[(y2 >> 8) & 0xff] ^ Table3[y3 & 0xff];
            x1 = Table0[y1 >> 24] ^ Table1[(y2 >> 16) & 0xff] ^ Table2[(y3 >> 8) & 0xff] ^ Table3[y0 & 0xff];
            x2 = Table0[y2 >> 24] ^ Table1[(y3 >> 16) & 0xff] ^ Table2[(y0 >> 8) & 0xff] ^ Table3[y1 & 0xff];
            x3 = Table0[y3 >> 24] ^ Table1[(y0 >> 16) & 0xff] ^ Table2[(y1 >> 8) & 0xff] ^ Table3[y2 & 0xff];
            state12 ^= x0;
            state13 ^= x1;
            state14 ^= x2;
            state15 ^= x3;

            x0 = state4 ^ rk[384];
            x1 = state5 ^ rk[384 + 1];
            x2 = state6 ^ rk[384 + 2];
            x3 = state7 ^ rk[384 + 3];
            y0 = Table0[x0 >> 24] ^ Table1[(x1 >> 16) & 0xff] ^ Table2[(x2 >> 8) & 0xff] ^ Table3[x3 & 0xff] ^ rk[384 + 4];
            y1 = Table0[x1 >> 24] ^ Table1[(x2 >> 16) & 0xff] ^ Table2[(x3 >> 8) & 0xff] ^ Table3[x0 & 0xff] ^ rk[384 + 5];
            y2 = Table0[x2 >> 24] ^ Table1[(x3 >> 16) & 0xff] ^ Table2[(x0 >> 8) & 0xff] ^ Table3[x1 & 0xff] ^ rk[384 + 6];
            y3 = Table0[x3 >> 24] ^ Table1[(x0 >> 16) & 0xff] ^ Table2[(x1 >> 8) & 0xff] ^ Table3[x2 & 0xff] ^ rk[384 + 7];
            x0 = Table0[y0 >> 24] ^ Table1[(y1 >> 16) & 0xff] ^ Table2[(y2 >> 8) & 0xff] ^ Table3[y3 & 0xff] ^ rk[384 + 8];
            x1 = Table0[y1 >> 24] ^ Table1[(y2 >> 16) & 0xff] ^ Table2[(y3 >> 8) & 0xff] ^ Table3[y0 & 0xff] ^ rk[384 + 9];
            x2 = Table0[y2 >> 24] ^ Table1[(y3 >> 16) & 0xff] ^ Table2[(y0 >> 8) & 0xff] ^ Table3[y1 & 0xff] ^ rk[384 + 10];
            x3 = Table0[y3 >> 24] ^ Table1[(y0 >> 16) & 0xff] ^ Table2[(y1 >> 8) & 0xff] ^ Table3[y2 & 0xff] ^ rk[384 + 11];
            y0 = Table0[x0 >> 24] ^ Table1[(x1 >> 16) & 0xff] ^ Table2[(x2 >> 8) & 0xff] ^ Table3[x3 & 0xff] ^ rk[384 + 12];
            y1 = Table0[x1 >> 24] ^ Table1[(x2 >> 16) & 0xff] ^ Table2[(x3 >> 8) & 0xff] ^ Table3[x0 & 0xff] ^ rk[384 + 13];
            y2 = Table0[x2 >> 24] ^ Table1[(x3 >> 16) & 0xff] ^ Table2[(x0 >> 8) & 0xff] ^ Table3[x1 & 0xff] ^ rk[384 + 14];
            y3 = Table0[x3 >> 24] ^ Table1[(x0 >> 16) & 0xff] ^ Table2[(x1 >> 8) & 0xff] ^ Table3[x2 & 0xff] ^ rk[384 + 15];
            x0 = Table0[y0 >> 24] ^ Table1[(y1 >> 16) & 0xff] ^ Table2[(y2 >> 8) & 0xff] ^ Table3[y3 & 0xff];
            x1 = Table0[y1 >> 24] ^ Table1[(y2 >> 16) & 0xff] ^ Table2[(y3 >> 8) & 0xff] ^ Table3[y0 & 0xff];
            x2 = Table0[y2 >> 24] ^ Table1[(y3 >> 16) & 0xff] ^ Table2[(y0 >> 8) & 0xff] ^ Table3[y1 & 0xff];
            x3 = Table0[y3 >> 24] ^ Table1[(y0 >> 16) & 0xff] ^ Table2[(y1 >> 8) & 0xff] ^ Table3[y2 & 0xff];
            state0 ^= x0;
            state1 ^= x1;
            state2 ^= x2;
            state3 ^= x3;
            x0 = state12 ^ rk[384 + 16];
            x1 = state13 ^ rk[384 + 17];
            x2 = state14 ^ rk[384 + 18];
            x3 = state15 ^ rk[384 + 19];
            y0 = Table0[x0 >> 24] ^ Table1[(x1 >> 16) & 0xff] ^ Table2[(x2 >> 8) & 0xff] ^ Table3[x3 & 0xff] ^ rk[384 + 20];
            y1 = Table0[x1 >> 24] ^ Table1[(x2 >> 16) & 0xff] ^ Table2[(x3 >> 8) & 0xff] ^ Table3[x0 & 0xff] ^ rk[384 + 21];
            y2 = Table0[x2 >> 24] ^ Table1[(x3 >> 16) & 0xff] ^ Table2[(x0 >> 8) & 0xff] ^ Table3[x1 & 0xff] ^ rk[384 + 22];
            y3 = Table0[x3 >> 24] ^ Table1[(x0 >> 16) & 0xff] ^ Table2[(x1 >> 8) & 0xff] ^ Table3[x2 & 0xff] ^ rk[384 + 23];
            x0 = Table0[y0 >> 24] ^ Table1[(y1 >> 16) & 0xff] ^ Table2[(y2 >> 8) & 0xff] ^ Table3[y3 & 0xff] ^ rk[384 + 24];
            x1 = Table0[y1 >> 24] ^ Table1[(y2 >> 16) & 0xff] ^ Table2[(y3 >> 8) & 0xff] ^ Table3[y0 & 0xff] ^ rk[384 + 25];
            x2 = Table0[y2 >> 24] ^ Table1[(y3 >> 16) & 0xff] ^ Table2[(y0 >> 8) & 0xff] ^ Table3[y1 & 0xff] ^ rk[384 + 26];
            x3 = Table0[y3 >> 24] ^ Table1[(y0 >> 16) & 0xff] ^ Table2[(y1 >> 8) & 0xff] ^ Table3[y2 & 0xff] ^ rk[384 + 27];
            y0 = Table0[x0 >> 24] ^ Table1[(x1 >> 16) & 0xff] ^ Table2[(x2 >> 8) & 0xff] ^ Table3[x3 & 0xff] ^ rk[384 + 28];
            y1 = Table0[x1 >> 24] ^ Table1[(x2 >> 16) & 0xff] ^ Table2[(x3 >> 8) & 0xff] ^ Table3[x0 & 0xff] ^ rk[384 + 29];
            y2 = Table0[x2 >> 24] ^ Table1[(x3 >> 16) & 0xff] ^ Table2[(x0 >> 8) & 0xff] ^ Table3[x1 & 0xff] ^ rk[384 + 30];
            y3 = Table0[x3 >> 24] ^ Table1[(x0 >> 16) & 0xff] ^ Table2[(x1 >> 8) & 0xff] ^ Table3[x2 & 0xff] ^ rk[384 + 31];
            x0 = Table0[y0 >> 24] ^ Table1[(y1 >> 16) & 0xff] ^ Table2[(y2 >> 8) & 0xff] ^ Table3[y3 & 0xff];
            x1 = Table0[y1 >> 24] ^ Table1[(y2 >> 16) & 0xff] ^ Table2[(y3 >> 8) & 0xff] ^ Table3[y0 & 0xff];
            x2 = Table0[y2 >> 24] ^ Table1[(y3 >> 16) & 0xff] ^ Table2[(y0 >> 8) & 0xff] ^ Table3[y1 & 0xff];
            x3 = Table0[y3 >> 24] ^ Table1[(y0 >> 16) & 0xff] ^ Table2[(y1 >> 8) & 0xff] ^ Table3[y2 & 0xff];
            state8 ^= x0;
            state9 ^= x1;
            state10 ^= x2;
            state11 ^= x3;

            x0 = state0 ^ rk[416];
            x1 = state1 ^ rk[416 + 1];
            x2 = state2 ^ rk[416 + 2];
            x3 = state3 ^ rk[416 + 3];
            y0 = Table0[x0 >> 24] ^ Table1[(x1 >> 16) & 0xff] ^ Table2[(x2 >> 8) & 0xff] ^ Table3[x3 & 0xff] ^ rk[416 + 4];
            y1 = Table0[x1 >> 24] ^ Table1[(x2 >> 16) & 0xff] ^ Table2[(x3 >> 8) & 0xff] ^ Table3[x0 & 0xff] ^ rk[416 + 5];
            y2 = Table0[x2 >> 24] ^ Table1[(x3 >> 16) & 0xff] ^ Table2[(x0 >> 8) & 0xff] ^ Table3[x1 & 0xff] ^ rk[416 + 6];
            y3 = Table0[x3 >> 24] ^ Table1[(x0 >> 16) & 0xff] ^ Table2[(x1 >> 8) & 0xff] ^ Table3[x2 & 0xff] ^ rk[416 + 7];
            x0 = Table0[y0 >> 24] ^ Table1[(y1 >> 16) & 0xff] ^ Table2[(y2 >> 8) & 0xff] ^ Table3[y3 & 0xff] ^ rk[416 + 8];
            x1 = Table0[y1 >> 24] ^ Table1[(y2 >> 16) & 0xff] ^ Table2[(y3 >> 8) & 0xff] ^ Table3[y0 & 0xff] ^ rk[416 + 9];
            x2 = Table0[y2 >> 24] ^ Table1[(y3 >> 16) & 0xff] ^ Table2[(y0 >> 8) & 0xff] ^ Table3[y1 & 0xff] ^ rk[416 + 10];
            x3 = Table0[y3 >> 24] ^ Table1[(y0 >> 16) & 0xff] ^ Table2[(y1 >> 8) & 0xff] ^ Table3[y2 & 0xff] ^ rk[416 + 11];
            y0 = Table0[x0 >> 24] ^ Table1[(x1 >> 16) & 0xff] ^ Table2[(x2 >> 8) & 0xff] ^ Table3[x3 & 0xff] ^ rk[416 + 12];
            y1 = Table0[x1 >> 24] ^ Table1[(x2 >> 16) & 0xff] ^ Table2[(x3 >> 8) & 0xff] ^ Table3[x0 & 0xff] ^ rk[416 + 13];
            y2 = Table0[x2 >> 24] ^ Table1[(x3 >> 16) & 0xff] ^ Table2[(x0 >> 8) & 0xff] ^ Table3[x1 & 0xff] ^ rk[416 + 14];
            y3 = Table0[x3 >> 24] ^ Table1[(x0 >> 16) & 0xff] ^ Table2[(x1 >> 8) & 0xff] ^ Table3[x2 & 0xff] ^ rk[416 + 15];
            x0 = Table0[y0 >> 24] ^ Table1[(y1 >> 16) & 0xff] ^ Table2[(y2 >> 8) & 0xff] ^ Table3[y3 & 0xff];
            x1 = Table0[y1 >> 24] ^ Table1[(y2 >> 16) & 0xff] ^ Table2[(y3 >> 8) & 0xff] ^ Table3[y0 & 0xff];
            x2 = Table0[y2 >> 24] ^ Table1[(y3 >> 16) & 0xff] ^ Table2[(y0 >> 8) & 0xff] ^ Table3[y1 & 0xff];
            x3 = Table0[y3 >> 24] ^ Table1[(y0 >> 16) & 0xff] ^ Table2[(y1 >> 8) & 0xff] ^ Table3[y2 & 0xff];
            state12 ^= x0;
            state13 ^= x1;
            state14 ^= x2;
            state15 ^= x3;
            x0 = state8 ^ rk[416 + 16];
            x1 = state9 ^ rk[416 + 17];
            x2 = state10 ^ rk[416 + 18];
            x3 = state11 ^ rk[416 + 19];
            y0 = Table0[x0 >> 24] ^ Table1[(x1 >> 16) & 0xff] ^ Table2[(x2 >> 8) & 0xff] ^ Table3[x3 & 0xff] ^ rk[416 + 20];
            y1 = Table0[x1 >> 24] ^ Table1[(x2 >> 16) & 0xff] ^ Table2[(x3 >> 8) & 0xff] ^ Table3[x0 & 0xff] ^ rk[416 + 21];
            y2 = Table0[x2 >> 24] ^ Table1[(x3 >> 16) & 0xff] ^ Table2[(x0 >> 8) & 0xff] ^ Table3[x1 & 0xff] ^ rk[416 + 22];
            y3 = Table0[x3 >> 24] ^ Table1[(x0 >> 16) & 0xff] ^ Table2[(x1 >> 8) & 0xff] ^ Table3[x2 & 0xff] ^ rk[416 + 23];
            x0 = Table0[y0 >> 24] ^ Table1[(y1 >> 16) & 0xff] ^ Table2[(y2 >> 8) & 0xff] ^ Table3[y3 & 0xff] ^ rk[416 + 24];
            x1 = Table0[y1 >> 24] ^ Table1[(y2 >> 16) & 0xff] ^ Table2[(y3 >> 8) & 0xff] ^ Table3[y0 & 0xff] ^ rk[416 + 25];
            x2 = Table0[y2 >> 24] ^ Table1[(y3 >> 16) & 0xff] ^ Table2[(y0 >> 8) & 0xff] ^ Table3[y1 & 0xff] ^ rk[416 + 26];
            x3 = Table0[y3 >> 24] ^ Table1[(y0 >> 16) & 0xff] ^ Table2[(y1 >> 8) & 0xff] ^ Table3[y2 & 0xff] ^ rk[416 + 27];
            y0 = Table0[x0 >> 24] ^ Table1[(x1 >> 16) & 0xff] ^ Table2[(x2 >> 8) & 0xff] ^ Table3[x3 & 0xff] ^ rk[416 + 28];
            y1 = Table0[x1 >> 24] ^ Table1[(x2 >> 16) & 0xff] ^ Table2[(x3 >> 8) & 0xff] ^ Table3[x0 & 0xff] ^ rk[416 + 29];
            y2 = Table0[x2 >> 24] ^ Table1[(x3 >> 16) & 0xff] ^ Table2[(x0 >> 8) & 0xff] ^ Table3[x1 & 0xff] ^ rk[416 + 30];
            y3 = Table0[x3 >> 24] ^ Table1[(x0 >> 16) & 0xff] ^ Table2[(x1 >> 8) & 0xff] ^ Table3[x2 & 0xff] ^ rk[416 + 31];
            x0 = Table0[y0 >> 24] ^ Table1[(y1 >> 16) & 0xff] ^ Table2[(y2 >> 8) & 0xff] ^ Table3[y3 & 0xff];
            x1 = Table0[y1 >> 24] ^ Table1[(y2 >> 16) & 0xff] ^ Table2[(y3 >> 8) & 0xff] ^ Table3[y0 & 0xff];
            x2 = Table0[y2 >> 24] ^ Table1[(y3 >> 16) & 0xff] ^ Table2[(y0 >> 8) & 0xff] ^ Table3[y1 & 0xff];
            x3 = Table0[y3 >> 24] ^ Table1[(y0 >> 16) & 0xff] ^ Table2[(y1 >> 8) & 0xff] ^ Table3[y2 & 0xff];
            state4 ^= x0;
            state5 ^= x1;
            state6 ^= x2;
            state7 ^= x3;

            m_state[0] ^= state8;
            m_state[1] ^= state9;
            m_state[2] ^= state10;
            m_state[3] ^= state11;
            m_state[4] ^= state12;
            m_state[5] ^= state13;
            m_state[6] ^= state14;
            m_state[7] ^= state15;
            m_state[8] ^= state0;
            m_state[9] ^= state1;
            m_state[10] ^= state2;
            m_state[11] ^= state3;
            m_state[12] ^= state4;
            m_state[13] ^= state5;
            m_state[14] ^= state6;
            m_state[15] ^= state7;
        }

        protected override void Finish()
        {
            byte[] pad = new byte[BlockSize];

            int buf_pos = m_buffer.Pos;

            Array.Copy(m_buffer.GetBytesZeroPadded(), 0, pad, 0, buf_pos);

            pad[buf_pos] = 0x80;

            if (buf_pos >= BlockSize - 18)
            {
                TransformBlock(pad, 0);

                pad.Clear();

                int padindex = BlockSize - 18;
                Converters.ConvertULongToBytes(m_processed_bytes * 8, pad, padindex);
                padindex += 16;

                int hashsizebits = HashSize * 8;
                pad[padindex++] = (byte)hashsizebits;
                pad[padindex] = (byte)(hashsizebits >> 8);

                m_processed_bytes = 0;
                TransformBlock(pad, 0);
            }
            else
            {
                Converters.ConvertULongToBytes(m_processed_bytes * 8, pad, BlockSize - 18);

                int hashsizebits = HashSize * 8;
                pad[BlockSize - 2] = (byte)hashsizebits;
                pad[BlockSize - 1] = (byte)(hashsizebits >> 8);

                if (buf_pos == 0)
                {
                    m_processed_bytes = 0;
                    TransformBlock(pad, 0);
                }
                else
                    TransformBlock(pad, 0);
            }
        }

        public SHAvite3_512Base(HashLib.HashSize a_hash_size)
            : base(a_hash_size, 128)
        {
            Initialize();
        }

        public override void Initialize()
        {
            if (HashSize == 48)
                Array.Copy(IV_384, 0, m_state, 0, 16);
            else
                Array.Copy(IV_512, 0, m_state, 0, 16);

            base.Initialize();
        }
    };
}
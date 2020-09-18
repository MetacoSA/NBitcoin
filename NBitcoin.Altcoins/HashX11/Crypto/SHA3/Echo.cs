using System;

namespace NBitcoin.Altcoins.HashX11.Crypto.SHA3
{
    internal class Echo224 : Echo256Base
    {
        public Echo224() :
            base(NBitcoin.Altcoins.HashX11.HashSize.HashSize224)
        {
        }
    }

    internal class Echo256 : Echo256Base
    {
        public Echo256() :
            base(NBitcoin.Altcoins.HashX11.HashSize.HashSize256)
        {
        }
    }

    internal class Echo384 : Echo512Base
    {
        public Echo384() :
            base(NBitcoin.Altcoins.HashX11.HashSize.HashSize384)
        {
        }
    }

    internal class Echo512 : Echo512Base
    {
        public Echo512() :
            base(NBitcoin.Altcoins.HashX11.HashSize.HashSize512)
        {
        }
    }

    internal abstract class EchoBase : BlockHash, ICryptoNotBuildIn
    {
        #region Consts
        protected readonly uint[] s_T0 = 
        {
            0xa56363c6, 0x847c7cf8, 0x997777ee, 0x8d7b7bf6, 0x0df2f2ff, 0xbd6b6bd6, 0xb16f6fde, 0x54c5c591, 
            0x50303060, 0x03010102, 0xa96767ce, 0x7d2b2b56, 0x19fefee7, 0x62d7d7b5, 0xe6abab4d, 0x9a7676ec, 
            0x45caca8f, 0x9d82821f, 0x40c9c989, 0x877d7dfa, 0x15fafaef, 0xeb5959b2, 0xc947478e, 0x0bf0f0fb, 
            0xecadad41, 0x67d4d4b3, 0xfda2a25f, 0xeaafaf45, 0xbf9c9c23, 0xf7a4a453, 0x967272e4, 0x5bc0c09b, 
            0xc2b7b775, 0x1cfdfde1, 0xae93933d, 0x6a26264c, 0x5a36366c, 0x413f3f7e, 0x02f7f7f5, 0x4fcccc83, 
            0x5c343468, 0xf4a5a551, 0x34e5e5d1, 0x08f1f1f9, 0x937171e2, 0x73d8d8ab, 0x53313162, 0x3f15152a, 
            0x0c040408, 0x52c7c795, 0x65232346, 0x5ec3c39d, 0x28181830, 0xa1969637, 0x0f05050a, 0xb59a9a2f, 
            0x0907070e, 0x36121224, 0x9b80801b, 0x3de2e2df, 0x26ebebcd, 0x6927274e, 0xcdb2b27f, 0x9f7575ea, 
            0x1b090912, 0x9e83831d, 0x742c2c58, 0x2e1a1a34, 0x2d1b1b36, 0xb26e6edc, 0xee5a5ab4, 0xfba0a05b, 
            0xf65252a4, 0x4d3b3b76, 0x61d6d6b7, 0xceb3b37d, 0x7b292952, 0x3ee3e3dd, 0x712f2f5e, 0x97848413, 
            0xf55353a6, 0x68d1d1b9, 0x00000000, 0x2cededc1, 0x60202040, 0x1ffcfce3, 0xc8b1b179, 0xed5b5bb6, 
            0xbe6a6ad4, 0x46cbcb8d, 0xd9bebe67, 0x4b393972, 0xde4a4a94, 0xd44c4c98, 0xe85858b0, 0x4acfcf85, 
            0x6bd0d0bb, 0x2aefefc5, 0xe5aaaa4f, 0x16fbfbed, 0xc5434386, 0xd74d4d9a, 0x55333366, 0x94858511, 
            0xcf45458a, 0x10f9f9e9, 0x06020204, 0x817f7ffe, 0xf05050a0, 0x443c3c78, 0xba9f9f25, 0xe3a8a84b, 
            0xf35151a2, 0xfea3a35d, 0xc0404080, 0x8a8f8f05, 0xad92923f, 0xbc9d9d21, 0x48383870, 0x04f5f5f1, 
            0xdfbcbc63, 0xc1b6b677, 0x75dadaaf, 0x63212142, 0x30101020, 0x1affffe5, 0x0ef3f3fd, 0x6dd2d2bf, 
            0x4ccdcd81, 0x140c0c18, 0x35131326, 0x2fececc3, 0xe15f5fbe, 0xa2979735, 0xcc444488, 0x3917172e, 
            0x57c4c493, 0xf2a7a755, 0x827e7efc, 0x473d3d7a, 0xac6464c8, 0xe75d5dba, 0x2b191932, 0x957373e6, 
            0xa06060c0, 0x98818119, 0xd14f4f9e, 0x7fdcdca3, 0x66222244, 0x7e2a2a54, 0xab90903b, 0x8388880b, 
            0xca46468c, 0x29eeeec7, 0xd3b8b86b, 0x3c141428, 0x79dedea7, 0xe25e5ebc, 0x1d0b0b16, 0x76dbdbad, 
            0x3be0e0db, 0x56323264, 0x4e3a3a74, 0x1e0a0a14, 0xdb494992, 0x0a06060c, 0x6c242448, 0xe45c5cb8, 
            0x5dc2c29f, 0x6ed3d3bd, 0xefacac43, 0xa66262c4, 0xa8919139, 0xa4959531, 0x37e4e4d3, 0x8b7979f2, 
            0x32e7e7d5, 0x43c8c88b, 0x5937376e, 0xb76d6dda, 0x8c8d8d01, 0x64d5d5b1, 0xd24e4e9c, 0xe0a9a949, 
            0xb46c6cd8, 0xfa5656ac, 0x07f4f4f3, 0x25eaeacf, 0xaf6565ca, 0x8e7a7af4, 0xe9aeae47, 0x18080810, 
            0xd5baba6f, 0x887878f0, 0x6f25254a, 0x722e2e5c, 0x241c1c38, 0xf1a6a657, 0xc7b4b473, 0x51c6c697, 
            0x23e8e8cb, 0x7cdddda1, 0x9c7474e8, 0x211f1f3e, 0xdd4b4b96, 0xdcbdbd61, 0x868b8b0d, 0x858a8a0f, 
            0x907070e0, 0x423e3e7c, 0xc4b5b571, 0xaa6666cc, 0xd8484890, 0x05030306, 0x01f6f6f7, 0x120e0e1c, 
            0xa36161c2, 0x5f35356a, 0xf95757ae, 0xd0b9b969, 0x91868617, 0x58c1c199, 0x271d1d3a, 0xb99e9e27, 
            0x38e1e1d9, 0x13f8f8eb, 0xb398982b, 0x33111122, 0xbb6969d2, 0x70d9d9a9, 0x898e8e07, 0xa7949433, 
            0xb69b9b2d, 0x221e1e3c, 0x92878715, 0x20e9e9c9, 0x49cece87, 0xff5555aa, 0x78282850, 0x7adfdfa5, 
            0x8f8c8c03, 0xf8a1a159, 0x80898909, 0x170d0d1a, 0xdabfbf65, 0x31e6e6d7, 0xc6424284, 0xb86868d0, 
            0xc3414182, 0xb0999929, 0x772d2d5a, 0x110f0f1e, 0xcbb0b07b, 0xfc5454a8, 0xd6bbbb6d, 0x3a16162c
        };

        protected readonly uint[] s_T1 = 
        {
            0x6363c6a5, 0x7c7cf884, 0x7777ee99, 0x7b7bf68d, 0xf2f2ff0d, 0x6b6bd6bd, 0x6f6fdeb1, 0xc5c59154, 
            0x30306050, 0x01010203, 0x6767cea9, 0x2b2b567d, 0xfefee719, 0xd7d7b562, 0xabab4de6, 0x7676ec9a, 
            0xcaca8f45, 0x82821f9d, 0xc9c98940, 0x7d7dfa87, 0xfafaef15, 0x5959b2eb, 0x47478ec9, 0xf0f0fb0b, 
            0xadad41ec, 0xd4d4b367, 0xa2a25ffd, 0xafaf45ea, 0x9c9c23bf, 0xa4a453f7, 0x7272e496, 0xc0c09b5b, 
            0xb7b775c2, 0xfdfde11c, 0x93933dae, 0x26264c6a, 0x36366c5a, 0x3f3f7e41, 0xf7f7f502, 0xcccc834f, 
            0x3434685c, 0xa5a551f4, 0xe5e5d134, 0xf1f1f908, 0x7171e293, 0xd8d8ab73, 0x31316253, 0x15152a3f, 
            0x0404080c, 0xc7c79552, 0x23234665, 0xc3c39d5e, 0x18183028, 0x969637a1, 0x05050a0f, 0x9a9a2fb5, 
            0x07070e09, 0x12122436, 0x80801b9b, 0xe2e2df3d, 0xebebcd26, 0x27274e69, 0xb2b27fcd, 0x7575ea9f, 
            0x0909121b, 0x83831d9e, 0x2c2c5874, 0x1a1a342e, 0x1b1b362d, 0x6e6edcb2, 0x5a5ab4ee, 0xa0a05bfb, 
            0x5252a4f6, 0x3b3b764d, 0xd6d6b761, 0xb3b37dce, 0x2929527b, 0xe3e3dd3e, 0x2f2f5e71, 0x84841397, 
            0x5353a6f5, 0xd1d1b968, 0x00000000, 0xededc12c, 0x20204060, 0xfcfce31f, 0xb1b179c8, 0x5b5bb6ed, 
            0x6a6ad4be, 0xcbcb8d46, 0xbebe67d9, 0x3939724b, 0x4a4a94de, 0x4c4c98d4, 0x5858b0e8, 0xcfcf854a, 
            0xd0d0bb6b, 0xefefc52a, 0xaaaa4fe5, 0xfbfbed16, 0x434386c5, 0x4d4d9ad7, 0x33336655, 0x85851194, 
            0x45458acf, 0xf9f9e910, 0x02020406, 0x7f7ffe81, 0x5050a0f0, 0x3c3c7844, 0x9f9f25ba, 0xa8a84be3, 
            0x5151a2f3, 0xa3a35dfe, 0x404080c0, 0x8f8f058a, 0x92923fad, 0x9d9d21bc, 0x38387048, 0xf5f5f104, 
            0xbcbc63df, 0xb6b677c1, 0xdadaaf75, 0x21214263, 0x10102030, 0xffffe51a, 0xf3f3fd0e, 0xd2d2bf6d, 
            0xcdcd814c, 0x0c0c1814, 0x13132635, 0xececc32f, 0x5f5fbee1, 0x979735a2, 0x444488cc, 0x17172e39, 
            0xc4c49357, 0xa7a755f2, 0x7e7efc82, 0x3d3d7a47, 0x6464c8ac, 0x5d5dbae7, 0x1919322b, 0x7373e695, 
            0x6060c0a0, 0x81811998, 0x4f4f9ed1, 0xdcdca37f, 0x22224466, 0x2a2a547e, 0x90903bab, 0x88880b83, 
            0x46468cca, 0xeeeec729, 0xb8b86bd3, 0x1414283c, 0xdedea779, 0x5e5ebce2, 0x0b0b161d, 0xdbdbad76, 
            0xe0e0db3b, 0x32326456, 0x3a3a744e, 0x0a0a141e, 0x494992db, 0x06060c0a, 0x2424486c, 0x5c5cb8e4, 
            0xc2c29f5d, 0xd3d3bd6e, 0xacac43ef, 0x6262c4a6, 0x919139a8, 0x959531a4, 0xe4e4d337, 0x7979f28b, 
            0xe7e7d532, 0xc8c88b43, 0x37376e59, 0x6d6ddab7, 0x8d8d018c, 0xd5d5b164, 0x4e4e9cd2, 0xa9a949e0, 
            0x6c6cd8b4, 0x5656acfa, 0xf4f4f307, 0xeaeacf25, 0x6565caaf, 0x7a7af48e, 0xaeae47e9, 0x08081018, 
            0xbaba6fd5, 0x7878f088, 0x25254a6f, 0x2e2e5c72, 0x1c1c3824, 0xa6a657f1, 0xb4b473c7, 0xc6c69751, 
            0xe8e8cb23, 0xdddda17c, 0x7474e89c, 0x1f1f3e21, 0x4b4b96dd, 0xbdbd61dc, 0x8b8b0d86, 0x8a8a0f85, 
            0x7070e090, 0x3e3e7c42, 0xb5b571c4, 0x6666ccaa, 0x484890d8, 0x03030605, 0xf6f6f701, 0x0e0e1c12, 
            0x6161c2a3, 0x35356a5f, 0x5757aef9, 0xb9b969d0, 0x86861791, 0xc1c19958, 0x1d1d3a27, 0x9e9e27b9, 
            0xe1e1d938, 0xf8f8eb13, 0x98982bb3, 0x11112233, 0x6969d2bb, 0xd9d9a970, 0x8e8e0789, 0x949433a7, 
            0x9b9b2db6, 0x1e1e3c22, 0x87871592, 0xe9e9c920, 0xcece8749, 0x5555aaff, 0x28285078, 0xdfdfa57a, 
            0x8c8c038f, 0xa1a159f8, 0x89890980, 0x0d0d1a17, 0xbfbf65da, 0xe6e6d731, 0x424284c6, 0x6868d0b8, 
            0x414182c3, 0x999929b0, 0x2d2d5a77, 0x0f0f1e11, 0xb0b07bcb, 0x5454a8fc, 0xbbbb6dd6, 0x16162c3a
        };

        protected readonly uint[] s_T2 = 
        {
            0x63c6a563, 0x7cf8847c, 0x77ee9977, 0x7bf68d7b, 0xf2ff0df2, 0x6bd6bd6b, 0x6fdeb16f, 0xc59154c5, 
            0x30605030, 0x01020301, 0x67cea967, 0x2b567d2b, 0xfee719fe, 0xd7b562d7, 0xab4de6ab, 0x76ec9a76, 
            0xca8f45ca, 0x821f9d82, 0xc98940c9, 0x7dfa877d, 0xfaef15fa, 0x59b2eb59, 0x478ec947, 0xf0fb0bf0, 
            0xad41ecad, 0xd4b367d4, 0xa25ffda2, 0xaf45eaaf, 0x9c23bf9c, 0xa453f7a4, 0x72e49672, 0xc09b5bc0, 
            0xb775c2b7, 0xfde11cfd, 0x933dae93, 0x264c6a26, 0x366c5a36, 0x3f7e413f, 0xf7f502f7, 0xcc834fcc, 
            0x34685c34, 0xa551f4a5, 0xe5d134e5, 0xf1f908f1, 0x71e29371, 0xd8ab73d8, 0x31625331, 0x152a3f15, 
            0x04080c04, 0xc79552c7, 0x23466523, 0xc39d5ec3, 0x18302818, 0x9637a196, 0x050a0f05, 0x9a2fb59a, 
            0x070e0907, 0x12243612, 0x801b9b80, 0xe2df3de2, 0xebcd26eb, 0x274e6927, 0xb27fcdb2, 0x75ea9f75, 
            0x09121b09, 0x831d9e83, 0x2c58742c, 0x1a342e1a, 0x1b362d1b, 0x6edcb26e, 0x5ab4ee5a, 0xa05bfba0, 
            0x52a4f652, 0x3b764d3b, 0xd6b761d6, 0xb37dceb3, 0x29527b29, 0xe3dd3ee3, 0x2f5e712f, 0x84139784, 
            0x53a6f553, 0xd1b968d1, 0x00000000, 0xedc12ced, 0x20406020, 0xfce31ffc, 0xb179c8b1, 0x5bb6ed5b, 
            0x6ad4be6a, 0xcb8d46cb, 0xbe67d9be, 0x39724b39, 0x4a94de4a, 0x4c98d44c, 0x58b0e858, 0xcf854acf, 
            0xd0bb6bd0, 0xefc52aef, 0xaa4fe5aa, 0xfbed16fb, 0x4386c543, 0x4d9ad74d, 0x33665533, 0x85119485, 
            0x458acf45, 0xf9e910f9, 0x02040602, 0x7ffe817f, 0x50a0f050, 0x3c78443c, 0x9f25ba9f, 0xa84be3a8, 
            0x51a2f351, 0xa35dfea3, 0x4080c040, 0x8f058a8f, 0x923fad92, 0x9d21bc9d, 0x38704838, 0xf5f104f5, 
            0xbc63dfbc, 0xb677c1b6, 0xdaaf75da, 0x21426321, 0x10203010, 0xffe51aff, 0xf3fd0ef3, 0xd2bf6dd2, 
            0xcd814ccd, 0x0c18140c, 0x13263513, 0xecc32fec, 0x5fbee15f, 0x9735a297, 0x4488cc44, 0x172e3917, 
            0xc49357c4, 0xa755f2a7, 0x7efc827e, 0x3d7a473d, 0x64c8ac64, 0x5dbae75d, 0x19322b19, 0x73e69573, 
            0x60c0a060, 0x81199881, 0x4f9ed14f, 0xdca37fdc, 0x22446622, 0x2a547e2a, 0x903bab90, 0x880b8388, 
            0x468cca46, 0xeec729ee, 0xb86bd3b8, 0x14283c14, 0xdea779de, 0x5ebce25e, 0x0b161d0b, 0xdbad76db, 
            0xe0db3be0, 0x32645632, 0x3a744e3a, 0x0a141e0a, 0x4992db49, 0x060c0a06, 0x24486c24, 0x5cb8e45c, 
            0xc29f5dc2, 0xd3bd6ed3, 0xac43efac, 0x62c4a662, 0x9139a891, 0x9531a495, 0xe4d337e4, 0x79f28b79, 
            0xe7d532e7, 0xc88b43c8, 0x376e5937, 0x6ddab76d, 0x8d018c8d, 0xd5b164d5, 0x4e9cd24e, 0xa949e0a9, 
            0x6cd8b46c, 0x56acfa56, 0xf4f307f4, 0xeacf25ea, 0x65caaf65, 0x7af48e7a, 0xae47e9ae, 0x08101808, 
            0xba6fd5ba, 0x78f08878, 0x254a6f25, 0x2e5c722e, 0x1c38241c, 0xa657f1a6, 0xb473c7b4, 0xc69751c6, 
            0xe8cb23e8, 0xdda17cdd, 0x74e89c74, 0x1f3e211f, 0x4b96dd4b, 0xbd61dcbd, 0x8b0d868b, 0x8a0f858a, 
            0x70e09070, 0x3e7c423e, 0xb571c4b5, 0x66ccaa66, 0x4890d848, 0x03060503, 0xf6f701f6, 0x0e1c120e, 
            0x61c2a361, 0x356a5f35, 0x57aef957, 0xb969d0b9, 0x86179186, 0xc19958c1, 0x1d3a271d, 0x9e27b99e, 
            0xe1d938e1, 0xf8eb13f8, 0x982bb398, 0x11223311, 0x69d2bb69, 0xd9a970d9, 0x8e07898e, 0x9433a794, 
            0x9b2db69b, 0x1e3c221e, 0x87159287, 0xe9c920e9, 0xce8749ce, 0x55aaff55, 0x28507828, 0xdfa57adf, 
            0x8c038f8c, 0xa159f8a1, 0x89098089, 0x0d1a170d, 0xbf65dabf, 0xe6d731e6, 0x4284c642, 0x68d0b868, 
            0x4182c341, 0x9929b099, 0x2d5a772d, 0x0f1e110f, 0xb07bcbb0, 0x54a8fc54, 0xbb6dd6bb, 0x162c3a16
        };

        protected readonly uint[] s_T3 = 
        {
            0xc6a56363, 0xf8847c7c, 0xee997777, 0xf68d7b7b, 0xff0df2f2, 0xd6bd6b6b, 0xdeb16f6f, 0x9154c5c5, 
            0x60503030, 0x02030101, 0xcea96767, 0x567d2b2b, 0xe719fefe, 0xb562d7d7, 0x4de6abab, 0xec9a7676, 
            0x8f45caca, 0x1f9d8282, 0x8940c9c9, 0xfa877d7d, 0xef15fafa, 0xb2eb5959, 0x8ec94747, 0xfb0bf0f0, 
            0x41ecadad, 0xb367d4d4, 0x5ffda2a2, 0x45eaafaf, 0x23bf9c9c, 0x53f7a4a4, 0xe4967272, 0x9b5bc0c0, 
            0x75c2b7b7, 0xe11cfdfd, 0x3dae9393, 0x4c6a2626, 0x6c5a3636, 0x7e413f3f, 0xf502f7f7, 0x834fcccc, 
            0x685c3434, 0x51f4a5a5, 0xd134e5e5, 0xf908f1f1, 0xe2937171, 0xab73d8d8, 0x62533131, 0x2a3f1515, 
            0x080c0404, 0x9552c7c7, 0x46652323, 0x9d5ec3c3, 0x30281818, 0x37a19696, 0x0a0f0505, 0x2fb59a9a, 
            0x0e090707, 0x24361212, 0x1b9b8080, 0xdf3de2e2, 0xcd26ebeb, 0x4e692727, 0x7fcdb2b2, 0xea9f7575, 
            0x121b0909, 0x1d9e8383, 0x58742c2c, 0x342e1a1a, 0x362d1b1b, 0xdcb26e6e, 0xb4ee5a5a, 0x5bfba0a0, 
            0xa4f65252, 0x764d3b3b, 0xb761d6d6, 0x7dceb3b3, 0x527b2929, 0xdd3ee3e3, 0x5e712f2f, 0x13978484, 
            0xa6f55353, 0xb968d1d1, 0x00000000, 0xc12ceded, 0x40602020, 0xe31ffcfc, 0x79c8b1b1, 0xb6ed5b5b, 
            0xd4be6a6a, 0x8d46cbcb, 0x67d9bebe, 0x724b3939, 0x94de4a4a, 0x98d44c4c, 0xb0e85858, 0x854acfcf, 
            0xbb6bd0d0, 0xc52aefef, 0x4fe5aaaa, 0xed16fbfb, 0x86c54343, 0x9ad74d4d, 0x66553333, 0x11948585, 
            0x8acf4545, 0xe910f9f9, 0x04060202, 0xfe817f7f, 0xa0f05050, 0x78443c3c, 0x25ba9f9f, 0x4be3a8a8, 
            0xa2f35151, 0x5dfea3a3, 0x80c04040, 0x058a8f8f, 0x3fad9292, 0x21bc9d9d, 0x70483838, 0xf104f5f5, 
            0x63dfbcbc, 0x77c1b6b6, 0xaf75dada, 0x42632121, 0x20301010, 0xe51affff, 0xfd0ef3f3, 0xbf6dd2d2, 
            0x814ccdcd, 0x18140c0c, 0x26351313, 0xc32fecec, 0xbee15f5f, 0x35a29797, 0x88cc4444, 0x2e391717, 
            0x9357c4c4, 0x55f2a7a7, 0xfc827e7e, 0x7a473d3d, 0xc8ac6464, 0xbae75d5d, 0x322b1919, 0xe6957373, 
            0xc0a06060, 0x19988181, 0x9ed14f4f, 0xa37fdcdc, 0x44662222, 0x547e2a2a, 0x3bab9090, 0x0b838888, 
            0x8cca4646, 0xc729eeee, 0x6bd3b8b8, 0x283c1414, 0xa779dede, 0xbce25e5e, 0x161d0b0b, 0xad76dbdb, 
            0xdb3be0e0, 0x64563232, 0x744e3a3a, 0x141e0a0a, 0x92db4949, 0x0c0a0606, 0x486c2424, 0xb8e45c5c, 
            0x9f5dc2c2, 0xbd6ed3d3, 0x43efacac, 0xc4a66262, 0x39a89191, 0x31a49595, 0xd337e4e4, 0xf28b7979, 
            0xd532e7e7, 0x8b43c8c8, 0x6e593737, 0xdab76d6d, 0x018c8d8d, 0xb164d5d5, 0x9cd24e4e, 0x49e0a9a9, 
            0xd8b46c6c, 0xacfa5656, 0xf307f4f4, 0xcf25eaea, 0xcaaf6565, 0xf48e7a7a, 0x47e9aeae, 0x10180808, 
            0x6fd5baba, 0xf0887878, 0x4a6f2525, 0x5c722e2e, 0x38241c1c, 0x57f1a6a6, 0x73c7b4b4, 0x9751c6c6, 
            0xcb23e8e8, 0xa17cdddd, 0xe89c7474, 0x3e211f1f, 0x96dd4b4b, 0x61dcbdbd, 0x0d868b8b, 0x0f858a8a, 
            0xe0907070, 0x7c423e3e, 0x71c4b5b5, 0xccaa6666, 0x90d84848, 0x06050303, 0xf701f6f6, 0x1c120e0e, 
            0xc2a36161, 0x6a5f3535, 0xaef95757, 0x69d0b9b9, 0x17918686, 0x9958c1c1, 0x3a271d1d, 0x27b99e9e, 
            0xd938e1e1, 0xeb13f8f8, 0x2bb39898, 0x22331111, 0xd2bb6969, 0xa970d9d9, 0x07898e8e, 0x33a79494, 
            0x2db69b9b, 0x3c221e1e, 0x15928787, 0xc920e9e9, 0x8749cece, 0xaaff5555, 0x50782828, 0xa57adfdf, 
            0x038f8c8c, 0x59f8a1a1, 0x09808989, 0x1a170d0d, 0x65dabfbf, 0xd731e6e6, 0x84c64242, 0xd0b86868, 
            0x82c34141, 0x29b09999, 0x5a772d2d, 0x1e110f0f, 0x7bcbb0b0, 0xa8fc5454, 0x6dd6bbbb, 0x2c3a1616
        };
        #endregion

        protected readonly ulong[] m_state = new ulong[32];
        protected bool m_last_block;

        public EchoBase(HashSize a_hash_size, int a_block_size)
            : base((int)a_hash_size, a_block_size)
        {
            Initialize();
        }

        protected override byte[] GetResult()
        {
            return Converters.ConvertULongsToBytes(m_state, 0, (HashSize + 7) / 8).SubArray(0, HashSize);
        }

        protected override void Finish()
        {
            byte[] pad = new byte[BlockSize];
            pad[0] = 0x80;

            ulong bits = m_processed_bytes * 8;

            int pad_len;

            if ((BlockSize - m_buffer.Pos) <= 18)
            {
                pad_len = BlockSize - m_buffer.Pos;
                m_processed_bytes -= (ulong)pad_len;
                TransformBytes(pad, 0, BlockSize - m_buffer.Pos);
                m_processed_bytes += (ulong)pad_len;

                pad.Clear();
            }

            int padindex = BlockSize - m_buffer.Pos - 18;

            pad[padindex++] = (byte)(HashSize * 8);
            pad[padindex++] = (byte)((HashSize * 8) >> 8);

            Converters.ConvertULongToBytes(bits, pad, padindex);

            m_last_block = (m_buffer.Pos == 0);

            pad_len = BlockSize - m_buffer.Pos;
            m_processed_bytes -= (ulong)pad_len;
            TransformBytes(pad, 0, BlockSize - m_buffer.Pos);
            m_processed_bytes += (ulong)pad_len;
        }

        public override void Initialize()
        {
            m_state.Clear();
            m_last_block = false;

            base.Initialize();
        }
    };

    internal abstract class Echo256Base : EchoBase
    {
        public Echo256Base(HashSize a_hash_size)
            : base(a_hash_size, 192)
        {
        }

        protected override void TransformBlock(byte[] a_data, int a_index)
        {
            Converters.ConvertBytesToULongs(a_data, a_index, BlockSize, m_state, 8);

            uint WA, WB, WC, WD;

            ulong[] cv = new ulong[8];

            cv[0] = m_state[0] ^ m_state[8] ^ m_state[16] ^ m_state[24];
            cv[1] = m_state[1] ^ m_state[9] ^ m_state[17] ^ m_state[25];
            cv[2] = m_state[2] ^ m_state[10] ^ m_state[18] ^ m_state[26];
            cv[3] = m_state[3] ^ m_state[11] ^ m_state[19] ^ m_state[27];
            cv[4] = m_state[4] ^ m_state[12] ^ m_state[20] ^ m_state[28];
            cv[5] = m_state[5] ^ m_state[13] ^ m_state[21] ^ m_state[29];
            cv[6] = m_state[6] ^ m_state[14] ^ m_state[22] ^ m_state[30];
            cv[7] = m_state[7] ^ m_state[15] ^ m_state[23] ^ m_state[31];

            ulong WL0 = m_state[0];
            ulong WH0 = m_state[1];
            ulong WL1 = m_state[2];
            ulong WH1 = m_state[3];
            ulong WL2 = m_state[4];
            ulong WH2 = m_state[5];
            ulong WL3 = m_state[6];
            ulong WH3 = m_state[7];
            ulong WL4 = m_state[8];
            ulong WH4 = m_state[9];
            ulong WL5 = m_state[10];
            ulong WH5 = m_state[11];
            ulong WL6 = m_state[12];
            ulong WH6 = m_state[13];
            ulong WL7 = m_state[14];
            ulong WH7 = m_state[15];
            ulong WL8 = m_state[16];
            ulong WH8 = m_state[17];
            ulong WL9 = m_state[18];
            ulong WH9 = m_state[19];
            ulong WL10 = m_state[20];
            ulong WH10 = m_state[21];
            ulong WL11 = m_state[22];
            ulong WH11 = m_state[23];
            ulong WL12 = m_state[24];
            ulong WH12 = m_state[25];
            ulong WL13 = m_state[26];
            ulong WH13 = m_state[27];
            ulong WL14 = m_state[28];
            ulong WH14 = m_state[29];
            ulong WL15 = m_state[30];
            ulong WH15 = m_state[31];

            int r = 8;

            ulong cnt = 0;
            if (!m_last_block)
                cnt = m_processed_bytes * 8;

            do
            {
                WA = s_T0[(byte)(WL0)] ^ s_T1[(byte)(WL0 >> 40)] ^ s_T2[(byte)(WH0 >> 16)] ^ s_T3[(byte)(WH0 >> 56)] ^ (uint)cnt;
                WB = s_T0[(byte)(WL0 >> 32)] ^ s_T1[(byte)(WH0 >> 8)] ^ s_T2[(byte)(WH0 >> 48)] ^ s_T3[(byte)(WL0 >> 24)] ^ (uint)(cnt >> 32);
                WC = s_T0[(byte)(WH0)] ^ s_T1[(byte)(WH0 >> 40)] ^ s_T2[(byte)(WL0 >> 16)] ^ s_T3[(byte)(WL0 >> 56)];
                WD = s_T0[(byte)(WH0 >> 32)] ^ s_T1[(byte)(WL0 >> 8)] ^ s_T2[(byte)(WL0 >> 48)] ^ s_T3[(byte)(WH0 >> 24)];
                cnt++;
                WL0 = s_T0[(byte)(WA)] ^ s_T1[(byte)(WB >> 8)] ^ s_T2[(byte)(WC >> 16)] ^ s_T3[(byte)(WD >> 24)];
                WL0 ^= (ulong)(s_T0[(byte)(WB)] ^ s_T1[(byte)(WC >> 8)] ^ s_T2[(byte)(WD >> 16)] ^ s_T3[(byte)(WA >> 24)]) << 32;
                WH0 = s_T0[(byte)(WC)] ^ s_T1[(byte)(WD >> 8)] ^ s_T2[(byte)(WA >> 16)] ^ s_T3[(byte)(WB >> 24)];
                WH0 ^= (ulong)(s_T0[(byte)(WD)] ^ s_T1[(byte)(WA >> 8)] ^ s_T2[(byte)(WB >> 16)] ^ s_T3[(byte)(WC >> 24)]) << 32;

                WA = s_T0[(byte)(WL1)] ^ s_T1[(byte)(WL1 >> 40)] ^ s_T2[(byte)(WH1 >> 16)] ^ s_T3[(byte)(WH1 >> 56)] ^ (uint)cnt;
                WB = s_T0[(byte)(WL1 >> 32)] ^ s_T1[(byte)(WH1 >> 8)] ^ s_T2[(byte)(WH1 >> 48)] ^ s_T3[(byte)(WL1 >> 24)] ^ (uint)(cnt >> 32);
                WC = s_T0[(byte)(WH1)] ^ s_T1[(byte)(WH1 >> 40)] ^ s_T2[(byte)(WL1 >> 16)] ^ s_T3[(byte)(WL1 >> 56)];
                WD = s_T0[(byte)(WH1 >> 32)] ^ s_T1[(byte)(WL1 >> 8)] ^ s_T2[(byte)(WL1 >> 48)] ^ s_T3[(byte)(WH1 >> 24)];
                cnt++;
                WL1 = s_T0[(byte)(WA)] ^ s_T1[(byte)(WB >> 8)] ^ s_T2[(byte)(WC >> 16)] ^ s_T3[(byte)(WD >> 24)];
                WL1 ^= (ulong)(s_T0[(byte)(WB)] ^ s_T1[(byte)(WC >> 8)] ^ s_T2[(byte)(WD >> 16)] ^ s_T3[(byte)(WA >> 24)]) << 32;
                WH1 = s_T0[(byte)(WC)] ^ s_T1[(byte)(WD >> 8)] ^ s_T2[(byte)(WA >> 16)] ^ s_T3[(byte)(WB >> 24)];
                WH1 ^= (ulong)(s_T0[(byte)(WD)] ^ s_T1[(byte)(WA >> 8)] ^ s_T2[(byte)(WB >> 16)] ^ s_T3[(byte)(WC >> 24)]) << 32;

                WA = s_T0[(byte)(WL2)] ^ s_T1[(byte)(WL2 >> 40)] ^ s_T2[(byte)(WH2 >> 16)] ^ s_T3[(byte)(WH2 >> 56)] ^ (uint)cnt;
                WB = s_T0[(byte)(WL2 >> 32)] ^ s_T1[(byte)(WH2 >> 8)] ^ s_T2[(byte)(WH2 >> 48)] ^ s_T3[(byte)(WL2 >> 24)] ^ (uint)(cnt >> 32);
                WC = s_T0[(byte)(WH2)] ^ s_T1[(byte)(WH2 >> 40)] ^ s_T2[(byte)(WL2 >> 16)] ^ s_T3[(byte)(WL2 >> 56)];
                WD = s_T0[(byte)(WH2 >> 32)] ^ s_T1[(byte)(WL2 >> 8)] ^ s_T2[(byte)(WL2 >> 48)] ^ s_T3[(byte)(WH2 >> 24)];
                cnt++;
                WL2 = s_T0[(byte)(WA)] ^ s_T1[(byte)(WB >> 8)] ^ s_T2[(byte)(WC >> 16)] ^ s_T3[(byte)(WD >> 24)];
                WL2 ^= (ulong)(s_T0[(byte)(WB)] ^ s_T1[(byte)(WC >> 8)] ^ s_T2[(byte)(WD >> 16)] ^ s_T3[(byte)(WA >> 24)]) << 32;
                WH2 = s_T0[(byte)(WC)] ^ s_T1[(byte)(WD >> 8)] ^ s_T2[(byte)(WA >> 16)] ^ s_T3[(byte)(WB >> 24)];
                WH2 ^= (ulong)(s_T0[(byte)(WD)] ^ s_T1[(byte)(WA >> 8)] ^ s_T2[(byte)(WB >> 16)] ^ s_T3[(byte)(WC >> 24)]) << 32;

                WA = s_T0[(byte)(WL3)] ^ s_T1[(byte)(WL3 >> 40)] ^ s_T2[(byte)(WH3 >> 16)] ^ s_T3[(byte)(WH3 >> 56)] ^ (uint)cnt;
                WB = s_T0[(byte)(WL3 >> 32)] ^ s_T1[(byte)(WH3 >> 8)] ^ s_T2[(byte)(WH3 >> 48)] ^ s_T3[(byte)(WL3 >> 24)] ^ (uint)(cnt >> 32);
                WC = s_T0[(byte)(WH3)] ^ s_T1[(byte)(WH3 >> 40)] ^ s_T2[(byte)(WL3 >> 16)] ^ s_T3[(byte)(WL3 >> 56)];
                WD = s_T0[(byte)(WH3 >> 32)] ^ s_T1[(byte)(WL3 >> 8)] ^ s_T2[(byte)(WL3 >> 48)] ^ s_T3[(byte)(WH3 >> 24)];
                cnt++;
                WL3 = s_T0[(byte)(WA)] ^ s_T1[(byte)(WB >> 8)] ^ s_T2[(byte)(WC >> 16)] ^ s_T3[(byte)(WD >> 24)];
                WL3 ^= (ulong)(s_T0[(byte)(WB)] ^ s_T1[(byte)(WC >> 8)] ^ s_T2[(byte)(WD >> 16)] ^ s_T3[(byte)(WA >> 24)]) << 32;
                WH3 = s_T0[(byte)(WC)] ^ s_T1[(byte)(WD >> 8)] ^ s_T2[(byte)(WA >> 16)] ^ s_T3[(byte)(WB >> 24)];
                WH3 ^= (ulong)(s_T0[(byte)(WD)] ^ s_T1[(byte)(WA >> 8)] ^ s_T2[(byte)(WB >> 16)] ^ s_T3[(byte)(WC >> 24)]) << 32;

                WA = s_T0[(byte)(WL4)] ^ s_T1[(byte)(WL4 >> 40)] ^ s_T2[(byte)(WH4 >> 16)] ^ s_T3[(byte)(WH4 >> 56)] ^ (uint)cnt;
                WB = s_T0[(byte)(WL4 >> 32)] ^ s_T1[(byte)(WH4 >> 8)] ^ s_T2[(byte)(WH4 >> 48)] ^ s_T3[(byte)(WL4 >> 24)] ^ (uint)(cnt >> 32);
                WC = s_T0[(byte)(WH4)] ^ s_T1[(byte)(WH4 >> 40)] ^ s_T2[(byte)(WL4 >> 16)] ^ s_T3[(byte)(WL4 >> 56)];
                WD = s_T0[(byte)(WH4 >> 32)] ^ s_T1[(byte)(WL4 >> 8)] ^ s_T2[(byte)(WL4 >> 48)] ^ s_T3[(byte)(WH4 >> 24)];
                cnt++;
                WL4 = s_T0[(byte)(WA)] ^ s_T1[(byte)(WB >> 8)] ^ s_T2[(byte)(WC >> 16)] ^ s_T3[(byte)(WD >> 24)];
                WL4 ^= (ulong)(s_T0[(byte)(WB)] ^ s_T1[(byte)(WC >> 8)] ^ s_T2[(byte)(WD >> 16)] ^ s_T3[(byte)(WA >> 24)]) << 32;
                WH4 = s_T0[(byte)(WC)] ^ s_T1[(byte)(WD >> 8)] ^ s_T2[(byte)(WA >> 16)] ^ s_T3[(byte)(WB >> 24)];
                WH4 ^= (ulong)(s_T0[(byte)(WD)] ^ s_T1[(byte)(WA >> 8)] ^ s_T2[(byte)(WB >> 16)] ^ s_T3[(byte)(WC >> 24)]) << 32;

                WA = s_T0[(byte)(WL5)] ^ s_T1[(byte)(WL5 >> 40)] ^ s_T2[(byte)(WH5 >> 16)] ^ s_T3[(byte)(WH5 >> 56)] ^ (uint)cnt;
                WB = s_T0[(byte)(WL5 >> 32)] ^ s_T1[(byte)(WH5 >> 8)] ^ s_T2[(byte)(WH5 >> 48)] ^ s_T3[(byte)(WL5 >> 24)] ^ (uint)(cnt >> 32);
                WC = s_T0[(byte)(WH5)] ^ s_T1[(byte)(WH5 >> 40)] ^ s_T2[(byte)(WL5 >> 16)] ^ s_T3[(byte)(WL5 >> 56)];
                WD = s_T0[(byte)(WH5 >> 32)] ^ s_T1[(byte)(WL5 >> 8)] ^ s_T2[(byte)(WL5 >> 48)] ^ s_T3[(byte)(WH5 >> 24)];
                cnt++;
                WL5 = s_T0[(byte)(WA)] ^ s_T1[(byte)(WB >> 8)] ^ s_T2[(byte)(WC >> 16)] ^ s_T3[(byte)(WD >> 24)];
                WL5 ^= (ulong)(s_T0[(byte)(WB)] ^ s_T1[(byte)(WC >> 8)] ^ s_T2[(byte)(WD >> 16)] ^ s_T3[(byte)(WA >> 24)]) << 32;
                WH5 = s_T0[(byte)(WC)] ^ s_T1[(byte)(WD >> 8)] ^ s_T2[(byte)(WA >> 16)] ^ s_T3[(byte)(WB >> 24)];
                WH5 ^= (ulong)(s_T0[(byte)(WD)] ^ s_T1[(byte)(WA >> 8)] ^ s_T2[(byte)(WB >> 16)] ^ s_T3[(byte)(WC >> 24)]) << 32;

                WA = s_T0[(byte)(WL6)] ^ s_T1[(byte)(WL6 >> 40)] ^ s_T2[(byte)(WH6 >> 16)] ^ s_T3[(byte)(WH6 >> 56)] ^ (uint)cnt;
                WB = s_T0[(byte)(WL6 >> 32)] ^ s_T1[(byte)(WH6 >> 8)] ^ s_T2[(byte)(WH6 >> 48)] ^ s_T3[(byte)(WL6 >> 24)] ^ (uint)(cnt >> 32);
                WC = s_T0[(byte)(WH6)] ^ s_T1[(byte)(WH6 >> 40)] ^ s_T2[(byte)(WL6 >> 16)] ^ s_T3[(byte)(WL6 >> 56)];
                WD = s_T0[(byte)(WH6 >> 32)] ^ s_T1[(byte)(WL6 >> 8)] ^ s_T2[(byte)(WL6 >> 48)] ^ s_T3[(byte)(WH6 >> 24)];
                cnt++;
                WL6 = s_T0[(byte)(WA)] ^ s_T1[(byte)(WB >> 8)] ^ s_T2[(byte)(WC >> 16)] ^ s_T3[(byte)(WD >> 24)];
                WL6 ^= (ulong)(s_T0[(byte)(WB)] ^ s_T1[(byte)(WC >> 8)] ^ s_T2[(byte)(WD >> 16)] ^ s_T3[(byte)(WA >> 24)]) << 32;
                WH6 = s_T0[(byte)(WC)] ^ s_T1[(byte)(WD >> 8)] ^ s_T2[(byte)(WA >> 16)] ^ s_T3[(byte)(WB >> 24)];
                WH6 ^= (ulong)(s_T0[(byte)(WD)] ^ s_T1[(byte)(WA >> 8)] ^ s_T2[(byte)(WB >> 16)] ^ s_T3[(byte)(WC >> 24)]) << 32;

                WA = s_T0[(byte)(WL7)] ^ s_T1[(byte)(WL7 >> 40)] ^ s_T2[(byte)(WH7 >> 16)] ^ s_T3[(byte)(WH7 >> 56)] ^ (uint)cnt;
                WB = s_T0[(byte)(WL7 >> 32)] ^ s_T1[(byte)(WH7 >> 8)] ^ s_T2[(byte)(WH7 >> 48)] ^ s_T3[(byte)(WL7 >> 24)] ^ (uint)(cnt >> 32);
                WC = s_T0[(byte)(WH7)] ^ s_T1[(byte)(WH7 >> 40)] ^ s_T2[(byte)(WL7 >> 16)] ^ s_T3[(byte)(WL7 >> 56)];
                WD = s_T0[(byte)(WH7 >> 32)] ^ s_T1[(byte)(WL7 >> 8)] ^ s_T2[(byte)(WL7 >> 48)] ^ s_T3[(byte)(WH7 >> 24)];
                cnt++;
                WL7 = s_T0[(byte)(WA)] ^ s_T1[(byte)(WB >> 8)] ^ s_T2[(byte)(WC >> 16)] ^ s_T3[(byte)(WD >> 24)];
                WL7 ^= (ulong)(s_T0[(byte)(WB)] ^ s_T1[(byte)(WC >> 8)] ^ s_T2[(byte)(WD >> 16)] ^ s_T3[(byte)(WA >> 24)]) << 32;
                WH7 = s_T0[(byte)(WC)] ^ s_T1[(byte)(WD >> 8)] ^ s_T2[(byte)(WA >> 16)] ^ s_T3[(byte)(WB >> 24)];
                WH7 ^= (ulong)(s_T0[(byte)(WD)] ^ s_T1[(byte)(WA >> 8)] ^ s_T2[(byte)(WB >> 16)] ^ s_T3[(byte)(WC >> 24)]) << 32;

                WA = s_T0[(byte)(WL8)] ^ s_T1[(byte)(WL8 >> 40)] ^ s_T2[(byte)(WH8 >> 16)] ^ s_T3[(byte)(WH8 >> 56)] ^ (uint)cnt;
                WB = s_T0[(byte)(WL8 >> 32)] ^ s_T1[(byte)(WH8 >> 8)] ^ s_T2[(byte)(WH8 >> 48)] ^ s_T3[(byte)(WL8 >> 24)] ^ (uint)(cnt >> 32);
                WC = s_T0[(byte)(WH8)] ^ s_T1[(byte)(WH8 >> 40)] ^ s_T2[(byte)(WL8 >> 16)] ^ s_T3[(byte)(WL8 >> 56)];
                WD = s_T0[(byte)(WH8 >> 32)] ^ s_T1[(byte)(WL8 >> 8)] ^ s_T2[(byte)(WL8 >> 48)] ^ s_T3[(byte)(WH8 >> 24)];
                cnt++;
                WL8 = s_T0[(byte)(WA)] ^ s_T1[(byte)(WB >> 8)] ^ s_T2[(byte)(WC >> 16)] ^ s_T3[(byte)(WD >> 24)];
                WL8 ^= (ulong)(s_T0[(byte)(WB)] ^ s_T1[(byte)(WC >> 8)] ^ s_T2[(byte)(WD >> 16)] ^ s_T3[(byte)(WA >> 24)]) << 32;
                WH8 = s_T0[(byte)(WC)] ^ s_T1[(byte)(WD >> 8)] ^ s_T2[(byte)(WA >> 16)] ^ s_T3[(byte)(WB >> 24)];
                WH8 ^= (ulong)(s_T0[(byte)(WD)] ^ s_T1[(byte)(WA >> 8)] ^ s_T2[(byte)(WB >> 16)] ^ s_T3[(byte)(WC >> 24)]) << 32;

                WA = s_T0[(byte)(WL9)] ^ s_T1[(byte)(WL9 >> 40)] ^ s_T2[(byte)(WH9 >> 16)] ^ s_T3[(byte)(WH9 >> 56)] ^ (uint)cnt;
                WB = s_T0[(byte)(WL9 >> 32)] ^ s_T1[(byte)(WH9 >> 8)] ^ s_T2[(byte)(WH9 >> 48)] ^ s_T3[(byte)(WL9 >> 24)] ^ (uint)(cnt >> 32);
                WC = s_T0[(byte)(WH9)] ^ s_T1[(byte)(WH9 >> 40)] ^ s_T2[(byte)(WL9 >> 16)] ^ s_T3[(byte)(WL9 >> 56)];
                WD = s_T0[(byte)(WH9 >> 32)] ^ s_T1[(byte)(WL9 >> 8)] ^ s_T2[(byte)(WL9 >> 48)] ^ s_T3[(byte)(WH9 >> 24)];
                cnt++;
                WL9 = s_T0[(byte)(WA)] ^ s_T1[(byte)(WB >> 8)] ^ s_T2[(byte)(WC >> 16)] ^ s_T3[(byte)(WD >> 24)];
                WL9 ^= (ulong)(s_T0[(byte)(WB)] ^ s_T1[(byte)(WC >> 8)] ^ s_T2[(byte)(WD >> 16)] ^ s_T3[(byte)(WA >> 24)]) << 32;
                WH9 = s_T0[(byte)(WC)] ^ s_T1[(byte)(WD >> 8)] ^ s_T2[(byte)(WA >> 16)] ^ s_T3[(byte)(WB >> 24)];
                WH9 ^= (ulong)(s_T0[(byte)(WD)] ^ s_T1[(byte)(WA >> 8)] ^ s_T2[(byte)(WB >> 16)] ^ s_T3[(byte)(WC >> 24)]) << 32;

                WA = s_T0[(byte)(WL10)] ^ s_T1[(byte)(WL10 >> 40)] ^ s_T2[(byte)(WH10 >> 16)] ^ s_T3[(byte)(WH10 >> 56)] ^ (uint)cnt;
                WB = s_T0[(byte)(WL10 >> 32)] ^ s_T1[(byte)(WH10 >> 8)] ^ s_T2[(byte)(WH10 >> 48)] ^ s_T3[(byte)(WL10 >> 24)] ^ (uint)(cnt >> 32);
                WC = s_T0[(byte)(WH10)] ^ s_T1[(byte)(WH10 >> 40)] ^ s_T2[(byte)(WL10 >> 16)] ^ s_T3[(byte)(WL10 >> 56)];
                WD = s_T0[(byte)(WH10 >> 32)] ^ s_T1[(byte)(WL10 >> 8)] ^ s_T2[(byte)(WL10 >> 48)] ^ s_T3[(byte)(WH10 >> 24)];
                cnt++;
                WL10 = s_T0[(byte)(WA)] ^ s_T1[(byte)(WB >> 8)] ^ s_T2[(byte)(WC >> 16)] ^ s_T3[(byte)(WD >> 24)];
                WL10 ^= (ulong)(s_T0[(byte)(WB)] ^ s_T1[(byte)(WC >> 8)] ^ s_T2[(byte)(WD >> 16)] ^ s_T3[(byte)(WA >> 24)]) << 32;
                WH10 = s_T0[(byte)(WC)] ^ s_T1[(byte)(WD >> 8)] ^ s_T2[(byte)(WA >> 16)] ^ s_T3[(byte)(WB >> 24)];
                WH10 ^= (ulong)(s_T0[(byte)(WD)] ^ s_T1[(byte)(WA >> 8)] ^ s_T2[(byte)(WB >> 16)] ^ s_T3[(byte)(WC >> 24)]) << 32;

                WA = s_T0[(byte)(WL11)] ^ s_T1[(byte)(WL11 >> 40)] ^ s_T2[(byte)(WH11 >> 16)] ^ s_T3[(byte)(WH11 >> 56)] ^ (uint)cnt;
                WB = s_T0[(byte)(WL11 >> 32)] ^ s_T1[(byte)(WH11 >> 8)] ^ s_T2[(byte)(WH11 >> 48)] ^ s_T3[(byte)(WL11 >> 24)] ^ (uint)(cnt >> 32);
                WC = s_T0[(byte)(WH11)] ^ s_T1[(byte)(WH11 >> 40)] ^ s_T2[(byte)(WL11 >> 16)] ^ s_T3[(byte)(WL11 >> 56)];
                WD = s_T0[(byte)(WH11 >> 32)] ^ s_T1[(byte)(WL11 >> 8)] ^ s_T2[(byte)(WL11 >> 48)] ^ s_T3[(byte)(WH11 >> 24)];
                cnt++;
                WL11 = s_T0[(byte)(WA)] ^ s_T1[(byte)(WB >> 8)] ^ s_T2[(byte)(WC >> 16)] ^ s_T3[(byte)(WD >> 24)];
                WL11 ^= (ulong)(s_T0[(byte)(WB)] ^ s_T1[(byte)(WC >> 8)] ^ s_T2[(byte)(WD >> 16)] ^ s_T3[(byte)(WA >> 24)]) << 32;
                WH11 = s_T0[(byte)(WC)] ^ s_T1[(byte)(WD >> 8)] ^ s_T2[(byte)(WA >> 16)] ^ s_T3[(byte)(WB >> 24)];
                WH11 ^= (ulong)(s_T0[(byte)(WD)] ^ s_T1[(byte)(WA >> 8)] ^ s_T2[(byte)(WB >> 16)] ^ s_T3[(byte)(WC >> 24)]) << 32;

                WA = s_T0[(byte)(WL12)] ^ s_T1[(byte)(WL12 >> 40)] ^ s_T2[(byte)(WH12 >> 16)] ^ s_T3[(byte)(WH12 >> 56)] ^ (uint)cnt;
                WB = s_T0[(byte)(WL12 >> 32)] ^ s_T1[(byte)(WH12 >> 8)] ^ s_T2[(byte)(WH12 >> 48)] ^ s_T3[(byte)(WL12 >> 24)] ^ (uint)(cnt >> 32);
                WC = s_T0[(byte)(WH12)] ^ s_T1[(byte)(WH12 >> 40)] ^ s_T2[(byte)(WL12 >> 16)] ^ s_T3[(byte)(WL12 >> 56)];
                WD = s_T0[(byte)(WH12 >> 32)] ^ s_T1[(byte)(WL12 >> 8)] ^ s_T2[(byte)(WL12 >> 48)] ^ s_T3[(byte)(WH12 >> 24)];
                cnt++;
                WL12 = s_T0[(byte)(WA)] ^ s_T1[(byte)(WB >> 8)] ^ s_T2[(byte)(WC >> 16)] ^ s_T3[(byte)(WD >> 24)];
                WL12 ^= (ulong)(s_T0[(byte)(WB)] ^ s_T1[(byte)(WC >> 8)] ^ s_T2[(byte)(WD >> 16)] ^ s_T3[(byte)(WA >> 24)]) << 32;
                WH12 = s_T0[(byte)(WC)] ^ s_T1[(byte)(WD >> 8)] ^ s_T2[(byte)(WA >> 16)] ^ s_T3[(byte)(WB >> 24)];
                WH12 ^= (ulong)(s_T0[(byte)(WD)] ^ s_T1[(byte)(WA >> 8)] ^ s_T2[(byte)(WB >> 16)] ^ s_T3[(byte)(WC >> 24)]) << 32;

                WA = s_T0[(byte)(WL13)] ^ s_T1[(byte)(WL13 >> 40)] ^ s_T2[(byte)(WH13 >> 16)] ^ s_T3[(byte)(WH13 >> 56)] ^ (uint)cnt;
                WB = s_T0[(byte)(WL13 >> 32)] ^ s_T1[(byte)(WH13 >> 8)] ^ s_T2[(byte)(WH13 >> 48)] ^ s_T3[(byte)(WL13 >> 24)] ^ (uint)(cnt >> 32);
                WC = s_T0[(byte)(WH13)] ^ s_T1[(byte)(WH13 >> 40)] ^ s_T2[(byte)(WL13 >> 16)] ^ s_T3[(byte)(WL13 >> 56)];
                WD = s_T0[(byte)(WH13 >> 32)] ^ s_T1[(byte)(WL13 >> 8)] ^ s_T2[(byte)(WL13 >> 48)] ^ s_T3[(byte)(WH13 >> 24)];
                cnt++;
                WL13 = s_T0[(byte)(WA)] ^ s_T1[(byte)(WB >> 8)] ^ s_T2[(byte)(WC >> 16)] ^ s_T3[(byte)(WD >> 24)];
                WL13 ^= (ulong)(s_T0[(byte)(WB)] ^ s_T1[(byte)(WC >> 8)] ^ s_T2[(byte)(WD >> 16)] ^ s_T3[(byte)(WA >> 24)]) << 32;
                WH13 = s_T0[(byte)(WC)] ^ s_T1[(byte)(WD >> 8)] ^ s_T2[(byte)(WA >> 16)] ^ s_T3[(byte)(WB >> 24)];
                WH13 ^= (ulong)(s_T0[(byte)(WD)] ^ s_T1[(byte)(WA >> 8)] ^ s_T2[(byte)(WB >> 16)] ^ s_T3[(byte)(WC >> 24)]) << 32;

                WA = s_T0[(byte)(WL14)] ^ s_T1[(byte)(WL14 >> 40)] ^ s_T2[(byte)(WH14 >> 16)] ^ s_T3[(byte)(WH14 >> 56)] ^ (uint)cnt;
                WB = s_T0[(byte)(WL14 >> 32)] ^ s_T1[(byte)(WH14 >> 8)] ^ s_T2[(byte)(WH14 >> 48)] ^ s_T3[(byte)(WL14 >> 24)] ^ (uint)(cnt >> 32);
                WC = s_T0[(byte)(WH14)] ^ s_T1[(byte)(WH14 >> 40)] ^ s_T2[(byte)(WL14 >> 16)] ^ s_T3[(byte)(WL14 >> 56)];
                WD = s_T0[(byte)(WH14 >> 32)] ^ s_T1[(byte)(WL14 >> 8)] ^ s_T2[(byte)(WL14 >> 48)] ^ s_T3[(byte)(WH14 >> 24)];
                cnt++;
                WL14 = s_T0[(byte)(WA)] ^ s_T1[(byte)(WB >> 8)] ^ s_T2[(byte)(WC >> 16)] ^ s_T3[(byte)(WD >> 24)];
                WL14 ^= (ulong)(s_T0[(byte)(WB)] ^ s_T1[(byte)(WC >> 8)] ^ s_T2[(byte)(WD >> 16)] ^ s_T3[(byte)(WA >> 24)]) << 32;
                WH14 = s_T0[(byte)(WC)] ^ s_T1[(byte)(WD >> 8)] ^ s_T2[(byte)(WA >> 16)] ^ s_T3[(byte)(WB >> 24)];
                WH14 ^= (ulong)(s_T0[(byte)(WD)] ^ s_T1[(byte)(WA >> 8)] ^ s_T2[(byte)(WB >> 16)] ^ s_T3[(byte)(WC >> 24)]) << 32;

                WA = s_T0[(byte)(WL15)] ^ s_T1[(byte)(WL15 >> 40)] ^ s_T2[(byte)(WH15 >> 16)] ^ s_T3[(byte)(WH15 >> 56)] ^ (uint)cnt;
                WB = s_T0[(byte)(WL15 >> 32)] ^ s_T1[(byte)(WH15 >> 8)] ^ s_T2[(byte)(WH15 >> 48)] ^ s_T3[(byte)(WL15 >> 24)] ^ (uint)(cnt >> 32);
                WC = s_T0[(byte)(WH15)] ^ s_T1[(byte)(WH15 >> 40)] ^ s_T2[(byte)(WL15 >> 16)] ^ s_T3[(byte)(WL15 >> 56)];
                WD = s_T0[(byte)(WH15 >> 32)] ^ s_T1[(byte)(WL15 >> 8)] ^ s_T2[(byte)(WL15 >> 48)] ^ s_T3[(byte)(WH15 >> 24)];
                cnt++;
                WL15 = s_T0[(byte)(WA)] ^ s_T1[(byte)(WB >> 8)] ^ s_T2[(byte)(WC >> 16)] ^ s_T3[(byte)(WD >> 24)];
                WL15 ^= (ulong)(s_T0[(byte)(WB)] ^ s_T1[(byte)(WC >> 8)] ^ s_T2[(byte)(WD >> 16)] ^ s_T3[(byte)(WA >> 24)]) << 32;
                WH15 = s_T0[(byte)(WC)] ^ s_T1[(byte)(WD >> 8)] ^ s_T2[(byte)(WA >> 16)] ^ s_T3[(byte)(WB >> 24)];
                WH15 ^= (ulong)(s_T0[(byte)(WD)] ^ s_T1[(byte)(WA >> 8)] ^ s_T2[(byte)(WB >> 16)] ^ s_T3[(byte)(WC >> 24)]) << 32;

                ulong WT0 = WL2;
                WL2 = WL10;
                WL10 = WT0;
                ulong WT1 = WH2;
                WH2 = WH10;
                WH10 = WT1;

                WT0 = WL1;
                WL1 = WL5;
                WL5 = WT0;
                WT1 = WH1;
                WH1 = WH5;
                WH5 = WT1;

                WT0 = WL3;
                WL3 = WL15;
                WL15 = WT0;
                WT1 = WH3;
                WH3 = WH15;
                WH15 = WT1;

                WT0 = ((WL0 >> 7) & 0x0101010101010101) * 27;
                WT0 ^= ((WL0 << 1) & 0xFEFEFEFEFEFEFEFE);
                WT1 = ((WL1 >> 7) & 0x0101010101010101) * 27;
                WT1 ^= ((WL1 << 1) & 0xFEFEFEFEFEFEFEFE);
                ulong WT2 = ((WL2 >> 7) & 0x0101010101010101) * 27;
                WT2 ^= ((WL2 << 1) & 0xFEFEFEFEFEFEFEFE);
                ulong WT3 = ((WL3 >> 7) & 0x0101010101010101) * 27;
                WT3 ^= ((WL3 << 1) & 0xFEFEFEFEFEFEFEFE);
                ulong WT4 = WL0 ^ WL1 ^ WL2 ^ WL3;
                WL0 ^= WT0 ^ WT1 ^ WT4;
                WL1 ^= WT1 ^ WT2 ^ WT4;
                WL2 ^= WT2 ^ WT3 ^ WT4;
                WL3 ^= WT0 ^ WT4 ^ WT3;

                WT0 = ((WH0 >> 7) & 0x0101010101010101) * 27;
                WT0 ^= ((WH0 << 1) & 0xFEFEFEFEFEFEFEFE);
                WT1 = ((WH1 >> 7) & 0x0101010101010101) * 27;
                WT1 ^= ((WH1 << 1) & 0xFEFEFEFEFEFEFEFE);
                WT2 = ((WH2 >> 7) & 0x0101010101010101) * 27;
                WT2 ^= ((WH2 << 1) & 0xFEFEFEFEFEFEFEFE);
                WT3 = ((WH3 >> 7) & 0x0101010101010101) * 27;
                WT3 ^= ((WH3 << 1) & 0xFEFEFEFEFEFEFEFE);
                WT4 = WH0 ^ WH1 ^ WH2 ^ WH3;
                WH0 ^= WT0 ^ WT1 ^ WT4;
                WH1 ^= WT1 ^ WT2 ^ WT4;
                WH2 ^= WT2 ^ WT3 ^ WT4;
                WH3 ^= WT0 ^ WT4 ^ WT3;

                WT0 = WL13;
                WL13 = WL9;
                WL9 = WT0;
                WT1 = WH13;
                WH13 = WH9;
                WH9 = WT1;

                WT0 = WL11;
                WL11 = WL7;
                WL7 = WT0;
                WT1 = WH11;
                WH11 = WH7;
                WH7 = WT1;

                WT0 = ((WL8 >> 7) & 0x0101010101010101) * 27;
                WT0 ^= ((WL8 << 1) & 0xFEFEFEFEFEFEFEFE);
                WT1 = ((WL9 >> 7) & 0x0101010101010101) * 27;
                WT1 ^= ((WL9 << 1) & 0xFEFEFEFEFEFEFEFE);
                WT2 = ((WL10 >> 7) & 0x0101010101010101) * 27;
                WT2 ^= ((WL10 << 1) & 0xFEFEFEFEFEFEFEFE);
                WT3 = ((WL11 >> 7) & 0x0101010101010101) * 27;
                WT3 ^= ((WL11 << 1) & 0xFEFEFEFEFEFEFEFE);
                WT4 = WL8 ^ WL9 ^ WL10 ^ WL11;
                WL8 ^= WT0 ^ WT1 ^ WT4;
                WL9 ^= WT1 ^ WT2 ^ WT4;
                WL10 ^= WT2 ^ WT3 ^ WT4;
                WL11 ^= WT0 ^ WT4 ^ WT3;

                WT0 = ((WH8 >> 7) & 0x0101010101010101) * 27;
                WT0 ^= ((WH8 << 1) & 0xFEFEFEFEFEFEFEFE);
                WT1 = ((WH9 >> 7) & 0x0101010101010101) * 27;
                WT1 ^= ((WH9 << 1) & 0xFEFEFEFEFEFEFEFE);
                WT2 = ((WH10 >> 7) & 0x0101010101010101) * 27;
                WT2 ^= ((WH10 << 1) & 0xFEFEFEFEFEFEFEFE);
                WT3 = ((WH11 >> 7) & 0x0101010101010101) * 27;
                WT3 ^= ((WH11 << 1) & 0xFEFEFEFEFEFEFEFE);
                WT4 = WH8 ^ WH9 ^ WH10 ^ WH11;
                WH8 ^= WT0 ^ WT1 ^ WT4;
                WH9 ^= WT1 ^ WT2 ^ WT4;
                WH10 ^= WT2 ^ WT3 ^ WT4;
                WH11 ^= WT0 ^ WT4 ^ WT3;

                WT0 = WL6;
                WL6 = WL14;
                WL14 = WT0;
                WT1 = WH6;
                WH6 = WH14;
                WH14 = WT1;

                WT0 = WL13;
                WL13 = WL5;
                WL5 = WT0;
                WT1 = WH13;
                WH13 = WH5;
                WH5 = WT1;

                WT0 = WL15;
                WL15 = WL7;
                WL7 = WT0;
                WT1 = WH15;
                WH15 = WH7;
                WH7 = WT1;

                WT0 = ((WL4 >> 7) & 0x0101010101010101) * 27;
                WT0 ^= ((WL4 << 1) & 0xFEFEFEFEFEFEFEFE);
                WT1 = ((WL5 >> 7) & 0x0101010101010101) * 27;
                WT1 ^= ((WL5 << 1) & 0xFEFEFEFEFEFEFEFE);
                WT2 = ((WL6 >> 7) & 0x0101010101010101) * 27;
                WT2 ^= ((WL6 << 1) & 0xFEFEFEFEFEFEFEFE);
                WT3 = ((WL7 >> 7) & 0x0101010101010101) * 27;
                WT3 ^= ((WL7 << 1) & 0xFEFEFEFEFEFEFEFE);
                WT4 = WL4 ^ WL5 ^ WL6 ^ WL7;
                WL4 ^= WT0 ^ WT1 ^ WT4;
                WL5 ^= WT1 ^ WT2 ^ WT4;
                WL6 ^= WT2 ^ WT3 ^ WT4;
                WL7 ^= WT0 ^ WT4 ^ WT3;

                WT0 = ((WH4 >> 7) & 0x0101010101010101) * 27;
                WT0 ^= ((WH4 << 1) & 0xFEFEFEFEFEFEFEFE);
                WT1 = ((WH5 >> 7) & 0x0101010101010101) * 27;
                WT1 ^= ((WH5 << 1) & 0xFEFEFEFEFEFEFEFE);
                WT2 = ((WH6 >> 7) & 0x0101010101010101) * 27;
                WT2 ^= ((WH6 << 1) & 0xFEFEFEFEFEFEFEFE);
                WT3 = ((WH7 >> 7) & 0x0101010101010101) * 27;
                WT3 ^= ((WH7 << 1) & 0xFEFEFEFEFEFEFEFE);
                WT4 = WH4 ^ WH5 ^ WH6 ^ WH7;
                WH4 ^= WT0 ^ WT1 ^ WT4;
                WH5 ^= WT1 ^ WT2 ^ WT4;
                WH6 ^= WT2 ^ WT3 ^ WT4;
                WH7 ^= WT0 ^ WT4 ^ WT3;

                WT0 = ((WL12 >> 7) & 0x0101010101010101) * 27;
                WT0 ^= ((WL12 << 1) & 0xFEFEFEFEFEFEFEFE);
                WT1 = ((WL13 >> 7) & 0x0101010101010101) * 27;
                WT1 ^= ((WL13 << 1) & 0xFEFEFEFEFEFEFEFE);
                WT2 = ((WL14 >> 7) & 0x0101010101010101) * 27;
                WT2 ^= ((WL14 << 1) & 0xFEFEFEFEFEFEFEFE);
                WT3 = ((WL15 >> 7) & 0x0101010101010101) * 27;
                WT3 ^= ((WL15 << 1) & 0xFEFEFEFEFEFEFEFE);
                WT4 = WL12 ^ WL13 ^ WL14 ^ WL15;
                WL12 ^= WT0 ^ WT1 ^ WT4;
                WL13 ^= WT1 ^ WT2 ^ WT4;
                WL14 ^= WT2 ^ WT3 ^ WT4;
                WL15 ^= WT0 ^ WT4 ^ WT3;

                WT0 = ((WH12 >> 7) & 0x0101010101010101) * 27;
                WT0 ^= ((WH12 << 1) & 0xFEFEFEFEFEFEFEFE);
                WT1 = ((WH13 >> 7) & 0x0101010101010101) * 27;
                WT1 ^= ((WH13 << 1) & 0xFEFEFEFEFEFEFEFE);
                WT2 = ((WH14 >> 7) & 0x0101010101010101) * 27;
                WT2 ^= ((WH14 << 1) & 0xFEFEFEFEFEFEFEFE);
                WT3 = ((WH15 >> 7) & 0x0101010101010101) * 27;
                WT3 ^= ((WH15 << 1) & 0xFEFEFEFEFEFEFEFE);
                WT4 = WH12 ^ WH13 ^ WH14 ^ WH15;
                WH12 ^= WT0 ^ WT1 ^ WT4;
                WH13 ^= WT1 ^ WT2 ^ WT4;
                WH14 ^= WT2 ^ WT3 ^ WT4;
                WH15 ^= WT0 ^ WT4 ^ WT3;

                r--;
            }
            while (r > 0);

            WL8 ^= WL12 ^ WL4 ^ WL0;
            WH8 ^= WH12 ^ WH4 ^ WH0;
            WL9 ^= WL13 ^ WL5 ^ WL1;
            WH9 ^= WH13 ^ WH5 ^ WH1;
            WL10 ^= WL14 ^ WL6 ^ WL2;
            WH10 ^= WH14 ^ WH6 ^ WH2;
            WL11 ^= WL15 ^ WL7 ^ WL3;
            WH11 ^= WH15 ^ WH7 ^ WH3;
            m_state[0] = cv[0] ^ WL8;
            m_state[1] = cv[1] ^ WH8;
            m_state[2] = cv[2] ^ WL9;
            m_state[3] = cv[3] ^ WH9;
            m_state[4] = cv[4] ^ WL10;
            m_state[5] = cv[5] ^ WH10;
            m_state[6] = cv[6] ^ WL11;
            m_state[7] = cv[7] ^ WH11;
        }

        public override void Initialize()
        {
            base.Initialize();

            for (int i = 0; i < 4; i++)
                m_state[2 * i] = (ulong)HashSize * 8;
        }
    };

    internal abstract class Echo512Base : EchoBase
    {
        public Echo512Base(HashSize a_hash_size)
            : base(a_hash_size, 128)
        {
        }

        protected override void TransformBlock(byte[] a_data, int a_index)
        {
            Converters.ConvertBytesToULongs(a_data, a_index, BlockSize, m_state, 16);

            uint WA, WB, WC, WD;

            ulong[] cv = new ulong[16];

            cv[0] = m_state[0] ^ m_state[16];
            cv[1] = m_state[1] ^ m_state[17];
            cv[2] = m_state[2] ^ m_state[18];
            cv[3] = m_state[3] ^ m_state[19];
            cv[4] = m_state[4] ^ m_state[20];
            cv[5] = m_state[5] ^ m_state[21];
            cv[6] = m_state[6] ^ m_state[22];
            cv[7] = m_state[7] ^ m_state[23];
            cv[8] = m_state[8] ^ m_state[24];
            cv[9] = m_state[9] ^ m_state[25];
            cv[10] = m_state[10] ^ m_state[26];
            cv[11] = m_state[11] ^ m_state[27];
            cv[12] = m_state[12] ^ m_state[28];
            cv[13] = m_state[13] ^ m_state[29];
            cv[14] = m_state[14] ^ m_state[30];
            cv[15] = m_state[15] ^ m_state[31];

            ulong WL0 = m_state[0];
            ulong WH0 = m_state[1];
            ulong WL1 = m_state[2];
            ulong WH1 = m_state[3];
            ulong WL2 = m_state[4];
            ulong WH2 = m_state[5];
            ulong WL3 = m_state[6];
            ulong WH3 = m_state[7];
            ulong WL4 = m_state[8];
            ulong WH4 = m_state[9];
            ulong WL5 = m_state[10];
            ulong WH5 = m_state[11];
            ulong WL6 = m_state[12];
            ulong WH6 = m_state[13];
            ulong WL7 = m_state[14];
            ulong WH7 = m_state[15];
            ulong WL8 = m_state[16];
            ulong WH8 = m_state[17];
            ulong WL9 = m_state[18];
            ulong WH9 = m_state[19];
            ulong WL10 = m_state[20];
            ulong WH10 = m_state[21];
            ulong WL11 = m_state[22];
            ulong WH11 = m_state[23];
            ulong WL12 = m_state[24];
            ulong WH12 = m_state[25];
            ulong WL13 = m_state[26];
            ulong WH13 = m_state[27];
            ulong WL14 = m_state[28];
            ulong WH14 = m_state[29];
            ulong WL15 = m_state[30];
            ulong WH15 = m_state[31];

            int r = 10;

            ulong cnt = 0;
            if (!m_last_block)
                cnt = m_processed_bytes * 8;

            do
            {
                WA = s_T0[(byte)(WL0)] ^ s_T1[(byte)(WL0 >> 40)] ^ s_T2[(byte)(WH0 >> 16)] ^ s_T3[(byte)(WH0 >> 56)] ^ (uint)cnt;
                WB = s_T0[(byte)(WL0 >> 32)] ^ s_T1[(byte)(WH0 >> 8)] ^ s_T2[(byte)(WH0 >> 48)] ^ s_T3[(byte)(WL0 >> 24)] ^ (uint)(cnt >> 32);
                WC = s_T0[(byte)(WH0)] ^ s_T1[(byte)(WH0 >> 40)] ^ s_T2[(byte)(WL0 >> 16)] ^ s_T3[(byte)(WL0 >> 56)];
                WD = s_T0[(byte)(WH0 >> 32)] ^ s_T1[(byte)(WL0 >> 8)] ^ s_T2[(byte)(WL0 >> 48)] ^ s_T3[(byte)(WH0 >> 24)];
                cnt++;
                WL0 = s_T0[(byte)(WA)] ^ s_T1[(byte)(WB >> 8)] ^ s_T2[(byte)(WC >> 16)] ^ s_T3[(byte)(WD >> 24)];
                WL0 ^= (ulong)(s_T0[(byte)(WB)] ^ s_T1[(byte)(WC >> 8)] ^ s_T2[(byte)(WD >> 16)] ^ s_T3[(byte)(WA >> 24)]) << 32;
                WH0 = s_T0[(byte)(WC)] ^ s_T1[(byte)(WD >> 8)] ^ s_T2[(byte)(WA >> 16)] ^ s_T3[(byte)(WB >> 24)];
                WH0 ^= (ulong)(s_T0[(byte)(WD)] ^ s_T1[(byte)(WA >> 8)] ^ s_T2[(byte)(WB >> 16)] ^ s_T3[(byte)(WC >> 24)]) << 32;

                WA = s_T0[(byte)(WL1)] ^ s_T1[(byte)(WL1 >> 40)] ^ s_T2[(byte)(WH1 >> 16)] ^ s_T3[(byte)(WH1 >> 56)] ^ (uint)cnt;
                WB = s_T0[(byte)(WL1 >> 32)] ^ s_T1[(byte)(WH1 >> 8)] ^ s_T2[(byte)(WH1 >> 48)] ^ s_T3[(byte)(WL1 >> 24)] ^ (uint)(cnt >> 32);
                WC = s_T0[(byte)(WH1)] ^ s_T1[(byte)(WH1 >> 40)] ^ s_T2[(byte)(WL1 >> 16)] ^ s_T3[(byte)(WL1 >> 56)];
                WD = s_T0[(byte)(WH1 >> 32)] ^ s_T1[(byte)(WL1 >> 8)] ^ s_T2[(byte)(WL1 >> 48)] ^ s_T3[(byte)(WH1 >> 24)];
                cnt++;
                WL1 = s_T0[(byte)(WA)] ^ s_T1[(byte)(WB >> 8)] ^ s_T2[(byte)(WC >> 16)] ^ s_T3[(byte)(WD >> 24)];
                WL1 ^= (ulong)(s_T0[(byte)(WB)] ^ s_T1[(byte)(WC >> 8)] ^ s_T2[(byte)(WD >> 16)] ^ s_T3[(byte)(WA >> 24)]) << 32;
                WH1 = s_T0[(byte)(WC)] ^ s_T1[(byte)(WD >> 8)] ^ s_T2[(byte)(WA >> 16)] ^ s_T3[(byte)(WB >> 24)];
                WH1 ^= (ulong)(s_T0[(byte)(WD)] ^ s_T1[(byte)(WA >> 8)] ^ s_T2[(byte)(WB >> 16)] ^ s_T3[(byte)(WC >> 24)]) << 32;

                WA = s_T0[(byte)(WL2)] ^ s_T1[(byte)(WL2 >> 40)] ^ s_T2[(byte)(WH2 >> 16)] ^ s_T3[(byte)(WH2 >> 56)] ^ (uint)cnt;
                WB = s_T0[(byte)(WL2 >> 32)] ^ s_T1[(byte)(WH2 >> 8)] ^ s_T2[(byte)(WH2 >> 48)] ^ s_T3[(byte)(WL2 >> 24)] ^ (uint)(cnt >> 32);
                WC = s_T0[(byte)(WH2)] ^ s_T1[(byte)(WH2 >> 40)] ^ s_T2[(byte)(WL2 >> 16)] ^ s_T3[(byte)(WL2 >> 56)];
                WD = s_T0[(byte)(WH2 >> 32)] ^ s_T1[(byte)(WL2 >> 8)] ^ s_T2[(byte)(WL2 >> 48)] ^ s_T3[(byte)(WH2 >> 24)];
                cnt++;
                WL2 = s_T0[(byte)(WA)] ^ s_T1[(byte)(WB >> 8)] ^ s_T2[(byte)(WC >> 16)] ^ s_T3[(byte)(WD >> 24)];
                WL2 ^= (ulong)(s_T0[(byte)(WB)] ^ s_T1[(byte)(WC >> 8)] ^ s_T2[(byte)(WD >> 16)] ^ s_T3[(byte)(WA >> 24)]) << 32;
                WH2 = s_T0[(byte)(WC)] ^ s_T1[(byte)(WD >> 8)] ^ s_T2[(byte)(WA >> 16)] ^ s_T3[(byte)(WB >> 24)];
                WH2 ^= (ulong)(s_T0[(byte)(WD)] ^ s_T1[(byte)(WA >> 8)] ^ s_T2[(byte)(WB >> 16)] ^ s_T3[(byte)(WC >> 24)]) << 32;

                WA = s_T0[(byte)(WL3)] ^ s_T1[(byte)(WL3 >> 40)] ^ s_T2[(byte)(WH3 >> 16)] ^ s_T3[(byte)(WH3 >> 56)] ^ (uint)cnt;
                WB = s_T0[(byte)(WL3 >> 32)] ^ s_T1[(byte)(WH3 >> 8)] ^ s_T2[(byte)(WH3 >> 48)] ^ s_T3[(byte)(WL3 >> 24)] ^ (uint)(cnt >> 32);
                WC = s_T0[(byte)(WH3)] ^ s_T1[(byte)(WH3 >> 40)] ^ s_T2[(byte)(WL3 >> 16)] ^ s_T3[(byte)(WL3 >> 56)];
                WD = s_T0[(byte)(WH3 >> 32)] ^ s_T1[(byte)(WL3 >> 8)] ^ s_T2[(byte)(WL3 >> 48)] ^ s_T3[(byte)(WH3 >> 24)];
                cnt++;
                WL3 = s_T0[(byte)(WA)] ^ s_T1[(byte)(WB >> 8)] ^ s_T2[(byte)(WC >> 16)] ^ s_T3[(byte)(WD >> 24)];
                WL3 ^= (ulong)(s_T0[(byte)(WB)] ^ s_T1[(byte)(WC >> 8)] ^ s_T2[(byte)(WD >> 16)] ^ s_T3[(byte)(WA >> 24)]) << 32;
                WH3 = s_T0[(byte)(WC)] ^ s_T1[(byte)(WD >> 8)] ^ s_T2[(byte)(WA >> 16)] ^ s_T3[(byte)(WB >> 24)];
                WH3 ^= (ulong)(s_T0[(byte)(WD)] ^ s_T1[(byte)(WA >> 8)] ^ s_T2[(byte)(WB >> 16)] ^ s_T3[(byte)(WC >> 24)]) << 32;

                WA = s_T0[(byte)(WL4)] ^ s_T1[(byte)(WL4 >> 40)] ^ s_T2[(byte)(WH4 >> 16)] ^ s_T3[(byte)(WH4 >> 56)] ^ (uint)cnt;
                WB = s_T0[(byte)(WL4 >> 32)] ^ s_T1[(byte)(WH4 >> 8)] ^ s_T2[(byte)(WH4 >> 48)] ^ s_T3[(byte)(WL4 >> 24)] ^ (uint)(cnt >> 32);
                WC = s_T0[(byte)(WH4)] ^ s_T1[(byte)(WH4 >> 40)] ^ s_T2[(byte)(WL4 >> 16)] ^ s_T3[(byte)(WL4 >> 56)];
                WD = s_T0[(byte)(WH4 >> 32)] ^ s_T1[(byte)(WL4 >> 8)] ^ s_T2[(byte)(WL4 >> 48)] ^ s_T3[(byte)(WH4 >> 24)];
                cnt++;
                WL4 = s_T0[(byte)(WA)] ^ s_T1[(byte)(WB >> 8)] ^ s_T2[(byte)(WC >> 16)] ^ s_T3[(byte)(WD >> 24)];
                WL4 ^= (ulong)(s_T0[(byte)(WB)] ^ s_T1[(byte)(WC >> 8)] ^ s_T2[(byte)(WD >> 16)] ^ s_T3[(byte)(WA >> 24)]) << 32;
                WH4 = s_T0[(byte)(WC)] ^ s_T1[(byte)(WD >> 8)] ^ s_T2[(byte)(WA >> 16)] ^ s_T3[(byte)(WB >> 24)];
                WH4 ^= (ulong)(s_T0[(byte)(WD)] ^ s_T1[(byte)(WA >> 8)] ^ s_T2[(byte)(WB >> 16)] ^ s_T3[(byte)(WC >> 24)]) << 32;

                WA = s_T0[(byte)(WL5)] ^ s_T1[(byte)(WL5 >> 40)] ^ s_T2[(byte)(WH5 >> 16)] ^ s_T3[(byte)(WH5 >> 56)] ^ (uint)cnt;
                WB = s_T0[(byte)(WL5 >> 32)] ^ s_T1[(byte)(WH5 >> 8)] ^ s_T2[(byte)(WH5 >> 48)] ^ s_T3[(byte)(WL5 >> 24)] ^ (uint)(cnt >> 32);
                WC = s_T0[(byte)(WH5)] ^ s_T1[(byte)(WH5 >> 40)] ^ s_T2[(byte)(WL5 >> 16)] ^ s_T3[(byte)(WL5 >> 56)];
                WD = s_T0[(byte)(WH5 >> 32)] ^ s_T1[(byte)(WL5 >> 8)] ^ s_T2[(byte)(WL5 >> 48)] ^ s_T3[(byte)(WH5 >> 24)];
                cnt++;
                WL5 = s_T0[(byte)(WA)] ^ s_T1[(byte)(WB >> 8)] ^ s_T2[(byte)(WC >> 16)] ^ s_T3[(byte)(WD >> 24)];
                WL5 ^= (ulong)(s_T0[(byte)(WB)] ^ s_T1[(byte)(WC >> 8)] ^ s_T2[(byte)(WD >> 16)] ^ s_T3[(byte)(WA >> 24)]) << 32;
                WH5 = s_T0[(byte)(WC)] ^ s_T1[(byte)(WD >> 8)] ^ s_T2[(byte)(WA >> 16)] ^ s_T3[(byte)(WB >> 24)];
                WH5 ^= (ulong)(s_T0[(byte)(WD)] ^ s_T1[(byte)(WA >> 8)] ^ s_T2[(byte)(WB >> 16)] ^ s_T3[(byte)(WC >> 24)]) << 32;

                WA = s_T0[(byte)(WL6)] ^ s_T1[(byte)(WL6 >> 40)] ^ s_T2[(byte)(WH6 >> 16)] ^ s_T3[(byte)(WH6 >> 56)] ^ (uint)cnt;
                WB = s_T0[(byte)(WL6 >> 32)] ^ s_T1[(byte)(WH6 >> 8)] ^ s_T2[(byte)(WH6 >> 48)] ^ s_T3[(byte)(WL6 >> 24)] ^ (uint)(cnt >> 32);
                WC = s_T0[(byte)(WH6)] ^ s_T1[(byte)(WH6 >> 40)] ^ s_T2[(byte)(WL6 >> 16)] ^ s_T3[(byte)(WL6 >> 56)];
                WD = s_T0[(byte)(WH6 >> 32)] ^ s_T1[(byte)(WL6 >> 8)] ^ s_T2[(byte)(WL6 >> 48)] ^ s_T3[(byte)(WH6 >> 24)];
                cnt++;
                WL6 = s_T0[(byte)(WA)] ^ s_T1[(byte)(WB >> 8)] ^ s_T2[(byte)(WC >> 16)] ^ s_T3[(byte)(WD >> 24)];
                WL6 ^= (ulong)(s_T0[(byte)(WB)] ^ s_T1[(byte)(WC >> 8)] ^ s_T2[(byte)(WD >> 16)] ^ s_T3[(byte)(WA >> 24)]) << 32;
                WH6 = s_T0[(byte)(WC)] ^ s_T1[(byte)(WD >> 8)] ^ s_T2[(byte)(WA >> 16)] ^ s_T3[(byte)(WB >> 24)];
                WH6 ^= (ulong)(s_T0[(byte)(WD)] ^ s_T1[(byte)(WA >> 8)] ^ s_T2[(byte)(WB >> 16)] ^ s_T3[(byte)(WC >> 24)]) << 32;

                WA = s_T0[(byte)(WL7)] ^ s_T1[(byte)(WL7 >> 40)] ^ s_T2[(byte)(WH7 >> 16)] ^ s_T3[(byte)(WH7 >> 56)] ^ (uint)cnt;
                WB = s_T0[(byte)(WL7 >> 32)] ^ s_T1[(byte)(WH7 >> 8)] ^ s_T2[(byte)(WH7 >> 48)] ^ s_T3[(byte)(WL7 >> 24)] ^ (uint)(cnt >> 32);
                WC = s_T0[(byte)(WH7)] ^ s_T1[(byte)(WH7 >> 40)] ^ s_T2[(byte)(WL7 >> 16)] ^ s_T3[(byte)(WL7 >> 56)];
                WD = s_T0[(byte)(WH7 >> 32)] ^ s_T1[(byte)(WL7 >> 8)] ^ s_T2[(byte)(WL7 >> 48)] ^ s_T3[(byte)(WH7 >> 24)];
                cnt++;
                WL7 = s_T0[(byte)(WA)] ^ s_T1[(byte)(WB >> 8)] ^ s_T2[(byte)(WC >> 16)] ^ s_T3[(byte)(WD >> 24)];
                WL7 ^= (ulong)(s_T0[(byte)(WB)] ^ s_T1[(byte)(WC >> 8)] ^ s_T2[(byte)(WD >> 16)] ^ s_T3[(byte)(WA >> 24)]) << 32;
                WH7 = s_T0[(byte)(WC)] ^ s_T1[(byte)(WD >> 8)] ^ s_T2[(byte)(WA >> 16)] ^ s_T3[(byte)(WB >> 24)];
                WH7 ^= (ulong)(s_T0[(byte)(WD)] ^ s_T1[(byte)(WA >> 8)] ^ s_T2[(byte)(WB >> 16)] ^ s_T3[(byte)(WC >> 24)]) << 32;

                WA = s_T0[(byte)(WL8)] ^ s_T1[(byte)(WL8 >> 40)] ^ s_T2[(byte)(WH8 >> 16)] ^ s_T3[(byte)(WH8 >> 56)] ^ (uint)cnt;
                WB = s_T0[(byte)(WL8 >> 32)] ^ s_T1[(byte)(WH8 >> 8)] ^ s_T2[(byte)(WH8 >> 48)] ^ s_T3[(byte)(WL8 >> 24)] ^ (uint)(cnt >> 32);
                WC = s_T0[(byte)(WH8)] ^ s_T1[(byte)(WH8 >> 40)] ^ s_T2[(byte)(WL8 >> 16)] ^ s_T3[(byte)(WL8 >> 56)];
                WD = s_T0[(byte)(WH8 >> 32)] ^ s_T1[(byte)(WL8 >> 8)] ^ s_T2[(byte)(WL8 >> 48)] ^ s_T3[(byte)(WH8 >> 24)];
                cnt++;
                WL8 = s_T0[(byte)(WA)] ^ s_T1[(byte)(WB >> 8)] ^ s_T2[(byte)(WC >> 16)] ^ s_T3[(byte)(WD >> 24)];
                WL8 ^= (ulong)(s_T0[(byte)(WB)] ^ s_T1[(byte)(WC >> 8)] ^ s_T2[(byte)(WD >> 16)] ^ s_T3[(byte)(WA >> 24)]) << 32;
                WH8 = s_T0[(byte)(WC)] ^ s_T1[(byte)(WD >> 8)] ^ s_T2[(byte)(WA >> 16)] ^ s_T3[(byte)(WB >> 24)];
                WH8 ^= (ulong)(s_T0[(byte)(WD)] ^ s_T1[(byte)(WA >> 8)] ^ s_T2[(byte)(WB >> 16)] ^ s_T3[(byte)(WC >> 24)]) << 32;

                WA = s_T0[(byte)(WL9)] ^ s_T1[(byte)(WL9 >> 40)] ^ s_T2[(byte)(WH9 >> 16)] ^ s_T3[(byte)(WH9 >> 56)] ^ (uint)cnt;
                WB = s_T0[(byte)(WL9 >> 32)] ^ s_T1[(byte)(WH9 >> 8)] ^ s_T2[(byte)(WH9 >> 48)] ^ s_T3[(byte)(WL9 >> 24)] ^ (uint)(cnt >> 32);
                WC = s_T0[(byte)(WH9)] ^ s_T1[(byte)(WH9 >> 40)] ^ s_T2[(byte)(WL9 >> 16)] ^ s_T3[(byte)(WL9 >> 56)];
                WD = s_T0[(byte)(WH9 >> 32)] ^ s_T1[(byte)(WL9 >> 8)] ^ s_T2[(byte)(WL9 >> 48)] ^ s_T3[(byte)(WH9 >> 24)];
                cnt++;
                WL9 = s_T0[(byte)(WA)] ^ s_T1[(byte)(WB >> 8)] ^ s_T2[(byte)(WC >> 16)] ^ s_T3[(byte)(WD >> 24)];
                WL9 ^= (ulong)(s_T0[(byte)(WB)] ^ s_T1[(byte)(WC >> 8)] ^ s_T2[(byte)(WD >> 16)] ^ s_T3[(byte)(WA >> 24)]) << 32;
                WH9 = s_T0[(byte)(WC)] ^ s_T1[(byte)(WD >> 8)] ^ s_T2[(byte)(WA >> 16)] ^ s_T3[(byte)(WB >> 24)];
                WH9 ^= (ulong)(s_T0[(byte)(WD)] ^ s_T1[(byte)(WA >> 8)] ^ s_T2[(byte)(WB >> 16)] ^ s_T3[(byte)(WC >> 24)]) << 32;

                WA = s_T0[(byte)(WL10)] ^ s_T1[(byte)(WL10 >> 40)] ^ s_T2[(byte)(WH10 >> 16)] ^ s_T3[(byte)(WH10 >> 56)] ^ (uint)cnt;
                WB = s_T0[(byte)(WL10 >> 32)] ^ s_T1[(byte)(WH10 >> 8)] ^ s_T2[(byte)(WH10 >> 48)] ^ s_T3[(byte)(WL10 >> 24)] ^ (uint)(cnt >> 32);
                WC = s_T0[(byte)(WH10)] ^ s_T1[(byte)(WH10 >> 40)] ^ s_T2[(byte)(WL10 >> 16)] ^ s_T3[(byte)(WL10 >> 56)];
                WD = s_T0[(byte)(WH10 >> 32)] ^ s_T1[(byte)(WL10 >> 8)] ^ s_T2[(byte)(WL10 >> 48)] ^ s_T3[(byte)(WH10 >> 24)];
                cnt++;
                WL10 = s_T0[(byte)(WA)] ^ s_T1[(byte)(WB >> 8)] ^ s_T2[(byte)(WC >> 16)] ^ s_T3[(byte)(WD >> 24)];
                WL10 ^= (ulong)(s_T0[(byte)(WB)] ^ s_T1[(byte)(WC >> 8)] ^ s_T2[(byte)(WD >> 16)] ^ s_T3[(byte)(WA >> 24)]) << 32;
                WH10 = s_T0[(byte)(WC)] ^ s_T1[(byte)(WD >> 8)] ^ s_T2[(byte)(WA >> 16)] ^ s_T3[(byte)(WB >> 24)];
                WH10 ^= (ulong)(s_T0[(byte)(WD)] ^ s_T1[(byte)(WA >> 8)] ^ s_T2[(byte)(WB >> 16)] ^ s_T3[(byte)(WC >> 24)]) << 32;

                WA = s_T0[(byte)(WL11)] ^ s_T1[(byte)(WL11 >> 40)] ^ s_T2[(byte)(WH11 >> 16)] ^ s_T3[(byte)(WH11 >> 56)] ^ (uint)cnt;
                WB = s_T0[(byte)(WL11 >> 32)] ^ s_T1[(byte)(WH11 >> 8)] ^ s_T2[(byte)(WH11 >> 48)] ^ s_T3[(byte)(WL11 >> 24)] ^ (uint)(cnt >> 32);
                WC = s_T0[(byte)(WH11)] ^ s_T1[(byte)(WH11 >> 40)] ^ s_T2[(byte)(WL11 >> 16)] ^ s_T3[(byte)(WL11 >> 56)];
                WD = s_T0[(byte)(WH11 >> 32)] ^ s_T1[(byte)(WL11 >> 8)] ^ s_T2[(byte)(WL11 >> 48)] ^ s_T3[(byte)(WH11 >> 24)];
                cnt++;
                WL11 = s_T0[(byte)(WA)] ^ s_T1[(byte)(WB >> 8)] ^ s_T2[(byte)(WC >> 16)] ^ s_T3[(byte)(WD >> 24)];
                WL11 ^= (ulong)(s_T0[(byte)(WB)] ^ s_T1[(byte)(WC >> 8)] ^ s_T2[(byte)(WD >> 16)] ^ s_T3[(byte)(WA >> 24)]) << 32;
                WH11 = s_T0[(byte)(WC)] ^ s_T1[(byte)(WD >> 8)] ^ s_T2[(byte)(WA >> 16)] ^ s_T3[(byte)(WB >> 24)];
                WH11 ^= (ulong)(s_T0[(byte)(WD)] ^ s_T1[(byte)(WA >> 8)] ^ s_T2[(byte)(WB >> 16)] ^ s_T3[(byte)(WC >> 24)]) << 32;

                WA = s_T0[(byte)(WL12)] ^ s_T1[(byte)(WL12 >> 40)] ^ s_T2[(byte)(WH12 >> 16)] ^ s_T3[(byte)(WH12 >> 56)] ^ (uint)cnt;
                WB = s_T0[(byte)(WL12 >> 32)] ^ s_T1[(byte)(WH12 >> 8)] ^ s_T2[(byte)(WH12 >> 48)] ^ s_T3[(byte)(WL12 >> 24)] ^ (uint)(cnt >> 32);
                WC = s_T0[(byte)(WH12)] ^ s_T1[(byte)(WH12 >> 40)] ^ s_T2[(byte)(WL12 >> 16)] ^ s_T3[(byte)(WL12 >> 56)];
                WD = s_T0[(byte)(WH12 >> 32)] ^ s_T1[(byte)(WL12 >> 8)] ^ s_T2[(byte)(WL12 >> 48)] ^ s_T3[(byte)(WH12 >> 24)];
                cnt++;
                WL12 = s_T0[(byte)(WA)] ^ s_T1[(byte)(WB >> 8)] ^ s_T2[(byte)(WC >> 16)] ^ s_T3[(byte)(WD >> 24)];
                WL12 ^= (ulong)(s_T0[(byte)(WB)] ^ s_T1[(byte)(WC >> 8)] ^ s_T2[(byte)(WD >> 16)] ^ s_T3[(byte)(WA >> 24)]) << 32;
                WH12 = s_T0[(byte)(WC)] ^ s_T1[(byte)(WD >> 8)] ^ s_T2[(byte)(WA >> 16)] ^ s_T3[(byte)(WB >> 24)];
                WH12 ^= (ulong)(s_T0[(byte)(WD)] ^ s_T1[(byte)(WA >> 8)] ^ s_T2[(byte)(WB >> 16)] ^ s_T3[(byte)(WC >> 24)]) << 32;

                WA = s_T0[(byte)(WL13)] ^ s_T1[(byte)(WL13 >> 40)] ^ s_T2[(byte)(WH13 >> 16)] ^ s_T3[(byte)(WH13 >> 56)] ^ (uint)cnt;
                WB = s_T0[(byte)(WL13 >> 32)] ^ s_T1[(byte)(WH13 >> 8)] ^ s_T2[(byte)(WH13 >> 48)] ^ s_T3[(byte)(WL13 >> 24)] ^ (uint)(cnt >> 32);
                WC = s_T0[(byte)(WH13)] ^ s_T1[(byte)(WH13 >> 40)] ^ s_T2[(byte)(WL13 >> 16)] ^ s_T3[(byte)(WL13 >> 56)];
                WD = s_T0[(byte)(WH13 >> 32)] ^ s_T1[(byte)(WL13 >> 8)] ^ s_T2[(byte)(WL13 >> 48)] ^ s_T3[(byte)(WH13 >> 24)];
                cnt++;
                WL13 = s_T0[(byte)(WA)] ^ s_T1[(byte)(WB >> 8)] ^ s_T2[(byte)(WC >> 16)] ^ s_T3[(byte)(WD >> 24)];
                WL13 ^= (ulong)(s_T0[(byte)(WB)] ^ s_T1[(byte)(WC >> 8)] ^ s_T2[(byte)(WD >> 16)] ^ s_T3[(byte)(WA >> 24)]) << 32;
                WH13 = s_T0[(byte)(WC)] ^ s_T1[(byte)(WD >> 8)] ^ s_T2[(byte)(WA >> 16)] ^ s_T3[(byte)(WB >> 24)];
                WH13 ^= (ulong)(s_T0[(byte)(WD)] ^ s_T1[(byte)(WA >> 8)] ^ s_T2[(byte)(WB >> 16)] ^ s_T3[(byte)(WC >> 24)]) << 32;

                WA = s_T0[(byte)(WL14)] ^ s_T1[(byte)(WL14 >> 40)] ^ s_T2[(byte)(WH14 >> 16)] ^ s_T3[(byte)(WH14 >> 56)] ^ (uint)cnt;
                WB = s_T0[(byte)(WL14 >> 32)] ^ s_T1[(byte)(WH14 >> 8)] ^ s_T2[(byte)(WH14 >> 48)] ^ s_T3[(byte)(WL14 >> 24)] ^ (uint)(cnt >> 32);
                WC = s_T0[(byte)(WH14)] ^ s_T1[(byte)(WH14 >> 40)] ^ s_T2[(byte)(WL14 >> 16)] ^ s_T3[(byte)(WL14 >> 56)];
                WD = s_T0[(byte)(WH14 >> 32)] ^ s_T1[(byte)(WL14 >> 8)] ^ s_T2[(byte)(WL14 >> 48)] ^ s_T3[(byte)(WH14 >> 24)];
                cnt++;
                WL14 = s_T0[(byte)(WA)] ^ s_T1[(byte)(WB >> 8)] ^ s_T2[(byte)(WC >> 16)] ^ s_T3[(byte)(WD >> 24)];
                WL14 ^= (ulong)(s_T0[(byte)(WB)] ^ s_T1[(byte)(WC >> 8)] ^ s_T2[(byte)(WD >> 16)] ^ s_T3[(byte)(WA >> 24)]) << 32;
                WH14 = s_T0[(byte)(WC)] ^ s_T1[(byte)(WD >> 8)] ^ s_T2[(byte)(WA >> 16)] ^ s_T3[(byte)(WB >> 24)];
                WH14 ^= (ulong)(s_T0[(byte)(WD)] ^ s_T1[(byte)(WA >> 8)] ^ s_T2[(byte)(WB >> 16)] ^ s_T3[(byte)(WC >> 24)]) << 32;

                WA = s_T0[(byte)(WL15)] ^ s_T1[(byte)(WL15 >> 40)] ^ s_T2[(byte)(WH15 >> 16)] ^ s_T3[(byte)(WH15 >> 56)] ^ (uint)cnt;
                WB = s_T0[(byte)(WL15 >> 32)] ^ s_T1[(byte)(WH15 >> 8)] ^ s_T2[(byte)(WH15 >> 48)] ^ s_T3[(byte)(WL15 >> 24)] ^ (uint)(cnt >> 32);
                WC = s_T0[(byte)(WH15)] ^ s_T1[(byte)(WH15 >> 40)] ^ s_T2[(byte)(WL15 >> 16)] ^ s_T3[(byte)(WL15 >> 56)];
                WD = s_T0[(byte)(WH15 >> 32)] ^ s_T1[(byte)(WL15 >> 8)] ^ s_T2[(byte)(WL15 >> 48)] ^ s_T3[(byte)(WH15 >> 24)];
                cnt++;
                WL15 = s_T0[(byte)(WA)] ^ s_T1[(byte)(WB >> 8)] ^ s_T2[(byte)(WC >> 16)] ^ s_T3[(byte)(WD >> 24)];
                WL15 ^= (ulong)(s_T0[(byte)(WB)] ^ s_T1[(byte)(WC >> 8)] ^ s_T2[(byte)(WD >> 16)] ^ s_T3[(byte)(WA >> 24)]) << 32;
                WH15 = s_T0[(byte)(WC)] ^ s_T1[(byte)(WD >> 8)] ^ s_T2[(byte)(WA >> 16)] ^ s_T3[(byte)(WB >> 24)];
                WH15 ^= (ulong)(s_T0[(byte)(WD)] ^ s_T1[(byte)(WA >> 8)] ^ s_T2[(byte)(WB >> 16)] ^ s_T3[(byte)(WC >> 24)]) << 32;

                ulong WT0 = WL2;
                WL2 = WL10;
                WL10 = WT0;
                ulong WT1 = WH2;
                WH2 = WH10;
                WH10 = WT1;

                WT0 = WL1;
                WL1 = WL5;
                WL5 = WT0;
                WT1 = WH1;
                WH1 = WH5;
                WH5 = WT1;

                WT0 = WL3;
                WL3 = WL15;
                WL15 = WT0;
                WT1 = WH3;
                WH3 = WH15;
                WH15 = WT1;

                WT0 = ((WL0 >> 7) & 0x0101010101010101) * 27;
                WT0 ^= ((WL0 << 1) & 0xFEFEFEFEFEFEFEFE);
                WT1 = ((WL1 >> 7) & 0x0101010101010101) * 27;
                WT1 ^= ((WL1 << 1) & 0xFEFEFEFEFEFEFEFE);
                ulong WT2 = ((WL2 >> 7) & 0x0101010101010101) * 27;
                WT2 ^= ((WL2 << 1) & 0xFEFEFEFEFEFEFEFE);
                ulong WT3 = ((WL3 >> 7) & 0x0101010101010101) * 27;
                WT3 ^= ((WL3 << 1) & 0xFEFEFEFEFEFEFEFE);
                ulong WT4 = WL0 ^ WL1 ^ WL2 ^ WL3;
                WL0 ^= WT0 ^ WT1 ^ WT4;
                WL1 ^= WT1 ^ WT2 ^ WT4;
                WL2 ^= WT2 ^ WT3 ^ WT4;
                WL3 ^= WT0 ^ WT4 ^ WT3;

                WT0 = ((WH0 >> 7) & 0x0101010101010101) * 27;
                WT0 ^= ((WH0 << 1) & 0xFEFEFEFEFEFEFEFE);
                WT1 = ((WH1 >> 7) & 0x0101010101010101) * 27;
                WT1 ^= ((WH1 << 1) & 0xFEFEFEFEFEFEFEFE);
                WT2 = ((WH2 >> 7) & 0x0101010101010101) * 27;
                WT2 ^= ((WH2 << 1) & 0xFEFEFEFEFEFEFEFE);
                WT3 = ((WH3 >> 7) & 0x0101010101010101) * 27;
                WT3 ^= ((WH3 << 1) & 0xFEFEFEFEFEFEFEFE);
                WT4 = WH0 ^ WH1 ^ WH2 ^ WH3;
                WH0 ^= WT0 ^ WT1 ^ WT4;
                WH1 ^= WT1 ^ WT2 ^ WT4;
                WH2 ^= WT2 ^ WT3 ^ WT4;
                WH3 ^= WT0 ^ WT4 ^ WT3;

                WT0 = WL13;
                WL13 = WL9;
                WL9 = WT0;
                WT1 = WH13;
                WH13 = WH9;
                WH9 = WT1;

                WT0 = WL11;
                WL11 = WL7;
                WL7 = WT0;
                WT1 = WH11;
                WH11 = WH7;
                WH7 = WT1;

                WT0 = ((WL8 >> 7) & 0x0101010101010101) * 27;
                WT0 ^= ((WL8 << 1) & 0xFEFEFEFEFEFEFEFE);
                WT1 = ((WL9 >> 7) & 0x0101010101010101) * 27;
                WT1 ^= ((WL9 << 1) & 0xFEFEFEFEFEFEFEFE);
                WT2 = ((WL10 >> 7) & 0x0101010101010101) * 27;
                WT2 ^= ((WL10 << 1) & 0xFEFEFEFEFEFEFEFE);
                WT3 = ((WL11 >> 7) & 0x0101010101010101) * 27;
                WT3 ^= ((WL11 << 1) & 0xFEFEFEFEFEFEFEFE);
                WT4 = WL8 ^ WL9 ^ WL10 ^ WL11;
                WL8 ^= WT0 ^ WT1 ^ WT4;
                WL9 ^= WT1 ^ WT2 ^ WT4;
                WL10 ^= WT2 ^ WT3 ^ WT4;
                WL11 ^= WT0 ^ WT4 ^ WT3;

                WT0 = ((WH8 >> 7) & 0x0101010101010101) * 27;
                WT0 ^= ((WH8 << 1) & 0xFEFEFEFEFEFEFEFE);
                WT1 = ((WH9 >> 7) & 0x0101010101010101) * 27;
                WT1 ^= ((WH9 << 1) & 0xFEFEFEFEFEFEFEFE);
                WT2 = ((WH10 >> 7) & 0x0101010101010101) * 27;
                WT2 ^= ((WH10 << 1) & 0xFEFEFEFEFEFEFEFE);
                WT3 = ((WH11 >> 7) & 0x0101010101010101) * 27;
                WT3 ^= ((WH11 << 1) & 0xFEFEFEFEFEFEFEFE);
                WT4 = WH8 ^ WH9 ^ WH10 ^ WH11;
                WH8 ^= WT0 ^ WT1 ^ WT4;
                WH9 ^= WT1 ^ WT2 ^ WT4;
                WH10 ^= WT2 ^ WT3 ^ WT4;
                WH11 ^= WT0 ^ WT4 ^ WT3;

                WT0 = WL6;
                WL6 = WL14;
                WL14 = WT0;
                WT1 = WH6;
                WH6 = WH14;
                WH14 = WT1;

                WT0 = WL13;
                WL13 = WL5;
                WL5 = WT0;
                WT1 = WH13;
                WH13 = WH5;
                WH5 = WT1;

                WT0 = WL15;
                WL15 = WL7;
                WL7 = WT0;
                WT1 = WH15;
                WH15 = WH7;
                WH7 = WT1;

                WT0 = ((WL4 >> 7) & 0x0101010101010101) * 27;
                WT0 ^= ((WL4 << 1) & 0xFEFEFEFEFEFEFEFE);
                WT1 = ((WL5 >> 7) & 0x0101010101010101) * 27;
                WT1 ^= ((WL5 << 1) & 0xFEFEFEFEFEFEFEFE);
                WT2 = ((WL6 >> 7) & 0x0101010101010101) * 27;
                WT2 ^= ((WL6 << 1) & 0xFEFEFEFEFEFEFEFE);
                WT3 = ((WL7 >> 7) & 0x0101010101010101) * 27;
                WT3 ^= ((WL7 << 1) & 0xFEFEFEFEFEFEFEFE);
                WT4 = WL4 ^ WL5 ^ WL6 ^ WL7;
                WL4 ^= WT0 ^ WT1 ^ WT4;
                WL5 ^= WT1 ^ WT2 ^ WT4;
                WL6 ^= WT2 ^ WT3 ^ WT4;
                WL7 ^= WT0 ^ WT4 ^ WT3;

                WT0 = ((WH4 >> 7) & 0x0101010101010101) * 27;
                WT0 ^= ((WH4 << 1) & 0xFEFEFEFEFEFEFEFE);
                WT1 = ((WH5 >> 7) & 0x0101010101010101) * 27;
                WT1 ^= ((WH5 << 1) & 0xFEFEFEFEFEFEFEFE);
                WT2 = ((WH6 >> 7) & 0x0101010101010101) * 27;
                WT2 ^= ((WH6 << 1) & 0xFEFEFEFEFEFEFEFE);
                WT3 = ((WH7 >> 7) & 0x0101010101010101) * 27;
                WT3 ^= ((WH7 << 1) & 0xFEFEFEFEFEFEFEFE);
                WT4 = WH4 ^ WH5 ^ WH6 ^ WH7;
                WH4 ^= WT0 ^ WT1 ^ WT4;
                WH5 ^= WT1 ^ WT2 ^ WT4;
                WH6 ^= WT2 ^ WT3 ^ WT4;
                WH7 ^= WT0 ^ WT4 ^ WT3;

                WT0 = ((WL12 >> 7) & 0x0101010101010101) * 27;
                WT0 ^= ((WL12 << 1) & 0xFEFEFEFEFEFEFEFE);
                WT1 = ((WL13 >> 7) & 0x0101010101010101) * 27;
                WT1 ^= ((WL13 << 1) & 0xFEFEFEFEFEFEFEFE);
                WT2 = ((WL14 >> 7) & 0x0101010101010101) * 27;
                WT2 ^= ((WL14 << 1) & 0xFEFEFEFEFEFEFEFE);
                WT3 = ((WL15 >> 7) & 0x0101010101010101) * 27;
                WT3 ^= ((WL15 << 1) & 0xFEFEFEFEFEFEFEFE);
                WT4 = WL12 ^ WL13 ^ WL14 ^ WL15;
                WL12 ^= WT0 ^ WT1 ^ WT4;
                WL13 ^= WT1 ^ WT2 ^ WT4;
                WL14 ^= WT2 ^ WT3 ^ WT4;
                WL15 ^= WT0 ^ WT4 ^ WT3;

                WT0 = ((WH12 >> 7) & 0x0101010101010101) * 27;
                WT0 ^= ((WH12 << 1) & 0xFEFEFEFEFEFEFEFE);
                WT1 = ((WH13 >> 7) & 0x0101010101010101) * 27;
                WT1 ^= ((WH13 << 1) & 0xFEFEFEFEFEFEFEFE);
                WT2 = ((WH14 >> 7) & 0x0101010101010101) * 27;
                WT2 ^= ((WH14 << 1) & 0xFEFEFEFEFEFEFEFE);
                WT3 = ((WH15 >> 7) & 0x0101010101010101) * 27;
                WT3 ^= ((WH15 << 1) & 0xFEFEFEFEFEFEFEFE);
                WT4 = WH12 ^ WH13 ^ WH14 ^ WH15;
                WH12 ^= WT0 ^ WT1 ^ WT4;
                WH13 ^= WT1 ^ WT2 ^ WT4;
                WH14 ^= WT2 ^ WT3 ^ WT4;
                WH15 ^= WT0 ^ WT4 ^ WT3;

                r--;
            }
            while (r > 0);

            m_state[0] = cv[0] ^ WL0 ^ WL8;
            m_state[1] = cv[1] ^ WH0 ^ WH8;
            m_state[2] = cv[2] ^ WL1 ^ WL9;
            m_state[3] = cv[3] ^ WH1 ^ WH9;
            m_state[4] = cv[4] ^ WL2 ^ WL10;
            m_state[5] = cv[5] ^ WH2 ^ WH10;
            m_state[6] = cv[6] ^ WL3 ^ WL11;
            m_state[7] = cv[7] ^ WH3 ^ WH11;
            m_state[8] = cv[8] ^ WL4 ^ WL12;
            m_state[9] = cv[9] ^ WH4 ^ WH12;
            m_state[10] = cv[10] ^ WL5 ^ WL13;
            m_state[11] = cv[11] ^ WH5 ^ WH13;
            m_state[12] = cv[12] ^ WL6 ^ WL14;
            m_state[13] = cv[13] ^ WH6 ^ WH14;
            m_state[14] = cv[14] ^ WL7 ^ WL15;
            m_state[15] = cv[15] ^ WH7 ^ WH15;
        }

        public override void Initialize()
        {
            base.Initialize();

            for (int i = 0; i < 8; i++)
                m_state[2 * i] = (ulong)HashSize * 8;
        }
    };
}
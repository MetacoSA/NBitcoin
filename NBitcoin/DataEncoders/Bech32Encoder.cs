﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace NBitcoin.DataEncoders
{
	public class Bech32FormatException : FormatException
	{
		public Bech32FormatException(string message, int[] indexes) : base(message)
		{
			if (indexes == null)
				throw new ArgumentNullException(nameof(indexes));
			ErrorIndexes = indexes;
			Array.Sort(ErrorIndexes);
		}
		public int[] ErrorIndexes
		{
			get; internal set;
		}
	}
	public class Bech32Encoder : DataEncoder
	{

		readonly static int[] GF1024_EXP = new int[] {
  1, 303, 635, 446, 997, 640, 121, 142, 959, 420, 350, 438, 166, 39, 543,
  335, 831, 691, 117, 632, 719, 97, 107, 374, 558, 797, 54, 150, 858, 877,
  724, 1013, 294, 23, 354, 61, 164, 633, 992, 538, 469, 659, 174, 868, 184,
  809, 766, 563, 866, 851, 257, 520, 45, 770, 535, 524, 408, 213, 436, 760,
  472, 330, 933, 799, 616, 361, 15, 391, 756, 814, 58, 608, 554, 680, 993,
  821, 942, 813, 843, 484, 193, 935, 321, 919, 572, 741, 423, 559, 562,
  589, 296, 191, 493, 685, 891, 665, 435, 60, 395, 2, 606, 511, 853, 746,
  32, 219, 284, 631, 840, 661, 837, 332, 78, 311, 670, 887, 111, 195, 505,
  190, 194, 214, 709, 380, 819, 69, 261, 957, 1018, 161, 739, 588, 7, 708,
  83, 328, 507, 736, 317, 899, 47, 348, 1000, 345, 882, 245, 367, 996, 943,
  514, 304, 90, 804, 295, 312, 793, 387, 833, 249, 921, 660, 618, 823, 496,
  722, 30, 782, 225, 892, 93, 480, 372, 112, 738, 867, 636, 890, 950, 968,
  386, 622, 642, 551, 369, 234, 846, 382, 365, 442, 592, 343, 986, 122,
  1023, 59, 847, 81, 790, 4, 437, 983, 931, 244, 64, 415, 529, 487, 944,
  35, 938, 664, 156, 583, 53, 999, 222, 390, 987, 341, 388, 389, 170, 721,
  879, 138, 522, 627, 765, 322, 230, 440, 14, 168, 143, 656, 991, 224, 595,
  550, 94, 657, 752, 667, 1005, 451, 734, 744, 638, 292, 585, 157, 872,
  590, 601, 827, 774, 930, 475, 571, 33, 500, 871, 969, 173, 21, 828, 450,
  1009, 147, 960, 705, 201, 228, 998, 497, 1021, 613, 688, 772, 508, 36,
  366, 715, 468, 956, 725, 730, 861, 425, 647, 701, 221, 759, 95, 958, 139,
  805, 8, 835, 679, 614, 449, 128, 791, 299, 974, 617, 70, 628, 57, 273,
  430, 67, 750, 405, 780, 703, 643, 776, 778, 340, 171, 1022, 276, 308,
  495, 243, 644, 460, 857, 28, 336, 286, 41, 695, 448, 431, 364, 149, 43,
  233, 63, 762, 902, 181, 240, 501, 584, 434, 275, 1008, 444, 443, 895,
  812, 612, 927, 383, 66, 961, 1006, 690, 346, 3, 881, 900, 747, 271, 672,
  162, 402, 456, 748, 971, 755, 490, 105, 808, 977, 72, 732, 182, 897, 625,
  163, 189, 947, 850, 46, 115, 403, 231, 151, 629, 278, 874, 16, 934, 110,
  492, 898, 256, 807, 598, 700, 498, 140, 481, 91, 523, 860, 134, 252, 771,
  824, 119, 38, 816, 820, 641, 342, 757, 513, 577, 990, 463, 40, 920, 955,
  17, 649, 533, 82, 103, 896, 862, 728, 259, 86, 466, 87, 253, 556, 323,
  457, 963, 432, 845, 527, 745, 849, 863, 1015, 888, 488, 567, 727, 132,
  674, 764, 109, 669, 6, 1003, 552, 246, 542, 96, 324, 781, 912, 248, 694,
  239, 980, 210, 880, 683, 144, 177, 325, 546, 491, 326, 339, 623, 941, 92,
  207, 783, 462, 263, 483, 517, 1012, 9, 620, 220, 984, 548, 512, 878, 421,
  113, 973, 280, 962, 159, 310, 945, 268, 465, 806, 889, 199, 76, 873, 865,
  34, 645, 227, 290, 418, 693, 926, 80, 569, 639, 11, 50, 291, 141, 206,
  544, 949, 185, 518, 133, 909, 135, 467, 376, 646, 914, 678, 841, 954,
  318, 242, 939, 951, 743, 1017, 976, 359, 167, 264, 100, 241, 218, 51, 12,
  758, 368, 453, 309, 192, 648, 826, 553, 473, 101, 478, 673, 397, 1001,
  118, 265, 331, 650, 356, 982, 652, 655, 510, 634, 145, 414, 830, 924,
  526, 966, 298, 737, 18, 504, 401, 697, 360, 288, 1020, 842, 203, 698,
  537, 676, 279, 581, 619, 536, 907, 876, 1019, 398, 152, 1010, 994, 68,
  42, 454, 580, 836, 99, 565, 137, 379, 503, 22, 77, 582, 282, 412, 352,
  611, 347, 300, 266, 570, 270, 911, 729, 44, 557, 108, 946, 637, 597, 461,
  630, 615, 238, 763, 681, 718, 334, 528, 200, 459, 413, 79, 24, 229, 713,
  906, 579, 384, 48, 893, 370, 923, 202, 917, 98, 794, 754, 197, 530, 662,
  52, 712, 677, 56, 62, 981, 509, 267, 789, 885, 561, 316, 684, 596, 226,
  13, 985, 779, 123, 720, 576, 753, 948, 406, 125, 315, 104, 519, 426, 502,
  313, 566, 1016, 767, 796, 281, 749, 740, 136, 84, 908, 424, 936, 198,
  355, 274, 735, 967, 5, 154, 428, 541, 785, 704, 486, 671, 600, 532, 381,
  540, 574, 187, 88, 378, 216, 621, 499, 419, 922, 485, 494, 476, 255, 114,
  188, 668, 297, 400, 918, 787, 158, 25, 458, 178, 564, 422, 768, 73, 1011,
  717, 575, 404, 547, 196, 829, 237, 394, 301, 37, 65, 176, 106, 89, 85,
  675, 979, 534, 803, 995, 363, 593, 120, 417, 452, 26, 699, 822, 223, 169,
  416, 235, 609, 773, 211, 607, 208, 302, 852, 965, 603, 357, 761, 247,
  817, 539, 250, 232, 272, 129, 568, 848, 624, 396, 710, 525, 183, 686, 10,
  285, 856, 307, 811, 160, 972, 55, 441, 289, 723, 305, 373, 351, 153, 733,
  409, 506, 975, 838, 573, 970, 988, 913, 471, 205, 337, 49, 594, 777, 549,
  815, 277, 27, 916, 333, 353, 844, 800, 146, 751, 186, 375, 769, 358, 392,
  883, 474, 788, 602, 74, 130, 329, 212, 155, 131, 102, 687, 293, 870, 742,
  726, 427, 217, 834, 904, 29, 127, 869, 407, 338, 832, 470, 482, 810, 399,
  439, 393, 604, 929, 682, 447, 714, 251, 455, 875, 319, 477, 464, 521,
  258, 377, 937, 489, 792, 172, 314, 327, 124, 20, 531, 953, 591, 886, 320,
  696, 71, 859, 578, 175, 587, 707, 663, 283, 179, 795, 989, 702, 940, 371,
  692, 689, 555, 903, 410, 651, 75, 429, 818, 362, 894, 515, 31, 545, 666,
  706, 952, 864, 269, 254, 349, 711, 802, 716, 784, 1007, 925, 801, 445,
  148, 260, 658, 385, 287, 262, 204, 126, 586, 1004, 236, 165, 854, 411,
  932, 560, 19, 215, 1002, 775, 653, 928, 901, 964, 884, 798, 839, 786,
  433, 610, 116, 855, 180, 479, 910, 1014, 599, 915, 905, 306, 516, 731,
  626, 978, 825, 344, 605, 654, 209
		};

		readonly static int[] GF1024_LOG = new[] {
		  -1, 0, 99, 363, 198, 726, 462, 132, 297, 495, 825, 528, 561, 693, 231,
		  66, 396, 429, 594, 990, 924, 264, 627, 33, 660, 759, 792, 858, 330, 891,
		  165, 957, 104, 259, 518, 208, 280, 776, 416, 13, 426, 333, 618, 339, 641,
		  52, 388, 140, 666, 852, 529, 560, 678, 213, 26, 832, 681, 309, 70, 194,
		  97, 35, 682, 341, 203, 777, 358, 312, 617, 125, 307, 931, 379, 765, 875,
		  951, 515, 628, 112, 659, 525, 196, 432, 134, 717, 781, 438, 440, 740,
		  780, 151, 408, 487, 169, 239, 293, 467, 21, 672, 622, 557, 571, 881, 433,
		  704, 376, 779, 22, 643, 460, 398, 116, 172, 503, 751, 389, 1004, 18, 576,
		  415, 789, 6, 192, 696, 923, 702, 981, 892, 302, 816, 876, 880, 457, 537,
		  411, 539, 716, 624, 224, 295, 406, 531, 7, 233, 478, 586, 864, 268, 974,
		  338, 27, 392, 614, 839, 727, 879, 211, 250, 758, 507, 830, 129, 369, 384,
		  36, 985, 12, 555, 232, 796, 221, 321, 920, 263, 42, 934, 778, 479, 761,
		  939, 1006, 344, 381, 823, 44, 535, 866, 739, 752, 385, 119, 91, 566, 80,
		  120, 117, 771, 675, 721, 514, 656, 271, 670, 602, 980, 850, 532, 488,
		  803, 1022, 475, 801, 878, 57, 121, 991, 742, 888, 559, 105, 497, 291,
		  215, 795, 236, 167, 692, 520, 272, 661, 229, 391, 814, 340, 184, 798,
		  984, 773, 650, 473, 345, 558, 548, 326, 202, 145, 465, 810, 471, 158,
		  813, 908, 412, 441, 964, 750, 401, 50, 915, 437, 975, 126, 979, 491, 556,
		  577, 636, 685, 510, 963, 638, 367, 815, 310, 723, 349, 323, 857, 394,
		  606, 505, 713, 630, 938, 106, 826, 332, 978, 599, 834, 521, 530, 248,
		  883, 32, 153, 90, 754, 592, 304, 635, 775, 804, 1, 150, 836, 1013, 828,
		  324, 565, 508, 113, 154, 708, 921, 703, 689, 138, 547, 911, 929, 82, 228,
		  443, 468, 480, 483, 922, 135, 877, 61, 578, 111, 860, 654, 15, 331, 851,
		  895, 484, 320, 218, 420, 190, 1019, 143, 362, 634, 141, 965, 10, 838,
		  632, 861, 34, 722, 580, 808, 869, 554, 598, 65, 954, 787, 337, 187, 281,
		  146, 563, 183, 668, 944, 171, 837, 23, 867, 541, 916, 741, 625, 123, 736,
		  186, 357, 665, 977, 179, 156, 219, 220, 216, 67, 870, 902, 774, 98, 820,
		  574, 613, 900, 755, 596, 370, 390, 769, 314, 701, 894, 56, 841, 949, 987,
		  631, 658, 587, 204, 797, 790, 522, 745, 9, 502, 763, 86, 719, 288, 706,
		  887, 728, 952, 311, 336, 446, 1002, 348, 96, 58, 199, 11, 901, 230, 833,
		  188, 352, 351, 973, 3, 906, 335, 301, 266, 244, 791, 564, 619, 909, 371,
		  444, 760, 657, 328, 647, 490, 425, 913, 511, 439, 540, 283, 40, 897, 849,
		  60, 570, 872, 257, 749, 912, 572, 1007, 170, 407, 898, 492, 79, 747, 732,
		  206, 454, 918, 375, 482, 399, 92, 748, 325, 163, 274, 405, 744, 260, 346,
		  707, 626, 595, 118, 842, 136, 279, 684, 584, 101, 500, 422, 149, 956,
		  1014, 493, 536, 705, 51, 914, 225, 409, 55, 822, 590, 448, 655, 205, 676,
		  925, 735, 431, 784, 54, 609, 604, 39, 812, 737, 729, 466, 14, 533, 958,
		  481, 770, 499, 855, 238, 182, 464, 569, 72, 947, 442, 642, 24, 87, 989,
		  688, 88, 47, 762, 623, 709, 455, 817, 526, 637, 258, 84, 845, 738, 768,
		  698, 423, 933, 664, 620, 607, 629, 212, 347, 249, 982, 935, 131, 89, 252,
		  927, 189, 788, 853, 237, 691, 646, 403, 1010, 734, 253, 874, 807, 903,
		  1020, 100, 802, 71, 799, 1003, 633, 355, 276, 300, 649, 64, 306, 161,
		  608, 496, 743, 180, 485, 819, 383, 1016, 226, 308, 393, 648, 107, 19, 37,
		  585, 2, 175, 645, 247, 527, 5, 419, 181, 317, 327, 519, 542, 289, 567,
		  430, 579, 950, 582, 994, 1021, 583, 234, 240, 976, 41, 160, 109, 677,
		  937, 210, 95, 959, 242, 753, 461, 114, 733, 368, 573, 458, 782, 605, 680,
		  544, 299, 73, 652, 905, 477, 690, 93, 824, 882, 277, 946, 361, 17, 945,
		  523, 472, 334, 930, 597, 603, 793, 404, 290, 942, 316, 731, 270, 960,
		  936, 133, 122, 821, 966, 679, 662, 907, 282, 968, 767, 653, 20, 697, 222,
		  164, 835, 30, 285, 886, 456, 436, 640, 286, 1015, 380, 840, 245, 724,
		  137, 593, 173, 130, 715, 85, 885, 551, 246, 449, 103, 366, 372, 714, 313,
		  865, 241, 699, 674, 374, 68, 421, 562, 292, 59, 809, 342, 651, 459, 227,
		  46, 711, 764, 868, 53, 413, 278, 800, 255, 993, 318, 854, 319, 695, 315,
		  469, 166, 489, 969, 730, 1001, 757, 873, 686, 197, 303, 919, 155, 673,
		  940, 712, 25, 999, 63, 863, 972, 967, 785, 152, 296, 512, 402, 377, 45,
		  899, 829, 354, 77, 69, 856, 417, 811, 953, 124, 418, 75, 794, 162, 414,
		  1018, 568, 254, 265, 772, 588, 16, 896, 157, 889, 298, 621, 110, 844,
		  1000, 108, 545, 601, 78, 862, 447, 185, 195, 818, 450, 387, 49, 805, 102,
		  986, 1005, 827, 329, 28, 932, 410, 287, 435, 451, 962, 517, 48, 174, 43,
		  893, 884, 261, 251, 516, 395, 910, 611, 29, 501, 223, 476, 364, 144, 871,
		  998, 687, 928, 115, 453, 513, 176, 94, 168, 667, 955, 353, 434, 382, 400,
		  139, 365, 996, 343, 948, 890, 1012, 663, 610, 718, 538, 1008, 639, 470,
		  848, 543, 1011, 859, 671, 756, 83, 427, 159, 746, 669, 589, 971, 524,
		  356, 995, 904, 256, 201, 988, 62, 397, 81, 720, 917, 209, 549, 943, 486,
		  76, 148, 207, 509, 644, 386, 700, 534, 177, 550, 961, 926, 546, 428, 284,
		  127, 294, 8, 269, 359, 506, 445, 997, 806, 591, 725, 178, 262, 846, 373,
		  831, 504, 305, 843, 553, 378, 1017, 783, 474, 683, 581, 200, 498, 694,
		  191, 217, 847, 941, 424, 235, 38, 74, 616, 786, 147, 4, 273, 214, 142,
		  575, 992, 463, 983, 243, 360, 970, 350, 267, 615, 766, 494, 31, 1009,
		  452, 710, 552, 128, 612, 600, 275, 322, 193
		};

		protected static readonly byte[] Byteset;
		static Bech32Encoder()
		{
			Byteset = Encoders.ASCII.DecodeData("qpzry9x8gf2tvdw0s3jn54khce6mua7l");
		}
		private static readonly uint[] Generator = { 0x3b6a57b2U, 0x26508e6dU, 0x1ea119faU, 0x3d4233ddU, 0x2a1462b3U };


		uint syndrome(uint residue)
		{
			var low = residue & 0x1f;
			return (uint)(low ^ (low << 10) ^ (low << 20) ^
			  (((residue >> 5) & 1) != 0 ? 0x31edd3c4 : 0) ^
			  (((residue >> 6) & 1) != 0 ? 0x335f86a8 : 0) ^
			  (((residue >> 7) & 1) != 0 ? 0x363b8870 : 0) ^
			  (((residue >> 8) & 1) != 0 ? 0x3e6390c9 : 0) ^
			  (((residue >> 9) & 1) != 0 ? 0x2ec72192 : 0) ^
			  (((residue >> 10) & 1) != 0 ? 0x1046f79d : 0) ^
			  (((residue >> 11) & 1) != 0 ? 0x208d4e33 : 0) ^
			  (((residue >> 12) & 1) != 0 ? 0x130ebd6f : 0) ^
			  (((residue >> 13) & 1) != 0 ? 0x2499fade : 0) ^
			  (((residue >> 14) & 1) != 0 ? 0x1b27d4b5 : 0) ^
			  (((residue >> 15) & 1) != 0 ? 0x04be1eb4 : 0) ^
			  (((residue >> 16) & 1) != 0 ? 0x0968b861 : 0) ^
			  (((residue >> 17) & 1) != 0 ? 0x1055f0c2 : 0) ^
			  (((residue >> 18) & 1) != 0 ? 0x20ab4584 : 0) ^
			  (((residue >> 19) & 1) != 0 ? 0x1342af08 : 0) ^
			  (((residue >> 20) & 1) != 0 ? 0x24f1f318 : 0) ^
			  (((residue >> 21) & 1) != 0 ? 0x1be34739 : 0) ^
			  (((residue >> 22) & 1) != 0 ? 0x35562f7b : 0) ^
			  (((residue >> 23) & 1) != 0 ? 0x3a3c5bff : 0) ^
			  (((residue >> 24) & 1) != 0 ? 0x266c96f7 : 0) ^
			  (((residue >> 25) & 1) != 0 ? 0x25c78b65 : 0) ^
			  (((residue >> 26) & 1) != 0 ? 0x1b1f13ea : 0) ^
			  (((residue >> 27) & 1) != 0 ? 0x34baa2f4 : 0) ^
			  (((residue >> 28) & 1) != 0 ? 0x3b61c0e1 : 0) ^
			  (((residue >> 29) & 1) != 0 ? 0x265325c2 : 0));
		}


		int[] locate_errors(uint residue, int length)
		{
			if (residue == 0)
			{
				return new int[0];
			}
			var syn = syndrome(residue);
			var s0 = syn & 0x3FF;
			var s1 = (syn >> 10) & 0x3FF;
			var s2 = syn >> 20;
			var l_s0 = GF1024_LOG[s0];
			var l_s1 = GF1024_LOG[s1];
			var l_s2 = GF1024_LOG[s2];
			if (l_s0 != -1 && l_s1 != -1 && l_s2 != -1 && (2 * l_s1 - l_s2 - l_s0 + 2046) % 1023 == 0)
			{
				var p1 = (l_s1 - l_s0 + 1023) % 1023;
				if (p1 >= length)
					return new int[0];
				var l_e1 = l_s0 + (1023 - 997) * p1;
				if ((l_e1 % 33) != 0)
					return new int[0];
				return new[] { p1 };
			}
			for (var p1 = 0; p1 < length; p1++)
			{
				var s2_s1p1 = s2 ^ (s1 == 0 ? 0 : GF1024_EXP[(l_s1 + p1) % 1023]);
				if (s2_s1p1 == 0)
					continue;
				var s1_s0p1 = s1 ^ (s0 == 0 ? 0 : GF1024_EXP[(l_s0 + p1) % 1023]);
				if (s1_s0p1 == 0)
					continue;
				var l_s1_s0p1 = GF1024_LOG[s1_s0p1];
				var p2 = (GF1024_LOG[s2_s1p1] - l_s1_s0p1 + 1023) % 1023;
				if (p2 >= length || p1 == p2)
					continue;
				var s1_s0p2 = s1 ^ (s0 == 0 ? 0 : GF1024_EXP[(l_s0 + p2) % 1023]);
				if (s1_s0p2 == 0)
					continue;
				var inv_p1_p2 = 1023 - GF1024_LOG[GF1024_EXP[p1] ^ GF1024_EXP[p2]];
				var l_e2 = l_s1_s0p1 + inv_p1_p2 + (1023 - 997) * p2;
				if ((l_e2 % 33) != 0)
					continue;
				var l_e1 = GF1024_LOG[s1_s0p2] + inv_p1_p2 + (1023 - 997) * p1;
				if ((l_e1 % 33) != 0)
					continue;
				if (p1 < p2)
				{
					return new int[] { p1, p2 };
				}
				else
				{
					return new int[] { p2, p1 };
				}
			}
			return new int[0];
		}

		internal Bech32Encoder(string hrp) : this(hrp == null ? null : Encoders.ASCII.DecodeData(hrp.ToLowerInvariant()))
		{
		}
		public Bech32Encoder(byte[] hrp)
		{
			if (hrp == null)
				throw new ArgumentNullException(nameof(hrp));

			_Hrp = hrp;
			var len = hrp.Length;
			_HrpExpand = new byte[(2 * len) + 1];
			for (int i = 0; i < len; i++)
			{
				_HrpExpand[i] = (byte)(hrp[i] >> 5);
				_HrpExpand[i + len + 1] = (byte)(hrp[i] & 31);
			}
		}

		protected readonly byte[] _HrpExpand;
		protected readonly byte[] _Hrp;
		public byte[] HumanReadablePart
		{
			get
			{
				return _Hrp;
			}
		}

#if HAS_SPAN
		private static uint Polymod(ReadOnlySpan<byte> values)
#else
		private static uint Polymod(byte[] values)
#endif
		{
			uint chk = 1;
			for (int i = 0; i < values.Length; i++)
			{
				var top = chk >> 25;
				chk = values[i] ^ ((chk & 0x1ffffff) << 5);
				chk ^= ((top >> 0) & 1) == 1 ? Generator[0] : 0;
				chk ^= ((top >> 1) & 1) == 1 ? Generator[1] : 0;
				chk ^= ((top >> 2) & 1) == 1 ? Generator[2] : 0;
				chk ^= ((top >> 3) & 1) == 1 ? Generator[3] : 0;
				chk ^= ((top >> 4) & 1) == 1 ? Generator[4] : 0;
			}
			return chk;
		}
#if HAS_SPAN
		protected virtual bool VerifyChecksum(ReadOnlySpan<byte> data, int bechStringLen, out int[] errorPosition)
#else
		protected virtual bool VerifyChecksum(byte[] data, int bechStringLen, out int[] errorPosition)
#endif
		{
			errorPosition = null;
#if HAS_SPAN
			Span<byte> values = _HrpExpand.Length + data.Length is int v && v > 256 ? new byte[v] : stackalloc byte[v];
			_HrpExpand.CopyTo(values);
			data.CopyTo(values.Slice(_HrpExpand.Length));
#else
			var values = _HrpExpand.Concat(data);
#endif
			var polymod = Polymod(values) ^ 1;
			if (polymod != 0)
			{
				var epos = locate_errors(polymod, bechStringLen - 1);
				errorPosition = epos;
				if (epos.Length == 0)
					return false;
				for (var ep = 0; ep < epos.Length; ++ep)
				{
					epos[ep] = bechStringLen - epos[ep] - (epos[ep] >= data.Length ? 2 : 1);
				}
				return false;
			}
			return true;
		}

#if HAS_SPAN
		private void CreateChecksum(byte[] data, int offset, int count, Span<byte> ret)
#else
		private byte[] CreateChecksum(byte[] data, int offset, int count)
#endif
		{
#if HAS_SPAN
			Span<byte> values = _HrpExpand.Length + count + 6 is int v && v > 256 ? new byte[v] :
																	  stackalloc byte[v];
#else
			var values = new byte[_HrpExpand.Length + count + 6];
#endif
			var valuesOffset = 0;
#if HAS_SPAN
			_HrpExpand.AsSpan().CopyTo(values.Slice(valuesOffset));
#else
			Array.Copy(_HrpExpand, 0, values, valuesOffset, _HrpExpand.Length);
#endif
			valuesOffset += _HrpExpand.Length;
#if HAS_SPAN
			data.AsSpan().Slice(offset).CopyTo(values.Slice(valuesOffset, count));
#else
			Array.Copy(data, offset, values, valuesOffset, count);
#endif
			valuesOffset += count;
			var polymod = Polymod(values) ^ 1;
#if !HAS_SPAN
			var ret = new byte[6];
#endif
			foreach (var i in Enumerable.Range(0, 6))
			{
				ret[i] = (byte)((polymod >> 5 * (5 - i)) & 31);
			}
#if !HAS_SPAN
			return ret;
#endif
		}


		public override string EncodeData(byte[] data, int offset, int count)
		{
#if HAS_SPAN
			Span<byte> combined = _Hrp.Length + 1 + count + 6 is int v && v > 256 ? new byte[v] :
																	  stackalloc byte[v];
#else
			var combined = new byte[_Hrp.Length + 1 + count + 6];
#endif
			int combinedOffset = 0;
#if HAS_SPAN
			_Hrp.CopyTo(combined);
#else
			Array.Copy(_Hrp, 0, combined, 0, _Hrp.Length);
#endif
			combinedOffset += _Hrp.Length;
			combined[combinedOffset] = 49;
			combinedOffset++;
#if HAS_SPAN
			data.AsSpan().Slice(offset).CopyTo(combined.Slice(combinedOffset, count));
#else
			Array.Copy(data, offset, combined, combinedOffset, count);
#endif
			combinedOffset += count;
#if HAS_SPAN
			Span<byte> checkSum = stackalloc byte[6];
			CreateChecksum(data, offset, count, checkSum);
#else
			var checkSum = CreateChecksum(data, offset, count);
#endif
#if HAS_SPAN
			checkSum.CopyTo(combined.Slice(combinedOffset, 6));
#else
			Array.Copy(checkSum, 0, combined, combinedOffset, 6);
#endif
			combinedOffset += 6;
			for (int i = 0; i < count + 6; i++)
			{
				combined[_Hrp.Length + 1 + i] = Byteset[combined[_Hrp.Length + 1 + i]];
			}
			return Encoders.ASCII.EncodeData(combined);
		}

		public static Bech32Encoder ExtractEncoderFromString(string test)
		{
			var i = test.LastIndexOf('1');
			if (i == -1)
				throw new FormatException("Invalid Bech32 string");

			return Encoders.Bech32(test.Substring(0, i));
		}

		protected virtual void CheckCase(string hrp)
		{
			if (hrp.ToLowerInvariant().Equals(hrp))
				return;
			if (hrp.ToUpperInvariant().Equals(hrp))
				return;
			throw new FormatException("Invalid bech32 string, mixed case detected");
		}
		public override byte[] DecodeData(string encoded)
		{
			throw new NotSupportedException();
		}
		protected virtual byte[] DecodeDataCore(string encoded)
		{
			if (encoded == null)
				throw new ArgumentNullException(nameof(encoded));
			CheckCase(encoded);
			encoded = encoded.ToLowerInvariant();
			var buffer = Encoders.ASCII.DecodeData(encoded);
			var pos = encoded.LastIndexOf("1", StringComparison.OrdinalIgnoreCase);
			if (pos < 1 || pos + 7 > encoded.Length || encoded.Length > 90)
			{
				throw new FormatException("bech missing separator, separator misplaced or too long input");
			}
			if (buffer.Length < _Hrp.Length)
				throw new FormatException("Mismatching human readable part");
			for (int i = 0; i < _Hrp.Length; i++)
			{
				if (buffer[i] != _Hrp[i])
					throw new FormatException("Mismatching human readable part");
			}
#if HAS_SPAN
			Span<byte> data = encoded.Length - pos - 1 is int v && v > 256 ? new byte[v] : stackalloc byte[v];
#else
			var data = new byte[encoded.Length - pos - 1];
#endif
			for (int j = 0, i = pos + 1; i < encoded.Length; i++, j++)
			{
				int index = Array.IndexOf(Byteset, buffer[i]);
				if (index == -1)
					throw new FormatException("bech chars are out of range");
				data[j] = (byte)index;
			}

			int[] error;
			if (!VerifyChecksum(data, encoded.Length, out error))
			{
				if (error.Length == 0)
					throw new FormatException("Error while verifying Bech32 checksum");
				else
					throw new Bech32FormatException($"Error in Bech32 string at {String.Join(",", error)}", error);
			}
#if HAS_SPAN
			return data.Slice(0, data.Length - 6).ToArray();
#else
			return data.Take(data.Length - 6).ToArray();
#endif
		}

		protected virtual byte[] ConvertBits(IEnumerable<byte> data, int fromBits, int toBits, bool pad = true)
		{
			var acc = 0;
			var bits = 0;
			var maxv = (1 << toBits) - 1;
			var ret = new List<byte>(64);
			foreach (var value in data)
			{
				if ((value >> fromBits) > 0)
					throw new FormatException("Invalid Bech32 string");
				acc = (acc << fromBits) | value;
				bits += fromBits;
				while (bits >= toBits)
				{
					bits -= toBits;
					ret.Add((byte)((acc >> bits) & maxv));
				}
			}
			if (pad)
			{
				if (bits > 0)
				{
					ret.Add((byte)((acc << (toBits - bits)) & maxv));
				}
			}
			else if (bits >= fromBits || (byte)(((acc << (toBits - bits)) & maxv)) != 0)
			{
				throw new FormatException("Invalid Bech32 string");
			}
			return ret.ToArray();
		}

		public virtual byte[] Decode(string addr, out byte witnessVerion)
		{
			if (addr == null)
				throw new ArgumentNullException(nameof(addr));
			CheckCase(addr);
			var data = DecodeDataCore(addr);

			var decoded = ConvertBits(data.Skip(1), 5, 8, false);
			if (decoded.Length < 2 || decoded.Length > 40)
				throw new FormatException("Invalid decoded data length");

			witnessVerion = data[0];
			if (witnessVerion > 16)
				throw new FormatException("Invalid decoded witness version");

			if (witnessVerion == 0 && decoded.Length != 20 && decoded.Length != 32)
				throw new FormatException("Decoded witness program with unknown length");
			return decoded;
		}

		public string Encode(byte witnessVerion, byte[] witnessProgramm)
		{
			var data = (new[] { witnessVerion }).Concat(ConvertBits(witnessProgramm, 8, 5));
			var ret = EncodeData(data);
			return ret;
		}
	}
}

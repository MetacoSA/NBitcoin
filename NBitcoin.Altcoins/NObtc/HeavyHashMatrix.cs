using System;

namespace NBitcoin.Altcoins 
{
    public class HeavyHashMatrix {
        public static double RoundOffError = 1e-9;
        public static uint RANK = 64;
        private ulong[,] _body = new ulong[RANK, RANK];

        public ulong[,] Body 
        { 
            get {
                return _body;
            }
        }
        public HeavyHashMatrix(uint256 seed) => PopulateMatrixBody(seed);
        public HeavyHashMatrix(ulong[,] body) => _body = body;

        private void PopulateMatrixBody(uint256 seed) {
            XoShiRo256PlusPlus prng = new XoShiRo256PlusPlus(seed);
            do
            {
                for (int i = 0; i < RANK; i++)
                {
                    for (int j = 0; j < RANK; j += 16)
                    {
                        ulong value = prng.GetNext();
                        for (int shift = 0; shift < 16; shift++)
                        {
                            _body[i, j + shift] = (value >> (4 * shift)) & 0xF;
                        }
                    }
                }
            } while(!IsFullRank());
        }

        private bool IsFullRank() => IsFullRankGaus(_body, RANK);

        public static bool IsFullRankGaus(ulong[,] A, uint size)
        {
            double[,] B = new double[size, size];
            bool[] rowSelected = new bool[size];
            
            for (int i = 0; i < size; ++i) {
                rowSelected[i] = false;
                for (int j = 0; j < size; ++j) {
                    B[i, j] = (double) A[i, j];
                }
            }
            
            bool fFullRank = true;
            for (int i = 0; i < size; i++) {
                int j;
                for (j = 0; j < size; j++) {
                    if (!rowSelected[j] && Math.Abs(B[j, i]) > RoundOffError) {
                        break;
                    }
                }
                
                if (j != size) {
                    rowSelected[j] = true;
                    for (int p = i + 1; p < size; p++) {
                        B[j, p] /= B[j, i];
                    }

                    for (int k = 0; k < size; k++) {
                        if (k != j && Math.Abs(B[k, i]) > RoundOffError) {
                            for (int p = i + 1; p < size; p++) {
                                B[k, p] -= B[k, i] * B[j, p];
                            }
                        }
                    }
                } else {
                    fFullRank = false;
                    break;
                }
            }

            return fFullRank;
        }

        public override string ToString() {
            string _str = "{";
            for (int i = 0; i < RANK; i++)
            {
                for (int j = 0; j < RANK; j++)
                {
                    if (j == 0) 
                    {
                        _str += "{ " + _body[i, j] + ", ";
                    } else if(j == RANK - 1) 
                    {
                        _str += _body[i, j] + " }";
                    } else 
                    {
                        _str += _body[i, j] + ", ";
                    }
                    
                }
                _str += " }";
            }
            return _str;
        }
    }
}
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections;

namespace HashLib
{
    public abstract class Hash32 : Hash<int>, IHash<int>
    {
        public Hash32(Type a_baseImplementation, bool a_isBuildIn, int a_blockSize) 
            : base(a_baseImplementation, a_isBuildIn, a_blockSize, 4)
        {
        }
    }
}

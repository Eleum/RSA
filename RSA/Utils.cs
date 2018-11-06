using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace RSA
{
    public static class Utils
    {
        public static BigInteger GetNumber(this BitArray bits)
        {
            BigInteger val = 0;

            for (int i = 0; i < bits.Count; i++)
            {
                if (bits[i]) val += 1 << i;
            }

            return val;
        }
    }
}

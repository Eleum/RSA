using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using System.Numerics;

namespace RSA
{
    public class ViewModel : INotifyPropertyChanged
    {
        private const int N = 1024; // bits count to generate random primes

        public event PropertyChangedEventHandler PropertyChanged;

        private void OnPropertyChanged<T>(Expression<Func<T>> expr)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(expr.GetType().ToString()));
        }

        public ViewModel()
        {
            var r = IsPrime(7451, 128);
            GetRandomPrime();
        }

        private void GetRandomPrime()
        {
            var k = N / 2;
            BigInteger num = 0;

            do
            {
                var rand = new Random();
                var bits = new BitArray(k);

                string str = "";

                for (int i = 0; i < k; i++)
                {
                    str += Convert.ToBoolean(rand.Next(0, 2)) ? "1" : "0";
                }

                num = BinToDec(str);
            }
            while (!IsPrime(num, 128));
        }

        private bool IsPrime(BigInteger num, int accuracy)
        {
            if (num % 2 == 0) return false;

            var d = GetDNumber(num);

            for(int i = 0; i < accuracy; i++)
            {
                if(!MillerTest(num, d)) return false;
            }

            return true;
        }

        private BigInteger GetDNumber(BigInteger n)
        {
            var d = n - 1;

            while (d % 2 == 0) d /= 2;

            return d;
        }

        /// <summary>
        /// Miller-Rabin method
        /// </summary>
        /// <param name="n">number to test</param>
        /// <param name="d">a number that d*2^r = n-1</param>
        /// <returns></returns>
        private bool MillerTest(BigInteger n, BigInteger d)
        {
            var test = RandomBigInteger(n);

            var pow = ModularPower(test, d, n);

            if (pow == 1 || pow == n - 1) return true;

            while(d != n - 1)
            {
                pow = (pow * pow) % n;
                d = d << 1;

                if (pow == 1) return false;
                if (pow == n - 1) return true;
            }

            return false; // number is composite
        }

        private BigInteger ModularPower(BigInteger x, BigInteger y, BigInteger n)
        {
            BigInteger res = 1;

            // making the x be less then n
            x %= n;

            while(y > 0)
            {
                // if y is odd, multiply x with result
                if((y & 1L) == 1)
                {
                    res = (res * x) % n;
                }

                // y must be even now
                y = y >> 1;
                x = (x * x) % n;
            }

            return res;
        }

        public static BigInteger RandomBigInteger(BigInteger max)
        {
            var random = new Random();
            byte[] bytes = max.ToByteArray();
            BigInteger R;

            do
            {
                random.NextBytes(bytes);
                bytes[bytes.Length - 1] &= 0x7F; //force sign bit to positive
                R = new BigInteger(bytes);
            } while (!(R >=2 && R <= max));

            return R;
        }

        public BigInteger BinToDec(string value)
        {
            // BigInteger can be found in the System.Numerics dll
            BigInteger res = 0;

            // I'm totally skipping error handling here
            foreach (char c in value)
            {
                res <<= 1;
                res += c == '1' ? 1 : 0;
            }

            return res;
        }
    }
}

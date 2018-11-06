using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace RSA
{
    public class ViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        private void OnPropertyChanged<T>(Expression<Func<T>> expr)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(expr.GetType().ToString()));
        }

        public ViewModel()
        {
            var r = IsPrime(7451, 128);
        }

        private bool IsPrime(int num, int accuracy)
        {
            if (num % 2 == 0) return false;

            var d = GetDNumber(num);

            for(int i = 0; i < accuracy; i++)
            {
                if(!MillerTest(num, d)) return false;
            }

            return true;
        }

        private int GetDNumber(int n)
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
        private bool MillerTest(int n, int d)
        {
            var @enum = Enumerable.Range(2, n - 2);

            // getting random number from enumeration
            var test = @enum.ElementAt(new Random().Next(0, @enum.Count()));

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

        private int ModularPower(int x, int y, int n)
        {
            var res = 1;

            // making the x be less then n
            x %= n;

            while(y > 0)
            {
                // if y is odd, multiply x with result
                if((y & 1) == 1)
                {
                    res = (res * x) % n;
                }

                // y must be even now
                y = y >> 1;
                x = (x * x) % n;
            }

            return res;
        }

    }
}

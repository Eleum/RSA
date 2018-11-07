using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using System.Numerics;
using System.Security.Cryptography;
using System.IO;
using System.Windows;
using System.Windows.Input;

namespace RSA
{
    public class ViewModel : INotifyPropertyChanged
    {
        private const int N = 1024, // bits count to generate random primes
            e = 65537, // 3, 5, 17, 257 are common choices too
            accuracy = 128;

        private readonly int[] earr = { 3, 5, 17, 257, 65537 };

        private string _publicKey, _privateKey, _message;
        private byte[] _encryptedMessage;
        private BigInteger _primeFirst, _primeSecond, n, c, d, m;

        public event PropertyChangedEventHandler PropertyChanged;

        private void OnPropertyChanged<T>(Expression<Func<T>> expr)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(((MemberExpression)expr.Body).Member.Name));
        }

        public ICommand GenerateRandomCommand => new RelayCommand(x =>
        {
            _primeFirst = GetRandomPrime();
            _primeSecond = GetRandomPrime();

            OnPropertyChanged(() => PrimeFirst);
            OnPropertyChanged(() => PrimeSecond);

            RecalculateParams();
        });

        public ICommand CopyValueCommand => new RelayCommand(x =>
        {
            Clipboard.SetText(x.ToString() == "1" ? PublicKey : PrivateKey);
        });

        public ICommand EncryptSessionKeyCommand => new RelayCommand(delegate 
        { 
            var PS = GetRandomHexNumber(128 - 16 - 3);
            var EB = $"0002{PS}00{SessionKey}";
            m = BigInteger.Parse(EB, System.Globalization.NumberStyles.AllowHexSpecifier);
            c = ModularPower(m, e, n);

            EncryptedSessionKey = c.ToString("X");
            OnPropertyChanged(() => EncryptedSessionKey);
        }, new Predicate<object>(_ => n != 0));

        public ICommand EncryptMessageCommand => new RelayCommand(delegate
        {
            var byteIV = Enumerable.Range(0, IV.Length / 2).Select(x => Convert.ToByte(IV.Substring(x * 2, 2), 16)).ToArray();

            var decrSessionMessage = ModularPower(
                BigInteger.Parse(EncryptedSessionKey, System.Globalization.NumberStyles.AllowHexSpecifier), 
                d, n);
            var decrSessionKey = decrSessionMessage.ToString("X2");
            decrSessionKey = decrSessionKey.Substring(decrSessionKey.Length - 32);

            var byteSession = Enumerable.Range(0, decrSessionKey.Length / 2).Select(x => Convert.ToByte(decrSessionKey.Substring(x * 2, 2), 16)).ToArray();
            _encryptedMessage = Encrypt(Message, byteSession, byteIV);
            OnPropertyChanged(() => EncryptedMessage);
        }, new Predicate<object>(_ => !string.IsNullOrWhiteSpace(Message)));

        public ICommand DecryptMessageCommand => new RelayCommand(delegate
        {
            try
            {
                var byteKey = Enumerable.Range(0, SessionKey.Length / 2).Select(x => Convert.ToByte(SessionKey.Substring(x * 2, 2), 16)).ToArray();
                var byteIV = Enumerable.Range(0, IV.Length / 2).Select(x => Convert.ToByte(IV.Substring(x * 2, 2), 16)).ToArray();
                EncryptedMessage = Decrypt(_encryptedMessage, byteKey, byteIV);
            }
            catch(Exception)
            {
                MessageBox.Show("Error occured. Message can't be decrypted", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }, new Predicate<object>(_ => !string.IsNullOrWhiteSpace(EncryptedMessage)));

        public string Message
        {
            get { return _message; }
            set
            {
                _message = value;
                OnPropertyChanged(() => Message);
            }
        }

        public string EncryptedMessage
        {
            get { return _encryptedMessage == null ? "" : Encoding.Default.GetString(_encryptedMessage); }
            set
            {
                _encryptedMessage = Encoding.ASCII.GetBytes(value);
                OnPropertyChanged(() => EncryptedMessage);
            }
        }

        public string PublicKey
        {
            get { return _publicKey; }
            set
            {
                _publicKey = value;
                OnPropertyChanged(() => PublicKey);
            }
        }

        public string PrivateKey
        {
            get { return _privateKey; }
            set
            {
                _privateKey = value;
                OnPropertyChanged(() => PrivateKey);
            }
        }

        public string PrimeFirst
        {
            get { return _primeFirst.ToString(); }
            set
            {
                var val = BigInteger.Parse(value);

                if (val == 0)
                {
                    MessageBox.Show("Choose a non-zero value", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                };

                if (IsPrime(val, accuracy))
                {
                    _primeFirst = val;

                    if(_primeSecond != 0) RecalculateParams();
                    OnPropertyChanged(() => PublicKey);
                }
                else
                {
                    MessageBox.Show($"Value {val} is not prime", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
        }

        public string PrimeSecond
        {
            get { return _primeSecond.ToString(); }
            set
            {
                var val = BigInteger.Parse(value);

                if (val == 0)
                {
                    MessageBox.Show("Choose a non-zero value", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                };

                if (IsPrime(val, accuracy))
                {
                    _primeSecond = val;

                    if (_primeFirst != 0) RecalculateParams();
                    OnPropertyChanged(() => PublicKey);
                }
                else
                {
                    MessageBox.Show($"Value {val} is not prime", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
        }

        public string SessionKey { get; set; }

        public string EncryptedSessionKey { get; set; }

        public string IV { get; set; }

        private void RecalculateParams()
        {
            n = _primeFirst * _primeSecond;
            var EulerFunc = (_primeFirst - 1) * (_primeSecond - 1);

            if (EulerFunc == 0)
            {
                MessageBox.Show("Some value is too small", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            ExtendedEuclidianAlgorithm(earr.Last(x => x < EulerFunc), EulerFunc, out d, out var ignore);

            PublicKey = $"{e}, {n}";
            PrivateKey = $"{e}, {d}";
        }

        public ViewModel()
        {
            SessionKey = GetRandomHexNumber(16);
            IV = GetRandomHexNumber(16);
            return;

            //var r = IsPrime(7451, 128);
            var pq = new BigInteger[] { GetRandomPrime(), GetRandomPrime() };
            var n = 1; var d = 1;
            //{e,n} - public key
            //{d,n} - private key

            var D = "4E636AF98E40F3ADCFCCB698F4E80B9F";
            var PS = GetRandomHexNumber(128 - 16 - 3);
            var EB = $"0002{PS}00{D}";
            var m = BigInteger.Parse(EB, System.Globalization.NumberStyles.AllowHexSpecifier);
            var c = ModularPower(m, e, n);
            var OB = c.ToString("X"); // encrypted session key

            var mes = "Hello world!";
            byte[] ba = Encoding.Default.GetBytes(mes);
            var hexstring = BitConverter.ToString(ba).Replace("-", "");

            var K = "5732164B3ABB6C4969ABA381C1CA75BA";
            var byteKey = Enumerable.Range(0, D.Length / 2).Select(x => Convert.ToByte(D.Substring(x * 2, 2), 16)).ToArray();
            var byteIV = Enumerable.Range(0, K.Length / 2).Select(x => Convert.ToByte(K.Substring(x * 2, 2), 16)).ToArray();

            var decrSessionKey = ModularPower(c, d, n);
            var parts = decrSessionKey.ToString("X2").Split(new string[] { "00" }, StringSplitOptions.None);
            var byteSession = Enumerable.Range(0, parts.Last().Length / 2).Select(x => Convert.ToByte(parts.Last().Substring(x * 2, 2), 16)).ToArray();
            var encr = Encrypt(mes, byteSession, byteIV);

            var decr = Decrypt(encr, byteKey, byteIV);
        }

        private BigInteger GetRandomPrime()
        {
            var k = N / 2;
            BigInteger num = 0;
            var rand = new Random();

            var bits = new BitArray(k);

            string str = "";

            for (int i = 0; i < k; i++)
            {
                if (i == 0 || i == 1 || i == 511)
                    str += "1";
                else
                    str += Convert.ToBoolean(rand.Next(0, 2)) ? "1" : "0";
            }

            num = BinToDec(str) - 2;
            do
            {
                num += 2;
            }
            // last part of condition is less-expensive test of gcd(num-1, e) == 1, coz e is odd prime
            while (!IsPrime(num, 128) && (num % e != 1));

            return num;
        }

        private bool IsPrime(BigInteger num, int accuracy)
        {
            if (num == 1 || num == 2 || num == 3) return true;
            if (num % 2 == 0) return false;

            var d = GetDNumber(num);

            for (int i = 0; i < accuracy; i++)
            {
                if (!MillerTest(num, d)) return false;
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

            while (d != n - 1)
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

            while (y > 0)
            {
                // if y is odd, multiply x with result
                if ((y & 1L) == 1)
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
            } while (!(R >= 2 && R <= max));

            return R;
        }

        public BigInteger BinToDec(string value)
        {
            BigInteger res = 0;

            // "I'm totally skipping error handling here" - пасеба чувак
            foreach (char c in value)
            {
                res <<= 1;
                res += c == '1' ? 1 : 0;
            }

            return res;
        }

        private BigInteger GetGCD(BigInteger a, BigInteger b)
        {
            while (b > 0)
            {
                var r = a % b;
                a = b;
                b = r;
            }

            return a;
        }

        private BigInteger ExtendedEuclidianAlgorithm(BigInteger a, BigInteger b, out BigInteger x, out BigInteger y)
        {
            var n = b;
            x = -1; y = -1;

            if (b == 0)
            {
                x = 1; y = 0;
                return a;
            }

            BigInteger x1 = 0, x2 = 1,
                y1 = 1, y2 = 0;

            while (b > 0)
            {
                var q = BigInteger.Divide(a, b);
                var r = a - q * b;
                x = x2 - q * x1;
                y = y2 - q * y1;

                a = b; b = r;
                x2 = x1; x1 = x;
                y2 = y1; y1 = y;
            }

            x = x2 > 0 ? x2 : x2 + n; y = y2; // d = a
            return a;
        }

        private static string GetRandomHexNumber(int bytes)
        {
            var random = new Random();
            byte[] buffer = new byte[bytes];
            random.NextBytes(buffer);
            string result = String.Concat(buffer.Select(x => x.ToString("X2")).ToArray());
            if (bytes % 2 == 0)
                return result;
            return result + random.Next(16).ToString("X");
        }

        static byte[] Encrypt(string plainText, byte[] Key, byte[] IV)
        {
            byte[] encrypted;
            // Create a new AesManaged.    
            using (AesManaged aes = new AesManaged())
            {
                // Create encryptor    
                ICryptoTransform encryptor = aes.CreateEncryptor(Key, IV);
                aes.Mode = CipherMode.CBC;

                // Create MemoryStream    
                using (MemoryStream ms = new MemoryStream())
                {
                    // Create crypto stream using the CryptoStream class. This class is the key to encryption    
                    // and encrypts and decrypts data from any given stream. In this case, we will pass a memory stream    
                    // to encrypt    
                    using (CryptoStream cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write))
                    {
                        // Create StreamWriter and write data to a stream    
                        using (StreamWriter sw = new StreamWriter(cs))
                            sw.Write(plainText);
                        encrypted = ms.ToArray();
                    }
                }
            }
            // Return encrypted data    
            return encrypted;
        }
        static string Decrypt(byte[] cipherText, byte[] Key, byte[] IV)
        {
            string plaintext = null;
            // Create AesManaged    
            using (AesManaged aes = new AesManaged())
            {
                // Create a decryptor    
                ICryptoTransform decryptor = aes.CreateDecryptor(Key, IV);
                aes.Mode = CipherMode.CBC;

                // Create the streams used for decryption.    
                using (MemoryStream ms = new MemoryStream(cipherText))
                {
                    // Create crypto stream    
                    using (CryptoStream cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Read))
                    {
                        // Read crypto stream    
                        using (StreamReader reader = new StreamReader(cs))
                            plaintext = reader.ReadToEnd();
                    }
                }
            }
            return plaintext;
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace RSA
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            DataContext = new ViewModel();
        }

        private void PublicKey_LostFocus(object sender, RoutedEventArgs e)
        {
            PublicKey.CaretIndex = 0;
            PrivateKey.CaretIndex = 0;
            FirstPrime.CaretIndex = 0;
            SecondPrime.CaretIndex = 0;
        }
    }
}

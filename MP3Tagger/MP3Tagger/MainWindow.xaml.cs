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

namespace MP3Tagger {
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window {
        public MainWindow() {
            InitializeComponent();
        }

        private void TextBox_GotFocus(object sender, RoutedEventArgs e) {
            var tb = sender as TextBox;
            if (tb.Text == "Search") {
                tb.Text = string.Empty;
            }

        }

        private void TextBox_LostFocus(object sender, RoutedEventArgs e) {
            var tb = sender as TextBox;
            if (string.IsNullOrWhiteSpace(tb.Text)) {
                tb.Text = "Search";
            }

        }
    }
}

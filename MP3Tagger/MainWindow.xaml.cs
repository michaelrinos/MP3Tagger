using MP3Tagger.ViewModels;
using System;
using System.Collections.Generic;
using System.IO;
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

        private void TextBox_PreviewKeyUp(object sender, KeyEventArgs e) {
            if (e.Key != System.Windows.Input.Key.Enter 
                || !(DataContext is MainViewModel)
                || !(sender is TextBox))
                return;

            var tb = sender as TextBox;
            var vm = DataContext as MainViewModel;
            vm.Search(vm.Items.First().Info.Information as DirectoryInfo , tb.Text); 

        }

        private void TreeView_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e) {
            var tv = sender as TreeView;
            if (DataContext != null && DataContext is MainViewModel) {
                var dc = DataContext as MainViewModel;
                dc.SelectedLocation = (FileSystemItemViewModel)e.NewValue;
            }

        }

        private void TreeView_PreviewKeyDown(object sender, KeyEventArgs e) {
            var tv = sender as TreeView;
            if (DataContext != null && DataContext is MainViewModel) {
                var dc = DataContext as MainViewModel;
                var text = dc.TreeText;
                dc.TreeText += e.Key;                
            }



        }
    }
}

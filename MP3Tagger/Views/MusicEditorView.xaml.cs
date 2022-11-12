using MP3Tagger.ViewModels;
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

namespace MP3Tagger.Views {
    /// <summary>
    /// Interaction logic for MusicEditorView.xaml
    /// </summary>
    public partial class MusicEditorView : UserControl {
        public MusicEditorView() {
            InitializeComponent();
        }

        private void DataGrid_AutoGeneratingColumn(object sender, DataGridAutoGeneratingColumnEventArgs e) {

        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            (this.DataContext as MusicEditorViewModel).RemoveDuplicates();
        }
        private void Button1_Click(object sender, RoutedEventArgs e)
        {
            (this.DataContext as MusicEditorViewModel).writeToFile();
        }
        private void dgList_ItemCreated(object sender, RoutedEventArgs e)
        {

            (sender as DataGrid).Columns[0].Visibility = Visibility.Collapsed;
            (sender as DataGrid).Columns[1].Visibility = Visibility.Collapsed;
            (sender as DataGrid).Columns[2].Visibility = Visibility.Collapsed;
            (sender as DataGrid).Columns[3].Visibility = Visibility.Collapsed;
            (sender as DataGrid).Columns[4].Visibility = Visibility.Collapsed;
        }
    }
}

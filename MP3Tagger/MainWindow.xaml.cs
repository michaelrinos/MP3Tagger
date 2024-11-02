using MP3Tagger.ViewModels;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
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
using System.Windows.Threading;

namespace MP3Tagger {
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window {

        private DispatcherTimer timer = null;

        public MainWindow(MainViewModel mainViewModel) {
            InitializeComponent();
            DataContext = mainViewModel;
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
            if (timer == null) {
                timer = new DispatcherTimer();
                timer.Interval = TimeSpan.FromSeconds(1);
                timer.Tick += HandleTick;
            } else {
                timer.Stop();
                timer.Interval = TimeSpan.FromMilliseconds(500);
                timer.Start();
            }
            var tv = sender as TreeView;
            var key = GetCharFromKey(e.Key);
            if (char.IsLetterOrDigit(key)) {
                if (DataContext != null && DataContext is MainViewModel) {
                    var dc = DataContext as MainViewModel;
                    if (string.IsNullOrEmpty(dc.TreeText)){
                        dc.TreeText = key.ToString();
                    } else {
                        dc.TreeText += key.ToString();
                    }
                }
            } else {
                if (e.Key == Key.Escape) {
                    if (DataContext != null && DataContext is MainViewModel) {
                        var dc = DataContext as MainViewModel;
                        dc.TreeText = string.Empty;
                    }
                }
            }
        }

        private void HandleTick(object sender, EventArgs e) {
            timer.Stop();
            timer = null;
            if (DataContext is MainViewModel) {
                ((MainViewModel)DataContext).TreeText = string.Empty;
            }
        }

        public enum MapType : uint {
            MAPVK_VK_TO_VSC = 0x0,
            MAPVK_VSC_TO_VK = 0x1,
            MAPVK_VK_TO_CHAR = 0x2,
            MAPVK_VSC_TO_VK_EX = 0x3,
        }

        [DllImport("user32.dll")]
        public static extern int ToUnicode(
            uint wVirtKey,
            uint wScanCode,
            byte[] lpKeyState,
            [Out, MarshalAs(UnmanagedType.LPWStr, SizeParamIndex = 4)]
            StringBuilder pwszBuff,
            int cchBuff,
            uint wFlags);

        [DllImport("user32.dll")]
        public static extern bool GetKeyboardState(byte[] lpKeyState);

        [DllImport("user32.dll")]
        public static extern uint MapVirtualKey(uint uCode, MapType uMapType);

        public static char GetCharFromKey(Key key) {
            char ch = ' ';

            int virtualKey = KeyInterop.VirtualKeyFromKey(key);
            byte[] keyboardState = new byte[256];
            GetKeyboardState(keyboardState);

            uint scanCode = MapVirtualKey((uint)virtualKey, MapType.MAPVK_VK_TO_VSC);
            StringBuilder stringBuilder = new StringBuilder(2);

            int result = ToUnicode((uint)virtualKey, scanCode, keyboardState, stringBuilder, stringBuilder.Capacity, 0);
            switch (result) {
                case -1:
                break;
                case 0:
                break;
                case 1: {
                    ch = stringBuilder[0];
                    break;
                }
                default: {
                    ch = stringBuilder[0];
                    break;
                }
            }
            return ch;
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (DataContext != null && DataContext is MainViewModel)
            {
                var dc = DataContext as MainViewModel;
                dc.DoShutdown();
            }

        }

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if (DataContext != null && DataContext is MainViewModel)
            {
                var dc = DataContext as MainViewModel;
                dc.DoKeyPress(e);
            }

        }
    }
}


using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows.Media.Imaging;

namespace MP3Tagger.Converters {
    [ValueConversion(typeof(string), typeof(bool))]
    public class PathTypeToImage : IValueConverter {
        

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture) {
            Uri path;
            if ((value as string).Contains(@"\")) {
                path = new Uri("pack://application:,,,/Images/hard_drive.png");
            } else {
                path = new Uri("pack://application:,,,/Images/folder_closed.png");

            }
            var source = new BitmapImage(path);
            return source;
            
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) {
            throw new NotSupportedException("Cannot convert image to path");
        }
    }
}

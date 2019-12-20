using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

namespace MP3Tagger.Converters {
    // /*
    public class ArrayValuesToString : IValueConverter {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {

            if (value == null)
                return null;
            if (value is Array) {
                Array t = value as Array;
                try {
                    return t.GetValue(0).ToString();
                }catch {
                    return new object();
                }
            }
            return new object();

        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return null;
        }
    }
    // */
}

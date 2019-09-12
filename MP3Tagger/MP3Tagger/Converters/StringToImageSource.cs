using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace MP3Tagger.Converters {
    [ValueConversion(typeof(string), typeof(ImageSource))]
    public class StringToImageSource : IValueConverter {
        /// <summary>
        /// Converts a bool value into a System string value dependent on the value(s) of the False/TrueStringValue an IsInverted
        /// </summary>
        /// <param name="value">The Value produced by the binding source</param>
        /// <param name="targetType">The type of the binding target</param>
        /// <param name="parameter">The converter parameter to use</param>
        /// <param name="culture">The culture to use in the converter</param>
        /// <returns>A System.string value dependant on the value of the False/TrueString value and IsInverted properties</returns>
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture) {
            if (value == null || value.GetType() != typeof(string))
                return null;
            ViewModels.Type type; 
            if (parameter is ViewModels.Type) {
                type = (ViewModels.Type) parameter;
            }
            string stringValue = value as string;
            var icon = Icon.ExtractAssociatedIcon(stringValue);
            return Imaging.CreateBitmapSourceFromHIcon(icon.Handle, System.Windows.Int32Rect.Empty, BitmapSizeOptions.FromWidthAndHeight(16, 16));
        }

        /// <summary>
        /// Converts a System.Icon value into a boolean value dependent on the boolean input value and the IsInverted
        /// </summary>
        /// <param name="value">The Value produced by the binding source</param>
        /// <param name="targetType">The type of the binding target</param>
        /// <param name="parameter">The converter parameter to use</param>
        /// <param name="culture">The culture to use in the converter</param>
        /// <returns>A boolean value dependent on the boolean input value and the IsInverted property</returns>
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) {
            return null;
        }
    }
}

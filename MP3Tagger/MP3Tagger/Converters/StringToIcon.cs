using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Drawing;

namespace MP3Tagger.Converters {
    [ValueConversion(typeof(string), typeof(Icon))]
    public class StringToIcon : IValueConverter {
        /// <summary>
        /// Converts a bool value into a System string value dependent on the value(s) of the False/TrueStringValue an IsInverted
        /// </summary>
        /// <param name="value">The Value produced by the binding source</param>
        /// <param name="targetType">The type of the binding target</param>
        /// <param name="parameter">The converter parameter to use</param>
        /// <param name="culture">The culture to use in the converter</param>
        /// <returns>A System.string value dependant on the value of the False/TrueString value and IsInverted properties</returns>
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture) {
            if (value == null || value.GetType() != typeof(bool))
                return null;
            string stringValue = value as string;
            return Icon.ExtractAssociatedIcon(stringValue);
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

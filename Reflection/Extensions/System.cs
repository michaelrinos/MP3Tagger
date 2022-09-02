using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Reflection;

namespace Reflection {
	public static partial class SystemExtensions {
        public static T Clone<T>(this T item) where T : new()
        {
            var bytes = Compression.SerializeAndCompressToBinary(item);
            return (T)Compression.DeserializeAndDecompressFromBinary(bytes);
        }

        public static object Clone(this object item)
        {
            var bytes = Compression.SerializeAndCompressToBinary(item);
            return Compression.DeserializeAndDecompressFromBinary(bytes);
        }

        /// <summary>
        /// Create a clone from base class instance
        /// </summary>
        /// <typeparam name="TD">Derived class type</typeparam>
        /// <typeparam name="TB">Base class type</typeparam>
        /// <param name="derivedClassInstance">Derived instance of <typeparamref name="TD"/></param>
        /// <param name="baseClassInstance">Base instance of <typeparamref name="TB"/></param>
        /// <param name="attributeFilters">Filter properties decorated with attribute <see cref="Type"/></param>
        /// <param name="clonePrivate">Include private properties</param>
        /// <returns>An instance of <typeparam name="TD"></typeparam></returns>
        public static TD Clone<TD, TB>(this TD derivedClassInstance, TB baseClassInstance, IEnumerable<Type> attributeFilters = null, bool clonePrivate = false)
            where TD : class, TB
        {
            var baseTypeProperties = baseClassInstance.GetType().GetProperties()
                .Where(pi => pi.CanWrite);

            // Exclude properties with private setters
            if (!clonePrivate)
                baseTypeProperties = baseTypeProperties.Where(pi => !pi.GetSetMethod(true).IsPrivate);

            var fields = attributeFilters == null
                // No filters
                ? baseTypeProperties

                // Filter properties decorated with attributes
                : baseTypeProperties.Where(pi => attributeFilters.Sum(af => pi.GetCustomAttributes(af, false).Length) > 0);

            // Iterate through the fields
            foreach (var propertyInfo in fields)
            {
                // Get property value from base instance
                var value = propertyInfo.GetValue(baseClassInstance, null);

                // Set derived instance property value
                if (null != value) propertyInfo.SetValue(derivedClassInstance, value, null);
            }

            return derivedClassInstance;
        }

        /// <summary>
        /// Copy values from another instance
        /// </summary>
        /// <typeparam name="TA">Type of instance A</typeparam>
        /// <typeparam name="TB">Type of instance B</typeparam>
        /// <param name="instanceA">Instance of <typeparamref name="TA"/></param>
        /// <param name="instanceB">Instance of <typeparamref name="TB"/></param>
        /// <param name="propertyMap">Property mapping</param>
        /// <param name="clonePrivate">Include private properties</param>
        /// <returns>An instance of <typeparam name="TA"></typeparam></returns>
        public static TA Copy<TA, TB>(this TA instanceA, TB instanceB, NameValueCollection propertyMap = null, bool clonePrivate = false)
            where TA : class
            where TB : class
        {
            return instanceA.Copy(instanceB, GetPropertyMap(instanceA, instanceB, propertyMap, clonePrivate));
        }

        /// <summary>
        /// Copy values from another instance
        /// </summary>
        /// <typeparam name="TA">Type of instance A</typeparam>
        /// <typeparam name="TB">Type of instance B</typeparam>
        /// <param name="instanceA">Instance of <typeparamref name="TA"/></param>
        /// <param name="instanceB">Instance of <typeparamref name="TB"/></param>
        /// <param name="propertyMapping">Property mapping</param>
        /// <returns>An instance of <typeparam name="TA"></typeparam></returns>
        public static TA Copy<TA, TB>(
            this TA instanceA, TB instanceB,
            IEnumerable<KeyValuePair<PropertyInfo, PropertyInfo>> propertyMapping)
            where TA : class
            where TB : class
        {
            // Iterate through the property mappings
            foreach (var map in propertyMapping)
            {
                // Get property value from base instance
                var value = map.Value.GetValue(instanceB, null);

                // Set derived instance property value
                if (null != value) map.Key.SetValue(instanceA, value, null);
            }

            return instanceA;
        }

        /// <summary>
        /// Get an enumerable list of property mappings
        /// </summary>
        /// <typeparam name="TA">Type of instance A</typeparam>
        /// <typeparam name="TB">Type of instance B</typeparam>
        /// <param name="instanceA">Instance of <typeparamref name="TA"/></param>
        /// <param name="instanceB">Instance of <typeparamref name="TB"/></param>
        /// <param name="propertyMap">Property mapping</param>
        /// <param name="clonePrivate">Include private properties</param>
        /// <returns>A list of property associations</returns>
        public static IEnumerable<KeyValuePair<PropertyInfo, PropertyInfo>> GetPropertyMap<TA, TB>(
            this TA instanceA, TB instanceB, NameValueCollection propertyMap = null, bool clonePrivate = false)
            where TA : class
            where TB : class
        {
            if (instanceA == null || instanceB == null)
                return Enumerable.Empty<KeyValuePair<PropertyInfo, PropertyInfo>>();

            return GetPropertyMap(instanceA.GetType(), instanceB.GetType(), propertyMap, clonePrivate);
        }

        /// <summary>
        /// Get an enumerable list of property mappings
        /// </summary>
        /// <param name="instanceAType">Type of instanceA</param>
        /// <param name="instanceBType">type of instanceB </param>
        /// <param name="propertyMap">Property mapping</param>
        /// <param name="clonePrivate">Include private properties</param>
        /// <returns>A list of property associations</returns>
        public static IEnumerable<KeyValuePair<PropertyInfo, PropertyInfo>> GetPropertyMap(
            this Type instanceAType, Type instanceBType, NameValueCollection propertyMap = null, bool clonePrivate = false)
        {
            var instanceAProperties = instanceAType.GetProperties()
                .Where(pi => pi.CanWrite);

            // Exclude properties with private setters
            if (!clonePrivate)
                instanceAProperties = instanceAProperties.Where(pi => !pi.GetSetMethod(true).IsPrivate);

            var instanceBProperties = instanceBType.GetProperties()
                .Where(pi => pi.CanWrite);

            return
                // Iterate through instanceAProperties
                from propertyInfo in instanceAProperties

                let instanceBProperty = propertyMap?[propertyInfo.Name] == null
                    // Match property names
                    ? instanceBProperties.FirstOrDefault(
                        x => string.Equals(x.Name, propertyInfo.Name, StringComparison.InvariantCultureIgnoreCase)
                             && x.PropertyType == propertyInfo.PropertyType)

                    // Handle custom mapping
                    : instanceBProperties.FirstOrDefault(
                        x => string.Equals(x.Name, propertyMap[propertyInfo.Name], StringComparison.InvariantCultureIgnoreCase))
                where instanceBProperty != null
                select new KeyValuePair<PropertyInfo, PropertyInfo>(propertyInfo, instanceBProperty);
        }

        public static object GetDefault(this Type type)
        {
            if (type.IsValueType)
            {
                return Activator.CreateInstance(type);
            }
            return null;
        }

        public static string ToYesNo(this bool? input)
        {
            if (input.HasValue == false)
                return null;

            return (input.Value) ? "Yes" : "No";
        }

        public static string FormatCommas(this int input)
        {
            return String.Format("{0:n0}", input);
        }

        public static string FormatCommas(this int? input)
        {
            return String.Format("{0:n0}", input);
        }

        public static string FormatCurrency(this float input, int decimals = 0)
        {
            return String.Format("{0:c" + decimals + "}", input);
        }

        public static string FormatCurrency(this float? input, int decimals = 0)
        {
            return String.Format("{0:c" + decimals + "}", input);
        }

        public static string FormatCurrency(this double input, int decimals = 0)
        {
            return String.Format("{0:c" + decimals + "}", input);
        }

        public static string FormatCurrency(this double? input, int decimals = 0)
        {
            return String.Format("{0:c" + decimals + "}", input);
        }

        public static string FormatCurrency(this decimal input, int decimals = 0)
        {
            return String.Format("{0:c" + decimals + "}", input);
        }

        public static string FormatCurrency(this decimal? input, int decimals = 0)
        {
            return String.Format("{0:c" + decimals + "}", input);
        }

        public static string FormatPercent(this float input, int decimals = 0)
        {
            if (float.IsNaN(input))
                return "";

            return String.Format("{0:p" + decimals + "}", input).Replace(" ", "");
        }

        public static string FormatPercent(this float? input, int decimals = 0)
        {
            if (input.HasValue == false || float.IsNaN(input.Value))
                return "";

            return String.Format("{0:p" + decimals + "}", input).Replace(" ", "");
        }

        public static string FormatPercent(this double input, int decimals = 0)
        {
            if (double.IsNaN(input))
                return "";

            return String.Format("{0:p" + decimals + "}", input).Replace(" ", "");
        }

        public static string FormatPercent(this double? input, int decimals = 0)
        {
            if (input.HasValue == false || double.IsNaN(input.Value))
                return "";

            return String.Format("{0:p" + decimals + "}", input).Replace(" ", "");
        }

        public static string FormatPercent(this decimal input, int decimals = 0)
        {
            return String.Format("{0:p" + decimals + "}", input).Replace(" ", "");
        }

        public static string FormatPercent(this decimal? input, int decimals = 0)
        {
            return String.Format("{0:p" + decimals + "}", input).Replace(" ", "");
        }

        public static string FormatDecimals(this decimal value, int maxDecimals = 2)
        {
            return value.ToString("0." + new String('#', maxDecimals));
        }

        public static string FormatDecimals(this decimal? value, int maxDecimals = 2, string valueIfNull = "")
        {
            if (value.HasValue == false)
                return valueIfNull;

            return FormatDecimals(value.Value, maxDecimals);
        }

        public static string FormatDecimals(this double value, int maxDecimals = 2)
        {
            return FormatDecimals(Convert.ToDecimal(value), maxDecimals);
        }

        public static string FormatDecimals(this double? value, int maxDecimals = 2, string valueIfNull = "")
        {
            if (value.HasValue == false || double.IsNaN(value.Value))
                return valueIfNull;

            return FormatDecimals(value.Value, maxDecimals);
        }

        /// <summary>
        /// Use to truncate fractions of a penny (as opposed to round)
        /// </summary>
        public static decimal TruncateDecimals(this decimal value, int maxDecimals = 2)
        {
            var t = Convert.ToDecimal(Math.Pow(10, maxDecimals));
            return Math.Truncate(value * t) / t;
        }

        public static Guid ToGuid(this int value)
        {
            byte[] bytes = new byte[16];
            BitConverter.GetBytes(value).CopyTo(bytes, 0);
            return new Guid(bytes);
        }

        public static int ToInt(this Guid guid)
        {
            byte[] bytes = guid.ToByteArray();
            return BitConverter.ToInt32(bytes, 0);
        }

        public static string Romanize(this int number)
        {
            if (number < 1 || number > 3999) return "Number out of range...";
            string result = string.Empty;

            while (number > 0)
            {
                string numberStr = number.ToString();
                string simbol1 = string.Empty;
                string simbol2 = string.Empty;
                string simbol3 = string.Empty;

                switch (numberStr.Length)
                {
                    case 4: // Units
                        simbol1 = "I"; simbol2 = "V"; simbol3 = "X";
                        break;
                    case 3: // Tens
                        simbol1 = "X"; simbol2 = "L"; simbol3 = "C";
                        break;
                    case 2: // Hundreads
                        simbol1 = "C"; simbol2 = "D"; simbol3 = "M";
                        break;
                    case 1: // Thousands
                        simbol1 = "M"; simbol2 = ""; simbol3 = "";
                        break;
                }
                result = _toRoman(number % 10, simbol1, simbol2, simbol3) + result;
                number = number / 10;
            }

            return result;
        }

        private static string _toRoman(int v, string simbol1, string simbol2, string simbol3)
        {
            string result = string.Empty;

            switch (v)
            {
                case 3:
                case 2:
                case 1: while (v-- > 0) result = simbol1 + result; break;
                case 4: result = simbol1 + simbol2; break;
                case 8:
                case 7:
                case 6:
                case 5:
                    result = simbol2;
                    while (v-- > 5) result = result + simbol1; break;
                case 9:
                    result = simbol1 + simbol3; break;
            }

            return result;
        }
    }
}
// */
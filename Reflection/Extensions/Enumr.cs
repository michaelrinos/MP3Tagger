using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;

namespace Reflection {
	public static class Enumr {
		public static IEnumerable<T> GetAttribute<T>(FieldInfo fi) {
			var val = (Enum)fi.GetValue(null);
			var att = fi.GetCustomAttributes(typeof(T), true);
			return att.Cast<T>();
		}

		public static KeyValuePair<Enum, string> GetDisplayName(FieldInfo fi) {
			var val = (Enum)fi.GetValue(null);
			var att = GetAttribute<DisplayNameAttribute>(fi);
			var name = att.Any()
				? att.First().DisplayName
				: val.ToString().Humanize();
			return new KeyValuePair<Enum, string>(val, name);
		}

		/// <summary>
		///     Gets the next enumeration item
		/// </summary>
		/// <typeparam name="T">Enumeration</typeparam>
		/// <param name="src">Enumeration item</param>
		/// <returns>Next enumeration item of <typeparamref name="T" /></returns>
		public static T Next<T>(this T src) where T : struct {
			if (!typeof(T).IsEnum)
				throw new ArgumentException($"Argument {typeof(T).FullName} is not an Enum");

			var array = (T[])Enum.GetValues(src.GetType());
			var nextItem = Array.IndexOf(array, src) + 1;
			return array.Length <= nextItem ? array[0] : array[nextItem];
		}

		/// <summary>
		///     Gets the previous enumeration item
		/// </summary>
		/// <typeparam name="T">Enumeration</typeparam>
		/// <param name="src">Enumeration item</param>
		/// <returns>Next enumeration item of <typeparamref name="T" /></returns>
		public static T Previous<T>(this T src) where T : struct {
			if (!typeof(T).IsEnum)
				throw new ArgumentException($"Argument {typeof(T).FullName} is not an Enum");

			var array = (T[])Enum.GetValues(src.GetType());
			var previousItem = Array.IndexOf(array, src) - 1;
			return array.Length <= previousItem ? array[array.Length - 1] : array[previousItem];
		}

		#region Flags

		public static bool IsSet(this object flags, object flag) {
			if (flags.GetType() != flag.GetType())
				throw new InvalidOperationException("flag types must match");

			var flagValue = Convert.ToInt64(flag);
			if (flagValue == 0)
				return false;

			var flagsValue = Convert.ToInt64(flags);

			return (flagsValue & flagValue) != 0;
		}

		public static T Set<T>(this T flags, T flag) where T : struct {
			var flagsValue = Convert.ToInt64(flags);
			var flagValue = Convert.ToInt64(flag);

			return (T)Enum.ToObject(typeof(T), flagsValue | flagValue);
		}

		public static T Unset<T>(this T flags, T flag) where T : struct {
			var flagsValue = Convert.ToInt64(flags);
			var flagValue = Convert.ToInt64(flag);

			return (T)Enum.ToObject(typeof(T), flagsValue & ~flagValue);
		}

		public static List<T> AllSet<T>(this T flags) where T : struct {
			return typeof(T).GetFields(BindingFlags.Public | BindingFlags.Static)
				.Where(f => flags.IsSet((T)f.GetValue(null)))
				.Select(f => (T)f.GetValue(null))
				.ToList();
		}

		public static string AllSetToString<T>(this T flags, string delim = ", ") where T : struct {
			return typeof(T).GetFields(BindingFlags.Public | BindingFlags.Static)
				.Where(f => flags.IsSet((T)f.GetValue(null)))
				.Select(s => GetDisplayName(s).Value)
				.Aggregate((a, b) => string.Format("{0}, {1}", a, b));
		}

		#endregion

		#region T Parse<T>

		/// <summary>
		///     Parses an string value into the specified enumerator
		/// </summary>
		/// <typeparam name="T">The enum to parse into</typeparam>
		/// <param name="value">The string value to parse</param>
		/// <param name="ignoreCase">true to ignore case; false to regard case</param>
		/// <returns></returns>
		public static T Parse<T>(string value, bool ignoreCase = false)
			where T : struct, IComparable, IFormattable, IConvertible {
			return (T)Enum.Parse(typeof(T), value, ignoreCase);
		}

		/// <summary>
		///     Parses an string value into the specified enumerator
		/// </summary>
		/// <typeparam name="T">The enum to parse into</typeparam>
		/// <param name="value">The value of the enumerator to parse</param>
		/// <returns></returns>
		public static T Parse<T>(byte? value) where T : struct, IComparable, IFormattable, IConvertible {
			return Parse<T>(value.ToString());
		}

		/// <summary>
		///     Parses an string value into the specified enumerator
		/// </summary>
		/// <typeparam name="T">The enum to parse into</typeparam>
		/// <param name="value">The value of the enumerator to parse</param>
		/// <returns></returns>
		public static T Parse<T>(byte value) where T : struct, IComparable, IFormattable, IConvertible {
			return Parse<T>(value.ToString());
		}

		/// <summary>
		///     Parses an string value into the specified enumerator
		/// </summary>
		/// <typeparam name="T">The enum to parse into</typeparam>
		/// <param name="value">The value of the enumerator to parse</param>
		/// <returns></returns>
		public static T Parse<T>(short? value) where T : struct, IComparable, IFormattable, IConvertible {
			return Parse<T>(value.ToString());
		}

		/// <summary>
		///     Parses an string value into the specified enumerator
		/// </summary>
		/// <typeparam name="T">The enum to parse into</typeparam>
		/// <param name="value">The value of the enumerator to parse</param>
		/// <returns></returns>
		public static T Parse<T>(short value) where T : struct, IComparable, IFormattable, IConvertible {
			return Parse<T>(value.ToString());
		}

		/// <summary>
		///     Parses an string value into the specified enumerator
		/// </summary>
		/// <typeparam name="T">The enum to parse into</typeparam>
		/// <param name="value">The value of the enumerator to parse</param>
		/// <returns></returns>
		public static T Parse<T>(int? value) where T : struct, IComparable, IFormattable, IConvertible {
			return Parse<T>(value.ToString());
		}

		/// <summary>
		///     Parses an string value into the specified enumerator
		/// </summary>
		/// <typeparam name="T">The enum to parse into</typeparam>
		/// <param name="value">The value of the enumerator to parse</param>
		/// <returns></returns>
		public static T Parse<T>(int value) where T : struct, IComparable, IFormattable, IConvertible {
			return Parse<T>(value.ToString());
		}

		/// <summary>
		///     Parses an string value into the specified enumerator
		/// </summary>
		/// <typeparam name="T">The enum to parse into</typeparam>
		/// <param name="value">The value of the enumerator to parse</param>
		/// <returns></returns>
		public static T Parse<T>(long? value) where T : struct, IComparable, IFormattable, IConvertible {
			return Parse<T>(value.ToString());
		}

		/// <summary>
		///     Parses an string value into the specified enumerator
		/// </summary>
		/// <typeparam name="T">The enum to parse into</typeparam>
		/// <param name="value">The value of the enumerator to parse</param>
		/// <returns></returns>
		public static T Parse<T>(long value) where T : struct, IComparable, IFormattable, IConvertible {
			return Parse<T>(value.ToString());
		}

		/// <summary>
		///     Parses an string value into the specified enumerator
		/// </summary>
		/// <typeparam name="T">The enum to parse into</typeparam>
		/// <param name="value">The value of the enumerator to parse</param>
		/// <remarks>Uses string value for conversion</remarks>
		public static T Parse<T>(Enum value) where T : struct, IComparable, IFormattable, IConvertible {
			return Parse<T>(value.ToString());
		}

		/// <summary>
		///     Parses an string value into the specified enumerator
		/// </summary>
		/// <param name="type">The enum type</param>
		/// <param name="value">The value of the enumerator to parse</param>
		/// <returns></returns>
		public static object Parse(Type type, string value, bool ignoreCase = false) {
			var safeType = Nullable.GetUnderlyingType(type) ?? type;
			try {
				return Enum.Parse(safeType, value);
			} catch (Exception) {
				foreach (var name in Enum.GetNames(safeType)) {
					var enumMemberAttribute =
						((EnumMemberAttribute[])safeType
						.GetField(name)
						.GetCustomAttributes(typeof(EnumMemberAttribute), true))
						.Single();
					if (string.Compare(enumMemberAttribute.Value, value, ignoreCase) != 0)
						continue;

					return Enum.Parse(safeType, name);
				}
			}

			return null;
		}

		#endregion

		#region T TryParse<T>

		/// <summary>
		///     Parses an string value into the specified enumerator
		/// </summary>
		/// <typeparam name="T">The enum to parse into</typeparam>
		/// <param name="value">The string value to parse</param>
		/// <returns></returns>
		public static bool TryParse<T>(string value, out T result) where T : struct, IComparable, IFormattable, IConvertible {
			return Enum.TryParse(value, false, out result);
		}

		/// <summary>
		///     Parses an string value into the specified enumerator
		/// </summary>
		/// <typeparam name="T">The enum to parse into</typeparam>
		/// <param name="value">The string value to parse</param>
		/// <param name="ignoreCase">true to ignore case; false to regard case</param>
		/// <returns></returns>
		public static bool TryParse<T>(string value, bool ignoreCase, out T result)
			where T : struct, IComparable, IFormattable, IConvertible {
			return Enum.TryParse(value, ignoreCase, out result);
		}

		/// <summary>
		///     Parses an string value into the specified enumerator
		/// </summary>
		/// <typeparam name="T">The enum to parse into</typeparam>
		/// <param name="value">The value of the enumerator to parse</param>
		/// <remarks>Uses string value for conversion</remarks>
		public static bool TryParse<T>(Enum value, out T result) where T : struct, IComparable, IFormattable, IConvertible {
			return TryParse(value.ToString(), out result);
		}

		#endregion

		/// <summary>
		/// Function to count how many bit flags are set for an enum
		/// </summary>
		/// <param name="lValue"></param>
		/// <returns>number of set bits</returns>
		public static int CountBits<T>(this T flags) where T : struct {
			if (!typeof(T).IsEnum)
				throw new ArgumentException($"Argument {typeof(T).FullName} is not an Enum");
			var lValue = Convert.ToInt64(flags);
			int iCount = 0;

			//Loop the value while there are still bits
			while (lValue != 0) {
				//Remove the end bit
				lValue = lValue & (lValue - 1);

				//Increment the count
				iCount++;
			}

			//Return the count
			return iCount;
		}

	}
}
using System;
using System.Collections.Generic;
using System.Text;
using System.Data;
using System.Globalization;
using System.Xml.Serialization;
using System.IO;

namespace Data
{
	public static class Reflector
	{
		public static ICollection<T> Reflect<T>(DataTable dt)
		{
			var result = new List<T>();

			foreach (DataRow row in dt.Rows)
			{
				T item;

				if (typeof(T).IsInterface)
				{
					throw new TypeInitializationException(typeof(T).FullName, new ArgumentException("Cannot instantiate an interface"));
				}
				else if (typeof(T) == typeof(string) // special case (is a class, but not constructor)
					|| !typeof(T).IsClass) // value type
				{
					item = (T)ReflectValue(typeof(T), row[0]);
				}
				else
				{
					item = Activator.CreateInstance<T>();
					foreach (var prop in typeof(T).GetProperties())
					{
						if (prop.CanWrite && dt.Columns.Contains(prop.Name))
						{
							//special handling for comma-delimited list to string array
							var val = (prop.PropertyType == typeof(String[])) ? row[prop.Name].ToString().Split(',') : row[prop.Name];

							prop.SetValue(item, ReflectValue(prop.PropertyType, val), null);
						}
					}
				}
				result.Add(item);
			}

			return result;
		}

		/// <summary>
		/// Attempts to reflect a value to a given type from any other type
		/// </summary>
		/// <param name="t">The output type</param>
		/// <param name="value">The value to reflect</param>
		/// <param name="format">The format of the value (used for reflecting to DateTime only)</param>
		/// <returns></returns>
		public static object ReflectValue(Type t, object value, string format = "")
		{
			try
			{
				// enums
				if ((Nullable.GetUnderlyingType(t) ?? t).IsEnum)
				{
					if (Nullable.GetUnderlyingType(t) != null && value == DBNull.Value) // nullable enum; null value
						return null;
					else
						return Enum.Parse((Nullable.GetUnderlyingType(t) ?? t), value.ToString());
				}
				// null strings
				else if (value == DBNull.Value && t == typeof(string))
					return Convert.ChangeType(String.Empty, t);
				// null everything else
				else if (value == DBNull.Value)
					return null;
				// datetime
				else if (t == typeof(DateTime))
				{
					if (String.IsNullOrEmpty(format))
						return DateTime.Parse(value.ToString());
					else
						return DateTime.ParseExact(value.ToString(), format, CultureInfo.InvariantCulture);
				}
				// value types and arrays
				else if (t.IsValueType || t.IsArray)
					return Convert.ChangeType(value, Nullable.GetUnderlyingType(t) ?? t);
				// strings
				else if (t == typeof(String))
					return value.ToString();
				// non-string reference types (ie. objects) xml implicit
				else
				{
					using (TextReader reader = new StringReader(value.ToString()))
					{
						return new XmlSerializer(t).Deserialize(reader);
					}
				}

			}
			catch (FormatException fEx)
			{
				throw new FormatException(String.Format("Cannot convert '{0}' to type {1}", value.ToString(), t.Name), fEx);
			}
		}
	}
}

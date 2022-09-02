
using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Runtime.Serialization;
using Newtonsoft.Json;
using Reflection;

namespace Reflection {
	public static class Reflection {
		/// <summary>
		///     Updates the properties of item with the input data row
		/// </summary>
		public static T Reflect<T>(DataTable dt, DataRow row, T item) {
			// T could be a boxed type; use .GetType() to get the unboxed type

			foreach (var prop in item.GetType().GetProperties()) {
				var propName = prop.Name;

				var attr =
					(SqlQueryParameterAttribute)prop.GetCustomAttributes(typeof(SqlQueryParameterAttribute), false).FirstOrDefault() ??
					new SqlQueryParameterAttribute();

				// short-circuit on ignore prop
				if (attr.Ignore)
					continue;

				// ParameterName
				if (!string.IsNullOrEmpty(attr.ParameterName))
					propName = attr.ParameterName;

				// Value
				if (!prop.CanWrite || !dt.Columns.Contains(propName))
					continue;

				// Serialization
				if (attr.Serialize != SerializationType.None) {
					if (attr.Serialize == SerializationType.Json) {
						prop.SetValue(item, JsonConvert.DeserializeObject(row[propName].ToString(), prop.PropertyType), null);
					}
					else
						throw new NotImplementedException("Serialization from " + attr.Serialize + " not implemented");
				} else {
					//special handling for comma-delimited list to string array
					var val = prop.PropertyType == typeof(string[])
						? row[propName].ToString().Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
						: row[propName];

					prop.SetValue(item, ReflectValue(prop.PropertyType, val), null);
				}
			}

			return item;
		}

		public static ICollection<T> Reflect<T>(DataTable dt) {
			var result = new List<T>();

			foreach (DataRow row in dt.Rows) {
				T item;

				if (typeof(T).IsInterface) {
					throw new TypeInitializationException(typeof(T).FullName, new ArgumentException("Cannot instantiate an interface"));
				}
				if (typeof(T) == typeof(string) // special case (is a class, but not constructor)
					|| !typeof(T).IsClass) // value type
				{
					item = (T)ReflectValue(typeof(T), row[0]);
				} else {
					item = Activator.CreateInstance<T>();

					var type = item.GetType();

					var start = type.GetMethod("OnReflectionStart");
					if (start != null)
						start.Invoke(item, null);

					Reflect(dt, row, item);

					var end = type.GetMethod("OnReflectionEnd");
					if (end != null)
						end.Invoke(item, null);
				}

				result.Add(item);
			}

			return result;
		}

		/// <summary>
		///     Attempts to reflect a value to a given type from any other type
		/// </summary>
		/// <param name="t">The output type</param>
		/// <param name="value">The value to reflect</param>
		/// <param name="format">The format of the value (used for reflecting to DateTime only)</param>
		/// <returns></returns>
		public static object ReflectValue(Type t, object value, string format = "") {
			try {
				var valueClassType = value.GetType();

				// enums
				if ((Nullable.GetUnderlyingType(t) ?? t).IsEnum) {
					// null value
					if (value == DBNull.Value) {
						// nullable enum => null
						if (t.IsGenericType && t.GetGenericTypeDefinition() == typeof(Nullable<>))
							return null;
						// not-nullable enum => default
						else
							return Activator.CreateInstance(t);
					}

					// parse value
					return Enum.Parse(Nullable.GetUnderlyingType(t) ?? t, value.ToString());
				}

				//rg/os: 6/2/2017 - testing what happens if we treat strings as null
				// null'ing strings causes problems when we reflect an object back from the db and null strings become unexpected empty strings
				// null
				if (value == DBNull.Value)
					return null;

				// datetime
				if (t == typeof(DateTime)) {
					if (string.IsNullOrEmpty(format))
						return DateTime.Parse(value.ToString());
					return DateTime.ParseExact(value.ToString(), format, CultureInfo.InvariantCulture);
				}
				// timespans
				if ((t == typeof(TimeSpan) || t == typeof(TimeSpan?)) && valueClassType == typeof(int))
					return new TimeSpan(0, 0, 0, 0, (int)value);
				if ((t == typeof(TimeSpan) || t == typeof(TimeSpan?)) && valueClassType == typeof(long))
					return TimeSpan.FromMilliseconds((long)value);
				//guids
				if (t == typeof(Guid))
					return Guid.Parse(value.ToString());
				// value types and arrays
				if (t.IsValueType || t.IsArray) {
					if (value.ToString() == "")
						return Activator.CreateInstance(t);

					return Convert.ChangeType(value, Nullable.GetUnderlyingType(t) ?? t);
				}
				// strings
				if (t == typeof(string))
					return value.ToString();
				// non-string reference types (ie. objects) xml implicit
				var attr =
					(SqlQueryParameterAttribute)
						value.GetType().GetCustomAttributes(typeof(SqlQueryParameterAttribute), false).FirstOrDefault();
				if (attr != null && attr.Serialize != SerializationType.None) {
					// JSON
					if (attr.Serialize == SerializationType.Json) {
						return JsonConvert.DeserializeObject(value.ToString(), t);
					}
					throw new NotImplementedException("Deserialization from " + attr.Serialize + " not implemented");
				}
				return value.ToString(); // too bad if you didn't make ToString() equal what you wanted to write
			} catch (FormatException fEx) {
				throw new FormatException($"Cannot convert '{value}' to type {t.Name}", fEx);
			}
		}

		/// <summary>
		///     Set a property's value
		/// </summary>
		/// <typeparam name="T">The output type</typeparam>
		/// <param name="entity">The current object</param>
		/// <param name="propertyPath">The full path to the property (e.g. College.Id, College.Admission.UReqLab)</param>
		/// <param name="value">The value to set the property to</param>
		/// <param name="rootObjectName">The name of the root object.</param>
		/// <param name="setNullValue">When true property will be explicitly set to null</param>
		/// <returns></returns>
		public static T SetPropertyValue<T>(this T entity, string propertyPath, string value, string rootObjectName = null,
			bool setNullValue = false)
			where T : class {
			if (!setNullValue && value == null)
				return entity;

			// Break up the dot delimited path
			var pathArray = propertyPath.Split('.').ToList();

			// Remove the root if necessary
			if (rootObjectName != null && pathArray.First() == rootObjectName)
				pathArray.RemoveAt(0);

			// Variable used for object traversal
			object currentEntity = entity;

			// Iterate through the object paths
			foreach (var path in pathArray) {
				if (currentEntity == null)
					break;

				var type = currentEntity.GetType();
				var property = type.GetProperty(path);

				if (property == null)
					break;

				var propertyType = property.PropertyType;

				if (propertyType.IsArray) {
					if (value == null)
						return entity;

					// Split the setup of comma delimited values
					var valueArray = value.Split(',');

					// Get array element type
					var elementType = propertyType.GetElementType();

					// Create an instance of an array of specified type and length
					var array = Array.CreateInstance(elementType, valueArray.Length);
					for (var i = 0; i < valueArray.Length; i++) {
						if (elementType.IsEnum) {
							var enumValue = Enumr.Parse(elementType, valueArray[i]);
							if (enumValue != null)
								array.SetValue(enumValue, i);
						} else
							array.SetValue(Convert.ChangeType(valueArray[i], elementType), i);
					}

					// Set property value
					property.SetValue(currentEntity, array, null);

					return entity;
				}


				if (propertyType.IsGenericType) {
					// Get generic element type
					var elementType = propertyType.GenericTypeArguments[0];

					if (elementType.IsAnsiClass && typeof(ICollection<>).MakeGenericType(elementType).IsAssignableFrom(propertyType)) {
						// Split the setup of comma delimited values
						var valueArray = value.Split(',');

						// Create an instance of a list of the specified type
						var collection = Activator.CreateInstance(propertyType);
						var addMethod = propertyType.GetMethod("Add");

						foreach (var item in valueArray)
							addMethod.Invoke(collection, new[] { Convert.ChangeType(item, elementType) });

						// Set property value
						property.SetValue(currentEntity, collection, null);

						return entity;
					}
				}

				if (propertyType.IsPrimitive ||
					propertyType == typeof(string) ||
					(propertyType.IsGenericType && propertyType.GetGenericTypeDefinition() == typeof(Nullable<>))) {
					// Handle null-able types
					var safeType = Nullable.GetUnderlyingType(propertyType) ?? propertyType;

					if (safeType.IsEnum) {
						// Handle unset values
						if (string.IsNullOrWhiteSpace(value))
							return entity;

						var enumValue = Enumr.Parse(safeType, value);

						// Set property value
						if (enumValue != null)
							property.SetValue(currentEntity, enumValue, null);

						return entity;
					}

					// Set property value
					property.SetValue(
						currentEntity,
						value == null ? null : Convert.ChangeType(value, safeType),
						null);

					return entity;
				}

				// Handle Guid types
				if (propertyType == typeof(Guid)) {
					property.SetValue(
						currentEntity,
						string.IsNullOrWhiteSpace(value) ? Guid.Empty : new Guid(value));

					return entity;
				}

				// Handle SequentialGuid types
				if (propertyType == typeof(SequentialGuid)) {
					property.SetValue(
						currentEntity,
						string.IsNullOrWhiteSpace(value) ? SequentialGuid.Empty : new SequentialGuid(value));

					return entity;
				}

				if (propertyType.IsEnum) {
					// Handle unset values
					if (string.IsNullOrWhiteSpace(value))
						return entity;

					// Set property value
					property.SetValue(
						currentEntity,
						Enum.Parse(propertyType, value),
						null);

					return entity;
				}

				var innerInstance = property.GetValue(currentEntity, null);
				if (innerInstance == null) {
					// Create an instance of the property's type
					innerInstance = Activator.CreateInstance(propertyType);

					// Set property to instantiated type
					property.SetValue(currentEntity, innerInstance, null);
				}

				// Traverse into sub-object
				currentEntity = innerInstance;
			}

			return entity;
		}
	}
}
// */
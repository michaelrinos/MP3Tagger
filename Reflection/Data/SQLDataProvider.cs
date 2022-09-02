using System;
using System.Collections.Generic;
using System.Linq;
using System.Collections;
using System.Data;
using Newtonsoft.Json;
using System.Data.Common;
using System.Data.SqlClient;
using Reflection.TutorCom.Core.Data;
using Microsoft.Extensions.Options;

namespace Reflection {

	/// <summary>
	/// Class for data manipulation
	/// </summary>
	public abstract class SqlDataProviderBase : DataProvider
	{

		public SqlDataProviderBase(IOptions<AppSettings> settings) : base(settings.Value.ConnectionString)
		{

		}
		public SqlDataProviderBase(string connectionString)
			: base(connectionString)
		{
		}

		public override DbCommand GetDbCommand(String command, DbConnection connection)
		{
			return new SqlCommand(command, (SqlConnection)connection);
		}

		public override DbConnection GetDbConnection()
		{
			return new SqlConnection(this.ConnectionString);
		}

		public override DbDataAdapter GetDbDataAdapter(String statement, DbConnection transactionConnection)
		{
			SqlDataAdapter adapter;
			if (transactionConnection != null)
			{
				adapter = new SqlDataAdapter(statement, (SqlConnection)transactionConnection);
			}
			else
			{
				adapter = new SqlDataAdapter(statement, new SqlConnection(this.ConnectionString));
			}

			return adapter;
		}

		public override Dictionary<string, object> ProcessParameter(object parameterObject, ref DbParameterCollection parameters)
		{
			Dictionary<string, object> parameterDictionary = null;

			// parameters
			if (parameterObject != null && parameterObject is Dictionary<string, object>)
			{
				parameterDictionary = parameterObject as Dictionary<string, object>;
			}
			else if (parameterObject != null)
			{
				if (parameterObject.GetType().IsArray)
					throw new ArgumentException("parameterObject must be an object with key/value pairs, or a Dictionary(string, object).");

				parameterDictionary = _createSqlParameterList(parameterObject);
			}

			// parameters
			if (parameterDictionary != null)
				foreach (var p in parameterDictionary)
				{
					//only allowed table types 
					if (p.Value is List<string> || p.Value is List<Guid> || p.Value is List<int>)
					{
						DataTable dt = new DataTable();
						dt.Columns.Add("Item");
						foreach (var pp in (ICollection)p.Value)
							dt.Rows.Add(pp);
						parameters.Add(new SqlParameter(p.Key, dt));

					}
					else
						parameters.Add(new SqlParameter(p.Key, p.Value ?? DBNull.Value));

				}

			return parameterDictionary;
		}

		/// <summary>
		/// Creates a list of SqlParameters from an object
		/// </summary>
		/// <param name="parameters"></param>
		/// <remarks>Complex properties are XmlSerialized</remarks>
		/// <returns></returns>
		private static Dictionary<string, object> _createSqlParameterList(object parameters)
		{
			var result = new Dictionary<string, object>();
			if (parameters != null)
			{
				// add properties
				foreach (var prop in parameters.GetType().GetProperties())
				{
					var attr = (SqlQueryParameterAttribute)prop.GetCustomAttributes(typeof(SqlQueryParameterAttribute), false).FirstOrDefault();

					// short-circuit on ignore prop
					if (attr != null && (attr.Ignore || attr.IgnoreInOnly))
						continue;

					string name = prop.Name;
					object value = prop.GetValue(parameters, null);

					if (attr != null)
					{
						// check attrs
						if (String.IsNullOrEmpty(attr.ParameterName) == false)
						{
							name = attr.ParameterName;
						}
						if (attr.MaxLength > 0 && value is String)
						{
							value = value.ToString().Substring(0, attr.MaxLength);
						}
						/*else if (attr.Serialize == SerializationType.Xml)
						{
							// assume primitive type, handle serailization with this attribute
							//
							// TODO: implement deserialization from Xml in Reflector
							//
							var serializer = new XmlSerializer(prop.PropertyType);
							var settings = new XmlWriterSettings()
							{
								Encoding = Encoding.UTF8,
								Indent = false,
								OmitXmlDeclaration = false,
							};

							using (var textWriter = new StringWriter())
							{
								using (var xmlWriter = XmlWriter.Create(textWriter, settings))
									serializer.Serialize(xmlWriter, value);

								value = textWriter.ToString();
							}
						}*/
						else if (attr.Serialize == SerializationType.Json)
						{
							value = JsonConvert.SerializeObject(value);
						}
					}

					// throw error on collections or arrays UNLESS they're byte[] or List<string> or List<Guid> or List<int>
					if (value is byte[]) { /* allow */ }
					else if (value is List<string> || value is List<Guid> || value is List<int>) { /* allow */ }
					else if (value is ICollection || (value != null && value.GetType().IsArray))
						throw new ArgumentException("Parameter cannot be a collection or array: " + name);

					// skip if nullable and null				
					if (value == null
						&& prop.PropertyType.IsGenericType
						&& prop.PropertyType.GetGenericTypeDefinition() == typeof(Nullable<>))
						continue;

					if (value != null && value.GetType().IsEnum)
						value = Convert.ToInt64(value);

					// add to dict
					result.Add(name, value);
				}
			}

			return result;
		}

	}

	public interface ISqlDataProvider : IDataProvider
	{

	}
	public class SqlDataProvider : SqlDataProviderBase, ISqlDataProvider
	{
		public SqlDataProvider(IOptions<AppSettings> settings) : base(settings)
		{

		}
		public SqlDataProvider(string connectionString) : base(connectionString)
		{

		}
	}
	public interface ISqlDataProviderHWHRO : IDataProvider
	{

	}

	/// <summary>
	/// I dont like this, would prefer to convert this to an abstract class, that have these methods pre-defined
	/// then implement a custom interface for each child of said abstract class.
	/// For now I'm leaving like this so i can get feedback from other devs as to ways to move forward.
	/// </summary>
	public class SqlDataProviderHWHRO : SqlDataProviderBase, ISqlDataProviderHWHRO
	{

		public SqlDataProviderHWHRO(IOptions<AppSettings> settings) : base(settings.Value.ConnectionString)
		{

		}
	}


	public interface ISqlDataProviderCMS : IDataProvider
	{
	}


	public class SqlDataProviderCMS : SqlDataProviderBase, ISqlDataProviderCMS
	{
		public SqlDataProviderCMS(IOptions<AppSettings> settings) : base(settings.Value.ConnectionStringCMS)
		{
		}
	}


	public interface ISqlDataProviderNotifications : IDataProvider
	{
	}

}

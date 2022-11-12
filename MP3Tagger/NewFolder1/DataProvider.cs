using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using Utility;
using System.Configuration;
using System.Net;
using System.Runtime.CompilerServices;
using System.Data.SqlClient;
using System.Data;
using System.Web;
using System.Reflection;
using System.IO;
using System.Xml;
using System.Xml.Serialization;
using System.Web.UI;
using System.Web.Caching;
using Utility.Logging;
using nGEN.Data;

namespace Data
{
	public abstract class DataProvider
	{
		private object _lockObject = new object();

		private DataSource _ds;

		protected EnvironmentMode environment { get { return EnvironmentMode.Current(); } }
		protected DataSource dataSource { get { return _ds; } }

		public int ApplicationId { get; private set; }
		public DbLog Log { get; private set; }
		public string ProcPrefix { get; set; }

		/// <summary>
		/// 
		/// </summary>
		/// <param name="applicationId"></param>
		/// <param name="loggingEmail"></param>
		/// <remarks>DO NOT change the default connections string -- use the overload to pass one in</remarks>
		protected DataProvider(int applicationId, string loggingEmail = null)
			: this(ConfigurationManager.AppSettings["NGENConnectionString"], applicationId, loggingEmail)
		{ }

		protected DataProvider(string customConnectionString, int applicationId, string loggingEmail = null)
		{
			_ds = new DataSource(customConnectionString);
			this.ApplicationId = applicationId;

			// DbLogProvider.Log doesn't make sense
			if (!(this is DbLogProvider))
				this.Log = new DbLog(applicationId, loggingEmail);
		}



		protected DataTable Execute(QueryType queryType, string command, object parameterObject = null)
		{
			lock (_lockObject)
			{
				SetParameters(parameterObject);
				return (DataTable)dataSource.DataManager.RunQuery(ProcPrefix + command, queryType);
			}
		}
		protected ICollection<T> Execute<T>(QueryType queryType, string command, object parameterObject = null)
		{
			lock (_lockObject)
			{
				SetParameters(parameterObject);
				if (queryType == QueryType.QT_TSql)
					command = CleanupStatement(command);
				return dataSource.DataManager.RunQueryForType<T>(ProcPrefix + command, queryType);
			}
		}

		protected DataTable ExecuteProc(string command, object parameterObject = null)
		{
			lock (_lockObject)
			{
				SetParameters(parameterObject);
				return (DataTable)dataSource.DataManager.RunQuery(ProcPrefix + command, QueryType.QT_Sproc);
			}
		}
		protected ICollection<T> ExecuteProc<T>(string command, object parameterObject = null)
		{
			lock (_lockObject)
			{
				SetParameters(parameterObject);
				return dataSource.DataManager.RunQueryForType<T>(ProcPrefix + command, QueryType.QT_Sproc);
			}
		}

		protected DataTable ExecuteTSql(string command, object parameterObject = null)
		{
			lock (_lockObject)
			{
				SetParameters(parameterObject);
				return (DataTable)dataSource.DataManager.RunQuery(CleanupStatement(command), QueryType.QT_TSql);
			}
		}
		protected ICollection<T> ExecuteTSql<T>(string command, object parameterObject = null)
		{
			lock (_lockObject)
			{
				SetParameters(parameterObject);
				return dataSource.DataManager.RunQueryForType<T>(CleanupStatement(command), QueryType.QT_TSql);
			}
		}




		//protected ICollection<T> ExecuteMany<T>(QueryType queryType, string command, IEnumerable parameterObject = null)
		//{
		//    var result = new List<T>();
		//    lock (_lockObject)
		//    {
		//        foreach (var p in parameterObject)
		//        {
		//            SetParameters(parameterObject);
		//            if (queryType == QueryType.QT_TSql)
		//                command = CleanupStatement(command);
		//            result.AddRange(dataSource.DataManager.RunQueryForType<T>(ProcPrefix + command, queryType));
		//        }
		//    }
		//    return result;
		//}

		//protected ICollection<T> ExecuteManyProc<T>(string command, IEnumerable parameterObject = null)
		//{
		//    var result = new List<T>();
		//    lock (_lockObject)
		//    {
		//        SetParameters(parameterObject);
		//        result.AddRange(dataSource.DataManager.RunQueryForType<T>(ProcPrefix + command, QueryType.QT_Sproc));
		//    }
		//    return result;
		//}

		//protected ICollection<T> ExecuteManyTSql<T>(string command, IEnumerable parameterObject = null)
		//{
		//    var result = new List<T>();
		//    lock (_lockObject)
		//    {
		//        SetParameters(parameterObject);
		//        result.AddRange(dataSource.DataManager.RunQueryForType<T>(CleanupStatement(command), QueryType.QT_TSql));
		//    }
		//    return result;
		//}




		private static string CleanupStatement(string command)
		{
			return command.Replace("\r\n", " ").Replace("\t", "");
		}

		protected void SetParameters(object parameters)
		{
			if (parameters.GetType().IsArray)
				throw new ArgumentException("cannot call SetParameters on arrays");

			if (parameters is ICollection)
				throw new ArgumentException("cannot call SetParameters on ICollection");

			this.dataSource.DataManager.Parameters.Clear();
			foreach (var p in DataProvider.CreateParameterList(parameters))
				this.dataSource.DataManager.Parameters.Add(p.Key, p.Value);
		}

		public static Dictionary<string, object> CreateParameterList(object parameters)
		{
			var result = new Dictionary<string, object>();

			if (parameters != null)
			{
				// add key properties
				foreach (var prop in parameters.GetType().GetProperties())
				{
					bool isNotBinaryData = prop.PropertyType != typeof(byte[]);
					bool isNotSimpleValue = !prop.PropertyType.IsValueType && prop.PropertyType != typeof(string);

					object value = null;

					// Skipping serialization of binary data to Base64encoded in xml. Was making
					// Sql Server throw error of invalid type.
					// Refer to \Git\Development\Common\nGEN.Data\DotNetSQLDataManager.cs
					//   and look for comment, "2021-03-20-DR"
					if (isNotSimpleValue && isNotBinaryData)
					{
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
							{
								serializer.Serialize(xmlWriter, prop.GetValue(parameters, null));
							}
							value = textWriter.ToString();
						}
					}
					else
					{
						value = prop.GetValue(parameters, null);
					}

					var attr = prop.GetCustomAttributes(typeof(ParameterAttribute), true);

					if (attr.Length == 0)
					{ // no attribute -- default behavior 
						result.Add(prop.Name, value);
					}
					else
					{
						var paramAttr = ((ParameterAttribute)attr[0]);

						if (paramAttr.Ignore)
						{
							/* do nothing */
						}
						else if (String.IsNullOrEmpty(paramAttr.ParameterName))
						{ // use property name as parameter name
							result.Add(prop.Name, value);
						}
						else
						{ // use ParameterName from attribute
							result.Add(paramAttr.ParameterName, value);
						}
					}
				}
			}
			return result;
		}

		#region caching

		private readonly LosFormatter serializer = new LosFormatter(false, "");

		private static object _cachelock = new object();

		protected object GetCachedResult(MethodBase method, params object[] methodParameters)
		{
			object result = null;
			var cacheKey = GenerateCacheKey(method, methodParameters);
			if (HttpRuntime.Cache[cacheKey] != null)
			{
				lock (_cachelock)
				{
					result = HttpRuntime.Cache[cacheKey];
				}

			}
			return result;
		}

		protected void CacheResult(object methodResult, MethodBase method, params object[] methodParameters)
		{
			// check for cache attribute
			var attr = method.GetCustomAttributes(typeof(CacheAttribute), false);
			if (attr.Length > 0)
			{
				// add to the cache
				lock (_cachelock)
				{
					HttpRuntime.Cache.Add(GenerateCacheKey(method, methodParameters),
						methodResult,
						null,
						(DateTime.UtcNow + ((CacheAttribute)attr[0]).Lifetime),
						TimeSpan.Zero,
						CacheItemPriority.High,
						null);
				}
			}
		}

		protected void ClearCachedResult(MethodBase method, params object[] methodParameters)
		{
			lock (_cachelock)
			{
				HttpRuntime.Cache.Remove(GenerateCacheKey(method, methodParameters));
			}
		}

		/// <summary>
		/// Create a cache key for the given method and set of input arguments.
		/// </summary>
		/// <param name="method">Method being called.</param>
		/// <param name="inputs">Input arguments.</param>
		/// <returns>A (hopefully) unique string to be used as a cache key.</returns>
		/// <ref>http://stackoverflow.com/questions/3180685/how-can-i-make-method-signature-caching</ref>
		private string GenerateCacheKey(MethodBase method, params object[] inputs)
		{
			try
			{
				var sb = new StringBuilder();

				if (method.DeclaringType != null)
					sb.Append(method.DeclaringType.FullName);

				sb.Append(':');
				sb.Append(method.Name);

				TextWriter writer = new StringWriter(sb);

				if (inputs != null)
				{
					foreach (var input in inputs)
					{
						sb.Append(':');
						if (input != null)
						{
							//Different instances of DateTime which represents the same value
							//sometimes serialize differently due to some internal variables which are different.
							//We therefore serialize it using Ticks instead.
							var inputDateTime = input as DateTime?;
							if (inputDateTime.HasValue)
							{
								sb.Append(inputDateTime.Value.Ticks);
							}
							else
							{
								//Serialize the input and write it to the key StringBuilder.
								serializer.Serialize(writer, input);
							}
						}
					}
				}

				return sb.ToString();
			}
			catch
			{
				//Something went wrong when generating the key (probably an input-value was not serializble.
				//Return a null key.
				return null;
			}
		}

		#endregion

		#region upsert

		/// <summary>
		/// Builds an upsert (if exists update, else insert) statement based on the collection
		/// </summary>
		/// <param name="tableName">The name of the table to execute the upsert on</param>
		/// <param name="primaryKeys">List of column names that represent the primary key</param>
		/// <param name="data">The collection of objects to upsert</param>
		/// <param name="suppressErrors">Whether or not to suppress any/all errors</param>
		/// <returns>The number of rows affected (not necessarily distint rows if the primary key is specified incorrectly)</returns>
		protected int UpsertCollection(string tableName, string[] primaryKeys, ICollection<object> data, bool suppressErrors = false)
		{
			int rowCount = 0;
			var sql = BuildUpsertStatement(tableName, primaryKeys, data);

			foreach (var d in data)
			{
				try
				{
					// TODO: change to .Single() when changing to .net 3.5
					foreach (var c in this.ExecuteTSql<int>(sql, d)) // should really be only one
						rowCount += c;
				}
				catch (Exception)
				{
					if (!suppressErrors)
						throw;
				}
			}
			return rowCount;
		}
		private string BuildUpsertStatement(string tableName, string[] primaryKeys, ICollection<object> data)
		{
			// .net 2.0 stufs
			var sb = new StringBuilder();
			var sep = "";
			var sepValue = " and ";

			if (data.Count == 0)
				return @"select 0;";

			string baseStatement =
				@"if exists (select 1 from {0} where {1}) begin
					update {0}
					set {2}
					where {1};

					select @@rowcount [rowcount];
				end
				else begin
					insert into {0} ({3})
					values ({4});

					select @@rowcount [rowcount];
				end;";

			// build pk phrase
			var pkPhrase = "";
			var primayKeysLowered = ""; // .net 2.0
			if (primaryKeys.Length > 0)
			{
				/* .net 3.5+ version
					pkPhrase = primaryKeys
						.Select(k => String.Format("[{0}] = @{0}", k))
						.Aggregate((i, j) => String.Format("{0} and {1}", i, j));
				*/
				// .net 2.0 version
				sb = new StringBuilder(); // no .Clear() in 2.0
				sep = "";
				sepValue = " and ";
				foreach (var pk in primaryKeys)
				{
					primayKeysLowered += String.Format("{0},", pk.ToLower());
					sb.AppendFormat("{1}[{0}] = @{0}", pk, sep);
					sep = sepValue;
				}
				pkPhrase = sb.ToString();
			}
			else
				pkPhrase = "1 = 0"; // no pk so no update ever; only inserts

			/* .net 3.5+ version
			var sample = data.First();
			*/
			// .net 2.0 version
			object[] collectionArray = new object[data.Count];
			data.CopyTo(collectionArray, 0);
			var sample = collectionArray[0];

			/* .net 3.5+ version
			var sampleProperties = sample.GetType().GetProperties()
				.Select(p => new
				{
					prop = p,
					//attr = (ColumnAttribute)p.GetCustomAttributes(typeof(ColumnAttribute), false).SingleOrDefault()
				})
				//.Where(x => !x.attr.Ignore) // only use un-ignored properties
				.Select(y => y.prop);
			*/
			// .net 2.0 version (much simpler since ignoring the "Column" attribute)
			var sampleProperties = sample.GetType().GetProperties();

			// build update phrase
			/* .net 3.5+ version
			var updatePhrase = sampleProperties
				.Where(p => !primaryKeys.Select(k => k.ToLower()).Contains(p.Name.ToLower()))
				.Select(p => String.Format("[{0}] = @{0}", p.Name))
				.Aggregate((i, j) => String.Format("{0}, {1}", i, j));
			*/
			// .net 2.0 version
			sb = new StringBuilder(); // no .Clear() in 2.0
			sep = "";
			sepValue = ", ";
			foreach (var prop in sampleProperties)
			{
				if (primayKeysLowered.IndexOf(prop.Name.ToLower()) == -1)
				{
					sb.AppendFormat("{1}[{0}] = @{0}", prop.Name, sep);
					sep = sepValue;
				}
			}
			var updatePhrase = sb.ToString();

			// build insert column phrase
			/* .net 3.5+ version
			var insertColumnPhrase = sampleProperties
				.Select(p => String.Format("[{0}]", p.Name))
				.Aggregate((i, j) => String.Format("{0}, {1}", i, j));
			*/
			// .net 2.0 version
			sb = new StringBuilder(); // no .Clear() in 2.0
			sep = "";
			sepValue = ", ";
			foreach (var prop in sampleProperties)
			{
				sb.AppendFormat("{1}[{0}]", prop.Name, sep);
				sep = sepValue;
			}
			var insertColumnPhrase = sb.ToString();

			// build insert value phrase
			/* .net 3.5+ version
			var insertValuePhrase = sampleProperties
				.Select(p => String.Format("@{0}", p.Name))
				.Aggregate((i, j) => String.Format("{0}, {1}", i, j));
			*/
			// .net 2.0 version
			sb = new StringBuilder(); // no .Clear() in 2.0
			sep = "";
			sepValue = ", ";
			foreach (var prop in sampleProperties)
			{
				sb.AppendFormat("{1}@{0}", prop.Name, sep);
				sep = sepValue;
			}
			var insertValuePhrase = sb.ToString();

			return new StringBuilder().AppendFormat(baseStatement,
				tableName,
				pkPhrase,
				updatePhrase,
				insertColumnPhrase,
				insertValuePhrase).ToString();
		}

		#endregion
	}
}

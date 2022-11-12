using nGEN.Data;
using System;
using System.Collections.Generic;
using System.Data;

namespace Data
{
	public delegate void DataReaderCompleteDelegate(System.Data.IDataReader reader);
	public delegate void DataReaderExceptionDelegate(Exception Exception);

	public interface IDataManager
	{
		//properties
		string ConnectionString { get; set; }
		System.Collections.Generic.Dictionary<string, Object> Parameters { get; }
		int CommandTimeout { get; set; }

		//connection functions
		bool OpenConnection();
		void CloseConnection();
		System.Data.ConnectionState ConnectionState { get; }

		//transaction functions
		void BeginTransaction();
		void CommitTransaction();
		void RollbackTransaction();

		//methods
		System.Object RunQuery(string sCommandText, QueryType eQueryType);
		void RunNoResultsQuery(string sCommandText, QueryType eQueryType);
		ICollection<T> RunQueryForType<T>(string sCommandText, QueryType eQueryType);

		[Obsolete("DO NOT USE THE PARAMS ARRAY PARAMETER")]
		System.Object RunQuery(string sCommandText, QueryType eQueryType, params object[] pParams);

		[Obsolete("DO NOT USE THE PARAMS ARRAY PARAMETER")]
		void RunNoResultsQuery(string sCommandText, QueryType eQueryType, params object[] pParams);

		void RunQueryAsync(string sCommandText, Data.QueryType eQueryType, DataReaderCompleteDelegate dDataReaderComplete, DataReaderExceptionDelegate dDataReaderException);
	}
}

using Data;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;

namespace Data
{
	public class DotNetSQLDataManager : IDataManager, IDisposable
	{
		//private members
		private string m_sConnectionString = "";
		private int m_iCommandTimeout = 90;
		private SqlConnection m_Connection = null;
		private System.Collections.Generic.Dictionary<string, Object> m_Parameters = new System.Collections.Generic.Dictionary<string, object>();
		private System.Data.SqlClient.SqlTransaction m_Transaction = null;

		//construction
		public DotNetSQLDataManager(string sConnectionString)
		{
			m_sConnectionString = sConnectionString;
			m_Connection = new System.Data.SqlClient.SqlConnection(m_sConnectionString);
		}

		#region IDisposable Members

		public void Dispose()
		{
			m_Parameters.Clear();
			m_Parameters = null;
			m_sConnectionString = "";

			//			try
			{
				m_Connection.Close();
			}
			// TODO: catch a more spefic exception below (only one we can handle gracefully)
			// commenting out this try/catch block for the meantime... -GAH
			//			catch
			//			{ /* do nothing */ }

			m_Connection = null;
		}

		#endregion

		#region IDataManager Members

		public string ConnectionString
		{
			get { return m_sConnectionString; }
			set { m_sConnectionString = value; }
		}

		public System.Collections.Generic.Dictionary<string, Object> Parameters
		{
			get { return m_Parameters; }
		}

		public int CommandTimeout
		{
			get { return m_iCommandTimeout; }
			set { m_iCommandTimeout = value; }
		}

		public bool OpenConnection()
		{
			//don't call Open() on a connection that is already open
			if (this.ConnectionState != ConnectionState.Closed)
				return false;
			else
			{
				m_Connection.Open();
				return true;
			}
		}

		public void CloseConnection()
		{
			m_Connection.Close();
		}

		public void BeginTransaction()
		{
			m_Transaction = m_Connection.BeginTransaction();
		}

		public void CommitTransaction()
		{
			m_Transaction.Commit();
			m_Transaction = null;
		}

		public void RollbackTransaction()
		{
			m_Transaction.Rollback();
			m_Transaction = null;
		}

		[Obsolete("DO NOT USE THE PARAMS ARRAY PARAMETER")]
		public void RunNoResultsQuery(string sCommandText, Data.QueryType eQueryType, params object[] pParams)
		{
			RunNoResultsQuery(sCommandText, eQueryType);
		}

		public void RunNoResultsQuery(string sCommandText, Data.QueryType eQueryType)
		{
			runInternalQuery(sCommandText, eQueryType, "", false);
		}

		[Obsolete("DO NOT USE THE PARAMS ARRAY PARAMETER")]
		public System.Object RunQuery(string sCommandText, QueryType eQueryType, params object[] pParams)
		{
			return RunQuery(sCommandText, eQueryType);
		}

		public System.Object RunQuery(string sCommandText, Data.QueryType eQueryType)
		{
			System.Object pResultsObj = (System.Object)runInternalQuery(sCommandText, eQueryType, "", false);
			return pResultsObj;
		}

		public ICollection<T> RunQueryForType<T>(string sCommandText, QueryType eQueryType)
		{
			return Reflector.Reflect<T>((DataTable)this.RunQuery(sCommandText, eQueryType));
		}

		public System.Data.ConnectionState ConnectionState { get { return m_Connection.State; } }

		#endregion

		//methods
		public void BeginTransaction(System.Data.IsolationLevel iso)
		{
			m_Transaction = m_Connection.BeginTransaction(iso);
		}

		public DataTable RunDataTableQuery(string sCommandText, Data.QueryType eQueryType)
		{
			DataTable pDataTable = runInternalQuery(sCommandText, eQueryType, "", false);
			return pDataTable;
		}

		public DataSet RunDataSetQuery(string sCommandText, Data.QueryType eQueryType)
		{
			DataTable pDataTable = runInternalQuery(sCommandText, eQueryType, "", false);
			DataSet pDS = new DataSet();
			pDS.Tables.Add(pDataTable);
			return pDS;
		}

		private DataTable runInternalQuery(string sCommandText, QueryType eQueryType, string sTableName, bool isRetry)
		{
			DataTable theDataTable = new DataTable(sTableName);
			SqlDataAdapter theAdapter;

			if (eQueryType == QueryType.QT_Sproc && sCommandText.IndexOf(".") == -1)
				theAdapter = new SqlDataAdapter("dbo." + sCommandText, m_Connection);
			else
				theAdapter = new SqlDataAdapter(sCommandText, m_Connection);

			theAdapter.SelectCommand.CommandTimeout = m_iCommandTimeout;

			//checking if this query is a part of transaction
			if (m_Transaction != null)
				theAdapter.SelectCommand.Transaction = m_Transaction;

			// query type
			if (eQueryType == Data.QueryType.QT_TSql)
				theAdapter.SelectCommand.CommandType = System.Data.CommandType.Text;
			else if (eQueryType == Data.QueryType.QT_Sproc)
				theAdapter.SelectCommand.CommandType = System.Data.CommandType.StoredProcedure;
			else
				throw new NotImplementedException("Unsupported query type was specified: '" + eQueryType.ToString() + "'");

			// parameters
			foreach (System.Collections.Generic.KeyValuePair<string, Object> theEntry in m_Parameters)
			{
				// 2021-03-20-DR:
				// BUG: the type is lost in the following code and sometimes cannot be resolved based on the value.
				// For example, if it's a byte[], .NET will implicitly convert to Base64 string and pass it to
				// SQL Server (SQL), and if SQL is expecting VARBINARY(MAX) and gets a Base64 string, it'll error with:
				//    Implicit conversion from data type nvarchar(max) to varbinary(max) is not allowed.
				//    Use the CONVERT function to run this query.
				//
				// WORKAROUND: don't pass null if the type should be byte[]. Set the value to byte[0] as a default,
				//	           and in the proc, set to null inside the code. 
				if (theEntry.Value == null)
				{
					theAdapter.SelectCommand.Parameters.AddWithValue(theEntry.Key, DBNull.Value);
				}
				else if (theEntry.Value is List<string> || theEntry.Value is List<Guid> || theEntry.Value is List<int>)
				{
					DataTable dt = new DataTable();
					dt.Columns.Add("Item");
					foreach (var pp in (ICollection)theEntry.Value)
						dt.Rows.Add(pp);
					theAdapter.SelectCommand.Parameters.AddWithValue(theEntry.Key, dt);
				}
				else
				{
					theAdapter.SelectCommand.Parameters.AddWithValue(theEntry.Key, theEntry.Value);
				}
			}

			try
			{
				theAdapter.Fill(theDataTable);
			}
			catch (Exception ex)
			{
				if (isRetry == true || ex.Message.IndexOf("General network error") == -1)
				{
					//add param list to error output
					string paramList = "";
					try
					{
						foreach (System.Collections.Generic.KeyValuePair<string, Object> theEntry in m_Parameters)
							paramList +=
							  String.Format("{0}=\"{1}\", ",
								theEntry.Key,
								((theEntry.Value == null) ? "" : theEntry.Value.ToString())
							  );

						if (paramList.EndsWith(", "))
							paramList = paramList.Substring(0, paramList.Length - 2);
					}
					catch { }

					//add command text to error output
					throw new ApplicationException(
					  String.Format("Unable to run Query [{0}] [{1}]: {2}",
						sCommandText,
						paramList,
						ex.Message
					  ),
					  ex);
				}
				else
				{
					//sleep 5 seconds and retry if we get "General network error"
					System.Threading.Thread.Sleep(5000);
					theDataTable = runInternalQuery(sCommandText, eQueryType, sTableName, true);
				}
			}

			theAdapter = null;

			return theDataTable;
		}

		/// <summary>
		/// Queries the datasource asynchronously on a separate database connection
		/// </summary>
		public void RunQueryAsync(string sCommandText, Data.QueryType eQueryType, DataReaderCompleteDelegate dDataReaderComplete, DataReaderExceptionDelegate dDataReaderException)
		{
			beginRunInternalQueryAsync(sCommandText, eQueryType, false, dDataReaderComplete, dDataReaderException);
		}

		private void beginRunInternalQueryAsync(string sCommandText, QueryType eQueryType, bool isRetry, DataReaderCompleteDelegate dDataReaderComplete, DataReaderExceptionDelegate dDataReaderException)
		{
			SqlCommand theCommand;

			if (eQueryType == QueryType.QT_Sproc && sCommandText.IndexOf(".") == -1)
				theCommand = new SqlCommand("dbo." + sCommandText, m_Connection);
			else
				theCommand = new SqlCommand(sCommandText, m_Connection);

			theCommand.CommandTimeout = m_iCommandTimeout;

			//checking if this query is a part of transaction
			if (m_Transaction != null)
				theCommand.Transaction = m_Transaction;

			if (eQueryType == Data.QueryType.QT_TSql)
				theCommand.CommandType = System.Data.CommandType.Text;
			else if (eQueryType == Data.QueryType.QT_Sproc)
				theCommand.CommandType = System.Data.CommandType.StoredProcedure;
			else
				throw new ApplicationException("Error, an unsupported query type was specified.");

			foreach (System.Collections.Generic.KeyValuePair<string, Object> theEntry in m_Parameters)
			{
				if (theEntry.Value == null)
					theCommand.Parameters.AddWithValue(theEntry.Key, DBNull.Value);
				else
					theCommand.Parameters.AddWithValue(theEntry.Key, theEntry.Value);
			}

			try
			{
				SqlConnection theConnection = new System.Data.SqlClient.SqlConnection(theCommand.Connection.ConnectionString + "; Async=true;");
				theConnection.Open();
				theCommand.Connection = theConnection;
				theCommand.BeginExecuteReader(delegate (IAsyncResult result) {
					using (SqlCommand theCommand1 = (SqlCommand)result.AsyncState)
					{
						try
						{
							SqlDataReader theReader = theCommand1.EndExecuteReader(result);

							if (dDataReaderComplete != null)
								dDataReaderComplete(theReader);
						}
						catch (Exception ex)
						{
							if (dDataReaderException != null)
								dDataReaderException(ex);
						}
						finally
						{
							theCommand1.Connection.Close();
						}
					}
				}, theCommand);
			}
			catch (Exception ex)
			{
				if (isRetry == true || ex.Message.IndexOf("General network error") == -1)
					throw new ApplicationException("Unable to run Query: " + ex.Message, ex);
				else
				{
					//sleep 5 seconds and retry if we get "General network error"
					System.Threading.Thread.Sleep(5000);
					beginRunInternalQueryAsync(sCommandText, eQueryType, true, dDataReaderComplete, dDataReaderException);

				}
			}

			theCommand = null;
		}
	}
}

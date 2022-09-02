using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data.Common;
using System.Data;
using System.Collections;
using Newtonsoft.Json;
using System.Threading.Tasks;

namespace Reflection {
	using System;
	using System.Collections.Generic;
	using System.Data;
	using System.Data.Common;
	using System.Threading;
	using System.Threading.Tasks;

	namespace TutorCom.Core.Data
	{
		public interface IDataProvider
		{
			string ConnectionString { get; }
			bool IsTransactionOpen { get; }
			int Timeout { get; set; }

			DbTransaction BeginTransaction(IsolationLevel isolation = IsolationLevel.ReadCommitted);
			void CommitTransaction();
			int ExecuteBatchUpdate(DataTable dataTable, int batchSize, DbCommand insertStatement, DbCommand updateStatement, DbCommand deleteStatement);
			ICollection<T> ExecuteProc<T>(string procName, object parameterObject = null, DataParameterDelegate drParameters = null, bool includeSchema = false);
			ICollection<T> ExecuteProc<T>(string procName, Func<DataTable, ICollection<T>> mapFunction, object parameterObject = null, DataParameterDelegate drParameters = null, bool includeSchema = false);
			//Task ExecuteProcAsync(string procName, object parameterObject = null, DataReaderCompleteDelegate drComplete = null, DataReaderExceptionDelegate drException = null, DataParameterDelegate drParameters = null);
			Task ExecuteProcAsync(string procName, object parameterObject = null, bool includeSchema = false);

			Task<ICollection<T>> ExecuteProcAsync<T>(string procName, object parameterObject = null, DataParameterDelegate drParameters = null, bool includeSchema = false);
			Task<ICollection<T>> ExecuteProcAsync<T>(string procName, Func<DataTable, ICollection<T>> mapFunction, object parameterObject = null, DataParameterDelegate drParameters = null, bool includeSchema = false);
			DataTable ExecuteProc_DataTableResult(string procName, object parameterObject = null, DataParameterDelegate drParameters = null, bool includeSchema = false);
			Task<DataTable> ExecuteProc_DataTableResultAsync(string procName, object parameterObject = null, DataParameterDelegate drParameters = null, bool includeSchema = false);
			DataSet ExecuteTSql(string command, object parameterObject = null, bool includeSchema = false);
			ICollection<T> ExecuteTSql<T>(string command, object parameterObject = null, bool includeSchema = false);
			DbCommand GetDbCommand(string command, DbConnection connection);
			DbConnection GetDbConnection();
			DbDataAdapter GetDbDataAdapter(string statement, DbConnection transactionConnection);
			Dictionary<string, object> ProcessParameter(object parameterObject, ref DbParameterCollection dbParameters);
			void RollbackTransaction();
			void SetTransaction(DbTransaction transaction);
		}

		/// <summary>
		/// Class for data manipulation
		/// </summary>
		public abstract class DataProvider : IDataProvider
		{
			const int _DEFAULT_TIMEOUT = 90;

			public string ConnectionString { get; private set; }
			public int Timeout { get; set; }

			// private connection is used only when executing transactions against the database. All other executions have their own connection objects
			DbConnection _transactionConnection;
			DbTransaction _transaction;
			int _beginTransactionCallsCount = 0;

			public abstract DbConnection GetDbConnection();

			public abstract DbDataAdapter GetDbDataAdapter(String statement, DbConnection transactionConnection);

			public abstract DbCommand GetDbCommand(String command, DbConnection connection);

			public abstract Dictionary<string, object> ProcessParameter(object parameterObject, ref DbParameterCollection dbParameters);

			protected DataProvider(string connectionString)
			{
				this.ConnectionString = connectionString;
				this.Timeout = _DEFAULT_TIMEOUT;
			}

			//TODO: rg: no way to share a transaction across data providers... do we need a method like this?
			/// <summary>
			/// Use with caution.
			/// </summary>
			public void SetTransaction(DbTransaction transaction)
			{
				_transactionConnection = transaction.Connection;
				_transaction = transaction;
			}

			public bool IsTransactionOpen { get { return _transaction != null; } }

			/// <summary>
			/// Opens the db connection and begins a new SQL transaction. All commands executed between BeginTransaction and Commit or Rollback will be in the transaction
			/// </summary>
			/// <param name="isolation">The isolation level of the transaction. Defaults to standard default "ReadCommitted".</param>
			public DbTransaction BeginTransaction(IsolationLevel isolation = IsolationLevel.ReadCommitted)
			{
				if (_beginTransactionCallsCount == 0 && !IsTransactionOpen)
				{

					_transactionConnection = GetDbConnection();
					_transactionConnection.Open();

					_transaction = _transactionConnection.BeginTransaction(isolation);

				}

				Interlocked.Increment(ref _beginTransactionCallsCount);


				return _transaction;
			}

			/// <summary>
			/// Commits any outstanding transaction
			/// </summary>
			public void CommitTransaction()
			{
				Interlocked.Decrement(ref _beginTransactionCallsCount);


				if (_transaction == null)
					throw new InvalidOperationException("No open transaction to commit");

				if (_beginTransactionCallsCount == 0)
				{
					_transaction.Commit();
					_transaction = null;

					try { _transactionConnection.Close(); }
					catch { /* suppress */ }
					finally { _transactionConnection = null; }
				}
			}

			/// <summary>
			/// Rolls back any outstanding transaction
			/// </summary>
			public void RollbackTransaction()
			{


				if (_transaction == null)
					throw new InvalidOperationException("No open transaction to rollback");

				_transaction.Rollback();
				_transaction = null;

				try { _transactionConnection.Close(); }
				catch { /* suppress */ }
				finally
				{
					_transactionConnection = null;
					Interlocked.Exchange(ref _beginTransactionCallsCount, 0);
				}
			}

			public DataTable ExecuteProc_DataTableResult(string procName, object parameterObject = null, DataParameterDelegate drParameters = null, bool includeSchema = false)
			{
				return ExecuteProc(procName, parameterObject, drParameters, includeSchema);
			}
			public Task<DataTable> ExecuteProc_DataTableResultAsync(string procName, object parameterObject = null, DataParameterDelegate drParameters = null, bool includeSchema = false)
			{
				return ExecuteProcAsync(procName, parameterObject, drParameters, includeSchema);
			}

			/// <summary>
			/// Executes procName using parameterObject
			/// </summary>
			/// <param name="procName">The sproc to execute</param>
			/// <param name="parameterObject">An object with key/value pairs, or a Dictionary(string, object)</param>
			/// <returns>First DataTable from sproc execution</returns>
			protected DataTable ExecuteProc(string procName, object parameterObject = null, DataParameterDelegate drParameters = null, bool includeSchema = false)
			{
				var dataSet = _executeStatement(CommandType.StoredProcedure, procName, includeSchema, parameterObject, drParameters: drParameters);
				if (dataSet.Tables.Count > 0)
					return dataSet.Tables[0];
				else
					return null;
			}

			protected async Task<DataTable> ExecuteProcAsync(string procName, object parameterObject = null, DataParameterDelegate drParameters = null, bool includeSchema = false)
			{
				var dataSet = await _executeStatementAsync(CommandType.StoredProcedure, procName, includeSchema, parameterObject, drParameters: drParameters);
				if (dataSet.Tables.Count > 0)
					return dataSet.Tables[0];
				else
					return null;
			}


			/// <summary>
			/// Executes procName using parameterObject
			/// </summary>
			/// <param name="procName">The sproc to execute</param>
			/// <param name="mapFunction">Function responsible for mapping the DataRow to a Collection item T</param>
			/// <param name="parameterObject">An object with key/value pairs, or a Dictionary(string, object)</param>
			/// <returns>First DataTable from sproc execution</returns>
			public ICollection<T> ExecuteProc<T>(
				string procName,
				Func<DataTable, ICollection<T>> mapFunction,
				object parameterObject = null,
				DataParameterDelegate drParameters = null,
				bool includeSchema = false)
			{
				var dataTable = ExecuteProc(procName, parameterObject, drParameters, includeSchema);

				if (dataTable == null)
					return null;

				/* - was there a single table and row output?
				 * - is the parameter object and output type the same?
				 * then looks like you're updating a single record, update input object instead of rebuilding
				 * -----
				 * this is used on INSERT to preserve data model properties that represent
				 * child data models (i.e. deep object) that are saved in multiple steps
				 */
				if (dataTable.Rows.Count == 1
					&& parameterObject is T)
					return new List<T>() { Reflection.Reflect<T>(dataTable, dataTable.Rows[0], (T)parameterObject) };
				else
					return mapFunction(dataTable);
			}


			public async Task<ICollection<T>> ExecuteProcAsync<T>(string procName, Func<DataTable, ICollection<T>> mapFunction, object parameterObject = null, DataParameterDelegate drParameters = null, bool includeSchema = false)
			{
				var dataTable = await ExecuteProcAsync(procName, parameterObject, drParameters, includeSchema);

				if (dataTable == null)
					return null;

				/* - was there a single table and row output?
				 * - is the parameter object and output type the same?
				 * then looks like you're updating a single record, update input object instead of rebuilding
				 * -----
				 * this is used on INSERT to preserve data model properties that represent
				 * child data models (i.e. deep object) that are saved in multiple steps
				 */
				if (dataTable.Rows.Count == 1
					&& parameterObject is T)
					return new List<T>() { Reflection.Reflect<T>(dataTable, dataTable.Rows[0], (T)parameterObject) };
				else
					return mapFunction(dataTable);
			}

			/// <summary>
			/// Reflects an object of type T after executing procName, using parameterObject.
			/// If parameterObject is of type T, and procName returns a single row, parameterObject is updated in-place.
			/// </summary>
			/// <param name="procName">The sproc to execute</param>
			/// <param name="parameterObject">An object with key/value pairs, or a Dictionary(string, object)</param>
			/// <returns>An object of type T, reflected from first DateTable in the result set</returns>
			public ICollection<T> ExecuteProc<T>(string procName, object parameterObject = null, DataParameterDelegate drParameters = null, bool includeSchema = false)
			{
				var dataTable = ExecuteProc(procName, parameterObject, drParameters, includeSchema);

				if (dataTable == null)
					return null;

				/* - was there a single table and row output?
				 * - is the parameter object and output type the same?
				 * then looks like you're updating a single record, update input object instead of rebuilding
				 * -----
				 * this is used on INSERT to preserve data model properties that represent
				 * child data models (i.e. deep object) that are saved in multiple steps
				 */
				if (dataTable.Rows.Count == 1
					&& parameterObject is T)
					return new List<T>() { Reflection.Reflect<T>(dataTable, dataTable.Rows[0], (T)parameterObject) };
				else
					return Reflection.Reflect<T>(dataTable);
			}

			public async Task<ICollection<T>> ExecuteProcAsync<T>(string procName, object parameterObject = null, DataParameterDelegate drParameters = null, bool includeSchema = false)
			{
				var dataTable = await ExecuteProcAsync(procName, parameterObject, drParameters, includeSchema);

				if (dataTable == null)
					return null;

				/* - was there a single table and row output?
				 * - is the parameter object and output type the same?
				 * then looks like you're updating a single record, update input object instead of rebuilding
				 * -----
				 * this is used on INSERT to preserve data model properties that represent
				 * child data models (i.e. deep object) that are saved in multiple steps
				 */
				if (dataTable.Rows.Count == 1
					&& parameterObject is T)
					return new List<T>() { Reflection.Reflect<T>(dataTable, dataTable.Rows[0], (T)parameterObject) };
				else
					return Reflection.Reflect<T>(dataTable);
			}

			/// <summary>
			/// Executes parameterized T-SQL using parameterObject
			/// </summary>
			/// <param name="command">SQL statement to execute</param>
			/// <param name="parameterObject">An object with key/value pairs, or a Dictionary(string, object)</param>
			/// <returns>the DataSet produced by execution</returns>
			public DataSet ExecuteTSql(string command, object parameterObject = null, bool includeSchema = false)
			{
				return _executeStatement(CommandType.Text, command, includeSchema, parameterObject);
			}

			/// <summary>
			/// Executes parameterized T-SQL using parameterObject
			/// </summary>
			/// <param name="command">SQL statement to execute</param>
			/// <param name="parameterObject">An object with key/value pairs, or a Dictionary(string, object)</param>
			/// <returns>An object of type T, reflected from first DateTable in the result set</returns>
			public ICollection<T> ExecuteTSql<T>(string command, object parameterObject = null, bool includeSchema = false)
			{
				return Reflection.Reflect<T>(_executeStatement(CommandType.Text, command, includeSchema, parameterObject).Tables[0]);
			}

			/// <summary>
			/// Executes Batch dbCommand and returns number of row updated by those command 
			/// </summary>
			/// <param name="dataTable"></param>
			/// <param name="batchSize"></param>
			/// <param name="insertStatement"></param>
			/// <param name="updateStatement"></param>
			/// <param name="deleteStatement"></param>
			/// <returns></returns>
			public int ExecuteBatchUpdate(DataTable dataTable, int batchSize, DbCommand insertStatement, DbCommand updateStatement, DbCommand deleteStatement)
			{
				DbDataAdapter adapter = null;

				// transaction
				if (_transaction != null && _transactionConnection != null)
				{
					adapter = GetDbDataAdapter(string.Empty, _transactionConnection);
					adapter.SelectCommand.Transaction = _transaction;
				}
				else
				{
					adapter = GetDbDataAdapter(string.Empty, null);
				}

				if (insertStatement != null)
				{
					adapter.InsertCommand = insertStatement;
					adapter.InsertCommand.UpdatedRowSource = UpdateRowSource.None;
				}
				if (updateStatement != null)
				{
					adapter.UpdateCommand = updateStatement;
					adapter.UpdateCommand.UpdatedRowSource = UpdateRowSource.None;
				}
				if (deleteStatement != null)
				{
					adapter.DeleteCommand = updateStatement;
					adapter.DeleteCommand.UpdatedRowSource = UpdateRowSource.None;
				}

				adapter.UpdateBatchSize = batchSize;
				try
				{
					return adapter.Update(dataTable);
				}
				catch (Exception sqlEx)
				{
					throw new ApplicationException(
						String.Concat("Unable to ExecuteBatchSql: ", sqlEx.Message),
						sqlEx);
				}
			}

			public async Task ExecuteProcAsync(string procName, object parameterObject = null, bool includeSchema = false)
			{
				await ExecuteProcAsync(procName, parameterObject, null, includeSchema);

				/* - was there a single table and row output?
				 * - is the parameter object and output type the same?
				 * then looks like you're updating a single record, update input object instead of rebuilding
				 * -----
				 * this is used on INSERT to preserve data model properties that represent
				 * child data models (i.e. deep object) that are saved in multiple steps
				 */

			}

			public async Task ExecuteProcAsync(string procName, object parameterObject = null, DataReaderCompleteDelegate drComplete = null, DataReaderExceptionDelegate drException = null, DataParameterDelegate drParameters = null)
			{
				Dictionary<string, object> parameterDictionary = null;

				try
				{
					//TODO: gr - investigate this commented out exception
					if (_transaction != null)
						throw new NotImplementedException("Transactions not supported in async execution");

					using (DbConnection connection = GetDbConnection())
					{
						var command = GetDbCommand(procName, connection);

						// timeout
						command.CommandType = CommandType.StoredProcedure;
						command.CommandTimeout = this.Timeout;
						DbParameterCollection parameters = command.Parameters;

						parameterDictionary = ProcessParameter(parameterObject, ref parameters);
						await connection.OpenAsync();
						command.Connection = connection;

						using (DbDataReader reader = await command.ExecuteReaderAsync())
						{
							try
							{
								if (drComplete != null)
								{
									drComplete(reader);
								}

								if (drParameters != null)
								{
									drParameters(command.Parameters);
								}
							}
							catch (DbException ex)
							{
								if (drException != null)
									drException(ex);
							}
						}
					}

				}
				catch (DbException ex)
				{
					// add param list to error output
					string paramList = "";
					try
					{
						if (parameterDictionary != null)
						{
							foreach (System.Collections.Generic.KeyValuePair<string, Object> theEntry in parameterDictionary)
								paramList +=
									String.Format("{0}=\"{1}\", ",
										theEntry.Key,
										((theEntry.Value == null) ? "" : theEntry.Value.ToString())
									);

							if (paramList.EndsWith(", "))
								paramList = paramList.Substring(0, paramList.Length - 2);
						}
					}
					catch { /* suppress */ }

					// add command text to error output
					throw new ApplicationException(
						String.Concat("Unable to execute query [", procName, "] [", paramList, "]: ", ex.Message),
						ex);
				}
			}


			#region private methods

			private DataSet _executeStatement(CommandType commandType, string statement, bool includeSchema, object parameterObject = null, bool isRetry = false, DataParameterDelegate drParameters = null)
			{
				DataSet dataSet = new DataSet();

				// build adapter
				DbDataAdapter adapter = null;

				// transaction?
				if (_transaction != null && _transactionConnection != null)
				{
					adapter = GetDbDataAdapter(statement, _transactionConnection);
					adapter.SelectCommand.Transaction = _transaction;
				}
				else
				{
					adapter = GetDbDataAdapter(statement, null);
				}

				DbParameterCollection parameters = adapter.SelectCommand.Parameters;

				Dictionary<string, object> parameterDictionary = ProcessParameter(parameterObject, ref parameters);

				adapter.SelectCommand.CommandType = commandType;
				adapter.SelectCommand.CommandTimeout = this.Timeout;

				try
				{
					//rg: JsonConvert.Serialize() causing issue with CMS test here, potentially expensive and error-prone, commenting out
					//System.Diagnostics.Debug.WriteLine(String.Concat("db -> ", statement, JsonConvert.SerializeObject(parameterObject, Formatting.None)));
					if (includeSchema)
						adapter.FillSchema(dataSet, SchemaType.Source);
					adapter.Fill(dataSet);

					if (drParameters != null)
					{
						drParameters(adapter.SelectCommand.Parameters);
					}
				}
				catch (DbException sqlEx)
				{
					/* rg: reconsider this retry logic, no memory of why it's here */
					if (!isRetry && sqlEx.Message.IndexOf("General network error") >= 0)
					{
						System.Diagnostics.Debug.WriteLine("db[RETRY]");

						// sleep 2.5 seconds and retry if we get "General network error"
						System.Threading.Thread.Sleep(2500);
						dataSet = _executeStatement(commandType, statement, includeSchema, parameterObject, true);
					}
					else
					{
						// add param list to error output					
						string paramList = "";
						try
						{
							if (parameterDictionary != null)
							{
								foreach (System.Collections.Generic.KeyValuePair<string, Object> theEntry in parameterDictionary)
									paramList +=
										String.Format("{0}=\"{1}\", ",
											theEntry.Key,
											((theEntry.Value == null) ? "" : theEntry.Value.ToString())
										);

								if (paramList.EndsWith(", "))
									paramList = paramList.Substring(0, paramList.Length - 2);
							}
						}
						catch { /* suppress */ }

						// add command text to error output
						throw new ApplicationException(
							String.Concat("Unable to execute query [", statement, "] [", paramList, "]: ", sqlEx.Message),
							sqlEx);
					}
				}

				return dataSet;
			}


			private async Task<DataSet> _executeStatementAsync(CommandType commandType, string statement, bool includeSchema, object parameterObject = null, bool isRetry = false, DataParameterDelegate drParameters = null)
			{
				DataSet dataSet = new DataSet();

				// build adapter
				DbDataAdapter adapter = null;

				// transaction?
				if (_transaction != null && _transactionConnection != null)
				{
					adapter = GetDbDataAdapter(statement, _transactionConnection);
					adapter.SelectCommand.Transaction = _transaction;
				}
				else
				{
					adapter = GetDbDataAdapter(statement, null);
				}

				DbParameterCollection parameters = adapter.SelectCommand.Parameters;

				Dictionary<string, object> parameterDictionary = ProcessParameter(parameterObject, ref parameters);

				adapter.SelectCommand.CommandType = commandType;
				adapter.SelectCommand.CommandTimeout = this.Timeout;

				try
				{
					//rg: JsonConvert.Serialize() causing issue with CMS test here, potentially expensive and error-prone, commenting out
					//System.Diagnostics.Debug.WriteLine(String.Concat("db -> ", statement, JsonConvert.SerializeObject(parameterObject, Formatting.None)));
					if (includeSchema)
						adapter.FillSchema(dataSet, SchemaType.Source);

					await Task.Factory.StartNew(() => adapter.Fill(dataSet));
					drParameters?.Invoke(adapter.SelectCommand.Parameters);
				}
				catch (DbException sqlEx)
				{
					/* rg: reconsider this retry logic, no memory of why it's here */
					if (!isRetry && sqlEx.Message.IndexOf("General network error") >= 0)
					{
						System.Diagnostics.Debug.WriteLine("db[RETRY]");

						// sleep 2.5 seconds and retry if we get "General network error"
						System.Threading.Thread.Sleep(2500);
						dataSet = await _executeStatementAsync(commandType, statement, includeSchema, parameterObject, true);
					}
					else
					{
						// add param list to error output					
						string paramList = "";
						try
						{
							if (parameterDictionary != null)
							{
								foreach (System.Collections.Generic.KeyValuePair<string, Object> theEntry in parameterDictionary)
									paramList +=
										String.Format("{0}=\"{1}\", ",
											theEntry.Key,
											((theEntry.Value == null) ? "" : theEntry.Value.ToString())
										);

								if (paramList.EndsWith(", "))
									paramList = paramList.Substring(0, paramList.Length - 2);
							}
						}
						catch { /* suppress */ }

						// add command text to error output
						throw new ApplicationException(
							String.Concat("Unable to execute query [", statement, "] [", paramList, "]: ", sqlEx.Message),
							sqlEx);
					}
				}

				return dataSet;
			}



			#endregion
		}




		/// <summary>
		/// Callback for ExecuteStatementAsync
		/// </summary>
		/// <param name="reader">The resultant reader</param>
		public delegate void DataReaderCompleteDelegate(System.Data.Common.DbDataReader reader);

		/// <summary>
		/// Exception handler for ExecuteStatementAsync
		/// </summary>
		/// <param name="sqlEx">The SQL exception thrown</param>
		public delegate void DataReaderExceptionDelegate(DbException dbEx);

		/// <summary>
		/// Data Parameter output
		/// </summary>
		/// <param name="parameters">The SQL exception thrown</param>
		public delegate void DataParameterDelegate(DbParameterCollection parameters);

		/// <summary>
		/// Thrown when a database item is not found
		/// </summary>
		public class NotFoundException : Exception
		{
			public NotFoundException()
				: base()
			{ }

			public NotFoundException(string itemName, string itemId)
				: base(String.Concat(itemName, " ", itemId, " not found"))
			{ }
		}
	}

}
// */
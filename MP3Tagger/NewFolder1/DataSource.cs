using System;
using System.Collections.Generic;
using System.Data;

namespace Data
{
	public class DataSource
	{
		//private members
		private string m_sConnectionString = "";
		private IDataManager m_pDataManager = null;

		//construction
		public DataSource()
		{
			//use the appsettings to get the default connection string
			m_sConnectionString = System.Configuration.ConfigurationManager.AppSettings["ConnectionString"];
		}
		public DataSource(string sConnectionString)
		{
			m_sConnectionString = sConnectionString;
		}
		public DataSource(IDataManager pDManager)
		{
			m_pDataManager = pDManager;
		}
		public DataSource(string sConnectionString, IDataManager pDManager)
		{
			m_sConnectionString = sConnectionString;
			m_pDataManager = pDManager;
		}

		//properties
		public string ConnectionString
		{
			get { return m_sConnectionString; }
			set { m_sConnectionString = value; }
		}
		public IDataManager DataManager
		{
			get
			{
				//this method will do the right thing for the caller. which is the_
				//following in this order:
				// 1) if an existing internal instance exists, return a reference to that
				// 2) if a global instance exists, return a reference to that
				// 3) if a valid connection string is passed, return a reference to a_
				//    new instance (we dont hold onto this, but just pass it back)
				// 4) throw an exception because we cant do anything else
				if (m_pDataManager == null)
				{
					if (m_sConnectionString.ToUpper().IndexOf(("Provider=Microsoft.Jet.OLEDB").ToUpper()) != -1)
					{
						throw new NotImplementedException("OleDbDataManager is not implemented");
					}
					else if (m_sConnectionString != "")
					{
						//NOTE: IN THE FUTURE, IF ADDITIONAL DATAMANAGERS BECOME AVAILABLE, THIS_
						//THIS METHOD WILL NEED TO PARSE THE CONNECTION STRING TO DISCOVER THE_
						//APPROPRIATE DATAMANAGER TO PERFORM A NEW ON HERE. BUT FOR NOW, SINCE_
						//WE HAVE ONLY ONE, WE WILL SIMPLY CREATE IT DIRECTLY NOW AND DEFER THE PARSING TO THE FUTURE
						m_pDataManager = new DotNetSQLDataManager(m_sConnectionString);
					}
					else
					{
						//throw an exception because we cant help the caller out since_
						//we dont have an existing instance, a global instance, or_
						//a valid connection string to create a new instance
						throw new Exceptions.Data.DataManagerNotAvailable("Error, unable to retrieve DataManager instance. No appropriate instance is available or could be created.", null);
					}
				}
				return m_pDataManager;
			}
			set { m_pDataManager = value; }
		}
	}
}

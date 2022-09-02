using Reflection.TutorCom.Core.Data;
using System;
using System.Collections.Generic;
using System.Data;
using System.Text;

namespace Reflection.Services {
	public class TransactionalService : ITransactional
	{
		protected IDataProvider DataProvider { get; }

		public TransactionalService(IDataProvider dataProvider)
		{
			this.DataProvider = dataProvider;
		}
		public bool IsTransactionOpen => this.DataProvider.IsTransactionOpen;

		public void BeginTransaction(IsolationLevel isolation = IsolationLevel.ReadCommitted)
		{
			this.DataProvider.BeginTransaction(isolation);
		}

		public void CommitTransaction()
		{
			this.DataProvider.CommitTransaction();
		}

		public void RollbackTransaction()
		{
			this.DataProvider.RollbackTransaction();
		}
	}
}

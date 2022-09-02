using System;
using System.Collections.Generic;
using System.Data;
using System.Text;

namespace Reflection.Services {
    public interface ITransactional {
        bool IsTransactionOpen { get; }
        void BeginTransaction(IsolationLevel isolation = IsolationLevel.ReadCommitted);

        void CommitTransaction();
        void RollbackTransaction();
    }
}

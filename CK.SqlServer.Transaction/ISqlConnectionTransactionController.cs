using System;
using System.Collections.Generic;
using System.Data;
using Microsoft.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CK.SqlServer
{
    /// <summary>
    /// Extends <see cref="ISqlConnectionController"/> to support transaction.
    /// </summary>
    public interface ISqlConnectionTransactionController : ISqlConnectionController
    {
        /// <summary>
        /// Gets the <see cref="ISqlTransactionCallContext"/> to which this connection controller belongs.
        /// </summary>
        new ISqlTransactionCallContext SqlCallContext { get; }

        /// <summary>
        /// Gets the current transaction count: the number of <see cref="BeginTransaction(IsolationLevel)"/>
        /// that have no associated <see cref="Commit"/> yet.
        /// Calling <see cref="RollbackAll"/> cancels all opened transactions.
        /// </summary>
        int TransactionCount { get; }

        /// <summary>
        /// Starts a transaction.
        /// Please read this before playing with isolation level: https://docs.microsoft.com/en-us/sql/t-sql/statements/set-transaction-isolation-level-transact-sql?view=sql-server-2017#remarks
        /// </summary>
        /// <param name="isolationLevel">The transaction isolation level.</param>
        /// <returns>A transaction that can be used to locally control the transaction.</returns>
        ISqlTransaction BeginTransaction( IsolationLevel isolationLevel = IsolationLevel.ReadCommitted );

        /// <summary>
        /// Commits the current transaction (commits the actual Sql transaction if it is is not a nested
        /// transaction, ie. <see cref="TransactionCount"/> = 1).
        /// Throws an <see cref="InvalidOperationException"/> if <see cref="TransactionCount"/> is 0.
        /// </summary>
        void Commit();
  
        /// <summary>
        /// Rollbacks all transactions currently opened.
        /// </summary>
        void RollbackAll();
    }
}

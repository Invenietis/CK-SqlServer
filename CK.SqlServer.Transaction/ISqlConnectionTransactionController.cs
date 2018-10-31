using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
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
        /// Calling <see cref="Rollback"/> cancels all opened transactions.
        /// </summary>
        int TransactionCount { get; }

        /// <summary>
        /// Gets the current <see cref="IsolationLevel"/>.
        /// When no transactions are active, this is <see cref="IsolationLevel.Unspecified"/>.
        /// This can be changed by calls to <see cref="BeginTransaction(IsolationLevel)"/>.
        /// Please read this before playing with isolation level: https://docs.microsoft.com/en-us/sql/t-sql/statements/set-transaction-isolation-level-transact-sql?view=sql-server-2017#remarks
        /// </summary>
        IsolationLevel CurrentIsolationLevel { get; }

        /// <summary>
        /// Starts a transaction.
        /// Please read this before playing with isolation level: https://docs.microsoft.com/en-us/sql/t-sql/statements/set-transaction-isolation-level-transact-sql?view=sql-server-2017#remarks
        /// </summary>
        /// <param name="isolationLevel">The transaction isolation level.</param>
        /// <returns>A transaction that can be used to locally control the transaction.</returns>
        ISqlTransaction BeginTransaction( IsolationLevel isolationLevel = IsolationLevel.ReadCommitted );

        /// <summary>
        /// Commits the current transaction (commits the actual Sql transaction if it is is not a nested transaction, ie. <see cref="TransactionCount"/> = 1).
        /// If no corresponding transaction have been opened (or have been already committed by <see cref="ISqlTransaction.Commit"/>),
        /// this throws an <see cref="InvalidOperationException"/>, but this can safely be called if a <see cref="Rollback"/> has been called (and in this case, false is returned).
        /// This  if <see cref="TransactionCount"/> is 1.
        /// </summary>
        /// <returns>False if <see cref="Rollback"/> has been called, true if the actual transaction is still alive.</returns>
        bool Commit();

        /// <summary>
        /// Rollbacks all transactions currently opened.
        /// </summary>
        void Rollback();
    }
}

using System;
using System.Collections.Generic;
using System.Data;
using System.Text;

namespace CK.SqlServer
{
    /// <summary>
    /// Simple abstraction that handles nested transaction.
    /// </summary>
    public interface ISqlTransaction : IDisposable
    {
        /// <summary>
        /// Gets the connection controller.
        /// </summary>
        ISqlConnectionTransactionController ConnectionController { get; }

        /// <summary>
        /// Gets the status of this transaction.
        /// </summary>
        SqlTransactionStatus Status { get; }

        /// <summary>
        /// Gets the isolation level of this transaction.
        /// </summary>
        IsolationLevel IsolationLevel { get; }

        /// <summary>
        /// Gets whether this transaction is a nested one.
        /// When true, committing this transaction commits any nested transaction but
        /// the actual transaction will be committed only when the non-nested, root, transaction
        /// will be committed.
        /// </summary>
        bool IsNested { get; }

        /// <summary>
        /// Commits this transaction and any active nested transactions.
        /// Throws a <see cref="InvalidOperationException"/> if <see cref="Status"/> is not <see cref="SqlTransactionStatus.Opened"/>.
        /// </summary>
        void Commit();

        /// <summary>
        /// Rollbacks all transactions.
        /// Throws a <see cref="InvalidOperationException"/> if <see cref="Status"/> is not <see cref="SqlTransactionStatus.Opened"/>.
        /// </summary>
        void RollbackAll();
    }
}

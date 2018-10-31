using System;
using System.Collections.Generic;
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
        /// Gets whether this transaction has been <see cref="Commit"/> or <see cref="Rollback"/>.
        /// </summary>
        bool IsAlive { get; }

        /// <summary>
        /// Gets whether this transaction is a nested one.
        /// When true, committing this transaction commits any nested transaction but
        /// the actual transaction will be committed only when the non-nested, root, transaction
        /// will be committed.
        /// </summary>
        bool IsNested { get; }

        /// <summary>
        /// Commits this transaction and any active nested transactions.
        /// </summary>
        /// <returns>False if <see cref="Rollback"/> has been called, true if the actual primary transaction is still alive.</returns>
        bool Commit();

        /// <summary>
        /// Rollbacks all transactions.
        /// </summary>
        void Rollback();
    }
}

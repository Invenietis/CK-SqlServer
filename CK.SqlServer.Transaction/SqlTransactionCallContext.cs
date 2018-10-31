using CK.Core;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Text;

namespace CK.SqlServer
{
    public class SqlTransactionCallContext : SqlStandardCallContext, ISqlTransactionCallContext
    {
        /// <summary>
        /// Initializes a new <see cref="SqlTransactionCallContext"/> that may be bound to an existing monitor
        /// or to a command executor. 
        /// <para>
        /// If a <paramref name="executor"/> is provided, the protected <see cref="OnCommandExecuting"/>,
        /// <see cref="OnCommandExecuted"/> and <see cref="OnCommandError"/> are no more called: it is up to
        /// the external executor to fully handle command execution.
        /// </para>
        /// </summary>
        /// <param name="monitor">
        /// Optional monitor to use. When null, a new <see cref="ActivityMonitor"/> will be created
        /// when <see cref="Monitor"/> property is accessed.
        /// </param>
        /// <param name="executor">
        /// Optional command executor to which all command execution will be forwarded.
        /// </param>
        public SqlTransactionCallContext( IActivityMonitor monitor = null, ISqlCommandExecutor executor = null )
            : base( monitor, executor )
        {
        }

        /// <summary>
        /// Gets the connection controller to use for a given connection string.
        /// This controller is cached for any new connection string.
        /// </summary>
        /// <param name="connectionString">The connection string.</param>
        /// <returns>The connection controller to use.</returns>
        public new ISqlConnectionTransactionController this[string connectionString] => (ISqlConnectionTransactionController)GetController( connectionString );

        /// <summary>
        /// Gets the connection controller to use for a given connection string provider.
        /// This controller is cached for any new connection string.
        /// </summary>
        /// <param name="provider">The connection string provider.</param>
        /// <returns>The connection controller to use.</returns>
        public new ISqlConnectionTransactionController this[ISqlConnectionStringProvider provider] => (ISqlConnectionTransactionController)GetController( provider.ConnectionString );

        /// <summary>
        /// Gets the connection controller to use for a given connection string.
        /// This is simply a more explicit call to the actual indexer: <see cref="ISqlCallContext.this[string]"/>.
        /// </summary>
        /// <param name="connectionString">The connection string.</param>
        /// <returns>The connection controller to use.</returns>
        public new ISqlConnectionTransactionController GetConnectionController( string connectionString ) => (ISqlConnectionTransactionController)GetController( connectionString );

        /// <summary>
        /// Gets the connection controller to use for a given connection string provider.
        /// This is simply a more explicit call to the actual indexer: <see cref="ISqlCallContext.this[ISqlConnectionStringProvider]"/>.
        /// </summary>
        /// <param name="provider">The connection string provider.</param>
        /// <returns>The connection controller to use.</returns>
        public new ISqlConnectionTransactionController GetConnectionController( ISqlConnectionStringProvider provider ) => (ISqlConnectionTransactionController)GetController( provider.ConnectionString );


        class TransactionController : Controller, ISqlConnectionTransactionController
        {
            readonly List<SqlTransaction> _transactions;

            public TransactionController( SqlTransactionCallContext ctx, string connectionString )
                : base( ctx, connectionString )
            {
                _transactions = new List<SqlTransaction>();
            }

            public int TransactionCount => _transactions.Count;

            public IsolationLevel CurrentIsolationLevel => throw new NotImplementedException();

            ISqlTransactionCallContext ISqlConnectionTransactionController.SqlCallContext => (ISqlTransactionCallContext)base.SqlCallContext;

            public IDisposable BeginTransaction( IsolationLevel isolationLevel = IsolationLevel.ReadCommitted )
            {
                throw new NotImplementedException();
            }

            public bool Commit()
            {
                throw new NotImplementedException();
            }

            public void Rollback()
            {
                throw new NotImplementedException();
            }

            ISqlTransaction ISqlConnectionTransactionController.BeginTransaction( IsolationLevel isolationLevel )
            {
                throw new NotImplementedException();
            }
        }

        protected override Controller CreateController( string connectionString )
        {
            return new TransactionController( this, connectionString );
        }

    }
}

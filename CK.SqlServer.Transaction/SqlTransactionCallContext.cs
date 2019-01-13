using CK.Core;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics;
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
            int _transactionCount;

            interface IInternalTransaction : ISqlTransaction
            {
                IInternalTransaction OnRollbackAll();

                Nested CreateSub( IsolationLevel isolation );
                void OnSubClose( IInternalTransaction newCurrent, int depth );
            }

            class Primary : IInternalTransaction
            {
                readonly TransactionController _c;
                readonly SqlTransaction _sqlTransaction;
                Nested _sub;
                SqlTransactionStatus _status;

                public Primary( TransactionController c, IsolationLevel isolationLevel )
                {
                    _c = c;
                    IsolationLevel = isolationLevel;
                    _sqlTransaction = c.Connection.BeginTransaction( isolationLevel );
                }

                public Nested SubTransaction => _sub;

                public ISqlConnectionTransactionController ConnectionController => _c;

                public SqlTransactionStatus Status => _status;

                public bool IsNested => false;

                public IsolationLevel IsolationLevel { get; }

                public SqlTransaction SqlTransaction => _sqlTransaction;

                public void Commit()
                {
                    if( _status != SqlTransactionStatus.Opened ) throw new InvalidOperationException();
                    _status = SqlTransactionStatus.Committed;
                    _sqlTransaction.Commit();
                    _sqlTransaction.Dispose();
                    _c.OnMainClosed();
                }

                public void Dispose()
                {
                    if( _status == SqlTransactionStatus.Opened )
                    {
                        _c.SqlCallContext.Monitor.Warn( "Uncommitted nor rollbacked transaction. The primary transaction is implicitly rollbacked." );
                        RollbackAll();
                    }
                }

                public void RollbackAll()
                {
                    if( _status != SqlTransactionStatus.Opened ) throw new InvalidOperationException();
                    IInternalTransaction s = SubTransaction;
                    while( s != null ) s = s.OnRollbackAll();
                    _status = SqlTransactionStatus.Rollbacked;
                    _sqlTransaction.Rollback();
                    _sqlTransaction.Dispose();
                    _c.OnMainClosed();
                }

                IInternalTransaction IInternalTransaction.OnRollbackAll() => null;

                Nested IInternalTransaction.CreateSub( IsolationLevel isolation )
                {
                    Debug.Assert( _sub == null );
                    return _sub = new Nested( this, isolation );
                }

                void IInternalTransaction.OnSubClose( IInternalTransaction newCurrent, int depth )
                {
                    Debug.Assert( _sub != null );
                    _c._transactionCount = depth;
                    _c._current = newCurrent;
                }
            }

            class Nested : IInternalTransaction
            {
                readonly IInternalTransaction _super;
                SqlTransactionStatus _status;
                Nested _sub;

                public Nested( IInternalTransaction super, IsolationLevel isolationLevel )
                {
                    _super = super;
                    if( (IsolationLevel = isolationLevel) != super.IsolationLevel )
                    {
                    }
                }

                void RestoreSuperIsolationLevel()
                {
                    if( IsolationLevel != _super.IsolationLevel )
                    {
                    }
                }

                public ISqlConnectionTransactionController ConnectionController => _super.ConnectionController;

                public SqlTransactionStatus Status => _status;

                public bool IsNested => true;

                public IsolationLevel IsolationLevel { get; }

                public void Commit()
                {
                    if( _status != SqlTransactionStatus.Opened ) throw new InvalidOperationException();
                    if( _sub != null ) _sub.OnCommitAbove();
                    _status = SqlTransactionStatus.Committed;
                    RestoreSuperIsolationLevel();
                    _super.OnSubClose( _super, 0 );
                }

                private void OnCommitAbove()
                {
                    if( _sub != null ) _sub.OnCommitAbove();
                    _status = SqlTransactionStatus.Committed;
                }

                public void Dispose()
                {
                    if( _status == SqlTransactionStatus.Opened )
                    {
                        ConnectionController.SqlCallContext.Monitor.Warn( "Uncommitted nor rollbacked transaction. The primary transaction is implicitly rollbacked." );
                        RollbackAll();
                    }
                }

                public void RollbackAll() => _super.RollbackAll();

                IInternalTransaction IInternalTransaction.OnRollbackAll()
                {
                    _status = SqlTransactionStatus.Rollbacked;
                    return _super;
                }

                Nested IInternalTransaction.CreateSub( IsolationLevel isolation )
                {
                    Debug.Assert( _sub == null );
                    return _sub = new Nested( this, isolation );
                }

                void IInternalTransaction.OnSubClose( IInternalTransaction newCurrent, int depth )
                {
                    Debug.Assert( _sub != null );
                    if( this == newCurrent ) _sub = null;
                    _super.OnSubClose( newCurrent, depth + 1 );
                }

            }

            Primary _main;
            IInternalTransaction _current;

            public TransactionController( SqlTransactionCallContext ctx, string connectionString )
                : base( ctx, connectionString )
            {
            }

            public int TransactionCount => _transactionCount;

            public IsolationLevel CurrentIsolationLevel => _main?.IsolationLevel ?? IsolationLevel.Unspecified;

            ISqlTransactionCallContext ISqlConnectionTransactionController.SqlCallContext => (ISqlTransactionCallContext)base.SqlCallContext;

            public SqlTransaction SqlTransaction => _main?.SqlTransaction;

            public void Commit()
            {
                if( _current == null ) throw new InvalidOperationException();
                _current.Commit();
            }

            public void RollbackAll()
            {
                if( _main == null ) throw new InvalidOperationException();
                _main.RollbackAll();
            }

            void OnMainClosed()
            {
                _main = null;
                _transactionCount = 0;
                ImplicitClose();
            }

            public ISqlTransaction BeginTransaction( IsolationLevel isolationLevel )
            {
                ++_transactionCount;
                if( _main == null )
                {
                    ImplicitOpen();
                    _current = _main = new Primary( this, isolationLevel );
                }
                else
                {
                    _current = _current.CreateSub( isolationLevel );
                }
                return _current;
            }
        }

        protected override Controller CreateController( string connectionString )
        {
            return new TransactionController( this, connectionString );
        }

        protected override void OnCommandExecuting( SqlCommand cmd, int retryNumber )
        {
            var c = (TransactionController)FindController( cmd.Connection );
            if( c != null ) cmd.Transaction = c.SqlTransaction;
            base.OnCommandExecuting( cmd, retryNumber );
        }

    }
}

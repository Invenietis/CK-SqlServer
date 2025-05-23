using CK.Core;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using Microsoft.Data.SqlClient;
using System.Diagnostics;
using System.Text;

namespace CK.SqlServer;

/// <summary>
/// Specializes <see cref="SqlStandardCallContext"/> to offer transaction support.
/// </summary>
public class SqlTransactionCallContext : SqlStandardCallContext, ISqlTransactionCallContext
{
    /// <summary>
    /// Initializes a new <see cref="SqlTransactionCallContext"/> that may be bound to an existing monitor
    /// or to a command executor. 
    /// <para>
    /// If a <paramref name="executor"/> is provided, the protected <see cref="SqlStandardCallContext.OnCommandExecuting"/>,
    /// <see cref="SqlStandardCallContext.OnCommandExecuted"/> and <see cref="SqlStandardCallContext.OnCommandError"/> are no
    /// more called: it is up to the external executor to fully handle command execution.
    /// </para>
    /// </summary>
    /// <param name="monitor">
    /// Optional monitor to use. When null, a new <see cref="ActivityMonitor"/> will be created
    /// when <see cref="SqlStandardCallContext.Monitor">Monitor</see> property is accessed.
    /// </param>
    /// <param name="executor">
    /// Optional command executor to which all command execution will be forwarded.
    /// </param>
    public SqlTransactionCallContext( IActivityMonitor? monitor = null, ISqlCommandExecutor? executor = null )
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
            Nested CreateSub( IsolationLevel isolation );

            void OnSubClose( IInternalTransaction newCurrent, int depth );
        }

        class Primary : IInternalTransaction
        {
            readonly TransactionController _c;
            readonly SqlTransaction _sqlTransaction;
            Nested? _sub;
            SqlTransactionStatus _status;

            public Primary( TransactionController c, IsolationLevel isolationLevel )
            {
                _c = c;
                IsolationLevel = isolationLevel;
                _sqlTransaction = c.Connection.BeginTransaction( isolationLevel );
            }

            public Nested? SubTransaction => _sub;

            public ISqlConnectionTransactionController ConnectionController => _c;

            public SqlTransactionStatus Status => _status;

            public bool IsNested => false;

            public IsolationLevel IsolationLevel { get; }

            public SqlTransaction SqlTransaction => _sqlTransaction;

            public void Commit()
            {
                if( _status != SqlTransactionStatus.Opened ) throw new InvalidOperationException();
                _sub?.OnCommitAbove();
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
                Throw.CheckState( _status == SqlTransactionStatus.Opened );
                if( _sub != null )
                {
                    _sub.OnRollbackAll();
                    _sub = null;
                }
                _status = SqlTransactionStatus.Rollbacked;
                _sqlTransaction.Rollback();
                _sqlTransaction.Dispose();
                _c.OnMainClosed();
            }

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
                if( newCurrent == this ) _sub = null;
            }
        }

        class Nested : IInternalTransaction
        {
            readonly IInternalTransaction _super;
            SqlTransactionStatus _status;
            Nested? _sub;

            public Nested( IInternalTransaction super, IsolationLevel isolationLevel )
            {
                _super = super;
                if( (IsolationLevel = isolationLevel) != super.IsolationLevel )
                {
                    if( !SetTransactionLevel( _super.ConnectionController, isolationLevel ) )
                    {
                        Throw.InvalidOperationException( $"Unable to set isolation level: {super.IsolationLevel} => {isolationLevel}." );
                    }
                }
            }

            bool RestoreSuperIsolationLevel()
            {
                return IsolationLevel == _super.IsolationLevel || SetTransactionLevel( _super.ConnectionController, _super.IsolationLevel );
            }

            public ISqlConnectionTransactionController ConnectionController => _super.ConnectionController;

            public SqlTransactionStatus Status => _status;

            public bool IsNested => true;

            public IsolationLevel IsolationLevel { get; }

            public void Commit()
            {
                if( _status != SqlTransactionStatus.Opened ) throw new InvalidOperationException();
                if( !RestoreSuperIsolationLevel() )
                {
                    RollbackAll();
                }
                else
                {
                    if( _sub != null )
                    {
                        _sub.OnCommitAbove();
                        _sub = null;
                    }
                    _status = SqlTransactionStatus.Committed;
                    RestoreSuperIsolationLevel();
                    _super.OnSubClose( _super, 1 );
                }
            }

            internal void OnCommitAbove()
            {
                if( _sub != null )
                {
                    _sub.OnCommitAbove();
                    _sub = null;
                }
                _status = SqlTransactionStatus.Committed;
            }

            public void Dispose()
            {
                if( _status == SqlTransactionStatus.Opened )
                {
                    ConnectionController.SqlCallContext.Monitor.Warn( SqlHelper.Sql, "Uncommitted nor rollbacked transaction. The primary transaction is implicitly rollbacked." );
                    RollbackAll();
                }
            }

            public void RollbackAll() => _super.RollbackAll();

            public void OnRollbackAll()
            {
                if( _sub != null )
                {
                    _sub.OnRollbackAll();
                    _sub = null;
                }
                _status = SqlTransactionStatus.Rollbacked;
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

        Primary? _main;
        IInternalTransaction? _current;

        public TransactionController( SqlTransactionCallContext ctx, string connectionString )
            : base( ctx, connectionString )
        {
        }

        public int TransactionCount => _transactionCount;

        public IsolationLevel CurrentIsolationLevel => _main?.IsolationLevel ?? IsolationLevel.Unspecified;

        ISqlTransactionCallContext ISqlConnectionTransactionController.SqlCallContext => (ISqlTransactionCallContext)base.SqlCallContext;

        public override SqlTransaction? Transaction => _main?.SqlTransaction;

        public void Commit()
        {
            Throw.CheckState( "No transaction opened.", _current != null );
            _current.Commit();
        }

        public void RollbackAll()
        {
            Throw.CheckState( "No transaction opened.", _main != null );
            _main.RollbackAll();
        }

        void OnMainClosed()
        {
            Debug.Assert( _main != null );
            if( _main.IsolationLevel != IsolationLevel.ReadCommitted )
            {
                SetTransactionLevel( this, IsolationLevel.ReadCommitted );
            }
            _main = null;
            _transactionCount = 0;
            ImplicitClose();
        }

        ISqlTransaction ISqlConnectionTransactionController.BeginTransaction( IsolationLevel isolationLevel )
        {
            return DoBeginTransaction( isolationLevel );
        }

        IInternalTransaction DoBeginTransaction( IsolationLevel isolationLevel )
        {
            ++_transactionCount;
            if( _main == null )
            {
                ImplicitOpen();
                _current = _main = new Primary( this, isolationLevel );
            }
            else
            {
                Debug.Assert( _current != null );
                _current = _current.CreateSub( isolationLevel );
            }
            return _current;
        }
    }

    static bool SetTransactionLevel( ISqlConnectionTransactionController controller, IsolationLevel isolationLevel )
    {
        using( var cmd = new SqlCommand( $"SET TRANSACTION ISOLATION LEVEL {isolationLevel.ToSqlString()};" ) )
        {
            cmd.Transaction = controller.Transaction;
            cmd.Connection = controller.Connection;
            try
            {
                cmd.ExecuteNonQuery();
                return true;
            }
            catch( Exception ex )
            {
                controller.SqlCallContext.Monitor.Error( $"Failed to set isolation level {isolationLevel}.", ex );
                return false;
            }
        }
    }

    /// <summary>
    /// Creates a new <see cref="TransactionController"/> instead of a
    /// base <see cref="SqlStandardCallContext.Controller"/>.
    /// </summary>
    /// <param name="connectionString">The connection string.</param>
    /// <returns>A transaction controller.</returns>
    protected override Controller CreateController( string connectionString )
    {
        return new TransactionController( this, connectionString );
    }

}

using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Data.SqlClient;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using CK.Core;

namespace CK.SqlServer
{
    /// <summary>
    /// Standard implementation, open to extensions, of a disposable <see cref="ISqlCallContext"/> that supports 
    /// query execution by explicitly implementing <see cref="ISqlCommandExecutor"/>).
    /// This is the simplest way to implement calls to the database.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This class directly implements <see cref="ISqlCommandExecutor"/> interface but with explicit methods in
    /// order to avoid interface pollution. If an executor is provided in the constuctor, the protected <see cref="OnCommandExecuting"/>,
    /// <see cref="OnCommandExecuted"/> and <see cref="OnCommandError"/> are no more called: it is up to the provided
    /// executor to fully handle command execution.
    /// </para>
    /// <para>
    /// The <see cref="ISqlConnectionController"/> are cached and reused until <see cref="Dispose"/> is called:
    /// when disposing this context any opened connection are closed (and disposed).
    /// </para>
    /// </remarks>
    public class SqlStandardCallContext : IDisposableSqlCallContext, ISqlCommandExecutor
    {
        object _cache;
        readonly ISqlCommandExecutor _executor;
        IActivityMonitor _monitor;
        readonly bool _ownedMonitor;

        /// <summary>
        /// Initializes a new <see cref="SqlStandardCallContext"/> that may be bound to an existing monitor
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
        public SqlStandardCallContext( IActivityMonitor monitor = null, ISqlCommandExecutor executor = null )
        {
            _ownedMonitor = monitor == null;
            _monitor = monitor;
            _executor = executor ?? this;
        }

        /// <summary>
        /// Gets the monitor that can be used to log activities.
        /// </summary>
        public IActivityMonitor Monitor => _monitor ?? (_monitor = new ActivityMonitor());

        ISqlCommandExecutor ISqlCallContext.Executor => _executor;

        /// <summary>
        /// Disposes any cached <see cref="SqlConnection"/>. 
        /// This <see cref="SqlStandardCallContext"/> instance can be reused once disposed.
        /// </summary>
        public virtual void Dispose()
        {
            if( _cache != null )
            {
                ControlledSqlConnection c = _cache as ControlledSqlConnection;
                if( c != null ) c.Dispose();
                else
                {
                    ControlledSqlConnection[] cache = _cache as ControlledSqlConnection[];
                    for( int i = 0; i < cache.Length; ++i ) cache[i].Dispose();
                }
                _cache = null;
                if( _monitor != null && _ownedMonitor )
                {
                    _monitor.MonitorEnd();
                    _monitor = null;
                }
            }
        }

        protected class ControlledSqlConnection : DbConnection, ISqlConnectionController
        {
            readonly SqlConnection _connection;
            readonly string _connectionString;
            readonly SqlStandardCallContext _ctx;
            int _explicitOpenCount;
            int _implicitOpenCount;
            bool _allowStateChange;

            public ControlledSqlConnection( SqlStandardCallContext ctx, string connectionString )
            {
                _ctx = ctx;
                _connectionString = connectionString;
                _connection = new SqlConnection( connectionString );
                _connection.StateChange += OnConnectionStateChange;
            }

            void OnConnectionStateChange( object sender, System.Data.StateChangeEventArgs e )
            {
                if( !_allowStateChange
                    && e.CurrentState != e.OriginalState
                    && (e.CurrentState == System.Data.ConnectionState.Closed
                        || e.CurrentState == System.Data.ConnectionState.Open) )
                {
                    throw new InvalidOperationException( "SqlConnection MUST be opened or closed only by its ISqlConnectionController." );
                }
            }

            public override string ConnectionString
            {
                get => _connectionString;
                set => throw new InvalidOperationException( $"ConnectionString of a {nameof(ControlledSqlConnection)} cannot be changed." );
            }

            public override string Database => _connection.Database;

            public override string DataSource => _connection.DataSource;

            public override string ServerVersion => _connection.ServerVersion;

            public override ConnectionState State => _connection.State;

            public override void ChangeDatabase( string databaseName )
            {
                _connection.ChangeDatabase( databaseName );
            }

            public override void Open()
            {
                ImplicitOpen();
            }

            public override Task OpenAsync( CancellationToken cancellationToken )
            {
                return ImplicitOpenAsync( cancellationToken );
            }


            public override void Close()
            {
                ImplicitClose();
            }

            public SqlConnection Connection => _connection;

            public DbConnection GetDbConnection() => this;

            protected override DbTransaction BeginDbTransaction( IsolationLevel isolationLevel )
            {
                throw new NotSupportedException( "Transaction support is disabled at this level. Use either an autonomous connection (based on the ConnectionString) or use a CK.SqlServer.Transaction.SqlTransactionCallContext if transactions are required." );
            }

            protected override DbCommand CreateDbCommand() 
            {
                var cmd = _connection.CreateCommand();
                cmd.Transaction = Transaction;
                return cmd;
            }

            public virtual SqlTransaction Transaction => null;

            public ISqlCallContext SqlCallContext => _ctx;

            public bool IsExplicitlyOpened => _explicitOpenCount != 0;

            void DoOpen()
            {
                _allowStateChange = true;
                try
                {
                    _connection.Open();
                }
                finally
                {
                    _allowStateChange = false;
                }
            }

            Task DoOpenAsync( CancellationToken cancellationToken )
            {
                _allowStateChange = true;
                return _connection.OpenAsync( cancellationToken ).ContinueWith( _ => _allowStateChange = false );
            }

            void DoClose()
            {
                _allowStateChange = true;
                try
                {
                    _connection.Close();
                }
                finally
                {
                    _allowStateChange = false;
                }
            }

            internal void ExplicitClose()
            {
                if( _explicitOpenCount > 0 )
                {
                    if( --_explicitOpenCount == 0 && _implicitOpenCount == 0 )
                    {
                        DoClose();
                    }
                }
            }

            sealed class AutoCloser : IDisposable
            {
                ControlledSqlConnection _c;

                public AutoCloser( ControlledSqlConnection c )
                {
                    _c = c;
                }

                public void Dispose()
                {
                    if( _c != null )
                    {
                        _c.ExplicitClose();
                        _c = null;
                    }
                }
            }

            public IDisposable ExplicitOpen()
            {
                if( ++_explicitOpenCount == 1 && _implicitOpenCount == 0 )
                {
                    DoOpen();
                }
                return new AutoCloser( this );
            }

            public async Task<IDisposable> ExplicitOpenAsync( CancellationToken cancellationToken = default(CancellationToken) )
            {
                if( ++_explicitOpenCount == 1 && _implicitOpenCount == 0 )
                {
                    await DoOpenAsync( cancellationToken ).ConfigureAwait( false );
                }
                return new AutoCloser( this );
            }

            protected int ExplicitOpenCount => _explicitOpenCount;

            protected int ImplicitOpenCount => _implicitOpenCount;

            protected void ImplicitClose()
            {
                if( _implicitOpenCount > 0 )
                {
                    if( --_implicitOpenCount == 0 && _explicitOpenCount == 0 )
                    {
                        DoClose();
                    }
                }
            }

            protected void ImplicitOpen()
            {
                if( ++_implicitOpenCount == 1 && _explicitOpenCount == 0 )
                {
                    DoOpen();
                }
            }

            protected Task ImplicitOpenAsync( CancellationToken cancellationToken )
            {
                if( ++_implicitOpenCount == 1 && _explicitOpenCount == 0 )
                {
                    return DoOpenAsync( cancellationToken );
                }
                return Task.CompletedTask;
            }

            protected override DbProviderFactory DbProviderFactory => SqlClientFactory.Instance;

            protected override void Dispose( bool disposing )
            {
                if( disposing )
                {
                    _allowStateChange = true;
                    _connection.Dispose();
                }
                base.Dispose( disposing );
            }

#if !NET461
            public override void EnlistTransaction( System.Transactions.Transaction transaction ) => _connection.EnlistTransaction( transaction );
#endif

            public override DataTable GetSchema() => _connection.GetSchema();

            public override DataTable GetSchema( string collectionName ) => _connection.GetSchema( collectionName );

            public override DataTable GetSchema( string collectionName, string[] restrictionValues ) => _connection.GetSchema( collectionName, restrictionValues );

        }

        /// <summary>
        /// Gets the <see cref="Controller"/> object for the given connection.
        /// This method manages the cache and calls the factory method <see cref="CreateController(string)"/>
        /// as needed.
        /// </summary>
        /// <param name="connectionString">The connection string.</param>
        /// <returns>The typed controller.</returns>
        protected ControlledSqlConnection GetController( string connectionString )
        {
            ControlledSqlConnection c;
            if( _cache == null )
            {
                c = CreateController( connectionString );
                _cache = c;
                return c;
            }
            ControlledSqlConnection newC;
            c = _cache as ControlledSqlConnection;
            if( c != null )
            {
                if( c.ConnectionString == connectionString ) return c;
                newC = CreateController( connectionString );
                _cache = new ControlledSqlConnection[] { c, newC };
            }
            else
            {
                var cache = (ControlledSqlConnection[])_cache;
                for( int i = 0; i < cache.Length; i++ )
                {
                    c = cache[i];
                    if( c.ConnectionString == connectionString ) return c;
                }
                var newCache = new ControlledSqlConnection[cache.Length + 1];
                Array.Copy( cache, newCache, cache.Length );
                newC = CreateController( connectionString );
                newCache[cache.Length] = newC;
                _cache = newCache;
            }
            return newC;
        }

        /// <summary>
        /// Finds a controller by its connection. This is required because the <see cref="SqlConnection.ConnectionString"/>
        /// may be different than the initialized one (security information may be removed).
        /// </summary>
        /// <param name="connection">The connection instance.</param>
        /// <returns>Null or the controller associated to the connection instance.</returns>
        public ISqlConnectionController FindController( SqlConnection connection )
        {
            if( _cache is ControlledSqlConnection controller ) return controller.Connection == connection ? controller : null;
            var cache = (ControlledSqlConnection[])_cache;
            foreach( var c in cache )
            {
                if( c.Connection == connection ) return c;
            }
            return null;
        }

        /// <summary>
        /// Creates a new <see cref="Controller"/> object.
        /// </summary>
        /// <param name="connectionString">The connection string.</param>
        /// <returns>The new controller.</returns>
        protected virtual ControlledSqlConnection CreateController( string connectionString )
        {
            return new ControlledSqlConnection( this, connectionString );
        }

        /// <summary>
        /// Gets the connection controller to use for a given connection string.
        /// This controller is cached for any new connection string.
        /// </summary>
        /// <param name="connectionString">The connection string.</param>
        /// <returns>The connection controller to use.</returns>
        public ISqlConnectionController this[string connectionString] => GetController( connectionString );

        /// <summary>
        /// Gets the connection controller to use for a given connection string provider.
        /// This controller is cached for any new connection string.
        /// </summary>
        /// <param name="provider">The connection string provider.</param>
        /// <returns>The connection controller to use.</returns>
        public ISqlConnectionController this[ISqlConnectionStringProvider provider] => GetController( provider.ConnectionString );

        /// <summary>
        /// Gets the connection controller to use for a given connection string.
        /// This is simply a more explicit call to the actual indexer: <see cref="ISqlCallContext.this[string]"/>.
        /// </summary>
        /// <param name="connectionString">The connection string.</param>
        /// <returns>The connection controller to use.</returns>
        public ISqlConnectionController GetConnectionController( string connectionString ) => GetController( connectionString );

        /// <summary>
        /// Gets the connection controller to use for a given connection string provider.
        /// This is simply a more explicit call to the actual indexer: <see cref="ISqlCallContext.this[ISqlConnectionStringProvider]"/>.
        /// </summary>
        /// <param name="provider">The connection string provider.</param>
        /// <returns>The connection controller to use.</returns>
        public ISqlConnectionController GetConnectionController( ISqlConnectionStringProvider provider ) => GetController( provider.ConnectionString );

        T ISqlCommandExecutor.ExecuteQuery<T>(
            IActivityMonitor monitor,
            SqlConnection connection,
            SqlCommand cmd,
            Func<SqlCommand, T> innerExecutor,
            SqlTransaction transaction )
        {
            Debug.Assert( connection != null && connection.State == System.Data.ConnectionState.Open );
            DateTime start = DateTime.UtcNow;
            int retryCount = 0;
            List<SqlDetailedException> previous = null;
            T result;
            for(; ; )
            {
                SqlDetailedException e = null;
                try
                {
                    cmd.Connection = connection;
                    cmd.Transaction = transaction;
                    OnCommandExecuting( cmd, retryCount );

                    result = innerExecutor( cmd );
                    break;
                }
                catch( IOException ex )
                {
                    e = SqlDetailedException.Create( cmd, ex, retryCount++ );
                }
                catch( SqlException ex )
                {
                    e = SqlDetailedException.Create( cmd, ex, retryCount++ );
                }
                Debug.Assert( e != null );
                Monitor.Error( e );
                if( previous == null ) previous = new List<SqlDetailedException>();
                TimeSpan retry = OnCommandError( cmd, connection, e, previous, start );
                if( retry.Ticks < 0
                    || retry == TimeSpan.MaxValue
                    || previous.Count > 1000 )
                {
                    throw e;
                }
                previous.Add( e );
                Thread.Sleep( retry );
            }
            OnCommandExecuted( cmd, retryCount, result );
            return result;
        }

        async Task<T> ISqlCommandExecutor.ExecuteQueryAsync<T>(
            IActivityMonitor monitor,
            SqlConnection connection,
            SqlCommand cmd,
            Func<SqlCommand, CancellationToken, Task<T>> innerExecutor,
            SqlTransaction transaction,
            CancellationToken cancellationToken )
        {
            Debug.Assert( connection != null && connection.State == System.Data.ConnectionState.Open );
            DateTime start = DateTime.UtcNow;
            int retryCount = 0;
            List<SqlDetailedException> previous = null;
            T result;
            for(; ; )
            {
                SqlDetailedException e = null;
                try
                {
                    cmd.Connection = connection;
                    cmd.Transaction = transaction;
                    OnCommandExecuting( cmd, retryCount );

                    result = await innerExecutor( cmd, cancellationToken ).ConfigureAwait( false );
                    break;
                }
                catch( IOException ex )
                {
                    e = SqlDetailedException.Create( cmd, ex, retryCount++ );
                }
                catch( SqlException ex )
                {
                    e = SqlDetailedException.Create( cmd, ex, retryCount++ );
                }
                Debug.Assert( e != null );
                Monitor.Error( e );
                if( previous == null ) previous = new List<SqlDetailedException>();
                TimeSpan retry = OnCommandError( cmd, connection, e, previous, start );
                if( retry.Ticks < 0
                    || retry == TimeSpan.MaxValue
                    || previous.Count > 1000 )
                {
                    throw e;
                }
                previous.Add( e );
                await Task.Delay( retry ).ConfigureAwait( false );
            }
            OnCommandExecuted( cmd, retryCount, result );
            return result;
        }

        /// <summary>
        /// Extension point called before a command is executed.
        /// </summary>
        /// <param name="cmd">The command that is ready to be executed.</param>
        /// <param name="retryNumber">Current number of retries (0 the first time).</param>
        protected virtual void OnCommandExecuting( SqlCommand cmd, int retryNumber ) { }

        /// <summary>
        /// Extension point called after a command has been successfully executed.
        /// </summary>
        /// <param name="cmd">The executed command.</param>
        /// <param name="retryCount">Number of tries before success.</param>
        /// <param name="result">
        /// The result of the <see cref="SqlCommand.ExecuteNonQuery"/> execution (number of rows),
        /// or the result of the <see cref="SqlCommand.ExecuteScalar"/>, or any result object built
        /// by a more complex function.
        /// </param>
        protected virtual void OnCommandExecuted( SqlCommand cmd, int retryCount, object result ) { }

        /// <summary>
        /// Extension point called after a command failed.
        /// At this level, this method does nothing and returns <see cref="TimeSpan.MaxValue"/>: no retry will be done.
        /// <para>
        /// Note that any negative TimeSpan as well as TimeSpan.MaxValue will result in
        /// the <see cref="SqlDetailedException"/> being thrown.
        /// </para>
        /// </summary>
        /// <param name="cmd">The executing command.</param>
        /// <param name="c">The connection.</param>
        /// <param name="ex">The exception caught and wrapped in a <see cref="SqlDetailedException"/>.</param>
        /// <param name="previous">Previous errors when retries have been made. Empty on the first error.</param>
        /// <param name="firstExecutionTimeUtc">The Utc time of the first try.</param>
        /// <returns>The time span to retry. A negative time span or <see cref="TimeSpan.MaxValue"/> to skip retry.</returns>
        protected virtual TimeSpan OnCommandError(
            SqlCommand cmd,
            SqlConnection c,
            SqlDetailedException ex,
            IReadOnlyList<SqlDetailedException> previous,
            DateTime firstExecutionTimeUtc ) => TimeSpan.MaxValue;

    }

}

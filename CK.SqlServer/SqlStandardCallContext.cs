using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using Microsoft.Data.SqlClient;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using CK.Core;

namespace CK.SqlServer;

/// <summary>
/// Standard implementation, open to extensions, of a disposable <see cref="ISqlCallContext"/> that supports 
/// query execution by explicitly implementing <see cref="ISqlCommandExecutor"/>).
/// This is the simplest way to implement calls to the database.
/// </summary>
/// <remarks>
/// <para>
/// This class directly implements <see cref="ISqlCommandExecutor"/> interface but with explicit methods in
/// order to avoid interface pollution. If an executor is provided in the constructor, the protected <see cref="OnCommandExecuting"/>,
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
    object? _cache;
    readonly ISqlCommandExecutor _executor;
    IActivityMonitor? _monitor;
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
    public SqlStandardCallContext( IActivityMonitor? monitor = null, ISqlCommandExecutor? executor = null )
    {
        _ownedMonitor = monitor == null;
        _monitor = monitor;
        _executor = executor ?? this;
    }

    /// <summary>
    /// Gets the monitor that can be used to log activities.
    /// </summary>
    public IActivityMonitor Monitor => _monitor ??= new ActivityMonitor( GetType().Name );

    ISqlCommandExecutor ISqlCallContext.Executor => _executor;

    /// <summary>
    /// Disposes any cached <see cref="SqlConnection"/>. 
    /// This <see cref="SqlStandardCallContext"/> instance can be reused once disposed.
    /// </summary>
    public virtual void Dispose()
    {
        if( _cache != null )
        {
            if( _cache is Controller c ) c.DisposeConnection();
            else
            {
                Controller[] cache = (Controller[])_cache;
                for( int i = 0; i < cache.Length; ++i ) cache[i].DisposeConnection();
            }
            _cache = null;
            if( _monitor != null && _ownedMonitor )
            {
                _monitor.MonitorEnd();
                _monitor = null;
            }
        }
    }

    /// <summary>
    /// Implements <see cref="ISqlConnectionController"/>.
    /// This class should be specialized by specialized call contexts.
    /// </summary>
    protected class Controller : ISqlConnectionController
    {
        readonly SqlConnection _connection;
        readonly string _connectionString;
        readonly SqlStandardCallContext _ctx;
        int _explicitOpenCount;
        int _implicitOpenCount;
        bool _directOpen;
        bool _isOpeningOrClosing;

        /// <summary>
        /// Initializes a new <see cref="Controller"/>.
        /// </summary>
        /// <param name="ctx">The holding context.</param>
        /// <param name="connectionString">The connection string.</param>
        public Controller( SqlStandardCallContext ctx, string connectionString )
        {
            Throw.CheckNotNullArgument( ctx );
            Throw.CheckNotNullOrWhiteSpaceArgument( connectionString );
            _ctx = ctx;
            _connectionString = connectionString;
            _connection = new SqlConnection( connectionString );
            _connection.StateChange += OnConnectionStateChange;
            _connection.InfoMessage += OnConnectionInfoMessage;
        }

        void OnConnectionInfoMessage( object sender, SqlInfoMessageEventArgs e )
        {
            if( SqlHelper.LogSqlServerInfoMessage )
            {
                var messages = e.Errors;
                if( messages != null && messages.Count > 0 )
                {
                    using( _ctx.Monitor.TemporarilySetMinimalFilter( LogFilter.Trace ) )
                    using( _ctx.Monitor.TemporarilySetAutoTags( SqlHelper.Sql ) )
                        foreach( SqlError m in messages )
                        {
                            _ctx.Monitor.Trace( $"Procedure:'{m.Procedure}@{m.LineNumber}', Class: '{m.Class}', , Number: '{m.Number}', Message: '{m.Message}'." );
                        }
                }
            }
        }

        void OnConnectionStateChange( object sender, System.Data.StateChangeEventArgs e )
        {
            if( _isOpeningOrClosing ) return;

            if( !_isOpeningOrClosing
                && e.CurrentState != e.OriginalState
                && (e.CurrentState == ConnectionState.Open || e.CurrentState == ConnectionState.Closed) )
            {
                if( e.CurrentState == ConnectionState.Open )
                {
                    // There is nothing more to do here since an actual connection cannot be opened twice,
                    // it means that is was closed.
                    _directOpen = true;
                }
                else
                {
                    if( !_directOpen )
                    {
                        Throw.InvalidOperationException( "Direct SqlConnection.Close() is allowed only if it was Open() or OpenAsync() directly." );
                    }
                    _directOpen = false;
                }
            }
        }

        /// <summary>
        /// Gets the connection string.
        /// Note that this is the original string, not the one available on the <see cref="Connection"/> since
        /// they may differ.
        /// </summary>
        /// <remarks>
        /// Try to use <see cref="SqlHelper.RemoveSensitiveInformations(string)"/> whenever this must be logged or appear
        /// in an exception message.
        /// </remarks>
        public string ConnectionString => _connectionString;

        /// <summary>
        /// Gets the connection itself.
        /// </summary>
        public SqlConnection Connection => _connection;

        /// <summary>
        /// Gets a null transaction since at this level, transactions are not managed.  
        /// </summary>
        public virtual SqlTransaction? Transaction => null;

        /// <summary>
        /// Gets the context that contains this controller.
        /// </summary>
        public ISqlCallContext SqlCallContext => _ctx;

        /// <summary>
        /// Gets whether this connection has been explicitly opened.
        /// </summary>
        public bool IsExplicitlyOpened => _explicitOpenCount != 0;

        void DoOpen()
        {
            _isOpeningOrClosing = true;
            try
            {
                _connection.Open();
            }
            catch( Exception ex )
            {
                throw new SqlDetailedException( $"While opening connection to '{SqlHelper.RemoveSensitiveInformations( ConnectionString )}'.", ex );
            }
            finally
            {
                _isOpeningOrClosing = false;
            }
        }

        async Task DoOpenAsync( CancellationToken cancellationToken )
        {
            _isOpeningOrClosing = true;
            try
            {
                await _connection.OpenAsync( cancellationToken );
            }
            catch( Exception ex )
            {
                throw new SqlDetailedException( $"While opening connection: '{SqlHelper.RemoveSensitiveInformations( ConnectionString )}'.", ex );
            }
            finally
            {
                _isOpeningOrClosing = false;
            }
        }

        void DoClose()
        {
            _isOpeningOrClosing = true;
            try
            {
                _connection.Close();
            }
            catch( Exception ex )
            {
                _ctx.Monitor.Warn( SqlHelper.Sql, $"While closing connection: '{SqlHelper.RemoveSensitiveInformations( ConnectionString )}'.", ex );
                throw;
            }
            finally
            {
                _isOpeningOrClosing = false;
            }
        }

        internal void ExplicitClose()
        {
            if( _explicitOpenCount > 0 )
            {
                if( --_explicitOpenCount == 0 && _implicitOpenCount == 0 && !_directOpen )
                {
                    DoClose();
                }
            }
        }

        sealed class AutoCloser : IDisposable
        {
            Controller? _c;

            public AutoCloser( Controller c )
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

        /// <summary>
        /// Opens the connection to the database if it were closed.
        /// The internal count is always incremented.
        /// Returns a IDisposable that will allow the connection to be disposed when disposed.
        /// If this IDisposable is not disposed, the connection will be automatically disposed
        /// when the root <see cref="IDisposableSqlCallContext"/> will be disposed.
        /// </summary>
        /// <returns>A IDisposable that can be disposed.</returns>
        public IDisposable ExplicitOpen()
        {
            if( ++_explicitOpenCount == 1 && _implicitOpenCount == 0 && !_directOpen )
            {
                DoOpen();
            }
            return new AutoCloser( this );
        }

        /// <summary>
        /// Opens the connection to the database if it were closed.
        /// The internal count is always incremented.
        /// Returns a IDisposable that will allow the connection to be disposed when disposed.
        /// If this IDisposable is not disposed, the connection will be automatically disposed
        /// when the root <see cref="IDisposableSqlCallContext"/> will be disposed.
        /// </summary>
        /// <returns>A IDisposable that can be disposed.</returns>
        public async Task<IDisposable> ExplicitOpenAsync( CancellationToken cancellationToken = default )
        {
            if( ++_explicitOpenCount == 1 && _implicitOpenCount == 0 && !_directOpen )
            {
                await DoOpenAsync( cancellationToken ).ConfigureAwait( false );
            }
            return new AutoCloser( this );
        }

        /// <summary>
        /// Gets the current number of explicit opening.
        /// </summary>
        protected int ExplicitOpenCount => _explicitOpenCount;

        /// <summary>
        /// Gets the current number of implicit opening.
        /// </summary>
        protected int ImplicitOpenCount => _implicitOpenCount;

        /// <summary>
        /// Reserved for specialization.
        /// A secondary counter is used for implicit open/close.
        /// </summary>
        protected void ImplicitClose()
        {
            if( _implicitOpenCount > 0 )
            {
                if( --_implicitOpenCount == 0 && _explicitOpenCount == 0 && !_directOpen )
                {
                    DoClose();
                }
            }
        }

        /// <summary>
        /// Reserved for specialization.
        /// A secondary counter is used for implicit open/close.
        /// </summary>
        protected void ImplicitOpen()
        {
            if( ++_implicitOpenCount == 1 && _explicitOpenCount == 0 && !_directOpen )
            {
                DoOpen();
            }
        }

        /// <summary>
        /// Reserved for specialization.
        /// A secondary counter is used for implicit open/close.
        /// </summary>
        protected Task ImplicitOpenAsync( CancellationToken cancellationToken )
        {
            if( ++_implicitOpenCount == 1 && _explicitOpenCount == 0 && !_directOpen )
            {
                return DoOpenAsync( cancellationToken );
            }
            return Task.CompletedTask;
        }

        internal void DisposeConnection()
        {
            _isOpeningOrClosing = true;
            _connection.Dispose();
        }

    }

    /// <summary>
    /// Gets the <see cref="Controller"/> object for the given connection.
    /// This method manages the cache and calls the factory method <see cref="CreateController(string)"/>
    /// as needed.
    /// </summary>
    /// <param name="connectionString">The connection string.</param>
    /// <returns>The typed controller.</returns>
    protected Controller GetController( string connectionString )
    {
        Controller? c;
        if( _cache == null )
        {
            c = CreateController( connectionString );
            _cache = c;
            return c;
        }
        Controller newC;
        c = _cache as Controller;
        if( c != null )
        {
            if( c.ConnectionString == connectionString ) return c;
            newC = CreateController( connectionString );
            _cache = new Controller[] { c, newC };
        }
        else
        {
            var cache = (Controller[])_cache;
            for( int i = 0; i < cache.Length; i++ )
            {
                c = cache[i];
                if( c.ConnectionString == connectionString ) return c;
            }
            var newCache = new Controller[cache.Length + 1];
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
    public ISqlConnectionController? FindController( SqlConnection connection )
    {
        if( _cache == null ) return null;
        if( _cache is Controller controller ) return controller.Connection == connection ? controller : null;
        var cache = (Controller[])_cache;
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
    protected virtual Controller CreateController( string connectionString )
    {
        return new Controller( this, connectionString );
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

    T ISqlCommandExecutor.ExecuteQuery<T>( IActivityMonitor monitor,
                                           SqlConnection connection,
                                           SqlTransaction? transaction,
                                           SqlCommand cmd,
                                           Func<SqlCommand, T> innerExecutor )
    {
        Debug.Assert( connection != null && connection.State == System.Data.ConnectionState.Open );
        DateTime start = DateTime.UtcNow;
        int retryCount = 0;
        List<SqlDetailedException>? previous = null;
        T result;
        using( monitor.OpenDebug( $"Sync execution of {SqlHelper.CommandAsText( cmd )}." ) )
        {
            for(; ; )
            {
                SqlDetailedException e;
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
                catch( Exception ex )
                {
                    Monitor.Fatal( ex );
                    throw;
                }
                Debug.Assert( e != null );
                Monitor.Error( e );
                if( previous == null ) previous = new List<SqlDetailedException>();
                TimeSpan retry = OnCommandError( cmd, e, previous, start );
                if( retry < TimeSpan.Zero
                    || retry == TimeSpan.MaxValue
                    || previous.Count > 1000 )
                {
                    throw e;
                }
                previous.Add( e );
                Thread.Sleep( retry );
            }
            OnCommandExecuted( cmd, retryCount, result );
        }
        return result;
    }

    async Task<T> ISqlCommandExecutor.ExecuteQueryAsync<T>( IActivityMonitor monitor,
                                                            SqlConnection connection,
                                                            SqlTransaction? transaction,
                                                            SqlCommand cmd,
                                                            Func<SqlCommand, CancellationToken, Task<T>> innerExecutor,
                                                            CancellationToken cancellationToken )
    {
        Debug.Assert( connection != null && connection.State == System.Data.ConnectionState.Open );
        DateTime start = DateTime.UtcNow;
        int retryCount = 0;
        List<SqlDetailedException>? previous = null;
        T result;
        using( monitor.OpenDebug( $"Async execution of {SqlHelper.CommandAsText( cmd )}." ) )
        {
            for(; ; )
            {
                SqlDetailedException e;
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
                catch( Exception ex )
                {
                    Monitor.Fatal( ex );
                    throw;
                }
                Debug.Assert( e != null );
                Monitor.Error( e );
                if( previous == null ) previous = new List<SqlDetailedException>();
                TimeSpan retry = OnCommandError( cmd, e, previous, start );
                if( retry.Ticks < 0
                    || retry == TimeSpan.MaxValue
                    || previous.Count > 1000 )
                {
                    throw e;
                }
                previous.Add( e );
                await Task.Delay( retry, cancellationToken ).ConfigureAwait( false );
            }
            OnCommandExecuted( cmd, retryCount, result );
        }
        return result;
    }

    /// <summary>
    /// Extension point called before a command is executed.
    /// Does nothing at this level.
    /// </summary>
    /// <param name="cmd">The command that is ready to be executed.</param>
    /// <param name="retryNumber">Current number of retries (0 the first time).</param>
    protected virtual void OnCommandExecuting( SqlCommand cmd, int retryNumber ) { }

    /// <summary>
    /// Extension point called after a command has been successfully executed.
    /// Does nothing at this level.
    /// </summary>
    /// <param name="cmd">The executed command.</param>
    /// <param name="retryCount">Number of tries before success.</param>
    /// <param name="result">
    /// The result of the <see cref="SqlCommand.ExecuteNonQuery"/> execution (number of rows),
    /// or the result of the <see cref="SqlCommand.ExecuteScalar"/>, or any result object built
    /// by a more complex function.
    /// </param>
    protected virtual void OnCommandExecuted( SqlCommand cmd, int retryCount, object? result ) { }

    /// <summary>
    /// Extension point called after a command failed.
    /// At this level, this method does nothing and returns <see cref="TimeSpan.MaxValue"/>: no retry will be done.
    /// <para>
    /// Note that any negative TimeSpan as well as TimeSpan.MaxValue will result in
    /// the <see cref="SqlDetailedException"/> being thrown.
    /// </para>
    /// </summary>
    /// <param name="cmd">
    /// The executing command. <see cref="SqlCommand.Connection"/> contains the current connection
    /// and <see cref="SqlCommand.Transaction"/> (that can be null) the current transaction if any.
    /// </param>
    /// <param name="ex">The exception caught and wrapped in a <see cref="SqlDetailedException"/>.</param>
    /// <param name="previous">Previous errors when retries have been made. Empty on the first error.</param>
    /// <param name="firstExecutionTimeUtc">The Utc time of the first try.</param>
    /// <returns>The time span to retry. A negative time span or <see cref="TimeSpan.MaxValue"/> to skip retry.</returns>
    protected virtual TimeSpan OnCommandError( SqlCommand cmd,
                                               SqlDetailedException ex,
                                               IReadOnlyList<SqlDetailedException> previous,
                                               DateTime firstExecutionTimeUtc ) => TimeSpan.MaxValue;

}

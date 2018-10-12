using System;
using System.Collections.Generic;
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
    /// This is the simplest way to implement calls to the database: by specializing this type, application specific
    /// properties (like the ActorId) can also be used to automatically set method parameter values when used 
    /// with a <see cref="CK.SqlServer.Setup.ParameterSourceAttribute"/>.
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
                Controller c = _cache as Controller;
                if( c != null ) c.Dispose();
                else
                {
                    Controller[] cache = _cache as Controller[];
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

        class Controller : ISqlConnectionController
        {
            readonly SqlStandardCallContext _ctx;
            internal readonly string ConnectionString;
            readonly SqlConnection _connection;
            int _openCount;

            public Controller( SqlStandardCallContext ctx, string connectionString )
            {
                _ctx = ctx;
                ConnectionString = connectionString;
                _connection = new SqlConnection( connectionString );
            }

            public SqlConnection Connection => _connection;

            public int ExplicitOpenCount => _openCount;

            public ISqlCallContext SqlCallContext => _ctx;

            public void ExplicitClose()
            {
                if( _openCount > 0 )
                {
                    if( --_openCount == 0 )
                    {
                        _connection.Close();
                    }
                }
            }

            public void ExplicitOpen()
            {
                if( ++_openCount == 1 )
                {
                    _connection.Open();
                }
            }

            public Task ExplicitOpenAsync()
            {
                if( ++_openCount == 1 )
                {
                    return _connection.OpenAsync();
                }
                return Task.CompletedTask;
            }

            public void Dispose()
            {
                _connection.Dispose();
            }
        }

        Controller GetProvider( string connectionString )
        {
            Controller c;
            if( _cache == null )
            {
                c = new Controller( this, connectionString );
                _cache = c;
                return c;
            }
            Controller newC;
            c = _cache as Controller;
            if( c != null )
            {
                if( c.ConnectionString == connectionString ) return c;
                newC = new Controller( this, connectionString );
                _cache = new Controller[] { c, newC };
            }
            else
            {
                Controller[] cache = (Controller[])_cache;
                for( int i = 0; i < cache.Length; i++ )
                {
                    c = cache[i];
                    if( c.ConnectionString == connectionString ) return c;
                }
                Controller[] newCache = new Controller[cache.Length + 1];
                Array.Copy( cache, newCache, cache.Length );
                newC = new Controller( this, connectionString );
                newCache[cache.Length] = newC;
                _cache = newCache;
            }
            return newC;
        }

        /// <summary>
        /// Gets the connection controller to use for a given connection string.
        /// This controller is cached for any new connection string.
        /// </summary>
        /// <param name="connectionString">The connection string.</param>
        /// <returns>The connection controller to use.</returns>
        public ISqlConnectionController this[string connectionString] => GetProvider( connectionString );

        /// <summary>
        /// Gets the connection controller to use for a given connection string provider.
        /// This controller is cached for any new connection string.
        /// </summary>
        /// <param name="provider">The connection string provider.</param>
        /// <returns>The connection controller to use.</returns>
        public ISqlConnectionController this[ISqlConnectionStringProvider provider] => GetProvider( provider.ConnectionString );

        T ISqlCommandExecutor.ExecuteQuery<T>( IActivityMonitor monitor, SqlConnection connection, SqlCommand cmd, Func<SqlCommand, T> innerExecutor )
        {
            DateTime start = DateTime.UtcNow;
            int retryCount = 0;
            List<SqlDetailedException> previous = null;
            T result;
            for(; ; )
            {
                SqlDetailedException e = null;
                try
                {
                    using( connection.EnsureOpen() )
                    {
                        cmd.Connection = connection;
                        OnCommandExecuting( cmd, retryCount );

                        result = innerExecutor( cmd );
                        break;
                    }
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

        async Task<T> ISqlCommandExecutor.ExecuteQueryAsync<T>( IActivityMonitor monitor, SqlConnection connection, SqlCommand cmd, Func<SqlCommand, CancellationToken, Task<T>> innerExecutor, CancellationToken cancellationToken )
        {
            DateTime start = DateTime.UtcNow;
            int retryCount = 0;
            List<SqlDetailedException> previous = null;
            T result;
            for(; ; )
            {
                SqlDetailedException e = null;
                try
                {
                    using( await connection.EnsureOpenAsync( cancellationToken ).ConfigureAwait( false ) )
                    {
                        cmd.Connection = connection;
                        OnCommandExecuting( cmd, retryCount );

                        result = await innerExecutor( cmd, cancellationToken ).ConfigureAwait( false );
                        break;
                    }
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

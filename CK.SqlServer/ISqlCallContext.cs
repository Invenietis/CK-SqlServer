using CK.Core;
using System;
using Microsoft.Data.SqlClient;

namespace CK.SqlServer
{
    /// <summary>
    /// A <see cref="ISqlCallContext"/> is the main <see langword="interface"/> to one or more Sql Server databases: <br/>
    /// It manages one or more <see cref="SqlConnection"/> (wrapped in <see cref="ISqlConnectionController"/>),
    /// and provides safe options (like pre opening a connection). <br/><br/>
    /// Note that a <see cref="ISqlCallContext"/> does not implement <see cref="IDisposable"/>,
    /// it is the object implementing <see cref="ISqlCallContext"/> that may be <see cref="IDisposable"/>: you never have to worry about opening, closing or disposing the connections.
    /// </summary>
    /// <remarks>
    /// A ISqlCallContext exposes a <see cref="ISqlCommandExecutor"/>, a <see cref="Monitor"/>
    /// and manages a cache of <see cref="ISqlConnectionController"/> that can be accessed either by
    /// connection string or by <see cref="ISqlConnectionStringProvider"/>.
    /// </remarks>
    public interface ISqlCallContext : StObjSupport.IScopedAutoService
    {
        /// <summary>
        /// Gets the <see cref="ISqlCommandExecutor"/> that must be used to call databases.
        /// </summary>
        ISqlCommandExecutor Executor { get; }

        /// <summary>
        /// Gets the monitor that can be used to log activities.
        /// </summary>
        IActivityMonitor Monitor { get; }

        /// <summary>
        /// Gets the connection controller to use for a given connection string.
        /// This controller is cached for any new connection string.
        /// </summary>
        /// <param name="connectionString">The connection string.</param>
        /// <returns>The connection controller to use.</returns>
        ISqlConnectionController this[string connectionString] { get; }

        /// <summary>
        /// Gets the connection controller to use for a given connection string provider.
        /// This controller is cached for any new connection string.
        /// </summary>
        /// <param name="provider">The connection string provider.</param>
        /// <returns>The connection controller to use.</returns>
        ISqlConnectionController this[ISqlConnectionStringProvider provider] { get; }

        /// <summary>
        /// Gets the connection controller to use for a given connection string.
        /// This is simply a more explicit call to the actual indexer: <see cref="ISqlCallContext.this[string]"/>.
        /// </summary>
        /// <param name="connectionString">The connection string.</param>
        /// <returns>The connection controller to use.</returns>
        ISqlConnectionController GetConnectionController( string connectionString );

        /// <summary>
        /// Gets the connection controller to use for a given connection string provider.
        /// This is simply a more explicit call to the actual indexer: <see cref="ISqlCallContext.this[ISqlConnectionStringProvider]"/>.
        /// </summary>
        /// <param name="provider">The connection string provider.</param>
        /// <returns>The connection controller to use.</returns>
        ISqlConnectionController GetConnectionController( ISqlConnectionStringProvider provider );

        /// <summary>
        /// Finds a controller by its connection. This is required because the <see cref="SqlConnection.ConnectionString"/>
        /// may be different than the initialized one (security information may be removed).
        /// </summary>
        /// <param name="connection">The connection instance.</param>
        /// <returns>Null or the controller associated to the connection instance.</returns>
        ISqlConnectionController? FindController( SqlConnection connection );

    }
}

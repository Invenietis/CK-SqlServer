using CK.Core;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace CK.SqlServer
{
    /// <summary>
    /// Defines the two required methods to support command execution.
    /// </summary>
    public interface ISqlCommandExecutor
    {
        /// <summary>
        /// Executes the given command synchronously, relying on a function to handle the actual command
        /// execution and result construction.
        /// </summary>
        /// <typeparam name="T">Type of the returned object.</typeparam>
        /// <param name="monitor">Monitor to use.</param>
        /// <param name="connection">The connection.</param>
        /// <param name="cmd">The command to execute.</param>
        /// <param name="innerExecutor">The actual executor.</param>
        /// <returns>The result of the call built by <paramref name="innerExecutor"/>.</returns>
        T ExecuteQuery<T>( IActivityMonitor monitor, SqlConnection connection, SqlCommand cmd, Func<SqlCommand, T> innerExecutor );

        /// <summary>
        /// Executes the given command asynchronously, relying on a function to handle the actual command
        /// execution and result construction.
        /// </summary>
        /// <typeparam name="T">Type of the returned object.</typeparam>
        /// <param name="monitor">Monitor to use.</param>
        /// <param name="connection">The connection.</param>
        /// <param name="cmd">The command to execute.</param>
        /// <param name="innerExecutor">The actual executor (asynchronous).</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The result of the call built by <paramref name="innerExecutor"/>.</returns>
        Task<T> ExecuteQueryAsync<T>( IActivityMonitor monitor, SqlConnection connection, SqlCommand cmd, Func<SqlCommand, CancellationToken, Task<T>> innerExecutor, CancellationToken cancellationToken );

    }
}

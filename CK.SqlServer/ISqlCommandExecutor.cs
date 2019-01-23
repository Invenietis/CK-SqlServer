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
        /// Note: The connection is MUST be opened.
        /// </summary>
        /// <typeparam name="T">Type of the returned object.</typeparam>
        /// <param name="monitor">Monitor to use.</param>
        /// <param name="connection">The already opened connection. An ArgumentException is thrown if the connection is not opened.</param>
        /// <param name="cmd">The command to execute.</param>
        /// <param name="innerExecutor">The actual executor.</param>
        /// <param name="transaction">Transaction into which the execution should be executed.</param>
        /// <returns>The result of the call built by <paramref name="innerExecutor"/>.</returns>
        T ExecuteQuery<T>( IActivityMonitor monitor, SqlConnection connection, SqlCommand cmd, Func<SqlCommand, T> innerExecutor, SqlTransaction transaction );

        /// <summary>
        /// Executes the given command asynchronously, relying on a function to handle the actual command
        /// execution and result construction.
        /// Note: The connection is MUST be opened.
        /// </summary>
        /// <typeparam name="T">Type of the returned object.</typeparam>
        /// <param name="monitor">Monitor to use.</param>
        /// <param name="connection">The already opened connection. An ArgumentException is thrown if the connection is not opened.</param>
        /// <param name="cmd">The command to execute.</param>
        /// <param name="innerExecutor">The actual executor (asynchronous).</param>
        /// <param name="transaction">Transaction into which the execution should be executed.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The result of the call built by <paramref name="innerExecutor"/>.</returns>
        Task<T> ExecuteQueryAsync<T>( IActivityMonitor monitor, SqlConnection connection, SqlCommand cmd, Func<SqlCommand, CancellationToken, Task<T>> innerExecutor, SqlTransaction transaction, CancellationToken cancellationToken = default(CancellationToken) );

    }
}

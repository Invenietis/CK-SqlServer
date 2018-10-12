using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace CK.SqlServer
{
    /// <summary>
    /// Extends <see cref="ISqlConnectionController"/>.
    /// </summary>
    public static class SqlConnectionControllerExtension
    {
        /// <summary>
        /// Executes the given command synchronously, relying on a function to handle the actual command
        /// execution and result construction.
        /// </summary>
        /// <typeparam name="T">Type of the returned object.</typeparam>
        /// <param name="this">This connection controller.</param>
        /// <param name="cmd">The command to execute.</param>
        /// <param name="innerExecutor">The actual executor.</param>
        /// <returns>The result of the call built by <paramref name="innerExecutor"/>.</returns>
        public static T ExecuteQuery<T>( this ISqlConnectionController @this, SqlCommand cmd, Func<SqlCommand, T> innerExecutor )
        {
            var ctx = @this.SqlCallContext;
            return ctx.Executor.ExecuteQuery( ctx.Monitor, @this.Connection, cmd, innerExecutor );
        }

        /// <summary>
        /// Executes the given command asynchronously, relying on a function to handle the actual command
        /// execution and result construction.
        /// </summary>
        /// <typeparam name="T">Type of the returned object.</typeparam>
        /// <param name="this">This connection controller.</param>
        /// <param name="cmd">The command to execute.</param>
        /// <param name="innerExecutor">The actual executor (asynchronous).</param>
        /// <param name="cancellationToken">Optional cancellation token.</param>
        /// <returns>The result of the call built by <paramref name="innerExecutor"/>.</returns>
        public static Task<T> ExecuteQueryAsync<T>( this ISqlConnectionController @this, SqlCommand cmd, Func<SqlCommand, CancellationToken, Task<T>> innerExecutor, CancellationToken cancellationToken = default( CancellationToken ) )
        {
            var ctx = @this.SqlCallContext;
            return ctx.Executor.ExecuteQueryAsync( ctx.Monitor, @this.Connection, cmd, innerExecutor, cancellationToken );
        }

        /// <summary>
        /// Executes the given command.
        /// </summary>
        /// <param name="this">This connection controller.</param>
        /// <param name="cmd">The command to execute.</param>
        /// <returns>The return of the <see cref="SqlCommand.ExecuteNonQuery"/> (number of rows affected).</returns>
        public static int ExecuteNonQuery( this ISqlConnectionController @this, SqlCommand cmd )
        {
            var ctx = @this.SqlCallContext;
            return ctx.Executor.ExecuteQuery( ctx.Monitor, @this.Connection, cmd, c => c.ExecuteNonQuery() );
        }

        /// <summary>
        /// Executes the query and returns the first column of the first row in the result
        /// set returned by the query on a closed or already opened connection.
        /// All other columns and rows are ignored.
        /// The returned object is null if no rows are returned.
        /// </summary>
        /// <param name="this">This connection controller.</param>
        /// <param name="cmd">The command to execute.</param>
        /// <returns>The read value (can be <see cref="DBNull.Value"/>) or null if no rows are returned.</returns>
        public static object ExecuteScalar( this ISqlConnectionController @this, SqlCommand cmd )
        {
            var ctx = @this.SqlCallContext;
            return ctx.Executor.ExecuteQuery( ctx.Monitor, @this.Connection, cmd, c => c.ExecuteScalar() );
        }
        
        /// <summary>
        /// Executes a command asynchrously.
        /// Can be interrupted thanks to a <see cref="CancellationToken"/>.
        /// </summary>
        /// <param name="this">This connection controller.</param>
        /// <param name="cmd">The command to execute.</param>
        /// <param name="cancellationToken">Optional cancellation token.</param>
        /// <returns>The return of the <see cref="SqlCommand.ExecuteNonQuery"/> (number of rows affected).</returns>
        public static Task<int> ExecuteNonQueryAsync( this ISqlConnectionController @this, SqlCommand cmd, CancellationToken cancellationToken = default( CancellationToken ) )
        {
            var ctx = @this.SqlCallContext;
            return ctx.Executor.ExecuteQueryAsync( ctx.Monitor, @this.Connection, cmd, (c,t) => c.ExecuteNonQueryAsync( t ), cancellationToken );
        }

        /// <summary>
        /// Executes the query asynchrously and returns the first column of the first row in the result
        /// set returned by the query on a closed or already opened connection.
        /// All other columns and rows are ignored.
        /// The returned object is null if no rows are returned.
        /// </summary>
        /// <param name="this">This connection controller.</param>
        /// <param name="cmd">The command to execute.</param>
        /// <param name="cancellationToken">Optional cancellation token.</param>
        /// <returns>The read value (can be <see cref="DBNull.Value"/>) or null if no rows are returned.</returns>
        public static Task<object> ExecuteScalarAsync( this ISqlConnectionController @this, SqlCommand cmd, CancellationToken cancellationToken = default( CancellationToken ) )
        {
            var ctx = @this.SqlCallContext;
            return ctx.Executor.ExecuteQueryAsync( ctx.Monitor, @this.Connection, cmd, ( c, t ) => c.ExecuteScalarAsync( t ), cancellationToken );
        }

        /// <summary>
        /// Executes a one-row query (uses <see cref="CommandBehavior.SingleRow"/>) and builds an object based on
        /// the row data.
        /// </summary>
        /// <typeparam name="T">The result object type.</typeparam>
        /// <param name="this">This connection controller.</param>
        /// <param name="cmd">The command to execute.</param>
        /// <param name="builder">The function that builds an object: called with a null <see cref="SqlDataRow"/> when there is no result.</param>
        /// <returns>The built object.</returns>
        public static T ExecuteSingleRow<T>( this ISqlConnectionController @this, SqlCommand cmd, Func<SqlDataRow, T> builder )
        {
            T ReadRow( SqlCommand c )
            {
                using( var r = c.ExecuteReader( CommandBehavior.SingleRow ) )
                {
                    return r.Read()
                            ? builder( new SqlDataRow( r ) )
                            : builder( null );
                }
            }
            var ctx = @this.SqlCallContext;
            return ctx.Executor.ExecuteQuery( ctx.Monitor, @this.Connection, cmd, ReadRow );
        }

        /// <summary>
        /// Executes a one-row query (uses <see cref="CommandBehavior.SingleRow"/>) and builds an object based on
        /// the row data.
        /// </summary>
        /// <typeparam name="T">The result object type.</typeparam>
        /// <param name="this">This connection controller.</param>
        /// <param name="cmd">The command to execute.</param>
        /// <param name="builder">The function that builds an object: called with a null <see cref="SqlDataRow"/> when there is no result.</param>
        /// <param name="cancellationToken">Optional cancellation token.</param>
        /// <returns>The built object.</returns>
        public static Task<T> ExecuteSingleRowAsync<T>( this ISqlConnectionController @this, SqlCommand cmd, Func<SqlDataRow, T> builder, CancellationToken cancellationToken = default(CancellationToken) )
        {
            async Task<T> ReadRowAsync( SqlCommand c, CancellationToken t )
            {
                using( var r = await c.ExecuteReaderAsync( CommandBehavior.SingleRow, t ).ConfigureAwait( false ) )
                {
                    return await r.ReadAsync( t ).ConfigureAwait( false )
                            ? builder( new SqlDataRow( r ) )
                            : builder( null );
                }
            }
            var ctx = @this.SqlCallContext;
            return ctx.Executor.ExecuteQueryAsync( ctx.Monitor, @this.Connection, cmd, ReadRowAsync, cancellationToken );
        }

        /// <summary>
        /// Executes a query and builds a list of objects.
        /// </summary>
        /// <typeparam name="T">The result object type.</typeparam>
        /// <param name="this">This connection controller.</param>
        /// <param name="cmd">The command to execute.</param>
        /// <param name="builder">The function that must build a <typeparamref name="T"/> for each <see cref="SqlDataRow"/>.</param>
        /// <returns>The built object.</returns>
        public static List<T> ExecuteReader<T>( this ISqlConnectionController @this, SqlCommand cmd, Func<SqlDataRow, T> builder )
        {
            List<T> ReadRows( SqlCommand c )
            {
                var collector = new List<T>();
                using( var r = c.ExecuteReader() )
                {
                    var row = new SqlDataRow( r );
                    while( r.Read() )
                    {
                        collector.Add( builder( row ) );
                    }
                }
                return collector;
            }
            var ctx = @this.SqlCallContext;
            return ctx.Executor.ExecuteQuery( ctx.Monitor, @this.Connection, cmd, ReadRows );
        }

        /// <summary>
        /// Executes a query and builds a list of objects.
        /// </summary>
        /// <typeparam name="T">The result object type.</typeparam>
        /// <param name="this">This connection controller.</param>
        /// <param name="cmd">The command to execute.</param>
        /// <param name="builder">The function that must build a <typeparamref name="T"/> for each <see cref="SqlDataRow"/>.</param>
        /// <param name="cancellationToken">Optional cancellation token.</param>
        /// <returns>The list of built object.</returns>
        public static Task<List<T>> ExecuteReaderAsync<T>( this ISqlConnectionController @this, SqlCommand cmd, Func<SqlDataRow, T> builder, CancellationToken cancellationToken = default( CancellationToken ) )
        {
            async Task<List<T>> ReadRowsAsync( SqlCommand c, CancellationToken t )
            {
                var collector = new List<T>();
                using( var r = await c.ExecuteReaderAsync( t ).ConfigureAwait( false ) )
                {
                    var row = new SqlDataRow( r );
                    while( await r.ReadAsync( t ).ConfigureAwait( false ) )
                    {
                        collector.Add( builder( row ) );
                    }
                }
                return collector;
            }
            var ctx = @this.SqlCallContext;
            return ctx.Executor.ExecuteQueryAsync( ctx.Monitor, @this.Connection, cmd, ReadRowsAsync, cancellationToken );
        }
    }
}

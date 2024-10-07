using System;
using System.Collections.Generic;
using System.Data;
using Microsoft.Data.SqlClient;
using CK.Core;
using System.Threading;
using System.Threading.Tasks;

#nullable enable

namespace CK.SqlServer;

/// <summary>
/// Extends <see cref="ISqlConnectionController"/>.
/// </summary>
public static class SqlConnectionControllerExtension
{
    /// <summary>
    /// Simple relay to <see cref="ISqlConnectionController.ExplicitOpen"/> that forgets the
    /// returned IDisposable. The connection will remain opened until the holding <see cref="IDisposableSqlCallContext"/>
    /// is disposed.
    /// </summary>
    /// <param name="ctx">This connection controller.</param>
    public static void PreOpen( this ISqlConnectionController ctx )
    {
        ctx.ExplicitOpen();
    }

    /// <summary>
    /// Simple relay to <see cref="ISqlConnectionController.ExplicitOpenAsync(CancellationToken)"/> that forgets the
    /// returned IDisposable. The connection will remain opened until the holding <see cref="IDisposableSqlCallContext"/>
    /// is disposed.
    /// </summary>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <param name="ctx">This connection controller.</param>
    public static Task PreOpenAsync( this ISqlConnectionController ctx, CancellationToken cancellationToken = default )
    {
        return ctx.ExplicitOpenAsync( cancellationToken );
    }

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
        using( @this.ExplicitOpen() )
        using( ctx.Monitor.TemporarilySetAutoTags( SqlHelper.Sql ) )
        {
            return ctx.Executor.ExecuteQuery( ctx.Monitor, @this.Connection, @this.Transaction, cmd, innerExecutor );
        }
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
    public static async Task<T> ExecuteQueryAsync<T>( this ISqlConnectionController @this,
                                                      SqlCommand cmd,
                                                      Func<SqlCommand, CancellationToken, Task<T>> innerExecutor,
                                                      CancellationToken cancellationToken = default )
    {
        var ctx = @this.SqlCallContext;
        using( await @this.ExplicitOpenAsync( cancellationToken ) )
        using( ctx.Monitor.TemporarilySetAutoTags( SqlHelper.Sql ) )
        {
            return await ctx.Executor.ExecuteQueryAsync( ctx.Monitor, @this.Connection, @this.Transaction, cmd, innerExecutor, cancellationToken );
        }
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
        using( @this.ExplicitOpen() )
        using( ctx.Monitor.TemporarilySetAutoTags( SqlHelper.Sql ) )
        {
            return ctx.Executor.ExecuteQuery( ctx.Monitor, @this.Connection, @this.Transaction, cmd, c => c.ExecuteNonQuery() );
        }
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
        using( @this.ExplicitOpen() )
        using( ctx.Monitor.TemporarilySetAutoTags( SqlHelper.Sql ) )
        {
            return ctx.Executor.ExecuteQuery( ctx.Monitor, @this.Connection, @this.Transaction, cmd, c => c.ExecuteScalar() );
        }
    }

    /// <summary>
    /// Executes a command asynchronously.
    /// Can be interrupted thanks to a <see cref="CancellationToken"/>.
    /// </summary>
    /// <param name="this">This connection controller.</param>
    /// <param name="cmd">The command to execute.</param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <returns>The return of the <see cref="SqlCommand.ExecuteNonQuery"/> (number of rows affected).</returns>
    public static async Task<int> ExecuteNonQueryAsync( this ISqlConnectionController @this, SqlCommand cmd, CancellationToken cancellationToken = default )
    {
        var ctx = @this.SqlCallContext;
        using( await @this.ExplicitOpenAsync( cancellationToken ) )
        using( ctx.Monitor.TemporarilySetAutoTags( SqlHelper.Sql ) )
        {
            return await ctx.Executor.ExecuteQueryAsync( ctx.Monitor, @this.Connection, @this.Transaction, cmd, ( c, t ) => c.ExecuteNonQueryAsync( t ), cancellationToken );
        }
    }

    /// <summary>
    /// Executes the query asynchronously and returns the first column of the first row in the result
    /// set returned by the query on a closed or already opened connection.
    /// All other columns and rows are ignored.
    /// The returned object is null if no rows are returned.
    /// </summary>
    /// <param name="this">This connection controller.</param>
    /// <param name="cmd">The command to execute.</param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <returns>The read value (can be <see cref="DBNull.Value"/>) or null if no rows are returned.</returns>
    public static async Task<object?> ExecuteScalarAsync( this ISqlConnectionController @this, SqlCommand cmd, CancellationToken cancellationToken = default )
    {
        var ctx = @this.SqlCallContext;
        using( await @this.ExplicitOpenAsync( cancellationToken ) )
        using( ctx.Monitor.TemporarilySetAutoTags( SqlHelper.Sql ) )
        {
            return await ctx.Executor.ExecuteQueryAsync( ctx.Monitor, @this.Connection, @this.Transaction, cmd, ( c, t ) => c.ExecuteScalarAsync( t ), cancellationToken );
        }
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
    public static T ExecuteSingleRow<T>( this ISqlConnectionController @this, SqlCommand cmd, Func<SqlDataRow?, T> builder )
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
        return ExecuteQuery( @this, cmd, ReadRow );
    }

    /// <summary>
    /// Executes a one-row query (uses <see cref="CommandBehavior.SingleRow"/>) and builds an object based on
    /// the row data.
    /// <para>
    /// Important: When there is no result, the <paramref name="builder"/> function is called with a null SqlDataRow
    /// so that void result can also be handled.
    /// </para>
    /// </summary>
    /// <typeparam name="T">The result object type.</typeparam>
    /// <param name="this">This connection controller.</param>
    /// <param name="cmd">The command to execute.</param>
    /// <param name="builder">The function that builds an object (called with a null <see cref="SqlDataRow"/> when there is no result).</param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <returns>The built object.</returns>
    public static Task<T> ExecuteSingleRowAsync<T>( this ISqlConnectionController @this, SqlCommand cmd, Func<SqlDataRow?, T> builder, CancellationToken cancellationToken = default )
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
        return ExecuteQueryAsync( @this, cmd, ReadRowAsync, cancellationToken );
    }

    /// <summary>
    /// Executes a query and builds a list of non null objects.
    /// </summary>
    /// <typeparam name="T">The result object type.</typeparam>
    /// <param name="this">This connection controller.</param>
    /// <param name="cmd">The command to execute.</param>
    /// <param name="builder">The function that must build a <typeparamref name="T"/> for each <see cref="SqlDataRow"/> or returns null to skip the item.</param>
    /// <returns>The built object.</returns>
    public static List<T> ExecuteReader<T>( this ISqlConnectionController @this, SqlCommand cmd, Func<SqlDataRow, T?> builder ) where T : notnull
    {
        List<T> ReadRows( SqlCommand c )
        {
            var collector = new List<T>();
            using( var r = c.ExecuteReader() )
            {
                var row = new SqlDataRow( r );
                while( r.Read() )
                {
                    var o = builder( row );
                    if( o is not null ) collector.Add( o );
                }
            }
            return collector;
        }
        return ExecuteQuery( @this, cmd, ReadRows );
    }

    /// <summary>
    /// Executes a query and builds a list of non null objects.
    /// </summary>
    /// <typeparam name="T">The result object type.</typeparam>
    /// <param name="this">This connection controller.</param>
    /// <param name="cmd">The command to execute.</param>
    /// <param name="builder">The function that must build a <typeparamref name="T"/> for each <see cref="SqlDataRow"/> or returns null to skip the item.</param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <returns>The list of built object.</returns>
    public static Task<List<T>> ExecuteReaderAsync<T>( this ISqlConnectionController @this,
                                                       SqlCommand cmd,
                                                       Func<SqlDataRow, T?> builder,
                                                       CancellationToken cancellationToken = default ) where T : notnull

    {
        async Task<List<T>> ReadRowsAsync( SqlCommand c, CancellationToken t )
        {
            var collector = new List<T>();
            using( var r = await c.ExecuteReaderAsync( t ).ConfigureAwait( false ) )
            {
                var row = new SqlDataRow( r );
                while( await r.ReadAsync( t ).ConfigureAwait( false ) )
                {
                    var o = builder( row );
                    if( o is not null ) collector.Add( o );
                }
            }
            return collector;
        }
        return ExecuteQueryAsync( @this, cmd, ReadRowsAsync, cancellationToken );
    }
}

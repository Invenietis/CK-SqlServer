using System;

namespace CK.SqlServer;

/// <summary>
/// Extends <see cref="ISqlCallContext"/> to expose <see cref="ISqlConnectionTransactionController"/>
/// instead of the basic <see cref="ISqlConnectionController"/> for each managed connections.
/// </summary>
public interface ISqlTransactionCallContext : ISqlCallContext
{
    /// <summary>
    /// Gets the connection controller to use for a given connection string.
    /// This controller is cached for any new connection string.
    /// </summary>
    /// <param name="connectionString">The connection string.</param>
    /// <returns>The connection controller to use.</returns>
    new ISqlConnectionTransactionController this[string connectionString] { get; }

    /// <summary>
    /// Gets the connection controller to use for a given connection string provider.
    /// This controller is cached for any new connection string.
    /// </summary>
    /// <param name="provider">The connection string provider.</param>
    /// <returns>The connection controller to use.</returns>
    new ISqlConnectionTransactionController this[ISqlConnectionStringProvider provider] { get; }

    /// <summary>
    /// Gets the connection controller to use for a given connection string.
    /// This is simply a more explicit call to the actual indexer: <see cref="ISqlCallContext.this[string]"/>.
    /// </summary>
    /// <param name="connectionString">The connection string.</param>
    /// <returns>The connection controller to use.</returns>
    new ISqlConnectionTransactionController GetConnectionController( string connectionString );

    /// <summary>
    /// Gets the connection controller to use for a given connection string provider.
    /// This is simply a more explicit call to the actual indexer: <see cref="ISqlCallContext.this[ISqlConnectionStringProvider]"/>.
    /// </summary>
    /// <param name="provider">The connection string provider.</param>
    /// <returns>The connection controller to use.</returns>
    new ISqlConnectionTransactionController GetConnectionController( ISqlConnectionStringProvider provider );

}

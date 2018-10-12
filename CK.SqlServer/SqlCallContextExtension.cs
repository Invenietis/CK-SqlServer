using System;
using System.Collections.Generic;
using System.Text;

namespace CK.SqlServer
{
    /// <summary>
    /// Extends <see cref="ISqlCallContext"/> interface.
    /// </summary>
    public static class SqlCallContextExtension
    {
        /// <summary>
        /// Gets the connection controller to use for a given connection string.
        /// This is simply a more explicit call to the actual indexer: <see cref="ISqlCallContext.this[string]"/>.
        /// </summary>
        /// <param name="this">This Sql call context.</param>
        /// <param name="connectionString">The connection string.</param>
        /// <returns>The connection controller to use.</returns>
        public static ISqlConnectionController GetConnectionController( this ISqlCallContext @this, string connectionString ) => @this[connectionString];

        /// <summary>
        /// Gets the connection controller to use for a given connection string provider.
        /// This is simply a more explicit call to the actual indexer: <see cref="ISqlCallContext.this[ISqlConnectionStringProvider]"/>.
        /// </summary>
        /// <param name="this">This Sql call context.</param>
        /// <param name="provider">The connection string provider.</param>
        /// <returns>The connection controller to use.</returns>
        public static ISqlConnectionController GetConnectionController( this ISqlCallContext @this, ISqlConnectionStringProvider provider ) => @this[provider];

    }
}

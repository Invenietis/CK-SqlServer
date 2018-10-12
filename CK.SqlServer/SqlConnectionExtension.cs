using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Data.SqlClient;
using System.Threading;
using System.Threading.Tasks;

namespace CK.SqlServer
{
    /// <summary>
    /// Extension methods for SqlConnection: we only bring <see cref="EnsureOpen"/> and <see cref="EnsureOpenAsync"/>
    /// here to avoid redundancy with external frameworks (like Dapper).
    /// </summary>
    public static class SqlConnectionExtension
    {
        sealed class AutoCloser : IDisposable
        {
            readonly DbConnection _c;

            public AutoCloser( DbConnection c )
            {
                _c = c;   
            }

            public void Dispose()
            {
                _c.Close();
            }
        }

        /// <summary>
        /// Helper to be used in a using statement to open the connection if it not already opened.
        /// Returns either a IDisposable that will close it or null it the connection was already opened.
        /// </summary>
        /// <param name="this">This connection.</param>
        /// <returns>A IDisposable or null.</returns>
        public static IDisposable EnsureOpen( this DbConnection @this )
        {
            if( @this.State == ConnectionState.Closed )
            {
                @this.Open();
                return new AutoCloser( @this );
            }
            return null;
        }

        /// <summary>
        /// Helper to be used in a using statement to open the connection if it not already opened.
        /// Returns either a IDisposable that will close it or null it the connection was already opened.
        /// </summary>
        /// <param name="this">This connection.</param>
        /// <param name="cancel">Optional cancellation token.</param>
        /// <returns>A IDisposable> or null.</returns>
        public static async Task<IDisposable> EnsureOpenAsync( this DbConnection @this, CancellationToken cancel = default(CancellationToken) )
        {
            if( @this.State == ConnectionState.Closed )
            {
                await @this.OpenAsync( cancel ).ConfigureAwait( false );
                return new AutoCloser( @this );
            }
            return null;
        }

    }
}

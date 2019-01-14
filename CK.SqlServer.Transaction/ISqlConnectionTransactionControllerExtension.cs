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
    /// Extension methods for ISqlConnectionTransactionController.
    /// </summary>
    public static class ISqlConnectionTransactionControllerExtension
    {
        static string _getIsolationLevel = @"SELECT CASE
    WHEN transaction_isolation_level = 1 THEN 'READ UNCOMMITTED'
    WHEN transaction_isolation_level = 2 AND is_read_committed_snapshot_on = 1 THEN 'READ COMMITTED SNAPSHOT'
    WHEN transaction_isolation_level = 2 AND is_read_committed_snapshot_on = 0 THEN 'READ COMMITTED'
    WHEN transaction_isolation_level = 3 THEN 'REPEATABLE READ'
    WHEN transaction_isolation_level = 4 THEN 'SERIALIZABLE'
    WHEN transaction_isolation_level = 5 THEN 'SNAPSHOT'
    ELSE NULL
END
FROM sys.dm_exec_sessions AS s CROSS JOIN sys.databases AS d WHERE  session_id = @@SPID AND d.database_id = DB_ID();";


        /// <summary>
        /// Calls the database and retrieves the current <see cref="IsolationLevel"/> if the
        /// connection is opened.
        /// Returns <see cref="IsolationLevel.Unspecified"/> if the database is not opened.
        /// </summary>
        /// <param name="this">This connection controller.</param>
        /// <param name="cancel">Optional cancellation token.</param>
        /// <returns>The isolation level.</returns>
        public static async Task<IsolationLevel> GetCurrentIsolationLevelAsync( this ISqlConnectionTransactionController @this, CancellationToken cancel = default( CancellationToken ) )
        {
            if( @this.Connection.State != ConnectionState.Open ) return IsolationLevel.Unspecified;
            using( var cmd = new SqlCommand( _getIsolationLevel ) )
            {
                cmd.Transaction = @this.Transaction;
                cmd.Connection = @this.Connection;
                object o = await cmd.ExecuteScalarAsync();
                return Parse( o == DBNull.Value ? null : (string)o );
            }
        }

        /// <summary>
        /// Calls the database and retrieves the current <see cref="IsolationLevel"/> if the
        /// connection is opened.
        /// Returns <see cref="IsolationLevel.Unspecified"/> if the database is not opened.
        /// </summary>
        /// <param name="this">This transaction aware controller.</param>
        /// <returns>The isolation level.</returns>
        public static IsolationLevel GetCurrentIsolationLevel( this ISqlConnectionTransactionController @this )
        {
            if( @this.Connection.State != ConnectionState.Open ) return IsolationLevel.Unspecified;
            using( var cmd = new SqlCommand( _getIsolationLevel ) )
            {
                cmd.Transaction = @this.Transaction;
                cmd.Connection = @this.Connection;
                object o = cmd.ExecuteScalar();
                return Parse( o == DBNull.Value ? null : (string)o );
            }
        }

        static IsolationLevel Parse( string s )
        {
            switch( s )
            {
                case "READ UNCOMMITTED": return IsolationLevel.ReadUncommitted;
                case "READ COMMITTED SNAPSHOT":
                case "READ COMMITTED": return IsolationLevel.ReadCommitted;
                case "REPEATABLE READ": return IsolationLevel.RepeatableRead;
                case "SERIALIZABLE": return IsolationLevel.Serializable;
                case "SNAPSHOT": return IsolationLevel.Snapshot;
            }
            return IsolationLevel.Unspecified;
        }
    }
}

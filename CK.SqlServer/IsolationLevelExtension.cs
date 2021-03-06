using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Text;

namespace CK.SqlServer
{
    /// <summary>
    /// Extends isolation level.
    /// </summary>
    public static class IsolationLevelExtension
    {
        /// <summary>
        /// Gets the Sql Server isolation level name: "READ UNCOMMITTED", "READ COMMITTED",
        /// "REPEATABLE READ", "SNAPSHOT" and "SERIALIZABLE".
        /// </summary>
        /// <param name="this">This level.</param>
        /// <returns>The Sql Server level name.</returns>
        public static string ToSqlString( this IsolationLevel @this )
        {
            switch( @this )
            {
                case IsolationLevel.ReadUncommitted: return "READ UNCOMMITTED";
                case IsolationLevel.ReadCommitted: return "READ COMMITTED";
                case IsolationLevel.RepeatableRead: return "REPEATABLE READ";
                case IsolationLevel.Snapshot: return "SNAPSHOT";
                case IsolationLevel.Serializable: return "SERIALIZABLE";
            }
            return null;
        }

    }
}

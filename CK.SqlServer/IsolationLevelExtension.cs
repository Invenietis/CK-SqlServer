using System;
using System.Collections.Generic;
using System.Data;
using Microsoft.Data.SqlClient;
using System.Text;

namespace CK.SqlServer;

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
    public static string? ToSqlString( this IsolationLevel @this )
    {
        return @this switch
        {
            IsolationLevel.ReadUncommitted => "READ UNCOMMITTED",
            IsolationLevel.ReadCommitted => "READ COMMITTED",
            IsolationLevel.RepeatableRead => "REPEATABLE READ",
            IsolationLevel.Snapshot => "SNAPSHOT",
            IsolationLevel.Serializable => "SERIALIZABLE",
            _ => null,
        };
    }

}

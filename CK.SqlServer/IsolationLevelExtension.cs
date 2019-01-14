using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Text;

namespace CK.SqlServer
{
    public static class IsolationLevelExtension
    {
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

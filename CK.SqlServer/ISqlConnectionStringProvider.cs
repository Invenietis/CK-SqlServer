using System;
using System.Collections.Generic;
using Microsoft.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CK.SqlServer;

/// <summary>
/// General and basic interface for objects that know their connection string.
/// </summary>
public interface ISqlConnectionStringProvider
{
    /// <summary>
    /// Gets the connection string.
    /// </summary>
    string ConnectionString { get; }
}

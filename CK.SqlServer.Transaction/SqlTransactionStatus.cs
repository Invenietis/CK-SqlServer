using System;
using System.Collections.Generic;
using System.Text;

namespace CK.SqlServer;

/// <summary>
/// Defines the three states of a transaction.
/// </summary>
public enum SqlTransactionStatus
{
    /// <summary>
    /// The transaction is opened but not yet committed nor rollbacked.
    /// </summary>
    Opened,

    /// <summary>
    /// The transaction has been committed.
    /// </summary>
    Committed,

    /// <summary>
    /// The transaction has been rollbacked.
    /// </summary>
    Rollbacked
}

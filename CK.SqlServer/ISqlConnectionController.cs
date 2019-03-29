using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace CK.SqlServer
{
    /// <summary>
    /// Controls the opening or closing of <see cref="SqlConnection"/> objects and
    /// supports minimal helpers to ease database calls thanks to <see cref="SqlConnectionControllerExtension"/>
    /// extension methods.
    /// </summary>
    public interface ISqlConnectionController
    {
        /// <summary>
        /// Gets the <see cref="ISqlCallContext"/> to which this connection controller belongs.
        /// </summary>
        ISqlCallContext SqlCallContext { get; }

        /// <summary>
        /// Gets the connection string.
        /// Note that this is the original string, not the one available on the <see cref="Connection"/> since
        /// they may differ.
        /// It can be opened or closed either by <see cref="ExplicitOpen"/>, <see cref="ExplicitOpenAsync"/>
        /// or can be opened/closed directly locally: when opening it directly (the <see cref="SqlConnection.State"/>
        /// MUST be closed), it must be closed directly.
        /// </summary>
        SqlConnection Connection { get; }

        /// <summary>
        /// Gets the <see cref="SqlTransaction"/> if one has been started, null otherwise. 
        /// </summary>
        SqlTransaction Transaction { get; }

        /// <summary>
        /// Opens the connection to the database if it were closed.
        /// The internal count is always incremented.
        /// Returns a IDisposable that will allow the connection to be disposed when disposed.
        /// If this IDisposable is not disposed, the connection will be automatically disposed
        /// when the root <see cref="IDisposableSqlCallContext"/> will be disposed.
        /// </summary>
        /// <returns>A IDisposable that can be disposed.</returns>
        IDisposable ExplicitOpen();

        /// <summary>
        /// Opens the connection to the database if it were closed.
        /// The internal count is always incremented.
        /// Returns a IDisposable that will allow the connection to be disposed when disposed.
        /// If this IDisposable is not disposed, the connection will be automatically disposed
        /// when the root <see cref="IDisposableSqlCallContext"/> will be disposed.
        /// </summary>
        /// <returns>A IDisposable that can be disposed.</returns>
        Task<IDisposable> ExplicitOpenAsync( CancellationToken cancellationToken = default( CancellationToken ) );

        /// <summary>
        /// Gets whether the connection has been explicitly opened at least once.
        /// </summary>
        bool IsExplicitlyOpened { get; }

    }
}

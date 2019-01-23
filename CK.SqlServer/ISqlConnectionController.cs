using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CK.SqlServer
{
    /// <summary>
    /// Controls the opening or closing of <see cref="SqlConnection"/> objects and
    /// supports comprehensive helpers to ease database call thanks to <see cref="SqlConnectionControllerExtension"/>
    /// extension methods.
    /// </summary>
    public interface ISqlConnectionController
    {
        /// <summary>
        /// Gets the <see cref="ISqlCallContext"/> to which this connection controller belongs.
        /// </summary>
        ISqlCallContext SqlCallContext { get; }

        /// <summary>
        /// Gets the controlled actual connection.
        /// It can be opened or closed but MUST not be opened or closed directly:
        /// an <see cref="InvalidOperationException"/> will be thrown is such case.
        /// </summary>
        SqlConnection Connection { get; }

        /// <summary>
        /// Gets the <see cref="SqlTransaction"/> if one has been started, null otherwise. 
        /// </summary>
        SqlTransaction Transaction { get; }

        /// <summary>
        /// Opens the connection to the database if it were closed.
        /// Returns a IDisposable that will auto close it.
        /// </summary>
        /// <returns>A IDisposable that mustbe disposed.</returns>
        IDisposable ExplicitOpen();

        /// <summary>
        /// Opens the connection to the database if it were closed.
        /// Returns a IDisposable that will auto close it.
        /// </summary>
        /// <returns>A IDisposable that mustbe disposed.</returns>
        Task<IDisposable> ExplicitOpenAsync();

        /// <summary>
        /// Gets whether the connection has been explicitly opened.
        /// </summary>
        bool IsExplicitlyOpened { get; }

    }
}

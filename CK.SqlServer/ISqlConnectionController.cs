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
        /// It can be opened or closed.
        /// </summary>
        SqlConnection Connection { get; }

        /// <summary>
        /// Gets the <see cref="SqlTransaction"/> if one has been started, null otherwise. 
        /// </summary>
        SqlTransaction Transaction { get; }

        /// <summary>
        /// Opens the connection to the database if it were closed (only increments <see cref="ExplicitOpenCount"/> if the 
        /// <see cref="Connection"/> were already opened). The connection will remain opened
        /// until a corresponding explicit call to <see cref="ExplicitClose"/> is made.
        /// </summary>
        void ExplicitOpen();

        /// <summary>
        /// Opens the connection to the database if it were closed (only increments <see cref="ExplicitOpenCount"/> if the 
        /// <see cref="Connection"/> were already opened). The connection will remain opened
        /// until a corresponding explicit call to <see cref="ExplicitClose"/> is made.
        /// </summary>
        Task ExplicitOpenAsync();

        /// <summary>
        /// Gets the current number of <see cref="ExplicitOpen"/>.
        /// </summary>
        int ExplicitOpenCount { get; }

        /// <summary>
        /// Closes the connection to the database: decrements <see cref="ExplicitOpenCount"/> and closes the 
        /// connection if it is zero.
        /// Calling this more times than <see cref="ExplicitOpen"/> is ignored (the <see cref="ExplicitOpenCount"/>
        /// is never negative).
        /// </summary>
        void ExplicitClose();
    }
}

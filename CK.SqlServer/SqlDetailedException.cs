using System;
using System.IO;
using System.Data.SqlClient;
#if NET461
using System.Runtime.Serialization;
#endif

namespace CK.SqlServer
{
    /// <summary>
    /// Wraps a <see cref="SqlException"/> and offers a detailed 
    /// message with the actual parameter values.
    /// </summary>
    [Serializable]
    public class SqlDetailedException : Exception
    {
        /// <summary>
        /// Initializes a new <see cref="SqlDetailedException"/> on an inner <see cref="SqlException"/>.
        /// </summary>
        /// <param name="message">Message for this exception.</param>
        /// <param name="ex">Inner exception.</param>
        public SqlDetailedException( string message, SqlException ex )
            : base( message, ex )
        {
        }

        /// <summary>
        /// Initializes a new <see cref="SqlDetailedException"/> on an inner <see cref="IOException"/>.
        /// </summary>
        /// <param name="message">Message for this exception.</param>
        /// <param name="ex">Inner exception.</param>
        public SqlDetailedException( string message, IOException ex )
            : base( message, ex )
        {
        }

#if NET461
        /// <summary>
        /// Serialization support.
        /// </summary>
        /// <param name="info">The serialization info.</param>
        /// <param name="c">The context.</param>
        protected SqlDetailedException( SerializationInfo info, StreamingContext c )
            : base( info, c )
        {
        }
#endif

        /// <summary>
        /// Creates a new SqlDetailedException with a message containing the call
        /// when a <see cref="SqlException"/> is received.
        /// </summary>
        /// <param name="cmd">Command that generated the exception.</param>
        /// <param name="ex">The exception itself.</param>
        /// <param name="retryCount">Number of previous tries.</param>
        /// <returns>The detailed exception.</returns>
        static public SqlDetailedException Create( SqlCommand cmd, SqlException ex, int retryCount = 0 )
        {
            return new SqlDetailedException( CreateMessage( cmd, retryCount ), ex );
        }

        /// <summary>
        /// Creates a new SqlDetailedException with a message containing the call
        /// when an <see cref="IOException"/> is received.
        /// </summary>
        /// <param name="cmd">Command that generated the exception.</param>
        /// <param name="ex">The exception itself.</param>
        /// <param name="retryCount">Number of previous tries.</param>
        /// <returns>The detailed exception.</returns>
        static public SqlDetailedException Create( SqlCommand cmd, IOException ex, int retryCount = 0 )
        {
            return new SqlDetailedException( CreateMessage( cmd, retryCount ), ex );
        }

        static string CreateMessage( SqlCommand cmd, int retryCount)
        {
            string m = SqlHelper.CommandAsText( cmd );
            if( retryCount > 0 )
            {
                m = $"[Retry n°{retryCount}] {m}";
            }
            return m;
        }

        /// <summary>
        /// Gets the <see cref="SqlException"/> or null if it is not a SqlException.
        /// </summary>
        public SqlException InnerSqlException => InnerException as SqlException;

        /// <summary>
        /// Gets the <see cref="SqlException"/> or null if it is not a SqlException.
        /// </summary>
        public IOException InnerIOException => InnerException as IOException;

    }
}

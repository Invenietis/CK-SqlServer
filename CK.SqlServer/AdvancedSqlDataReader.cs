using System;
using System.Data.SqlClient;
using System.Threading.Tasks;

namespace CK.SqlServer
{
    public class AdvancedSqlDataReader : IDisposable
    {
        /// <summary>
        /// Encapsulates the <see cref="NextRow"/> information.
        /// </summary>
        public readonly struct RowLookup
        {
            /// <summary>
            /// Gets the next row or null if no more rows exist.
            /// </summary>
            public ISqlDataRow Row { get; }

            /// <summary>
            /// Gets whether the non null <see cref="Row"/> is the first row of a new result set.
            /// </summary>
            public bool IsResultFirstRow { get; }
        }

        readonly SqlDataReader _r;
        readonly SqlDataRow _rWrapper;
        ISqlDataRow _row;

        public AdvancedSqlDataReader( SqlDataReader r )
        {
            _r = r;
            _rWrapper = new SqlDataRow( r );
        }

        public void Dispose() => _r.Dispose();

        /// <summary>
        /// Gets the current row or null if none.
        /// </summary>
        public ISqlDataRow Row => _row;

        public RowLookup NextRow { get; }

        public bool HasRows => _r.HasRows;

        public bool NextResult()
        {
            return _r.NextResult();
        }

        public Task<bool> NextResultAsync()
        {
            return _r.NextResultAsync();
        }

        public bool Read()
        {
            if( _r.Read() )
            {
                _row = _rWrapper;
                return true;  
            }
            _row = null;
            return false;
        }

        public Task<bool> ReadAsync()
        {
            return _r.ReadAsync();
        }

    }
}

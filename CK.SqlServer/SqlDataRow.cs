using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;

namespace CK.SqlServer
{

    class DataReader : IDisposable
    {
        readonly SqlDataReader _r;

        public DataReader( SqlDataReader r )
        {
            _r = r;
        }

        public void Dispose() => ((IDisposable)_r).Dispose();

        public ISqlDataRow Row { get; }

        public bool HasRows { get; }

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
            return _r.Read();
        }

        public Task<bool> ReadAsync()
        {
            return _r.ReadAsync();
        }

    }

    /// <summary>
    /// Minimal wrapper that hides a <see cref="SqlDataReader"/>: only row data can be accessed
    /// through it.
    /// This doesn't implement <see cref="IDataRecord"/> because of <see cref="IDataRecord.GetData(int)"/> that is globally unsupported.
    /// Adds helpers like <see cref="GetBytes(int)"/> or <see cref="GetValues()"/>.
    /// </summary>
    public class SqlDataRow : ISqlDataRow
    {
        readonly SqlDataReader _r;

        /// <summary>
        /// Initialize a new row on a <see cref="SqlDataReader"/>.
        /// </summary>
        /// <param name="reader">The reader (can not be null).</param>
        public SqlDataRow( SqlDataReader reader )
        {
            if( reader == null ) throw new ArgumentNullException( nameof( reader ) );
            _r = reader;
        }

        /// <inheritdoc />
        public object this[int i] => _r[i];

        /// <inheritdoc />
        public object this[string name] => _r[name];


        /// <inheritdoc />
        public int FieldCount => _r.FieldCount;

        /// <inheritdoc />
        public bool GetBoolean( int i ) => _r.GetBoolean( i );

        /// <inheritdoc />
        public byte GetByte( int i ) => _r.GetByte( i );

        /// <inheritdoc />
        public long GetBytes( int i, long fieldOffset, byte[] buffer, int bufferIndex, int length ) => _r.GetBytes( i, fieldOffset, buffer, bufferIndex, length );

        /// <inheritdoc />
        public byte[] GetBytes( int i ) => _r.IsDBNull( i ) ? null : _r.GetSqlBytes( i ).Value;

        /// <inheritdoc />
        public char GetChar( int i ) => _r.GetChar( i );

        /// <inheritdoc />
        public long GetChars( int i, long dataIndex, char[] buffer, int bufferIndex, int length ) => GetChars( i, dataIndex, buffer, bufferIndex, length );

        /// <inheritdoc />
        public string GetDataTypeName( int i ) => _r.GetDataTypeName( i );

        /// <inheritdoc />
        public DateTime GetDateTime( int i ) => _r.GetDateTime( i );

        /// <inheritdoc />
        public DateTimeOffset GetDateTimeOffset( int i ) => _r.GetDateTimeOffset( i );

        /// <inheritdoc />
        public decimal GetDecimal( int i ) => _r.GetDecimal( i );

        /// <inheritdoc />
        public double GetDouble( int i ) => _r.GetDouble( i );

        /// <inheritdoc />
        public Type GetFieldType( int i ) => _r.GetFieldType( i );

        /// <inheritdoc />
        public T GetFieldValue<T>( int i ) => _r.GetFieldValue<T>( i );

        /// <inheritdoc />
        public Task<T> GetFieldValueAsync<T>( int i, CancellationToken cancellationToken = default( CancellationToken ) ) => _r.GetFieldValueAsync<T>( i, cancellationToken );

        /// <inheritdoc />
        public float GetFloat( int i ) => _r.GetFloat( i );

        /// <inheritdoc />
        public Guid GetGuid( int i ) => _r.GetGuid( i );

        /// <inheritdoc />
        public short GetInt16( int i ) => _r.GetInt16( i );

        /// <inheritdoc />
        public int GetInt32( int i ) => _r.GetInt32( i );

        /// <inheritdoc />
        public long GetInt64( int i ) => _r.GetInt64( i );

        /// <inheritdoc />
        public string GetName( int i ) => _r.GetName( i );

        /// <inheritdoc />
        public int GetOrdinal( string name ) => _r.GetOrdinal( name );

        /// <inheritdoc />
        public string GetString( int i ) => _r.GetString( i );

        /// <inheritdoc />
        public TextReader GetTextReader( int i ) => _r.GetTextReader( i );

        /// <inheritdoc />
        public object GetValue( int i ) => _r.GetValue( i );

        /// <inheritdoc />
        public int GetValues( object[] values ) => _r.GetValues( values );

        /// <inheritdoc />
        public object[] GetValues()
        {
            object[] o = new object[_r.FieldCount];
            _r.GetValues( o );
            return o;
        }

        /// <inheritdoc />
        public XmlReader GetXmlReader( int i ) => _r.GetXmlReader( i );

        /// <inheritdoc />
        public bool IsDBNull( int i ) => _r.IsDBNull( i );

        /// <inheritdoc />
        public Task<bool> IsDBNullAsync( int i, CancellationToken cancellationToken = default( CancellationToken ) ) => _r.IsDBNullAsync( i, cancellationToken );
    }
}

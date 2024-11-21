using System;
using System.Collections.Generic;
using System.Data;
using Microsoft.Data.SqlClient;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using CK.Core;

namespace CK.SqlServer;


/// <summary>
/// Minimal wrapper that hides a <see cref="SqlDataReader"/>: only row data can be accessed
/// through it.
/// Adds helpers like <see cref="GetBytes(int)"/> or <see cref="GetValues()"/>.
/// </summary>
public class SqlDataRow
{
    readonly SqlDataReader _r;

    /// <summary>
    /// Initialize a new row on a <see cref="SqlDataReader"/>.
    /// </summary>
    /// <param name="reader">The reader (can not be null).</param>
    public SqlDataRow( SqlDataReader reader )
    {
        Throw.CheckNotNullArgument( reader );
        _r = reader;
    }

    /// <summary>
    /// Gets the value of the specified column in its native format given the column
    /// ordinal.
    /// </summary>
    /// <param name="i">The zero-based column ordinal.</param>
    /// <returns>The value of the specified column in its native format.</returns>
    public object this[int i] => _r[i];

    /// <summary>
    /// Gets the value of the specified column in its native format given the column
    /// name.
    /// </summary>
    /// <param name="name">The column name.</param>
    /// <returns>The value of the specified column in its native format.</returns>
    public object this[string name] => _r[name];

    /// <summary>
    /// Gets the number of columns in the current row.
    /// </summary>
    public int FieldCount => _r.FieldCount;

    /// <summary>
    /// Gets the value of the specified column as a Boolean.
    /// </summary>
    /// <param name="i">The zero-based column ordinal.</param>
    /// <returns>The value of the column.</returns>
    public bool GetBoolean( int i ) => _r.GetBoolean( i );

    /// <summary>
    /// Gets the value of the specified column as a byte.
    /// </summary>
    /// <param name="i">The zero-based column ordinal.</param>
    /// <returns>The value of the specified column as a byte.</returns>
    public byte GetByte( int i ) => _r.GetByte( i );

    /// <summary>
    /// Reads a stream of bytes from the specified column offset into the buffer an array
    /// starting at the given buffer offset.
    /// </summary>
    /// <param name="i">The zero-based column ordinal.</param>
    /// <param name="fieldOffset">The index within the field from which to begin the read operation.</param>
    /// <param name="buffer">The buffer into which to read the stream of bytes.</param>
    /// <param name="bufferIndex">The index within the buffer where the write operation is to start.</param>
    /// <param name="length">The maximum length to copy into the buffer.</param>
    /// <returns>The actual number of bytes read.</returns>
    public long GetBytes( int i, long fieldOffset, byte[] buffer, int bufferIndex, int length ) => _r.GetBytes( i, fieldOffset, buffer, bufferIndex, length );

    /// <summary>
    /// Gets the value of the specified column as an array of bytes.
    /// Kindly returns null if <see cref="IsDBNull(int)"/> is true.
    /// </summary>
    /// <param name="i">The zero-based column ordinal.</param>
    /// <returns>The value of the specified column as an array of bytes or null if <see cref="IsDBNull(int)"/> is true.</returns>
    public byte[]? GetBytes( int i ) => _r.IsDBNull( i ) ? null : _r.GetSqlBytes( i ).Value;

    /// <summary>
    /// Gets the value of the specified column as a single character.
    /// </summary>
    /// <param name="i">The zero-based column ordinal.</param>
    /// <returns>The value of the specified column.</returns>
    public char GetChar( int i ) => _r.GetChar( i );

    /// <summary>
    /// Reads a stream of characters from the specified column offset into the buffer
    /// as an array starting at the given buffer offset.
    /// </summary>
    /// <param name="i">The zero-based column ordinal.</param>
    /// <param name="dataIndex">The index within the field from which to begin the read operation.</param>
    /// <param name="buffer">The buffer into which to read the stream of bytes.</param>
    /// <param name="bufferIndex">The index within the buffer where the write operation is to start.</param>
    /// <param name="length">The maximum length to copy into the buffer.</param>
    /// <returns>The actual number of characters read.</returns>
    public long GetChars( int i, long dataIndex, char[] buffer, int bufferIndex, int length ) => GetChars( i, dataIndex, buffer, bufferIndex, length );

    /// <summary>
    /// Gets a string representing the data type of the specified column.
    /// </summary>
    /// <param name="i">The zero-based column ordinal.</param>
    /// <returns>The string representing the data type of the specified column.</returns>
    public string GetDataTypeName( int i ) => _r.GetDataTypeName( i );

    /// <summary>
    /// Gets the value of the specified column as a <see cref="DateTime"/> object.
    /// </summary>
    /// <param name="i">The zero-based column ordinal.</param>
    /// <returns>The value of the specified column.</returns>
    public DateTime GetDateTime( int i ) => _r.GetDateTime( i );

    /// <summary>
    /// Retrieves the value of the specified column as a <see cref="DateTimeOffset"/> object.
    /// </summary>
    /// <param name="i">The zero-based column ordinal.</param>
    /// <returns>The value of the specified column.</returns>
    public DateTimeOffset GetDateTimeOffset( int i ) => _r.GetDateTimeOffset( i );

    /// <summary>
    /// Gets the value of the specified column as a <see cref="Decimal"/> object.
    /// </summary>
    /// <param name="i">The zero-based column ordinal.</param>
    /// <returns>The value of the specified column.</returns>
    public decimal GetDecimal( int i ) => _r.GetDecimal( i );

    /// <summary>
    /// Gets the value of the specified column as a double-precision floating point number.
    /// </summary>
    /// <param name="i">The zero-based column ordinal.</param>
    /// <returns>The value of the specified column.</returns>
    public double GetDouble( int i ) => _r.GetDouble( i );

    /// <summary>
    /// Gets the <see cref="Type"/> that is the data type of the object.
    /// </summary>
    /// <param name="i">The zero-based column ordinal.</param>
    /// <returns>
    /// The System.Type that is the data type of the object. If the type does not exist
    /// on the client, in the case of a User-Defined Type (UDT) returned from the database,
    /// GetFieldType returns null.
    /// </returns>
    public Type GetFieldType( int i ) => _r.GetFieldType( i );

    /// <summary>
    /// Synchronously gets the value of the specified column as a type.
    /// </summary>
    /// <typeparam name="T">Type of the value.</typeparam>
    /// <param name="i">The zero-based column ordinal.</param>
    /// <returns>The returned type object.</returns>
    public T GetFieldValue<T>( int i ) => _r.GetFieldValue<T>( i );

    /// <summary>
    /// Asynchronously gets the value of the specified column as a type.
    /// </summary>
    /// <typeparam name="T">Type of the value.</typeparam>
    /// <param name="i">The zero-based column ordinal.</param>
    /// <param name="cancellationToken"></param>
    /// <returns>The returned type object.</returns>
    public Task<T> GetFieldValueAsync<T>( int i, CancellationToken cancellationToken = default ) => _r.GetFieldValueAsync<T>( i, cancellationToken );

    /// <summary>
    /// Gets the value of the specified column as a single-precision floating point number.
    /// </summary>
    /// <param name="i">The zero-based column ordinal.</param>
    /// <returns>The value of the specified column.</returns>
    public float GetFloat( int i ) => _r.GetFloat( i );

    /// <summary>
    /// Gets the value of the specified column as a globally unique identifier (GUID).
    /// </summary>
    /// <param name="i">The zero-based column ordinal.</param>
    /// <returns>The value of the specified column.</returns>
    public Guid GetGuid( int i ) => _r.GetGuid( i );

    /// <summary>
    /// Gets the value of the specified column as a 16-bit signed integer.
    /// </summary>
    /// <param name="i">The zero-based column ordinal.</param>
    /// <returns>The value of the specified column.</returns>
    public short GetInt16( int i ) => _r.GetInt16( i );

    /// <summary>
    /// Gets the value of the specified column as a 32-bit signed integer.
    /// </summary>
    /// <param name="i">The zero-based column ordinal.</param>
    /// <returns>The value of the specified column.</returns>
    public int GetInt32( int i ) => _r.GetInt32( i );

    /// <summary>
    /// Gets the value of the specified column as a 64-bit signed integer.
    /// </summary>
    /// <param name="i">The zero-based column ordinal.</param>
    /// <returns>The value of the specified column.</returns>
    public long GetInt64( int i ) => _r.GetInt64( i );

    /// <summary>
    /// Gets the name of the specified column.
    /// </summary>
    /// <param name="i">The zero-based column ordinal.</param>
    /// <returns>The name of the specified column.</returns>
    public string GetName( int i ) => _r.GetName( i );

    /// <summary>
    /// Gets the column ordinal, given the name of the column.
    /// </summary>
    /// <param name="name">The name of the column.</param>
    /// <returns>The zero-based column ordinal.</returns>
    public int GetOrdinal( string name ) => _r.GetOrdinal( name );

    /// <summary>
    /// Gets the value of the specified column as a string.
    /// </summary>
    /// <param name="i">The zero-based column ordinal.</param>
    /// <returns>The value of the specified column.</returns>
    public string GetString( int i ) => _r.GetString( i );

    /// <summary>
    /// Retrieves Char, NChar, NText, NVarChar, text, varChar, and Variant data types
    /// as a System.IO.TextReader.
    /// </summary>
    /// <param name="i">The zero-based column ordinal.</param>
    /// <returns>A text reader.</returns>
    public TextReader GetTextReader( int i ) => _r.GetTextReader( i );

    /// <summary>
    /// Gets the value of the specified column in its native format.
    /// </summary>
    /// <param name="i">The zero-based column ordinal.</param>
    /// <returns>The value of the specified column in its native format.</returns>
    public object GetValue( int i ) => _r.GetValue( i );

    /// <summary>
    /// Populates an array of objects with the column values of the current row.
    /// </summary>
    /// <param name="values">An array of System.Object into which to copy the attribute columns.</param>
    /// <returns>The number of instances of objects copied in the array.</returns>
    public int GetValues( object[] values ) => _r.GetValues( values );

    /// <summary>
    /// Creates an array of objects with the column values of the current row.
    /// </summary>
    /// <returns>The values of the row.</returns>
    public object[] GetValues()
    {
        object[] o = new object[_r.FieldCount];
        _r.GetValues( o );
        return o;
    }

    /// <summary>
    /// Retrieves data of type XML as a <see cref="XmlReader"/>.
    /// </summary>
    /// <param name="i">The zero-based column ordinal.</param>
    /// <returns>The xml reader.</returns>
    public XmlReader GetXmlReader( int i ) => _r.GetXmlReader( i );

    /// <summary>
    /// Gets whether the column contains non-existent or missing values.
    /// </summary>
    /// <param name="i">The zero-based column ordinal.</param>
    /// <returns>true if the specified column value is equivalent to <see cref="DBNull"/>; otherwise false.</returns>
    public bool IsDBNull( int i ) => _r.IsDBNull( i );

    /// <summary>
    /// An asynchronous version of <see cref="SqlDataReader.IsDBNull(int)"/>,
    /// which gets a value that indicates whether the column contains non-existent or
    /// missing values. The cancellation token can be used to request that the operation
    /// be abandoned before the command timeout elapses. Exceptions will be reported
    /// via the returned Task object.
    /// </summary>
    /// <param name="i">The zero-based column ordinal.</param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <returns>true if the specified column value is equivalent to DBNull otherwise false.</returns>
    public Task<bool> IsDBNullAsync( int i, CancellationToken cancellationToken = default ) => _r.IsDBNullAsync( i, cancellationToken );
}

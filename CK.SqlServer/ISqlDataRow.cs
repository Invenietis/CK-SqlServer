using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;

namespace CK.SqlServer
{
    /// <summary>
    /// This interface has more accessors than <see cref="System.Data.IDataRecord"/> except
    /// the (globally unsupported) <see cref="System.Data.IDataRecord.GetData(int)"/>.
    /// </summary>
    public interface ISqlDataRow
    {
        /// <summary>
        /// Gets the value of the specified column in its native format given the column
        /// ordinal.
        /// </summary>
        /// <param name="i">The zero-based column ordinal.</param>
        /// <returns>The value of the specified column in its native format.</returns>
        object this[int i] { get; }

        /// <summary>
        /// Gets the value of the specified column in its native format given the column
        /// name.
        /// </summary>
        /// <param name="name">The column name.</param>
        /// <returns>The value of the specified column in its native format.</returns>
        object this[string name] { get; }

        /// <summary>
        /// Gets the number of columns in the current row.
        /// </summary>
        int FieldCount { get; }

        /// <summary>
        /// Gets the value of the specified column as a Boolean.
        /// </summary>
        /// <param name="i">The zero-based column ordinal.</param>
        /// <returns>The value of the column.</returns>
        bool GetBoolean( int i );

        /// <summary>
        /// Gets the value of the specified column as a byte.
        /// </summary>
        /// <param name="i">The zero-based column ordinal.</param>
        /// <returns>The value of the specified column as a byte.</returns>
        byte GetByte( int i );

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
        byte[] GetBytes( int i );

        /// <summary>
        /// Gets the value of the specified column as an array of bytes.
        /// Kindly returns null if <see cref="IsDBNull(int)"/> is true.
        /// </summary>
        /// <param name="i">The zero-based column ordinal.</param>
        /// <returns>The value of the specified column as an array of bytes or null if <see cref="IsDBNull(int)"/> is true.</returns>
        long GetBytes( int i, long fieldOffset, byte[] buffer, int bufferIndex, int length );

        /// <summary>
        /// Gets the value of the specified column as a single character.
        /// </summary>
        /// <param name="i">The zero-based column ordinal.</param>
        /// <returns>The value of the specified column.</returns>
        char GetChar( int i );

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
        long GetChars( int i, long dataIndex, char[] buffer, int bufferIndex, int length );

        /// <summary>
        /// Gets a string representing the data type of the specified column.
        /// </summary>
        /// <param name="i">The zero-based column ordinal.</param>
        /// <returns>The string representing the data type of the specified column.</returns>
        string GetDataTypeName( int i );

        /// <summary>
        /// Gets the value of the specified column as a <see cref="DateTime"/> object.
        /// </summary>
        /// <param name="i">The zero-based column ordinal.</param>
        /// <returns>The value of the specified column.</returns>
        DateTime GetDateTime( int i );

        /// <summary>
        /// Retrieves the value of the specified column as a <see cref="DateTimeOffset"/> object.
        /// </summary>
        /// <param name="i">The zero-based column ordinal.</param>
        /// <returns>The value of the specified column.</returns>
        DateTimeOffset GetDateTimeOffset( int i );

        /// <summary>
        /// Gets the value of the specified column as a <see cref="Decimal"/> object.
        /// </summary>
        /// <param name="i">The zero-based column ordinal.</param>
        /// <returns>The value of the specified column.</returns>
        decimal GetDecimal( int i );

        /// <summary>
        /// Gets the value of the specified column as a double-precision floating point number.
        /// </summary>
        /// <param name="i">The zero-based column ordinal.</param>
        /// <returns>The value of the specified column.</returns>
        double GetDouble( int i );

        /// <summary>
        /// Gets the <see cref="Type"/> that is the data type of the object.
        /// </summary>
        /// <param name="i">The zero-based column ordinal.</param>
        /// <returns>
        /// The System.Type that is the data type of the object. If the type does not exist
        /// on the client, in the case of a User-Defined Type (UDT) returned from the database,
        /// GetFieldType returns null.
        /// </returns>
        Type GetFieldType( int i );

        /// <summary>
        /// Synchronously gets the value of the specified column as a type.
        /// </summary>
        /// <typeparam name="T">Type of the value.</typeparam>
        /// <param name="i">The zero-based column ordinal.</param>
        /// <returns>The returned type object.</returns>
        T GetFieldValue<T>( int i );

        /// <summary>
        /// Asynchronously gets the value of the specified column as a type.
        /// </summary>
        /// <typeparam name="T">Type of the value.</typeparam>
        /// <param name="i">The zero-based column ordinal.</param>
        /// <param name="cancellationToken"></param>
        /// <returns>The returned type object.</returns>
        Task<T> GetFieldValueAsync<T>( int i, CancellationToken cancellationToken = default );

        /// <summary>
        /// Gets the value of the specified column as a single-precision floating point number.
        /// </summary>
        /// <param name="i">The zero-based column ordinal.</param>
        /// <returns>The value of the specified column.</returns>
        float GetFloat( int i );

        /// <summary>
        /// Gets the value of the specified column as a globally unique identifier (GUID).
        /// </summary>
        /// <param name="i">The zero-based column ordinal.</param>
        /// <returns>The value of the specified column.</returns>
        Guid GetGuid( int i );

        /// <summary>
        /// Gets the value of the specified column as a 16-bit signed integer.
        /// </summary>
        /// <param name="i">The zero-based column ordinal.</param>
        /// <returns>The value of the specified column.</returns>
        short GetInt16( int i );

        /// <summary>
        /// Gets the value of the specified column as a 32-bit signed integer.
        /// </summary>
        /// <param name="i">The zero-based column ordinal.</param>
        /// <returns>The value of the specified column.</returns>
        int GetInt32( int i );

        /// <summary>
        /// Gets the value of the specified column as a 64-bit signed integer.
        /// </summary>
        /// <param name="i">The zero-based column ordinal.</param>
        /// <returns>The value of the specified column.</returns>
        long GetInt64( int i );

        /// <summary>
        /// Gets the name of the specified column.
        /// </summary>
        /// <param name="i">The zero-based column ordinal.</param>
        /// <returns>The name of the specified column.</returns>
        string GetName( int i );

        /// <summary>
        /// Gets the column ordinal, given the name of the column.
        /// </summary>
        /// <param name="name">The name of the column.</param>
        /// <returns>The zero-based column ordinal.</returns>
        int GetOrdinal( string name );

        /// <summary>
        /// Gets the value of the specified column as a string.
        /// </summary>
        /// <param name="i">The zero-based column ordinal.</param>
        /// <returns>The value of the specified column.</returns>
        string GetString( int i );

        /// <summary>
        /// Retrieves Char, NChar, NText, NVarChar, text, varChar, and Variant data types
        /// as a System.IO.TextReader.
        /// </summary>
        /// <param name="i">The zero-based column ordinal.</param>
        /// <returns>A text reader.</returns>
        TextReader GetTextReader( int i );

        /// <summary>
        /// Gets the value of the specified column in its native format.
        /// </summary>
        /// <param name="i">The zero-based column ordinal.</param>
        /// <returns>The value of the specified column in its native format.</returns>
        object GetValue( int i );

        /// <summary>
        /// Populates an array of objects with the column values of the current row.
        /// </summary>
        /// <param name="values">An array of System.Object into which to copy the attribute columns.</param>
        /// <returns>The number of instances of objects copied in the array.</returns>
        object[] GetValues();

        /// <summary>
        /// Creates an array of objects with the column values of the current row.
        /// </summary>
        /// <returns>The values of the row.</returns>
        int GetValues( object[] values );

        /// <summary>
        /// Retrieves data of type XML as a <see cref="XmlReader"/>.
        /// </summary>
        /// <param name="i">The zero-based column ordinal.</param>
        /// <returns>The xml reader.</returns>
        XmlReader GetXmlReader( int i );

        /// <summary>
        /// Gets whether the column contains non-existent or missing values.
        /// </summary>
        /// <param name="i">The zero-based column ordinal.</param>
        /// <returns>true if the specified column value is equivalent to <see cref="DBNull"/>; otherwise false.</returns>
        bool IsDBNull( int i );

        /// <summary>
        /// An asynchronous version of System.Data.SqlClient.SqlDataReader.IsDBNull(System.Int32),
        /// which gets a value that indicates whether the column contains non-existent or
        /// missing values. The cancellation token can be used to request that the operation
        /// be abandoned before the command timeout elapses. Exceptions will be reported
        /// via the returned Task object.
        /// </summary>
        /// <param name="i">The zero-based column ordinal.</param>
        /// <param name="cancellationToken">Optional cancellation token.</param>
        /// <returns>true if the specified column value is equivalent to DBNull otherwise false.</returns>
        Task<bool> IsDBNullAsync( int i, CancellationToken cancellationToken = default );
    }
}

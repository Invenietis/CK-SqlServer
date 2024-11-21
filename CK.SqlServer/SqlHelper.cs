using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Data.SqlClient;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Globalization;
using System.Text.RegularExpressions;
using System.Reflection;
using CK.Core;

namespace CK.SqlServer;

/// <summary>
/// Offers utility methods to deal with Sql Server objects and data.
/// </summary>
public static class SqlHelper
{
    /// <summary>
    /// The "Sql" tag.
    /// </summary>
    public static readonly CKTrait Sql = ActivityMonitor.Tags.Register( "Sql" );

    static double _timeoutFactor = 1.0;

    /// <summary>
    /// Gets or sets a factor that can used to alter the <see cref="SqlCommand.CommandTimeout"/> configuration (the default value for
    /// command timeout is 30 seconds.).
    /// This factor obviously defaults to 1.0 (by default, nothing is changed).
    /// <para>
    /// This value cannot be negative. A value of 0 indicates no limit: no more SqlCommand will benefit of any timeout.
    /// </para>
    /// <para>
    /// Nothing is automatic here: using this property must be done explicitly by user code, the recommended
    /// approach being: the sooner the better.
    /// </para>
    /// <para>
    /// Right after the instantiation of the command (in any factory method) is the perfect place to apply
    /// this factor: this is to avoid multiple application of this factor that won't be easy to track.
    /// </para>
    /// </summary>
    public static double CommandTimeoutFactor
    {
        get => _timeoutFactor;
        set
        {
            if( value < 0 ) throw new ArgumentOutOfRangeException( "Must be zero or positive.", nameof( CommandTimeoutFactor ) );
            _timeoutFactor = value;
        }
    }

    /// <summary>
    /// Gets or sets whether <see cref="SqlConnection.InfoMessage"/> events should be logged into each <see cref="ISqlCallContext.Monitor"/>.
    /// Default to false.
    /// </summary>
    public static bool LogSqlServerInfoMessage { get; set; }

    /// <summary>
    /// The minimal possible datetime2(n) is the same as the <see cref="Util.UtcMinValue"/> whatever the
    /// precision is (n being 0 to 7). In sql it is 'convert( datetime2(n), '00010101' )'.
    /// Things get more complicated for <see cref="IsUtcMaxValue(DateTime)"/>.
    /// </summary>
    /// <param name="d">The date time to test.</param>
    public static bool IsUtcMinValue( DateTime d ) => d == Util.UtcMinValue;

    /// <summary>
    /// This is the maximal value of a datetime2(0). All other precisions (up to 7) have
    /// a greater value up to <see cref="Util.UtcMaxValue"/> (for precision 7).
    /// </summary>
    public static readonly DateTime UtcMaxValuePrecision0 = Util.UtcMaxValue.AddTicks( -9999999 );

    /// <summary>
    /// Check whether the date is equal or greater to <see cref="UtcMaxValuePrecision0"/>.
    /// </summary>
    /// <param name="d">The date time to test.</param>
    public static bool IsUtcMaxValue( DateTime d ) => d >= UtcMaxValuePrecision0;

    /// <summary>
    /// The maximal possible datetime2(n) depends on the precision (that must be between 0 to 7 included).
    /// The very maximum is <see cref="Util.UtcMaxValue"/> that is 'convert( datetime2(7), '99991231 23:59:59.9999999' )' in sql
    /// but for the other precision, trailing 9 must be removed up to the precision.
    /// </summary>
    /// <param name="d">The date time to test.</param>
    /// <param name="precision">The precision. Must be between 0 to 7 included.</param>
    public static bool IsUtcMaxValue( DateTime d, int precision )
    {
        if( precision < 0 || precision > 7 ) throw new ArgumentOutOfRangeException( nameof( precision ) );
        if( d < UtcMaxValuePrecision0 ) return false;
        switch( precision )
        {
            case 0: return d >= UtcMaxValuePrecision0;
            case 7: return d == Util.UtcMaxValue;
            case 1: return d >= Util.UtcMaxValue.AddTicks( -999999 );
            case 2: return d >= Util.UtcMaxValue.AddTicks( -99999 );
            case 3: return d >= Util.UtcMaxValue.AddTicks( -9999 );
            case 4: return d >= Util.UtcMaxValue.AddTicks( -999 );
            case 5: return d >= Util.UtcMaxValue.AddTicks( -99 );
            default:
                Debug.Assert( precision == 6 );
                return d >= Util.UtcMaxValue.AddTicks( -9 );
        }
    }

    /// <summary>
    /// Standard name of the return value. Applied to functions and stored procedures.
    /// </summary>
    public const string ReturnParameterName = "RETURN_VALUE";

    static readonly Regex _rGo = new Regex( @"^\s*GO(?:\s|$)+", RegexOptions.Multiline | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled );

    /// <summary>
    /// Writes the command as a text with its parameters. Handles stored procedure calls as well as simple text commands.
    /// </summary>
    /// <param name="w">The writer to use.</param>
    /// <param name="cmd">The command to write.</param>
    /// <returns>The writer.</returns>
    static public TextWriter CommandAsText( TextWriter w, SqlCommand cmd )
    {
        if( cmd.CommandType == CommandType.StoredProcedure )
        {
            w.Write( $"exec {cmd.CommandText} <= " );
            WriteCallParameters( w, cmd.Parameters );
        }
        else
        {
            WriteCallParameters( w, cmd.Parameters );
            w.Write( " => " );
            w.Write( cmd.CommandText );
        }
        return w;
    }

    /// <summary>
    /// Returns a string with the command and its parameters. 
    /// Handles stored procedure calls as well as simple text commands.
    /// </summary>
    /// <param name="cmd">The command to call.</param>
    /// <returns>A textual representation.</returns>
    static public string CommandAsText( SqlCommand cmd )
    {
        StringWriter w = new StringWriter();
        CommandAsText( w, cmd );
        return w.GetStringBuilder().ToString();
    }

    /// <summary>
    /// Writes to the given <see cref="TextWriter"/> the parameters separated by commas.
    /// </summary>
    /// <param name="w">The target <see cref="TextWriter"/>.</param>
    /// <param name="c"><see cref="SqlParameterCollection"/> to write.</param>
    static public TextWriter WriteCallParameters( TextWriter w, SqlParameterCollection c )
    {
        bool atLeastOne = false;
        foreach( SqlParameter p in c )
        {
            if( p.Direction != ParameterDirection.ReturnValue )
            {
                if( atLeastOne ) w.Write( ", " );
                atLeastOne = true;
                w.Write( p.ParameterName );
                w.Write( '=' );
                w.Write( SqlValue( p.Value, p.SqlDbType ) );
                if( p.Direction != ParameterDirection.Input ) w.Write( " output" );
            }
        }
        return w;
    }

    /// <summary>
    /// Uses <see cref="SqlConnectionStringBuilder"/> to remove <see cref="SqlConnectionStringBuilder.UserID"/>
    /// and <see cref="SqlConnectionStringBuilder.Password"/> from the provided string.
    /// This never throws: worst case is to return a string with the exception type name followed by the
    /// exception message.
    /// </summary>
    /// <param name="connectionString">The connection string to cleanup.</param>
    /// <returns>A string that may be an error message.</returns>
    public static string RemoveSensitiveInformations( string connectionString )
    {
        if( string.IsNullOrWhiteSpace( connectionString ) )
        {
            return $"ArgumentException: connection string cannot be null or empty.";
        }
        try
        {
            var c = new SqlConnectionStringBuilder( connectionString );
            c["Password"] = null;
            c["User ID"] = null;
            return c.ToString();

        }
        catch( Exception ex )
        {
            return $"{ex.GetType().Name}: {ex.Message}";
        }
    }

    static readonly Type?[] _typesMap = new Type?[]
    {
        typeof(long), // SqlDbType.BigInt
        typeof(byte[]), // SqlDbType.Binary
        typeof(bool), // SqlDbType.Bit
        typeof(string), // SqlDbType.Char
        typeof(DateTime), // SqlDbType.DateTime
        typeof(decimal), // SqlDbType.Decimal
        typeof(double), // SqlDbType.Float
        typeof(byte[]), // SqlDbType.Image
        typeof(int), // SqlDbType.Int
        typeof(decimal), // SqlDbType.Money
        typeof(string), // SqlDbType.NChar
        typeof(string), // SqlDbType.NText
        typeof(string), // SqlDbType.NVarChar
        typeof(float), // SqlDbType.Real
        typeof(Guid), // SqlDbType.UniqueIdentifier
        typeof(DateTime), // SqlDbType.SmallDateTime
        typeof(short), // SqlDbType.SmallInt
        typeof(decimal), // SqlDbType.SmallMoney
        typeof(string), // SqlDbType.Text
        typeof(byte[]), // SqlDbType.Timestamp
        typeof(byte), // SqlDbType.TinyInt
        typeof(byte[]), // SqlDbType.VarBinary
        typeof(string), // SqlDbType.VarChar
        typeof(object), // SqlDbType.Variant
        null,
        typeof(string), // SqlDbType.Xml
        null, null, null,
        typeof(object), // SqlDbType.Udt
        typeof(object), // SqlDbType.Structured
        typeof(DateTime), // SqlDbType.Date
        typeof(TimeSpan), // SqlDbType.Time
        typeof(DateTime), // SqlDbType.DateTime2
        typeof(DateTimeOffset), // SqlDbType.DateTimeOffset
    };

    /// <summary>
    /// Simple association from <see cref="SqlDbType"/> to a <see cref="Type"/>.
    /// </summary>
    /// <param name="tSql">Sql type.</param>
    /// <returns>.net type to consider.</returns>
    static public Type? FromSqlDbTypeToNetType( SqlDbType tSql )
    {
        Debug.Assert( _typesMap.Length == 35 );
        return _typesMap[(int)tSql];
    }

    /// <summary>
    /// Gets whether a Type is associated to a <see cref="SqlDbType"/>.
    /// </summary>
    /// <param name="t">The type to search.</param>
    /// <returns>True if the type has a mapping to a Sql type./></returns>
    static public bool HasDirectMapping( Type t )
    {
        return _typesMap.Any( m => m == t );
    }

    /// <summary>
    /// Gets the string returned by <see cref="SqlValue"/> whenever a conversion failed.
    /// </summary>
    static public readonly string SqlValueError = "<Unable to convert as string>";

    /// <summary>
    /// Express a value of a given <see cref="SqlDbType"/> into a syntactically compatible string. 
    /// </summary>
    /// <param name="v">Object for which a string representation must be obtained.</param>
    /// <param name="dbType">Sql type.</param>
    /// <param name="throwError">True to throw exception on error.</param>
    /// <returns>
    /// A sql string that represents the value or <see cref="SqlValueError"/> ("&lt;Unable to convert as string&gt;")
    /// on error when <paramref name="throwError"/> is false.
    /// </returns>
    static public string SqlValue( object v, SqlDbType dbType, bool throwError = false )
    {
        if( v == null || v == DBNull.Value ) return "null";
        try
        {
            switch( dbType )
            {
                case SqlDbType.NVarChar: return String.Format( "N'{0}'", SqlEncodeStringContent( Convert.ToString( v, CultureInfo.InvariantCulture )! ) );
                case SqlDbType.Int: return Convert.ToString( v, CultureInfo.InvariantCulture )!;
                case SqlDbType.Bit: return Convert.ToBoolean( v ) ? "1" : "0";
                case SqlDbType.Char: goto case SqlDbType.VarChar;
                case SqlDbType.VarChar: return String.Format( "'{0}'", SqlEncodeStringContent( Convert.ToString( v, CultureInfo.InvariantCulture )! ) );
                case SqlDbType.NChar: goto case SqlDbType.NVarChar;
                case SqlDbType.DateTime: return String.Format( "convert( DateTime, '{0:s}', 126 )", v );
                case SqlDbType.DateTime2: return String.Format( "'{0:O}'", v );
                case SqlDbType.Time: return String.Format( "'{0:c}'", v );
                case SqlDbType.TinyInt: return Convert.ToString( v, CultureInfo.InvariantCulture )!;
                case SqlDbType.UniqueIdentifier: return ((Guid)v).ToString( "B" );
                case SqlDbType.SmallInt: return Convert.ToString( v, CultureInfo.InvariantCulture )!;
                case SqlDbType.SmallDateTime: return String.Format( "convert( SmallDateTime, '{0:s}', 126 )", v );
                case SqlDbType.BigInt: return Convert.ToString( v, CultureInfo.InvariantCulture )!;
                case SqlDbType.NText: goto case SqlDbType.NVarChar;
                case SqlDbType.Float: return Convert.ToString( v, CultureInfo.InvariantCulture )!;
                case SqlDbType.Real: return Convert.ToString( v, CultureInfo.InvariantCulture )!;
                case SqlDbType.Money: return Convert.ToString( v, CultureInfo.InvariantCulture )!;
                case SqlDbType.Decimal: return Convert.ToString( v, CultureInfo.InvariantCulture )!;
                case SqlDbType.Xml: return string.Format( "cast( '{0}' as xml )", SqlEncodeStringContent( Convert.ToString( v, CultureInfo.InvariantCulture )! ) );
                case SqlDbType.Structured: return Convert.ToString( v, CultureInfo.InvariantCulture )!;
                case SqlDbType.Binary:
                case SqlDbType.VarBinary:
                {
                    if( v is not byte[] bytes )
                    {
                        if( throwError ) throw new Exception( $"Unable to convert '{v.GetType()}' to byte[] to compute sql string representation for {dbType}." );
                        return SqlValueError;
                    }
                    StringBuilder b = new StringBuilder( "0x" );
                    for( int i = 0; i < bytes.Length; i++ )
                    {
                        const string c = "0123456789ABCDEF";
                        b.Append( c[bytes[i] >> 4] );
                        b.Append( c[bytes[i] & 0x0F] );
                    }
                    return b.ToString();
                }
                default:
                {
                    if( throwError ) throw new Exception( $"No sql string representation for: {dbType}" );
                    return SqlValueError;
                }
            }
        }
        catch( Exception )
        {
            if( throwError ) throw;
            return SqlValueError;
        }
    }


    /// <summary>
    /// Determines whether a <see cref="SqlDbType"/> usually requires a length. 
    /// </summary>
    /// <param name="dbType">The type to challenge.</param>
    /// <returns>True for <see cref="SqlDbType.NVarChar"/>, <see cref="SqlDbType.NChar"/>, 
    /// <see cref="SqlDbType.VarChar"/>, <see cref="SqlDbType.Char"/>, <see cref="SqlDbType.Binary"/>
    /// and <see cref="SqlDbType.VarBinary"/>. False otherwise.</returns>
    /// <remarks>
    /// Theoretically these types can be used with a default length of 1, 
    /// but we choose to consider them to always be used as 'variable length array' types.
    /// </remarks>
    static public bool IsArrayType( SqlDbType dbType )
    {
        return dbType switch
        {
            SqlDbType.NVarChar => true,
            SqlDbType.VarChar => true,
            SqlDbType.Char => true,
            SqlDbType.NChar => true,
            SqlDbType.Binary => true,
            SqlDbType.VarBinary => true,
            _ => false,
        };
    }

    /// <summary>
    /// Provides a correct string content by replacing ' with ''.
    /// This does not enclose the result by surrounding quotes: this has to be done at the caller level.
    /// </summary>
    /// <param name="s">The starting string.</param>
    /// <returns>An encoded string.</returns>
    static public string SqlEncodeStringContent( string s )
    {
        return s == null ? string.Empty : s.Replace( "'", "''" );
    }

    /// <summary>
    /// Protects pattern meta character of Sql Server: <c>[</c>, <c>_</c> and <c>%</c> by 
    /// appropriates encoding. Then, if <paramref name="expandWildCards"/> is true, 
    /// expands <c>*</c> and <c>?</c> by appropriate pattern markers.
    /// </summary>
    /// <param name="s">The starting string.</param>
    /// <param name="expandWildCards">True if the pattern contains * and ? that must be expanded.. See remarks.</param>
    /// <param name="innerPattern">True to ensure that the pattern starts and ends with a %. See remarks.</param>
    /// <returns>An encoded string.</returns>
    /// <remarks>
    /// When <paramref name="expandWildCards"/> is true, use \* for a real *, \? for a 
    /// real ?. \ can be used directly except when directly followed by *, ? or another \: it must then be duplicated.<br/>
    /// When <paramref name="innerPattern"/> is true, an empty or null string is returned as '%'.
    /// </remarks>
    static public string SqlEncodePattern( string s, bool expandWildCards, bool innerPattern )
    {
        if( s == null || s.Length == 0 ) return innerPattern ? "%" : String.Empty;
        StringBuilder b = new StringBuilder( s );
        b.Replace( "'", "''" );
        b.Replace( "[", "[[]" );
        b.Replace( "_", "[_]" );
        b.Replace( "%", "[%]" );
        if( expandWildCards )
        {
            b.Replace( @"\\", "\x0" );
            b.Replace( @"\*", "\x1" );
            b.Replace( @"\?", "\x2" );
            b.Replace( '*', '%' );
            b.Replace( '?', '_' );
            b.Replace( '\x0', '\\' );
            b.Replace( '\x1', '*' );
            b.Replace( '\x2', '?' );
        }
        if( innerPattern )
        {
            if( b[0] != '%' ) b.Insert( 0, '%' );
            if( b.Length > 1 && b[b.Length - 1] != '%' ) b.Append( '%' );
        }
        return b.ToString();
    }

    /// <summary>
    /// Splits a script that may contain 'GO' separators (that must be alone of their line).
    /// </summary>
    /// <param name="script">Script to split.</param>
    /// <returns>Zero (if the <paramref name="script"/> was null empty) or more scripts.</returns>
    /// <remarks>
    /// The 'GO' may be lowercase but must always be alone on its line.
    /// </remarks>
    static public IEnumerable<string> SplitGoSeparator( string script )
    {
        if( !string.IsNullOrWhiteSpace( script ) )
        {
            int curBeg = 0;
            for( Match goDelim = _rGo.Match( script ); goDelim.Success; goDelim = goDelim.NextMatch() )
            {
                int lenScript = goDelim.Index - curBeg;
                if( lenScript > 0 )
                {
                    yield return script.Substring( curBeg, lenScript );
                }
                curBeg = goDelim.Index + goDelim.Length;
            }
            if( script.Length > curBeg )
            {
                yield return script.Substring( curBeg ).TrimEnd();
            }
        }
    }
}

using NUnit.Framework;
using System;
using System.Data;

namespace CK.SqlServer.Tests
{
    [TestFixture]
    public class SqlHelperTests
    {
        [TestCase( 1, SqlDbType.Int, "1" )]
        [TestCase( "a\'b", SqlDbType.NVarChar, "N'a''b'" )]
        [TestCase( null, SqlDbType.NVarChar, "null" )]
        [TestCase( 0.0, SqlDbType.Float, "0" )]
        [TestCase( 0.012, SqlDbType.Float, "0.012" )]
        [TestCase( "special:DBNull.Value", SqlDbType.NVarChar, "null" )]
        [TestCase( "special:DateTime", SqlDbType.DateTime, "convert( DateTime, '2016-11-05T20:00:43', 126 )" )]
        [TestCase( "special:DateTime", SqlDbType.DateTime2, "'2016-11-05T20:00:43.0000000'" )]
        [TestCase( "special:Guid", SqlDbType.UniqueIdentifier, "{63f7ff58-3101-4099-a18f-6d749b1748c8}" )]
        [TestCase( new byte[] { }, SqlDbType.VarBinary, "0x" )]
        [TestCase( new byte[] { 16 }, SqlDbType.VarBinary, "0x10" )]
        [TestCase( new byte[] { 0x10, 0xFF, 0x01 }, SqlDbType.VarBinary, "0x10FF01" )]
        public void SqlHelper_SqlValue_works( object value, SqlDbType dbType, string result )
        {
            Guid g = new Guid( "63F7FF58-3101-4099-A18F-6D749B1748C8" );
            string s = value as string;
            if( s != null )
            {
                if( s == "special:DBNull.Value" ) value = DBNull.Value;
                if( s == "special:DateTime" ) value = new DateTime( 2016, 11, 5, 20, 0, 43 );
                if( s == "special:Guid" ) value = g;
            }
            Assert.That( SqlHelper.SqlValue( value, dbType ), Is.EqualTo( result ) );
        }
    }
}

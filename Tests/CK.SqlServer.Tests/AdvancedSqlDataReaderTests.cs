using FluentAssertions;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static CK.Testing.SqlServerTestHelper;

namespace CK.SqlServer.Tests
{
    [TestFixture]
    public class AdvancedSqlDataReaderTests
    {
        [SetUp]
        public void EnsureDatabase() => TestHelper.EnsureDatabase();

        [Test]
        public void SqlDataReader_and_AdvancedSqlDataReader_work_the_same()
        {
            const string sql = "select 1, 2, 3; select A = 1, B = 2, C = 3; select 'never' where 1 = 0; select Here = 'Something' union all select 'exists';";

            using( var con = TestHelper.CreateOpenedConnection() )
            using( var cmd = new SqlCommand( sql, con ) )
            {
                using( var r = cmd.ExecuteReader() )
                {
                    r.HasRows.Should().BeTrue();
                    r.Invoking( _ => _.GetInt32(0) ).Should().Throw<InvalidOperationException>();
                    r.Read().Should().BeTrue();
                    r.GetInt32( 0 ).Should().Be( 1 );
                    r.GetName( 0 ).Should().Be( String.Empty );
                    r.GetInt32( 1 ).Should().Be( 2 );
                    r.GetName( 1 ).Should().Be( String.Empty );
                    r.Invoking( _ => _.GetInt32( 3 ) ).Should().Throw<IndexOutOfRangeException>();
                    r.Invoking( _ => _.GetName( 3 ) ).Should().Throw<IndexOutOfRangeException>();
                    r.HasRows.Should().BeTrue();
                    r.Read().Should().BeFalse();
                    r.Invoking( _ => _.GetInt32(0) ).Should().Throw<InvalidOperationException>();
                    r.Read().Should().BeFalse();
                    r.NextResult().Should().BeTrue();
                    r.HasRows.Should().BeTrue();
                    r.Invoking( _ => _.GetInt32(0) ).Should().Throw<InvalidOperationException>();
                    r.Read().Should().BeTrue();
                    r.HasRows.Should().BeTrue();
                    r.GetInt32( 0 ).Should().Be( 1 );
                    r.GetName( 0 ).Should().Be( "A" );
                    r.GetInt32( 1 ).Should().Be( 2 );
                    r.GetName( 1 ).Should().Be( "B" );
                    r.GetInt32( 2 ).Should().Be( 3 );
                    r.GetName( 2 ).Should().Be( "C" );
                    r.Read().Should().BeFalse();
                    r.Invoking( _ => _.GetInt32(0) ).Should().Throw<InvalidOperationException>();
                    r.Read().Should().BeFalse();
                    r.NextResult().Should().BeTrue();
                    r.HasRows.Should().BeFalse();
                    r.Read().Should().BeFalse();
                    r.NextResult().Should().BeTrue();
                    r.HasRows.Should().BeTrue();
                    r.Read().Should().BeTrue();
                    r.GetName( 0 ).Should().Be( "Here" );
                    r.GetString( 0 ).Should().Be( "Something" );
                    r.Read().Should().BeTrue();
                    r.GetString( 0 ).Should().Be( "exists" );
                    r.Read().Should().BeFalse();
                    r.NextResult().Should().BeFalse();
                    r.Read().Should().BeFalse();
                    r.NextResult().Should().BeFalse();
                }
                using( var r = new AdvancedSqlDataReader( cmd.ExecuteReader() ) )
                {
                    r.HasRows.Should().BeTrue();
                    r.Row.Should().BeNull();
                    r.Read().Should().BeTrue();
                    r.Row.GetInt32( 0 ).Should().Be( 1 );
                    r.Row.GetName( 0 ).Should().Be( String.Empty );
                    r.Row.GetInt32( 1 ).Should().Be( 2 );
                    r.Row.GetName( 1 ).Should().Be( String.Empty );
                    r.Row.Invoking( _ => _.GetInt32( 3 ) ).Should().Throw<IndexOutOfRangeException>();
                    r.Row.Invoking( _ => _.GetName( 3 ) ).Should().Throw<IndexOutOfRangeException>();
                    r.HasRows.Should().BeTrue();
                    r.Read().Should().BeFalse();
                    r.Row.Should().BeNull();
                    r.Read().Should().BeFalse();
                    r.NextResult().Should().BeTrue();
                    r.HasRows.Should().BeTrue();
                    r.Row.Should().BeNull();
                    r.Read().Should().BeTrue();
                    r.HasRows.Should().BeTrue();
                    r.Row.GetInt32( 0 ).Should().Be( 1 );
                    r.Row.GetName( 0 ).Should().Be( "A" );
                    r.Row.GetInt32( 1 ).Should().Be( 2 );
                    r.Row.GetName( 1 ).Should().Be( "B" );
                    r.Row.GetInt32( 2 ).Should().Be( 3 );
                    r.Row.GetName( 2 ).Should().Be( "C" );
                    r.Read().Should().BeFalse();
                    r.Row.Should().BeNull();
                    r.Read().Should().BeFalse();
                    r.NextResult().Should().BeTrue();
                    r.HasRows.Should().BeFalse();
                    r.Read().Should().BeFalse();
                    r.NextResult().Should().BeTrue();
                    r.HasRows.Should().BeTrue();
                    r.Read().Should().BeTrue();
                    r.Row.GetName( 0 ).Should().Be( "Here" );
                    r.Row.GetString( 0 ).Should().Be( "Something" );
                    r.Read().Should().BeTrue();
                    r.Row.GetString( 0 ).Should().Be( "exists" );
                    r.Read().Should().BeFalse();
                    r.NextResult().Should().BeFalse();
                    r.Read().Should().BeFalse();
                    r.NextResult().Should().BeFalse();
                }
            }

        }
    }
}

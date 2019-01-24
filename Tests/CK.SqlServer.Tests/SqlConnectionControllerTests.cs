using FluentAssertions;
using NUnit.Framework;
using System;
using System.Data;
using System.Data.Common;
using System.Data.SqlClient;
using System.Threading.Tasks;
using static CK.Testing.SqlServerTestHelper;

namespace CK.SqlServer.Tests
{
    [TestFixture]
    public class SqlConnectionControllerTests
    {
        [SetUp]
        public void EnsureDatabase()
        {
            TestHelper.EnsureDatabase();
            TestHelper.ExecuteScripts( "if not exists(select 1 from sys.schemas where name = 'CK') exec('create schema CK');" );
        }

        [Test]
        public void ExplicitOpen_and_Dispose_are_order_independent()
        {
            using( var ctx = new SqlStandardCallContext( TestHelper.Monitor ) )
            {
                ISqlConnectionController c = ctx[TestHelper.GetConnectionString()];
                c.Connection.State.Should().Be( ConnectionState.Closed );
                var d1 = c.ExplicitOpen();
                c.Connection.State.Should().Be( ConnectionState.Open );
                var d2 = c.ExplicitOpen();
                var d3 = c.ExplicitOpen();
                d1.Should().NotBeNull();
                d2.Should().NotBeNull();
                d3.Should().NotBeNull();
                c.Connection.State.Should().Be( ConnectionState.Open );
                d1.Dispose();
                d1.Dispose();
                d2.Dispose();
                c.Connection.State.Should().Be( ConnectionState.Open );
                d3.Dispose();
                c.Connection.State.Should().Be( ConnectionState.Closed );
            }
        }

        [Test]
        public async Task ExplicitOpenAsync_and_Dispose_are_order_independent()
        {
            using( var ctx = new SqlStandardCallContext( TestHelper.Monitor ) )
            {
                ISqlConnectionController c = ctx[TestHelper.GetConnectionString()];
                c.Connection.State.Should().Be( ConnectionState.Closed );
                var d1 = await c.ExplicitOpenAsync();
                c.Connection.State.Should().Be( ConnectionState.Open );
                var d2 = await c.ExplicitOpenAsync();
                var d3 = await c.ExplicitOpenAsync();
                d1.Should().NotBeNull();
                d2.Should().NotBeNull();
                d3.Should().NotBeNull();
                c.Connection.State.Should().Be( ConnectionState.Open );
                d1.Dispose();
                d1.Dispose();
                d2.Dispose();
                c.Connection.State.Should().Be( ConnectionState.Open );
                d3.Dispose();
                c.Connection.State.Should().Be( ConnectionState.Closed );
            }
        }

        [Test]
        public void Using_the_GetDbConnetion_connection_is_implicitly_opened_and_closed()
        {
            using( var ctx = new SqlStandardCallContext( TestHelper.Monitor ) )
            {
                ISqlConnectionController c = ctx[TestHelper.GetConnectionString()];
                c.GetDbConnection().Invoking( dbCon => dbCon.Open() ).Should().NotThrow();
                c.Connection.State.Should().Be( ConnectionState.Open );
                c.GetDbConnection().Invoking( dbCon => dbCon.Close() ).Should().NotThrow();
                c.Connection.State.Should().Be( ConnectionState.Closed );
            }
        }

        [Test]
        public void Using_the_GetDbConnetion_to_issue_a_command_automatically_opens_the_connection()
        {
            void DoSomething( DbConnection con )
            {
                bool wasClosed = con.State == ConnectionState.Closed;
                if( wasClosed ) con.Open();
                var cmd = con.CreateCommand();
                cmd.CommandText = "select 1;";
                cmd.ExecuteScalar().Should().Be( 1 );
                if( wasClosed ) con.Close();
            }

            using( var ctx = new SqlStandardCallContext( TestHelper.Monitor ) )
            {
                ISqlConnectionController c = ctx[TestHelper.GetConnectionString()];

                // Closed.
                DoSomething( c.GetDbConnection() );

                using( c.ExplicitOpen() )
                {
                    DoSomething( c.GetDbConnection() );
                }
            }
        }

        [Test]
        public void Directly_opening_a_connection_throws_but_SqlStandardCallContext_Dispose_cleans_up()
        {
            SqlConnection directRef;
            using( var ctx = new SqlStandardCallContext( TestHelper.Monitor ) )
            {
                ISqlConnectionController c = ctx[TestHelper.GetConnectionString()];
                directRef = c.Connection;
                c.Connection.Awaiting( async oCon => await oCon.OpenAsync() )
                    .Should().Throw<InvalidOperationException>().WithMessage( "*ISqlConnectionController*" );
                c.Connection.State.Should().Be( ConnectionState.Open, "Unfortunately, the connection has been opened." );
                c.Connection.Invoking( oCon => oCon.Close() )
                    .Should().Throw<InvalidOperationException>().WithMessage( "*ISqlConnectionController*" );
                // Open it again (ignoring the exception).
                c.Connection.Awaiting( async oCon => await oCon.OpenAsync() )
                    .Should().Throw<InvalidOperationException>().WithMessage( "*ISqlConnectionController*" );
                c.Connection.State.Should().Be( ConnectionState.Open );
            }
            directRef.State.Should().Be( ConnectionState.Closed, "Our Dispose does the job of at leas cleaning all..." );
        }
    }
}

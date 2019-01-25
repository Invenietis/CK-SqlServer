using CK.Core;
using FluentAssertions;
using NUnit.Framework;
using System;
using System.Data;
using System.Data.Common;
using System.Data.SqlClient;
using System.Threading;
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
        public void Direct_open_close_of_the_connection_is_possible()
        {
            void DoSomething( SqlConnection con )
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
                DoSomething( c.Connection );
                c.Connection.State.Should().Be( ConnectionState.Closed );

                using( c.ExplicitOpen() )
                {
                    c.Connection.State.Should().Be( ConnectionState.Open );
                    DoSomething( c.Connection );
                    c.Connection.State.Should().Be( ConnectionState.Open );
                }
                c.Connection.State.Should().Be( ConnectionState.Closed );
            }
        }

        [Test]
        public void Directly_opening_and_closing_connection_async()
        {
            SqlConnection directRef;
            using( var ctx = new SqlStandardCallContext( TestHelper.Monitor ) )
            {
                ISqlConnectionController c = ctx[TestHelper.GetConnectionString()];
                directRef = c.Connection;
                c.Connection.Awaiting( async oCon => await oCon.OpenAsync() ).Should().NotThrow();
                c.Connection.State.Should().Be( ConnectionState.Open );
                c.Connection.Invoking( oCon => oCon.Close() ).Should().NotThrow();
                c.Connection.Awaiting( async oCon => await oCon.OpenAsync() ).Should().NotThrow();
                c.Connection.State.Should().Be( ConnectionState.Open );
            }
            directRef.State.Should().Be( ConnectionState.Closed );
        }

        class ExternalExecutor : ISqlCommandExecutor
        {
            public T ExecuteQuery<T>( IActivityMonitor monitor, SqlConnection connection, SqlTransaction transaction, SqlCommand cmd, Func<SqlCommand, T> innerExecutor )
            {
                monitor.Should().BeSameAs( TestHelper.Monitor );
                connection.Should().NotBeNull();
                transaction.Should().BeNull( "We don't have transaction here." );
                cmd.CommandText.Should().Be( "some text" );
                return default( T );
            }

            public Task<T> ExecuteQueryAsync<T>( IActivityMonitor monitor, SqlConnection connection, SqlTransaction transaction, SqlCommand cmd, Func<SqlCommand, CancellationToken, Task<T>> innerExecutor, CancellationToken cancellationToken = default( CancellationToken ) )
            {
                monitor.Should().BeSameAs( TestHelper.Monitor );
                connection.Should().NotBeNull();
                transaction.Should().BeNull( "We don't have transaction here." );
                cmd.CommandText.Should().Be( "some text" );
                return Task.FromResult( default( T ) );
            }
        }

        [Test]
        public void external_executor_receives_correctly_configured_command_and_opened_connection()
        {
            using( var ctx = new SqlStandardCallContext( TestHelper.Monitor, new ExternalExecutor() ) )
            {
                ctx[TestHelper.MasterConnectionString].ExecuteScalar( new SqlCommand( "some text" ) )
                    .Should().Be( null );
            }
        }
    }
}

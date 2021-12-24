using CK.Core;
using FluentAssertions;
using NUnit.Framework;
using System;
using System.Data;
using System.Data.Common;
using Microsoft.Data.SqlClient;
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
        public async Task ExplicitOpenAsync_and_Dispose_are_order_independent_Async()
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
            static void DoSomething( SqlConnection con )
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
        public async Task Directly_opening_and_closing_connection_Async()
        {
            SqlConnection directRef;
            using( var ctx = new SqlStandardCallContext( TestHelper.Monitor ) )
            {
                ISqlConnectionController c = ctx[TestHelper.GetConnectionString()];
                directRef = c.Connection;
                await c.Connection.Awaiting( oCon => oCon.OpenAsync() ).Should().NotThrowAsync();
                c.Connection.State.Should().Be( ConnectionState.Open );
                c.Connection.Invoking( oCon => oCon.Close() ).Should().NotThrow();
                await c.Connection.Awaiting( oCon => oCon.OpenAsync() ).Should().NotThrowAsync();
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
                return default;
            }

            public Task<T> ExecuteQueryAsync<T>( IActivityMonitor monitor,
                                                 SqlConnection connection,
                                                 SqlTransaction transaction,
                                                 SqlCommand cmd,
                                                 Func<SqlCommand, CancellationToken, Task<T>> innerExecutor,
                                                 CancellationToken cancellationToken = default )
            {
                monitor.Should().BeSameAs( TestHelper.Monitor );
                connection.Should().NotBeNull();
                transaction.Should().BeNull( "We don't have transaction here." );
                cmd.CommandText.Should().Be( "some text" );
                return Task.FromResult<T>( default );
            }
        }

        [Test]
        public async Task external_executor_receives_correctly_configured_command_and_opened_connection_Async()
        {
            using( var ctx = new SqlStandardCallContext( TestHelper.Monitor, new ExternalExecutor() ) )
            {
                object o = await ctx[TestHelper.MasterConnectionString].ExecuteScalarAsync( new SqlCommand( "some text" ) );
                o.Should().Be( null );
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

        [Test]
        public Task exec_SqlCommand_throws_a_SqlDetailedException_when_a_SqlException_is_thrown_Async()
        {
            return CallCatchAsync<SqlDetailedException>( "select * from kexistepas;" );
        }

        [Test]
        public void exec_SqlCommand_throws_a_SqlDetailedException_when_a_SqlException_is_thrown()
        {
            SyncCallCatch<SqlDetailedException>( "select * from kexistepas;" );
        }

        [Test]
        public Task exec_throws_SqlDetailedException_when_database_does_not_exist_Async()
        {
            return CallCatchAsync<SqlDetailedException>( "select 1;", TestHelper.GetConnectionString( "kexistepas-db" ) );
        }

        [Test]
        public void exec_throws_SqlDetailedException_when_database_does_not_exist()
        {
            SyncCallCatch<SqlDetailedException>( "select 1;", TestHelper.GetConnectionString( "kexistepas-db" ) );
        }

        [Test]
        [Explicit( "When trying to resolve a bad server name it takes a loooooooong time." )]
        public async Task exec_throws_SqlDetailedException_when_server_does_not_exist_Async()
        {
           Assume.That( TestHelper.IsExplicitAllowed, "Press Ctrl key to allow this test to run." );
           await CallCatchAsync<SqlDetailedException>( "select 1;", "Server=serverOfNothing;Database=ThisIsNotADatabase;Integrated Security=SSPI" );
        }

        static async Task CallCatchAsync<TException>( string cmd, string connectionString = null ) where TException : Exception
        {
            using( IDisposableSqlCallContext c = new SqlStandardCallContext( TestHelper.Monitor ) )
            using( var command = new SqlCommand( cmd ) )
            {
                ISqlConnectionController con = c[connectionString ?? TestHelper.GetConnectionString()];
                try
                {
                    // If the asynchronous process is lost (if the exception is not correctly managed),
                    // this test will fail with a task Canceled exception after:
                    // - 30 second when testing for connection string.... because when trying to resolve a bad server name it takes a loooooooong time.
                    // - 1 second in other cases.
                    CancellationTokenSource source = new CancellationTokenSource();
                    source.CancelAfter( connectionString == null ? 1000 : 30 * 1000 );
                    await con.ExecuteNonQueryAsync( command, source.Token );
                    Assert.Fail( $"Should have raised {typeof(TException).Name}." );
                }
                catch( TException ex )
                {
                }
            }
        }

        static void SyncCallCatch<TException>( string cmd, string connectionString = null )
        {
            using( IDisposableSqlCallContext c = new SqlStandardCallContext( TestHelper.Monitor ) )
            using( var command = new SqlCommand( cmd ) )
            {
                ISqlConnectionController con = c[connectionString ?? TestHelper.GetConnectionString()];
                try
                {
                    con.ExecuteNonQuery( command );
                    Assert.Fail( $"Should have raised {typeof(TException).Name}." );
                }
                catch( Exception ex )
                {
                    Assert.That( ex, Is.InstanceOf<TException>() );
                }
            }
        }

    }
}

using FluentAssertions;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Data;
using Microsoft.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static CK.Testing.SqlServerTestHelper;

namespace CK.SqlServer.Transaction.Tests;

[TestFixture]
public class SqlTransactionCallContextTests
{
    [SetUp]
    public void EnsureDatabase()
    {
        TestHelper.EnsureDatabase();
    }

    [Test]
    public void transaction_controller_implicitly_opens_the_connection()
    {
        using( var ctx = new SqlTransactionCallContext( TestHelper.Monitor ) )
        {
            var controller = ctx.GetConnectionController( TestHelper.GetConnectionString() );
            BeginTranAndCommit( controller );
            controller.Connection.State.Should().Be( ConnectionState.Closed );

            // Explicit openening.
            using( controller.ExplicitOpen() )
            {
                BeginTranAndCommit( controller );
                controller.Connection.State.Should().Be( ConnectionState.Open );
            }
            controller.Connection.State.Should().Be( ConnectionState.Closed );
        }

        static void BeginTranAndCommit( ISqlConnectionTransactionController controller )
        {
            ISqlTransaction tran = controller.BeginTransaction();
            controller.Connection.State.Should().Be( ConnectionState.Open );
            tran.IsNested.Should().BeFalse();
            tran.Status.Should().Be( SqlTransactionStatus.Opened );
            tran.Commit();
            tran.Status.Should().Be( SqlTransactionStatus.Committed );
            tran.Invoking( t => t.Dispose() ).Should().NotThrow();
        }
    }

    [Test]
    public void basic_transaction_test()
    {
        ResetTranTestTable();
        using( var ctx = new SqlTransactionCallContext( TestHelper.Monitor ) )
        {
            var controller = ctx.GetConnectionController( TestHelper.GetConnectionString() );
            var messageId = DoCommitTest( controller, 0, 0 );
            messageId.Should().Be( 1, "One message has been created." );
            messageId = DoRollbackAllAndDisposeTest( controller, 0, messageId );
            messageId.Should().Be( 3, "Two messages have been created and canceled." );
        }
    }

    [Test]
    public void basic_nested_transaction_test_with_ExplicitOpen_and_continue_on_the_same_connection()
    {
        // create table dbo.tTranTest( Id int identity(1,1) primary key, Msg varchar(50) not null );
        ResetTranTestTable();
        using( var ctx = new SqlTransactionCallContext( TestHelper.Monitor ) )
        {
            var controller = ctx.GetConnectionController( TestHelper.GetConnectionString() );
            // The connection is maintained opened.
            controller.ExplicitOpen();
            DoCommitTest( controller, tranCount: 0, messageId: 0 ).Should().Be( 1, "messageId from 0 to 1." );
            using( var tran = controller.BeginTransaction() )
            {
                DoCommitTest( controller, 1, 1 ).Should().Be( 2, "messageId from 1 to 2." );
                DoCommitTest( controller, 1, 2 ).Should().Be( 3, "messageId from 2 to 3." );
                using( var tran2 = controller.BeginTransaction() )
                {
                    DoCommitTest( controller, 2, 3 ).Should().Be( 4, "messageId from 3 to 4." );
                    DoRollbackAllAndDisposeTest( controller, 2, 4 ).Should().Be( 6, "messageId from 4 to 6." );
                    controller.TransactionCount.Should().Be( 0 );
                    tran2.Status.Should().Be( SqlTransactionStatus.Rollbacked );
                }
                tran.Status.Should().Be( SqlTransactionStatus.Rollbacked );
            }
            controller.Connection.State.Should().Be( ConnectionState.Open );
            DoCommitTest( controller, tranCount: 0, messageId: 6 )
                .Should().Be( 7, "messageId from 6 to 7 (The identity is out of transaction!)." );
            ReadMessage( controller, 1 ).Should().NotBeNull( "Done before the transaction." );
            ReadMessage( controller, 2 ).Should().BeNull( "in rollbacked transaction." );
            ReadMessage( controller, 3 ).Should().BeNull();
            ReadMessage( controller, 4 ).Should().BeNull();
            ReadMessage( controller, 5 ).Should().BeNull();
            ReadMessage( controller, 6 ).Should().BeNull();
            ReadMessage( controller, 7 ).Should().NotBeNull( "Done after the transaction." );
        }
    }

    [Test]
    public void basic_nested_transaction_test_with_ExplicitOpen()
    {
        ResetTranTestTable();
        using( var ctx = new SqlTransactionCallContext( TestHelper.Monitor ) )
        {
            var controller = ctx.GetConnectionController( TestHelper.GetConnectionString() );
            var open1 = controller.ExplicitOpen();
            DoCommitTest( controller, 0, 0 ).Should().Be( 1, "messageId from 0 to 1." );
            using( var tran = controller.BeginTransaction() )
            {
                DoCommitTest( controller, 1, 1 ).Should().Be( 2, "messageId from 1 to 2." );
                // Since a transaction has been opened, we can dispose the explicit opening:
                // The SqlTransactionCallContext.BeginTransaction has implicitly opened the connection.
                open1.Dispose();
                DoCommitTest( controller, 1, 2 ).Should().Be( 3, "messageId from 2 to 3." );
                using( var tran2 = controller.BeginTransaction() )
                {
                    DoCommitTest( controller, 2, 3 ).Should().Be( 4, "messageId from 3 to 4." );
                    DoRollbackAllAndDisposeTest( controller, 2, 4 ).Should().Be( 6, "messageId from 4 to 6." );
                    controller.TransactionCount.Should().Be( 0 );
                    tran2.Status.Should().Be( SqlTransactionStatus.Rollbacked );
                }
                tran.Status.Should().Be( SqlTransactionStatus.Rollbacked );
            }
        }
    }

    [Test]
    public void basic_nested_transaction_test()
    {
        ResetTranTestTable();
        using( var ctx = new SqlTransactionCallContext( TestHelper.Monitor ) )
        {
            var controller = ctx.GetConnectionController( TestHelper.GetConnectionString() );
            DoCommitTest( controller, 0, 0 ).Should().Be( 1, "messageId from 0 to 1." );
            using( var tran = controller.BeginTransaction() )
            {
                DoCommitTest( controller, 1, 1 ).Should().Be( 2, "messageId from 1 to 2." );
                DoCommitTest( controller, 1, 2 ).Should().Be( 3, "messageId from 2 to 3." );
                using( var tran2 = controller.BeginTransaction() )
                {
                    DoCommitTest( controller, 2, 3 ).Should().Be( 4, "messageId from 3 to 4." );
                    DoRollbackAllAndDisposeTest( controller, 2, 4 ).Should().Be( 6, "messageId from 4 to 6." );
                    controller.TransactionCount.Should().Be( 0 );
                    tran2.Status.Should().Be( SqlTransactionStatus.Rollbacked );
                }
                tran.Status.Should().Be( SqlTransactionStatus.Rollbacked );
            }
        }
    }

    [Test]
    public void nested_transaction_with_levels()
    {
        ResetTranTestTable();
        using( var ctx = new SqlTransactionCallContext( TestHelper.Monitor ) )
        {
            var controller = ctx.GetConnectionController( TestHelper.GetConnectionString() );
            controller.Connection.State.Should().Be( ConnectionState.Closed );
            controller.GetCurrentIsolationLevel().Should().Be( IsolationLevel.Unspecified );
            using( var tran0 = controller.BeginTransaction() )
            {
                controller.GetCurrentIsolationLevel().Should().Be( IsolationLevel.ReadCommitted );
                DoCommitTest( controller, tranCount: 1, messageId: 0 ).Should().Be( 1, "messageId from 0 to 1." );
                using( var tran1 = controller.BeginTransaction( IsolationLevel.Serializable ) )
                {
                    controller.GetCurrentIsolationLevel().Should().Be( IsolationLevel.Serializable );
                    DoCommitTest( controller, tranCount: 2, messageId: 1 ).Should().Be( 2, "messageId from 1 to 2." );
                    controller.GetCurrentIsolationLevel().Should().Be( IsolationLevel.Serializable );

                    using( var tran2 = controller.BeginTransaction( IsolationLevel.ReadUncommitted ) )
                    {
                        DoCommitTest( controller, tranCount: 3, messageId: 2 ).Should().Be( 3, "messageId from 2 to 3." );
                        controller.GetCurrentIsolationLevel().Should().Be( IsolationLevel.ReadUncommitted );
                        DoRollbackAllAndDisposeTest( controller, tranCount: 3, messageId: 3 ).Should().Be( 5, "messageId from 3 to 5." );
                        controller.TransactionCount.Should().Be( 0 );
                        tran2.Status.Should().Be( SqlTransactionStatus.Rollbacked );
                    }
                    tran1.Status.Should().Be( SqlTransactionStatus.Rollbacked );
                }
                tran0.Status.Should().Be( SqlTransactionStatus.Rollbacked );
            }
        }
    }

    [Test]
    public async Task nested_transaction_with_levels_asynchronous_Async()
    {
        ResetTranTestTable();
        using( var ctx = new SqlTransactionCallContext( TestHelper.Monitor ) )
        {
            var controller = ctx.GetConnectionController( TestHelper.GetConnectionString() );
            controller.Connection.State.Should().Be( ConnectionState.Closed );
            controller.GetCurrentIsolationLevel().Should().Be( IsolationLevel.Unspecified );
            using( var tran0 = controller.BeginTransaction() )
            {
                controller.GetCurrentIsolationLevel().Should().Be( IsolationLevel.ReadCommitted );
                (await DoCommitTestAsync( controller, tranCount: 1, messageId: 0 )).Should().Be( 1, "messageId from 0 to 1." );
                using( var tran1 = controller.BeginTransaction( IsolationLevel.Serializable ) )
                {
                    controller.GetCurrentIsolationLevel().Should().Be( IsolationLevel.Serializable );
                    (await DoCommitTestAsync( controller, tranCount: 2, messageId: 1 )).Should().Be( 2, "messageId from 1 to 2." );
                    controller.GetCurrentIsolationLevel().Should().Be( IsolationLevel.Serializable );

                    using( var tran2 = controller.BeginTransaction( IsolationLevel.ReadUncommitted ) )
                    {
                        (await DoCommitTestAsync( controller, tranCount: 3, messageId: 2 )).Should().Be( 3, "messageId from 2 to 3." );
                        controller.GetCurrentIsolationLevel().Should().Be( IsolationLevel.ReadUncommitted );
                        (await DoRollbackAllAndDisposeTestAsync( controller, tranCount: 3, messageId: 3 )).Should().Be( 5, "messageId from 3 to 5." );
                        controller.TransactionCount.Should().Be( 0 );
                        tran2.Status.Should().Be( SqlTransactionStatus.Rollbacked );
                    }
                    tran1.Status.Should().Be( SqlTransactionStatus.Rollbacked );
                }
                tran0.Status.Should().Be( SqlTransactionStatus.Rollbacked );
            }
        }
    }


    [Test]
    public void default_isolation_level_ReadCommitted_is_restored()
    {
        ResetTranTestTable();
        using( var ctx = new SqlTransactionCallContext( TestHelper.Monitor ) )
        {
            var controller = ctx.GetConnectionController( TestHelper.GetConnectionString() );
            controller.Connection.State.Should().Be( ConnectionState.Closed );
            controller.GetCurrentIsolationLevel().Should().Be( IsolationLevel.Unspecified );
            using( controller.ExplicitOpen() )
            {
                controller.GetCurrentIsolationLevel().Should().Be( IsolationLevel.ReadCommitted );
                using( var tran = controller.BeginTransaction( IsolationLevel.Serializable ) )
                {
                    controller.GetCurrentIsolationLevel().Should().Be( IsolationLevel.Serializable );
                }
                controller.GetCurrentIsolationLevel().Should().Be( IsolationLevel.ReadCommitted );
            }
            controller.GetCurrentIsolationLevel().Should().Be( IsolationLevel.Unspecified );
        }
    }

    static int DoCommitTest( ISqlConnectionTransactionController controller, int tranCount, int messageId )
    {
        var message = Guid.NewGuid().ToString();
        using( var tran = controller.BeginTransaction() )
        {
            tran.Status.Should().Be( SqlTransactionStatus.Opened );
            controller.TransactionCount.Should().Be( tranCount + 1 );
            AddMessage( controller, message ).Should().Be( ++messageId );
            ReadMessage( controller, messageId ).Should().Be( message );
            tran.Commit();
            controller.TransactionCount.Should().Be( tranCount );
            tran.Status.Should().Be( SqlTransactionStatus.Committed );
        }
        ReadMessage( controller, messageId ).Should().Be( message );
        return messageId;
    }

    static async Task<int> DoCommitTestAsync( ISqlConnectionTransactionController controller, int tranCount, int messageId )
    {
        var message = Guid.NewGuid().ToString();
        using( var tran = controller.BeginTransaction() )
        {
            tran.Status.Should().Be( SqlTransactionStatus.Opened );
            controller.TransactionCount.Should().Be( tranCount + 1 );
            (await AddMessageAsync( controller, message )).Should().Be( ++messageId );
            (await ReadMessageAsync( controller, messageId )).Should().Be( message );
            tran.Commit();
            controller.TransactionCount.Should().Be( tranCount );
            tran.Status.Should().Be( SqlTransactionStatus.Committed );
        }
        (await ReadMessageAsync( controller, messageId )).Should().Be( message );
        return messageId;
    }

    static int DoRollbackAllAndDisposeTest( ISqlConnectionTransactionController controller, int tranCount, int messageId )
    {
        var message = Guid.NewGuid().ToString();
        using( var tran = controller.BeginTransaction() )
        {
            tran.Status.Should().Be( SqlTransactionStatus.Opened );
            controller.TransactionCount.Should().Be( tranCount + 1 );
            AddMessage( controller, message ).Should().Be( ++messageId );
            ReadMessage( controller, messageId ).Should().Be( message );
            tran.RollbackAll();
            controller.TransactionCount.Should().Be( 0 );
            tran.Status.Should().Be( SqlTransactionStatus.Rollbacked );
        }
        ReadMessage( controller, 2 ).Should().BeNull();
        message = Guid.NewGuid().ToString();
        using( var tran = controller.BeginTransaction() )
        {
            tran.Status.Should().Be( SqlTransactionStatus.Opened );
            controller.TransactionCount.Should().Be( 1 );
            AddMessage( controller, message ).Should().Be( ++messageId );
            ReadMessage( controller, messageId ).Should().Be( message );
        }
        controller.TransactionCount.Should().Be( 0 );
        ReadMessage( controller, messageId ).Should().BeNull();
        return messageId;
    }

    static async Task<int> DoRollbackAllAndDisposeTestAsync( ISqlConnectionTransactionController controller, int tranCount, int messageId )
    {
        var message = Guid.NewGuid().ToString();
        using( var tran = controller.BeginTransaction() )
        {
            tran.Status.Should().Be( SqlTransactionStatus.Opened );
            controller.TransactionCount.Should().Be( tranCount + 1 );
            (await AddMessageAsync( controller, message )).Should().Be( ++messageId );
            (await ReadMessageAsync( controller, messageId )).Should().Be( message );
            tran.RollbackAll();
            controller.TransactionCount.Should().Be( 0 );
            tran.Status.Should().Be( SqlTransactionStatus.Rollbacked );
        }
        (await ReadMessageAsync( controller, 2 )).Should().BeNull();
        message = Guid.NewGuid().ToString();
        using( var tran = controller.BeginTransaction() )
        {
            tran.Status.Should().Be( SqlTransactionStatus.Opened );
            controller.TransactionCount.Should().Be( 1 );
            (await AddMessageAsync( controller, message )).Should().Be( ++messageId );
            (await ReadMessageAsync( controller, messageId )).Should().Be( message );
        }
        controller.TransactionCount.Should().Be( 0 );
        (await ReadMessageAsync( controller, messageId )).Should().BeNull();
        return messageId;
    }


    static readonly SqlCommand _resetCommand;
    static readonly SqlCommand _addMessageCommand;
    static readonly SqlCommand _readMessageCommand;

    static SqlTransactionCallContextTests()
    {
        _resetCommand = new SqlCommand( $"if object_id('dbo.tTranTest') is not null drop table dbo.tTranTest; create table dbo.tTranTest( Id int identity(1,1) primary key, Msg varchar(50) not null );" );
        _addMessageCommand = new SqlCommand( $"insert into dbo.tTranTest( Msg ) values( @Msg ); select cast(SCOPE_IDENTITY() as int);" );
        _addMessageCommand.Parameters.Add( "@Msg", SqlDbType.VarChar, 50 );
        _readMessageCommand = new SqlCommand( "select Msg from dbo.tTranTest where Id = @Id" );
        _readMessageCommand.Parameters.Add( "@Id", SqlDbType.Int );
    }

    static void ResetTranTestTable()
    {
        using( var con = TestHelper.CreateOpenedConnection() )
        {
            _resetCommand.Connection = con;
            _resetCommand.ExecuteNonQuery();
            _resetCommand.Connection = null;
        }
    }

    static int AddMessage( ISqlConnectionController c, string msg )
    {
        _addMessageCommand.Parameters[0].Value = msg;
        return (int)c.ExecuteScalar( _addMessageCommand );
    }

    static async Task<int> AddMessageAsync( ISqlConnectionController c, string msg )
    {
        _addMessageCommand.Parameters[0].Value = msg;
        return (int)await c.ExecuteScalarAsync( _addMessageCommand );
    }

    static string ReadMessage( ISqlConnectionController c, int id )
    {
        _readMessageCommand.Parameters[0].Value = id;
        return (string)c.ExecuteScalar( _readMessageCommand );
    }
    static async Task<string> ReadMessageAsync( ISqlConnectionController c, int id )
    {
        _readMessageCommand.Parameters[0].Value = id;
        return (string)await c.ExecuteScalarAsync( _readMessageCommand );
    }


}

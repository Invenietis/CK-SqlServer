using FluentAssertions;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static CK.Testing.SqlServerTestHelper;

namespace CK.SqlServer.Transaction.Tests
{
    [TestFixture]
    public class SqlTransactionCallContextTests
    {
        [Test]
        public void transaction_controller_implicitly_opens_the_connection()
        {
            using( var ctx = new SqlTransactionCallContext( TestHelper.Monitor ) )
            {
                var controller = ctx.GetConnectionController( TestHelper.GetConnectionString() );
                BeginTranAndCommit( controller );
                controller.Connection.State.Should().Be( ConnectionState.Closed );

                // Explicit openening.
                controller.ExplicitOpen();
                BeginTranAndCommit( controller );
                controller.Connection.State.Should().Be( ConnectionState.Open );
                controller.ExplicitClose();
                controller.Connection.State.Should().Be( ConnectionState.Closed );
            }

            void BeginTranAndCommit( ISqlConnectionTransactionController controller )
            {
                controller.Connection.State.Should().Be( ConnectionState.Closed );
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
                using( var tran = controller.BeginTransaction() )
                {
                    tran.Status.Should().Be( SqlTransactionStatus.Opened );
                    controller.TransactionCount.Should().Be( 1 );
                    AddMessage( controller, "Yo" ).Should().Be( 1 );
                    ReadMessage( controller, 1 ).Should().Be( "Yo" );
                    tran.Commit();
                    controller.TransactionCount.Should().Be( 0 );
                    tran.Status.Should().Be( SqlTransactionStatus.Committed );
                }
                ReadMessage( controller, 1 ).Should().Be( "Yo" );
                using( var tran = controller.BeginTransaction() )
                {
                    tran.Status.Should().Be( SqlTransactionStatus.Opened );
                    controller.TransactionCount.Should().Be( 1 );
                    AddMessage( controller, "Ay" ).Should().Be( 2 );
                    ReadMessage( controller, 2 ).Should().Be( "Ay" );
                    tran.RollbackAll();
                    controller.TransactionCount.Should().Be( 0 );
                    tran.Status.Should().Be( SqlTransactionStatus.Rollbacked );
                }
                ReadMessage( controller, 2 ).Should().BeNull();
                using( var tran = controller.BeginTransaction() )
                {
                    tran.Status.Should().Be( SqlTransactionStatus.Opened );
                    controller.TransactionCount.Should().Be( 1 );
                    AddMessage( controller, "AyAy" ).Should().Be( 3 );
                    ReadMessage( controller, 3 ).Should().Be( "AyAy" );
                }
                controller.TransactionCount.Should().Be( 0 );
                ReadMessage( controller, 3 ).Should().BeNull();
            }
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
        void ResetTranTestTable()
        {
            using( var con = TestHelper.CreateOpenedConnection() )
            {
                _resetCommand.Connection = con;
                _resetCommand.ExecuteNonQuery();
                _resetCommand.Connection = null;
            }
        }

        int AddMessage( ISqlConnectionController c, string msg )
        {
            _addMessageCommand.Parameters[0].Value = msg;
            return (int)c.ExecuteScalar( _addMessageCommand );
        }

        string ReadMessage( ISqlConnectionController c, int id )
        {
            _readMessageCommand.Parameters[0].Value = id;
            return (string)c.ExecuteScalar( _readMessageCommand );
        }


    }
}

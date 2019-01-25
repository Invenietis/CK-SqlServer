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
    public class BasicTransactionTests
    {
        [Test]
        public void opening_twice_a_connection_is_an_error_but_it_can_be_closed_multiple_times()
        {
            using( var c = new SqlConnection( TestHelper.GetConnectionString() ) )
            {
                c.Invoking( _ => _.Open() ).Should().NotThrow();
                c.Invoking( _ => _.Open() ).Should().Throw<InvalidOperationException>();
                c.Invoking( _ => _.Close() ).Should().NotThrow();
                c.Invoking( _ => _.Close() ).Should().NotThrow();
            }
        }

        [Test]
        public void begin_transaction_on_closed_connection_is_an_invalid_operation()
        {
            using( var c = new SqlConnection( TestHelper.GetConnectionString() ) )
            {
                c.Invoking( _ => _.BeginTransaction() ).Should().Throw<InvalidOperationException>();
            }
        }

        [Test]
        [SetUICulture( "en-US" )]
        public void nested_transactions_are_not_supported()
        {
            TestHelper.EnsureDatabase();
            using( var c = TestHelper.CreateOpenedConnection() )
            {
                using( var t1 = c.BeginTransaction() )
                {
                    c.BeginTransaction();
                    c.Invoking( _ => _.BeginTransaction() ).Should().Throw<InvalidOperationException>();
                }
            }
        }

        [Test]
        [SetUICulture( "en-US" )]
        public void command_MUST_be_associated_to_the_connection_transaction()
        {
            using( var c = TestHelper.CreateOpenedConnection() )
            using( var cmd = new SqlCommand( "select 1;" ) )
            {
                var t = c.BeginTransaction();
                cmd.Connection = c;
                cmd.ExecuteScalar();
                cmd.Invoking( _ => _.ExecuteScalar() ).Should().Throw<InvalidOperationException>();
            }
        }

        [Test]
        [SetUICulture( "en-US" )]
        public void connection_and_transaction_must_exactly_match()
        {
            using( var c1 = TestHelper.CreateOpenedConnection() )
            using( var c2 = TestHelper.CreateOpenedConnection() )
            using( var cmd = new SqlCommand( "select 1;" ) )
            {
                var t1 = c1.BeginTransaction();
                cmd.Connection = c2;
                cmd.Transaction = t1;
                cmd.ExecuteScalar();
                cmd.Invoking( _ => _.ExecuteScalar() ).Should().Throw<InvalidOperationException>();
            }
        }

    }
}

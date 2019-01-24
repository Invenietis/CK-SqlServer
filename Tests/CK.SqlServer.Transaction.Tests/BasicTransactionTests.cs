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
        public void nested_transactions_are_not_supported()
        {
            TestHelper.EnsureDatabase();
            using( var c = TestHelper.CreateOpenedConnection() )
            {
                using( var t1 = c.BeginTransaction() )
                {
                    c.Invoking( _ => _.BeginTransaction() ).Should().Throw<InvalidOperationException>();
                }
            }
        }
    }
}

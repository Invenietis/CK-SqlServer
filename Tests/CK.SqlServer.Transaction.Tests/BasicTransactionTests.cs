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

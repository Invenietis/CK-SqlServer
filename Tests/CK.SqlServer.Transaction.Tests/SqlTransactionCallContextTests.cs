using FluentAssertions;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Data;
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
        public void basic_transaction_woeks()
        {
            using( var ctx = new SqlTransactionCallContext( TestHelper.Monitor ) )
            {
                var controller = ctx.GetConnectionController( TestHelper.GetConnectionString() );
                controller.Connection.State.Should().Be( ConnectionState.Closed );
            }
        }
    }
}

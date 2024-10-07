using FluentAssertions;
using NUnit.Framework;
using System;
using System.Data;
using Microsoft.Data.SqlClient;
using System.Threading.Tasks;
using static CK.Testing.SqlServerTestHelper;

namespace CK.SqlServer.Tests;

[TestFixture]
public class ISqlConnectionControllerExtensionTests
{
    [SetUp]
    public void EnsureDatabase()
    {
        TestHelper.EnsureDatabase();
        TestHelper.ExecuteScripts( "if not exists(select 1 from sys.schemas where name = 'CK') exec('create schema CK');" );
    }

    [Test]
    public void using_ISqlConnectionController_extension_methods()
    {
        string tableName = "CK.t" + Guid.NewGuid().ToString( "N" );
        var create = new SqlCommand( $"create table {tableName} ( id int, name varchar(10) ); insert into {tableName}(id,name) values (1,'One'), (2,'Two'), (3,'Three');" );
        var scalar = new SqlCommand( $"select name from {tableName} where id=@Id;" );
        scalar.Parameters.AddWithValue( "@Id", 3 );
        var row = new SqlCommand( $"select top 1 id, name from {tableName} order by id;" );
        var reader = new SqlCommand( $"select id, name from {tableName} order by id;" );
        var clean = new SqlCommand( $"drop table {tableName};" );

        using( var ctx = new SqlStandardCallContext( TestHelper.Monitor ) )
        {
            ISqlConnectionController c = ctx[TestHelper.GetConnectionString()];
            c.Connection.State.Should().Be( ConnectionState.Closed );

            c.ExecuteNonQuery( create );
            c.ExecuteScalar( scalar ).Should().Be( "Three" );
            var rowResult = c.ExecuteSingleRow( row, r => Tuple.Create( r.GetInt32( 0 ), r.GetString( 1 ) ) );
            rowResult.Item1.Should().Be( 1 );
            rowResult.Item2.Should().Be( "One" );
            var readerResult = c.ExecuteReader( reader, r => Tuple.Create( r.GetInt32( 0 ), r.GetString( 1 ) ) );
            readerResult.Should().HaveCount( 3 );
            readerResult[0].Item1.Should().Be( 1 );
            readerResult[1].Item2.Should().Be( "Two" );
            readerResult[2].Item2.Should().Be( "Three" );
            c.ExecuteNonQuery( clean );
        }
    }

    [Test]
    public async Task using_ISqlConnectionController_extension_methods_asynchronous_Async()
    {
        string tableName = "CK.t" + Guid.NewGuid().ToString( "N" );
        var create = new SqlCommand( $"create table {tableName} ( id int, name varchar(10) ); insert into {tableName}(id,name) values (1,'One'), (2,'Two'), (3,'Three');" );
        var scalar = new SqlCommand( $"select name from {tableName} where id=@Id;" );
        scalar.Parameters.AddWithValue( "@Id", 3 );
        var row = new SqlCommand( $"select top 1 id, name from {tableName} order by id;" );
        var reader = new SqlCommand( $"select id, name from {tableName} order by id;" );
        var clean = new SqlCommand( $"drop table {tableName};" );

        using( var ctx = new SqlStandardCallContext( TestHelper.Monitor ) )
        {
            ISqlConnectionController c = ctx[TestHelper.GetConnectionString()];
            c.Connection.State.Should().Be( ConnectionState.Closed );
            await c.ExecuteNonQueryAsync( create );
            (await c.ExecuteScalarAsync( scalar )).Should().Be( "Three" );
            var rowResult = await c.ExecuteSingleRowAsync( row, r => Tuple.Create( r.GetInt32( 0 ), r.GetString( 1 ) ) );
            rowResult.Item1.Should().Be( 1 );
            rowResult.Item2.Should().Be( "One" );
            var readerResult = await c.ExecuteReaderAsync( reader, r => Tuple.Create( r.GetInt32( 0 ), r.GetString( 1 ) ) );
            readerResult.Should().HaveCount( 3 );
            readerResult[0].Item1.Should().Be( 1 );
            readerResult[1].Item2.Should().Be( "Two" );
            readerResult[2].Item2.Should().Be( "Three" );
            await c.ExecuteNonQueryAsync( clean );
        }
    }

    [Test]
    public void using_ISqlConnectionController_extension_methods_thows_a_SqlDetailedException()
    {
        var bug = new SqlCommand( "bug" );
        using( var ctx = new SqlStandardCallContext( TestHelper.Monitor ) )
        {
            var c = ctx[TestHelper.MasterConnectionString];
            c.Invoking( co => co.ExecuteNonQuery( bug ) ).Should().Throw<SqlDetailedException>();
            c.Invoking( co => co.ExecuteScalar( bug ) ).Should().Throw<SqlDetailedException>();
            c.Invoking( co => co.ExecuteSingleRow( bug, r => 0 ) ).Should().Throw<SqlDetailedException>();
            c.Invoking( co => co.ExecuteReader( bug, r => 0 ) ).Should().Throw<SqlDetailedException>();
        }
    }

    [Test]
    public async Task using_ISqlConnectionController_extension_methods_async_thows_a_SqlDetailedException_Async()
    {
        var bug = new SqlCommand( "bug" );
        using( var ctx = new SqlStandardCallContext( TestHelper.Monitor ) )
        {
            var c = ctx[TestHelper.MasterConnectionString];
            await c.Awaiting( co => co.ExecuteNonQueryAsync( bug ) ).Should().ThrowAsync<SqlDetailedException>();
            await c.Awaiting( co => co.ExecuteScalarAsync( bug ) ).Should().ThrowAsync<SqlDetailedException>();
            await c.Awaiting( co => co.ExecuteSingleRowAsync( bug, r => 0 ) ).Should().ThrowAsync<SqlDetailedException>();
            await c.Awaiting( co => co.ExecuteReaderAsync( bug, r => 0 ) ).Should().ThrowAsync<SqlDetailedException>();
        }
    }


    [Test]
    public void reading_big_text_with_execute_scalar_fails_limit_is_2033_characters()
    {
        using( var ctx = new SqlStandardCallContext( TestHelper.Monitor ) )
        {
            var c = ctx[TestHelper.MasterConnectionString];

            string read;

            SqlCommand cFailXml = new SqlCommand( "select * from sys.objects for xml path" );
            read = (string)c.ExecuteScalar( cFailXml );
            read.Should().HaveLength( 2033, "2033 is the upper limit for ExecuteScalar." );

            SqlCommand cFailJson = new SqlCommand( "select * from sys.objects for json auto" );
            read = (string)c.ExecuteScalar( cFailJson );
            read.Should().HaveLength( 2033, "2033 is the upper limit for ExecuteScalar." );

            // Using convert works for Json and Xml.
            SqlCommand cConvJson = new SqlCommand( "select convert( nvarchar(max), (select * from sys.objects for json auto))" );
            string readJsonConvert = (string)c.ExecuteScalar( cConvJson );
            readJsonConvert.Length.Should().BeGreaterThan( 20 * 1024 );

            SqlCommand cConvXml = new SqlCommand( "select convert( nvarchar(max), (select * from sys.objects for xml path))" );
            string readXmlConvert = (string)c.ExecuteScalar( cConvXml );
            readXmlConvert.Length.Should().BeGreaterThan( 20 * 1024 );

            // Using the SqlDataReader works for Json and Xml.
            SqlCommand cReaderJson = new SqlCommand( "select 1, Json = (select * from sys.objects for json auto)" );
            string readJsonViaReader = c.ExecuteSingleRow( cReaderJson, r => r.GetString( 1 ) );
            readJsonViaReader.Length.Should().BeGreaterThan( 20 * 1024 );

            Assert.That( readJsonViaReader, Is.EqualTo( readJsonConvert ) );

            SqlCommand cReaderXml = new SqlCommand( "select Xml = (select * from sys.objects for xml path)" );
            string readXmlViaReader = c.ExecuteSingleRow( cReaderXml, r => r.GetString( 0 ) );
            readXmlViaReader.Length.Should().BeGreaterThan( 20 * 1024 );

            readXmlViaReader.Should().Be( readXmlConvert );
        }
    }

    [Test]
    public async Task ExecuteScalarAsync_works_on_closed_or_opened_connection_Async()
    {
        using( var cmd = new SqlCommand( "select count(*) from sys.objects" ) )
        using( var ctx = new SqlStandardCallContext() )
        {
            ISqlConnectionController c = ctx[TestHelper.GetConnectionString()];
            c.Connection.State.Should().Be( ConnectionState.Closed );
            using( c.ExplicitOpen() )
            {
                c.Connection.State.Should().Be( ConnectionState.Open );
                ((int)c.ExecuteScalar( cmd )).Should().BeGreaterThan( 0 );
            }
            c.Connection.State.Should().Be( ConnectionState.Closed );
            ((int)await c.ExecuteScalarAsync( cmd )).Should().BeGreaterThan( 0 );
            c.Connection.State.Should().Be( ConnectionState.Closed );
            cmd.CommandText = "select count(*) from sys.objects where name='no-object-here'";
            using( await c.ExplicitOpenAsync() )
            {
                c.Connection.State.Should().Be( ConnectionState.Open );
                ((int)await c.ExecuteScalarAsync( cmd )).Should().Be( 0 );
            }
            c.Connection.State.Should().Be( ConnectionState.Closed );
            ((int)await c.ExecuteScalarAsync( cmd )).Should().Be( 0 );
            c.Connection.State.Should().Be( ConnectionState.Closed );
            cmd.CommandText = "select name from sys.tables where name='no-object-here'";
            using( await c.ExplicitOpenAsync() )
            {
                (await c.ExecuteScalarAsync( cmd )).Should().BeNull();
            }
            c.Connection.State.Should().Be( ConnectionState.Closed );
        }
    }

    [TestCase( "OneRow" )]
    [TestCase( "NoRow" )]
    public async Task ExecuteSingleRowAsync_works_on_closed_or_opened_connection_Async( string mode )
    {
        using var cmd = new SqlCommand( $"select top({(mode == "OneRow" ? 1 : 0)}) * from sys.objects" );

        using( var ctx = new SqlStandardCallContext() )
        {
            ISqlConnectionController c = ctx[TestHelper.GetConnectionString()];
            c.Connection.State.Should().Be( ConnectionState.Closed );
            using( await c.ExplicitOpenAsync() )
            {
                c.Connection.State.Should().Be( ConnectionState.Open );

                int count = await c.ExecuteSingleRowAsync( cmd, r =>
                {
                    if( mode == "NoRow" )
                    {
                        r.Should().BeNull();
                        return 0;
                    }
                    else
                    {
                        r.Should().NotBeNull();
                        return 1;
                    }
                } );
            }
            c.Connection.State.Should().Be( ConnectionState.Closed );
        }

    }

}

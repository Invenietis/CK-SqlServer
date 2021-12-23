using CK.Core;
using FluentAssertions;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using Microsoft.Data.SqlClient;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using static CK.Testing.SqlServerTestHelper;

namespace CK.SqlServer.Tests
{
    [TestFixture]
    public class SqlConnectionControllerInfoMessageTests
    {
        [SetUp]
        public void EnsureDatabase()
        {
            TestHelper.EnsureDatabase();
            TestHelper.ExecuteScripts( "if not exists(select 1 from sys.schemas where name = 'CK') exec('create schema CK');" );
        }

        [Test]
        public void InfoMessage_are_monitored_when_SqlHelper_LogSqlServerInfoMessage_is_set_to_true()
        {
            SqlHelper.LogSqlServerInfoMessage = true;
            IReadOnlyList<ActivityMonitorSimpleCollector.Entry> entries = null;
            var m = new ActivityMonitor( false );
            // The messages are Traced but the monitor level is temporarily set to LogFilter.Trace:
            // Even if the monitor should not catch them, info messages traces will be emitted.
            m.MinimalFilter = LogFilter.Release;
            using( m.CollectEntries( logs => entries = logs, LogLevelFilter.Debug ) )
            using( m.Output.CreateBridgeTo( TestHelper.Monitor.Output.BridgeTarget ) )
            using( var ctx = new SqlStandardCallContext( m ) )
            {
                ISqlConnectionController c = ctx[TestHelper.GetConnectionString()];
                c.ExplicitOpen();
                var cmd = c.Connection.CreateCommand();
                cmd.CommandText = "print 'Here I am: a print.';";
                cmd.ExecuteNonQuery();
            }
            entries.Any( e => e.Text.Contains( "Here I am: a print." ) ).Should().BeTrue();
            SqlHelper.LogSqlServerInfoMessage = false;
        }

        class ThreadSafeTraceCounter : IActivityMonitorClient
        {
            public int Count;

            public void OnAutoTagsChanged( CKTrait newTrait )
            {
            }

            public void OnGroupClosed( IActivityLogGroup group, IReadOnlyList<ActivityLogGroupConclusion> conclusions )
            {
            }

            public void OnGroupClosing( IActivityLogGroup group, ref List<ActivityLogGroupConclusion> conclusions )
            {
            }

            public void OnOpenGroup( IActivityLogGroup group )
            {
            }

            public void OnTopicChanged( string newTopic, string fileName, int lineNumber )
            {
            }

            public void OnUnfilteredLog( ref ActivityMonitorLogData data )
            {
                if( data.MaskedLevel == LogLevel.Trace )
                {
                    Interlocked.Increment( ref Count );
                }
            }
        }

        [Test]
        public async Task InfoMessage_are_Threadsafe_Async()
        {
            SqlHelper.LogSqlServerInfoMessage = true;

            static async Task Do200Prints( ThreadSafeTraceCounter t )
            {
                var m = new ActivityMonitor( false );
                m.Output.RegisterClient( t );
                using( var ctx = new SqlStandardCallContext( m ) )
                {
                    ISqlConnectionController c = ctx[TestHelper.GetConnectionString()];
                    c.ExplicitOpen();
                    var cmd = c.Connection.CreateCommand();
                    cmd.CommandText = "print 'Here I am: a print.';";
                    for( int i = 0; i < 200; ++i )
                    {
                        await cmd.ExecuteNonQueryAsync();
                        Thread.Sleep( 5 );
                    }
                }
            }

            ThreadSafeTraceCounter c = new ThreadSafeTraceCounter();
            var tasks = Enumerable.Range( 0, 100 ).Select( _ => Do200Prints( c ) ).ToArray();
            await Task.WhenAll( tasks );

            c.Count.Should().Be( 100 * 200 );

            SqlHelper.LogSqlServerInfoMessage = false;
        }




    }
}

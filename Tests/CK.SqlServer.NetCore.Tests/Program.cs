using NUnitLite;
using System.Reflection;

namespace CK.SqlServer.Tests.NetCore
{
    public static class Program
    {
        public static int Main(string[] args)
        {
            return new AutoRun(Assembly.GetEntryAssembly()).Execute(args);
        }
    }
}

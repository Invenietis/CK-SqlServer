using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CK.SqlServer;

/// <summary>
/// Defines a disposable <see cref="ISqlCallContext"/>. 
/// </summary>
public interface IDisposableSqlCallContext : ISqlCallContext, IDisposable
{
}

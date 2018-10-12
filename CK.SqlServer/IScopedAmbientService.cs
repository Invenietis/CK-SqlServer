using System;
using System.Collections.Generic;
using System.Text;

namespace CK.SqlServer.Impl
{
    /// <summary>
    /// Interface marker definition for scoped services.
    /// The name of the interface is enough and is defined here because CK.StObj.Model must not
    /// be a dependency of this package.
    /// </summary>
    public interface IScopedAmbientService
    {
    }
}

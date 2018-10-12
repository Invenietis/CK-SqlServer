using System;

//
// This enables the SqlStandardCallContext that is a IScopedAmbientService to be handled by
// CK.StObj automatic DI.
//
[assembly: CK.Setup.IsModel()]
namespace CK.Setup { class IsModelAttribute : Attribute {} }


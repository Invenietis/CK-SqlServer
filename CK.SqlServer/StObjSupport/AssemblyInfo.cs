using System;

//
// This enables the SqlStandardCallContext that is a IScopedAmbientService to be handled by
// CK.StObj automatic DI.
//
[assembly: CK.Setup.IsModelDependent()]
namespace CK.Setup { class IsModelDependentAttribute : Attribute {} }


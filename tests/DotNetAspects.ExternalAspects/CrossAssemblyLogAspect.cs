using System.Collections.Generic;
using DotNetAspects.Args;
using DotNetAspects.Interception;
using DotNetAspects.Serialization;

namespace DotNetAspects.ExternalAspects
{
    /// <summary>
    /// An aspect defined in a *separate* assembly from the code it is applied to.
    /// Used to verify the weaver resolves DotNetAspects types transitively
    /// (the consuming assembly does not reference DotNetAspects directly).
    /// </summary>
    [PSerializable]
    public class CrossAssemblyLogAspect : MethodInterceptionAspect
    {
        public static List<string> Log { get; } = new();

        public override void OnInvoke(MethodInterceptionArgs args)
        {
            Log.Add($"Before: {args.Method?.Name}");
            args.Proceed();
            Log.Add($"After: {args.Method?.Name}");
        }
    }
}

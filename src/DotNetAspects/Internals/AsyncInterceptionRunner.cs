using System.Threading.Tasks;
using DotNetAspects.Args;
using DotNetAspects.Interception;

namespace DotNetAspects.Internals
{
    /// <summary>
    /// Runtime helper invoked by the weaver to drive <see cref="MethodInterceptionAspect"/> for
    /// asynchronous (<see cref="Task"/> / <see cref="Task{TResult}"/>) methods.
    /// </summary>
    /// <remarks>
    /// These runners call <see cref="MethodInterceptionAspect.OnInvokeAsync"/> and adapt the resulting
    /// task to the woven method's actual return type, reading the (already unwrapped)
    /// <see cref="MethodInterceptionArgs.ReturnValue"/> for <see cref="Task{TResult}"/> methods.
    /// </remarks>
    public static class AsyncInterceptionRunner
    {
        /// <summary>
        /// Drives interception for a method returning a non-generic <see cref="Task"/>.
        /// </summary>
        public static async Task RunVoid(MethodInterceptionAspect aspect, MethodInterceptionArgs args)
        {
            await aspect.OnInvokeAsync(args).ConfigureAwait(false);
        }

        /// <summary>
        /// Drives interception for a method returning <see cref="Task{TResult}"/> and returns the
        /// (possibly aspect-modified) result.
        /// </summary>
        public static async Task<T> RunResult<T>(MethodInterceptionAspect aspect, MethodInterceptionArgs args)
        {
            await aspect.OnInvokeAsync(args).ConfigureAwait(false);
            var rv = args.ReturnValue;
            if (rv is T t)
                return t;
            return rv == null ? default! : (T)rv;
        }
    }
}

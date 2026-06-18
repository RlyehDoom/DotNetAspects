using System;
using System.Threading.Tasks;
using DotNetAspects.Args;
using DotNetAspects.Interception;

namespace DotNetAspects.Internals
{
    /// <summary>
    /// Runtime helper invoked by the weaver to apply <see cref="OnMethodBoundaryAspect"/> callbacks
    /// around an asynchronous (<see cref="Task"/> / <see cref="Task{TResult}"/>) method.
    /// </summary>
    /// <remarks>
    /// The weaver calls <see cref="OnMethodBoundaryAspect.OnEntry"/> synchronously (and honors
    /// <see cref="FlowBehavior.Return"/>) before invoking the original method. These runners then await
    /// the resulting task and invoke <see cref="OnMethodBoundaryAspect.OnSuccess"/>,
    /// <see cref="OnMethodBoundaryAspect.OnException"/> and <see cref="OnMethodBoundaryAspect.OnExit"/>
    /// at the correct point in the asynchronous flow.
    /// </remarks>
    public static class AsyncBoundaryRunner
    {
        /// <summary>
        /// Awaits a non-generic <see cref="Task"/> and applies the boundary callbacks.
        /// </summary>
        public static async Task RunTask(Task inner, OnMethodBoundaryAspect aspect, MethodExecutionArgs args)
        {
            try
            {
                await inner.ConfigureAwait(false);
                args.ReturnValue = null;
                aspect.OnSuccess(args);
            }
            catch (Exception ex)
            {
                args.Exception = ex;
                aspect.OnException(args);
                if (args.FlowBehavior != FlowBehavior.Return && args.FlowBehavior != FlowBehavior.Continue)
                    throw;
            }
            finally
            {
                aspect.OnExit(args);
            }
        }

        /// <summary>
        /// Awaits a <see cref="Task{TResult}"/>, exposes the result via <see cref="MethodExecutionArgs.ReturnValue"/>
        /// and applies the boundary callbacks. The (possibly modified) return value is propagated to the caller.
        /// </summary>
        public static async Task<T> RunTaskOfT<T>(Task<T> inner, OnMethodBoundaryAspect aspect, MethodExecutionArgs args)
        {
            try
            {
                var result = await inner.ConfigureAwait(false);
                args.ReturnValue = result;
                aspect.OnSuccess(args);
                // Allow OnSuccess to override the return value.
                return args.ReturnValue is T modified ? modified : result;
            }
            catch (Exception ex)
            {
                args.Exception = ex;
                aspect.OnException(args);
                if (args.FlowBehavior == FlowBehavior.Return || args.FlowBehavior == FlowBehavior.Continue)
                    return args.ReturnValue is T rv ? rv : default!;
                throw;
            }
            finally
            {
                aspect.OnExit(args);
            }
        }
    }
}

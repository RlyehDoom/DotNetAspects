using System;
using DotNetAspects.Args;

namespace DotNetAspects.Interception
{
    /// <summary>
    /// Base class for aspects that execute at method boundaries (entry, success, exception, exit).
    /// Inherit from this class to create custom boundary logic without replacing the method call.
    /// </summary>
    /// <remarks>
    /// Unlike <see cref="MethodInterceptionAspect"/>, this aspect does not replace the method call.
    /// It executes at specific points around the method execution.
    /// </remarks>
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class | AttributeTargets.Assembly, AllowMultiple = true, Inherited = true)]
    public abstract class OnMethodBoundaryAspect : Attribute
    {
        /// <summary>
        /// Gets or sets the priority of this aspect.
        /// Lower values execute first (outer aspects).
        /// </summary>
        public int AspectPriority { get; set; }

        /// <summary>
        /// Called before the target method starts executing.
        /// </summary>
        /// <param name="args">Information about the method execution.</param>
        /// <remarks>
        /// Set <see cref="MethodExecutionArgs.FlowBehavior"/> to <see cref="FlowBehavior.Return"/>
        /// and <see cref="MethodExecutionArgs.ReturnValue"/> to skip the method execution.
        /// </remarks>
        public virtual void OnEntry(MethodExecutionArgs args)
        {
        }

        /// <summary>
        /// Called after the target method completes successfully (no exception).
        /// </summary>
        /// <param name="args">Information about the method execution, including the return value.</param>
        public virtual void OnSuccess(MethodExecutionArgs args)
        {
        }

        /// <summary>
        /// Called when the target method throws an exception.
        /// </summary>
        /// <param name="args">Information about the method execution, including the exception.</param>
        /// <remarks>
        /// Set <see cref="MethodExecutionArgs.FlowBehavior"/> to control exception handling:
        /// - <see cref="FlowBehavior.RethrowException"/>: Re-throw the original exception (default).
        /// - <see cref="FlowBehavior.Return"/>: Swallow the exception and return normally.
        /// - <see cref="FlowBehavior.ThrowException"/>: Throw a new exception (set <see cref="MethodExecutionArgs.Exception"/>).
        /// </remarks>
        public virtual void OnException(MethodExecutionArgs args)
        {
        }

        /// <summary>
        /// Called after the target method completes, regardless of success or failure.
        /// </summary>
        /// <param name="args">Information about the method execution.</param>
        /// <remarks>
        /// This method is always called, similar to a finally block.
        /// </remarks>
        public virtual void OnExit(MethodExecutionArgs args)
        {
        }
    }
}

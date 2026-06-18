using System;
using System.Threading.Tasks;
using DotNetAspects.Args;

namespace DotNetAspects.Interception
{
    /// <summary>
    /// Base class for aspects that intercept method invocations.
    /// Inherit from this class to create custom method interception logic.
    /// </summary>
    /// <remarks>
    /// This aspect completely replaces the method invocation. You must call
    /// <see cref="MethodInterceptionArgs.Proceed"/> or <see cref="MethodInterceptionArgs.Invoke"/>
    /// to execute the original method.
    /// </remarks>
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class | AttributeTargets.Assembly, AllowMultiple = true, Inherited = true)]
    public abstract class MethodInterceptionAspect : Attribute
    {
        /// <summary>
        /// Gets or sets the priority of this aspect.
        /// Lower values execute first (outer aspects).
        /// </summary>
        public int AspectPriority { get; set; }

        /// <summary>
        /// Called when the target method is invoked.
        /// Override this method to implement custom interception logic.
        /// </summary>
        /// <param name="args">Information about the method invocation.</param>
        /// <remarks>
        /// You must call <see cref="MethodInterceptionArgs.Proceed"/> or
        /// <see cref="MethodInterceptionArgs.Invoke"/> to execute the original method,
        /// or set <see cref="MethodInterceptionArgs.ReturnValue"/> to return a custom value.
        /// </remarks>
        public virtual void OnInvoke(MethodInterceptionArgs args)
        {
            args.Proceed();
        }

        /// <summary>
        /// Called when an asynchronous (<see cref="Task"/> / <see cref="Task{TResult}"/>) target method
        /// is invoked. Override this method to implement async-correct interception logic.
        /// </summary>
        /// <param name="args">Information about the method invocation.</param>
        /// <returns>A task that completes when interception finishes.</returns>
        /// <remarks>
        /// You must <c>await</c> <see cref="MethodInterceptionArgs.ProceedAsync"/> to execute the original
        /// method, or set <see cref="MethodInterceptionArgs.ReturnValue"/> to return a custom value.
        /// The default implementation simply awaits <see cref="MethodInterceptionArgs.ProceedAsync"/>.
        /// For async methods, override this method rather than <see cref="OnInvoke"/>.
        /// </remarks>
        public virtual async Task OnInvokeAsync(MethodInterceptionArgs args)
        {
            await args.ProceedAsync().ConfigureAwait(false);
        }
    }
}

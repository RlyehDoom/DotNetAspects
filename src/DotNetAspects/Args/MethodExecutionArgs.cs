using System;
using System.Reflection;

namespace DotNetAspects.Args
{
    /// <summary>
    /// Specifies the flow behavior after an aspect method returns.
    /// </summary>
    public enum FlowBehavior
    {
        /// <summary>
        /// Default behavior - continue with normal execution.
        /// </summary>
        Default = 0,

        /// <summary>
        /// Continue with normal execution.
        /// </summary>
        Continue = 1,

        /// <summary>
        /// Re-throw the current exception (only valid in OnException).
        /// </summary>
        RethrowException = 2,

        /// <summary>
        /// Return immediately without executing the method body.
        /// </summary>
        Return = 3,

        /// <summary>
        /// Throw a new exception.
        /// </summary>
        ThrowException = 4
    }

    /// <summary>
    /// Arguments for method boundary aspects (OnEntry, OnSuccess, OnException, OnExit).
    /// </summary>
    public class MethodExecutionArgs
    {
        /// <summary>
        /// Initializes a new instance of <see cref="MethodExecutionArgs"/>.
        /// </summary>
        public MethodExecutionArgs()
        {
        }

        /// <summary>
        /// Initializes a new instance of <see cref="MethodExecutionArgs"/> with the specified values.
        /// </summary>
        /// <param name="instance">The instance on which the method is invoked.</param>
        /// <param name="method">The method being executed.</param>
        /// <param name="arguments">The arguments passed to the method.</param>
        public MethodExecutionArgs(object instance, MethodBase method, IArguments arguments)
        {
            Instance = instance;
            Method = method;
            Arguments = arguments;
        }

        /// <summary>
        /// Gets or sets the instance on which the method is being invoked.
        /// Null for static methods.
        /// </summary>
        public object? Instance { get; set; }

        /// <summary>
        /// Gets or sets the method being executed.
        /// </summary>
        public MethodBase? Method { get; set; }

        /// <summary>
        /// Gets or sets the arguments of the method call.
        /// </summary>
        public IArguments? Arguments { get; set; }

        /// <summary>
        /// Gets or sets the return value of the method.
        /// Only available in OnSuccess and OnExit.
        /// </summary>
        public object? ReturnValue { get; set; }

        /// <summary>
        /// Gets or sets the exception thrown by the method.
        /// Only available in OnException.
        /// </summary>
        public Exception? Exception { get; set; }

        /// <summary>
        /// Gets or sets the flow behavior to apply after the aspect method returns.
        /// </summary>
        public FlowBehavior FlowBehavior { get; set; } = FlowBehavior.Default;

        /// <summary>
        /// Gets or sets a tag that can be used to pass state between aspect methods.
        /// </summary>
        public object? Tag { get; set; }

        /// <summary>
        /// Gets or sets the yield value for iterator methods.
        /// </summary>
        public object? YieldValue { get; set; }
    }
}

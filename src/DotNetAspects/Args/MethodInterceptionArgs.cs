using System;
using System.Reflection;

namespace DotNetAspects.Args
{
    /// <summary>
    /// Arguments for method interception aspects.
    /// Contains all information about the intercepted method call.
    /// </summary>
    public class MethodInterceptionArgs
    {
        private Func<object[], object?>? _invoker;
        private MethodInfo? _originalMethod;

        /// <summary>
        /// Initializes a new instance of <see cref="MethodInterceptionArgs"/>.
        /// </summary>
        public MethodInterceptionArgs()
        {
            Arguments = new Arguments();
        }

        /// <summary>
        /// Initializes a new instance of <see cref="MethodInterceptionArgs"/> with the specified invoker.
        /// </summary>
        public MethodInterceptionArgs(
            object? instance,
            MethodBase? method,
            IArguments? arguments,
            Func<object[], object?>? invoker)
        {
            Instance = instance;
            Method = method;
            Arguments = arguments ?? new Arguments();
            _invoker = invoker;
        }

        /// <summary>
        /// Initializes a new instance with reflection-based invocation.
        /// Used by the weaver when delegate creation is not practical.
        /// </summary>
        public MethodInterceptionArgs(
            object? instance,
            MethodBase? method,
            IArguments? arguments,
            MethodInfo originalMethod)
        {
            Instance = instance;
            Method = method;
            Arguments = arguments ?? new Arguments();
            _originalMethod = originalMethod;
        }

        /// <summary>
        /// Gets or sets the instance on which the method is being invoked. Null for static methods.
        /// </summary>
        public object? Instance { get; set; }

        /// <summary>
        /// Gets or sets the method being intercepted.
        /// </summary>
        public MethodBase? Method { get; set; }

        /// <summary>
        /// Gets or sets the arguments passed to the method.
        /// </summary>
        public IArguments Arguments { get; set; }

        /// <summary>
        /// Gets or sets the return value of the method.
        /// </summary>
        public object? ReturnValue { get; set; }

        /// <summary>
        /// Sets the original method for reflection-based invocation.
        /// </summary>
        /// <param name="originalMethod">The original method to invoke.</param>
        public void SetOriginalMethod(MethodInfo originalMethod)
        {
            _originalMethod = originalMethod;
        }

        /// <summary>
        /// Sets the invoker delegate for the method.
        /// </summary>
        /// <param name="invoker">The delegate to invoke the method.</param>
        public void SetInvoker(Func<object[], object?> invoker)
        {
            _invoker = invoker;
        }

        /// <summary>
        /// Proceeds with the original method invocation using the current arguments.
        /// </summary>
        /// <returns>The return value of the method.</returns>
        public object? Proceed()
        {
            return Invoke(Arguments);
        }

        /// <summary>
        /// Invokes the original method with the specified arguments.
        /// </summary>
        /// <param name="arguments">The arguments to pass to the method.</param>
        /// <returns>The return value of the method.</returns>
        public object? Invoke(IArguments arguments)
        {
            object? result;

            if (_invoker != null)
            {
                result = _invoker(arguments.ToArray());
            }
            else if (_originalMethod != null)
            {
                result = _originalMethod.Invoke(Instance, arguments.ToArray());
            }
            else
            {
                throw new InvalidOperationException(
                    "No invoker or original method has been configured for this interception.");
            }

            ReturnValue = result;
            return result;
        }

        /// <summary>
        /// Gets the method binding for direct invocation.
        /// </summary>
        public MethodBinding? Binding => _originalMethod != null
            ? new MethodBinding(Instance, _originalMethod)
            : null;
    }

    /// <summary>
    /// Represents a binding to a method that can be invoked directly.
    /// </summary>
    public class MethodBinding
    {
        private readonly object? _instance;
        private readonly MethodInfo _method;

        /// <summary>
        /// Initializes a new instance of <see cref="MethodBinding"/>.
        /// </summary>
        /// <param name="instance">The instance on which to invoke the method.</param>
        /// <param name="method">The method to invoke.</param>
        public MethodBinding(object? instance, MethodInfo method)
        {
            _instance = instance;
            _method = method;
        }

        /// <summary>
        /// Invokes the method with the specified arguments.
        /// </summary>
        /// <param name="arguments">The arguments to pass to the method.</param>
        /// <returns>The return value of the method.</returns>
        public object? Invoke(params object?[] arguments)
        {
            return _method.Invoke(_instance, arguments);
        }
    }
}

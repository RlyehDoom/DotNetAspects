using System;
using System.Collections;
using System.Collections.Generic;

namespace DotNetAspects.Args
{
    /// <summary>
    /// Represents the arguments of a method invocation.
    /// Implements IEnumerable&lt;object&gt; to support LINQ operations like .First(), .Where(), etc.
    /// </summary>
    public interface IArguments : IEnumerable<object>, IEnumerable
    {
        /// <summary>
        /// Gets the number of arguments.
        /// </summary>
        int Count { get; }

        /// <summary>
        /// Gets the argument at the specified index.
        /// </summary>
        /// <param name="index">The zero-based index of the argument.</param>
        /// <returns>The argument value.</returns>
        object this[int index] { get; }

        /// <summary>
        /// Gets the argument at the specified index.
        /// </summary>
        /// <param name="index">The zero-based index of the argument.</param>
        /// <returns>The argument value.</returns>
        object GetArgument(int index);

        /// <summary>
        /// Sets the argument at the specified index.
        /// </summary>
        /// <param name="index">The zero-based index of the argument.</param>
        /// <param name="value">The new value for the argument.</param>
        void SetArgument(int index, object value);

        /// <summary>
        /// Converts the arguments to an array.
        /// </summary>
        /// <returns>An array containing all arguments.</returns>
        object[] ToArray();
    }
}

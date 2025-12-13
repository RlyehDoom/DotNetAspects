using System;
using System.Collections;
using System.Collections.Generic;

namespace DotNetAspects.Args
{
    /// <summary>
    /// Default implementation of <see cref="IArguments"/>.
    /// Supports LINQ operations through IEnumerable&lt;object&gt; implementation.
    /// </summary>
    public class Arguments : IArguments
    {
        private readonly object[] _arguments;

        /// <summary>
        /// Initializes a new instance of <see cref="Arguments"/> with the specified values.
        /// </summary>
        /// <param name="arguments">The argument values.</param>
        public Arguments(params object[] arguments)
        {
            _arguments = arguments ?? Array.Empty<object>();
        }

        /// <inheritdoc/>
        public int Count => _arguments.Length;

        /// <inheritdoc/>
        public object this[int index] => GetArgument(index);

        /// <inheritdoc/>
        public object GetArgument(int index)
        {
            if (index < 0 || index >= _arguments.Length)
                throw new ArgumentOutOfRangeException(nameof(index));

            return _arguments[index];
        }

        /// <inheritdoc/>
        public void SetArgument(int index, object value)
        {
            if (index < 0 || index >= _arguments.Length)
                throw new ArgumentOutOfRangeException(nameof(index));

            _arguments[index] = value;
        }

        /// <inheritdoc/>
        public object[] ToArray()
        {
            // For zero or one argument, return the internal array directly (safe for most use cases)
            if (_arguments.Length <= 1)
                return _arguments;

            var result = new object[_arguments.Length];
            Array.Copy(_arguments, result, _arguments.Length);
            return result;
        }

        /// <inheritdoc/>
        public object[] GetRawArray() => _arguments;

        /// <inheritdoc/>
        public IEnumerator<object> GetEnumerator()
        {
            foreach (var arg in _arguments)
            {
                yield return arg;
            }
        }

        /// <inheritdoc/>
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}

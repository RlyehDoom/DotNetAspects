#nullable disable
using System;
using System.Collections;
using System.Collections.Generic;
using DotNetAspects.Args;

namespace DotNetAspects.Internals
{
    /// <summary>
    /// Strongly-typed arguments container for single argument methods.
    /// </summary>
    /// <typeparam name="T">The type of the first argument.</typeparam>
    public class Arguments<T> : IArguments
    {
        /// <summary>
        /// Gets or sets the first argument.
        /// </summary>
        public T Arg0 { get; set; }

        /// <inheritdoc/>
        public int Count => 1;

        /// <inheritdoc/>
        public object this[int index] => GetArgument(index);

        /// <inheritdoc/>
        public object GetArgument(int index)
        {
            switch (index)
            {
                case 0: return Arg0;
                default: throw new ArgumentOutOfRangeException(nameof(index));
            }
        }

        /// <inheritdoc/>
        public void SetArgument(int index, object value)
        {
            switch (index)
            {
                case 0:
                    Arg0 = (T)value;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(index));
            }
        }

        /// <inheritdoc/>
        public object[] ToArray() => new object[] { Arg0 };

        /// <inheritdoc/>
        public object[] GetRawArray() => ToArray();

        /// <inheritdoc/>
        public IEnumerator<object> GetEnumerator()
        {
            yield return Arg0;
        }

        /// <inheritdoc/>
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }

    /// <summary>
    /// Strongly-typed arguments container for two-argument methods.
    /// </summary>
    public class Arguments<T1, T2> : IArguments
    {
        /// <summary>
        /// Gets or sets the first argument.
        /// </summary>
        public T1 Arg0 { get; set; }

        /// <summary>
        /// Gets or sets the second argument.
        /// </summary>
        public T2 Arg1 { get; set; }

        /// <inheritdoc/>
        public int Count => 2;

        /// <inheritdoc/>
        public object this[int index] => GetArgument(index);

        /// <inheritdoc/>
        public object GetArgument(int index)
        {
            switch (index)
            {
                case 0: return Arg0;
                case 1: return Arg1;
                default: throw new ArgumentOutOfRangeException(nameof(index));
            }
        }

        /// <inheritdoc/>
        public void SetArgument(int index, object value)
        {
            switch (index)
            {
                case 0:
                    Arg0 = (T1)value;
                    break;
                case 1:
                    Arg1 = (T2)value;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(index));
            }
        }

        /// <inheritdoc/>
        public object[] ToArray() => new object[] { Arg0, Arg1 };

        /// <inheritdoc/>
        public object[] GetRawArray() => ToArray();

        /// <inheritdoc/>
        public IEnumerator<object> GetEnumerator()
        {
            yield return Arg0;
            yield return Arg1;
        }

        /// <inheritdoc/>
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }

    /// <summary>
    /// Strongly-typed arguments container for three-argument methods.
    /// </summary>
    public class Arguments<T1, T2, T3> : IArguments
    {
        /// <summary>
        /// Gets or sets the first argument.
        /// </summary>
        public T1 Arg0 { get; set; }

        /// <summary>
        /// Gets or sets the second argument.
        /// </summary>
        public T2 Arg1 { get; set; }

        /// <summary>
        /// Gets or sets the third argument.
        /// </summary>
        public T3 Arg2 { get; set; }

        /// <inheritdoc/>
        public int Count => 3;

        /// <inheritdoc/>
        public object this[int index] => GetArgument(index);

        /// <inheritdoc/>
        public object GetArgument(int index)
        {
            switch (index)
            {
                case 0: return Arg0;
                case 1: return Arg1;
                case 2: return Arg2;
                default: throw new ArgumentOutOfRangeException(nameof(index));
            }
        }

        /// <inheritdoc/>
        public void SetArgument(int index, object value)
        {
            switch (index)
            {
                case 0:
                    Arg0 = (T1)value;
                    break;
                case 1:
                    Arg1 = (T2)value;
                    break;
                case 2:
                    Arg2 = (T3)value;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(index));
            }
        }

        /// <inheritdoc/>
        public object[] ToArray() => new object[] { Arg0, Arg1, Arg2 };

        /// <inheritdoc/>
        public object[] GetRawArray() => ToArray();

        /// <inheritdoc/>
        public IEnumerator<object> GetEnumerator()
        {
            yield return Arg0;
            yield return Arg1;
            yield return Arg2;
        }

        /// <inheritdoc/>
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }

    /// <summary>
    /// Strongly-typed arguments container for four-argument methods.
    /// </summary>
    public class Arguments<T1, T2, T3, T4> : IArguments
    {
        /// <summary>
        /// Gets or sets the first argument.
        /// </summary>
        public T1 Arg0 { get; set; }

        /// <summary>
        /// Gets or sets the second argument.
        /// </summary>
        public T2 Arg1 { get; set; }

        /// <summary>
        /// Gets or sets the third argument.
        /// </summary>
        public T3 Arg2 { get; set; }

        /// <summary>
        /// Gets or sets the fourth argument.
        /// </summary>
        public T4 Arg3 { get; set; }

        /// <inheritdoc/>
        public int Count => 4;

        /// <inheritdoc/>
        public object this[int index] => GetArgument(index);

        /// <inheritdoc/>
        public object GetArgument(int index)
        {
            switch (index)
            {
                case 0: return Arg0;
                case 1: return Arg1;
                case 2: return Arg2;
                case 3: return Arg3;
                default: throw new ArgumentOutOfRangeException(nameof(index));
            }
        }

        /// <inheritdoc/>
        public void SetArgument(int index, object value)
        {
            switch (index)
            {
                case 0:
                    Arg0 = (T1)value;
                    break;
                case 1:
                    Arg1 = (T2)value;
                    break;
                case 2:
                    Arg2 = (T3)value;
                    break;
                case 3:
                    Arg3 = (T4)value;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(index));
            }
        }

        /// <inheritdoc/>
        public object[] ToArray() => new object[] { Arg0, Arg1, Arg2, Arg3 };

        /// <inheritdoc/>
        public object[] GetRawArray() => ToArray();

        /// <inheritdoc/>
        public IEnumerator<object> GetEnumerator()
        {
            yield return Arg0;
            yield return Arg1;
            yield return Arg2;
            yield return Arg3;
        }

        /// <inheritdoc/>
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}

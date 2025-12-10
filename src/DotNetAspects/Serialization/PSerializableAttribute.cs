using System;

namespace DotNetAspects.Serialization
{
    /// <summary>
    /// Indicates that an aspect can be serialized.
    /// This attribute is required for aspects that need to persist state across compilations.
    /// </summary>
    /// <remarks>
    /// This is a compatibility attribute for migration.
    /// In this implementation, it serves as a marker attribute.
    /// </remarks>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, AllowMultiple = false, Inherited = true)]
    public sealed class PSerializableAttribute : Attribute
    {
        /// <summary>
        /// Initializes a new instance of <see cref="PSerializableAttribute"/>.
        /// </summary>
        public PSerializableAttribute()
        {
        }
    }
}

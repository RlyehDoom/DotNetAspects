using System;

namespace DotNetAspects.Extensibility
{
    /// <summary>
    /// Specifies how a multicast attribute should be applied to targets.
    /// </summary>
    /// <remarks>
    /// This attribute is used to configure how aspects are propagated to their targets.
    /// </remarks>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
    public sealed class MulticastAttributeUsageAttribute : Attribute
    {
        /// <summary>
        /// Initializes a new instance of <see cref="MulticastAttributeUsageAttribute"/>.
        /// </summary>
        public MulticastAttributeUsageAttribute()
        {
        }

        /// <summary>
        /// Initializes a new instance of <see cref="MulticastAttributeUsageAttribute"/>
        /// with the specified target types.
        /// </summary>
        /// <param name="validOn">The valid targets for the aspect.</param>
        public MulticastAttributeUsageAttribute(MulticastTargets validOn)
        {
            ValidOn = validOn;
        }

        /// <summary>
        /// Gets or sets the types of targets on which the aspect can be applied.
        /// </summary>
        public MulticastTargets ValidOn { get; set; } = MulticastTargets.All;

        /// <summary>
        /// Gets or sets the attributes of members to which the aspect can be applied.
        /// </summary>
        public MulticastAttributes TargetMemberAttributes { get; set; } = MulticastAttributes.All;

        /// <summary>
        /// Gets or sets the attributes of types to which the aspect can be applied.
        /// </summary>
        public MulticastAttributes TargetTypeAttributes { get; set; } = MulticastAttributes.All;

        /// <summary>
        /// Gets or sets a value indicating whether the aspect should be inherited.
        /// </summary>
        public bool Inheritance { get; set; } = true;

        /// <summary>
        /// Gets or sets a value indicating whether multiple instances are allowed.
        /// </summary>
        public bool AllowMultiple { get; set; } = true;

        /// <summary>
        /// Gets or sets a value indicating whether metadata should be persisted.
        /// </summary>
        /// <remarks>
        /// When true, the aspect metadata is available at runtime.
        /// </remarks>
        public bool PersistMetaData { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the aspect should replace
        /// any existing aspect of the same type.
        /// </summary>
        public bool Replace { get; set; }

        /// <summary>
        /// Gets or sets the external type patterns to which the aspect can be applied.
        /// </summary>
        public string TargetExternalTypePatterns { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the external member patterns to which the aspect can be applied.
        /// </summary>
        public string TargetExternalMemberPatterns { get; set; } = string.Empty;
    }
}

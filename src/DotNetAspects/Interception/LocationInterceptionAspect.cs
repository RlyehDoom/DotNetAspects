using System;
using DotNetAspects.Args;

namespace DotNetAspects.Interception
{
    /// <summary>
    /// Base class for aspects that intercept property or field access.
    /// Inherit from this class to create custom property/field interception logic.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Class | AttributeTargets.Assembly, AllowMultiple = true, Inherited = true)]
    public abstract class LocationInterceptionAspect : Attribute
    {
        /// <summary>
        /// Gets or sets the priority of this aspect.
        /// Lower values execute first (outer aspects).
        /// </summary>
        public int AspectPriority { get; set; }

        /// <summary>
        /// Called when the property or field value is being retrieved.
        /// </summary>
        /// <param name="args">Information about the location access.</param>
        /// <remarks>
        /// Call <see cref="LocationInterceptionArgs.ProceedGetValue"/> to get the actual value,
        /// or set <see cref="LocationInterceptionArgs.Value"/> directly to return a custom value.
        /// </remarks>
        public virtual void OnGetValue(LocationInterceptionArgs args)
        {
            args.ProceedGetValue();
        }

        /// <summary>
        /// Called when the property or field value is being set.
        /// </summary>
        /// <param name="args">Information about the location access.</param>
        /// <remarks>
        /// Modify <see cref="LocationInterceptionArgs.Value"/> before calling
        /// <see cref="LocationInterceptionArgs.ProceedSetValue"/> to change the value being set.
        /// </remarks>
        public virtual void OnSetValue(LocationInterceptionArgs args)
        {
            args.ProceedSetValue();
        }
    }
}

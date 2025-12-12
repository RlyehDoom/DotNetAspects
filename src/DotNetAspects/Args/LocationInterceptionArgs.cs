using System;
using System.Reflection;

namespace DotNetAspects.Args
{
    /// <summary>
    /// Represents information about a location (field or property).
    /// </summary>
    public class LocationInfo
    {
        /// <summary>
        /// Gets or sets the name of the location.
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the type of the location.
        /// </summary>
        public Type LocationType { get; set; } = typeof(object);

        /// <summary>
        /// Gets or sets the declaring type of the location.
        /// </summary>
        public Type? DeclaringType { get; set; }

        /// <summary>
        /// Gets or sets the PropertyInfo if this location is a property.
        /// </summary>
        public PropertyInfo? PropertyInfo { get; set; }

        /// <summary>
        /// Gets or sets the FieldInfo if this location is a field.
        /// </summary>
        public FieldInfo? FieldInfo { get; set; }
    }

    /// <summary>
    /// Arguments for location (property/field) interception aspects.
    /// </summary>
    public class LocationInterceptionArgs
    {
        /// <summary>
        /// Internal getter delegate. Public for IL weaving access.
        /// </summary>
        public Func<object>? _getter;

        /// <summary>
        /// Internal setter delegate. Public for IL weaving access.
        /// </summary>
        public Action<object>? _setter;

        /// <summary>
        /// Initializes a new instance of <see cref="LocationInterceptionArgs"/>.
        /// </summary>
        public LocationInterceptionArgs()
        {
        }

        /// <summary>
        /// Initializes a new instance of <see cref="LocationInterceptionArgs"/> with the specified values.
        /// </summary>
        /// <param name="instance">The instance on which the location is accessed.</param>
        /// <param name="location">Information about the location.</param>
        /// <param name="getter">Delegate to get the current value.</param>
        /// <param name="setter">Delegate to set the value.</param>
        public LocationInterceptionArgs(
            object instance,
            LocationInfo location,
            Func<object> getter,
            Action<object> setter)
        {
            Instance = instance;
            Location = location;
            _getter = getter;
            _setter = setter;
        }

        /// <summary>
        /// Gets or sets the instance on which the location is being accessed.
        /// Null for static members.
        /// </summary>
        public object? Instance { get; set; }

        /// <summary>
        /// Gets or sets information about the location (property or field).
        /// </summary>
        public LocationInfo? Location { get; set; }

        /// <summary>
        /// Gets or sets the value being get or set.
        /// </summary>
        public object? Value { get; set; }

        /// <summary>
        /// Gets or sets the index arguments (for indexed properties).
        /// </summary>
        public IArguments? Index { get; set; }

        /// <summary>
        /// Gets or sets the name of the location (property or field name).
        /// </summary>
        public string? LocationName { get; set; }

        /// <summary>
        /// Gets or sets the type of the location.
        /// </summary>
        public Type? LocationType { get; set; }

        /// <summary>
        /// Gets the current value from the underlying location.
        /// </summary>
        /// <returns>The current value.</returns>
        public object GetCurrentValue()
        {
            if (_getter == null)
                throw new InvalidOperationException("No getter has been configured for this location.");

            return _getter();
        }

        /// <summary>
        /// Sets the value to the underlying location.
        /// </summary>
        /// <param name="value">The value to set.</param>
        public void SetNewValue(object value)
        {
            if (_setter == null)
                throw new InvalidOperationException("No setter has been configured for this location.");

            _setter(value);
        }

        /// <summary>
        /// Proceeds with getting the value from the underlying location.
        /// </summary>
        public void ProceedGetValue()
        {
            Value = GetCurrentValue();
        }

        /// <summary>
        /// Proceeds with setting the value to the underlying location.
        /// </summary>
        public void ProceedSetValue()
        {
            SetNewValue(Value!);
        }
    }
}

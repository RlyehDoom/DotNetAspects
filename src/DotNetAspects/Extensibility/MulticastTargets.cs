using System;

namespace DotNetAspects.Extensibility
{
    /// <summary>
    /// Specifies the types of targets to which an aspect can be applied.
    /// </summary>
    [Flags]
    public enum MulticastTargets
    {
        /// <summary>
        /// No target.
        /// </summary>
        None = 0,

        /// <summary>
        /// Target is a class.
        /// </summary>
        Class = 1,

        /// <summary>
        /// Target is a struct.
        /// </summary>
        Struct = 2,

        /// <summary>
        /// Target is an interface.
        /// </summary>
        Interface = 4,

        /// <summary>
        /// Target is a delegate.
        /// </summary>
        Delegate = 8,

        /// <summary>
        /// Target is an enum.
        /// </summary>
        Enum = 16,

        /// <summary>
        /// Target is any type (class, struct, interface, delegate, or enum).
        /// </summary>
        AnyType = Class | Struct | Interface | Delegate | Enum,

        /// <summary>
        /// Target is a field.
        /// </summary>
        Field = 32,

        /// <summary>
        /// Target is a method (excluding constructors).
        /// </summary>
        Method = 64,

        /// <summary>
        /// Target is a constructor.
        /// </summary>
        Constructor = 128,

        /// <summary>
        /// Target is a property.
        /// </summary>
        Property = 256,

        /// <summary>
        /// Target is an event.
        /// </summary>
        Event = 512,

        /// <summary>
        /// Target is any member.
        /// </summary>
        AnyMember = Field | Method | Constructor | Property | Event,

        /// <summary>
        /// Target is an assembly.
        /// </summary>
        Assembly = 1024,

        /// <summary>
        /// Target is a parameter.
        /// </summary>
        Parameter = 2048,

        /// <summary>
        /// Target is a return value.
        /// </summary>
        ReturnValue = 4096,

        /// <summary>
        /// Target can be any element.
        /// </summary>
        All = AnyType | AnyMember | Assembly | Parameter | ReturnValue
    }

    /// <summary>
    /// Specifies the attributes of members to which an aspect can be applied.
    /// </summary>
    [Flags]
    public enum MulticastAttributes
    {
        /// <summary>
        /// No specific attributes.
        /// </summary>
        None = 0,

        /// <summary>
        /// Private members.
        /// </summary>
        Private = 1,

        /// <summary>
        /// Protected members.
        /// </summary>
        Protected = 2,

        /// <summary>
        /// Internal members.
        /// </summary>
        Internal = 4,

        /// <summary>
        /// Internal and protected members.
        /// </summary>
        InternalAndProtected = 8,

        /// <summary>
        /// Internal or protected members.
        /// </summary>
        InternalOrProtected = 16,

        /// <summary>
        /// Public members.
        /// </summary>
        Public = 32,

        /// <summary>
        /// Any visibility.
        /// </summary>
        AnyVisibility = Private | Protected | Internal | InternalAndProtected | InternalOrProtected | Public,

        /// <summary>
        /// Static members.
        /// </summary>
        Static = 64,

        /// <summary>
        /// Instance members.
        /// </summary>
        Instance = 128,

        /// <summary>
        /// Abstract members.
        /// </summary>
        Abstract = 256,

        /// <summary>
        /// Non-abstract members.
        /// </summary>
        NonAbstract = 512,

        /// <summary>
        /// Virtual members.
        /// </summary>
        Virtual = 1024,

        /// <summary>
        /// Non-virtual members.
        /// </summary>
        NonVirtual = 2048,

        /// <summary>
        /// Managed code only.
        /// </summary>
        Managed = 4096,

        /// <summary>
        /// Non-managed code.
        /// </summary>
        NonManaged = 8192,

        /// <summary>
        /// Literal fields (constants).
        /// </summary>
        Literal = 16384,

        /// <summary>
        /// Non-literal fields.
        /// </summary>
        NonLiteral = 32768,

        /// <summary>
        /// Input parameters.
        /// </summary>
        InParameter = 65536,

        /// <summary>
        /// Output parameters.
        /// </summary>
        OutParameter = 131072,

        /// <summary>
        /// Reference parameters.
        /// </summary>
        RefParameter = 262144,

        /// <summary>
        /// Compiler-generated members.
        /// </summary>
        CompilerGenerated = 524288,

        /// <summary>
        /// User-generated members.
        /// </summary>
        UserGenerated = 1048576,

        /// <summary>
        /// All attributes.
        /// </summary>
        All = AnyVisibility | Static | Instance | Abstract | NonAbstract | Virtual | NonVirtual | Managed | NonManaged | Literal | NonLiteral | InParameter | OutParameter | RefParameter | CompilerGenerated | UserGenerated
    }
}

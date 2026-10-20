namespace Campus.Contracts;

/// <summary>Identifies a data identity declaration for compile-time semantic analysis.</summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Field |
    AttributeTargets.Property | AttributeTargets.Parameter | AttributeTargets.ReturnValue,
    Inherited = false, AllowMultiple = false)]
public sealed class DataIdAttribute : Attribute
{
}

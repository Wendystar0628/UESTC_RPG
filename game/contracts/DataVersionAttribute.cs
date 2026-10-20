namespace Campus.Contracts;

/// <summary>Identifies a data version declaration for compile-time semantic analysis.</summary>
[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property | AttributeTargets.Parameter |
    AttributeTargets.ReturnValue, Inherited = false, AllowMultiple = false)]
public sealed class DataVersionAttribute : Attribute
{
}

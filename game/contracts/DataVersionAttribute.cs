namespace Campus.Contracts;

/// <summary>Identifies a data version declaration for compile-time semantic analysis.</summary>
/// <remarks>只供门禁识别，不是许可名单。</remarks>
[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property | AttributeTargets.Parameter |
    AttributeTargets.ReturnValue, Inherited = false, AllowMultiple = false)]
public sealed class DataVersionAttribute : Attribute
{
}

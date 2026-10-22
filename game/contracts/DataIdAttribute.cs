namespace Campus.Contracts;

/// <summary>Marks a data identity for semantic checks and string-value serialization.</summary>
/// <remarks>标记身份语义，不是许可名单；字符串值类型由 ContractJson 序列化。</remarks>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Field |
    AttributeTargets.Property | AttributeTargets.Parameter | AttributeTargets.ReturnValue,
    Inherited = false, AllowMultiple = false)]
public sealed class DataIdAttribute : Attribute
{
}

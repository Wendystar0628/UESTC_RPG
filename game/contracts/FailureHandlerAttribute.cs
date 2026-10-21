namespace Campus.Contracts;

/// <summary>Identifies an exception parameter received by the failure flow described in Document.</summary>
/// <remarks>只供门禁识别，不是许可名单。</remarks>
[AttributeUsage(AttributeTargets.Parameter, Inherited = false, AllowMultiple = false)]
public sealed class FailureHandlerAttribute(string document) : Attribute
{
    public string Document { get; } = document;
}

namespace Campus.Contracts;

/// <summary>Identifies an exception parameter received by the failure flow described in Document.</summary>
[AttributeUsage(AttributeTargets.Parameter, Inherited = false, AllowMultiple = false)]
public sealed class FailureHandlerAttribute(string document) : Attribute
{
    public string Document { get; } = document;
}

namespace Campus.Gen.Walkable;

public enum WalkBakeFreshnessFailure { UnknownVersion, InvalidData, StaleInputs }

/// <summary>Typed boot-data rejection, allowing callers to log unsupported, damaged and outdated products separately.</summary>
public sealed class WalkBakeFreshnessException(WalkBakeFreshnessFailure failure, string message, Exception? innerException = null)
    : InvalidOperationException(message, innerException)
{
    public WalkBakeFreshnessFailure Failure { get; } = failure;
}

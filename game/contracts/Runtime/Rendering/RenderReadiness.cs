namespace Campus.Contracts;

public interface IRenderReadiness
{
    string? Error { get; }
    bool Pending { get; }
}

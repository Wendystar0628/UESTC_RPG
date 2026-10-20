namespace Campus.Contracts;

public interface IRenderReadiness
{
    Exception? Failure { get; }
    bool Pending { get; }
}

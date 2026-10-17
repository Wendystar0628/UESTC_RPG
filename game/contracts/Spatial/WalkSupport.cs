namespace Campus.Contracts;

/// <summary>Immutable exact support surfaces, prepared from the same geometry as walking collision.</summary>
public interface IWalkSupportSource
{
    float? SupportY(float x, float z, float yHint);
}

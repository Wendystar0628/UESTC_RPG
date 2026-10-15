namespace Campus.Contracts;

[AttributeUsage(AttributeTargets.Assembly)]
public sealed class MapScopeAttribute(string map) : Attribute
{
    public string Map { get; } = map;
}

namespace Campus.Contracts;

/// <summary>Reads required installation resources while preserving engine caching and caller ownership.</summary>
public interface IRequiredResources
{
    byte[] ReadBytes(string path);
    string ReadText(string path);
    T Load<T>(string path) where T : Godot.Resource;
}

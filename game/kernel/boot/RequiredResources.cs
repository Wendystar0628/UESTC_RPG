using Campus.Contracts;
using Campus.Contracts.AppFlow;
using Godot;

namespace Campus.Kernel.Boot;

/// <summary>The single failure boundary for required installation resource reads.</summary>
[KernelService(typeof(IRequiredResources))]
public sealed class RequiredResources : IRequiredResources
{
    public byte[] ReadBytes(string path) => ReadFile(path,
        file => file.GetBuffer(checked((long)file.GetLength())), bytes => bytes.Length == 0);

    public string ReadText(string path) => ReadFile(path, file => file.GetAsText(), string.IsNullOrEmpty);

    public T Load<T>(string path) where T : Resource => Read(path,
        () => ResourceLoader.Load<T>(path)
            ?? throw new FileNotFoundException("Required resource could not be loaded.", path));

    private static T ReadFile<T>(string path, Func<Godot.FileAccess, T> read, Func<T, bool> empty) => Read(path, () =>
    {
        using var file = Godot.FileAccess.Open(path, Godot.FileAccess.ModeFlags.Read);
        if (file is null)
            throw new IOException($"Cannot open required resource '{path}': {Godot.FileAccess.GetOpenError()}.");
        var value = read(file);
        var error = file.GetError();
        if (error is not (Error.Ok or Error.FileEof))
            throw new IOException($"Cannot read required resource '{path}': {error}.");
        if (empty(value)) throw new InvalidDataException($"Required resource is empty: {path}.");
        return value;
    });

    private static T Read<T>(string path, Func<T> read)
    {
        try { return read(); }
        catch (Exception error)
        {
            throw new AppStartupException(AppStartupFailureCodes.Installation,
                $"Required installation resource could not be read: {path}.", error);
        }
    }
}

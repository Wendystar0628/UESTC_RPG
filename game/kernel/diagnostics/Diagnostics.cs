using Godot;

namespace Campus.Kernel.Diagnostics;

/// <summary>Writes diagnostics after the owning operation has handled its failure.</summary>
public static class Diagnostics
{
    /// <summary>Records an error; recovery and termination remain with the caller.</summary>
    public static void Error(string context, Exception error) => GD.PushError($"{context} {error.ToString()}");

    /// <summary>Records a warning; recovery and termination remain with the caller.</summary>
    public static void Warning(string context, Exception error) => GD.PushWarning($"{context} {error.ToString()}");
}

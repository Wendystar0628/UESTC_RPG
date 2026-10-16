using Godot;

namespace Campus.Kernel.Bake;

/// <summary>Trusted boot-pack wrapper only. This resource is never loaded from or written into the user cache.</summary>
[GlobalClass]
public partial class BootTileResource : Resource
{
    [Export] public byte[] Payload { get; set; } = [];
}

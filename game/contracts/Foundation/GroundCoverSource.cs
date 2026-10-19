namespace Campus.Contracts;

/// <summary>CPU masks owned by one map preparation; the upload owner retains them until map unload.</summary>
public sealed record GroundCoverData(GroundMaskGrid Grid, byte[] Mask1, byte[] Mask2);

/// <summary>Prepares the ground module's shared seasonal mask inputs without engine resources.</summary>
public interface IGroundCoverSource : IContentModule
{
    GroundCoverData PrepareGroundCover(BuildContext context);
}

namespace Campus.Contracts;

/// <summary>VF-B glass Reserved byte: card type and actual physical-curtain presence at this LOD.</summary>
public static class WindowCardEncoding
{
    public const byte TypeMask = 0x7f;
    public const byte PhysicalCurtainFlag = 0x80;
    public const byte Lobby = 16;
}

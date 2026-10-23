namespace Campus.Contracts.Save;

public static class SaveErrorKeys
{
    public const string Permission = "save.error.permission";
    public const string DiskFull = "save.error.disk_full";
    public const string Io = "save.error.io";
    public const string Verification = "save.error.verification";
    public const string Snapshot = "save.error.snapshot";
    public const string RestoreWorld = "save.error.restore_world";
    public const string NewerSlot = "save.error.newer_slot";
    public const string ReadOnlySlot = "save.error.read_only_slot";
    public const string Timeout = "save.error.timeout";
}

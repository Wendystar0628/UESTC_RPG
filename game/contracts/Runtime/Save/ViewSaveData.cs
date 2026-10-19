namespace Campus.Contracts.Save;

/// <summary>The view section's single DTO, shared with asynchronous restore preparation.</summary>
public sealed record ViewSaveData(ViewMode Mode, Pose Pose, NavSpace? Space, int SpeedStep, int FlySpeedStep);

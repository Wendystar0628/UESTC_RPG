using Campus.Contracts.Settings;
using Godot;

namespace Campus.Kernel.Renderer;

/// <summary>Remembers successful startup warmup for the engine's device/driver pipeline-cache identity.</summary>
internal sealed class StartupShaderCompilation
{
    private readonly ISettingsStore _settings;
    private readonly ISettingsSection _section;
    private readonly string _driver;
    private readonly string _cacheUuid;

    private StartupShaderCompilation(ISettingsStore settings, string driver, string cacheUuid)
    {
        _settings = settings;
        _section = settings.Section("ui").Child("startup");
        _driver = driver;
        _cacheUuid = cacheUuid;
        FirstShaderCompilation = _section.String("shaderDriver") != driver ||
            _section.String("shaderCacheUuid") != cacheUuid;
    }

    public bool FirstShaderCompilation { get; }

    public static StartupShaderCompilation? Capture(ISettingsStore settings)
    {
        var cacheUuid = RenderingServer.GetRenderingDevice()?.GetDevicePipelineCacheUuid();
        if (string.IsNullOrWhiteSpace(cacheUuid)) return null;
        var driver = RenderingServer.GetCurrentRenderingDriverName();
        if (string.IsNullOrWhiteSpace(driver)) return null;
        return new(settings, driver, cacheUuid);
    }

    // Called only after the final view readiness barrier succeeds; failures/cancellation leave no success record.
    public void RecordSuccess()
    {
        if (!FirstShaderCompilation) return;
        _section.Set("shaderDriver", _driver);
        _section.Set("shaderCacheUuid", _cacheUuid);
        _settings.TrySave();
    }
}

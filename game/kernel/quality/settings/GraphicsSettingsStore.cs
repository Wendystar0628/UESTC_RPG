using Campus.Contracts.Quality;
using Campus.Kernel.Settings;
using Godot;

namespace Campus.Kernel.Quality.Settings;

// WP-32's local storage seam: the service can be exercised without initializing Godot.
public interface IGraphicsSettingsStore
{
    QualityProfile? Read();
    Task Write(QualityProfile profile);
}

/// <summary>Graphics' view of the graphics section in settings.json: the preset and the knobs that differ from it.</summary>
public sealed class GraphicsSettingsStore(SettingsStore settings, QualityCatalog catalog) : IGraphicsSettingsStore
{
    private readonly SettingsSection _section = settings.Section("graphics");

    public QualityProfile? Read()
    {
        // No graphics section means a fresh install or an intentional reset.
        if (_section.Keys().Count == 0) return null;
        var preset = _section.String("preset") switch
        {
            "balanced" => QualityPreset.Balanced,
            "fine" => QualityPreset.Fine,
            "ultra" => QualityPreset.Ultra,
            _ => QualityPreset.Fine,
        };
        var overrides = new Dictionary<QualityKnob, float>();
        var knobs = _section.Child("knobs");
        foreach (var knob in QualityCatalog.All)
        {
            var name = QualityCatalog.Name(knob);
            if (!knobs.TryFloat(name, out var number)) continue;
            if (catalog.IsValid(knob, number)) overrides[knob] = number;
            else GD.PushWarning($"[WP-32] Ignoring invalid setting {name}.");
        }
        return new(preset, overrides);
    }

    public Task Write(QualityProfile profile)
    {
        _section.Clear();
        _section.Set("preset", profile.Preset.ToString().ToLowerInvariant());
        var knobs = _section.Child("knobs");
        foreach (var pair in profile.Overrides.OrderBy(pair => pair.Key))
            knobs.Set(QualityCatalog.Name(pair.Key), pair.Value);
        return settings.SaveAsync();
    }
}

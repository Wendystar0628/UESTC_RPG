using System.Text.Json;
using Campus.Contracts.Audio;
using Godot;

namespace Campus.Audio.Core;

internal sealed class AudioClipLibrary
{
    private readonly Dictionary<(string Path, bool Loop), AudioStream> _clips = [];
    private readonly HashSet<string> _missing = new(StringComparer.Ordinal);
    private readonly AudioStream?[] _ui = new AudioStream?[13];
    private readonly double[] _cooldowns = Enumerable.Repeat(.08, 13).ToArray();
    public int UiClipCount { get; private set; }

    public void LoadUi()
    {
        const string path = "res://data/audio/ui_cues.json";
        if (!Godot.FileAccess.FileExists(path)) return;
        using var document = JsonDocument.Parse(Godot.FileAccess.GetFileAsBytes(path));
        var root = document.RootElement;
        if (root.GetProperty("schema").GetInt32() != 1) throw new InvalidDataException("Unsupported UI cue schema.");
        var seen = new HashSet<UiCue>();
        foreach (var entry in root.GetProperty("cues").EnumerateArray())
        {
            if (!Enum.TryParse<UiCue>(entry.GetProperty("cue").GetString(), out var cue) || !Enum.IsDefined(cue) || !seen.Add(cue))
                throw new InvalidDataException("Unknown or duplicate UI cue.");
            var cooldown = entry.GetProperty("cooldownSeconds").GetDouble();
            if (!double.IsFinite(cooldown) || cooldown < (cue == UiCue.SliderTick ? .1 : .08) || cooldown > 2d)
                throw new InvalidDataException("Invalid UI cue cooldown.");
            _cooldowns[UiIndex(cue)] = cooldown;
            if (!entry.TryGetProperty("resourcePath", out var resource) || resource.ValueKind == JsonValueKind.Null) continue;
            var clip = Load(resource.GetString()!, false);
            _ui[UiIndex(cue)] = clip;
            if (clip is not null) UiClipCount++;
        }
    }

    public AudioStream? Ui(UiCue cue) => _ui[UiIndex(cue)];
    public double Cooldown(UiCue cue) => _cooldowns[UiIndex(cue)];

    public static void ValidateMusic()
    {
        const string path = "res://data/audio/music.json";
        if (!Godot.FileAccess.FileExists(path)) return;
        using var document = JsonDocument.Parse(Godot.FileAccess.GetFileAsBytes(path));
        var root = document.RootElement;
        if (root.GetProperty("schema").GetInt32() != 1) throw new InvalidDataException("Unsupported music cue schema.");
        var cues = root.GetProperty("cues");
        foreach (var cue in Enum.GetValues<MusicCue>())
            if (!cues.TryGetProperty(cue.ToString(), out var target) || target.GetString() != nameof(MusicCue.Silence))
                throw new InvalidDataException("Commissioned music is not installed; every music cue must remain Silence.");
    }

    public static int UiIndex(UiCue cue) => cue switch
    {
        UiCue.Focus => 0,
        UiCue.Hover => 1,
        UiCue.Confirm => 2,
        UiCue.Back => 3,
        UiCue.Toggle => 4,
        UiCue.PageOpen => 5,
        UiCue.PageClose => 6,
        UiCue.SliderTick => 7,
        UiCue.TeleportConfirm => 8,
        UiCue.TeleportArrive => 9,
        UiCue.Saved => 10,
        UiCue.ShiftDayNight => 11,
        UiCue.Denied => 12,
        _ => throw new ArgumentOutOfRangeException(nameof(cue))
    };

    public AudioStream? Load(string path, bool loop)
    {
        if (string.IsNullOrWhiteSpace(path) || !path.StartsWith("res://assets/audio/", StringComparison.Ordinal) ||
            path.Contains("..", StringComparison.Ordinal) || path.Contains('\\') ||
            !(path.EndsWith(".ogg", StringComparison.OrdinalIgnoreCase) || path.EndsWith(".wav", StringComparison.OrdinalIgnoreCase)))
            throw new ArgumentException("Audio clips must be packaged WAV/Ogg assets under res://assets/audio/.", nameof(path));
        if (_clips.TryGetValue((path, loop), out var existing)) return existing;
        if (_missing.Contains(path)) return null;
        if (!ResourceLoader.Exists(path))
        {
            _missing.Add(path);
            GD.PushWarning("Audio asset is not available; cue remains silent: " + path);
            return null;
        }
        var source = ResourceLoader.Load<AudioStream>(path);
        if (source is null) return null;
        // Do not modify ResourceLoader's shared resource or another voice's loop state.
        var stream = (AudioStream)source.Duplicate();
        switch (stream)
        {
            case AudioStreamOggVorbis ogg:
                ogg.Loop = loop;
                break;
            case AudioStreamWav wav:
                wav.LoopMode = loop ? AudioStreamWav.LoopModeEnum.Forward : AudioStreamWav.LoopModeEnum.Disabled;
                break;
            default: throw new InvalidDataException("Unsupported audio stream type: " + path);
        }
        _clips.Add((path, loop), stream);
        return stream;
    }
}

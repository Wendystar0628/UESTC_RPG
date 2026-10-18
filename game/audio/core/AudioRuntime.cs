using Campus.Contracts;
using Campus.Contracts.AppFlow;
using Campus.Contracts.Audio;
using Campus.Contracts.Maps;
using Campus.Contracts.Settings;
using Campus.Contracts.Startup;
using Godot;
using RuntimeKernel = Campus.Kernel.Registry.Kernel;
using NVector3 = System.Numerics.Vector3;

namespace Campus.Audio.Core;

[KernelService(typeof(IAudioService), Owner = "WP-53a")]
[KernelService(typeof(IAudioVoiceService), Owner = "WP-53a")]
[KernelNode("audio", Owner = "WP-53a")]
public partial class AudioRuntime : Node, IAudioService, IAudioVoiceService, IMapAware, IBootParticipant
{
    private readonly AudioBusTree _buses = new();
    private readonly AudioClipLibrary _clips = new();
    private readonly AudioMix _mix = new(AudioSettings.Default);
    private readonly double[] _lastCue = Enumerable.Repeat(double.NegativeInfinity, 13).ToArray();
    private readonly long[] _uiStreams = Enumerable.Repeat(AudioStreamPlaybackPolyphonic.InvalidId, 16).ToArray();
    private AudioSettingsStore? _store;
    private AudioVoicePool? _pool;
    private AudioStreamPlayer? _ui;
    private IWorldState? _world;
    private IVeilState? _veil;
    private IAppFlow? _app;
    private bool _initialized, _enabled, _enteredTree, _audible, _background;
    private long _frame;
    private bool _worldReset;
    private double _seconds;
    private float _routeChangeDelay = -1f;
    private int _bootPhases;

    public string Id => "audio";
    public int Order => 20;

    public AudioSettings Settings { get; private set; } = AudioSettings.Default;
    public event Action<AudioSettings>? SettingsChanged;
    public AudioSnapshot Snapshot { get; private set; } = new(0, 0, 0, 0, 0, AudioMix.SilenceDb, 0);

    public override void _EnterTree()
    {
        _enteredTree = true;
        if (_world is not null) _world.InstanceChanged += OnWorldInstanceChanged;
        _worldReset = true;
    }

    public override void _Ready()
    {
        ProcessPriority = FramePriority.Audio;
        ProcessMode = ProcessModeEnum.Always;
        _enteredTree = true;
        if (_initialized) _ui!.Play();
    }

    public Task Initialize(BootPhase phase, BootContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        var mask = phase switch
        {
            BootPhase.BeforeMap => 1,
            BootPhase.AfterMapWarm => 2,
            BootPhase.BeforeReveal => 4,
            _ => throw new ArgumentOutOfRangeException(nameof(phase))
        };
        if ((_bootPhases & mask) != 0) return Task.CompletedTask;
        if (phase == BootPhase.BeforeMap) InitializeCore(context);
        else if (!_initialized) throw new InvalidOperationException("Audio BeforeMap phase was skipped.");
        if (phase == BootPhase.BeforeReveal) AudioBusTree.Validate();
        _bootPhases |= mask;
        return Task.CompletedTask;
    }

    private void InitializeCore(BootContext context)
    {
        if (_initialized) return;
        var audio = context.Run.Audio;
        _enabled = audio.Enabled;
        var fixedSettings = audio.Fixed;
        _store = fixedSettings ? null : new AudioSettingsStore(RuntimeKernel.Get<ISettingsStore>());
        Settings = _store?.Load() ?? AudioSettings.Default;
        _mix.SetSettings(Settings, true);
        _mix.SetAudible(false, 0f);
        _buses.Initialize(audio.Limiter);
        ApplyRoute();
        _clips.LoadUi();
        AudioClipLibrary.ValidateMusic();
        _pool = new AudioVoicePool(this, _clips, _mix);
        _pool.Initialize();
        _ui = new AudioStreamPlayer
        {
            Name = "UiVoices",
            Bus = AudioBuses.Ui,
            VolumeDb = AudioMix.SilenceDb,
            Stream = new AudioStreamPolyphonic { Polyphony = _uiStreams.Length }
        };
        AddChild(_ui);
        if (_enteredTree) _ui.Play();
        _world = RuntimeKernel.Get<IWorldState>();
        if (_enteredTree) _world.InstanceChanged += OnWorldInstanceChanged;
        _veil = RuntimeKernel.Get<IVeilState>();
        _app = RuntimeKernel.Get<IAppFlow>();
        _initialized = true;
        GD.Print($"Audio ready: {AudioBusTree.Layout.Length} buses, {_clips.UiClipCount}/13 licensed UI clips, output {(_enabled ? "enabled" : "off")}");
    }

    public void ApplySettings(AudioSettings next)
    {
        next = AudioMix.Normalize(next);
        if (next == Settings) return;
        var routeChanged = next.Mono != Settings.Mono || next.OutputDevice != Settings.OutputDevice;
        Settings = next;
        _mix.SetSettings(next);
        _mix.SetBackground(_background, next.Background);
        if (routeChanged && _initialized)
        {
            _mix.SetRouteAvailable(false);
            _routeChangeDelay = .12f;
        }
        _store?.Queue(next);
        SettingsChanged?.Invoke(next);
    }

    public void PlayUi(UiCue cue)
    {
        var index = AudioClipLibrary.UiIndex(cue);
        if (!_initialized || !_enteredTree || !_enabled || !Settings.UiSounds || _mix.Gain(AudioGroup.Ui) <= 0f) return;
        if ((cue is UiCue.Hover or UiCue.Focus) && !Settings.HoverSounds) return;
        if (cue == UiCue.ShiftDayNight && _world!.Flow.Rate > 10) return;
        var gap = _clips.Cooldown(cue);
        if (_seconds - _lastCue[index] < gap || _clips.Ui(cue) is not { } clip) return;
        using var playback = _ui!.GetStreamPlayback() as AudioStreamPlaybackPolyphonic;
        if (playback is null) return;
        for (var i = 0; i < _uiStreams.Length; i++)
        {
            if (_uiStreams[i] != AudioStreamPlaybackPolyphonic.InvalidId && playback.IsStreamPlaying(_uiStreams[i])) continue;
            var id = playback.PlayStream(clip);
            if (id == AudioStreamPlaybackPolyphonic.InvalidId) return;
            _uiStreams[i] = id;
            _lastCue[index] = _seconds;
            return;
        }
    }

    public void SetMusic(MusicCue cue, float fadeSeconds = 1.5f)
    {
        if (!float.IsFinite(fadeSeconds) || fadeSeconds < 0f) throw new ArgumentOutOfRangeException(nameof(fadeSeconds));
        // Commissioned music is deliberately absent in this mainline. Every documented cue is Silence.
        _ = cue switch
        {
            MusicCue.Silence or MusicCue.Title or MusicCue.EnterNew or MusicCue.EnterContinue or MusicCue.ReturnTitle => MusicCue.Silence,
            _ => throw new ArgumentOutOfRangeException(nameof(cue))
        };
    }

    public void Cine(string cue, float seconds, float db = 0f)
    {
        if (!float.IsFinite(seconds) || seconds < 0f || !float.IsFinite(db)) throw new ArgumentOutOfRangeException(nameof(seconds));
        switch (cue)
        {
            case "title.start": SetMusic(MusicCue.Title, seconds); break;
            case "title.fadeIn": SetMusic(MusicCue.Title, seconds); break;
            case "title.fadeOut": SetMusic(MusicCue.Silence, seconds); break;
            case "duck":
                if (db >= 0f) ReleaseDuck(DuckReason.Cinematic);
                else Duck(DuckReason.Cinematic, db, seconds, seconds);
                break;
            case "amb.altitude":
                // Mainline ambience follows the actual camera altitude; authored override is deferred with cinematics.
                break;
            default: throw new ArgumentException("Unknown cinematic audio cue: " + cue, nameof(cue));
        }
    }

    public void Duck(DuckReason reason, float db, float attackSeconds, float releaseSeconds) => _mix.Duck(reason, db, attackSeconds, releaseSeconds);
    public void ReleaseDuck(DuckReason reason) => _mix.ReleaseDuck(reason);
    public int BusIndex(string name) => AudioBusTree.Index(name);
    public AudioVoiceHandle Play(in AudioVoiceRequest request) => _initialized && _enteredTree && _enabled ? _pool!.Play(request) : AudioVoiceHandle.Invalid;
    public bool IsPlaying(AudioVoiceHandle handle) => _pool?.IsPlaying(handle) ?? false;
    public void Update(AudioVoiceHandle handle, NVector3 position, float gainDb, float pitchScale = 1f) => _pool?.Update(handle, position, gainDb, pitchScale);
    public void Stop(AudioVoiceHandle handle, float fadeSeconds = .1f) => _pool?.Stop(handle, fadeSeconds);

    public override void _Process(double delta)
    {
        if (!_initialized) return;
        var dt = (float)Math.Clamp(delta, 0d, .1d);
        _seconds += dt;
        _frame++;
        if (_worldReset)
        {
            _worldReset = false;
            _pool!.StopMapVoices();
            _mix.ResetWorldAutomation();
            Array.Fill(_lastCue, double.NegativeInfinity);
        }
        var quitting = _app!.State switch
        {
            AppState.Boot or AppState.Title or AppState.Entering or AppState.Playing or AppState.Leaving => false,
            AppState.Quitting => true,
            _ => throw new InvalidOperationException("Unknown application state for audio.")
        };
        // CompleteBoot follows Reveal; the final readiness phase lets the bed enter during Reveal itself.
        var audible = _enabled && !quitting && (_audible || (_bootPhases & 4) != 0 && _veil!.Opacity < .999f);
        if (audible != _audible)
        {
            _audible = audible;
            _mix.SetAudible(audible, audible ? 3f : .3f);
            if (quitting) _store?.Flush();
        }
        _mix.UpdateVeil(_veil!.Opacity, _veil.Reason);
        _mix.Step(dt);
        _store?.Step(dt);
        if (_routeChangeDelay >= 0f)
        {
            _routeChangeDelay -= dt;
            if (_routeChangeDelay <= 0f) { ApplyRoute(); _mix.SetRouteAvailable(true); _routeChangeDelay = -1f; }
        }
        _ui!.VolumeDb = Settings.UiSounds ? AudioMix.ToDb(_mix.Gain(AudioGroup.Ui)) : AudioMix.SilenceDb;
        var voices = _pool!.Step(dt);
        var uiCount = 0;
        using (var playback = _ui.GetStreamPlayback() as AudioStreamPlaybackPolyphonic)
            if (playback is not null)
                for (var i = 0; i < _uiStreams.Length; i++)
                    if (_uiStreams[i] != AudioStreamPlaybackPolyphonic.InvalidId && playback.IsStreamPlaying(_uiStreams[i])) uiCount++;
        Snapshot = new(_frame, voices.Active + uiCount, voices.Spatial, 0, 0,
            Math.Max(AudioServer.GetBusPeakVolumeLeftDb(0, 0), AudioServer.GetBusPeakVolumeRightDb(0, 0)), 0);
    }

    private void ApplyRoute()
    {
        _buses.Mono(Settings.Mono);
        var devices = AudioServer.GetOutputDeviceList();
        var requested = Settings.OutputDevice;
        AudioServer.OutputDevice = requested is not null && devices.Contains(requested, StringComparer.Ordinal) ? requested : "Default";
    }

    public override void _Notification(int what)
    {
        if (what == NotificationApplicationFocusOut || what == NotificationApplicationFocusIn)
        {
            _background = what == NotificationApplicationFocusOut;
            _mix.SetBackground(_background, Settings.Background);
        }
        if (what == NotificationWMCloseRequest) _store?.Flush();
    }

    public void OnMapUnloading(MapId from)
    {
        _mix.BeginMapUnload();
        _pool?.StopMapVoices();
    }
    public void OnMapLoaded(IMapContext ctx) { }
    private void OnWorldInstanceChanged(long _) => _worldReset = true;

    public override void _ExitTree()
    {
        if (_world is not null) _world.InstanceChanged -= OnWorldInstanceChanged;
        _store?.Flush();
        _enteredTree = false;
    }
}

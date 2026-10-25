using Campus.Contracts;
using Campus.Audio.Core;
using Campus.Contracts.AppFlow;
using Campus.Contracts.Maps;
using Campus.Contracts.Audio;
using Campus.Contracts.Save;
using Campus.Contracts.Settings;
using Campus.Hud;
using Campus.Hud.Motion;
using Campus.Hud.Style;
using Campus.Hud.Widgets;
using Campus.Kernel.Boot;
using Campus.Kernel.Diagnostics;
using Campus.Kernel.Settings;
using Campus.Kernel.World;
using Campus.Style.Materials;
using Campus.Style.Palette;
using Godot;

namespace Campus.Tests;

/// <summary>Explicit HUD dependency assembly shared by lifetime and title-return regressions.</summary>
internal static class HudTestSupport
{
    // The fixture supplies only explicit dependencies; it never constructs the production boot graph.
    internal static HudRootDependencies Dependencies(HudSettings settings, HudModel model, MotionPolicy motion, WorldStateService world,
        TestWorldState view, TestAppFlow flow, TestInputRouter router, AudioRuntime audio, TestSaveService? saves = null, TestMapService? maps = null)
    {
        var resources = new RequiredResources();
        return new HudRootDependencies(
            Settings: settings,
            Model: model,
            Motion: motion,
            WorldControl: world,
            Flow: flow,
            Exit: new TestAppExit(allowQuit: false),
            Failure: new TestAppFailure(),
            Audio: audio,
            PaletteCatalog: new PaletteRegistrySource(resources),
            ShaderMaterials: new ShaderMaterialSource(resources),
            ThemeClock: new UiThemeClock(world, settings, motion, resources),
            InputRouter: router,
            ViewControl: view,
            Capture: new UnusedCapture(),
            Maps: maps ?? new TestMapService(),
            WorldState: world,
            Saves: saves ?? new TestSaveService(),
            PaletteProbe: new Probe(),
            Diagnostics: new DiagnosticLog(),
            Resources: resources);
    }

    private sealed class Probe : IPaletteProbe
    {
        public void SampleHorizon(Season season, WeatherVec weather, Span<System.Numerics.Vector3> srgb24) =>
            srgb24.Fill(System.Numerics.Vector3.Zero);
    }

    private sealed class UnusedCapture : IGameCapture
    {
        public Task<CaptureResult> CaptureAsync(CaptureRequest request, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<byte[]?> CaptureThumbnailAsync(CancellationToken cancellationToken = default) => throw new NotSupportedException();
    }
}

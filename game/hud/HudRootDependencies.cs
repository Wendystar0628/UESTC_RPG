using Campus.Contracts;
using Campus.Contracts.AppFlow;
using Campus.Contracts.Audio;
using Campus.Contracts.Diagnostics;
using Campus.Contracts.Maps;
using Campus.Contracts.Save;
using Campus.Hud.Motion;
using Campus.Hud.Style;

namespace Campus.Hud;

/// <summary>The HUD root's explicit dependencies, supplied by the composition root before tree entry.</summary>
public sealed record HudRootDependencies(
    HudSettings Settings,
    HudModel Model,
    MotionPolicy Motion,
    IWorldControl WorldControl,
    IAppFlow Flow,
    IAudioService Audio,
    IPaletteCatalogSource PaletteCatalog,
    IShaderMaterialSource ShaderMaterials,
    UiThemeClock ThemeClock,
    IInputRouter InputRouter,
    IViewControl ViewControl,
    IGameCapture Capture,
    IMapService Maps,
    IWorldState WorldState,
    ISaveService Saves,
    IPaletteProbe PaletteProbe,
    IDiagnosticLog Diagnostics);

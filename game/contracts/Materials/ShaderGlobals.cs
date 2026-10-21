namespace Campus.Contracts;

/// <summary>Global shader parameter names from ENG-PERF 1.5; values are owned by their registered systems.</summary>
public static class ShaderGlobals
{
    /// <summary>Sole writer: Campus.Kernel.World.WorldStateNode.</summary>
    public const string Clock = "clock";
    /// <summary>Sole writer: Campus.Kernel.World.WorldStateNode.</summary>
    public const string AnimTime = "anim_time";
    /// <summary>Sole writer: Campus.Kernel.World.WorldStateNode; value from WorldTime.AnimLoopSeconds.</summary>
    public const string AnimLoopSeconds = "anim_loop_seconds";
    /// <summary>Sole writer: Campus.Kernel.Frame.FrameClockNode.</summary>
    public const string FadeClock = "fade_clock";
    /// <summary>Sole writer: Campus.Kernel.Quality.Settings.QualityRuntime.</summary>
    public const string StepRate = "step_rate";
    /// <summary>Sole writer: Campus.Kernel.World.WorldStateNode.</summary>
    public const string Season = "season";
    /// <summary>Sole writer: Campus.Kernel.World.WorldStateNode.</summary>
    public const string SeasonAtmosphere = "season_atmosphere";
    /// <summary>Sole writer: Campus.Kernel.World.WorldStateNode.</summary>
    public const string SunDir = "sun_dir";
    /// <summary>Sole writer: Campus.Kernel.World.WorldStateNode.</summary>
    public const string MoonDir = "moon_dir";
    /// <summary>Sole writer: Campus.Kernel.World.WorldStateNode.</summary>
    public const string MoonLit = "moon_lit";
    /// <summary>Sole writer: Campus.Kernel.Lighting.KeyLightDirector.</summary>
    public const string KeyDir = "key_dir";
    /// <summary>Sole writer: Campus.Kernel.Lighting.KeyLightDirector.</summary>
    public const string KeyStrength = "key_strength";
    /// <summary>Sole writer: Campus.Kernel.Lighting.KeyLightDirector.</summary>
    public const string ShadowStrength = "shadow_strength";
    /// <summary>Sole writer: Campus.Kernel.Lighting.KeyLightDirector.</summary>
    public const string RetreatY = "retreat_y";
    /// <summary>Reserved: project.godot default only; no runtime writer.</summary>
    public const string Wind = "wind";
    /// <summary>Sole writer: Campus.Systems.Vegetation.SeasonalSurfaceNode.</summary>
    public const string Snow = "snow";
    /// <summary>Sole writer: Campus.Systems.Vegetation.SeasonalSurfaceNode; gold-leaf channel, mixed leaves/petals, ground snow divided by 1.5, reserved zero.</summary>
    public const string GroundAmt = "ground_amt";
    /// <summary>Sole writer: Campus.Systems.Sky.SkyNode.</summary>
    public const string Stars = "stars";
    /// <summary>Sole writer: Campus.Style.Palette.Keys.PaletteNode.</summary>
    public const string Fog = "fog";
    /// <summary>Sole writer: Campus.Style.Palette.Keys.PaletteNode.</summary>
    public const string FogLow = "fog_low";
    /// <summary>Reserved: project.godot default only; no runtime writer.</summary>
    public const string Medium = "medium";
    /// <summary>Reserved: project.godot default only; no runtime writer.</summary>
    public const string MediumColor = "medium_color";
    /// <summary>Sole writer: Campus.Hud.Style.UiThemeClock.</summary>
    public const string UiNight = "ui_night";
    /// <summary>Sole writer: Campus.Systems.Water.WaterSystem.</summary>
    public const string ReflectVp = "reflect_vp";
    /// <summary>Sole writer: Campus.Systems.Water.WaterSystem.</summary>
    public const string ReflectLake = "reflect_lake";
    /// <summary>Reserved: project.godot default only; no runtime writer.</summary>
    public const string Incident = "incident";
    /// <summary>Reserved: project.godot default only; no runtime writer.</summary>
    public const string IncidentRoute = "incident_route";
    /// <summary>Sole writer: Campus.Systems.Weather.Precip.PrecipNode.</summary>
    public const string WeatherA = "weather_a";
    /// <summary>Sole writer: Campus.Systems.Weather.Precip.PrecipNode.</summary>
    public const string WeatherB = "weather_b";
    /// <summary>Sole writer: Campus.Systems.Weather.Precip.PrecipNode.</summary>
    public const string WeatherFog = "weather_fog";
    /// <summary>Sole writer: Campus.Systems.Sky.SkyNode.</summary>
    public const string Cloud = "cloud";
    /// <summary>Reserved: project.godot default only; no runtime writer.</summary>
    public const string Lightning = "lightning";
    /// <summary>Reserved: project.godot default only; no runtime writer.</summary>
    public const string SnowFx = "snow_fx";
    /// <summary>Sole writer: Campus.Kernel.Lighting.KeyLightDirector.</summary>
    public const string OvercastDir = "overcast_dir";
    /// <summary>Sole writer: Campus.Kernel.Quality.Settings.QualityRuntime.</summary>
    public const string Quality = "quality";
    /// <summary>Sole writer: Campus.Kernel.Maps.MapDataSource.</summary>
    public const string MapRect = "map_rect";
    /// <summary>Sole writer: Campus.Camera.ObserverFlight.</summary>
    public const string EdgeFog = "edge_fog";
    /// <summary>Reserved: project.godot default only; no runtime writer.</summary>
    public const string InteriorLight = "interior_light";
    /// <summary>Sole writer: Campus.Style.Palette.Keys.PaletteNode.</summary>
    public const string Palette = "palette";
    /// <summary>Sole writer: Campus.Kernel.Lighting.LightChannelService.</summary>
    public const string LightChannels = "light_channels";
    /// <summary>Sole writer: Campus.Systems.Nightlight.NightlightNode.</summary>
    public const string WindowOverride = "window_override";
    /// <summary>Sole writer: Campus.Systems.Nightlight.NightlightNode.</summary>
    public const string WindowThresholds = "window_thresholds";
    /// <summary>Sole writer: Campus.Systems.Nightlight.NightlightNode.</summary>
    public const string LampGrid = "lamp_grid";
    /// <summary>Sole writer: Campus.Systems.Nightlight.NightlightNode.</summary>
    public const string LampTable = "lamp_table";
    /// <summary>Sole writer: Campus.Systems.Vegetation.SeasonalSurfaceNode.</summary>
    public const string TreeSpecies = "tree_species";
    /// <summary>Sole writer: Campus.Style.Materials.ToonLibrary.</summary>
    public const string MatTable = "mat_table";
    /// <summary>Sole writer: Campus.Style.Materials.ToonLibrary.</summary>
    public const string WindowCards = "window_cards";
    /// <summary>Sole writer: Campus.Kernel.Maps.GroundCoverTextures.</summary>
    public const string GroundMask = "ground_mask";
    /// <summary>Reserved: project.godot default only; no runtime writer.</summary>
    public const string TerrainHeight = "terrain_height";
    /// <summary>Sole writer: Campus.Systems.Water.WaterSystem.</summary>
    public const string ReflectTex = "reflect_tex";
    /// <summary>Sole writer: Campus.Systems.Sky.SkyNode.</summary>
    public const string CloudNoise = "cloud_noise";
    /// <summary>Sole writer: Campus.Systems.Weather.Precip.PrecipNode.</summary>
    public const string PrecipOcc = "precip_occ";
    /// <summary>Sole writer: Campus.Systems.Nightlight.Lamps.NightlightReflections; shared lamp/window reflection references.</summary>
    public const string ReflLamps = "refl_lamps";
    /// <summary>Sole writer: Campus.Kernel.Maps.GroundCoverTextures.</summary>
    public const string GroundMask2 = "ground_mask2";
}

/// <summary>Per-instance shader parameter names; these are not global uniforms.</summary>
public static class InstanceShaderParameters
{
    /// <summary>Player body context: indoor blend, wetness, snow and reserved zero; produced by game/camera/walk/ and written by its figure host.</summary>
    public const string FigCtx = "fig_ctx";

    /// <summary>Signed fade start on FadeClock; zero means no active fade.</summary>
    public const string FadeT0 = "fade_t0";

    /// <summary>Per-instance retreat-line height bias in metres.</summary>
    public const string RetreatBias = "retreat_bias";

    /// <summary>Enables noise cutout on vegetation shadow proxies.</summary>
    public const string ShadowProxy = "shadow_proxy";

    /// <summary>Landmark fog exemption, from zero to one; defaults to zero and is owned by game/systems/sky/.</summary>
    public const string FogExempt = "fog_exempt";

    /// <summary>Precipitation loop-box displacement in xyz and half-size in w; owned by game/systems/weather/precip/.</summary>
    public const string PrecipD = "precip_d";

    /// <summary>Precipitation direction in xyz and streak length in w; owned by game/systems/weather/precip/.</summary>
    public const string PrecipV = "precip_v";

    /// <summary>Precipitation kind, intensity, role and reserved value; owned by game/systems/weather/precip/.</summary>
    public const string PrecipK = "precip_k";
}

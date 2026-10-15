using Godot;
namespace Campus.Kernel.Renderer;

public static class ShaderNames
{
    public static readonly StringName Clock = new("clock");
    public static readonly StringName AnimTime = new("anim_time");
    public static readonly StringName FadeClock = new("fade_clock");
    public static readonly StringName StepRate = new("step_rate");
    public static readonly StringName Season = new("season");
    public static readonly StringName SunDir = new("sun_dir");
    public static readonly StringName MoonDir = new("moon_dir");
    public static readonly StringName MoonLit = new("moon_lit");
    public static readonly StringName KeyDir = new("key_dir");
    public static readonly StringName KeyStrength = new("key_strength");
    public static readonly StringName ShadowStrength = new("shadow_strength");
    public static readonly StringName RetreatY = new("retreat_y");
    public static readonly StringName Wind = new("wind");
    public static readonly StringName Snow = new("snow");
    public static readonly StringName GroundAmt = new("ground_amt");
    public static readonly StringName Veil = new("veil");
    public static readonly StringName Stars = new("stars");
    public static readonly StringName Fog = new("fog");
    public static readonly StringName FogLow = new("fog_low");
    public static readonly StringName Medium = new("medium");
    public static readonly StringName MediumColor = new("medium_color");
    public static readonly StringName UiNight = new("ui_night");
    public static readonly StringName ReflectVp = new("reflect_vp");
    public static readonly StringName ReflectLake = new("reflect_lake");
    public static readonly StringName Incident = new("incident");
    public static readonly StringName IncidentRoute = new("incident_route");
    public static readonly StringName WeatherA = new("weather_a");
    public static readonly StringName WeatherB = new("weather_b");
    public static readonly StringName WeatherFog = new("weather_fog");
    public static readonly StringName Cloud = new("cloud");
    public static readonly StringName Lightning = new("lightning");
    public static readonly StringName SnowFx = new("snow_fx");
    public static readonly StringName OvercastDir = new("overcast_dir");
    public static readonly StringName Quality = new("quality");
    public static readonly StringName MapRect = new("map_rect");
    public static readonly StringName EdgeFog = new("edge_fog");
    public static readonly StringName Shaft = new("shaft");
    public static readonly StringName InteriorLight = new("interior_light");
    public static readonly StringName Palette = new("palette");
    public static readonly StringName LightChannels = new("light_channels");
    public static readonly StringName WindowOverride = new("window_override");
    public static readonly StringName LampGrid = new("lamp_grid");
    public static readonly StringName LampTable = new("lamp_table");
    public static readonly StringName TreeSpecies = new("tree_species");
    public static readonly StringName MatTable = new("mat_table");
    public static readonly StringName GroundMask = new("ground_mask");
    public static readonly StringName TerrainHeight = new("terrain_height");
    public static readonly StringName ReflectTex = new("reflect_tex");
    public static readonly StringName CloudNoise = new("cloud_noise");
    public static readonly StringName PrecipOcc = new("precip_occ");
    public static readonly StringName ReflLamps = new("refl_lamps");
    public static readonly StringName GroundMask2 = new("ground_mask2");
}

public static class InstanceShaderNames
{
    public static readonly StringName FadeT0 = new("fade_t0");
    public static readonly StringName RetreatBias = new("retreat_bias");
    public static readonly StringName ShadowProxy = new("shadow_proxy");
    public static readonly StringName FogExempt = new("fog_exempt");
    public static readonly StringName PrecipD = new("precip_d");
    public static readonly StringName PrecipV = new("precip_v");
    public static readonly StringName PrecipK = new("precip_k");
}

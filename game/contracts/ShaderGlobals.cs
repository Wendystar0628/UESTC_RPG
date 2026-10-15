namespace Campus.Contracts;

/// <summary>Global shader parameter names from ENG-PERF 1.5; values are owned by their registered systems.</summary>
public static class ShaderGlobals
{
    public const string Clock = "clock";
    public const string AnimTime = "anim_time";
    public const string FadeClock = "fade_clock";
    public const string StepRate = "step_rate";
    public const string Season = "season";
    public const string SunDir = "sun_dir";
    public const string MoonDir = "moon_dir";
    public const string MoonLit = "moon_lit";
    public const string KeyDir = "key_dir";
    public const string KeyStrength = "key_strength";
    public const string ShadowStrength = "shadow_strength";
    public const string RetreatY = "retreat_y";
    public const string Wind = "wind";
    public const string Snow = "snow";
    public const string GroundAmt = "ground_amt";
    public const string Veil = "veil";
    public const string Stars = "stars";
    public const string Fog = "fog";
    /// <summary>Ground fog ceiling, density, lake fog strength and reserved value; written by WP-37.</summary>
    public const string FogLow = "fog_low";
    public const string Medium = "medium";
    public const string MediumColor = "medium_color";
    public const string UiNight = "ui_night";
    public const string ReflectVp = "reflect_vp";
    public const string ReflectLake = "reflect_lake";
    public const string Incident = "incident";
    public const string IncidentRoute = "incident_route";
    public const string WeatherA = "weather_a";
    public const string WeatherB = "weather_b";
    public const string WeatherFog = "weather_fog";
    public const string Cloud = "cloud";
    public const string Lightning = "lightning";
    public const string SnowFx = "snow_fx";
    public const string OvercastDir = "overcast_dir";
    public const string Quality = "quality";
    public const string MapRect = "map_rect";
    public const string EdgeFog = "edge_fog";
    public const string Shaft = "shaft";
    public const string InteriorLight = "interior_light";
    public const string Palette = "palette";
    public const string LightChannels = "light_channels";
    public const string WindowOverride = "window_override";
    public const string LampGrid = "lamp_grid";
    public const string LampTable = "lamp_table";
    public const string TreeSpecies = "tree_species";
    public const string MatTable = "mat_table";
    public const string GroundMask = "ground_mask";
    public const string TerrainHeight = "terrain_height";
    public const string ReflectTex = "reflect_tex";
    public const string CloudNoise = "cloud_noise";
    public const string PrecipOcc = "precip_occ";
    public const string ReflLamps = "refl_lamps";
    public const string GroundMask2 = "ground_mask2";
}

/// <summary>Per-instance shader parameter names; these are not global uniforms.</summary>
public static class InstanceShaderParameters
{
    /// <summary>Signed fade start on FadeClock; zero means no active fade.</summary>
    public const string FadeT0 = "fade_t0";

    /// <summary>Per-instance retreat-line height bias in metres.</summary>
    public const string RetreatBias = "retreat_bias";

    /// <summary>Enables noise cutout on vegetation shadow proxies.</summary>
    public const string ShadowProxy = "shadow_proxy";

    /// <summary>Landmark fog exemption, from zero to one; defaults to zero and is owned by WP-40.</summary>
    public const string FogExempt = "fog_exempt";
}

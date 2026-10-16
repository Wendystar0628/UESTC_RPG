using System.Globalization;
using Campus.Contracts;
using Godot;

namespace Campus.Style.Palette.Keys;

/// <summary>Debug overlay for <c>--inspect=palette</c>: the live palette texture, sky bands and key swatches (English text: the default font has no CJK).</summary>
public partial class PaletteInspector : CanvasLayer
{
    private const int SkyH = 128;
    private static readonly string[] Singles =
        ["sun.light", "ambient.sky", "shadow.1", "fog.sun", "fog.anti", "bounce.ground", "sky.cloudLit", "sky.cloudBelly", "sky.moon", "window.warm"];
    private static readonly string[] Pairs =
        ["wall", "stone.beige", "wall.dormYellow", "roof.grey", "glass", "brick", "uestc.blue", "ginkgo", "grass", "camphor", "maple", "bloom.pink"];

    private readonly PaletteNode _node;
    private readonly Image _sunImage = Image.CreateEmpty(1, SkyH, false, Image.Format.Rgba8);
    private readonly Image _antiImage = Image.CreateEmpty(1, SkyH, false, Image.Format.Rgba8);
    private readonly ImageTexture _sunTexture;
    private readonly ImageTexture _antiTexture;
    private readonly Dictionary<string, ColorRect> _swatches = new(StringComparer.Ordinal);
    private readonly Label _title = new();
    private readonly Label _readout = new();

    public PaletteInspector(PaletteNode node)
    {
        _node = node;
        Layer = 30;
        _sunTexture = ImageTexture.CreateFromImage(_sunImage);
        _antiTexture = ImageTexture.CreateFromImage(_antiImage);
        const float x0 = 880, y0 = 16;
        AddChild(new ColorRect { Position = new Vector2(x0, y0), Size = new Vector2(1024, 1048), Color = new Color(0.07f, 0.08f, 0.11f, 0.9f), MouseFilter = Control.MouseFilterEnum.Ignore });
        _title.Position = new Vector2(x0 + 16, y0 + 8);
        AddChild(_title);
        AddChild(new TextureRect
        {
            Texture = node.Texture,
            Position = new Vector2(x0 + 16, y0 + 40),
            Size = new Vector2(992, 248),
            ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
            StretchMode = TextureRect.StretchModeEnum.Scale,
            TextureFilter = CanvasItem.TextureFilterEnum.Nearest
        });
        AddChild(new Label { Position = new Vector2(x0 + 16, y0 + 292), Text = "palette 256x68 (live ImageTexture): row 0 now | 1 lamp | 2-3 interior shade/lit | 4-63 seasonal rows, x = season 0..4 | 64-67 rows 0-3 of IDs 256-511" });
        AddChild(new Label { Position = new Vector2(x0 + 16, y0 + 324), Text = "sky: sun side | anti-sun" });
        AddChild(new TextureRect { Texture = _sunTexture, Position = new Vector2(x0 + 16, y0 + 352), Size = new Vector2(110, 340), ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize, StretchMode = TextureRect.StretchModeEnum.Scale, TextureFilter = CanvasItem.TextureFilterEnum.Linear });
        AddChild(new TextureRect { Texture = _antiTexture, Position = new Vector2(x0 + 132, y0 + 352), Size = new Vector2(110, 340), ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize, StretchMode = TextureRect.StretchModeEnum.Scale, TextureFilter = CanvasItem.TextureFilterEnum.Linear });
        var y = y0 + 352f;
        foreach (var token in Singles)
        {
            AddSwatch(token, new Vector2(x0 + 270, y), new Vector2(48, 26));
            AddChild(new Label { Position = new Vector2(x0 + 326, y), Text = token });
            y += 34;
        }
        y = y0 + 352f;
        foreach (var family in Pairs)
        {
            AddSwatch(family + ".lit", new Vector2(x0 + 520, y), new Vector2(48, 26));
            AddSwatch(family + ".shade", new Vector2(x0 + 570, y), new Vector2(48, 26));
            AddChild(new Label { Position = new Vector2(x0 + 628, y), Text = family + "  lit | shade" });
            y += 34;
        }
        _readout.Position = new Vector2(x0 + 16, y0 + 770);
        AddChild(_readout);
    }

    private void AddSwatch(string token, Vector2 position, Vector2 size)
    {
        var rect = new ColorRect { Position = position, Size = size, MouseFilter = Control.MouseFilterEnum.Ignore };
        _swatches[token] = rect;
        AddChild(rect);
    }

    public void Refresh(WorldFrame w, double clock, double season)
    {
        var pixels = _node.Pixels;
        foreach (var (token, rect) in _swatches)
        {
            if (!_node.Registry.TryGet(new PaletteToken(token), out var e)) { rect.Color = new Color(0, 0, 0, 0); continue; }
            var i = e.Id * 4;
            rect.Color = new Color(pixels[i] / 255f, pixels[i + 1] / 255f, pixels[i + 2] / 255f);
        }
        FillSky(_sunImage, w.Frame, true);
        FillSky(_antiImage, w.Frame, false);
        _sunTexture.Update(_sunImage);
        _antiTexture.Update(_antiImage);
        var s = _node.Current;
        var hh = (int)Math.Floor(clock);
        var mm = (int)Math.Round((clock - hh) * 60.0);
        _title.Text = string.Create(CultureInfo.InvariantCulture,
            $"WP-37 colour script   {SeasonName(season)} {hh:00}:{mm:00}   sun {w.SunAltDeg:+0.00;-0.00} deg   x {w.X:0.00}   registry {Path.GetFileName(_node.RegistrySource)}");
        _readout.Text = string.Create(CultureInfo.InvariantCulture,
            $"r {w.Frame.V[FrameLayout.R]:0.000}   wMin {w.Frame.V[FrameLayout.WMin]:0.00}   key intensity {w.Frame.V[FrameLayout.Intensity]:0.00}   satGain {w.Frame.V[FrameLayout.SatGain]:0.00}   litLift {w.Frame.V[FrameLayout.LitLift]:0.000}\n" +
            $"snapshot rev {s.Rev}: line {s.LineDarkness:0.00}  glow {s.Glow:0.000}/{s.GlowIntensity:0.00}  vignette {s.Vignette:0.00}  exposure {s.Exposure:+0.00;-0.00} EV  stars {s.Stars:0.00}  cloud {s.CloudCover:0.00}  lampPool {s.LampPoolGain:0.0}\n" +
            $"CDL slope {s.CdlSlope.X:0.00} offset {s.CdlOffset.X:0.000} power {s.CdlPower.X:0.00} sat {s.CdlSaturation:0.00}   fog {s.Fog.X:0.00} / haze {s.Fog.Y:0.0} deg / {s.Fog.Z:0} m / {s.Fog.W:0} m   fog_low {s.FogLow.X:0} m {s.FogLow.Y:0.00} lake {s.FogLow.Z:0.0}\n" +
            $"wall {_node.HexOf("wall.lit")} / {_node.HexOf("wall.shade")}  interior {_node.HexOf("wall.lit", 2)} / {_node.HexOf("wall.lit", 3)}  shadow.1 {_node.HexOf("shadow.1")}  sun.light {_node.HexOf("sun.light")}");
    }

    private static string SeasonName(double s) => ((int)Math.Floor(((s % 4) + 4) % 4)) switch
    {
        0 => "spring",
        1 => "summer",
        2 => "autumn",
        3 => "winter",
        _ => "?"
    };

    private void FillSky(Image image, FrameVector f, bool sunSide)
    {
        var stops = _node.SkyStops;
        for (var y = 0; y < SkyH; y++)
        {
            var e = 90.0 * Math.Pow((SkyH - 1 - y) / (double)(SkyH - 1), 1.8);
            var k = 0;
            while (k < 3 && e > stops[k + 1]) k++;
            var t = Math.Clamp((e - stops[k]) / (stops[k + 1] - stops[k]), 0.0, 1.0);
            var (r, g, b) = Oklab.ToSrgbClipped(Oklab.Lerp(f.Sky(sunSide, k), f.Sky(sunSide, k + 1), t));
            image.SetPixel(0, y, new Color((float)r, (float)g, (float)b));
        }
    }
}

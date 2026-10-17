using Campus.Contracts;
using Campus.Contracts.AppFlow;
using Campus.Contracts.Atlas;
using Campus.Contracts.Maps;
using Godot;

namespace Campus.Hud.Map;

/// <summary>小地图部分：左上角常驻小地图的挂载、每帧跟随、区域名（SYS-ATLAS）。</summary>
public partial class MapHud
{
    private const float RegionHz = 10;
    private const float RegionHold = 1f;
    private const float WalkHalfWidth = 120;

    private Spring _halfWidth = new(WalkHalfWidth, 0.6f);
    private Vector2 _lastFeet;
    private long _instance = -1;
    private float _speed;
    private bool _resetMinimapView = true;
    private float _regionClock;
    private string? _regionShown;
    private string? _regionCandidate;
    private float _regionCandidateAge;

    private void WireMinimap()
    {
        _minimap.Clicked += () => Open(AtlasTab.Layers);
        _minimap.AtlasButton.Pressed += () => Open(AtlasTab.Layers);
        _minimap.IndexButton.Pressed += () => Open(AtlasTab.Index);
    }

    /// <summary>
    /// 小地图挂进 WP-35 的左上角槽位 "MinimapSlot"（槽位名由 WP-35 定，SYS-ATLAS）：U、--ui=0、过场、设置卡与存档页
    /// 打开时随槽位隐藏。没有 HudRoot 时（只有 D0 舞台）留在本节点自己的 CanvasLayer 10 上。
    /// 大地图页留在本节点自己的 CanvasLayer 10 上，打开期间由 <see cref="CoverOthers"/> 收起其余界面层。
    /// </summary>
    private void MountMinimap()
    {
        if (GetParent()?.FindChild("MinimapSlot", true, false) is not Control slot) return;
        _minimap.Reparent(slot, keepGlobalTransform: false);
        _minimap.Position = Vector2.Zero;
    }

    private void UpdateMinimap(float dt)
    {
        var feet = _observer.Feet;
        var at = new Vector2(feet.X, feet.Z);
        if (_world.Instance != _instance || _resetMinimapView)
        {
            // 新的世界实例（读档、新游戏），或不连续地回到 Playing（同一世界继续）：
            // 平滑量与标记参考直接落到新值，与冷启动恢复同一起点（ENG-ARCH）
            _instance = _world.Instance;
            _lastFeet = at;
            _speed = 0;
            _halfWidth.Snap(TargetHalfWidth(0));
            _resetMinimapView = false;
            _minimap.Markers.Invalidate();
        }
        var v = dt > 0 ? (at - _lastFeet).Length() / dt : 0;
        _lastFeet = at;
        _speed += (MathF.Min(v, 400) - _speed) * MathF.Min(1, dt * 4);
        var w = _halfWidth.Step(TargetHalfWidth(_speed), dt);
        var ppm = _minimap.ViewportHalf / w;
        var view = new MapView(at, ppm, 0);
        var size = _minimap.Viewport.Size;
        _minimap.Base.SetView(view);
        _minimap.Markers.Follow(view, size);
        var pose = _rig.Pose;
        _minimap.Player.Follow(size * 0.5f, _observer.YawDeg, pose.YawDeg, HorizontalFov(pose));
        _minimap.SetScale(ppm);
        _minimap.SetInterfaceState(Input.MouseMode == Input.MouseModeEnum.Visible);
        // 镜头在室内、地下、水下时整体不透明度 0.6；雾幕段跟着雾幕淡出、淡回（SYS-ATLAS）
        var dim = _medium.Current.Kind switch
        {
            MediumKind.Interior or MediumKind.Underground or MediumKind.Underwater => 0.6f,
            MediumKind.Air or MediumKind.FogSea => 1f,
            _ => throw new InvalidOperationException(_medium.Current.Kind.ToString()),
        };
        _minimap.Modulate = new Color(1, 1, 1, dim * (1 - Math.Clamp(_veil.Opacity, 0, 1)));
        UpdateRegion(dt, at);
    }

    /// <summary>行走固定 120 m；旁观 W = clamp(120 + 1.1·h + 2.5·v, 120, 1300)（SYS-ATLAS）。</summary>
    private float TargetHalfWidth(float speed)
    {
        switch (_observer.Mode)
        {
            case ViewMode.Walk: return WalkHalfWidth;
            case ViewMode.Fly:
                // 离地高度 h 应取常驻的低精度地形（SYS-VIEW）；界面拿不到地形，观光期地面是相对平面
                // （campus.json 的 heightNote），先按眼高计
                var h = MathF.Max(0, _observer.Eye.Y);
                return Math.Clamp(WalkHalfWidth + 1.1f * h + 2.5f * speed, WalkHalfWidth, 1300);
            default: throw new InvalidOperationException(_observer.Mode.ToString());
        }
    }

    /// <summary>每秒 10 次查 8 m 区域栅格；新区域连续保持 1 s 才换字（SYS-ATLAS）。</summary>
    private void UpdateRegion(float dt, Vector2 at)
    {
        _regionClock += dt;
        if (_regionClock < 1 / RegionHz && _regionShown is not null) return;
        var step = _regionClock;
        _regionClock = 0;
        var id = _atlas.RegionAt(at.X, at.Y) ?? "";
        if (_regionShown is null) { _regionShown = id; _regionCandidate = id; ShowRegion(); return; }
        if (id != _regionCandidate) { _regionCandidate = id; _regionCandidateAge = 0; }
        else _regionCandidateAge += step;
        if (_regionCandidate != _regionShown && _regionCandidateAge >= RegionHold) { _regionShown = _regionCandidate; ShowRegion(); }
        else if (_observer.Space is { Building: not null }) ShowRegion();
    }

    private void ShowRegion()
    {
        var text = RegionText(_regionShown);
        if (_observer.Space is { Building: not null } space) text += $" · {space.Floor + 1}F";
        _minimap.SetRegion(text);
    }

    /// <summary>"核心区 · 图书馆"：大区名 · 地点名；区域表还空着时写图名。</summary>
    private string RegionText(string? id)
    {
        if (id is { Length: > 0 } && _atlas.Regions.FirstOrDefault(region => region.Id == id) is { } place)
        {
            if (place.Parent is { } p && _atlas.Regions.FirstOrDefault(region => region.Id == p) is { } parent) return parent.Name.Zh + " · " + place.Name.Zh;
            return place.Name.Zh;
        }
        return _map?.Name.Zh ?? "";
    }
}

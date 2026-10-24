using Campus.Contracts.Atlas;

namespace Campus.Hud.Map;

/// <summary>
/// 大地图的目的地预取（SYS-ATLAS-TRAVEL）：传送卡打开即请求，悬停标记或目录项满时长才请求；同一时刻只持一份，
/// 悬停目标满时长后优先，移开回到卡片的目的地，都没有就释放。计时只按帧推进的 dt 累加。
/// 拒绝（null）不在同一目标上逐帧重试。界面不显示就绪、不等 Ready：预取失败归大地图服务，由它经地图失败通道上报。
/// </summary>
internal sealed class DestinationPrefetch(Func<MarkerId, TravelStyle, IAtlasPrefetch?> request) : IDisposable
{
    /// <summary>悬停地图纸上的标记满这么久才预取（SYS-ATLAS-TRAVEL）。</summary>
    public const float MarkerHoverSeconds = 0.3f;
    /// <summary>悬停目录项满这么久才预取（SYS-ATLAS-TRAVEL）。</summary>
    public const float IndexHoverSeconds = 0.15f;
    /// <summary>悬停按默认方式预取：传送卡的主按钮默认是传送（SYS-ATLAS-TRAVEL）。</summary>
    private const TravelStyle HoverStyle = TravelStyle.Teleport;

    private readonly Func<MarkerId, TravelStyle, IAtlasPrefetch?> _request = request ?? throw new ArgumentNullException(nameof(request));
    private (MarkerId Id, TravelStyle Style)? _card, _held;
    private MarkerId? _hover;
    private float _hoverNeed, _hoverHeld;
    private bool _hoverDue;
    private IAtlasPrefetch? _lease;

    /// <summary>传送卡打开：立即请求它的目的地。</summary>
    public void ShowCard(MarkerId id, TravelStyle style)
    {
        _card = (id, style);
        Apply();
    }

    /// <summary>传送卡收起：不再为它保留目的地。</summary>
    public void HideCard()
    {
        _card = null;
        Apply();
    }

    /// <summary>悬停换了目标就从零计时；null 是移开。同一目标的重复事件不重置计时。</summary>
    public void Hover(MarkerId? id, float dwellSeconds)
    {
        if (id == _hover) return;
        _hover = id;
        _hoverNeed = dwellSeconds;
        _hoverHeld = 0;
        _hoverDue = false;
        Apply();
    }

    /// <summary>每帧推进悬停计时；满时长时请求。</summary>
    public void Tick(float dt)
    {
        if (_hover is null || _hoverDue) return;
        _hoverHeld += dt;
        if (_hoverHeld < _hoverNeed) return;
        _hoverDue = true;
        Apply();
    }

    /// <summary>关地图、换图：卡片与悬停一起清掉并释放。</summary>
    public void Clear()
    {
        _card = null;
        _hover = null;
        _hoverDue = false;
        Apply();
    }

    public void Dispose() => Clear();

    private void Apply()
    {
        var want = _hoverDue && _hover is { } hovered ? (hovered, HoverStyle) : _card;
        if (want == _held) return;
        Release();
        if (want is not { } target) return;
        _held = target;
        _lease = _request(target.Id, target.Style);
    }

    private void Release()
    {
        _lease?.Dispose();
        _lease = null;
        _held = null;
    }
}

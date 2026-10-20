using Campus.Contracts;

namespace Campus.Systems.Nightlight.Windows;

public enum WindowLight : byte { Auto = 0, ForceOff = 128, ForceOn = 255 }
public enum WindowTint : byte { Auto = 0, Warm = 128, Cool = 255 }
public enum WindowCurtain : byte { Auto = 0, Open = 128, Drawn = 255 }

/// <summary>
/// window_override 的内容（256×256 RGBA8，纹素 = (窗号 % 256, 窗号 / 256)，只覆盖校内 0–65535 号窗）：
/// R 亮灭（0 不覆盖、128 强制灭、255 强制亮），G 冷暖（0 按哈希、128 暖、255 冷），B 窗帘（0 按哈希、128 无帘、255 拉上），A 保留 0。
/// 只在变化时上传，频率不超过 10 Hz（ENG-PERF）。只持有正式窗号快照的引用，换快照或切图时清空。
/// </summary>
public sealed class WindowOverrides
{
    public const int Side = WindowNumberingSnapshot.OverrideTextureSide;
    public const int Capacity = WindowNumberingSnapshot.Capacity;
    public const double MinUploadInterval = 0.1;

    private readonly byte[] _data = new byte[Capacity * 4];
    private double _sinceUpload = double.MaxValue;
    private int _count;

    public ReadOnlySpan<byte> Data => _data;
    public WindowNumberingSnapshot? Numbering { get; private set; }
    public bool Dirty { get; private set; } = true;
    /// <summary>当前有覆盖值的窗数。</summary>
    public int Count => _count;

    public static bool InRange(int windowId) => windowId is >= 0 and < Capacity;

    public void Bind(WindowNumberingSnapshot? numbering)
    {
        if (ReferenceEquals(Numbering, numbering)) return;
        ClearAll();
        Numbering = numbering;
    }

    public void Set(int windowId, WindowLight light, WindowTint tint = WindowTint.Auto, WindowCurtain curtain = WindowCurtain.Auto)
    {
        var numbering = Numbering ?? throw new InvalidOperationException("Bind the prepared window numbering before setting an override.");
        if (!InRange(windowId) || !numbering.ContainsId(windowId))
            throw new ArgumentOutOfRangeException(nameof(windowId), windowId, "The window is outside the current map's prepared numbering.");
        var i = windowId * 4;
        var wasSet = _data[i] != 0 || _data[i + 1] != 0 || _data[i + 2] != 0;
        var isSet = light != WindowLight.Auto || tint != WindowTint.Auto || curtain != WindowCurtain.Auto;
        if (_data[i] == (byte)light && _data[i + 1] == (byte)tint && _data[i + 2] == (byte)curtain) return;
        _data[i] = (byte)light;
        _data[i + 1] = (byte)tint;
        _data[i + 2] = (byte)curtain;
        _data[i + 3] = 0;
        _count += (isSet ? 1 : 0) - (wasSet ? 1 : 0);
        Dirty = true;
    }

    public void Clear(int windowId) => Set(windowId, WindowLight.Auto);

    public void ClearAll()
    {
        if (_count == 0 && !Dirty) return;
        Array.Clear(_data);
        _count = 0;
        Dirty = true;
    }

    /// <summary>每帧调用：脏了而且离上次上传超过 0.1 s 时返回 true，调用方随即整张 Update()。</summary>
    public bool TakeUpload(double realDt)
    {
        _sinceUpload += Math.Max(realDt, 0.0);
        if (!Dirty || _sinceUpload < MinUploadInterval) return false;
        Dirty = false;
        _sinceUpload = 0.0;
        return true;
    }
}

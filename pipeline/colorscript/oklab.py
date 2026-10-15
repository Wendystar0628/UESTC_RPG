"""OKLab / OKLCH 与 sRGB 的互换（WP-37 色彩脚本，SYS-TIME §3.1、CORE-CONVENTIONS 的"颜色"一条）。

全部用双精度纯 Python 计算，不依赖 numpy，保证同一输入在任何机器上逐位相同。
矩阵取自 Björn Ottosson 的 OKLab 定义（公有领域）。
"""

from __future__ import annotations

import math
from typing import Iterable, Sequence, Tuple

Lab = Tuple[float, float, float]
Lch = Tuple[float, float, float]
Rgb = Tuple[float, float, float]


def srgb_to_linear(c: float) -> float:
    if c <= 0.04045:
        return c / 12.92
    return ((c + 0.055) / 1.055) ** 2.4


def linear_to_srgb(c: float) -> float:
    if c <= 0.0031308:
        return 12.92 * c
    return 1.055 * (c ** (1.0 / 2.4)) - 0.055


def _cbrt(x: float) -> float:
    return math.copysign(abs(x) ** (1.0 / 3.0), x)


def linear_to_oklab(rgb: Rgb) -> Lab:
    r, g, b = rgb
    l = 0.4122214708 * r + 0.5363325363 * g + 0.0514459929 * b
    m = 0.2119034982 * r + 0.6806995451 * g + 0.1073969566 * b
    s = 0.0883024619 * r + 0.2817188376 * g + 0.6299787005 * b
    l_, m_, s_ = _cbrt(l), _cbrt(m), _cbrt(s)
    return (
        0.2104542553 * l_ + 0.7936177850 * m_ - 0.0040720468 * s_,
        1.9779984951 * l_ - 2.4285922050 * m_ + 0.4505937099 * s_,
        0.0259040371 * l_ + 0.7827717662 * m_ - 0.8086757660 * s_,
    )


def oklab_to_linear(lab: Lab) -> Rgb:
    L, a, b = lab
    l_ = L + 0.3963377774 * a + 0.2158037573 * b
    m_ = L - 0.1055613458 * a - 0.0638541728 * b
    s_ = L - 0.0894841775 * a - 1.2914855480 * b
    l, m, s = l_ ** 3, m_ ** 3, s_ ** 3
    return (
        4.0767416621 * l - 3.3077115913 * m + 0.2309699292 * s,
        -1.2684380046 * l + 2.6097574011 * m - 0.3413193965 * s,
        -0.0041960863 * l - 0.7034186147 * m + 1.7076147010 * s,
    )


def hex_to_srgb(h: str) -> Rgb:
    h = h.strip().lstrip("#")
    if len(h) != 6:
        raise ValueError(f"色值必须是 #RRGGBB：{h!r}")
    return tuple(int(h[i:i + 2], 16) / 255.0 for i in (0, 2, 4))  # type: ignore[return-value]


def srgb_to_hex(rgb: Rgb) -> str:
    return "#" + "".join(f"{to_byte(c):02X}" for c in rgb)


def to_byte(c: float) -> int:
    return int(math.floor(min(1.0, max(0.0, c)) * 255.0 + 0.5))


def hex_to_oklab(h: str) -> Lab:
    return linear_to_oklab(tuple(srgb_to_linear(c) for c in hex_to_srgb(h)))  # type: ignore[arg-type]


def lab_to_lch(lab: Lab) -> Lch:
    L, a, b = lab
    c = math.hypot(a, b)
    h = math.degrees(math.atan2(b, a)) % 360.0 if c > 1e-9 else 0.0
    return (L, c, h)


def lch_to_lab(lch: Lch) -> Lab:
    L, c, h = lch
    r = math.radians(h)
    return (L, c * math.cos(r), c * math.sin(r))


def hex_to_oklch(h: str) -> Lch:
    return lab_to_lch(hex_to_oklab(h))


def in_gamut(lin: Rgb, eps: float = 1e-7) -> bool:
    return all(-eps <= c <= 1.0 + eps for c in lin)


def oklab_to_srgb_clipped(lab: Lab) -> Rgb:
    """越出 sRGB 色域时只降彩度、不动 L（ART-STYLE §3.2 末行），二分到 1e-6。"""
    lin = oklab_to_linear(lab)
    if not in_gamut(lin):
        L, c, h = lab_to_lch(lab)
        L = min(1.0, max(0.0, L))
        lo, hi = 0.0, c
        for _ in range(40):
            mid = 0.5 * (lo + hi)
            if in_gamut(oklab_to_linear(lch_to_lab((L, mid, h)))):
                lo = mid
            else:
                hi = mid
        lin = oklab_to_linear(lch_to_lab((L, lo, h)))
    return tuple(linear_to_srgb(min(1.0, max(0.0, c))) for c in lin)  # type: ignore[return-value]


def oklab_to_hex(lab: Lab) -> str:
    return srgb_to_hex(oklab_to_srgb_clipped(lab))


def oklch_to_hex(lch: Lch) -> str:
    return oklab_to_hex(lch_to_lab(lch))


def hue_diff(h_from: float, h_to: float) -> float:
    """从 h_from 到 h_to 的最短有向角差，范围 (−180, 180]。"""
    d = (h_to - h_from) % 360.0
    if d > 180.0:
        d -= 360.0
    return d


def lerp(a: float, b: float, t: float) -> float:
    return a + (b - a) * t


def lerp_lab(p: Lab, q: Lab, t: float) -> Lab:
    return (lerp(p[0], q[0], t), lerp(p[1], q[1], t), lerp(p[2], q[2], t))


def lerp_lch(p: Lch, q: Lch, t: float) -> Lch:
    return (lerp(p[0], q[0], t), lerp(p[1], q[1], t), (p[2] + hue_diff(p[2], q[2]) * t) % 360.0)


def smoothstep(e0: float, e1: float, x: float) -> float:
    """与 GLSL 相同，允许 e0 > e1（反向）。"""
    if e0 == e1:
        return 0.0 if x < e0 else 1.0
    t = min(1.0, max(0.0, (x - e0) / (e1 - e0)))
    return t * t * (3.0 - 2.0 * t)


def smootherstep(t: float) -> float:
    t = min(1.0, max(0.0, t))
    return t * t * t * (t * (6.0 * t - 15.0) + 10.0)


def cie_lstar(lab: Lab) -> float:
    """OKLab → CIE L*（D65），给"夜间暗部 L* 不低于 12"一条用（SYS-TIME §8）。"""
    lin = oklab_to_linear(lab)
    y = 0.2126 * lin[0] + 0.7152 * lin[1] + 0.0722 * lin[2]
    y = max(0.0, y)
    return 116.0 * _cbrt(y) - 16.0 if y > 0.008856 else 903.3 * y


def mean(xs: Iterable[float]) -> float:
    xs = list(xs)
    return sum(xs) / len(xs)


def fmt_lch(lch: Sequence[float]) -> str:
    return f"L{lch[0]:.3f} C{lch[1]:.3f} h{lch[2]:.0f}"

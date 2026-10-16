"""WP-37 色板联表预览（离线）。

用法（项目 .venv）：
    python pipeline/colorscript/render_review.py [--out <目录>]

输出（默认 <工作树>/output/render-review/）：
    wp37_autumn_keyframes.png   秋季 12 张基准关键帧条带（场景小样 + 色块 + 标量 + 自检）
    wp37_colorscript_4x12.png   4 季 × 12 关键帧总表，每季下附一条 24 h 连续插值带
    wp37_default_golden.png     默认状态（秋 17:30 落金初灯）放大样张与登记值对照
    wp37_selfcheck.txt          影色自检全文（SYS-TIME §8）
    wp37_grid.json              4 × 12 格的推导结果（色值十六进制），供 WP-04、Astra 对照

场景小样是示意：按 toon 合成（影 + (受光 − 影) × 主光强度 × 阴影）在线性空间里算，再按上屏近似
（曝光、ART-STYLE §7.2 的高光肩部、CDL）出图；色块一律是调色板原值。
"""

from __future__ import annotations

import argparse
import json
import math
import os
import sys
from typing import Dict, List, Optional, Sequence, Tuple

import numpy as np
from PIL import Image, ImageDraw, ImageFont

HERE = os.path.dirname(os.path.abspath(__file__))
if HERE not in sys.path:
    sys.path.insert(0, HERE)

import colorscript as cs  # noqa: E402
import oklab as ok  # noqa: E402
import selfcheck as sc  # noqa: E402

FONT_CANDIDATES = [r"C:\Windows\Fonts\msyh.ttc", r"C:\Windows\Fonts\NotoSansSC-VF.ttf", r"C:\Windows\Fonts\simhei.ttf"]

PAPER = (246, 244, 240)
INK = (40, 44, 56)
SUB = (110, 114, 126)
RULE = (214, 210, 204)
FAIL_C = (196, 58, 48)
PASS_C = (52, 132, 92)


def font(size: int) -> ImageFont.FreeTypeFont:
    for p in FONT_CANDIDATES:
        if os.path.exists(p):
            return ImageFont.truetype(p, size)
    return ImageFont.load_default()


# ---------------------------------------------------------------- 颜色工具

def lab_rgb8(lab: ok.Lab) -> Tuple[int, int, int]:
    return tuple(ok.to_byte(c) for c in ok.oklab_to_srgb_clipped(lab))  # type: ignore[return-value]


def lab_lin(lab: ok.Lab) -> np.ndarray:
    srgb = ok.oklab_to_srgb_clipped(lab)
    return np.array([ok.srgb_to_linear(c) for c in srgb], dtype=np.float64)


def lin_to_rgb8(lin: np.ndarray) -> np.ndarray:
    lin = np.clip(lin, 0.0, 1.0)
    srgb = np.where(lin <= 0.0031308, 12.92 * lin, 1.055 * np.power(lin, 1 / 2.4) - 0.055)
    return np.clip(np.floor(srgb * 255.0 + 0.5), 0, 255).astype(np.uint8)


def post_lin(lin: np.ndarray, f: cs.Frame) -> np.ndarray:
    """上屏近似：曝光 → 高光肩部（ART-STYLE §7.2）→ CDL（sRGB 域）。输入输出都是线性。"""
    c = lin * (2.0 ** float(f.post.get("exposureEv", 0.0)))
    peak = np.max(c, axis=-1, keepdims=True)
    dd = 0.24
    np_ = 1.0 - dd * dd / (peak + dd - 0.76)
    mask = peak > 0.76
    scaled = c * np.where(mask, np_ / np.maximum(peak, 1e-6), 1.0)
    g = 1.0 - 1.0 / (0.15 * (peak - np_) + 1.0)
    shoulder = scaled * (1 - g) + np_ * g
    c = np.where(mask, shoulder, c)
    cdl = f.post.get("cdl") or {"slope": 1.0, "offset": 0.0, "power": 1.0, "saturation": 1.0}
    c = np.clip(c, 0.0, 1.0)
    srgb = np.where(c <= 0.0031308, 12.92 * c, 1.055 * np.power(c, 1 / 2.4) - 0.055)
    srgb = np.clip(srgb * cdl["slope"] + cdl["offset"], 0.0, 1.0) ** cdl["power"]
    luma = (srgb * np.array([0.2126, 0.7152, 0.0722])).sum(axis=-1, keepdims=True)
    srgb = np.clip(luma + (srgb - luma) * cdl["saturation"], 0.0, 1.0)
    return np.where(srgb <= 0.04045, srgb / 12.92, ((srgb + 0.055) / 1.055) ** 2.4)


# ---------------------------------------------------------------- 场景小样

class Det:
    """确定性的小哈希序列（只给预览排星星、窗灯）。"""

    def __init__(self, seed: int):
        self.s = seed & 0xFFFFFFFF

    def next(self) -> float:
        self.s = (1664525 * self.s + 1013904223) & 0xFFFFFFFF
        return self.s / 4294967296.0


SKY_TOP_DEG = 60.0

SEASON_TREES = {
    "spring": ("bloom.pink", "ginkgo"),
    "summer": ("ginkgo", "camphor"),
    "autumn": ("ginkgo", "maple"),
    "winter": ("tree.bare", "camphor"),
}


def toon(m: cs.MaterialResult, ks: float, lit_factor: float = 1.0) -> np.ndarray:
    lit, shade = lab_lin(m.lit), lab_lin(m.shade)
    return shade + (lit - shade) * ks * lit_factor


def render_card(src: cs.Sources, f: cs.Frame, mats: Dict[str, cs.MaterialResult], W: int, H: int,
                scale: int = 2) -> Image.Image:
    w, h = W * scale, H * scale
    horizon = int(h * 0.56)
    ks = cs.key_strength_for(f)
    ss = cs.shadow_strength_for(f)
    shadow_lit = 1.0 - ss            # 投影里还剩的受光比例
    season = f.season
    # 天空：竖向 5 段按 skyStopsDeg 的仰角排（地平线 → 天顶），横向太阳侧 → 背阳侧。
    # 小样的天空画到仰角 SKY_TOP_DEG，地平线附近按 0.6 次方展开，好看清 0–25° 的色带
    stops_t = np.array(src.sky_stops) / 90.0
    rows = horizon
    frac = np.clip((horizon - np.arange(rows) - 0.5) / horizon, 0, 1)
    elev = SKY_TOP_DEG * frac ** 1.6
    t = elev / 90.0

    def band(cols: List[ok.Lab]) -> np.ndarray:
        labs = np.array(cols)
        out = np.zeros((rows, 3))
        for i in range(3):
            out[:, i] = np.interp(t, stops_t, labs[:, i])
        return out

    sun_lab, anti_lab = band(f.sky_sun), band(f.sky_anti)
    xs = np.linspace(0, 1, w)
    bx = np.clip((xs - 0.22) / (0.85 - 0.22), 0, 1)
    bx = bx * bx * (3 - 2 * bx)
    sky = sun_lab[:, None, :] * (1 - bx[None, :, None]) + anti_lab[:, None, :] * bx[None, :, None]
    # 霾带：贴着地平线向雾色混
    haze_px = max(2.0, f.fog_scalars.get("hazeDeg", 2.5) * 0.020 * h)
    fog_row = np.array(f.fog_sun)[None, :] * (1 - bx[:, None]) + np.array(f.fog_anti)[None, :] * bx[:, None]
    dist = (horizon - np.arange(rows) - 0.5)[:, None, None]
    hw = np.exp(-np.maximum(dist, 0) / haze_px) * 0.75
    sky = sky * (1 - hw) + fog_row[None, :, :] * hw
    sky_lin = _lab_array_to_lin(sky)
    img = np.zeros((h, w, 3))
    img[:horizon] = sky_lin
    # 地面（先填草，远处向雾色混）
    grass = mats["grass"]
    ground_tok = "snow" if season == "winter" and "snow" in mats else "grass"
    g_m = mats[ground_tok]
    g_lit = toon(g_m, ks)
    gy = np.arange(h - horizon)
    near = (gy / max(1, (h - horizon - 1)))[:, None, None]
    fog_amt = (1 - near) ** 2.2 * 0.55
    fog_lin = _lab_array_to_lin(fog_row[None, :, :])[0]
    img[horizon:] = g_lit[None, None, :] * (1 - fog_amt) + fog_lin[None, :, :] * fog_amt
    pil = Image.fromarray(lin_to_rgb8(post_lin(img, f)))
    d = ImageDraw.Draw(pil)

    def col(lin: np.ndarray, fogk: float = 0.0, fog_side: float = 0.5) -> Tuple[int, int, int]:
        if fogk > 0:
            fl = lab_lin(ok.lerp_lab(f.fog_sun, f.fog_anti, fog_side))
            lin = lin * (1 - fogk) + fl * fogk
        return tuple(int(v) for v in lin_to_rgb8(post_lin(lin[None, :], f))[0])  # type: ignore[return-value]

    def line_for(lin: np.ndarray) -> Tuple[int, int, int]:
        lab = ok.linear_to_oklab(tuple(float(v) for v in np.clip(lin, 0, 1)))  # type: ignore[arg-type]
        return col(lab_lin(cs.line_color(lab, f)))

    lw = max(1, scale)
    # 星星
    stars = f.sky_fx.get("stars", 0.0)
    if stars > 0.02:
        r = Det(7)
        for _ in range(int(140 * stars)):
            x, y = r.next() * w, r.next() * horizon * 0.85
            a = (0.35 + 0.65 * r.next()) * stars
            base = np.array(pil.getpixel((int(x), int(y))), dtype=float)
            c = base + (255 - base) * a * 0.8
            d.point((x, y), fill=tuple(int(v) for v in c))
    # 日月盘（按同一条仰角映射定位）
    def elev_y(deg: float) -> float:
        return horizon - horizon * (max(0.0, deg) / SKY_TOP_DEG) ** (1 / 1.6)

    if f.key_source == "sun" or (f.sky_body == "sun" and f.alt > -1.0):
        if f.alt > -1.0:
            cy = elev_y(f.alt) if f.alt > 0 else horizon + 0.004 * h
            rr = 0.035 * h
            L, C, hh = ok.lab_to_lch(f.key_color)
            disk = ok.lch_to_lab((min(0.99, L + 0.08), C * (1.4 if f.alt < 8 else 0.4), hh))
            d.ellipse((0.07 * w - rr, cy - rr, 0.07 * w + rr, cy + rr), fill=col(lab_lin(disk)))
    if f.key_source == "moon" or f.id in ("nightfall",):
        malt = 4.0 if f.id == "nightfall" else 26.0
        cy = elev_y(malt)
        rr = 0.028 * h
        moon = ok.hex_to_oklab("#FFF3D6") if malt < 10 else ok.lerp_lab(ok.hex_to_oklab("#FFF3D6"), f.key_color, 0.5)
        d.ellipse((0.07 * w - rr, cy - rr, 0.07 * w + rr, cy + rr), fill=col(lab_lin(moon)))
    # 云
    amt = f.sky_fx.get("cloudAmount", 0.1)
    clouds = [(0.62, 0.20, 0.16), (0.34, 0.34, 0.10), (0.84, 0.40, 0.12), (0.10, 0.12, 0.09)]
    ncl = max(1, int(round(amt * 10)))
    for i, (cx, cy, cr) in enumerate(clouds[:min(len(clouds), ncl)]):
        _cloud(d, f, cx * w, cy * horizon, cr * w * (0.7 + amt), col, lw)
    # 远景剪影（雾里）
    far = mats["wall"]
    r = Det(11)
    x = 0.0
    while x < w:
        bw = (0.03 + 0.05 * r.next()) * w
        bh = (0.02 + 0.06 * r.next()) * h
        side = min(1.0, x / w)
        d.rectangle((x, horizon - bh, x + bw, horizon), fill=col(toon(far, ks, 0.5), 0.80, side))
        x += bw * (0.9 + 0.3 * r.next())
    # 地面：小路（石材）与投影
    stone = mats["stone.beige"]
    path = [(0.30 * w, h), (0.52 * w, h), (0.60 * w, horizon + 0.02 * h), (0.56 * w, horizon + 0.02 * h)]
    d.polygon(path, fill=col(toon(stone, ks), 0.05))
    # 秋季：只在银杏树冠下铺一圈落叶（SYS-TIME §4），草坪、铺地、落叶三种地面分开
    if season == "autumn" and "ginkgo" in mats:
        gk = mats["ginkgo"]
        d.ellipse((0.08 * w, horizon + 0.15 * h, 0.36 * w, horizon + 0.22 * h), fill=col(toon(gk, ks), 0.04))
    # 主楼前的铺地（浅灰白铺装），与草坪分开
    pave = mats["wall.greyWhite"]
    d.polygon([(0.33 * w, horizon + 0.135 * h), (0.53 * w, horizon + 0.105 * h), (0.60 * w, horizon + 0.17 * h),
               (0.36 * w, horizon + 0.215 * h)], fill=col(toon(pave, ks), 0.05))
    # 主楼（暖米白墙）
    bx0, bx1, bx2 = 0.40 * w, 0.52 * w, 0.84 * w
    top0, top1 = horizon - 0.30 * h, horizon - 0.34 * h
    base0, base1 = horizon + 0.10 * h, horizon + 0.07 * h
    wall = mats["wall"]
    roof = mats["roof.grey"]
    glass = mats["glass"]
    # 楼的投影（向右后方）
    shadow_poly = [(bx1, base0), (bx2, base1), (bx2 + 0.16 * w, base1 + 0.06 * h), (bx1 + 0.12 * w, base0 + 0.10 * h)]
    g_shadow = toon(g_m, ks, shadow_lit)
    d.polygon(shadow_poly, fill=col(g_shadow, 0.08))
    lit_face = [(bx0, top0 + 0.02 * h), (bx1, top0), (bx1, base0), (bx0, base0 - 0.02 * h)]
    shade_face = [(bx1, top0), (bx2, top1), (bx2, base1), (bx1, base0)]
    roof_poly = [(bx0, top0 + 0.02 * h), (bx1, top0), (bx2, top1), (bx2 - 0.10 * w, top1 - 0.025 * h), (bx0 + 0.04 * w, top0 - 0.005 * h)]
    c_lit, c_sh = toon(wall, ks), lab_lin(wall.shade)
    d.polygon(lit_face, fill=col(c_lit, 0.06), outline=line_for(c_lit), width=lw)
    d.polygon(shade_face, fill=col(c_sh, 0.06), outline=line_for(c_sh), width=lw)
    d.polygon(roof_poly, fill=col(toon(roof, ks), 0.06), outline=line_for(toon(roof, ks)), width=lw)
    # 窗：玻璃 + 按窗灯比例亮起
    win_frac = float(f.lights.get("windows", 0.0)) if f.lights else _windows_from_alt(f)
    wl = lab_lin(ok.hex_to_oklab("#FFE0A8"))
    wc = lab_lin(ok.hex_to_oklab("#EEF4FF"))
    rr2 = Det(23)
    for face, (xa, xb, ya, yb, lit_side) in (("lit", (bx0, bx1, top0, base0, True)), ("sh", (bx1, bx2, top0, base0, False))):
        ncol = 3 if face == "lit" else 7
        for ri in range(5):
            for ci in range(ncol):
                u0 = (ci + 0.25) / ncol
                u1 = (ci + 0.75) / ncol
                v0 = 0.10 + ri * 0.17
                v1 = v0 + 0.09
                if face == "lit":
                    xa0, xa1 = bx0 + (bx1 - bx0) * u0, bx0 + (bx1 - bx0) * u1
                    ya0 = top0 + 0.02 * h * (1 - u0) + (base0 - top0) * v0
                    ya1 = top0 + 0.02 * h * (1 - u1) + (base0 - top0) * v1
                    poly = [(xa0, ya0), (xa1, ya0 - 0.003 * h), (xa1, ya1 - 0.003 * h), (xa0, ya1)]
                else:
                    xa0, xa1 = bx1 + (bx2 - bx1) * u0, bx1 + (bx2 - bx1) * u1
                    yt0 = top0 + (top1 - top0) * u0
                    yt1 = top0 + (top1 - top0) * u1
                    yb0 = base0 + (base1 - base0) * u0
                    yb1 = base0 + (base1 - base0) * u1
                    poly = [(xa0, yt0 + (yb0 - yt0) * v0), (xa1, yt1 + (yb1 - yt1) * v0),
                            (xa1, yt1 + (yb1 - yt1) * v1), (xa0, yt0 + (yb0 - yt0) * v1)]
                if rr2.next() < win_frac:
                    lamp = wl if rr2.next() < 0.67 else wc
                    d.polygon(poly, fill=col(lamp * 1.0))
                else:
                    gcol = toon(glass, ks) if lit_side else lab_lin(glass.shade)
                    d.polygon(poly, fill=col(gcol, 0.04))
    # 远一点的石材楼
    sx0, sx1 = 0.13 * w, 0.32 * w
    stop_ = horizon - 0.17 * h
    d.polygon([(sx0, stop_), (sx1, stop_), (sx1, horizon + 0.03 * h), (sx0, horizon + 0.03 * h)],
              fill=col(lab_lin(stone.shade) if False else toon(stone, ks), 0.22, 0.0),
              outline=line_for(toon(stone, ks)), width=lw)
    d.polygon([(sx1, stop_), (sx1 + 0.06 * w, stop_ + 0.012 * h), (sx1 + 0.06 * w, horizon + 0.028 * h), (sx1, horizon + 0.03 * h)],
              fill=col(lab_lin(stone.shade), 0.22, 0.2), outline=line_for(lab_lin(stone.shade)), width=lw)
    # 跑道（砖红）
    brick = mats["brick"]
    d.polygon([(0.70 * w, h), (w, h), (w, h - 0.07 * h), (0.78 * w, h - 0.05 * h)], fill=col(toon(brick, ks)),
              outline=line_for(toon(brick, ks)), width=lw)
    # 树
    t_main, t_second = SEASON_TREES[season]
    _tree(d, mats, t_main, 0.22 * w, horizon + 0.16 * h, 0.13 * w, ks, ss, col, line_for, lw, f, trunk=mats.get("tree.bare"),
          bare=(t_main == "tree.bare"))
    _tree(d, mats, t_second, 0.92 * w, horizon + 0.02 * h, 0.07 * w, ks, ss, col, line_for, lw, f, trunk=mats.get("tree.bare"),
          fog=0.15)
    if season == "winter":
        _snow_caps(d, mats, ks, col, (roof_poly, bx0, bx2, top0, top1), w, h)
    # 路灯
    street = float(f.lights.get("street", 0.0)) if f.lights else 0.0
    for lx in (0.62, 0.68):
        px, py = lx * w, horizon + 0.06 * h + (lx - 0.62) * 0.5 * h
        pole = mats["frame.alu"]
        d.line((px, py, px, py - 0.10 * h), fill=col(toon(pole, ks)), width=lw)
        if street > 0.05:
            gl = wl * (0.6 + 0.4 * street)
            rr = 0.012 * h
            d.ellipse((px - rr, py - 0.10 * h - rr, px + rr, py - 0.10 * h + rr), fill=col(gl * 1.3))
    pil = pil.resize((W, H), Image.LANCZOS)
    return pil


def _windows_from_alt(f: cs.Frame) -> float:
    return max(0.0, min(0.85, ok.smoothstep(9.0, 2.0, f.alt) * 0.85))


def _lab_array_to_lin(arr: np.ndarray) -> np.ndarray:
    L, a, b = arr[..., 0], arr[..., 1], arr[..., 2]
    l_ = L + 0.3963377774 * a + 0.2158037573 * b
    m_ = L - 0.1055613458 * a - 0.0638541728 * b
    s_ = L - 0.0894841775 * a - 1.2914855480 * b
    l, m, s = l_ ** 3, m_ ** 3, s_ ** 3
    r = 4.0767416621 * l - 3.3077115913 * m + 0.2309699292 * s
    g = -1.2684380046 * l + 2.6097574011 * m - 0.3413193965 * s
    bb = -0.0041960863 * l - 0.7034186147 * m + 1.7076147010 * s
    return np.clip(np.stack([r, g, bb], axis=-1), 0.0, 1.0)


def _cloud(d: ImageDraw.ImageDraw, f: cs.Frame, cx: float, cy: float, r: float, col, lw: int) -> None:
    lit = lab_lin(f.colors["sky.cloudLit"])
    rim = lab_lin(f.colors["sky.cloudRim"])
    belly = lab_lin(f.colors["sky.cloudBelly"])
    deep = lab_lin(f.colors["sky.cloudDeep"])
    blobs = [(-0.55, 0.10, 0.45), (0.0, -0.05, 0.6), (0.55, 0.08, 0.42), (0.25, 0.18, 0.40), (-0.25, 0.20, 0.38)]
    for dx, dy, rr in blobs:      # 云腹
        x, y, q = cx + dx * r, cy + dy * r + 0.12 * r, rr * r
        d.ellipse((x - q, y - q * 0.7, x + q, y + q * 0.7), fill=col(belly))
    for dx, dy, rr in blobs[:3]:  # 最暗处
        x, y, q = cx + dx * r, cy + dy * r + 0.30 * r, rr * r * 0.6
        d.ellipse((x - q, y - q * 0.35, x + q, y + q * 0.35), fill=col(deep))
    for dx, dy, rr in blobs:      # 受光顶缘（偏太阳侧，即左上）
        x, y, q = cx + dx * r - 0.06 * r, cy + dy * r - 0.06 * r, rr * r * 0.82
        d.ellipse((x - q, y - q * 0.62, x + q, y + q * 0.42), fill=col(lit))
    x, y, q = cx - 0.62 * r, cy + 0.02 * r, 0.30 * r     # 金边
    d.arc((x - q, y - q * 0.6, x + q, y + q * 0.6), 140, 250, fill=col(rim), width=max(2, lw * 2))


def _tree(d, mats, token, cx, base_y, r, ks, ss, col, line_for, lw, f, trunk=None, fog=0.0, bare=False):
    tr = trunk if trunk is not None else None
    if tr is not None:
        tc = toon(tr, ks)
        d.rectangle((cx - 0.06 * r, base_y - 0.9 * r, cx + 0.06 * r, base_y), fill=col(tc, fog), outline=line_for(tc), width=lw)
    if token not in mats:
        return
    m = mats[token]
    if bare:
        # 裸枝笔触：几根斜枝
        c = col(toon(m, ks), fog)
        for ang in (-50, -25, 0, 25, 55):
            a = math.radians(ang - 90)
            d.line((cx, base_y - 0.8 * r, cx + math.cos(a) * r, base_y - 0.8 * r + math.sin(a) * r), fill=c, width=lw * 2)
        return
    lit, shade = lab_lin(m.lit), lab_lin(m.shade)
    deep = lab_lin(m.deep) if m.deep is not None else shade
    cy = base_y - 1.35 * r
    blobs = [(-0.45, 0.15, 0.55), (0.40, 0.18, 0.55), (0.0, -0.30, 0.62), (-0.10, 0.35, 0.5), (0.35, -0.10, 0.45)]
    for dx, dy, rr in blobs:      # 深影打底
        x, y, q = cx + dx * r, cy + dy * r, rr * r
        d.ellipse((x - q, y - q, x + q, y + q), fill=col(deep, fog), outline=line_for(deep), width=lw)
    for dx, dy, rr in blobs:      # 影
        x, y, q = cx + dx * r - 0.08 * r, cy + dy * r - 0.06 * r, rr * r * 0.88
        d.ellipse((x - q, y - q, x + q, y + q), fill=col(shade, fog))
    for dx, dy, rr in blobs:      # 受光（左上，太阳侧）
        x, y, q = cx + dx * r - 0.20 * r, cy + dy * r - 0.17 * r, rr * r * 0.62
        c = shade + (lit - shade) * ks
        d.ellipse((x - q, y - q, x + q, y + q), fill=col(c, fog))
    if token == "ginkgo" and f.season == "autumn" or token == "maple":
        pass


def _snow_caps(d, mats, ks, col, roof, w, h):
    snow = mats.get("snow")
    if snow is None:
        return
    roof_poly, bx0, bx2, top0, top1 = roof
    d.polygon(roof_poly, fill=col(toon(snow, ks)))


# ---------------------------------------------------------------- 版式

def swatch(d: ImageDraw.ImageDraw, x: int, y: int, wdt: int, hgt: int, lab: ok.Lab, outline=RULE) -> None:
    d.rectangle((x, y, x + wdt - 1, y + hgt - 1), fill=lab_rgb8(lab), outline=outline)


def text(d, xy, s, size=12, fill=INK, anchor="la"):
    d.text(xy, s, font=font(size), fill=fill, anchor=anchor)


def hhmm(c: float) -> str:
    c = c % 24.0
    hh = int(c)
    mm = int(round((c - hh) * 60))
    if mm == 60:
        hh, mm = (hh + 1) % 24, 0
    return f"{hh:02d}:{mm:02d}"


def frame_title(f: cs.Frame) -> str:
    return f"{f.name} · {hhmm(f.clock)} · h {f.alt:+.1f}°"


def render_autumn_strip(src: cs.Sources, grid: dict, status, out_path: str) -> None:
    frames = grid["frames"]["autumn"]
    colw, card_h = 300, 196
    probe = src.probe["preview"]
    mat_rows = probe["stripMaterials"] + probe["stripVegetation"]
    row_h = 22
    height = 70 + 34 + card_h + 16 + 150 + 16 + row_h * (len(mat_rows) + 1) + 16 + 170 + 40
    img = Image.new("RGB", (colw * len(frames) + 40, height), PAPER)
    d = ImageDraw.Draw(img)
    text(d, (20, 16), "WP-37 秋季 12 张基准关键帧（SYS-TIME §3.2）· 色板条带", 22)
    text(d, (20, 46), "场景小样按上屏近似（toon 合成 + 曝光 + 高光肩部 + CDL）；色块为调色板原值。受光 | 影 | 室内影（第 2 行）| 室内受光（第 3 行）；植被另有深影。", 13, SUB)
    for i, f in enumerate(frames):
        x0 = 20 + i * colw
        y = 80
        fails, passes = status.get(("autumn", f.id), (0, 0))
        text(d, (x0, y), f.name + ("（默认）" if f.cls == "default" else ""), 17)
        text(d, (x0 + colw - 16, y + 2), f"{hhmm(f.clock)}  h {f.alt:+.1f}°", 13, SUB, "ra")
        y += 28
        mats = grid["materials"][("autumn", f.id)]
        img.paste(render_card(src, f, mats, colw - 16, card_h), (x0, y))
        y += card_h + 10
        # 天空两列 + 全局随帧色
        for j in range(5):
            swatch(d, x0, y + (4 - j) * 22, 34, 22, f.sky_sun[j], outline=None)
            swatch(d, x0 + 36, y + (4 - j) * 22, 34, 22, f.sky_anti[j], outline=None)
        text(d, (x0, y + 112), "日 / 背日", 11, SUB)
        globals_ = [("sun.light", f.key_color), ("ambient.sky", ok.lch_to_lab((0.75, *f.ambient))),
                    ("shadow.1", mats["shadow.1"].shade), ("fog.sun", f.fog_sun), ("fog.anti", f.fog_anti),
                    ("bounce", f.colors["bounce.ground"]), ("interior", ok.lch_to_lab((0.80, *f.ab_colors["interior.warm"]))),
                    ("shaft.far", f.colors["shaft.far"]), ("rim", f.colors["rim"]), ("glare", f.colors["glass.glare"]),
                    ("line.w", f.line_warm), ("line.c", f.line_cool),
                    ("cloudLit", f.colors["sky.cloudLit"]), ("cloudRim", f.colors["sky.cloudRim"]),
                    ("belly", f.colors["sky.cloudBelly"]), ("deep", f.colors["sky.cloudDeep"])]
        for j, (nm, lab) in enumerate(globals_):
            cx, cy = x0 + 80 + (j % 4) * 50, y + (j // 4) * 34
            swatch(d, cx, cy, 46, 20, lab)
            text(d, (cx, cy + 21), nm, 9, SUB)
        y += 136 + 16
        # 材质表
        text(d, (x0, y), "材质", 11, SUB)
        for j, hd in enumerate(("受光", "影", "室内影", "室内受光", "深影")):
            text(d, (x0 + 96 + j * 40, y), hd, 10, SUB)
        y += row_h - 4
        for t in mat_rows:
            m = mats[t]
            nm = t if len(t) <= 14 else t[:13] + "…"
            text(d, (x0, y + 3), nm, 11)
            for j, lab in enumerate((m.lit, m.shade, m.shade_in, m.lit_in)):
                swatch(d, x0 + 96 + j * 40, y, 38, row_h - 3, lab)
            if m.deep is not None:
                swatch(d, x0 + 96 + 4 * 40, y, 24, row_h - 3, m.deep)
            y += row_h
        y += 12
        # 标量
        lines = [
            f"r {f.r:.2f} · w_min {f.w_min:.2f} · 天光 C{f.ambient[0]:.3f} h{f.ambient[1]:.0f}",
            f"主光 {f.key_source} 强度 {f.intensity:.2f} · 饱和 {f.sat_gain:.2f} · lift {f.lit_lift:.3f}",
            f"描线加深 {f.line_darken:.2f} · 曝光 {f.post['exposureEv']:+.2f} EV",
            f"柔光 {f.post['glowBloom']:.3f} / 强度 {f.post['glowIntensity']:.2f}",
            f"雾 {f.fog_scalars['startM']:.0f} m / {f.fog_scalars['silhouetteM']:.0f} m · 密度 {f.fog_scalars['density']:.2f} · 霾 {f.fog_scalars['hazeDeg']:.1f}°",
            f"星空 {f.sky_fx['stars']:.2f} · 云量 {f.sky_fx['cloudAmount']:.2f} · 窗灯 {f.lights.get('windows', 0):.0%}",
            f"墙 受光 L{mats['wall'].lit[0]:.3f} 影 L{mats['wall'].shade[0]:.3f} ΔL {mats['wall'].lit[0] - mats['wall'].shade[0]:.3f}",
            f"情绪：{f.mood}",
        ]
        for ln in lines:
            text(d, (x0, y), ln, 11)
            y += 17
        y += 4
        text(d, (x0, y), f"影色自检 PASS {passes} · FAIL {fails}", 12, FAIL_C if fails else PASS_C)
    img.save(out_path, optimize=False)


def render_4x12(src: cs.Sources, grid: dict, curves: Dict[str, cs.DayCurve], status, out_path: str) -> None:
    cw, ch = 248, 150
    left = 70
    sw_h = 18
    strip_h = 54
    row_h = 30 + ch + 6 + sw_h * 2 + 8 + strip_h + 26
    W = left + cw * 12 + 20
    H = 90 + row_h * 4 + 20
    img = Image.new("RGB", (W, H), PAPER)
    d = ImageDraw.Draw(img)
    text(d, (20, 16), "WP-37 色板联表 · 4 季 × 12 关键帧（秋季基准 + 四季增量，SYS-TIME §3.2、§3.3）", 22)
    text(d, (20, 46), "每格：场景小样（上屏近似）；下方两行色块：天空地平线 / 天顶 · 主光 · 墙受光 / 墙影 · shadow.1 · 季树受光 / 影 / 深影 · 草受光 / 影；钟点是该季到达同一太阳高度的时刻。每季下方是 24 h 连续插值带。", 12, SUB)
    for si, s in enumerate(cs.SEASONS):
        y0 = 80 + si * row_h
        text(d, (20, y0 + 30 + ch // 2), cs.SEASON_NAMES[s], 30, INK, "lm")
        frames = grid["frames"][s]
        tree = SEASON_TREES[s][0] if SEASON_TREES[s][0] != "tree.bare" else "ginkgo"
        for i, f in enumerate(frames):
            x0 = left + i * cw
            mats = grid["materials"][(s, f.id)]
            fails, passes = status.get((s, f.id), (0, 0))
            text(d, (x0, y0 + 4), f.name, 14)
            text(d, (x0 + cw - 12, y0 + 6), f"{hhmm(f.clock)}  {f.alt:+.1f}°", 11, SUB, "ra")
            img.paste(render_card(src, f, mats, cw - 10, ch), (x0, y0 + 26))
            if fails:
                d.rectangle((x0, y0 + 26, x0 + cw - 11, y0 + 26 + ch - 1), outline=FAIL_C, width=3)
            yy = y0 + 30 + ch + 2
            row1 = [f.sky_sun[0], f.sky_anti[0], f.sky_sun[4], f.key_color, mats["wall"].lit, mats["wall"].shade]
            row2 = [mats["shadow.1"].shade, mats[tree].lit, mats[tree].shade, mats[tree].deep or mats[tree].shade,
                    mats["grass"].lit, mats["grass"].shade]
            for j, lab in enumerate(row1):
                swatch(d, x0 + j * 39, yy, 38, sw_h, lab, outline=None)
            for j, lab in enumerate(row2):
                swatch(d, x0 + j * 39, yy + sw_h, 38, sw_h, lab, outline=None)
        # 24 h 连续带
        ys = y0 + 30 + ch + 2 + sw_h * 2 + 8
        _day_strip(src, d, img, curves[s], s, left, ys, cw * 12 - 10, strip_h)
    img.save(out_path, optimize=False)


def _day_strip(src, d, img, curve, season, x0, y0, wdt, hgt):
    """24 h 连续插值：上 = 地平线（太阳侧 / 背阳侧各半，即 IPaletteProbe 的取样），中 = 天顶，下 = 墙受光 / 墙影的上屏合成。"""
    n = wdt
    wall_alb = ok.hex_to_oklab(next(m["albedo"] for m in src.probe["materials"] if m["token"] == "wall"))
    band = np.zeros((hgt, n, 3), dtype=np.uint8)
    h3 = hgt // 3
    for i in range(n):
        c = 24.0 * i / n
        f = cs.frame_at(src, curve, season, c)
        hor = ok.lerp_lab(f.sky_sun[0], f.sky_anti[0], 0.5)
        zen = f.sky_sun[4]
        lit = cs.derive_lit(src, wall_alb, f)
        sh = cs.derive_shade(src, wall_alb, lit, f, "building")
        ks = cs.key_strength_for(f)
        band[:h3, i] = lab_rgb8(hor)
        band[h3:2 * h3, i] = lab_rgb8(zen)
        band[2 * h3:2 * h3 + h3 // 2, i] = lab_rgb8(lit if ks > 0.5 else ok.lerp_lab(sh, lit, ks))
        band[2 * h3 + h3 // 2:, i] = lab_rgb8(sh)
    img.paste(Image.fromarray(band), (x0, y0))
    for hr in range(0, 25, 3):
        x = x0 + int(n * hr / 24)
        d.line((x, y0 + hgt, x, y0 + hgt + 4), fill=SUB)
        text(d, (x, y0 + hgt + 5), f"{hr:02d}", 10, SUB, "ma")
    text(d, (x0 + n + 4, y0), "地平线", 9, SUB)
    text(d, (x0 + n + 4, y0 + h3), "天顶", 9, SUB)
    text(d, (x0 + n + 4, y0 + 2 * h3), "墙", 9, SUB)


def render_default(src: cs.Sources, grid: dict, results, out_path: str) -> None:
    f = next(fr for fr in grid["frames"]["autumn"] if fr.id == "golden")
    mats = grid["materials"][("autumn", "golden")]
    W, CW, CH = 1580, 880, 540
    img = Image.new("RGB", (W, 1400), PAPER)
    d = ImageDraw.Draw(img)
    text(d, (24, 18), "默认状态 · 秋 17:30「落金初灯」· 太阳 5.475° / 241.0°（SYS-TIME §0、§2）", 24)
    text(d, (24, 52), "r 0.78 · ambient.sky C 0.032 / h 275 · w_min 0 · line_darken 0.15（ART-STYLE §3.2 默认格）。场景为上屏近似示意，色块为调色板原值。", 14, SUB)
    img.paste(render_card(src, f, mats, CW, CH, scale=2), (24, 84))
    x = 24 + CW + 28
    y = 84
    text(d, (x, y), "登记值与示例对照（左：推导；右：文档；ΔE 为 OKLab × 100）", 15)
    y += 28
    rows = [r for r in results if r[1] == "autumn" and r[3].startswith(("登记值", "校验值", "示例"))]
    for lvl, s_, k, item, t, msg in rows:
        got_hex, rest = msg.split(" 对 ")
        ref_hex = rest.split("：")[0]
        de = rest.split("：")[-1]
        swatch(d, x, y, 34, 21, ok.hex_to_oklab(got_hex))
        swatch(d, x + 36, y, 34, 21, ok.hex_to_oklab(ref_hex))
        where = "秋正午" if k == "noon" else "默认格"
        text(d, (x + 80, y + 2), f"{t} · {item}（{where}）  {got_hex} / {ref_hex}  {de}", 12,
             PASS_C if lvl == "PASS" else (FAIL_C if lvl == "FAIL" else SUB))
        y += 25
    y_table_end = y
    # 下半：材质色块（左）与默认格自检（右）
    y0 = max(84 + CH, y_table_end) + 24
    text(d, (24, y0), "默认格全部材质（受光 / 影 / 室内影 / 室内受光；植被另加深影）", 15)
    items = list(mats.items())
    per_row = 4
    yy = y0 + 28
    for idx, (t, m) in enumerate(items):
        cx = 24 + (idx % per_row) * 222
        cy = yy + (idx // per_row) * 32
        for j, lab in enumerate((m.lit, m.shade, m.shade_in, m.lit_in)):
            swatch(d, cx + j * 28, cy, 27, 22, lab)
        if m.deep is not None:
            swatch(d, cx + 112, cy, 14, 22, m.deep)
        text(d, (cx + 132, cy + 4), t if len(t) <= 13 else t[:12] + "…", 11)
    y_mat_end = yy + ((len(items) + per_row - 1) // per_row) * 32
    gold = [r for r in results if r[1] == "autumn" and r[2] == "golden" and not r[3].startswith(("登记值", "校验值", "示例"))]
    fails = [r for r in gold if r[0] == "FAIL"]
    text(d, (x, y0), f"默认格影色自检（SYS-TIME §8）：PASS {sum(1 for r in gold if r[0] == 'PASS')} · FAIL {len(fails)}", 15,
         FAIL_C if fails else PASS_C)
    yy = y0 + 28
    keep = ("wall", "shadow.1", "stone.beige", "wall.greyWhite", "wall.dormYellow", "brick", "ginkgo", "grass", "camphor", "maple", "uestc.blue")
    picks = fails + [r for r in gold if r[0] != "FAIL" and r[4] in keep]
    for lvl, s_, k, item, t, msg in picks[:30]:
        mark = "+" if lvl == "PASS" else ("x" if lvl == "FAIL" else "·")
        text(d, (x, yy), f"{mark} {t} · {item}：{msg}", 11, PASS_C if lvl == "PASS" else (FAIL_C if lvl == "FAIL" else SUB))
        yy += 18
    img = img.crop((0, 0, W, max(y_mat_end, yy) + 20))
    img.save(out_path, optimize=False)


def grid_json(src: cs.Sources, grid: dict) -> dict:
    out = {"format": "campus.colorscript.grid/1", "note": "WP-37 色彩脚本推导结果（预览用，非运行时数据）", "seasons": {}}
    for s in cs.SEASONS:
        sd = {}
        for f in grid["frames"][s]:
            mats = grid["materials"][(s, f.id)]
            sd[f.id] = {
                "name": f.name, "clock": round(f.clock, 4), "altDeg": round(f.alt, 3),
                "r": round(f.r, 4), "wMin": f.w_min, "ambientSky": [round(f.ambient[0], 4), round(f.ambient[1], 2)],
                "sky.sun": [ok.oklab_to_hex(c) for c in f.sky_sun], "sky.anti": [ok.oklab_to_hex(c) for c in f.sky_anti],
                "sun.light": ok.oklab_to_hex(f.key_color),
                "fog": [ok.oklab_to_hex(f.fog_sun), ok.oklab_to_hex(f.fog_anti)],
                "colors": {t: ok.oklab_to_hex(v) for t, v in f.colors.items()},
                "notes": f.notes,
                "materials": {t: {"lit": ok.oklab_to_hex(m.lit), "shade": ok.oklab_to_hex(m.shade),
                                  "interiorShade": ok.oklab_to_hex(m.shade_in), "interiorLit": ok.oklab_to_hex(m.lit_in),
                                  "interiorPatch": ok.oklab_to_hex(cs.interior_composite(src, m.shade_in, m.lit_in)),
                                  **({"deep": ok.oklab_to_hex(m.deep)} if m.deep is not None else {})}
                              for t, m in mats.items()},
            }
        out["seasons"][s] = sd
    return out


def main(argv: Optional[Sequence[str]] = None) -> int:
    ap = argparse.ArgumentParser(description="WP-37 色板联表预览")
    ap.add_argument("--out", default=os.path.join(cs.REPO, "output", "render-review"))
    ap.add_argument("--only-check", action="store_true", help="只跑影色自检，不出图")
    args = ap.parse_args(argv)
    src = cs.Sources()
    grid = cs.derive_grid(src)
    results = sc.run(src, grid)
    summ = sc.summarize(results)
    os.makedirs(args.out, exist_ok=True)
    with open(os.path.join(args.out, "wp37_selfcheck.txt"), "w", encoding="utf-8", newline="\n") as fh:
        fh.write(sc.format_report(results))
    print(f"影色自检：PASS {summ['PASS']} · FAIL {summ['FAIL']} · INFO {summ['INFO']}")
    if args.only_check:
        return 1 if summ["FAIL"] else 0
    status = sc.frame_status(results)
    curves = cs.day_curves(src, grid["frames"])
    render_autumn_strip(src, grid, status, os.path.join(args.out, "wp37_autumn_keyframes.png"))
    render_4x12(src, grid, curves, status, os.path.join(args.out, "wp37_colorscript_4x12.png"))
    render_default(src, grid, results, os.path.join(args.out, "wp37_default_golden.png"))
    with open(os.path.join(args.out, "wp37_grid.json"), "w", encoding="utf-8", newline="\n") as fh:
        json.dump(grid_json(src, grid), fh, ensure_ascii=False, indent=1, sort_keys=False)
        fh.write("\n")
    for n in ("wp37_autumn_keyframes.png", "wp37_colorscript_4x12.png", "wp37_default_golden.png", "wp37_selfcheck.txt", "wp37_grid.json"):
        print(os.path.join(args.out, n))
    return 1 if summ["FAIL"] else 0


if __name__ == "__main__":
    raise SystemExit(main())

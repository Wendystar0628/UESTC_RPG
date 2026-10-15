"""WP-37 色彩脚本的参考实现（离线，Python）。

读 game/style/palette/keys/ 下的源数据，按 SYS-TIME §3、ART-STYLE §3 与 §8.3 推导：
- 秋季 12 张基准关键帧 → 四季修正与季节专属覆盖 → （天气修正接口）；
- 每个材质在每个"关键帧 × 季节"格里的受光色、影色、室内两行；
- 沿一天的插值（单调三次 Hermite、OKLab，色相大转时走 OKLCH）。

运行时的 C# 求值器（game/style/palette/keys，D0 合入后写）以本文件为参考，结果应逐值一致。
"""

from __future__ import annotations

import copy
import json
import math
import os
from typing import Any, Dict, List, Optional, Tuple

import oklab as ok

HERE = os.path.dirname(os.path.abspath(__file__))
REPO = os.path.normpath(os.path.join(HERE, "..", ".."))
KEYS_DIR = os.path.join(REPO, "game", "style", "palette", "keys")

SEASONS = ["spring", "summer", "autumn", "winter"]
SEASON_NAMES = {"spring": "春", "summer": "夏", "autumn": "秋", "winter": "冬"}

# SYS-TIME §2 美术太阳：日出、日落钟点，日出、日落方位，正午高度上限 H
ART_SUN = {
    "spring": (6 + 50 / 60, 19 + 20 / 60, 98.0, 262.0, 55.0),
    "summer": (6 + 10 / 60, 19 + 55 / 60, 85.0, 272.0, 62.0),
    "autumn": (7 + 35 / 60, 18 + 5 / 60, 113.0, 248.5, 41.0),
    "winter": (7 + 55 / 60, 17 + 55 / 60, 117.0, 244.0, 34.0),
}
NIGHT_H = 24.0
SUN_EXP = 1.15
AUTUMN_H = 41.0
NORM_FROM = 25.0          # 25° 以上按正午归一（假设，见 autumn.keys.json notes）
# 主光强度 key_strength 的 smoothstep 两端（°）。SYS-TIME §2 现为 (−1, 4)；主代理评审要求 h≈0° 时约 25–35%，
# WP-37 建议改为 (−2, 4)：0° 时 0.26，0.5° 时 0.38，−2° 以下仍为 0（不碰无影蓝调）。写入方是 WP-03，这里只给预览与自检用。
KEY_STRENGTH_EDGES = (-2.0, 4.0)

COLOR_TOKENS = [
    "bounce.ground", "shaft.near", "shaft.far", "rim", "glass.glare",
    "sky.cloudLit", "sky.cloudRim", "sky.cloudBelly", "sky.cloudDeep", "sky.moon",
]
AB_TOKENS = ["interior.warm"]


# ---------------------------------------------------------------- 数据读取

def _load(name: str, base: str = KEYS_DIR) -> Any:
    with open(os.path.join(base, name), "r", encoding="utf-8") as f:
        return json.load(f)


class Sources:
    def __init__(self, keys_dir: str = KEYS_DIR, probe_path: Optional[str] = None):
        self.autumn = _load("autumn.keys.json", keys_dir)
        self.derive = _load("derive.json", keys_dir)
        self.seasons = _load("seasons.json", keys_dir)
        self.vegetation = _load("vegetation.json", keys_dir)
        self.weather = _load("weather.json", keys_dir)
        probe_path = probe_path or os.path.join(HERE, "probe_materials.json")
        with open(probe_path, "r", encoding="utf-8") as f:
            self.probe = json.load(f)
        self.key_ids = [k["id"] for k in self.autumn["keys"]]
        self.sky_stops = [float(x) for x in self.autumn["skyStopsDeg"]]
        validate(self)


class DataError(ValueError):
    pass


def _is_hex(v: Any) -> bool:
    if not isinstance(v, str) or len(v) != 7 or v[0] != "#":
        return False
    try:
        int(v[1:], 16)
        return True
    except ValueError:
        return False


def validate(src: "Sources") -> None:
    errs: List[str] = []
    keys = src.autumn["keys"]
    stops = src.autumn.get("skyStopsDeg")
    if not (isinstance(stops, list) and len(stops) == 5 and stops[0] == 0 and stops[-1] == 90
            and all(stops[i] < stops[i + 1] for i in range(4))):
        errs.append("skyStopsDeg 必须是从 0 到 90 递增的 5 个仰角")
    if len(keys) != 12:
        errs.append(f"秋季基准关键帧应为 12 张，实际 {len(keys)}")
    ids = [k["id"] for k in keys]
    if len(set(ids)) != len(ids):
        errs.append("关键帧 id 重复")
    defaults = [k["id"] for k in keys if k.get("default")]
    if defaults != ["golden"]:
        errs.append(f"默认帧必须正好一张且是 golden，实际 {defaults}")
    for k in keys:
        kid = k["id"]
        for side in ("sun", "anti"):
            band = k["sky"][side]
            if len(band) != 5 or not all(_is_hex(c) for c in band):
                errs.append(f"{kid}: sky.{side} 必须是 5 个 #RRGGBB")
        for f in ("sun", "anti"):
            if not _is_hex(k["fog"][f]):
                errs.append(f"{kid}: fog.{f} 不是色值")
        if not isinstance(k.get("fogLow"), dict) or not all(x in k["fogLow"] for x in ("topM", "density", "lake")):
            errs.append(f"{kid}: 缺 fogLow（topM、density、lake）")
        if not _is_hex(k["key"]["color"]):
            errs.append(f"{kid}: key.color 不是色值")
        for t in COLOR_TOKENS:
            if not _is_hex(k["colors"].get(t)):
                errs.append(f"{kid}: 缺色名 {t}")
        for t in AB_TOKENS:
            v = k["colors"].get(t)
            if not (isinstance(v, dict) and "c" in v and "h" in v):
                errs.append(f"{kid}: {t} 应写 c、h")
        for t in ("warm", "cool"):
            if not _is_hex(k["line"][t]):
                errs.append(f"{kid}: line.{t} 不是色值")
        if k["side"] not in ("dawn", "dusk", "noon", "nadir"):
            errs.append(f"{kid}: side 不合法")
        # 锚点高度与示例钟点在秋季美术轨迹上一致（深夜锚在最低点）
        if k["side"] in ("dawn", "dusk"):
            h, side, _ = sun_altitude(k["exampleClock"], "autumn")
            if abs(h - k["altDeg"]) > 0.05 or side != k["side"]:
                errs.append(f"{kid}: altDeg {k['altDeg']} 与示例钟点的太阳高度 {h:.3f}（{side}）不符")
        if k["class"] not in ("day", "default", "lowSun", "twilight", "night"):
            errs.append(f"{kid}: class 不合法")
    for s in SEASONS:
        if s not in src.seasons["seasons"]:
            errs.append(f"seasons.json 缺 {s}")
    for scope_name, scope in src.seasons["scopes"].items():
        if scope != "all":
            for kid in scope:
                if kid not in ids:
                    errs.append(f"scope {scope_name} 引用了不存在的关键帧 {kid}")
    for s, sd in src.seasons["seasons"].items():
        for ov in sd.get("overrides", []):
            for kid in ov["keys"]:
                if kid not in ids:
                    errs.append(f"{s} 覆盖引用了不存在的关键帧 {kid}")
    for hero in src.vegetation["heroFrames"].values():
        if hero not in ids:
            errs.append(f"英雄帧 {hero} 不存在")
    if errs:
        raise DataError("源数据不合法：\n  " + "\n  ".join(errs))


# ---------------------------------------------------------------- 美术太阳（SYS-TIME §2）

def sun_altitude(clock: float, season: str) -> Tuple[float, str, float]:
    """返回 (高度°, 侧, u)。侧：dawn / dusk；正午与最低点归到它所在的一侧。"""
    rise, sset, _, _, H = ART_SUN[season]
    c = clock % 24.0
    if rise <= c <= sset:
        u = (c - rise) / (sset - rise)
        h = H * math.sin(math.pi * u) ** SUN_EXP
        return h, ("dawn" if u < 0.5 else "dusk"), u
    night = (24.0 - sset) + rise
    u = ((c - sset) % 24.0) / night
    h = -NIGHT_H * math.sin(math.pi * u) ** SUN_EXP
    return h, ("dusk" if u < 0.5 else "dawn"), u


def sun_azimuth(clock: float, season: str) -> Optional[float]:
    rise, sset, a0, a1, _ = ART_SUN[season]
    c = clock % 24.0
    if rise <= c <= sset:
        return a0 + (a1 - a0) * (c - rise) / (sset - rise)
    return None


def to_autumn_x(h: float, season: str) -> float:
    """季节高度 → 秋季等效高度（25° 以上按正午归一）。"""
    H = ART_SUN[season][4]
    if h <= NORM_FROM:
        return h
    return NORM_FROM + (h - NORM_FROM) / (H - NORM_FROM) * (AUTUMN_H - NORM_FROM)


def from_autumn_x(x: float, season: str) -> float:
    H = ART_SUN[season][4]
    if x <= NORM_FROM:
        return x
    return NORM_FROM + (x - NORM_FROM) / (AUTUMN_H - NORM_FROM) * (H - NORM_FROM)


def phase(x: float, side: str) -> float:
    """沿一天的单调相位 p ∈ [0, 2)：最低点 0 → 晨侧 → 正午 1 → 昏侧 → 最低点 2。"""
    span = AUTUMN_H + NIGHT_H
    if side == "nadir":
        return 0.0
    if side == "noon":
        return 1.0
    if side == "dawn":
        return (x + NIGHT_H) / span
    return 1.0 + (AUTUMN_H - x) / span


def clock_for_key(key: Dict[str, Any], season: str) -> float:
    """该季到达这个关键帧锚点（同侧、同等效高度）的钟点。"""
    rise, sset, _, _, H = ART_SUN[season]
    side = key["side"]
    if side == "noon":
        return 0.5 * (rise + sset)
    if side == "nadir":
        night = (24.0 - sset) + rise
        return (sset + 0.5 * night) % 24.0
    h = from_autumn_x(key["altDeg"], season)
    if h >= 0.0:
        v = min(1.0, h / H) ** (1.0 / SUN_EXP)
        u = math.asin(v) / math.pi
        if side == "dusk":
            u = 1.0 - u
        return rise + u * (sset - rise)
    night = (24.0 - sset) + rise
    v = min(1.0, -h / NIGHT_H) ** (1.0 / SUN_EXP)
    u = math.asin(v) / math.pi
    if side == "dawn":
        u = 1.0 - u
    return (sset + u * night) % 24.0


def key_strength_for(frame: "Frame") -> float:
    """预览用：主光强度。太阳 smoothstep(−1°, 4°, h)；月光帧取 1；天光帧（无影蓝调）取 0（SYS-TIME §2）。"""
    src = frame.key_source
    if src == "moon":
        return 1.0
    if src == "sky":
        return 0.0
    return ok.smoothstep(KEY_STRENGTH_EDGES[0], KEY_STRENGTH_EDGES[1], frame.alt)


def shadow_strength_for(frame: "Frame") -> float:
    src = frame.key_source
    if src == "moon":
        return 0.45
    if src == "sky":
        return 0.0
    return 1.0


# ---------------------------------------------------------------- 帧

class Frame:
    """一个"关键帧 × 季节"格（或插值出的中间帧），颜色全部存 OKLab。"""

    def __init__(self) -> None:
        self.id = ""
        self.name = ""
        self.cls = ""
        self.season = "autumn"
        self.side = "dusk"
        self.alt = 0.0
        self.x = 0.0
        self.clock = 0.0
        self.mood = ""
        self.sky_body = "sun"
        self.sky_sun: List[ok.Lab] = []
        self.sky_anti: List[ok.Lab] = []
        self.fog_sun: ok.Lab = (0, 0, 0)
        self.fog_anti: ok.Lab = (0, 0, 0)
        self.fog_scalars: Dict[str, float] = {}
        self.fog_low: Dict[str, float] = {"topM": 0.0, "density": 0.0, "lake": 0.0}
        self.key_source = "sun"
        self.key_color: ok.Lab = (0, 0, 0)
        self.intensity = 1.0
        self.sat_gain = 1.0
        self.lit_lift = 1.0
        self.r = 0.8
        self.ambient: Tuple[float, float] = (0.03, 275.0)   # (C, h)
        self.w_min = 0.0
        self.line_darken = 0.15
        self.line_warm: ok.Lab = (0, 0, 0)
        self.line_cool: ok.Lab = (0, 0, 0)
        self.colors: Dict[str, ok.Lab] = {}
        self.ab_colors: Dict[str, Tuple[float, float]] = {}
        self.post: Dict[str, Any] = {}
        self.sky_fx: Dict[str, float] = {}
        self.lights: Dict[str, Any] = {}
        self.lamp_pool_gain = 1.0
        self.notes: List[str] = []

    def copy(self) -> "Frame":
        return copy.deepcopy(self)


def frame_from_key(k: Dict[str, Any]) -> Frame:
    f = Frame()
    f.id, f.name, f.cls, f.mood = k["id"], k["name"], k["class"], k.get("mood", "")
    f.side, f.alt = k["side"], float(k["altDeg"])
    f.x = f.alt
    f.clock = float(k["exampleClock"])
    f.sky_body = k["sky"].get("body", "sun")
    f.sky_sun = [ok.hex_to_oklab(c) for c in k["sky"]["sun"]]
    f.sky_anti = [ok.hex_to_oklab(c) for c in k["sky"]["anti"]]
    fog = k["fog"]
    f.fog_sun, f.fog_anti = ok.hex_to_oklab(fog["sun"]), ok.hex_to_oklab(fog["anti"])
    f.fog_scalars = {
        "startM": float(fog["startM"]), "silhouetteM": float(fog["silhouetteM"]),
        "density": float(fog["density"]), "hazeDeg": float(fog["hazeDeg"]),
    }
    low = k["fogLow"]   # 全局参数 fog_low（ENG-PERF §1.5）：贴地雾高度上限 m、贴地雾密度、湖面雾强度、预留
    f.fog_low = {"topM": float(low["topM"]), "density": float(low["density"]), "lake": float(low["lake"])}
    key = k["key"]
    f.key_source = key["source"]
    f.key_color = ok.hex_to_oklab(key["color"])
    f.intensity = float(key["intensity"])
    f.sat_gain = float(key["satGain"])
    f.lit_lift = float(key["litLift"])
    sh = k["shade"]
    f.r = float(sh["r"])
    f.ambient = (float(sh["ambientSky"]["c"]), float(sh["ambientSky"]["h"]))
    f.w_min = float(sh["wMin"])
    f.line_darken = float(k["line"]["darken"])
    f.line_warm = ok.hex_to_oklab(k["line"]["warm"])
    f.line_cool = ok.hex_to_oklab(k["line"]["cool"])
    f.colors = {t: ok.hex_to_oklab(k["colors"][t]) for t in COLOR_TOKENS}
    f.ab_colors = {t: (float(k["colors"][t]["c"]), float(k["colors"][t]["h"])) for t in AB_TOKENS}
    f.post = copy.deepcopy(k["post"])
    f.sky_fx = {kk: float(v) for kk, v in k["skyFx"].items()}
    f.lights = copy.deepcopy(k.get("lights", {}))
    return f


def autumn_frames(src: Sources) -> List[Frame]:
    return [frame_from_key(k) for k in src.autumn["keys"]]


# ---------------------------------------------------------------- 四季修正（SYS-TIME §3.3）

def _scope_weight(src: Sources, scope: str, key_id: str) -> float:
    sc = src.seasons["scopes"][scope]
    if sc == "all":
        return 1.0
    return float(sc.get(key_id, 0.0))


def _shift_lch(lab: ok.Lab, dL: float, dC: float, dh: float, w: float) -> ok.Lab:
    L, C, h = ok.lab_to_lch(lab)
    return ok.lch_to_lab((L + dL * w, max(0.0, C + dC * w), (h + dh * w) % 360.0))


def apply_season(src: Sources, base: Frame, season: str) -> Frame:
    f = base.copy()
    f.season = season
    sd = src.seasons["seasons"][season]
    kid = base.id
    # 天空：天顶增量按 skyFalloff 分到 5 段
    z = sd["sky"]["zenith"]
    w_sky = _scope_weight(src, sd["sky"]["scope"], kid)
    fall = src.seasons["skyFalloff"]
    for band in (f.sky_sun, f.sky_anti):
        for i in range(5):
            band[i] = _shift_lch(band[i], z["dL"], z["dC"], z["dh"], w_sky * fall[i])
    # 主光
    for op in sd["key"]:
        w = _scope_weight(src, op["scope"], kid)
        if w <= 0.0:
            continue
        if op["op"] == "toward":
            f.key_color = ok.lerp_lab(f.key_color, ok.hex_to_oklab(op["color"]), op["amount"] * w)
        elif op["op"] == "chroma":
            L, C, h = ok.lab_to_lch(f.key_color)
            f.key_color = ok.lch_to_lab((L, C * (1.0 + (op["mul"] - 1.0) * w), h))
        else:
            raise DataError(f"未知的主光增量 {op['op']}")
    # 阴影：ambient.sky 与 r
    shd = sd["shade"]
    w = _scope_weight(src, shd["scope"], kid)
    c, h = f.ambient
    f.ambient = (max(0.0, c + shd["ambientSky"]["dC"] * w), (h + shd["ambientSky"]["dh"] * w) % 360.0)
    f.r = f.r + shd["dr"] * w
    rmax = src.seasons["limits"]["rMax"].get(f.cls)
    if rmax is not None and f.r > rmax + 1e-9:
        f.notes.append(f"r {f.r:.3f} 夹到 {rmax:.3f}（白天 ΔL ≥ 0.13）")
        f.r = rmax
    zmin = src.seasons["limits"].get("zenithMinC", {}).get(kid)
    if zmin is not None:
        for band in (f.sky_sun, f.sky_anti):
            L, C, h = ok.lab_to_lch(band[4])
            if C < zmin:
                f.notes.append(f"天顶彩度 {C:.3f} 夹到 {zmin:.2f}（正午太平）")
                band[4] = ok.lch_to_lab((L, zmin, h))
    # 雾
    fg = sd["fog"]
    wf = _scope_weight(src, fg["scope"], kid)
    for attr in ("fog_sun", "fog_anti"):
        L, a, b = getattr(f, attr)
        if fg.get("toward"):
            _, ta, tb = ok.hex_to_oklab(fg["toward"])
            t = fg["abAmount"] * wf
            a, b = ok.lerp(a, ta, t), ok.lerp(b, tb, t)
        setattr(f, attr, (min(1.0, L + fg["dL"]), a, b))
    f.fog_scalars["startM"] *= fg["visibilityMul"]
    f.fog_scalars["silhouetteM"] *= fg["visibilityMul"]
    f.fog_scalars["hazeDeg"] *= fg["hazeMul"]
    # 后期与灯照
    f.post["glowIntensity"] = f.post["glowIntensity"] * sd["post"]["glowIntensityMul"]
    f.lamp_pool_gain = float(sd.get("lampPoolGain", 1.0))
    # 季节专属覆盖
    for ov in sd.get("overrides", []):
        if kid in ov["keys"]:
            for t, v in ov.get("colors", {}).items():
                f.colors[t] = ok.hex_to_oklab(v)
            add = ov.get("skyFx", {}).get("cloudAmountAdd")
            if add:
                f.sky_fx["cloudAmount"] = min(1.0, f.sky_fx["cloudAmount"] + add)
            f.notes.append(f"覆盖：{ov['what']}")
    # 本季到达锚点的钟点
    k = next(kk for kk in src.autumn["keys"] if kk["id"] == kid)
    f.clock = clock_for_key(k, season)
    f.alt = from_autumn_x(base.x, season) if base.side not in ("noon", "nadir") else (
        ART_SUN[season][4] if base.side == "noon" else -NIGHT_H)
    return f


def season_frames(src: Sources) -> Dict[str, List[Frame]]:
    base = autumn_frames(src)
    return {s: [apply_season(src, b, s) for b in base] for s in SEASONS}


# ---------------------------------------------------------------- 材质推导（ART-STYLE §3.2、§3.4、§8.3）

def _light_m(src: Sources, f: Frame) -> float:
    ref = src.derive["lit"]["adaptRef"]
    return min(1.0, (f.intensity / ref) ** (1.0 / 3.0))


class HeroRef:
    """植被的基准光：英雄帧的主光 (a, b)、饱和增益（WLD-VEGETATION §2.3）。
    mode = "hero"：固有色的 (a, b) 就是英雄帧受光值；mode = "albedo"：直接给固有色，受光按一般材质推导。
    c_ref、warm_ref：英雄帧上的受光彩度与低暖光度，给植被受光彩度上限用（vegetationWarmCap）。"""

    def __init__(self, key_ab: Tuple[float, float], sat_gain: float, frame_id: str, season: str, mode: str = "hero"):
        self.key_ab, self.sat_gain, self.frame_id, self.season = key_ab, sat_gain, frame_id, season
        self.mode = mode
        self.c_ref = 0.0
        self.h_ref = 0.0
        self.warm_ref = 0.0


def _tint_factor(src: Sources, albedo: ok.Lab) -> float:
    fo = src.derive["lit"].get("tintChromaFalloff")
    if not fo:
        return 1.0
    c = (albedo[1] ** 2 + albedo[2] ** 2) ** 0.5
    return ok.lerp(1.0, fo["minFactor"], ok.smoothstep(fo["edges"][0], fo["edges"][1], c))


def derive_lit(src: Sources, albedo: ok.Lab, f: Frame, hero: Optional[HeroRef] = None) -> ok.Lab:
    """受光色。hero 为空：(a, b) = m × (satGain × 固有 + tintK × 主光)。
    hero 不为空（植被）：固有色的 (a, b) 就是英雄帧受光值，其他帧只按与英雄帧主光之差偏移：
    (a, b) = m × (satGain / satGain_hero × 固有 + vegetationTintK × (主光 − 主光_hero))。"""
    m = _light_m(src, f)
    tint = src.derive["lit"]["tintK"] * _tint_factor(src, albedo)
    La, aa, ba = albedo
    L = m * (1.0 - (1.0 - min(1.0, max(0.0, La))) ** f.lit_lift)
    _, ka, kb = f.key_color
    if hero is None or hero.mode == "albedo":
        a = m * (f.sat_gain * aa + tint * ka)
        b = m * (f.sat_gain * ba + tint * kb)
    else:
        g = f.sat_gain / hero.sat_gain
        vt = src.derive["lit"].get("vegetationTintK", src.derive["lit"]["tintK"])
        a = m * (g * aa + vt * (ka - hero.key_ab[0]))
        b = m * (g * ba + vt * (kb - hero.key_ab[1]))
    return (L, a, b)


def invert_lit(src: Sources, lit: ok.Lab, f: Frame) -> Tuple[ok.Lab, HeroRef]:
    """由英雄帧的受光值反解植被的固有色：只反解明度，(a, b) 保留受光值（除以该帧的 m）。"""
    m = _light_m(src, f)
    Ll, al, bl = lit
    La = 1.0 - max(0.0, 1.0 - Ll / m) ** (1.0 / f.lit_lift)
    hero = HeroRef((f.key_color[1], f.key_color[2]), f.sat_gain, f.id, f.season)
    return (La, al / m, bl / m), hero


def _rotate_hue(src: Sources, h_a: float, h_s: float, dh_max: float) -> float:
    d = ok.hue_diff(h_a, h_s)
    step = min(dh_max, abs(d))
    lo, hi = src.derive["shade"]["yellowGuard"]
    if lo <= h_a <= hi and d > 0.0:
        # 黄、橙不往绿转：最短方向朝绿（色相增大）时改向红转
        return (h_a - step) % 360.0
    g = src.derive["shade"].get("greenGuard")
    if g and g[0] < h_a <= g[1] and d < 0.0:
        # 绿不往黄转（不进橄榄）：最短方向朝黄（色相减小）时改向青转
        return (h_a + step) % 360.0
    return (h_a + math.copysign(step, d)) % 360.0


def derive_shade(src: Sources, albedo: ok.Lab, lit: ok.Lab, f: Frame, cls: str,
                 r_override: Optional[float] = None,
                 ambient_override: Optional[Tuple[float, float]] = None) -> ok.Lab:
    mc = src.derive["materialClasses"][cls]
    sp = src.derive["shade"]
    r = f.r if r_override is None else r_override
    c_s, h_s = f.ambient if ambient_override is None else ambient_override
    La, Ca, ha = ok.lab_to_lch(albedo)
    L = lit[0] * r * mc["rMat"]
    C1 = min(Ca * mc["kC"], Ca + sp["chromaMulMax"])
    if sp.get("scaleChromaWithLight", False):
        C1 *= _light_m(src, f)
    h1 = _rotate_hue(src, ha, h_s, mc["dhMax"]) if Ca > 1e-6 else h_s
    _, a1, b1 = ok.lch_to_lab((L, C1, h1))
    e0, e1 = sp["neutralCEdge"]
    w = max(f.w_min, ok.smoothstep(e0, e1, Ca))
    _, sa, sb = ok.lch_to_lab((1.0, c_s, h_s))
    a, b = ok.lerp(a1, sa, w), ok.lerp(b1, sb, w)
    # 越出色域只降彩度、不动 L：在输出时做（oklab_to_srgb_clipped）
    return (L, a, b)


def derive_interior(src: Sources, albedo: ok.Lab, lit: ok.Lab, f: Frame, cls: str) -> Tuple[ok.Lab, ok.Lab]:
    ip = src.derive["interior"]
    warm = f.ab_colors["interior.warm"]
    shade_in = derive_shade(src, albedo, lit, f, cls, r_override=ip["rIn"], ambient_override=warm)
    L, C, h = ok.lab_to_lch(lit)
    d = ok.hue_diff(h, ip["orangeHue"])
    h2 = (h + math.copysign(min(ip["litHueMax"], abs(d)), d)) % 360.0 if C > 1e-6 else h
    lit_in = ok.lch_to_lab((L, C * ip["litChromaMul"], h2))
    return shade_in, lit_in


def derive_deep(src: Sources, shade: ok.Lab) -> ok.Lab:
    d = src.derive["deep"]
    L, C, h = ok.lab_to_lch(shade)
    return ok.lch_to_lab((L * d["lMul"], C * d["cMul"], h))


class MaterialResult:
    def __init__(self, token: str, name: str, cls: str, albedo: ok.Lab, lit: ok.Lab, shade: ok.Lab,
                 shade_in: ok.Lab, lit_in: ok.Lab, deep: Optional[ok.Lab] = None, meta: Optional[dict] = None):
        self.token, self.name, self.cls = token, name, cls
        self.albedo, self.lit, self.shade = albedo, lit, shade
        self.shade_in, self.lit_in, self.deep = shade_in, lit_in, deep
        self.meta = meta or {}


def cap_lit_chroma(src: Sources, albedo: ok.Lab, lit: ok.Lab, shade: ok.Lab) -> ok.Lab:
    """有彩材质的受光彩度上限 = 影彩度 / chromaCapRatio（保住身份色：影 C ≥ 0.6 × 受光 C，SYS-TIME §8）。
    只在强色光（日出、日落）把高彩材质的受光推得过艳时起作用；不改明度与色相。"""
    cap = src.derive["lit"].get("chromaCap")
    if not cap:
        return lit
    Ca = math.hypot(albedo[1], albedo[2])
    if Ca < cap["minAlbedoC"]:
        return lit
    L, C, h = ok.lab_to_lch(lit)
    cmax = math.hypot(shade[1], shade[2]) / cap["ratio"]
    if C <= cmax:
        return lit
    return ok.lch_to_lab((L, cmax, h))


def warm_low(src: Sources, f: Frame) -> float:
    """低暖光度：太阳低（秋季等效高度 x 低于 altDeg 区间）且主光偏色重（主光彩度高于 keyC 区间）时趋近 1。"""
    cfg = src.derive["lit"]["vegetationWarmCap"]
    low = 1.0 - ok.smoothstep(cfg["altDeg"][0], cfg["altDeg"][1], f.x)
    _, kc, _ = ok.lab_to_lch(f.key_color)
    return low * ok.smoothstep(cfg["keyC"][0], cfg["keyC"][1], kc)


def cap_vegetation_chroma(src: Sources, lit: ok.Lab, f: Frame, hero: HeroRef) -> ok.Lab:
    """植被受光彩度上限 = 英雄帧受光彩度 × (1 − k × max(0, 本帧低暖光度 − 英雄帧低暖光度))：
    草和叶在任何帧都不比英雄帧更艳，低太阳、暖主光时再收一档，傍晚的草是暖黄绿而不是荧光色（主代理评审）；
    色相限制在英雄帧受光色相 ± hueMax 内。"""
    cfg = src.derive["lit"]["vegetationWarmCap"]
    cmax = hero.c_ref * (1.0 - cfg["k"] * max(0.0, warm_low(src, f) - hero.warm_ref))
    L, C, h = ok.lab_to_lch(lit)
    # 色相也只许在英雄帧受光色相 ± hueMax 内随主光走：绿叶在橙光下不转进橄榄
    d = ok.hue_diff(hero.h_ref, h)
    if abs(d) > cfg["hueMax"] and hero.c_ref > 1e-6:
        h = (hero.h_ref + math.copysign(cfg["hueMax"], d)) % 360.0
    return ok.lch_to_lab((L, min(C, cmax), h))


def derive_material(src: Sources, token: str, name: str, cls: str, albedo: ok.Lab, f: Frame,
                    meta: Optional[dict] = None, hero: Optional[HeroRef] = None) -> MaterialResult:
    lit = derive_lit(src, albedo, f, hero)
    if hero is not None:
        lit = cap_vegetation_chroma(src, lit, f, hero)
    shade = derive_shade(src, albedo, lit, f, cls)
    lit = cap_lit_chroma(src, albedo, lit, shade)
    s_in, l_in = derive_interior(src, albedo, lit, f, cls)
    deep = derive_deep(src, shade) if src.derive["materialClasses"][cls]["steps"] == "three" else None
    return MaterialResult(token, name, cls, albedo, lit, shade, s_in, l_in, deep, meta)


# ---------------------------------------------------------------- 植被的季相固有色

VegEntry = Tuple[str, str, ok.Lab, HeroRef]


def vegetation_albedos(src: Sources, frames: Dict[str, List[Frame]]) -> Dict[str, Dict[str, VegEntry]]:
    """返回 {季: {token: (名称, 类, 固有色 OKLab, 基准光)}}。"""
    heroes = src.vegetation["heroFrames"]
    out: Dict[str, Dict[str, VegEntry]] = {s: {} for s in SEASONS}
    by_id = {s: {f.id: f for f in frames[s]} for s in SEASONS}

    def entry(value: Any, hero_frame: Frame) -> Tuple[ok.Lab, HeroRef]:
        if isinstance(value, dict):          # {"albedo": "#..."}：直接给固有色
            alb = ok.hex_to_oklab(value["albedo"])
            ref = HeroRef((hero_frame.key_color[1], hero_frame.key_color[2]), hero_frame.sat_gain,
                          hero_frame.id, hero_frame.season, mode="albedo")
        else:                                # 英雄帧受光值
            alb, ref = invert_lit(src, ok.hex_to_oklab(value), hero_frame)
        lit_hero = derive_lit(src, alb, hero_frame, ref)
        ref.c_ref = math.hypot(lit_hero[1], lit_hero[2])
        if ref.mode == "albedo":
            # 直接给固有色的：英雄帧上主光最多再给固有彩度 × satGain 的 albedoChromaGain 倍，金色主要靠色相，不靠堆彩度
            gain = src.derive["lit"]["vegetationWarmCap"]["albedoChromaGain"]
            ref.c_ref = min(ref.c_ref, math.hypot(alb[1], alb[2]) * hero_frame.sat_gain * gain)
        ref.h_ref = ok.lab_to_lch(lit_hero)[2]
        ref.warm_ref = warm_low(src, hero_frame)
        return alb, ref

    for v in src.vegetation["seasonal"]:
        for s in SEASONS:
            alb, ref = entry(v["lit"][s], by_id[s][heroes[s]])
            out[s][v["token"]] = (v["name"], v["class"], alb, ref)
    for v in src.vegetation["constant"]:
        alb, ref = entry(v["lit"], by_id[v["heroSeason"]][heroes[v["heroSeason"]]])
        for s in SEASONS:
            out[s][v["token"]] = (v["name"], v["class"], alb, ref)
    return out


def derive_grid(src: Sources) -> Dict[str, Any]:
    """4 季 × 12 关键帧的完整推导结果。"""
    frames = season_frames(src)
    veg = vegetation_albedos(src, frames)
    grid: Dict[str, Any] = {"frames": frames, "materials": {}, "vegetation": veg}
    for s in SEASONS:
        for f in frames[s]:
            res: Dict[str, MaterialResult] = {}
            for m in src.probe["materials"]:
                if "seasons" in m and s not in m["seasons"]:
                    continue
                res[m["token"]] = derive_material(src, m["token"], m["name"], m["class"],
                                                  ok.hex_to_oklab(m["albedo"]), f, meta=m)
            for token, (name, cls, alb, ref) in veg[s].items():
                res[token] = derive_material(src, token, name, cls, alb, f, meta={"vegetation": True}, hero=ref)
            grid["materials"][(s, f.id)] = res
    return grid


# ---------------------------------------------------------------- 天气修正（接口，SYS-WEATHER §7）

WEATHER_FIELDS = ["Cloud", "CloudType", "Cb", "Overcast", "Precip", "PrecipKind", "Wet", "Puddle",
                  "Fog", "GroundFog", "Storm", "SnowAdd", "Rainbow", "Lightning"]


def clear_weather() -> Dict[str, float]:
    """晴：全 0，只有雾密度倍率 Fog 为 1（ENG-ARCH §3 WeatherVec 的注释）。"""
    v = {k: 0.0 for k in WEATHER_FIELDS}
    v["Fog"] = 1.0
    return v


def apply_weather(src: Sources, base: Frame, wv: Dict[str, float]) -> Frame:
    """天气修正量叠在四季修正之后（SYS-TIME §3.1）。晴天返回原帧的拷贝，逐值不变。"""
    f = base.copy()
    W = src.weather
    g = float(wv.get("Overcast", 0.0))
    p = float(wv.get("Precip", 0.0))
    k = float(wv.get("PrecipKind", 0.0))
    wet = float(wv.get("Wet", 0.0))
    if g == 0.0 and p == 0.0 and wet == 0.0 and float(wv.get("Fog", 1.0)) == 1.0:
        return f
    # 主光 EV：阴 −1.0，大雨再 −0.3
    ev = W["keyEv"]["overcast"] * g + W["keyEv"]["heavyRainExtra"] * p * (1.0 - k)
    f.intensity = f.intensity * (2.0 ** ev)
    # 软光：r 的下限连续接入
    rf = W["shade"]["rFloor"]
    target = max(f.r, rf["base"] + rf["perOvercast"] * g)
    f.r = f.r + g * (target - f.r)
    # ambient.sky 向 weather.overcast 的色相混 g
    c0, h0 = f.ambient
    tgt = W["shade"]["ambientSkyTarget"]
    f.ambient = (ok.lerp(c0, tgt["c"], g), (h0 + ok.hue_diff(h0, tgt["h"]) * g) % 360.0)
    # 天色：向珍珠粉紫混 g，L ≥ 0.72、彩度不低于 0.02（雨天 L 再降 0.05）
    so = W["sky"]["overcast"]
    oc = ok.lch_to_lab((max(so["minL"], 0.78 - so["rainLDrop"] * p * (1.0 - k)), so["c"], so["h"]))
    for band in (f.sky_sun, f.sky_anti):
        for i in range(5):
            mixed = ok.lerp_lab(band[i], oc, g)
            L, C, h = ok.lab_to_lch(mixed)
            if g > 0.5:
                L, C = max(L, so["minL"] if f.cls in ("day", "default", "dawnDusk") else L), max(C, so["minC"])
            band[i] = ok.lch_to_lab((L, C, h))
    # 雨后初晴：洗净度给饱和与天色彩度
    washed = wet * (1.0 - g) * (1.0 - p)
    f.sat_gain += W["washedSatAdd"] * washed
    for band in (f.sky_sun, f.sky_anti):
        for i in range(5):
            L, C, h = ok.lab_to_lch(band[i])
            band[i] = ok.lch_to_lab((L, C + W["sky"]["washedChromaAdd"] * washed, h))
    # 云量由天气改写
    if W["cloudAmount"] == "replace":
        f.sky_fx["cloudAmount"] = max(f.sky_fx["cloudAmount"] * (1.0 - g), float(wv.get("Cloud", 0.0)))
    f.notes.append(f"天气修正 g={g:.2f} p={p:.2f} wet={wet:.2f}")
    return f


def rain_vegetation_chroma_mul(src: Sources, wv: Dict[str, float]) -> float:
    """雨天植被彩度 +15%（SYS-WEATHER §2）：按降水 × (1 − 降水类型) 连续接入。"""
    p = float(wv.get("Precip", 0.0)) * (1.0 - float(wv.get("PrecipKind", 0.0)))
    return 1.0 + (src.weather["rainVegetationChromaMul"] - 1.0) * min(1.0, max(p, float(wv.get("Wet", 0.0))))


# ---------------------------------------------------------------- 沿一天插值（SYS-TIME §3.1）

def _fc_slopes(xs: List[float], ys: List[float]) -> List[float]:
    """周期的 Fritsch–Carlson 单调三次 Hermite 斜率。xs 已排序且首尾不重合，周期为 2。"""
    n = len(xs)
    d = []
    for i in range(n):
        x0, x1 = xs[i], xs[(i + 1) % n] + (2.0 if i == n - 1 else 0.0)
        d.append((ys[(i + 1) % n] - ys[i]) / (x1 - x0))
    m = []
    for i in range(n):
        dp, dn = d[i - 1], d[i]
        # 两侧割线异号或有一侧为平：斜率取 0；否则取调和平均（Fritsch–Butland），保证不过冲
        m.append(0.0 if dp * dn <= 0.0 else 2.0 * dp * dn / (dp + dn))
    return m


def _hermite(y0: float, y1: float, m0: float, m1: float, hseg: float, t: float) -> float:
    t2, t3 = t * t, t * t * t
    return ((2 * t3 - 3 * t2 + 1) * y0 + (t3 - 2 * t2 + t) * hseg * m0
            + (-2 * t3 + 3 * t2) * y1 + (t3 - t2) * hseg * m1)


def _arc_through(h0: float, d: float, target: float) -> bool:
    if d >= 0.0:
        return (target - h0) % 360.0 <= d
    return (h0 - target) % 360.0 <= -d


def _hue_path(h0: float, h1: float, via_magenta: bool) -> float:
    d = ok.hue_diff(h0, h1)
    if via_magenta and not _arc_through(h0, d, 345.0):
        alt = d - math.copysign(360.0, d)
        if _arc_through(h0, alt, 345.0):
            return alt
    return d


class DayCurve:
    """一季的 12 个锚点沿一天的插值器。"""

    LOG_SCALARS = {"intensity", "fog.density", "fog.startM", "fog.silhouetteM"}

    def __init__(self, frames: List[Frame]):
        order = sorted(frames, key=lambda f: phase(f.x, f.side))
        self.frames = order
        self.ps = [phase(f.x, f.side) for f in order]
        self.vectors = [self._flatten(f) for f in order]
        self.names = [n for n, _, _ in self.vectors[0]]
        self.kinds = {n: k for n, k, _ in self.vectors[0]}
        n = len(self.names)
        self.slopes: List[List[float]] = []
        for j in range(n):
            ys = [vec[j][2] for vec in self.vectors]
            if self.names[j] in self.LOG_SCALARS:
                ys = [math.log2(max(1e-6, y)) for y in ys]
            self.slopes.append(_fc_slopes(self.ps, ys))

    @staticmethod
    def _flatten(f: Frame) -> List[Tuple[str, str, float]]:
        out: List[Tuple[str, str, float]] = []

        def col(name: str, lab: ok.Lab) -> None:
            for i, c in enumerate("Lab"):
                out.append((f"{name}.{c}", "color", lab[i]))

        for i in range(5):
            col(f"sky.sun.{i}", f.sky_sun[i])
            col(f"sky.anti.{i}", f.sky_anti[i])
        col("fog.sun", f.fog_sun)
        col("fog.anti", f.fog_anti)
        col("sun.light", f.key_color)
        col("line.warm", f.line_warm)
        col("line.cool", f.line_cool)
        for t in COLOR_TOKENS:
            col(t, f.colors[t])
        c, h = f.ambient
        _, a, b = ok.lch_to_lab((1.0, c, h))
        out += [("ambient.sky.a", "scalar", a), ("ambient.sky.b", "scalar", b)]
        c, h = f.ab_colors["interior.warm"]
        _, a, b = ok.lch_to_lab((1.0, c, h))
        out += [("interior.warm.a", "scalar", a), ("interior.warm.b", "scalar", b)]
        for name, v in (("intensity", f.intensity), ("satGain", f.sat_gain), ("litLift", f.lit_lift),
                        ("r", f.r), ("wMin", f.w_min), ("line.darken", f.line_darken)):
            out.append((name, "scalar", v))
        for k2, v in sorted(f.fog_scalars.items()):
            out.append((f"fog.{k2}", "scalar", v))
        for k2, v in sorted(f.fog_low.items()):
            out.append((f"fogLow.{k2}", "scalar", v))
        for k2, v in sorted(f.sky_fx.items()):
            out.append((f"skyFx.{k2}", "scalar", v))
        for k2 in ("exposureEv", "glowBloom", "glowIntensity"):
            out.append((f"post.{k2}", "scalar", float(f.post[k2])))
        return out

    def _segment(self, p: float) -> Tuple[int, int, float, float]:
        n = len(self.ps)
        p = p % 2.0
        for i in range(n):
            p0 = self.ps[i]
            p1 = self.ps[(i + 1) % n] + (2.0 if i == n - 1 else 0.0)
            pp = p if p >= p0 else p + 2.0
            if p0 <= pp <= p1:
                return i, (i + 1) % n, (pp - p0) / (p1 - p0), p1 - p0
        return n - 1, 0, 0.0, 1.0

    def evaluate(self, p: float) -> Dict[str, float]:
        i, j, t, hseg = self._segment(p)
        vals: Dict[str, float] = {}
        for k, name in enumerate(self.names):
            y0, y1 = self.vectors[i][k][2], self.vectors[j][k][2]
            m0, m1 = self.slopes[k][i], self.slopes[k][j]
            if name in self.LOG_SCALARS:
                v = 2.0 ** _hermite(math.log2(max(1e-6, y0)), math.log2(max(1e-6, y1)), m0, m1, hseg, t)
            else:
                v = _hermite(y0, y1, m0, m1, hseg, t)
            vals[name] = v
        # 色相大转的色带：相邻键色相差 > 40° 走 OKLCH（天空色带 > 60° 强制经过洋红）
        te = ok.smoothstep(0.0, 1.0, t)
        for base in {n.rsplit(".", 1)[0] for n in self.names if self.kinds[n] == "color"}:
            a0 = (self.vectors[i][self.names.index(base + ".L")][2],
                  self.vectors[i][self.names.index(base + ".a")][2],
                  self.vectors[i][self.names.index(base + ".b")][2])
            a1 = (self.vectors[j][self.names.index(base + ".L")][2],
                  self.vectors[j][self.names.index(base + ".a")][2],
                  self.vectors[j][self.names.index(base + ".b")][2])
            L0, C0, h0 = ok.lab_to_lch(a0)
            L1, C1, h1 = ok.lab_to_lch(a1)
            if C0 < 0.02 or C1 < 0.02:
                continue
            d = ok.hue_diff(h0, h1)
            if abs(d) <= 40.0:
                continue
            sky = base.startswith("sky.sun") or base.startswith("sky.anti")
            dd = _hue_path(h0, h1, via_magenta=sky and abs(d) > 60.0)
            C = ok.lerp(C0, C1, te)
            h = (h0 + dd * te) % 360.0
            _, a, b = ok.lch_to_lab((1.0, C, h))
            vals[base + ".a"], vals[base + ".b"] = a, b
        return vals

    def color(self, vals: Dict[str, float], name: str) -> ok.Lab:
        return (vals[name + ".L"], vals[name + ".a"], vals[name + ".b"])


def day_curves(src: Sources, frames: Optional[Dict[str, List[Frame]]] = None) -> Dict[str, DayCurve]:
    frames = frames or season_frames(src)
    return {s: DayCurve(frames[s]) for s in SEASONS}


def phase_at(clock: float, season: str) -> float:
    h, side, _ = sun_altitude(clock, season)
    return phase(to_autumn_x(h, season), side)


def frame_at(src: Sources, curve: DayCurve, season: str, clock: float) -> Frame:
    """插值出任意钟点的帧（只填预览要用的字段）。"""
    vals = curve.evaluate(phase_at(clock, season))
    f = Frame()
    f.season, f.clock = season, clock
    h, side, _ = sun_altitude(clock, season)
    f.alt, f.side, f.x = h, side, to_autumn_x(h, season)
    f.id = f"t{clock:05.2f}"
    f.sky_sun = [curve.color(vals, f"sky.sun.{i}") for i in range(5)]
    f.sky_anti = [curve.color(vals, f"sky.anti.{i}") for i in range(5)]
    f.fog_sun, f.fog_anti = curve.color(vals, "fog.sun"), curve.color(vals, "fog.anti")
    f.key_color = curve.color(vals, "sun.light")
    f.line_warm, f.line_cool = curve.color(vals, "line.warm"), curve.color(vals, "line.cool")
    f.colors = {t: curve.color(vals, t) for t in COLOR_TOKENS}
    a, b = vals["ambient.sky.a"], vals["ambient.sky.b"]
    f.ambient = (math.hypot(a, b), math.degrees(math.atan2(b, a)) % 360.0)
    a, b = vals["interior.warm.a"], vals["interior.warm.b"]
    f.ab_colors = {"interior.warm": (math.hypot(a, b), math.degrees(math.atan2(b, a)) % 360.0)}
    f.intensity, f.sat_gain, f.lit_lift = vals["intensity"], vals["satGain"], vals["litLift"]
    f.r, f.w_min, f.line_darken = vals["r"], vals["wMin"], vals["line.darken"]
    f.fog_scalars = {k[4:]: v for k, v in vals.items() if k.startswith("fog.") and not k[4:].split(".")[0] in ("sun", "anti")}
    f.fog_low = {k[7:]: v for k, v in vals.items() if k.startswith("fogLow.")}
    f.sky_fx = {k[6:]: v for k, v in vals.items() if k.startswith("skyFx.")}
    f.post = {"exposureEv": vals["post.exposureEv"], "glowBloom": vals["post.glowBloom"],
              "glowIntensity": vals["post.glowIntensity"]}
    # 主光来源：日在地平线上为 sun；−2° 到 −7° 为无影的天光；更低为月光
    f.key_source = "sun" if h > -2.0 else ("sky" if h > -7.0 else "moon")
    f.cls = "night" if h < -3.0 else ("twilight" if h < 0 else "day")
    return f


# ---------------------------------------------------------------- IPaletteProbe 参考（UI-VISUAL §11.11）

def sample_horizon(src: Sources, curve: DayCurve, season: str, wv: Optional[Dict[str, float]] = None) -> List[ok.Rgb]:
    """24 个整点的地平线天色（sRGB 0–1）：太阳侧与背阳侧的地平线色各半（假设）。"""
    out = []
    for hour in range(24):
        f = frame_at(src, curve, season, float(hour))
        if wv is not None:
            f = apply_weather(src, f, wv)
        lab = ok.lerp_lab(f.sky_sun[0], f.sky_anti[0], 0.5)
        out.append(ok.oklab_to_srgb_clipped(lab))
    return out


# ---------------------------------------------------------------- 线色（ART-STYLE §4.2，预览与自检用）

def line_color(base: ok.Lab, f: Frame) -> ok.Lab:
    lin = ok.oklab_to_linear(base)
    lin = tuple(min(1.0, max(0.0, c)) for c in lin)
    bl = 0.2126 * lin[0] + 0.7152 * lin[1] + 0.0722 * lin[2]
    warm = ok.smoothstep(-0.01, 0.03, base[2])
    lw = ok.oklab_to_linear(f.line_warm)
    lc_ = ok.oklab_to_linear(f.line_cool)
    tint = tuple(ok.lerp(lc_[i], lw[i], warm) for i in range(3))
    lc = tuple(ok.lerp(lin[i], bl, 0.35) * f.line_darken for i in range(3))
    lc = tuple(ok.lerp(lc[i], tint[i], 0.22) for i in range(3))
    return ok.linear_to_oklab(lc)  # type: ignore[arg-type]


def exposure_lab(lab: ok.Lab, ev: float) -> ok.Lab:
    """上屏值的近似：线性 × 2^ev（OKLab 各分量同乘 2^(ev/3)）。"""
    k = 2.0 ** (ev / 3.0)
    return (lab[0] * k, lab[1] * k, lab[2] * k)

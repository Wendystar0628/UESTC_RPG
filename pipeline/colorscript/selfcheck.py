"""调色板逐行自检，即影色自检（SYS-TIME，推导规则 ART-STYLE，夜间与阴雨 ART-STYLE）。

只查能在调色板上判定的项；"主体与背景明度差""跳色面积""画面明度分布"要看截图，留给 WP-41 的集中测试。
每条结果是 (级别, 季, 关键帧, 项, 材质, 说明)，级别为 FAIL / PASS / INFO。
"""

from __future__ import annotations

from typing import Dict, List, Tuple

import colorscript as cs
import oklab as ok

Result = Tuple[str, str, str, str, str, str]

LIGHT_FACE_CLASSES = {"building", "ground", "snow"}   # "浅色面"只指 r_mat = 1 的墙、地、屋面、雪，不含植被与金属（假设）
NEUTRAL_C_MAX = 0.06          # 中性材质：固有色彩度 < 0.06，标明暖影的暖墙（warmShade）除外（SYS-TIME，main 931e15f）
LINE_NIGHT = (0.06, 0.10)     # 夜间描线比影暗 0.06–0.10（SYS-TIME，主代理裁定以此为准）
LIGHT_FACE_ALBEDO_L = 0.80    # 浅色面另要求固有色 L ≥ 0.80（ART-STYLE 大面积墙面）；0.78 左右的中明度墙受光刚过 0.85 时两条限值不可同时满足（假设）
NEUTRAL_C_MIN = 0.012         # 影彩度低于它时色相无意义，按近中性灰处理
IDENTITY_CLASSES = {"day", "default", "lowSun"}     # 身份色的色相只在白天与晨昏判；蓝调和夜间由 w_min 有意拉向蓝紫，改判蓝紫；彩度比所有时段都判
DE_REGISTERED = 2.0           # 与登记值比对：OKLab ΔE × 100 ≤ 2 记为一致


def _de(p: ok.Lab, q: ok.Lab) -> float:
    return 100.0 * ((p[0] - q[0]) ** 2 + (p[1] - q[1]) ** 2 + (p[2] - q[2]) ** 2) ** 0.5


def _in_range(h: float, lo: float, hi: float) -> bool:
    return lo <= h % 360.0 <= hi


def _w(src: cs.Sources, f: cs.Frame, Ca: float) -> float:
    e0, e1 = src.derive["shade"]["neutralCEdge"]
    return max(f.w_min, ok.smoothstep(e0, e1, Ca))


def check_frame(src: cs.Sources, f: cs.Frame, mats: Dict[str, cs.MaterialResult]) -> List[Result]:
    out: List[Result] = []
    s, k = f.season, f.id
    sky_c, sky_h = f.ambient
    olive = src.derive["shade"]["oliveZone"]
    green = src.derive["shade"]["greenRange"]
    ev = float(f.post.get("exposureEv", 0.0))
    strict_default = (f.cls == "default" and s == "autumn")
    for t, m in mats.items():
        La, Ca, ha = ok.lab_to_lch(m.albedo)
        Ll, Cl, hl = ok.lab_to_lch(m.lit)
        Ls, Cs, hs = ok.lab_to_lch(m.shade)
        # 中性材质只指白墙、灰石、混凝土、雪这类面（ART-STYLE 第 3 条）：不含植被与花（花瓣向品红偏，§3.5）、玻璃
        neutral = (Ca < NEUTRAL_C_MAX and not m.meta.get("warmShade", False) and m.meta.get("kind") != "glass"
                   and m.cls not in ("vegetation", "flower"))
        # 1 中性材质：影色相落在 ambient.sky ±30° 内
        if neutral and Cs >= NEUTRAL_C_MIN:
            d = abs(ok.hue_diff(hs, sky_h))
            lvl = "PASS" if d <= 30.0 else "FAIL"
            out.append((lvl, s, k, "中性影随天光", t, f"影 h{hs:.0f} 与天光 h{sky_h:.0f} 差 {d:.0f}°（≤ 30°）"))
        # 2 有彩材质：身份色。彩度比在所有时段都判；色相只在白天与晨昏判
        if Ca >= 0.08:
            ratio = Cs / Cl if Cl > 1e-6 else 1.0
            ok_c = ratio >= 0.6
            if f.cls in IDENTITY_CLASSES:
                d = abs(ok.hue_diff(ha, hs))
                lvl = "PASS" if (d <= 20.0 and ok_c) else "FAIL"
                out.append((lvl, s, k, "身份色", t, f"影 h{hs:.0f} 与固有 h{ha:.0f} 差 {d:.0f}°（≤ 20°）；影 C/受光 C = {ratio:.2f}（≥ 0.6）"))
            else:
                out.append(("PASS" if ok_c else "FAIL", s, k, "身份色彩度", t, f"影 C/受光 C = {ratio:.2f}（≥ 0.6）"))
        # 3 非绿色材质的影不落橄榄区（变体按基准色名的固有色相判类：草坪亮块仍是绿色材质）
        h_class = ok.lab_to_lch(mats[m.meta["variantOf"]].albedo)[2] if m.meta.get("variantOf") in mats else ha
        if not _in_range(h_class, *green) and Cs >= 0.02 and Ca >= 0.015:
            bad = _in_range(hs, *olive)
            out.append(("FAIL" if bad else "PASS", s, k, "不落橄榄区", t, f"影 h{hs:.0f}（禁区 {olive[0]:.0f}–{olive[1]:.0f}°）"))
        # 4 浅色面
        if m.cls in LIGHT_FACE_CLASSES and Ll >= 0.85 and La >= LIGHT_FACE_ALBEDO_L:
            dL = Ll - Ls
            if f.cls == "day":
                okk = Ls >= 0.74 and 0.13 <= dL <= 0.20
                out.append(("PASS" if okk else "FAIL", s, k, "浅色面（白天）", t, f"影 L{Ls:.3f}（≥ 0.74）ΔL {dL:.3f}（0.13–0.20）"))
            elif strict_default:
                okk = Ls >= 0.62 and 0.18 <= dL <= 0.30
                out.append(("PASS" if okk else "FAIL", s, k, "浅色面（默认格）", t, f"影 L{Ls:.3f}（≥ 0.62）ΔL {dL:.3f}（0.18–0.30）"))
            else:
                out.append(("INFO", s, k, "浅色面", t, f"影 L{Ls:.3f} ΔL {dL:.3f}（本类帧文档无限值）"))
        # 5 夜间
        if f.cls == "night":
            shade_on = cs.exposure_lab(m.shade, ev)
            lstar = ok.cie_lstar(shade_on)
            out.append(("PASS" if lstar >= 12.0 else "FAIL", s, k, "夜间暗部 L*", t, f"上屏影 L* {lstar:.1f}（≥ 12）"))
            if neutral and Cs >= NEUTRAL_C_MIN:
                bv = _in_range(hs, 240.0, 320.0)
                out.append(("PASS" if bv else "FAIL", s, k, "夜间蓝紫", t, f"影 h{hs:.0f}（240–320°）"))
            if t == "shadow.1" or (m.cls in LIGHT_FACE_CLASSES and La >= 0.80):
                okk = shade_on[0] >= 0.34
                out.append(("PASS" if okk else "FAIL", s, k, "夜间影 L 下限", t, f"上屏影 L{shade_on[0]:.3f}（≥ 0.34）"))
    # 天空
    zen = ok.lab_to_lch(f.sky_sun[4])
    if f.cls == "night":
        zl = cs.exposure_lab(f.sky_sun[4], ev)[0]
        out.append(("PASS" if zl >= 0.24 else "FAIL", s, k, "夜空天顶 L", "sky.sun.4", f"上屏 L{zl:.3f}（≥ 0.24）"))
        # 天空比建筑亮：地平线天色比月光下的浅墙受光亮（看剪影）
        wall = mats.get("wall")
        if wall is not None:
            hor = max(cs.exposure_lab(f.sky_sun[0], ev)[0], cs.exposure_lab(f.sky_anti[0], ev)[0])
            wl = cs.exposure_lab(wall.shade, ev)[0]
            out.append(("PASS" if hor > wl else "FAIL", s, k, "天空比建筑亮", "sky.*.0", f"地平线 L{hor:.3f} > 墙影 L{wl:.3f}"))
    if k == "noon":
        out.append(("PASS" if zen[1] >= 0.15 else "FAIL", s, k, "正午天顶彩度", "sky.sun.4", f"C {zen[1]:.3f}（≥ 0.15，SYS-TIME）"))
    # 线比影暗（夜间，SYS-TIME）：按 ART-STYLE 的线色公式与本帧的 line_darken、line.warm、line.cool 算
    if f.cls == "night":
        for t in ("shadow.1", "wall"):
            if t in mats:
                lc = cs.line_color(mats[t].shade, f)
                dl = mats[t].shade[0] - lc[0]
                okk = LINE_NIGHT[0] <= dl <= LINE_NIGHT[1]
                out.append(("PASS" if okk else "FAIL", s, k, "夜间线比影暗", t,
                            f"ΔL {dl:.3f}（{LINE_NIGHT[0]:.2f}–{LINE_NIGHT[1]:.2f}；line_darken {f.line_darken:.2f}）"))
    return out


def check_registered(src: cs.Sources, grid: dict) -> List[Result]:
    """默认格（秋 · 黄金）与正午与文档登记值比对（ART-STYLE；WLD-VEGETATION）。"""
    out: List[Result] = []
    gold = grid["materials"][("autumn", "golden")]
    noon = grid["materials"][("autumn", "noon")]
    for m in src.probe["materials"]:
        t = m["token"]
        reg = m.get("registered", {})
        for which in ("lit", "shade"):
            if which in reg and t in gold:
                got = getattr(gold[t], which)
                de = _de(got, ok.hex_to_oklab(reg[which]))
                out.append(("PASS" if de <= DE_REGISTERED else "INFO", "autumn", "golden", f"登记值 {which}", t,
                            f"{ok.oklab_to_hex(got)} 对 {reg[which]}：ΔE {de:.2f}"))
        if "noonShade" in m and t in noon:
            got = noon[t].shade
            de = _de(got, ok.hex_to_oklab(m["noonShade"]))
            out.append(("PASS" if de <= DE_REGISTERED else "INFO", "autumn", "noon", "示例 正午影", t,
                        f"{ok.oklab_to_hex(got)} 对 {m['noonShade']}：ΔE {de:.2f}"))
    targets = [("ginkgo", "shade", "#B78102"), ("ginkgo", "deep", "#9A6C03"), ("grass", "shade", "#71823E"),
               ("camphor", "shade", "#1E5C34"), ("camphor", "deep", "#114D28"), ("bloom.pink", "shade", "#BA97AB")]
    for t, which, hx in targets:
        got = getattr(gold[t], which)
        de = _de(got, ok.hex_to_oklab(hx))
        lvl = "PASS" if de <= DE_REGISTERED else ("FAIL" if (t, which) == ("ginkgo", "deep") else "INFO")
        out.append((lvl, "autumn", "golden", f"校验值 {which}", t, f"{ok.oklab_to_hex(got)} 对 {hx}：ΔE {de:.2f}"))
    for t, hx in (("ginkgo", "#CB9223"), ("grass", "#81924D"), ("camphor", "#2A673E")):
        got = noon[t].shade
        de = _de(got, ok.hex_to_oklab(hx))
        out.append(("PASS" if de <= DE_REGISTERED else "INFO", "autumn", "noon", "示例 正午影", t,
                    f"{ok.oklab_to_hex(got)} 对 {hx}：ΔE {de:.2f}"))
    return out


def run(src: cs.Sources, grid: dict) -> List[Result]:
    res: List[Result] = []
    for s in cs.SEASONS:
        for f in grid["frames"][s]:
            res += check_frame(src, f, grid["materials"][(s, f.id)])
    res += check_registered(src, grid)
    return res


def summarize(results: List[Result]) -> Dict[str, int]:
    out = {"PASS": 0, "FAIL": 0, "INFO": 0}
    for r in results:
        out[r[0]] += 1
    return out


def frame_status(results: List[Result]) -> Dict[Tuple[str, str], Tuple[int, int]]:
    """每格 (失败数, 通过数)。"""
    st: Dict[Tuple[str, str], List[int]] = {}
    for lvl, s, k, *_ in results:
        v = st.setdefault((s, k), [0, 0])
        if lvl == "FAIL":
            v[0] += 1
        elif lvl == "PASS":
            v[1] += 1
    return {key: (v[0], v[1]) for key, v in st.items()}


def format_report(results: List[Result]) -> str:
    lines = []
    summ = summarize(results)
    lines.append(f"影色自检（SYS-TIME）：PASS {summ['PASS']} · FAIL {summ['FAIL']} · INFO {summ['INFO']}")
    for lvl in ("FAIL", "INFO", "PASS"):
        rows = [r for r in results if r[0] == lvl]
        if not rows:
            continue
        lines.append("")
        lines.append(f"== {lvl}（{len(rows)}）")
        for _, s, k, item, t, msg in rows:
            lines.append(f"{cs.SEASON_NAMES[s]} {k:13s} {item:10s} {t:18s} {msg}")
    return "\n".join(lines) + "\n"

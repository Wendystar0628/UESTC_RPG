"""生成 game/style/palette/keys/registry.fallback.json：契约 PaletteRegistryData 格式的后备色名登记表。

WP-04 的正式登记表合入之前，运行时求值器用它把色名换成色号。色号布局照主代理定的方案与 WP-04 草案
（随帧 0–31、退役 32–39、天气与发光 40–63、表面色对 64 起、季相行 4–63），季相色名另占 118–177 的色号（第 0 行写当前季节的值）。
固有色取自 probe_materials.json 与 keys/*.json；同一份输入重建逐字节相同。
用法：python pipeline/colorscript/make_registry_fallback.py
"""

from __future__ import annotations

import json
import os

import oklab as ok

HERE = os.path.dirname(os.path.abspath(__file__))
KEYS = os.path.normpath(os.path.join(HERE, "..", "..", "game", "style", "palette", "keys"))

# 随帧与常量单色（0–31）：色号与 WP-04 草案一致
FRAME = [
    ("sun.light", 0), ("ambient.sky", 1), ("bounce.ground", 2), ("interior.warm", 3), ("line.warm", 4), ("line.cool", 5),
    ("shaft.near", 6), ("shaft.far", 7), ("rim", 8), ("glass.glare", 9), ("window.warm", 10), ("window.cool", 11),
    ("shadow.1", 12), ("fog.sun", 13), ("fog.anti", 14),
    ("sky.sun.0", 15), ("sky.sun.1", 16), ("sky.sun.2", 17), ("sky.sun.3", 18), ("sky.sun.4", 19),
    ("sky.anti.0", 20), ("sky.anti.1", 21), ("sky.anti.2", 22), ("sky.anti.3", 23), ("sky.anti.4", 24),
    ("sky.cloudLit", 25), ("sky.cloudRim", 26), ("sky.cloudBelly", 27), ("sky.cloudDeep", 28), ("sky.moon", 29),
]
RETIRED = [("shadow.contact", 32), ("line.building", 33), ("line.tree", 34), ("shadow.interior", 35)]
WEATHER = [("weather.overcast", 40), ("weather.snowOvercast", 41), ("weather.nightRain", 42), ("weather.cbBase", 43),
           ("weather.fog", 44), ("weather.washedSky", 45)]
EMISSIVE = [("sign.green", 48), ("firefly", 49), ("water.poolGlow", 50)]
# 表面色对（受光偶数、影 = 受光 + 1），族名与次序照 WP-04 草案
PAIRS = ["snow", "wall", "stone.beige", "wall.greyWhite", "wall.dormYellow", "wall.creamCoffee", "band.terracotta",
         "panel.yellowGreen", "panel.pinkSand", "panel.brown", "panel.paleGold", "tile.canteen", "base.warmBrown",
         "roof.grey", "roof.silverMetal", "frame.alu", "glass", "brick", "uestc.blue", "uestc.techblue", "uestc.gold",
         "bloom.capsule", "sign.wetFloor", "umbrella.grayblue", "umbrella.graygreen", "umbrella.beige", "umbrella.graypink"]
EXTRA_ALBEDO = {   # WP-04 草案里由它提议、WP-37 认可的固有色（ART-STYLE §8.6）
    "sign.wetFloor": ("#D6C27E", "fabric"), "umbrella.grayblue": ("#8C9CAE", "fabric"),
    "umbrella.graygreen": ("#93A294", "fabric"), "umbrella.beige": ("#D5C8AE", "fabric"),
    "umbrella.graypink": ("#C3A3A8", "fabric"),
}
CONSTANTS = {   # ART-STYLE §8.1、§8.6 的常量与天气色（参考值）
    "window.warm": "#FFE0A8", "window.cool": "#EEF4FF", "weather.snowOvercast": "#B8B4CC", "weather.cbBase": "#5A6090",
    "weather.fog": "#E4DCE4", "sign.green": "#4FBF7A", "firefly": "#DFFF8A", "water.poolGlow": "#8FD8E0",
}
SEASON_ROWS = ["ginkgo", "metaseq", "cherry", "maple", "willow", "plum", "deciduous", "jacaranda", "hibiscus", "camphor",
               "camphor.red", "nanmu", "palm", "photinia", "loropetalum", "hedge", "grass", "reed", "lotus", "tree.bare",
               "bloom.pink", "bloom.plum", "bloom.white", "bloom.violet", "bloom.rose", "bloom.crape", "bloom.yellow",
               "bloom.lotus", "plume", "crop"]
SEASON_ID0 = 118


def srgb(hex_: str):
    return [round(c, 6) for c in ok.hex_to_srgb(hex_)]


def lab_srgb(lab):
    return [round(c, 6) for c in ok.oklab_to_srgb_clipped(lab)]


def main() -> None:
    with open(os.path.join(KEYS, "autumn.keys.json"), encoding="utf-8") as f:
        autumn = json.load(f)
    with open(os.path.join(KEYS, "vegetation.json"), encoding="utf-8") as f:
        veg = json.load(f)
    with open(os.path.join(HERE, "probe_materials.json"), encoding="utf-8") as f:
        probe = {m["token"]: m for m in json.load(f)["materials"]}
    golden = next(k for k in autumn["keys"] if k["id"] == "golden")
    frame_default = {
        "sun.light": golden["key"]["color"], "bounce.ground": golden["colors"]["bounce.ground"],
        "line.warm": golden["line"]["warm"], "line.cool": golden["line"]["cool"],
        "shaft.near": golden["colors"]["shaft.near"], "shaft.far": golden["colors"]["shaft.far"],
        "rim": golden["colors"]["rim"], "glass.glare": golden["colors"]["glass.glare"],
        "fog.sun": golden["fog"]["sun"], "fog.anti": golden["fog"]["anti"], "shadow.1": "#A2A7BD",
        "sky.cloudLit": golden["colors"]["sky.cloudLit"], "sky.cloudRim": golden["colors"]["sky.cloudRim"],
        "sky.cloudBelly": golden["colors"]["sky.cloudBelly"], "sky.cloudDeep": golden["colors"]["sky.cloudDeep"], "sky.moon": golden["colors"]["sky.moon"],
    }
    for i in range(5):
        frame_default[f"sky.sun.{i}"] = golden["sky"]["sun"][i]
        frame_default[f"sky.anti.{i}"] = golden["sky"]["anti"][i]
    entries = []
    for token, i in FRAME:
        if token == "ambient.sky":
            col = lab_srgb(ok.lch_to_lab((0.75, 0.032, 275)))
        elif token == "interior.warm":
            col = lab_srgb(ok.lch_to_lab((0.80, 0.05, 65)))
        else:
            col = srgb(frame_default.get(token) or CONSTANTS[token])
        entries.append({"token": token, "id": i, "albedo": col, "materialClass": "frame", "seasonal": False, "role": "frame"})
    for token, i in RETIRED:
        entries.append({"token": token, "id": i, "albedo": [0.0, 0.0, 0.0], "materialClass": "retired", "seasonal": False, "role": "retired"})
    for token, i in WEATHER:
        if token == "weather.overcast":
            col = lab_srgb(ok.lch_to_lab((0.78, 0.022, 320)))
        elif token == "weather.nightRain":
            col = lab_srgb(ok.lch_to_lab((0.34, 0.05, 300)))
        elif token == "weather.washedSky":
            col = srgb("#435FB8")
        else:
            col = srgb(CONSTANTS[token])
        entries.append({"token": token, "id": i, "albedo": col, "materialClass": "weather", "seasonal": False, "role": "weather"})
    for token, i in EMISSIVE:
        entries.append({"token": token, "id": i, "albedo": srgb(CONSTANTS[token]), "materialClass": "emissive", "seasonal": False, "role": "emissive"})
    constant_veg = {v["token"]: v for v in veg["constant"]}
    for n, family in enumerate(PAIRS):
        lit_id = 64 + 2 * n
        if family in probe:
            albedo, cls = probe[family]["albedo"], probe[family]["class"]
        elif family in EXTRA_ALBEDO:
            albedo, cls = EXTRA_ALBEDO[family]
        elif family in constant_veg:
            albedo, cls = constant_veg[family]["lit"], constant_veg[family]["class"]   # 参考值：植被按英雄帧推导
        else:
            raise KeyError(family)
        for role, offset in (("lit", 0), ("shade", 1)):
            entries.append({"token": f"{family}.{role}", "id": lit_id + offset, "albedo": srgb(albedo), "materialClass": cls,
                            "seasonal": False, "role": role})
    seasonal = {v["token"]: v for v in veg["seasonal"]}
    for n, family in enumerate(SEASON_ROWS):
        row = 4 + 2 * n
        lit_id = SEASON_ID0 + 2 * n
        if family in seasonal:
            ref = seasonal[family]["lit"]["autumn"]
            ref = ref["albedo"] if isinstance(ref, dict) else ref
            cls = seasonal[family]["class"]
        else:
            ref, cls = constant_veg[family]["lit"], constant_veg[family]["class"]
        for role, offset in (("lit", 0), ("shade", 1)):
            entries.append({"token": f"{family}.{role}", "id": lit_id + offset, "albedo": srgb(ref), "materialClass": cls,
                            "seasonal": True, "row": row + offset, "role": role})
    entries.sort(key=lambda e: e["id"])
    out = {"schema": 1, "entries": entries}
    path = os.path.join(KEYS, "registry.fallback.json")
    with open(path, "w", encoding="utf-8", newline="\n") as f:
        f.write("{\n  \"schema\": 1,\n  \"entries\": [\n")
        f.write(",\n".join("    " + json.dumps(e, ensure_ascii=False) for e in entries))
        f.write("\n  ]\n}\n")
    print(path, len(entries))


if __name__ == "__main__":
    main()

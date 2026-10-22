"""从登记表、C# 规则值及共用雾源生成构建期着色器 include。
用法：python scripts/build/generate-style-shaders.py [--root 仓库根目录] [--list-inputs（只列原生增量构建输入）]
产出：game/style/shaders/style_consts.gdshaderinc；game/style/outline/fog.glslinc。
"""
from pathlib import Path
import argparse
import json
import re

PALETTE_SOURCE = "game/style/palette/core.json"
FOG_SOURCE = "game/style/shaders/fog.gdshaderinc"

# Explicit current consumers: path, C# literal member, shader name, shader type, enum (if any).
BINDINGS = [('game/contracts/Materials/PaletteEncoding.cs', 'FirstSeasonalRow', 'PAL_ROW_SEASON0', None, None),
 ('game/contracts/Materials/PaletteEncoding.cs', 'Bank0Height', 'PAL_BANK_ROWS', None, None),
 ('game/contracts/Materials/PaletteEncoding.cs', 'Capacity', 'PAL_CAPACITY', None, None),
 ('game/contracts/Materials/PaletteEncoding.cs', 'Width', 'PAL_WIDTH', None, None),
 ('game/contracts/Materials/PaletteEncoding.cs', 'SeasonSampleCount', 'PAL_SEASON_SAMPLES', None, None),
 ('game/contracts/Materials/PaletteEncoding.cs', 'ColorBankFlag', 'TOON_F_COLOR_BANK', None, None),
 ('game/contracts/Materials/PaletteEncoding.cs', 'DecalBaseBankFlag', 'TOON_F_DECAL_BANK', None, None),
 ('game/contracts/Materials/PaletteEncoding.cs', 'LampBankUnit', 'LAMP_COLOR_BANK_UNIT', None, None),
 ('game/contracts/Materials/PaletteEncoding.cs', 'LampEnergyUnit', 'LAMP_ENERGY_UNIT', None, None),
 ('game/contracts/Materials/PaletteEncoding.cs', 'LampMaxEnergyPercent', 'LAMP_ENERGY_PERCENT', None, None),
 ('game/contracts/Materials/PaletteEncoding.cs', 'CurrentRow', 'PAL_ROW_CUR', None, None),
 ('game/contracts/Materials/PaletteEncoding.cs', 'LampRow', 'PAL_ROW_LAMP', None, None),
 ('game/contracts/Materials/PaletteEncoding.cs', 'InteriorShadeRow', 'PAL_ROW_IN_SHADE', None, None),
 ('game/contracts/Materials/PaletteEncoding.cs', 'InteriorLitRow', 'PAL_ROW_IN_LIT', None, None),
 ('game/contracts/Materials/PaletteEncoding.cs', 'ShadeOffset', 'PAL_SHADE_OFFSET', None, None),
 ('game/contracts/Geometry/GeometryBundle.cs', 'Interior', 'TOON_F_INTERIOR', 'int', None),
 ('game/contracts/Geometry/GeometryBundle.cs', 'Outskirts', 'TOON_F_OUTSKIRT', 'int', None),
 ('game/contracts/Geometry/GeometryBundle.cs', 'Decal', 'TOON_F_DECAL', 'int', None),
 ('game/contracts/Geometry/GeometryBundle.cs', 'NoShadow', 'TOON_F_NOSHADOW', 'int', None),
 ('game/contracts/Materials/Style.cs', 'Two', 'TOON_STEPS_TWO', 'int', 'ShadeSteps'),
 ('game/contracts/Materials/Style.cs', 'TwoHalf', 'TOON_STEPS_TWO_HALF', 'int', 'ShadeSteps'),
 ('game/contracts/Materials/Style.cs', 'Soft', 'TOON_STEPS_SOFT', 'int', 'ShadeSteps'),
 ('game/contracts/Materials/Style.cs', 'Three', 'TOON_STEPS_THREE', 'int', 'ShadeSteps'),
 ('game/contracts/Materials/Style.cs', 'Escalator', 'MAT_ESCALATOR_FLAG', 'int', 'ToonFx'),
 ('game/contracts/Materials/WetSurface.cs', 'ScaleA', 'PW_SCALE_A', None, None),
 ('game/contracts/Materials/WetSurface.cs', 'ScaleB', 'PW_SCALE_B', None, None),
 ('game/contracts/Materials/WetSurface.cs', 'WeightA', 'PW_WEIGHT_A', None, None),
 ('game/contracts/Materials/WetSurface.cs', 'PuddleTop', 'PW_PUDDLE_TOP', None, None),
 ('game/contracts/Materials/WetSurface.cs', 'PuddleSpan', 'PW_PUDDLE_SPAN', None, None),
 ('game/contracts/Materials/WetSurface.cs', 'FlatFrom', 'PW_FLAT_FROM', None, None),
 ('game/contracts/Materials/WetSurface.cs', 'FlatTo', 'PW_FLAT_TO', None, None),
 ('game/contracts/Materials/WetSurface.cs', 'ResponseFrom', 'PW_RESPONSE_FROM', None, None),
 ('game/contracts/Materials/WetSurface.cs', 'ResponseTo', 'PW_RESPONSE_TO', None, None),
 ('game/contracts/Materials/WetSurface.cs', 'OffsetX', 'PW_OFFSET_X', None, None),
 ('game/contracts/Materials/WetSurface.cs', 'OffsetZ', 'PW_OFFSET_Z', None, None),
 ('game/contracts/Materials/WetSurface.cs', 'ExposureFrom', 'PW_EXPOSURE_FROM', None, None),
 ('game/contracts/Materials/WetSurface.cs', 'ExposureSide', 'PW_EXPOSURE_SIDE', None, None),
 ('game/contracts/Materials/WetSurface.cs', 'ExposureGain', 'PW_EXPOSURE_GAIN', None, None),
 ('game/contracts/Materials/WetSurface.cs', 'ExposureTo', 'PW_EXPOSURE_TO', None, None),
 ('game/contracts/Determinism/IntegerNoise.cs', 'PuddleMixA', 'STYLE_HASH_MIX_A', None, None),
 ('game/contracts/Determinism/IntegerNoise.cs', 'PuddleMixB', 'STYLE_HASH_MIX_B', None, None),
 ('game/contracts/Determinism/IntegerNoise.cs', 'PuddleSaltX', 'PW_SALT_X', None, None),
 ('game/contracts/Determinism/IntegerNoise.cs', 'PuddleSaltZ', 'PW_SALT_Z', None, None),
 ('game/contracts/Determinism/IntegerNoise.cs', 'Unit24', 'STYLE_UNIT24', None, None),
 ('game/systems/nightlight/Schedule/ScheduleMath.cs', 'OvercastDeg', 'LAMP_HEFF_OVERCAST', None, None),
 ('game/systems/nightlight/Schedule/ScheduleMath.cs', 'PrecipDeg', 'LAMP_HEFF_PRECIP', None, None),
 ('game/systems/nightlight/Schedule/ScheduleMath.cs', 'WindowRowSalt', 'LAMP_WINDOW_ROW_SALT', None, None),
 ('game/systems/nightlight/Schedule/ScheduleMath.cs', 'WindowGridSalt', 'LAMP_WINDOW_GRID_SALT', None, None),
 ('game/systems/nightlight/Schedule/ScheduleMath.cs',
  'WindowGroupColumns',
  'LAMP_WINDOW_GROUP_COLUMNS',
  None,
  None),
 ('game/systems/nightlight/Schedule/ScheduleMath.cs',
  'WindowIndependent',
  'LAMP_WINDOW_INDEPENDENT',
  None,
  None),
 ('game/systems/nightlight/Schedule/ScheduleMath.cs',
  'WindowBuildingSpread',
  'LAMP_WINDOW_BUILDING_SPREAD',
  None,
  None),
 ('game/systems/nightlight/Lamps/LampTable.cs', 'HeadLevels', 'LAMP_HEAD_LEVELS', None, None),
 ('game/systems/nightlight/Lamps/LampTable.cs', 'HeadMetresPerUnit', 'LAMP_HEAD_METRES', None, None),
 ('game/systems/nightlight/Lamps/LampTable.cs', 'SequenceUnit', 'LAMP_SEQUENCE_UNIT', None, None),
 ('game/contracts/Lighting/LightingContracts.cs', 'Count', 'LIGHT_CHANNEL_COUNT', None, None),
 ('game/systems/nightlight/Glow/GlowSets.cs', 'WindowHaloRatio', 'GLOW_WINDOW_RATIO', None, None),
 ('game/systems/nightlight/Glow/GlowSets.cs', 'OutskirtsFlag', 'GLOW_OUTSKIRTS_FLAG', None, None),
 ('game/systems/nightlight/Glow/GlowSets.cs', 'OutskirtsFirstId', 'WINDOW_CAMPUS_CAPACITY', None, None),
 ('game/style/palette/keys/ColorScriptDerive.cs', 'LampIntensityM', 'TOON_EMIT_ROW1_M', 'float', None),
 ('game/style/palette/keys/ColorScriptDerive.cs', 'TintK', 'TOON_EMIT_ROW1_TINT', 'float', None),
 ('game/style/shaders/MatTable.cs', 'Width', 'MAT_TABLE_WIDTH', None, None),
 ('game/style/shaders/MatTable.cs', 'FixedBandScale', 'MAT_FIXED_BAND_SCALE', None, None),
 ('game/style/shaders/MatTable.cs', 'FwidthBandScale', 'MAT_FWIDTH_BAND_SCALE', None, None),
 ('game/style/shaders/MatTable.cs', 'BevelScale', 'MAT_BEVEL_SCALE', None, None),
 ('game/style/shaders/MatTable.cs', 'LineOuterOnly', 'LF_OUTER_ONLY', 'uint', None),
 ('game/style/shaders/MatTable.cs', 'LineInnerSoft', 'LF_INNER_SOFT', 'uint', None),
 ('game/style/shaders/MatTable.cs', 'LineNone', 'LF_NOLINE', 'uint', None),
 ('game/style/shaders/cards/WindowCardKinds.cs', 'Desks', 'WC_F_DESKS', 'int', 'CardFurniture'),
 ('game/style/shaders/cards/WindowCardKinds.cs', 'Bench', 'WC_F_BENCH', 'int', 'CardFurniture'),
 ('game/style/shaders/cards/WindowCardKinds.cs', 'Shelf', 'WC_F_SHELF', 'int', 'CardFurniture'),
 ('game/style/shaders/cards/WindowCardKinds.cs', 'Bunk', 'WC_F_BUNK', 'int', 'CardFurniture'),
 ('game/style/shaders/cards/WindowCardKinds.cs', 'Stall', 'WC_F_STALL', 'int', 'CardFurniture'),
 ('game/style/shaders/cards/WindowCardKinds.cs', 'Tables', 'WC_F_TABLES', 'int', 'CardFurniture'),
 ('game/style/shaders/cards/WindowCardKinds.cs', 'Counter', 'WC_F_COUNTER', 'int', 'CardFurniture'),
 ('game/style/shaders/cards/WindowCardKinds.cs', 'Equip', 'WC_F_EQUIP', 'int', 'CardFurniture'),
 ('game/style/shaders/cards/WindowCardKinds.cs', 'Stair', 'WC_F_STAIR', 'int', 'CardFurniture'),
 ('game/style/shaders/cards/WindowCardKinds.cs', 'Turnstile', 'WC_F_TURNSTILE', 'int', 'CardFurniture'),
 ('game/style/shaders/cards/WindowCardKinds.cs', 'Elevator', 'WC_F_ELEVATOR', 'int', 'CardFurniture'),
 ('game/style/shaders/cards/WindowCardKinds.cs', 'Board', 'WC_S_BOARD', 'int', 'CardSide'),
 ('game/style/shaders/cards/WindowCardKinds.cs', 'Bunk', 'WC_S_BUNK', 'int', 'CardSide'),
 ('game/style/shaders/cards/WindowCardKinds.cs', 'Doors', 'WC_S_DOORS', 'int', 'CardSide'),
 ('game/style/shaders/cards/WindowCardKinds.cs', 'Shelf', 'WC_S_SHELF', 'int', 'CardSide'),
 ('game/style/shaders/cards/WindowCardKinds.cs', 'SideCurtain', 'WC_SCR_CURTAIN', 'int', 'CardScreen'),
 ('game/style/shaders/cards/WindowCardKinds.cs', 'Venetian', 'WC_SCR_VENETIAN', 'int', 'CardScreen'),
 ('game/style/shaders/cards/WindowCardKinds.cs', 'Frosted', 'WC_SCR_FROSTED', 'int', 'CardScreen'),
 ('game/style/shaders/cards/WindowCardKinds.cs', 'Roller', 'WC_SCR_ROLLER', 'int', 'CardScreen'),
 ('game/style/shaders/cards/WindowCardKinds.cs', 'Strips', 'WC_C_STRIPS', 'int', 'CardCeiling'),
 ('game/style/shaders/cards/WindowCardKinds.cs', 'Panels', 'WC_C_PANELS', 'int', 'CardCeiling'),
 ('game/style/shaders/cards/WindowCardKinds.cs', 'Single', 'WC_C_SINGLE', 'int', 'CardCeiling'),
 ('game/style/shaders/cards/WindowCardKinds.cs', 'Downlights', 'WC_C_DOWN', 'int', 'CardCeiling'),
 ('game/style/shaders/cards/WindowCardKinds.cs', 'Truss', 'WC_C_TRUSS', 'int', 'CardCeiling'),
 ('game/style/shaders/cards/WindowCardKinds.cs', 'Exit', 'WC_A_EXIT', 'int', 'CardAccent'),
 ('game/style/shaders/cards/WindowCardKinds.cs', 'Amber', 'WC_A_AMBER', 'int', 'CardAccent'),
 ('game/style/shaders/cards/WindowCardKinds.cs', 'BedCurtain', 'WC_A_BEDCURTAIN', 'int', 'CardAccent'),
 ('game/style/shaders/cards/WindowCardKinds.cs', 'LightBox', 'WC_A_LIGHTBOX', 'int', 'CardAccent'),
 ('game/style/shaders/cards/WindowCardKinds.cs', 'Water', 'WC_A_WATER', 'int', 'CardAccent'),
 ('game/style/shaders/cards/WindowCardKinds.cs', 'Indicator', 'WC_A_INDICATOR', 'int', 'CardAccent'),
 ('game/style/shaders/cards/WindowCardKinds.cs', 'Lobby', 'WC_A_LOBBY', 'int', 'CardAccent'),
 ('game/contracts/Lighting/WindowCardEncoding.cs', 'TypeMask', 'WCARD_TYPE_MASK', 'int', None),
 ('game/contracts/Lighting/WindowCardEncoding.cs',
  'PhysicalCurtainFlag',
  'WCARD_PHYSICAL_CURTAIN',
  'int',
  None),
 ('game/style/palette/keys/ColorScriptCurves.cs', 'NightSideFromDeg', 'SKY_NIGHT_SIDE_FROM', None, None),
 ('game/style/palette/keys/ColorScriptCurves.cs', 'NightSideToDeg', 'SKY_NIGHT_SIDE_TO', None, None),
 ('game/systems/vegetation/VegFade.cs', 'CloudReady', 'VEG_CLOUD_READY', None, None),
 ('game/gen/vegetation/Understorey.cs', 'UnderVariantBit', 'VEG_VARIANT_UNDER', None, None)]

# Only existing shader consumers are named here; their indices live in core.json.
PALETTE_BINDINGS = [
    ('sun.light', 'PAL_SUN_LIGHT'),
    ('ambient.sky', 'PAL_AMBIENT_SKY'),
    ('bounce.ground', 'PAL_BOUNCE_GROUND'),
    ('interior.warm', 'PAL_INTERIOR_WARM'),
    ('line.warm', 'PAL_LINE_WARM'),
    ('line.cool', 'PAL_LINE_COOL'),
    ('rim', 'PAL_RIM'),
    ('glass.glare', 'PAL_GLASS_GLARE'),
    ('window.warm', 'PAL_WINDOW_WARM'),
    ('window.cool', 'PAL_WINDOW_COOL'),
    ('shadow.1', 'PAL_SHADOW_1'),
    ('fog.sun', 'PAL_FOG_SUN'),
    ('fog.anti', 'PAL_FOG_ANTI'),
    ('sky.sun.0', 'PAL_SKY_SUN0'),
    ('sky.anti.0', 'PAL_SKY_ANTI0'),
    ('sky.cloudLit', 'PAL_SKY_CLOUD_LIT'),
    ('sky.cloudRim', 'PAL_SKY_CLOUD_RIM'),
    ('sky.cloudBelly', 'PAL_SKY_CLOUD_BELLY'),
    ('sky.cloudDeep', 'PAL_SKY_CLOUD_DEEP'),
    ('sky.moon', 'PAL_SKY_MOON'),
    ('weather.overcast', 'PAL_WEATHER_OVERCAST'),
    ('weather.snowOvercast', 'PAL_WEATHER_SNOW_OVERCAST'),
    ('weather.nightRain', 'PAL_WEATHER_NIGHT_RAIN'),
    ('weather.cbBase', 'PAL_WEATHER_CB_BASE'),
    ('weather.fog', 'PAL_WEATHER_FOG'),
    ('weather.washedSky', 'PAL_WEATHER_WASHED_SKY'),
    ('sign.green', 'PAL_SIGN_GREEN'),
    ('firefly', 'PAL_FIREFLY'),
    ('water.poolGlow', 'PAL_WATER_POOL_GLOW'),
    ('lamp.warm', 'PAL_LAMP_WARM'),
    ('lamp.amber', 'PAL_LAMP_AMBER'),
    ('lamp.cool', 'PAL_LAMP_COOL'),
]

# Existing scalar rule pairs form the shader's distance bands.
VECTOR_BINDINGS = [
    ('game/systems/vegetation/VegFade.cs', 'NearFadeStart', 'NearFadeEnd', 'VEG_POOL_FADE'),
    ('game/systems/vegetation/VegFade.cs', 'CloudFadeStart', 'CloudFadeEnd', 'VEG_CLOUD_FADE'),
    ('game/gen/vegetation/Understorey.cs', 'FadeStart', 'DrawDistance', 'VEG_UNDER_FADE'),
]


def source_inputs(root):
    paths = {binding[0] for binding in BINDINGS + VECTOR_BINDINGS}
    paths.update((PALETTE_SOURCE, FOG_SOURCE))
    return sorted({str((root / path).resolve()) for path in paths} | {str(Path(__file__).resolve())})


def literal_rule(root, path, member, kind=None, enum=None):
    source = (root / path).read_text(encoding="utf-8-sig")
    if enum:
        source = re.search(r"\benum " + enum + r"\b[^{}]*\{([^}]*)\}", source, re.S).group(1)
        match = re.search(r"\b" + member + r"\s*=\s*(0x[0-9a-fA-F]+|[0-9]+)\b", source)
        value = match.group(1) if match else None
        kind = "int"
    else:
        match = re.search(r"\bconst\s+(int|byte|uint|float|double|bool)\s+" + member +
                          r"\s*=\s*(-?(?:0x[0-9a-fA-F]+|[0-9]+(?:\.[0-9]+)?(?:[eE][+-]?[0-9]+)?)[fFdDuU]?|true|false)\s*;", source)
        value = match.group(2) if match else None
        if match and kind is None:
            kind = {"byte": "int", "double": "float"}.get(match.group(1), match.group(1))
    if value is None:
        raise ValueError(f"Expected literal C# rule {path}: {member}")
    if kind == "bool":
        return kind, value
    value = re.sub(r"[uU]$" if value.lstrip("-").startswith("0x") else r"[fFdDuU]$", "", value)
    if kind == "float" and not re.search(r"[.eE]", value):
        value += ".0"
    if kind == "uint":
        value += "u"
    return kind, value


def style_constants(root):
    lines = ["// Generated by scripts/build/generate-style-shaders.py; edit the rule source or palette registry.",
             "#ifndef CAMPUS_STYLE_CONSTS_INCLUDED", "#define CAMPUS_STYLE_CONSTS_INCLUDED", ""]
    for path, member, name, kind, enum in BINDINGS:
        kind, value = literal_rule(root, path, member, kind, enum)
        lines.append(f"const {kind} {name} = {value};")
    palette = json.loads((root / PALETTE_SOURCE).read_text(encoding="utf-8-sig"))
    indices = {entry["token"]: entry["index"] for entry in palette["entries"]}
    for token, name in PALETTE_BINDINGS:
        lines.append(f"const int {name} = {indices[token]};")
    for path, first, last, name in VECTOR_BINDINGS:
        _, a = literal_rule(root, path, first, "float")
        _, b = literal_rule(root, path, last, "float")
        lines.append(f"const vec2 {name} = vec2({a}, {b});")
    return "\n".join(lines + ["", "#endif", ""])

def fog_glsl(root):
    source = (root / FOG_SOURCE).read_text(encoding="utf-8-sig")
    # RDShaderFile expands only main-file includes; post_outline.glsl imports
    # these constants before fog.glslinc instead of leaving a nested directive.
    source = source.replace('#include "res://style/shaders/style_consts.gdshaderinc"\n', '')
    # Godot array initializers use braces; GLSL uses a typed array constructor.
    source = re.sub(r"const (\w+) (\w+)\[(\d+)\] = \{([^}]+)\};",
                    lambda m: f"const {m[1]} {m[2]}[{m[3]}] = {m[1]}[{m[3]}]({m[4]});", source)
    return "// Generated by scripts/build/generate-style-shaders.py from fog.gdshaderinc.\n" + source

def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--root", type=Path, default=Path(__file__).resolve().parents[2])
    parser.add_argument("--list-inputs", action="store_true")
    args = parser.parse_args()
    if args.list_inputs:
        print("\n".join(source_inputs(args.root)))
        return
    outputs = {"game/style/shaders/style_consts.gdshaderinc": style_constants(args.root),
               "game/style/outline/fog.glslinc": fog_glsl(args.root)}
    for path, content in outputs.items():
        target = args.root / path
        if not target.exists() or target.read_bytes() != content.encode("utf-8"):
            target.write_bytes(content.encode("utf-8"))

if __name__ == "__main__":
    main()

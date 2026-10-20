"""从 C# 规则值及共用雾源生成着色器 include；--check 检查随仓库的生成物是否最新。
用法：python scripts/build/generate-style-shaders.py [--root 仓库根目录] [--check]
"""
from pathlib import Path
import argparse
import re

# Explicit current consumers: path, C# literal member, shader name, shader type, enum (if any).
BINDINGS = [('game/contracts/Foundation/PaletteEncoding.cs', 'FirstSeasonalRow', 'PAL_ROW_SEASON0', None, None),
 ('game/contracts/Foundation/PaletteEncoding.cs', 'LegacyHeight', 'PAL_BANK_ROWS', None, None),
 ('game/contracts/Foundation/PaletteEncoding.cs', 'Capacity', 'PAL_CAPACITY', None, None),
 ('game/contracts/Foundation/PaletteEncoding.cs', 'Width', 'PAL_WIDTH', None, None),
 ('game/contracts/Foundation/PaletteEncoding.cs', 'SeasonSampleCount', 'PAL_SEASON_SAMPLES', None, None),
 ('game/contracts/Foundation/PaletteEncoding.cs', 'ColorBankFlag', 'TOON_F_COLOR_BANK', None, None),
 ('game/contracts/Foundation/PaletteEncoding.cs', 'DecalBaseBankFlag', 'TOON_F_DECAL_BANK', None, None),
 ('game/contracts/Foundation/PaletteEncoding.cs', 'LampBankUnit', 'LAMP_COLOR_BANK_UNIT', None, None),
 ('game/contracts/Foundation/PaletteEncoding.cs', 'LampEnergyUnit', 'LAMP_ENERGY_UNIT', None, None),
 ('game/contracts/Foundation/PaletteEncoding.cs', 'LampMaxEnergyPercent', 'LAMP_ENERGY_PERCENT', None, None),
 ('game/contracts/Foundation/PaletteEncoding.cs', 'CurrentRow', 'PAL_ROW_CUR', None, None),
 ('game/contracts/Foundation/PaletteEncoding.cs', 'LampRow', 'PAL_ROW_LAMP', None, None),
 ('game/contracts/Foundation/PaletteEncoding.cs', 'InteriorShadeRow', 'PAL_ROW_IN_SHADE', None, None),
 ('game/contracts/Foundation/PaletteEncoding.cs', 'InteriorLitRow', 'PAL_ROW_IN_LIT', None, None),
 ('game/contracts/Foundation/PaletteEncoding.cs', 'ShadeOffset', 'PAL_SHADE_OFFSET', None, None),
 ('game/contracts/Foundation/Content.cs', 'Interior', 'TOON_F_INTERIOR', 'int', None),
 ('game/contracts/Foundation/Content.cs', 'Outskirts', 'TOON_F_OUTSKIRT', 'int', None),
 ('game/contracts/Foundation/Content.cs', 'Decal', 'TOON_F_DECAL', 'int', None),
 ('game/contracts/Foundation/Content.cs', 'NoShadow', 'TOON_F_NOSHADOW', 'int', None),
 ('game/contracts/Foundation/Style.cs', 'Two', 'TOON_STEPS_TWO', 'int', 'ShadeSteps'),
 ('game/contracts/Foundation/Style.cs', 'TwoHalf', 'TOON_STEPS_TWO_HALF', 'int', 'ShadeSteps'),
 ('game/contracts/Foundation/Style.cs', 'Soft', 'TOON_STEPS_SOFT', 'int', 'ShadeSteps'),
 ('game/contracts/Foundation/Style.cs', 'Three', 'TOON_STEPS_THREE', 'int', 'ShadeSteps'),
 ('game/contracts/Foundation/Style.cs', 'Escalator', 'MAT_ESCALATOR_FLAG', 'int', 'ToonFx'),
 ('game/contracts/Foundation/WetSurface.cs', 'ScaleA', 'PW_SCALE_A', None, None),
 ('game/contracts/Foundation/WetSurface.cs', 'ScaleB', 'PW_SCALE_B', None, None),
 ('game/contracts/Foundation/WetSurface.cs', 'WeightA', 'PW_WEIGHT_A', None, None),
 ('game/contracts/Foundation/WetSurface.cs', 'PuddleTop', 'PW_PUDDLE_TOP', None, None),
 ('game/contracts/Foundation/WetSurface.cs', 'PuddleSpan', 'PW_PUDDLE_SPAN', None, None),
 ('game/contracts/Foundation/WetSurface.cs', 'FlatFrom', 'PW_FLAT_FROM', None, None),
 ('game/contracts/Foundation/WetSurface.cs', 'FlatTo', 'PW_FLAT_TO', None, None),
 ('game/contracts/Foundation/WetSurface.cs', 'ResponseFrom', 'PW_RESPONSE_FROM', None, None),
 ('game/contracts/Foundation/WetSurface.cs', 'ResponseTo', 'PW_RESPONSE_TO', None, None),
 ('game/contracts/Foundation/WetSurface.cs', 'OffsetX', 'PW_OFFSET_X', None, None),
 ('game/contracts/Foundation/WetSurface.cs', 'OffsetZ', 'PW_OFFSET_Z', None, None),
 ('game/contracts/Foundation/WetSurface.cs', 'ExposureFrom', 'PW_EXPOSURE_FROM', None, None),
 ('game/contracts/Foundation/WetSurface.cs', 'ExposureSide', 'PW_EXPOSURE_SIDE', None, None),
 ('game/contracts/Foundation/WetSurface.cs', 'ExposureGain', 'PW_EXPOSURE_GAIN', None, None),
 ('game/contracts/Foundation/WetSurface.cs', 'ExposureTo', 'PW_EXPOSURE_TO', None, None),
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
 ('game/contracts/Foundation/LightingContracts.cs', 'Count', 'LIGHT_CHANNEL_COUNT', None, None),
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
 ('game/style/shaders/cards/WindowCardKinds.cs', 'Office', 'WCARD_DEFAULT', 'int', 'WindowCardKind'),
 ('game/style/shaders/cards/WindowCardTable.cs', 'Width', 'WCARD_WIDTH', None, None),
 ('game/style/shaders/cards/WindowCardTable.cs', 'Rows', 'WCARD_ROWS', None, None),
 ('game/style/shaders/cards/WindowCardTable.cs', 'Tag', 'WCARD_TAG', None, None),
 ('game/contracts/Foundation/WindowCardEncoding.cs', 'TypeMask', 'WCARD_TYPE_MASK', 'int', None),
 ('game/contracts/Foundation/WindowCardEncoding.cs',
  'PhysicalCurtainFlag',
  'WCARD_PHYSICAL_CURTAIN',
  'int',
  None)]

def style_constants(root):
    lines = ["// Generated by scripts/build/generate-style-shaders.py; edit the C# rule source.",
             "#ifndef CAMPUS_STYLE_CONSTS_INCLUDED", "#define CAMPUS_STYLE_CONSTS_INCLUDED", ""]
    for path, member, name, kind, enum in BINDINGS:
        source = (root / path).read_text(encoding="utf-8-sig")
        if enum:
            source = re.search(r"\benum " + enum + r"\b[^{}]*\{([^}]*)\}", source, re.S).group(1)
            match = re.search(r"\b" + member + r"\s*=\s*(0x[0-9a-fA-F]+|[0-9]+)\b", source)
            value = match.group(1) if match else None
            kind = "int"
        else:
            match = re.search(r"\bconst\s+(int|byte|uint|float|double)\s+" + member +
                              r"\s*=\s*(-?(?:0x[0-9a-fA-F]+|[0-9]+(?:\.[0-9]+)?(?:[eE][+-]?[0-9]+)?)[fFdDuU]?)\s*;", source)
            value = match.group(2) if match else None
            if match and kind is None:
                kind = {"byte": "int", "double": "float"}.get(match.group(1), match.group(1))
        if value is None:
            raise ValueError(f"Expected literal C# rule {path}: {member}")
        value = re.sub(r"[uU]$" if value.lstrip("-").startswith("0x") else r"[fFdDuU]$", "", value)
        if kind == "float" and not re.search(r"[.eE]", value):
            value += ".0"
        if kind == "uint":
            value += "u"
        lines.append(f"const {kind} {name} = {value};")
    return "\n".join(lines + ["", "#endif", ""])

def fog_glsl(root):
    source = (root / "game/style/shaders/fog.gdshaderinc").read_text(encoding="utf-8-sig")
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
    parser.add_argument("--check", action="store_true")
    args = parser.parse_args()
    outputs = {"game/style/shaders/style_consts.gdshaderinc": style_constants(args.root),
               "game/style/outline/fog.glslinc": fog_glsl(args.root)}
    stale = []
    for path, content in outputs.items():
        target = args.root / path
        if not target.exists() or target.read_bytes() != content.encode("utf-8"):
            stale.append(path)
            if not args.check:
                target.write_bytes(content.encode("utf-8"))
    if args.check and stale:
        parser.exit(1, "Stale generated shaders; run scripts/build/generate-style-shaders.py: " + ", ".join(stale) + "\n")

if __name__ == "__main__":
    main()

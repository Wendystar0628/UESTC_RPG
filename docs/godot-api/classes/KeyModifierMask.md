<!-- 由 scripts/godot-api/gen-api-docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# KeyModifierMask

命名空间 `Godot`；enum

## 枚举值

- `CodeMask = 8388607`：Bit mask with all bits enabled except for modifier keys.
- `ModifierMask = 2130706432`：Bit mask with all modifier bits enabled.
- `MaskCmdOrCtrl = 16777216`：Automatically remapped to [`Key.Meta`](Key.md) on macOS and [`Key.Ctrl`](Key.md) on other platforms, this mask is never set in the actual events, and should be used for key mapping only.
- `MaskShift = 33554432`：Shift key mask.
- `MaskAlt = 67108864`：Alt or Option (on macOS) key mask.
- `MaskMeta = 134217728`：Command (on macOS) or Meta/Windows key mask.
- `MaskCtrl = 268435456`：Control key mask.
- `MaskKpad = 536870912`：Keypad key mask.
- `MaskGroupSwitch = 1073741824`：Group Switch key mask.

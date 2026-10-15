<!-- 由 scripts/godot-api/gen_api_docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# ColorPicker

命名空间 `Godot`；class；继承 [`VBoxContainer`](VBoxContainer.md) → [`BoxContainer`](BoxContainer.md) → [`Container`](Container.md) → [`Control`](Control.md) → [`CanvasItem`](CanvasItem.md) → [`Node`](Node.md) → [`GodotObject`](GodotObject.md)

A widget that provides an interface for selecting or modifying a color. It can optionally provide functionalities like a color sampler (eyedropper), color modes, and presets.

**Note:** This control is the color picker widget itself. You can use a [`ColorPickerButton`](ColorPickerButton.md) instead if you need a button that brings up a `ColorPicker` in a popup.

## 构造函数

### ColorPicker

```csharp
public ColorPicker()
```

## 属性

### CanAddSwatches

```csharp
public bool CanAddSwatches { get; set; }
```

引擎名：`can_add_swatches`

If `true`, it's possible to add presets under Swatches. If `false`, the button to add presets is disabled.

### Color

```csharp
public Color Color { get; set; }
```

引擎名：`color`

The currently selected color.

### ColorMode

```csharp
public ColorPicker.ColorModeType ColorMode { get; set; }
```

引擎名：`color_mode`

The currently selected color mode.

### ColorModesVisible

```csharp
public bool ColorModesVisible { get; set; }
```

引擎名：`color_modes_visible`

If `true`, the color mode buttons are visible.

### DeferredMode

```csharp
public bool DeferredMode { get; set; }
```

引擎名：`deferred_mode`

If `true`, the color will apply only after the user releases the mouse button, otherwise it will apply immediately even in mouse motion event (which can cause performance issues).

### EditAlpha

```csharp
public bool EditAlpha { get; set; }
```

引擎名：`edit_alpha`

If `true`, shows an alpha channel slider (opacity).

### EditIntensity

```csharp
public bool EditIntensity { get; set; }
```

引擎名：`edit_intensity`

If `true`, shows an intensity slider. The intensity is applied as follows: convert the color to linear encoding, multiply it by `2 ** intensity`, and then convert it back to nonlinear sRGB encoding.

### HexVisible

```csharp
public bool HexVisible { get; set; }
```

引擎名：`hex_visible`

If `true`, the hex color code input field is visible.

### PickerShape

```csharp
public ColorPicker.PickerShapeType PickerShape { get; set; }
```

引擎名：`picker_shape`

The shape of the color space view.

### PresetsVisible

```csharp
public bool PresetsVisible { get; set; }
```

引擎名：`presets_visible`

If `true`, the Swatches and Recent Colors presets are visible.

### SamplerVisible

```csharp
public bool SamplerVisible { get; set; }
```

引擎名：`sampler_visible`

If `true`, the color sampler and color preview are visible.

### SlidersVisible

```csharp
public bool SlidersVisible { get; set; }
```

引擎名：`sliders_visible`

If `true`, the color sliders are visible.

## 方法

### AddPreset

```csharp
public void AddPreset(Color color)
```

引擎名：`add_preset`

Adds the given color to a list of color presets. The presets are displayed in the color picker and the user will be able to select them.

**Note:** The presets list is only for *this* color picker.

### AddRecentPreset

```csharp
public void AddRecentPreset(Color color)
```

引擎名：`add_recent_preset`

Adds the given color to a list of color recent presets so that it can be picked later. Recent presets are the colors that were picked recently, a new preset is automatically created and added to recent presets when you pick a new color.

**Note:** The recent presets list is only for *this* color picker.

### AreModesVisible

```csharp
public bool AreModesVisible()
```

引擎名：`are_modes_visible`

### ArePresetsVisible

```csharp
public bool ArePresetsVisible()
```

引擎名：`are_presets_visible`

### AreSlidersVisible

```csharp
public bool AreSlidersVisible()
```

引擎名：`are_sliders_visible`

### AreSwatchesEnabled

```csharp
public bool AreSwatchesEnabled()
```

引擎名：`are_swatches_enabled`

### ErasePreset

```csharp
public void ErasePreset(Color color)
```

引擎名：`erase_preset`

Removes the given color from the list of color presets of this color picker.

### EraseRecentPreset

```csharp
public void EraseRecentPreset(Color color)
```

引擎名：`erase_recent_preset`

Removes the given color from the list of color recent presets of this color picker.

### GetColorMode

```csharp
public ColorPicker.ColorModeType GetColorMode()
```

引擎名：`get_color_mode`

### GetPickColor

```csharp
public Color GetPickColor()
```

引擎名：`get_pick_color`

### GetPickerShape

```csharp
public ColorPicker.PickerShapeType GetPickerShape()
```

引擎名：`get_picker_shape`

### GetPresets

```csharp
public Color[] GetPresets()
```

引擎名：`get_presets`

Returns the list of colors in the presets of the color picker.

### GetRecentPresets

```csharp
public Color[] GetRecentPresets()
```

引擎名：`get_recent_presets`

Returns the list of colors in the recent presets of the color picker.

### IsDeferredMode

```csharp
public bool IsDeferredMode()
```

引擎名：`is_deferred_mode`

### IsEditingAlpha

```csharp
public bool IsEditingAlpha()
```

引擎名：`is_editing_alpha`

### IsEditingIntensity

```csharp
public bool IsEditingIntensity()
```

引擎名：`is_editing_intensity`

### IsHexVisible

```csharp
public bool IsHexVisible()
```

引擎名：`is_hex_visible`

### IsSamplerVisible

```csharp
public bool IsSamplerVisible()
```

引擎名：`is_sampler_visible`

### SetCanAddSwatches

```csharp
public void SetCanAddSwatches(bool enabled)
```

引擎名：`set_can_add_swatches`

### SetColorMode

```csharp
public void SetColorMode(ColorPicker.ColorModeType colorMode)
```

引擎名：`set_color_mode`

### SetDeferredMode

```csharp
public void SetDeferredMode(bool mode)
```

引擎名：`set_deferred_mode`

### SetEditAlpha

```csharp
public void SetEditAlpha(bool show)
```

引擎名：`set_edit_alpha`

### SetEditIntensity

```csharp
public void SetEditIntensity(bool show)
```

引擎名：`set_edit_intensity`

### SetHexVisible

```csharp
public void SetHexVisible(bool visible)
```

引擎名：`set_hex_visible`

### SetModesVisible

```csharp
public void SetModesVisible(bool visible)
```

引擎名：`set_modes_visible`

### SetPickColor

```csharp
public void SetPickColor(Color color)
```

引擎名：`set_pick_color`

### SetPickerShape

```csharp
public void SetPickerShape(ColorPicker.PickerShapeType shape)
```

引擎名：`set_picker_shape`

### SetPresetsVisible

```csharp
public void SetPresetsVisible(bool visible)
```

引擎名：`set_presets_visible`

### SetSamplerVisible

```csharp
public void SetSamplerVisible(bool visible)
```

引擎名：`set_sampler_visible`

### SetSlidersVisible

```csharp
public void SetSlidersVisible(bool visible)
```

引擎名：`set_sliders_visible`

## 事件（信号）

### ColorChanged

```csharp
public event ColorPicker.ColorChangedEventHandler ColorChanged
```

引擎名：`color_changed`

Emitted when the color is changed.

### PresetAdded

```csharp
public event ColorPicker.PresetAddedEventHandler PresetAdded
```

引擎名：`preset_added`

Emitted when a preset is added.

### PresetRemoved

```csharp
public event ColorPicker.PresetRemovedEventHandler PresetRemoved
```

引擎名：`preset_removed`

Emitted when a preset is removed.

## ColorChangedEventHandler

命名空间 `Godot`；delegate

Represents the method that handles the [`ColorPicker.ColorChanged`](ColorPicker.md) event of a [`ColorPicker`](ColorPicker.md) class.

### 签名

#### Invoke

```csharp
delegate void ColorChangedEventHandler(Color color)
```

## ColorModeType

命名空间 `Godot`；enum

### 枚举值

- `Rgb = 0`：Allows editing the color with Red/Green/Blue sliders in sRGB color space.
- `Hsv = 1`：Allows editing the color with Hue/Saturation/Value sliders.
- `Raw = 2`
- `Linear = 2`：Allows editing the color with Red/Green/Blue sliders in linear color space.
- `Okhsl = 3`：Allows editing the color with Hue/Saturation/Lightness sliders.

## PickerShapeType

命名空间 `Godot`；enum

### 枚举值

- `HsvRectangle = 0`：HSV Color Model rectangle color space.
- `HsvWheel = 1`：HSV Color Model rectangle color space with a wheel.
- `VhsCircle = 2`：HSV Color Model circle color space.
- `OkhslCircle = 3`：HSL OK Color Model circle color space.
- `None = 4`：The color space shape and the shape select button are hidden.
- `OkHsRectangle = 5`：OKHSL Color Model rectangle with constant lightness.
- `OkHlRectangle = 6`：OKHSL Color Model rectangle with constant saturation.

## PresetAddedEventHandler

命名空间 `Godot`；delegate

Represents the method that handles the [`ColorPicker.PresetAdded`](ColorPicker.md) event of a [`ColorPicker`](ColorPicker.md) class.

### 签名

#### Invoke

```csharp
delegate void PresetAddedEventHandler(Color color)
```

## PresetRemovedEventHandler

命名空间 `Godot`；delegate

Represents the method that handles the [`ColorPicker.PresetRemoved`](ColorPicker.md) event of a [`ColorPicker`](ColorPicker.md) class.

### 签名

#### Invoke

```csharp
delegate void PresetRemovedEventHandler(Color color)
```

<!-- 由 scripts/godot-api/gen_api_docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# Light2D

命名空间 `Godot`；class；继承 [`Node2D`](Node2D.md) → [`CanvasItem`](CanvasItem.md) → [`Node`](Node.md) → [`GodotObject`](GodotObject.md)

Casts light in a 2D environment. A light is defined as a color, an energy value, a mode (see constants), and various other parameters (range and shadows-related).

## 属性

### BlendMode

```csharp
public Light2D.BlendModeEnum BlendMode { get; set; }
```

引擎名：`blend_mode`

The Light2D's blend mode.

### Color

```csharp
public Color Color { get; set; }
```

引擎名：`color`

The Light2D's [`Color`](Color.md).

### EditorOnly

```csharp
public bool EditorOnly { get; set; }
```

引擎名：`editor_only`

If `true`, Light2D will only appear when editing the scene.

### Enabled

```csharp
public bool Enabled { get; set; }
```

引擎名：`enabled`

If `true`, Light2D will emit light.

### Energy

```csharp
public float Energy { get; set; }
```

引擎名：`energy`

The Light2D's energy value. The larger the value, the stronger the light.

### RangeItemCullMask

```csharp
public int RangeItemCullMask { get; set; }
```

引擎名：`range_item_cull_mask`

The layer mask. Only objects with a matching [`CanvasItem.LightMask`](CanvasItem.md) will be affected by the Light2D. See also `ShadowItemCullMask`, which affects which objects can cast shadows.

**Note:** `RangeItemCullMask` is ignored by [`DirectionalLight2D`](DirectionalLight2D.md), which will always light a 2D node regardless of the 2D node's [`CanvasItem.LightMask`](CanvasItem.md).

### RangeLayerMax

```csharp
public int RangeLayerMax { get; set; }
```

引擎名：`range_layer_max`

Maximum layer value of objects that are affected by the Light2D.

### RangeLayerMin

```csharp
public int RangeLayerMin { get; set; }
```

引擎名：`range_layer_min`

Minimum layer value of objects that are affected by the Light2D.

### RangeZMax

```csharp
public int RangeZMax { get; set; }
```

引擎名：`range_z_max`

Maximum `z` value of objects that are affected by the Light2D.

### RangeZMin

```csharp
public int RangeZMin { get; set; }
```

引擎名：`range_z_min`

Minimum `z` value of objects that are affected by the Light2D.

### ShadowColor

```csharp
public Color ShadowColor { get; set; }
```

引擎名：`shadow_color`

[`Color`](Color.md) of shadows cast by the Light2D.

### ShadowEnabled

```csharp
public bool ShadowEnabled { get; set; }
```

引擎名：`shadow_enabled`

If `true`, the Light2D will cast shadows.

### ShadowFilter

```csharp
public Light2D.ShadowFilterEnum ShadowFilter { get; set; }
```

引擎名：`shadow_filter`

Shadow filter type.

### ShadowFilterSmooth

```csharp
public float ShadowFilterSmooth { get; set; }
```

引擎名：`shadow_filter_smooth`

Smoothing value for shadows. Higher values will result in softer shadows, at the cost of visible streaks that can appear in shadow rendering. `ShadowFilterSmooth` only has an effect if `ShadowFilter` is [`Light2D.ShadowFilterEnum.Pcf5`](Light2D.md) or [`Light2D.ShadowFilterEnum.Pcf13`](Light2D.md).

### ShadowItemCullMask

```csharp
public int ShadowItemCullMask { get; set; }
```

引擎名：`shadow_item_cull_mask`

The shadow mask. Used with [`LightOccluder2D`](LightOccluder2D.md) to cast shadows. Only occluders with a matching [`CanvasItem.LightMask`](CanvasItem.md) will cast shadows. See also `RangeItemCullMask`, which affects which objects can *receive* the light.

## 方法

### GetBlendMode

```csharp
public Light2D.BlendModeEnum GetBlendMode()
```

引擎名：`get_blend_mode`

### GetColor

```csharp
public Color GetColor()
```

引擎名：`get_color`

### GetEnergy

```csharp
public float GetEnergy()
```

引擎名：`get_energy`

### GetHeight

```csharp
public float GetHeight()
```

引擎名：`get_height`

Returns the light's height, which is used in 2D normal mapping. See [`PointLight2D.Height`](PointLight2D.md) and [`DirectionalLight2D.Height`](DirectionalLight2D.md).

### GetItemCullMask

```csharp
public int GetItemCullMask()
```

引擎名：`get_item_cull_mask`

### GetItemShadowCullMask

```csharp
public int GetItemShadowCullMask()
```

引擎名：`get_item_shadow_cull_mask`

### GetLayerRangeMax

```csharp
public int GetLayerRangeMax()
```

引擎名：`get_layer_range_max`

### GetLayerRangeMin

```csharp
public int GetLayerRangeMin()
```

引擎名：`get_layer_range_min`

### GetShadowColor

```csharp
public Color GetShadowColor()
```

引擎名：`get_shadow_color`

### GetShadowFilter

```csharp
public Light2D.ShadowFilterEnum GetShadowFilter()
```

引擎名：`get_shadow_filter`

### GetShadowSmooth

```csharp
public float GetShadowSmooth()
```

引擎名：`get_shadow_smooth`

### GetZRangeMax

```csharp
public int GetZRangeMax()
```

引擎名：`get_z_range_max`

### GetZRangeMin

```csharp
public int GetZRangeMin()
```

引擎名：`get_z_range_min`

### IsEditorOnly

```csharp
public bool IsEditorOnly()
```

引擎名：`is_editor_only`

### IsEnabled

```csharp
public bool IsEnabled()
```

引擎名：`is_enabled`

### IsShadowEnabled

```csharp
public bool IsShadowEnabled()
```

引擎名：`is_shadow_enabled`

### SetBlendMode

```csharp
public void SetBlendMode(Light2D.BlendModeEnum mode)
```

引擎名：`set_blend_mode`

### SetColor

```csharp
public void SetColor(Color color)
```

引擎名：`set_color`

### SetEditorOnly

```csharp
public void SetEditorOnly(bool editorOnly)
```

引擎名：`set_editor_only`

### SetEnabled

```csharp
public void SetEnabled(bool enabled)
```

引擎名：`set_enabled`

### SetEnergy

```csharp
public void SetEnergy(float energy)
```

引擎名：`set_energy`

### SetHeight

```csharp
public void SetHeight(float height)
```

引擎名：`set_height`

Sets the light's height, which is used in 2D normal mapping. See [`PointLight2D.Height`](PointLight2D.md) and [`DirectionalLight2D.Height`](DirectionalLight2D.md).

### SetItemCullMask

```csharp
public void SetItemCullMask(int itemCullMask)
```

引擎名：`set_item_cull_mask`

### SetItemShadowCullMask

```csharp
public void SetItemShadowCullMask(int itemShadowCullMask)
```

引擎名：`set_item_shadow_cull_mask`

### SetLayerRangeMax

```csharp
public void SetLayerRangeMax(int layer)
```

引擎名：`set_layer_range_max`

### SetLayerRangeMin

```csharp
public void SetLayerRangeMin(int layer)
```

引擎名：`set_layer_range_min`

### SetShadowColor

```csharp
public void SetShadowColor(Color shadowColor)
```

引擎名：`set_shadow_color`

### SetShadowEnabled

```csharp
public void SetShadowEnabled(bool enabled)
```

引擎名：`set_shadow_enabled`

### SetShadowFilter

```csharp
public void SetShadowFilter(Light2D.ShadowFilterEnum filter)
```

引擎名：`set_shadow_filter`

### SetShadowSmooth

```csharp
public void SetShadowSmooth(float smooth)
```

引擎名：`set_shadow_smooth`

### SetZRangeMax

```csharp
public void SetZRangeMax(int z)
```

引擎名：`set_z_range_max`

### SetZRangeMin

```csharp
public void SetZRangeMin(int z)
```

引擎名：`set_z_range_min`

## BlendModeEnum

命名空间 `Godot`；enum

### 枚举值

- `Add = 0`：Adds the value of pixels corresponding to the Light2D to the values of pixels under it.
- `Sub = 1`：Subtracts the value of pixels corresponding to the Light2D to the values of pixels under it, resulting in inversed light effect.
- `Mix = 2`：Mix the value of pixels corresponding to the Light2D to the values of pixels under it by linear interpolation.

## ShadowFilterEnum

命名空间 `Godot`；enum

### 枚举值

- `None = 0`：No filter applies to the shadow map.
- `Pcf5 = 1`：Percentage closer filtering (5 samples) applies to the shadow map.
- `Pcf13 = 2`：Percentage closer filtering (13 samples) applies to the shadow map.

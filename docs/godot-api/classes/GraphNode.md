<!-- 由 scripts/godot-api/gen_api_docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# GraphNode

命名空间 `Godot`；class；继承 [`GraphElement`](GraphElement.md) → [`Container`](Container.md) → [`Control`](Control.md) → [`CanvasItem`](CanvasItem.md) → [`Node`](Node.md) → [`GodotObject`](GodotObject.md)

`GraphNode` allows to create nodes for a [`GraphEdit`](GraphEdit.md) graph with customizable content based on its child controls. `GraphNode` is derived from [`Container`](Container.md) and it is responsible for placing its children on screen. This works similar to [`VBoxContainer`](VBoxContainer.md). Children, in turn, provide `GraphNode` with so-called slots, each of which can have a connection port on either side.

Each `GraphNode` slot is defined by its index and can provide the node with up to two ports: one on the left, and one on the right. By convention the left port is also referred to as the **input port** and the right port is referred to as the **output port**. Each port can be enabled and configured individually, using different type and color. The type is an arbitrary value that you can define using your own considerations. The parent [`GraphEdit`](GraphEdit.md) will receive this information on each connect and disconnect request.

Slots can be configured in the Inspector dock once you add at least one child [`Control`](Control.md). The properties are grouped by each slot's index in the "Slot" section.

**Note:** While GraphNode is set up using slots and slot indices, connections are made between the ports which are enabled. Because of that [`GraphEdit`](GraphEdit.md) uses the port's index and not the slot's index. You can use `GetInputPortSlot()` and `GetOutputPortSlot()` to get the slot index from the port index.

## 构造函数

### GraphNode

```csharp
public GraphNode()
```

## 属性

### IgnoreInvalidConnectionType

```csharp
public bool IgnoreInvalidConnectionType { get; set; }
```

引擎名：`ignore_invalid_connection_type`

If `true`, you can connect ports with different types, even if the connection was not explicitly allowed in the parent [`GraphEdit`](GraphEdit.md).

### SlotsFocusMode

```csharp
public Control.FocusModeEnum SlotsFocusMode { get; set; }
```

引擎名：`slots_focus_mode`

Determines how connection slots can be focused.

- If set to [`Control.FocusModeEnum.Click`](Control.md), connections can only be made with the mouse.

- If set to [`Control.FocusModeEnum.All`](Control.md), slots can also be focused using the `ProjectSettings.input/ui_up` and `ProjectSettings.input/ui_down` and connected using `ProjectSettings.input/ui_left` and `ProjectSettings.input/ui_right` input actions.

- If set to [`Control.FocusModeEnum.Accessibility`](Control.md), slot input actions are only enabled when the screen reader is active.

### Title

```csharp
public string Title { get; set; }
```

引擎名：`title`

The text displayed in the GraphNode's title bar.

## 方法

### ClearAllSlots

```csharp
public void ClearAllSlots()
```

引擎名：`clear_all_slots`

Disables all slots of the GraphNode. This will remove all input/output ports from the GraphNode.

### ClearSlot

```csharp
public void ClearSlot(int slotIndex)
```

引擎名：`clear_slot`

Disables the slot with the given `slotIndex`. This will remove the corresponding input and output port from the GraphNode.

### GetInputPortColor

```csharp
public Color GetInputPortColor(int portIdx)
```

引擎名：`get_input_port_color`

Returns the [`Color`](Color.md) of the input port with the given `portIdx`.

### GetInputPortCount

```csharp
public int GetInputPortCount()
```

引擎名：`get_input_port_count`

Returns the number of slots with an enabled input port.

### GetInputPortPosition

```csharp
public Vector2 GetInputPortPosition(int portIdx)
```

引擎名：`get_input_port_position`

Returns the position of the input port with the given `portIdx`.

### GetInputPortSlot

```csharp
public int GetInputPortSlot(int portIdx)
```

引擎名：`get_input_port_slot`

Returns the corresponding slot index of the input port with the given `portIdx`.

### GetInputPortType

```csharp
public int GetInputPortType(int portIdx)
```

引擎名：`get_input_port_type`

Returns the type of the input port with the given `portIdx`.

### GetOutputPortColor

```csharp
public Color GetOutputPortColor(int portIdx)
```

引擎名：`get_output_port_color`

Returns the [`Color`](Color.md) of the output port with the given `portIdx`.

### GetOutputPortCount

```csharp
public int GetOutputPortCount()
```

引擎名：`get_output_port_count`

Returns the number of slots with an enabled output port.

### GetOutputPortPosition

```csharp
public Vector2 GetOutputPortPosition(int portIdx)
```

引擎名：`get_output_port_position`

Returns the position of the output port with the given `portIdx`.

### GetOutputPortSlot

```csharp
public int GetOutputPortSlot(int portIdx)
```

引擎名：`get_output_port_slot`

Returns the corresponding slot index of the output port with the given `portIdx`.

### GetOutputPortType

```csharp
public int GetOutputPortType(int portIdx)
```

引擎名：`get_output_port_type`

Returns the type of the output port with the given `portIdx`.

### GetSlotColorLeft

```csharp
public Color GetSlotColorLeft(int slotIndex)
```

引擎名：`get_slot_color_left`

Returns the left (input) [`Color`](Color.md) of the slot with the given `slotIndex`.

### GetSlotColorRight

```csharp
public Color GetSlotColorRight(int slotIndex)
```

引擎名：`get_slot_color_right`

Returns the right (output) [`Color`](Color.md) of the slot with the given `slotIndex`.

### GetSlotCustomIconLeft

```csharp
public Texture2D GetSlotCustomIconLeft(int slotIndex)
```

引擎名：`get_slot_custom_icon_left`

Returns the left (input) custom [`Texture2D`](Texture2D.md) of the slot with the given `slotIndex`.

### GetSlotCustomIconRight

```csharp
public Texture2D GetSlotCustomIconRight(int slotIndex)
```

引擎名：`get_slot_custom_icon_right`

Returns the right (output) custom [`Texture2D`](Texture2D.md) of the slot with the given `slotIndex`.

### GetSlotMetadataLeft

```csharp
public Variant GetSlotMetadataLeft(int slotIndex)
```

引擎名：`get_slot_metadata_left`

Returns the left (input) metadata of the slot with the given `slotIndex`.

### GetSlotMetadataRight

```csharp
public Variant GetSlotMetadataRight(int slotIndex)
```

引擎名：`get_slot_metadata_right`

Returns the right (output) metadata of the slot with the given `slotIndex`.

### GetSlotTypeLeft

```csharp
public int GetSlotTypeLeft(int slotIndex)
```

引擎名：`get_slot_type_left`

Returns the left (input) type of the slot with the given `slotIndex`.

### GetSlotTypeRight

```csharp
public int GetSlotTypeRight(int slotIndex)
```

引擎名：`get_slot_type_right`

Returns the right (output) type of the slot with the given `slotIndex`.

### GetSlotsFocusMode

```csharp
public Control.FocusModeEnum GetSlotsFocusMode()
```

引擎名：`get_slots_focus_mode`

### GetTitle

```csharp
public string GetTitle()
```

引擎名：`get_title`

### GetTitlebarHBox

```csharp
public HBoxContainer GetTitlebarHBox()
```

Returns the [`HBoxContainer`](HBoxContainer.md) used for the title bar, only containing a [`Label`](Label.md) for displaying the title by default. This can be used to add custom controls to the title bar such as option or close buttons.

### IsIgnoringValidConnectionType

```csharp
public bool IsIgnoringValidConnectionType()
```

引擎名：`is_ignoring_valid_connection_type`

### IsSlotDrawStylebox

```csharp
public bool IsSlotDrawStylebox(int slotIndex)
```

引擎名：`is_slot_draw_stylebox`

Returns `true` if the background [`StyleBox`](StyleBox.md) of the slot with the given `slotIndex` is drawn.

### IsSlotEnabledLeft

```csharp
public bool IsSlotEnabledLeft(int slotIndex)
```

引擎名：`is_slot_enabled_left`

Returns `true` if left (input) side of the slot with the given `slotIndex` is enabled.

### IsSlotEnabledRight

```csharp
public bool IsSlotEnabledRight(int slotIndex)
```

引擎名：`is_slot_enabled_right`

Returns `true` if right (output) side of the slot with the given `slotIndex` is enabled.

### SetIgnoreInvalidConnectionType

```csharp
public void SetIgnoreInvalidConnectionType(bool ignore)
```

引擎名：`set_ignore_invalid_connection_type`

### SetSlot

```csharp
public void SetSlot(int slotIndex, bool enableLeftPort, int typeLeft, Color colorLeft, bool enableRightPort, int typeRight, Color colorRight, Texture2D customIconLeft = null, Texture2D customIconRight = null, bool drawStylebox = true)
```

引擎名：`set_slot`

Sets properties of the slot with the given `slotIndex`.

If `enableLeftPort`/`enableRightPort` is `true`, a port will appear and the slot will be able to be connected from this side.

With `typeLeft`/`typeRight` an arbitrary type can be assigned to each port. Two ports can be connected if they share the same type, or if the connection between their types is allowed in the parent [`GraphEdit`](GraphEdit.md) (see [`GraphEdit.AddValidConnectionType`](GraphEdit.md)). Keep in mind that the [`GraphEdit`](GraphEdit.md) has the final say in accepting the connection. Type compatibility simply allows the [`GraphEdit.ConnectionRequest`](GraphEdit.md) signal to be emitted.

Ports can be further customized using `colorLeft`/`colorRight` and `customIconLeft`/`customIconRight`. The color parameter adds a tint to the icon. The custom icon can be used to override the default port dot.

Additionally, `drawStylebox` can be used to enable or disable drawing of the background stylebox for each slot. See `slot`.

Individual properties can also be set using one of the `set_slot_*` methods.

**Note:** This method only sets properties of the slot. To create the slot itself, add a [`Control`](Control.md)-derived child to the GraphNode.

### SetSlotColorLeft

```csharp
public void SetSlotColorLeft(int slotIndex, Color color)
```

引擎名：`set_slot_color_left`

Sets the [`Color`](Color.md) of the left (input) side of the slot with the given `slotIndex` to `color`.

### SetSlotColorRight

```csharp
public void SetSlotColorRight(int slotIndex, Color color)
```

引擎名：`set_slot_color_right`

Sets the [`Color`](Color.md) of the right (output) side of the slot with the given `slotIndex` to `color`.

### SetSlotCustomIconLeft

```csharp
public void SetSlotCustomIconLeft(int slotIndex, Texture2D customIcon)
```

引擎名：`set_slot_custom_icon_left`

Sets the custom [`Texture2D`](Texture2D.md) of the left (input) side of the slot with the given `slotIndex` to `customIcon`.

### SetSlotCustomIconRight

```csharp
public void SetSlotCustomIconRight(int slotIndex, Texture2D customIcon)
```

引擎名：`set_slot_custom_icon_right`

Sets the custom [`Texture2D`](Texture2D.md) of the right (output) side of the slot with the given `slotIndex` to `customIcon`.

### SetSlotDrawStylebox

```csharp
public void SetSlotDrawStylebox(int slotIndex, bool enable)
```

引擎名：`set_slot_draw_stylebox`

Toggles the background [`StyleBox`](StyleBox.md) of the slot with the given `slotIndex`.

### SetSlotEnabledLeft

```csharp
public void SetSlotEnabledLeft(int slotIndex, bool enable)
```

引擎名：`set_slot_enabled_left`

Toggles the left (input) side of the slot with the given `slotIndex`. If `enable` is `true`, a port will appear on the left side and the slot will be able to be connected from this side.

### SetSlotEnabledRight

```csharp
public void SetSlotEnabledRight(int slotIndex, bool enable)
```

引擎名：`set_slot_enabled_right`

Toggles the right (output) side of the slot with the given `slotIndex`. If `enable` is `true`, a port will appear on the right side and the slot will be able to be connected from this side.

### SetSlotMetadataLeft

```csharp
public void SetSlotMetadataLeft(int slotIndex, Variant value)
```

引擎名：`set_slot_metadata_left`

Sets the custom metadata for the left (input) side of the slot with the given `slotIndex` to `value`.

### SetSlotMetadataRight

```csharp
public void SetSlotMetadataRight(int slotIndex, Variant value)
```

引擎名：`set_slot_metadata_right`

Sets the custom metadata for the right (output) side of the slot with the given `slotIndex` to `value`.

### SetSlotTypeLeft

```csharp
public void SetSlotTypeLeft(int slotIndex, int type)
```

引擎名：`set_slot_type_left`

Sets the left (input) type of the slot with the given `slotIndex` to `type`. If the value is negative, all connections will be disallowed to be created via user inputs.

### SetSlotTypeRight

```csharp
public void SetSlotTypeRight(int slotIndex, int type)
```

引擎名：`set_slot_type_right`

Sets the right (output) type of the slot with the given `slotIndex` to `type`. If the value is negative, all connections will be disallowed to be created via user inputs.

### SetSlotsFocusMode

```csharp
public void SetSlotsFocusMode(Control.FocusModeEnum focusMode)
```

引擎名：`set_slots_focus_mode`

### SetTitle

```csharp
public void SetTitle(string title)
```

引擎名：`set_title`

### _DrawPort

```csharp
public virtual void _DrawPort(int slotIndex, Vector2I position, bool left, Color color)
```

引擎名：`_draw_port`

## 事件（信号）

### CloseRequest

```csharp
public event Action CloseRequest
```

Emitted when removing the GraphElement is requested.

### SlotSizesChanged

```csharp
public event Action SlotSizesChanged
```

引擎名：`slot_sizes_changed`

Emitted when any slot's size might have changed.

### SlotUpdated

```csharp
public event GraphNode.SlotUpdatedEventHandler SlotUpdated
```

引擎名：`slot_updated`

Emitted when any GraphNode's slot is updated.

## SlotUpdatedEventHandler

命名空间 `Godot`；delegate

Represents the method that handles the [`GraphNode.SlotUpdated`](GraphNode.md) event of a [`GraphNode`](GraphNode.md) class.

### 签名

#### Invoke

```csharp
delegate void SlotUpdatedEventHandler(long slotIndex)
```

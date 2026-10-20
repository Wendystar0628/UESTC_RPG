<!-- 由 scripts/godot-api/gen-api-docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# GraphEdit

命名空间 `Godot`；class；继承 [`Control`](Control.md) → [`CanvasItem`](CanvasItem.md) → [`Node`](Node.md) → [`GodotObject`](GodotObject.md)

`GraphEdit` provides tools for creation, manipulation, and display of various graphs. Its main purpose in the engine is to power the visual programming systems, such as visual shaders, but it is also available for use in user projects.

`GraphEdit` by itself is only an empty container, representing an infinite grid where [`GraphNode`](GraphNode.md)s can be placed. Each [`GraphNode`](GraphNode.md) represents a node in the graph, a single unit of data in the connected scheme. `GraphEdit`, in turn, helps to control various interactions with nodes and between nodes. When the user attempts to connect, disconnect, or delete a [`GraphNode`](GraphNode.md), a signal is emitted in the `GraphEdit`, but no action is taken by default. It is the responsibility of the programmer utilizing this control to implement the necessary logic to determine how each request should be handled.

**Performance:** It is greatly advised to enable low-processor usage mode (see [`OS.LowProcessorUsageMode`](OS.md)) when using GraphEdits.

**Note:** Keep in mind that [`Node.GetChildren`](Node.md) will also return the connection layer node named `_connection_layer` due to technical limitations. This behavior may change in future releases.

## 构造函数

### GraphEdit

```csharp
public GraphEdit()
```

## 属性

### ArrangeNodesButtonHidden

```csharp
public bool ArrangeNodesButtonHidden { get; set; }
```

If `true`, the button to automatically arrange graph nodes is visible.

### ConnectionLinesAntialiased

```csharp
public bool ConnectionLinesAntialiased { get; set; }
```

引擎名：`connection_lines_antialiased`

If `true`, the lines between nodes will use antialiasing.

### ConnectionLinesCurvature

```csharp
public float ConnectionLinesCurvature { get; set; }
```

引擎名：`connection_lines_curvature`

The curvature of the lines between the nodes. 0 results in straight lines.

### ConnectionLinesThickness

```csharp
public float ConnectionLinesThickness { get; set; }
```

引擎名：`connection_lines_thickness`

The thickness of the lines between the nodes.

### Connections

```csharp
public Array<Dictionary> Connections { get; set; }
```

引擎名：`connections`

The connections between [`GraphNode`](GraphNode.md)s.

A connection is represented as a [`Collections.Dictionary`](Collections.Dictionary.md) in the form of:

```csharp
{
from_node: StringName,
from_port: int,
to_node: StringName,
to_port: int,
keep_alive: bool
}

```

Connections with `keep_alive` set to `false` may be deleted automatically if invalid during a redraw.

### GridPattern

```csharp
public GraphEdit.GridPatternEnum GridPattern { get; set; }
```

引擎名：`grid_pattern`

The pattern used for drawing the grid.

### MinimapEnabled

```csharp
public bool MinimapEnabled { get; set; }
```

引擎名：`minimap_enabled`

If `true`, the minimap is visible.

### MinimapOpacity

```csharp
public float MinimapOpacity { get; set; }
```

引擎名：`minimap_opacity`

The opacity of the minimap rectangle.

### MinimapSize

```csharp
public Vector2 MinimapSize { get; set; }
```

引擎名：`minimap_size`

The size of the minimap rectangle. The map itself is based on the size of the grid area and is scaled to fit this rectangle.

### PanningScheme

```csharp
public GraphEdit.PanningSchemeEnum PanningScheme { get; set; }
```

引擎名：`panning_scheme`

Defines the control scheme for panning with mouse wheel.

### RightDisconnects

```csharp
public bool RightDisconnects { get; set; }
```

引擎名：`right_disconnects`

If `true`, enables disconnection of existing connections in the GraphEdit by dragging the right end.

### ScrollOffset

```csharp
public Vector2 ScrollOffset { get; set; }
```

引擎名：`scroll_offset`

The scroll offset.

### ShowArrangeButton

```csharp
public bool ShowArrangeButton { get; set; }
```

引擎名：`show_arrange_button`

If `true`, the button to automatically arrange graph nodes is visible.

### ShowGrid

```csharp
public bool ShowGrid { get; set; }
```

引擎名：`show_grid`

If `true`, the grid is visible.

### ShowGridButtons

```csharp
public bool ShowGridButtons { get; set; }
```

引擎名：`show_grid_buttons`

If `true`, buttons that allow to configure grid and snapping options are visible.

### ShowMenu

```csharp
public bool ShowMenu { get; set; }
```

引擎名：`show_menu`

If `true`, the menu toolbar is visible.

### ShowMinimapButton

```csharp
public bool ShowMinimapButton { get; set; }
```

引擎名：`show_minimap_button`

If `true`, the button to toggle the minimap is visible.

### ShowZoomButtons

```csharp
public bool ShowZoomButtons { get; set; }
```

引擎名：`show_zoom_buttons`

If `true`, buttons that allow to change and reset the zoom level are visible.

### ShowZoomLabel

```csharp
public bool ShowZoomLabel { get; set; }
```

引擎名：`show_zoom_label`

If `true`, the label with the current zoom level is visible. The zoom level is displayed in percents.

### SnapDistance

```csharp
public int SnapDistance { get; set; }
```

The snapping distance in pixels, also determines the grid line distance.

### SnappingDistance

```csharp
public int SnappingDistance { get; set; }
```

引擎名：`snapping_distance`

The snapping distance in pixels, also determines the grid line distance.

### SnappingEnabled

```csharp
public bool SnappingEnabled { get; set; }
```

引擎名：`snapping_enabled`

If `true`, enables snapping.

### TypeNames

```csharp
public Dictionary TypeNames { get; set; }
```

引擎名：`type_names`

[`Collections.Dictionary`](Collections.Dictionary.md) of human-readable port type names.

### UseSnap

```csharp
public bool UseSnap { get; set; }
```

If `true`, enables snapping.

### Zoom

```csharp
public float Zoom { get; set; }
```

引擎名：`zoom`

The current zoom value.

### ZoomMax

```csharp
public float ZoomMax { get; set; }
```

引擎名：`zoom_max`

The upper zoom limit.

### ZoomMin

```csharp
public float ZoomMin { get; set; }
```

引擎名：`zoom_min`

The lower zoom limit.

### ZoomStep

```csharp
public float ZoomStep { get; set; }
```

引擎名：`zoom_step`

The step of each zoom level.

## 方法

### AddValidConnectionType

```csharp
public void AddValidConnectionType(int fromType, int toType)
```

引擎名：`add_valid_connection_type`

Allows the connection between two different port types. The port type is defined individually for the left and the right port of each slot with the [`GraphNode.SetSlot`](GraphNode.md) method.

See also `IsValidConnectionType()` and `RemoveValidConnectionType()`.

### AddValidLeftDisconnectType

```csharp
public void AddValidLeftDisconnectType(int type)
```

引擎名：`add_valid_left_disconnect_type`

Allows to disconnect nodes when dragging from the left port of the [`GraphNode`](GraphNode.md)'s slot if it has the specified type. See also `RemoveValidLeftDisconnectType()`.

### AddValidRightDisconnectType

```csharp
public void AddValidRightDisconnectType(int type)
```

引擎名：`add_valid_right_disconnect_type`

Allows to disconnect nodes when dragging from the right port of the [`GraphNode`](GraphNode.md)'s slot if it has the specified type. See also `RemoveValidRightDisconnectType()`.

### ArrangeNodes

```csharp
public void ArrangeNodes()
```

引擎名：`arrange_nodes`

Rearranges selected nodes in a layout with minimum crossings between connections and uniform horizontal and vertical gap between nodes.

### AttachGraphElementToFrame

```csharp
public void AttachGraphElementToFrame(StringName element, StringName frame)
```

引擎名：`attach_graph_element_to_frame`

Attaches the `element` [`GraphElement`](GraphElement.md) to the `frame` [`GraphFrame`](GraphFrame.md).

### ClearConnections

```csharp
public void ClearConnections()
```

引擎名：`clear_connections`

Removes all connections between nodes.

### ConnectNode

```csharp
public Error ConnectNode(StringName fromNode, int fromPort, StringName toNode, int toPort)
```

引擎名：`connect_node`

Create a connection between the `fromPort` of the `fromNode` [`GraphNode`](GraphNode.md) and the `toPort` of the `toNode` [`GraphNode`](GraphNode.md). If the connection already exists, no connection is created.

Connections with `keepAlive` set to `false` may be deleted automatically if invalid during a redraw.

### ConnectNode

```csharp
public Error ConnectNode(StringName fromNode, int fromPort, StringName toNode, int toPort, bool keepAlive = false)
```

引擎名：`connect_node`

Create a connection between the `fromPort` of the `fromNode` [`GraphNode`](GraphNode.md) and the `toPort` of the `toNode` [`GraphNode`](GraphNode.md). If the connection already exists, no connection is created.

Connections with `keepAlive` set to `false` may be deleted automatically if invalid during a redraw.

### DetachGraphElementFromFrame

```csharp
public void DetachGraphElementFromFrame(StringName element)
```

引擎名：`detach_graph_element_from_frame`

Detaches the `element` [`GraphElement`](GraphElement.md) from the [`GraphFrame`](GraphFrame.md) it is currently attached to.

### DisconnectNode

```csharp
public void DisconnectNode(StringName fromNode, int fromPort, StringName toNode, int toPort)
```

引擎名：`disconnect_node`

Removes the connection between the `fromPort` of the `fromNode` [`GraphNode`](GraphNode.md) and the `toPort` of the `toNode` [`GraphNode`](GraphNode.md). If the connection does not exist, no connection is removed.

### ForceConnectionDragEnd

```csharp
public void ForceConnectionDragEnd()
```

引擎名：`force_connection_drag_end`

Ends the creation of the current connection. In other words, if you are dragging a connection you can use this method to abort the process and remove the line that followed your cursor.

This is best used together with `ConnectionDragStarted` and `ConnectionDragEnded` to add custom behavior like node addition through shortcuts.

**Note:** This method suppresses any other connection request signals apart from `ConnectionDragEnded`.

### GetAttachedNodesOfFrame

```csharp
public Array<StringName> GetAttachedNodesOfFrame(StringName frame)
```

引擎名：`get_attached_nodes_of_frame`

Returns an array of node names that are attached to the [`GraphFrame`](GraphFrame.md) with the given name.

### GetClosestConnectionAtPoint

```csharp
public Dictionary GetClosestConnectionAtPoint(Vector2 point, float maxDistance = 4f)
```

引擎名：`get_closest_connection_at_point`

Returns the closest connection to the given point in screen space. If no connection is found within `maxDistance` pixels, an empty [`Collections.Dictionary`](Collections.Dictionary.md) is returned.

A connection is represented as a [`Collections.Dictionary`](Collections.Dictionary.md) in the form of:

```csharp
{
from_node: StringName,
from_port: int,
to_node: StringName,
to_port: int,
keep_alive: bool
}

```

For example, getting a connection at a given mouse position can be achieved like this:

### GetConnectionCount

```csharp
public int GetConnectionCount(StringName fromNode, int fromPort)
```

引擎名：`get_connection_count`

Returns the number of connections from `fromPort` of `fromNode`.

### GetConnectionLine

```csharp
public Vector2[] GetConnectionLine(Vector2 fromNode, Vector2 toNode)
```

引擎名：`get_connection_line`

Returns the points which would make up a connection between `fromNode` and `toNode`.

### GetConnectionLinesCurvature

```csharp
public float GetConnectionLinesCurvature()
```

引擎名：`get_connection_lines_curvature`

### GetConnectionLinesThickness

```csharp
public float GetConnectionLinesThickness()
```

引擎名：`get_connection_lines_thickness`

### GetConnectionList

```csharp
public Array<Dictionary> GetConnectionList()
```

引擎名：`get_connection_list`

### GetConnectionListFromNode

```csharp
public Array<Dictionary> GetConnectionListFromNode(StringName node)
```

引擎名：`get_connection_list_from_node`

Returns an [`Collections.Array`](Collections.Array.md) containing a list of all connections for `node`.

A connection is represented as a [`Collections.Dictionary`](Collections.Dictionary.md) in the form of:

```csharp
{
from_node: StringName,
from_port: int,
to_node: StringName,
to_port: int,
keep_alive: bool
}

```

**Example:** Get all connections on a specific port:

```csharp
func get_connection_list_from_port(node, port):
var connections = get_connection_list_from_node(node)
var result = []
for connection in connections:
var dict = {}
if connection["from_node"] == node and connection["from_port"] == port:
dict["node"] = connection["to_node"]
dict["port"] = connection["to_port"]
dict["type"] = "left"
result.push_back(dict)
elif connection["to_node"] == node and connection["to_port"] == port:
dict["node"] = connection["from_node"]
dict["port"] = connection["from_port"]
dict["type"] = "right"
result.push_back(dict)
return result

```

### GetConnectionsIntersectingWithRect

```csharp
public Array<Dictionary> GetConnectionsIntersectingWithRect(Rect2 rect)
```

引擎名：`get_connections_intersecting_with_rect`

Returns an [`Collections.Array`](Collections.Array.md) containing the list of connections that intersect with the given [`Rect2`](Rect2.md).

A connection is represented as a [`Collections.Dictionary`](Collections.Dictionary.md) in the form of:

```csharp
{
from_node: StringName,
from_port: int,
to_node: StringName,
to_port: int,
keep_alive: bool
}

```

### GetElementFrame

```csharp
public GraphFrame GetElementFrame(StringName element)
```

引擎名：`get_element_frame`

Returns the [`GraphFrame`](GraphFrame.md) that contains the [`GraphElement`](GraphElement.md) with the given name.

### GetGridPattern

```csharp
public GraphEdit.GridPatternEnum GetGridPattern()
```

引擎名：`get_grid_pattern`

### GetMenuHBox

```csharp
public HBoxContainer GetMenuHBox()
```

Gets the [`HBoxContainer`](HBoxContainer.md) that contains the zooming and grid snap controls in the top left of the graph. You can use this method to reposition the toolbar or to add your own custom controls to it.

**Warning:** This is a required internal node, removing and freeing it may cause a crash. If you wish to hide it or any of its children, use their [`CanvasItem.Visible`](CanvasItem.md) property.

### GetMinimapOpacity

```csharp
public float GetMinimapOpacity()
```

引擎名：`get_minimap_opacity`

### GetMinimapSize

```csharp
public Vector2 GetMinimapSize()
```

引擎名：`get_minimap_size`

### GetPanningScheme

```csharp
public GraphEdit.PanningSchemeEnum GetPanningScheme()
```

引擎名：`get_panning_scheme`

### GetScrollOffset

```csharp
public Vector2 GetScrollOffset()
```

引擎名：`get_scroll_offset`

### GetSnappingDistance

```csharp
public int GetSnappingDistance()
```

引擎名：`get_snapping_distance`

### GetTypeNames

```csharp
public Dictionary GetTypeNames()
```

引擎名：`get_type_names`

### GetZoom

```csharp
public float GetZoom()
```

引擎名：`get_zoom`

### GetZoomHBox

```csharp
public HBoxContainer GetZoomHBox()
```

Gets the [`HBoxContainer`](HBoxContainer.md) that contains the zooming and grid snap controls in the top left of the graph. You can use this method to reposition the toolbar or to add your own custom controls to it.

**Warning:** This is a required internal node, removing and freeing it may cause a crash. If you wish to hide it or any of its children, use their [`CanvasItem.Visible`](CanvasItem.md) property.

### GetZoomMax

```csharp
public float GetZoomMax()
```

引擎名：`get_zoom_max`

### GetZoomMin

```csharp
public float GetZoomMin()
```

引擎名：`get_zoom_min`

### GetZoomStep

```csharp
public float GetZoomStep()
```

引擎名：`get_zoom_step`

### IsArrangeNodesButtonHidden

```csharp
public bool IsArrangeNodesButtonHidden()
```

### IsConnectionLinesAntialiased

```csharp
public bool IsConnectionLinesAntialiased()
```

引擎名：`is_connection_lines_antialiased`

### IsMinimapEnabled

```csharp
public bool IsMinimapEnabled()
```

引擎名：`is_minimap_enabled`

### IsNodeConnected

```csharp
public bool IsNodeConnected(StringName fromNode, int fromPort, StringName toNode, int toPort)
```

引擎名：`is_node_connected`

Returns `true` if the `fromPort` of the `fromNode` [`GraphNode`](GraphNode.md) is connected to the `toPort` of the `toNode` [`GraphNode`](GraphNode.md).

### IsRightDisconnectsEnabled

```csharp
public bool IsRightDisconnectsEnabled()
```

引擎名：`is_right_disconnects_enabled`

### IsShowingArrangeButton

```csharp
public bool IsShowingArrangeButton()
```

引擎名：`is_showing_arrange_button`

### IsShowingGrid

```csharp
public bool IsShowingGrid()
```

引擎名：`is_showing_grid`

### IsShowingGridButtons

```csharp
public bool IsShowingGridButtons()
```

引擎名：`is_showing_grid_buttons`

### IsShowingMenu

```csharp
public bool IsShowingMenu()
```

引擎名：`is_showing_menu`

### IsShowingMinimapButton

```csharp
public bool IsShowingMinimapButton()
```

引擎名：`is_showing_minimap_button`

### IsShowingZoomButtons

```csharp
public bool IsShowingZoomButtons()
```

引擎名：`is_showing_zoom_buttons`

### IsShowingZoomLabel

```csharp
public bool IsShowingZoomLabel()
```

引擎名：`is_showing_zoom_label`

### IsSnappingEnabled

```csharp
public bool IsSnappingEnabled()
```

引擎名：`is_snapping_enabled`

### IsValidConnectionType

```csharp
public bool IsValidConnectionType(int fromType, int toType)
```

引擎名：`is_valid_connection_type`

Returns whether it's possible to make a connection between two different port types. The port type is defined individually for the left and the right port of each slot with the [`GraphNode.SetSlot`](GraphNode.md) method.

See also `AddValidConnectionType()` and `RemoveValidConnectionType()`.

### RemoveValidConnectionType

```csharp
public void RemoveValidConnectionType(int fromType, int toType)
```

引擎名：`remove_valid_connection_type`

Disallows the connection between two different port types previously allowed by `AddValidConnectionType()`. The port type is defined individually for the left and the right port of each slot with the [`GraphNode.SetSlot`](GraphNode.md) method.

See also `IsValidConnectionType()`.

### RemoveValidLeftDisconnectType

```csharp
public void RemoveValidLeftDisconnectType(int type)
```

引擎名：`remove_valid_left_disconnect_type`

Disallows to disconnect nodes when dragging from the left port of the [`GraphNode`](GraphNode.md)'s slot if it has the specified type. Use this to disable a disconnection previously allowed with `AddValidLeftDisconnectType()`.

### RemoveValidRightDisconnectType

```csharp
public void RemoveValidRightDisconnectType(int type)
```

引擎名：`remove_valid_right_disconnect_type`

Disallows to disconnect nodes when dragging from the right port of the [`GraphNode`](GraphNode.md)'s slot if it has the specified type. Use this to disable a disconnection previously allowed with `AddValidRightDisconnectType()`.

### SetArrangeNodesButtonHidden

```csharp
public void SetArrangeNodesButtonHidden(bool enable)
```

### SetConnectionActivity

```csharp
public void SetConnectionActivity(StringName fromNode, int fromPort, StringName toNode, int toPort, float amount)
```

引擎名：`set_connection_activity`

Sets the coloration of the connection between `fromNode`'s `fromPort` and `toNode`'s `toPort` with the color provided in the `activity` theme property. The color is linearly interpolated between the connection color and the activity color using `amount` as weight.

### SetConnectionLinesAntialiased

```csharp
public void SetConnectionLinesAntialiased(bool pixels)
```

引擎名：`set_connection_lines_antialiased`

### SetConnectionLinesCurvature

```csharp
public void SetConnectionLinesCurvature(float curvature)
```

引擎名：`set_connection_lines_curvature`

### SetConnectionLinesThickness

```csharp
public void SetConnectionLinesThickness(float pixels)
```

引擎名：`set_connection_lines_thickness`

### SetConnections

```csharp
public void SetConnections(Array<Dictionary> connections)
```

引擎名：`set_connections`

### SetGridPattern

```csharp
public void SetGridPattern(GraphEdit.GridPatternEnum pattern)
```

引擎名：`set_grid_pattern`

### SetMinimapEnabled

```csharp
public void SetMinimapEnabled(bool enable)
```

引擎名：`set_minimap_enabled`

### SetMinimapOpacity

```csharp
public void SetMinimapOpacity(float opacity)
```

引擎名：`set_minimap_opacity`

### SetMinimapSize

```csharp
public void SetMinimapSize(Vector2 size)
```

引擎名：`set_minimap_size`

### SetPanningScheme

```csharp
public void SetPanningScheme(GraphEdit.PanningSchemeEnum scheme)
```

引擎名：`set_panning_scheme`

### SetRightDisconnects

```csharp
public void SetRightDisconnects(bool enable)
```

引擎名：`set_right_disconnects`

### SetScrollOffset

```csharp
public void SetScrollOffset(Vector2 offset)
```

引擎名：`set_scroll_offset`

### SetSelected

```csharp
public void SetSelected(Node node)
```

引擎名：`set_selected`

Sets the specified `node` as the one selected.

### SetShowArrangeButton

```csharp
public void SetShowArrangeButton(bool hidden)
```

引擎名：`set_show_arrange_button`

### SetShowGrid

```csharp
public void SetShowGrid(bool enable)
```

引擎名：`set_show_grid`

### SetShowGridButtons

```csharp
public void SetShowGridButtons(bool hidden)
```

引擎名：`set_show_grid_buttons`

### SetShowMenu

```csharp
public void SetShowMenu(bool hidden)
```

引擎名：`set_show_menu`

### SetShowMinimapButton

```csharp
public void SetShowMinimapButton(bool hidden)
```

引擎名：`set_show_minimap_button`

### SetShowZoomButtons

```csharp
public void SetShowZoomButtons(bool hidden)
```

引擎名：`set_show_zoom_buttons`

### SetShowZoomLabel

```csharp
public void SetShowZoomLabel(bool enable)
```

引擎名：`set_show_zoom_label`

### SetSnappingDistance

```csharp
public void SetSnappingDistance(int pixels)
```

引擎名：`set_snapping_distance`

### SetSnappingEnabled

```csharp
public void SetSnappingEnabled(bool enable)
```

引擎名：`set_snapping_enabled`

### SetTypeNames

```csharp
public void SetTypeNames(Dictionary typeNames)
```

引擎名：`set_type_names`

### SetZoom

```csharp
public void SetZoom(float zoom)
```

引擎名：`set_zoom`

### SetZoomMax

```csharp
public void SetZoomMax(float zoomMax)
```

引擎名：`set_zoom_max`

### SetZoomMin

```csharp
public void SetZoomMin(float zoomMin)
```

引擎名：`set_zoom_min`

### SetZoomStep

```csharp
public void SetZoomStep(float zoomStep)
```

引擎名：`set_zoom_step`

### _GetConnectionLine

```csharp
public virtual Vector2[] _GetConnectionLine(Vector2 fromPosition, Vector2 toPosition)
```

引擎名：`_get_connection_line`

Virtual method which can be overridden to customize how connections are drawn.

### _IsInInputHotzone

```csharp
public virtual bool _IsInInputHotzone(GodotObject inNode, int inPort, Vector2 mousePosition)
```

引擎名：`_is_in_input_hotzone`

Returns whether the `mousePosition` is in the input hot zone.

By default, a hot zone is a [`Rect2`](Rect2.md) positioned such that its center is at `inNode`.[`GraphNode.GetInputPortPosition`](GraphNode.md)(`inPort`) (For output's case, call [`GraphNode.GetOutputPortPosition`](GraphNode.md) instead). The hot zone's width is twice the Theme Property `port_grab_distance_horizontal`, and its height is twice the `port_grab_distance_vertical`.

Below is a sample code to help get started:

```csharp
func _is_in_input_hotzone(in_node, in_port, mouse_position):
var port_size = Vector2(get_theme_constant("port_grab_distance_horizontal"), get_theme_constant("port_grab_distance_vertical"))
var port_pos = in_node.get_position() + in_node.get_input_port_position(in_port) - port_size / 2
var rect = Rect2(port_pos, port_size)

return rect.has_point(mouse_position)

```

### _IsInOutputHotzone

```csharp
public virtual bool _IsInOutputHotzone(GodotObject inNode, int inPort, Vector2 mousePosition)
```

引擎名：`_is_in_output_hotzone`

Returns whether the `mousePosition` is in the output hot zone. For more information on hot zones, see `_IsInInputHotzone()`.

Below is a sample code to help get started:

```csharp
func _is_in_output_hotzone(in_node, in_port, mouse_position):
var port_size = Vector2(get_theme_constant("port_grab_distance_horizontal"), get_theme_constant("port_grab_distance_vertical"))
var port_pos = in_node.get_position() + in_node.get_output_port_position(in_port) - port_size / 2
var rect = Rect2(port_pos, port_size)

return rect.has_point(mouse_position)

```

### _IsNodeHoverValid

```csharp
public virtual bool _IsNodeHoverValid(StringName fromNode, int fromPort, StringName toNode, int toPort)
```

引擎名：`_is_node_hover_valid`

This virtual method can be used to insert additional error detection while the user is dragging a connection over a valid port.

Return `true` if the connection is indeed valid or return `false` if the connection is impossible. If the connection is impossible, no snapping to the port and thus no connection request to that port will happen.

In this example a connection to same node is suppressed:

```csharp
public override bool _IsNodeHoverValid(StringName fromNode, int fromPort, StringName toNode, int toPort)
{
return fromNode != toNode;
}

```

## 事件（信号）

### BeginNodeMove

```csharp
public event Action BeginNodeMove
```

引擎名：`begin_node_move`

Emitted at the beginning of a [`GraphElement`](GraphElement.md)'s movement.

### ConnectionDragEnded

```csharp
public event Action ConnectionDragEnded
```

引擎名：`connection_drag_ended`

Emitted at the end of a connection drag.

### ConnectionDragStarted

```csharp
public event GraphEdit.ConnectionDragStartedEventHandler ConnectionDragStarted
```

引擎名：`connection_drag_started`

Emitted at the beginning of a connection drag.

### ConnectionFromEmpty

```csharp
public event GraphEdit.ConnectionFromEmptyEventHandler ConnectionFromEmpty
```

引擎名：`connection_from_empty`

Emitted when user drags a connection from an input port into the empty space of the graph.

### ConnectionRequest

```csharp
public event GraphEdit.ConnectionRequestEventHandler ConnectionRequest
```

引擎名：`connection_request`

Emitted to the GraphEdit when the connection between the `fromPort` of the `fromNode` [`GraphNode`](GraphNode.md) and the `toPort` of the `toNode` [`GraphNode`](GraphNode.md) is attempted to be created.

### ConnectionToEmpty

```csharp
public event GraphEdit.ConnectionToEmptyEventHandler ConnectionToEmpty
```

引擎名：`connection_to_empty`

Emitted when user drags a connection from an output port into the empty space of the graph.

### CopyNodesRequest

```csharp
public event Action CopyNodesRequest
```

引擎名：`copy_nodes_request`

Emitted when this `GraphEdit` captures a `ui_copy` action (Ctrl + C by default). In general, this signal indicates that the selected [`GraphElement`](GraphElement.md)s should be copied.

### CutNodesRequest

```csharp
public event Action CutNodesRequest
```

引擎名：`cut_nodes_request`

Emitted when this `GraphEdit` captures a `ui_cut` action (Ctrl + X by default). In general, this signal indicates that the selected [`GraphElement`](GraphElement.md)s should be cut.

### DeleteNodesRequest

```csharp
public event GraphEdit.DeleteNodesRequestEventHandler DeleteNodesRequest
```

引擎名：`delete_nodes_request`

Emitted when this `GraphEdit` captures a `ui_graph_delete` action (Delete by default).

`nodes` is an array of node names that should be removed. These usually include all selected nodes.

### DisconnectionRequest

```csharp
public event GraphEdit.DisconnectionRequestEventHandler DisconnectionRequest
```

引擎名：`disconnection_request`

Emitted to the GraphEdit when the connection between `fromPort` of `fromNode` [`GraphNode`](GraphNode.md) and `toPort` of `toNode` [`GraphNode`](GraphNode.md) is attempted to be removed.

### DuplicateNodesRequest

```csharp
public event Action DuplicateNodesRequest
```

引擎名：`duplicate_nodes_request`

Emitted when this `GraphEdit` captures a `ui_graph_duplicate` action (Ctrl + D by default). In general, this signal indicates that the selected [`GraphElement`](GraphElement.md)s should be duplicated.

### EndNodeMove

```csharp
public event Action EndNodeMove
```

引擎名：`end_node_move`

Emitted at the end of a [`GraphElement`](GraphElement.md)'s movement.

### FrameRectChanged

```csharp
public event GraphEdit.FrameRectChangedEventHandler FrameRectChanged
```

引擎名：`frame_rect_changed`

Emitted when the [`GraphFrame`](GraphFrame.md) `frame` is resized to `newRect`.

### GraphElementsLinkedToFrameRequest

```csharp
public event GraphEdit.GraphElementsLinkedToFrameRequestEventHandler GraphElementsLinkedToFrameRequest
```

引擎名：`graph_elements_linked_to_frame_request`

Emitted when one or more [`GraphElement`](GraphElement.md)s are dropped onto the [`GraphFrame`](GraphFrame.md) named `frame`, when they were not previously attached to any other one.

`elements` is an array of [`GraphElement`](GraphElement.md)s to be attached.

### NodeDeselected

```csharp
public event GraphEdit.NodeDeselectedEventHandler NodeDeselected
```

引擎名：`node_deselected`

Emitted when the given [`GraphElement`](GraphElement.md) node is deselected.

### NodeSelected

```csharp
public event GraphEdit.NodeSelectedEventHandler NodeSelected
```

引擎名：`node_selected`

Emitted when the given [`GraphElement`](GraphElement.md) node is selected.

### PasteNodesRequest

```csharp
public event Action PasteNodesRequest
```

引擎名：`paste_nodes_request`

Emitted when this `GraphEdit` captures a `ui_paste` action (Ctrl + V by default). In general, this signal indicates that previously copied [`GraphElement`](GraphElement.md)s should be pasted.

### PopupRequest

```csharp
public event GraphEdit.PopupRequestEventHandler PopupRequest
```

引擎名：`popup_request`

Emitted when a popup is requested. Happens on right-clicking in the GraphEdit. `atPosition` is the position of the mouse pointer when the signal is sent.

### ScrollOffsetChanged

```csharp
public event GraphEdit.ScrollOffsetChangedEventHandler ScrollOffsetChanged
```

引擎名：`scroll_offset_changed`

Emitted when the scroll offset is changed by the user. It will not be emitted when changed in code.

## ConnectionDragStartedEventHandler

命名空间 `Godot`；delegate

Represents the method that handles the [`GraphEdit.ConnectionDragStarted`](GraphEdit.md) event of a [`GraphEdit`](GraphEdit.md) class.

### 签名

#### Invoke

```csharp
delegate void ConnectionDragStartedEventHandler(StringName fromNode, long fromPort, bool isOutput)
```

## ConnectionFromEmptyEventHandler

命名空间 `Godot`；delegate

Represents the method that handles the [`GraphEdit.ConnectionFromEmpty`](GraphEdit.md) event of a [`GraphEdit`](GraphEdit.md) class.

### 签名

#### Invoke

```csharp
delegate void ConnectionFromEmptyEventHandler(StringName toNode, long toPort, Vector2 releasePosition)
```

## ConnectionRequestEventHandler

命名空间 `Godot`；delegate

Represents the method that handles the [`GraphEdit.ConnectionRequest`](GraphEdit.md) event of a [`GraphEdit`](GraphEdit.md) class.

### 签名

#### Invoke

```csharp
delegate void ConnectionRequestEventHandler(StringName fromNode, long fromPort, StringName toNode, long toPort)
```

## ConnectionToEmptyEventHandler

命名空间 `Godot`；delegate

Represents the method that handles the [`GraphEdit.ConnectionToEmpty`](GraphEdit.md) event of a [`GraphEdit`](GraphEdit.md) class.

### 签名

#### Invoke

```csharp
delegate void ConnectionToEmptyEventHandler(StringName fromNode, long fromPort, Vector2 releasePosition)
```

## DeleteNodesRequestEventHandler

命名空间 `Godot`；delegate

Represents the method that handles the [`GraphEdit.DeleteNodesRequest`](GraphEdit.md) event of a [`GraphEdit`](GraphEdit.md) class.

### 签名

#### Invoke

```csharp
delegate void DeleteNodesRequestEventHandler(Array<StringName> nodes)
```

## DisconnectionRequestEventHandler

命名空间 `Godot`；delegate

Represents the method that handles the [`GraphEdit.DisconnectionRequest`](GraphEdit.md) event of a [`GraphEdit`](GraphEdit.md) class.

### 签名

#### Invoke

```csharp
delegate void DisconnectionRequestEventHandler(StringName fromNode, long fromPort, StringName toNode, long toPort)
```

## FrameRectChangedEventHandler

命名空间 `Godot`；delegate

Represents the method that handles the [`GraphEdit.FrameRectChanged`](GraphEdit.md) event of a [`GraphEdit`](GraphEdit.md) class.

### 签名

#### Invoke

```csharp
delegate void FrameRectChangedEventHandler(GraphFrame frame, Rect2 newRect)
```

## GraphElementsLinkedToFrameRequestEventHandler

命名空间 `Godot`；delegate

Represents the method that handles the [`GraphEdit.GraphElementsLinkedToFrameRequest`](GraphEdit.md) event of a [`GraphEdit`](GraphEdit.md) class.

### 签名

#### Invoke

```csharp
delegate void GraphElementsLinkedToFrameRequestEventHandler(Array elements, StringName frame)
```

## GridPatternEnum

命名空间 `Godot`；enum

### 枚举值

- `Lines = 0`：Draw the grid using solid lines.
- `Dots = 1`：Draw the grid using dots.

## NodeDeselectedEventHandler

命名空间 `Godot`；delegate

Represents the method that handles the [`GraphEdit.NodeDeselected`](GraphEdit.md) event of a [`GraphEdit`](GraphEdit.md) class.

### 签名

#### Invoke

```csharp
delegate void NodeDeselectedEventHandler(Node node)
```

## NodeSelectedEventHandler

命名空间 `Godot`；delegate

Represents the method that handles the [`GraphEdit.NodeSelected`](GraphEdit.md) event of a [`GraphEdit`](GraphEdit.md) class.

### 签名

#### Invoke

```csharp
delegate void NodeSelectedEventHandler(Node node)
```

## PanningSchemeEnum

命名空间 `Godot`；enum

### 枚举值

- `Zooms = 0`：Mouse Wheel will zoom, Ctrl + Mouse Wheel will move the view.
- `Pans = 1`：Mouse Wheel will move the view, Ctrl + Mouse Wheel will zoom.

## PopupRequestEventHandler

命名空间 `Godot`；delegate

Represents the method that handles the [`GraphEdit.PopupRequest`](GraphEdit.md) event of a [`GraphEdit`](GraphEdit.md) class.

### 签名

#### Invoke

```csharp
delegate void PopupRequestEventHandler(Vector2 atPosition)
```

## ScrollOffsetChangedEventHandler

命名空间 `Godot`；delegate

Represents the method that handles the [`GraphEdit.ScrollOffsetChanged`](GraphEdit.md) event of a [`GraphEdit`](GraphEdit.md) class.

### 签名

#### Invoke

```csharp
delegate void ScrollOffsetChangedEventHandler(Vector2 offset)
```

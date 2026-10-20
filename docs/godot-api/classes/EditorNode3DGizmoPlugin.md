<!-- 由 scripts/godot-api/gen-api-docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# EditorNode3DGizmoPlugin

命名空间 `Godot`；class；继承 [`Resource`](Resource.md) → [`RefCounted`](RefCounted.md) → [`GodotObject`](GodotObject.md)；仅编辑器（GodotSharpEditor）

`EditorNode3DGizmoPlugin` allows you to define a new type of Gizmo. There are two main ways to do so: extending `EditorNode3DGizmoPlugin` for the simpler gizmos, or creating a new [`EditorNode3DGizmo`](EditorNode3DGizmo.md) type. See the tutorial in the documentation for more info.

To use `EditorNode3DGizmoPlugin`, register it using the [`EditorPlugin.AddNode3DGizmoPlugin`](EditorPlugin.md) method first.

## 构造函数

### EditorNode3DGizmoPlugin

```csharp
public EditorNode3DGizmoPlugin()
```

## 方法

### AddMaterial

```csharp
public void AddMaterial(string name, StandardMaterial3D material)
```

引擎名：`add_material`

Adds a new material to the internal material list for the plugin. It can then be accessed with `GetMaterial()`. Should not be overridden.

### CreateHandleMaterial

```csharp
public void CreateHandleMaterial(string name, bool billboard = false, Texture2D texture = null)
```

引擎名：`create_handle_material`

Creates a handle material with its variants (selected and/or editable) and adds them to the internal material list. They can then be accessed with `GetMaterial()` and used in [`EditorNode3DGizmo.AddHandles`](EditorNode3DGizmo.md). Should not be overridden.

You can optionally provide a texture to use instead of the default icon.

### CreateIconMaterial

```csharp
public void CreateIconMaterial(string name, Texture2D texture, bool onTop = false, Color? color = null)
```

引擎名：`create_icon_material`

Creates an icon material with its variants (selected and/or editable) and adds them to the internal material list. They can then be accessed with `GetMaterial()` and used in [`EditorNode3DGizmo.AddUnscaledBillboard`](EditorNode3DGizmo.md). Should not be overridden.

参数：
- `color`：If the parameter is null, then the default value is `new Color(1.0f, 1.0f, 1.0f, 1.0f)`.

### CreateMaterial

```csharp
public void CreateMaterial(string name, Color color, bool billboard = false, bool onTop = false, bool useVertexColor = false)
```

引擎名：`create_material`

Creates an unshaded material with its variants (selected and/or editable) and adds them to the internal material list. They can then be accessed with `GetMaterial()` and used in [`EditorNode3DGizmo.AddMesh`](EditorNode3DGizmo.md) and [`EditorNode3DGizmo.AddLines`](EditorNode3DGizmo.md). Should not be overridden.

### GetMaterial

```csharp
public StandardMaterial3D GetMaterial(string name, EditorNode3DGizmo gizmo = null)
```

引擎名：`get_material`

Gets material from the internal list of materials. If an [`EditorNode3DGizmo`](EditorNode3DGizmo.md) is provided, it will try to get the corresponding variant (selected and/or editable).

### _BeginHandleAction

```csharp
public virtual void _BeginHandleAction(EditorNode3DGizmo gizmo, int handleId, bool secondary)
```

引擎名：`_begin_handle_action`

### _CanBeHidden

```csharp
public virtual bool _CanBeHidden()
```

引擎名：`_can_be_hidden`

Override this method to define whether the gizmos handled by this plugin can be hidden or not. Returns `true` if not overridden.

### _CanCommitHandleOnClick

```csharp
public virtual bool _CanCommitHandleOnClick()
```

引擎名：`_can_commit_handle_on_click`

Override this method to define whether the gizmos should commit when the final handle position is the same as the initial one. Returns `false` if not overridden.

### _CommitHandle

```csharp
public virtual void _CommitHandle(EditorNode3DGizmo gizmo, int handleId, bool secondary, Variant restore, bool cancel)
```

引擎名：`_commit_handle`

Override this method to commit a handle being edited (handles must have been previously added by [`EditorNode3DGizmo.AddHandles`](EditorNode3DGizmo.md) during `_Redraw()`). This usually means creating an [`UndoRedo`](UndoRedo.md) action for the change, using the current handle value as "do" and the `restore` argument as "undo".

If the `cancel` argument is `true`, the `restore` value should be directly set, without any [`UndoRedo`](UndoRedo.md) action.

The `secondary` argument is `true` when the committed handle is secondary (see [`EditorNode3DGizmo.AddHandles`](EditorNode3DGizmo.md) for more information).

Called for this plugin's active gizmos.

### _CommitSubgizmos

```csharp
public virtual void _CommitSubgizmos(EditorNode3DGizmo gizmo, int[] ids, Array<Transform3D> restores, bool cancel)
```

引擎名：`_commit_subgizmos`

Override this method to commit a group of subgizmos being edited (see `_SubgizmosIntersectRay()` and `_SubgizmosIntersectFrustum()`). This usually means creating an [`UndoRedo`](UndoRedo.md) action for the change, using the current transforms as "do" and the `restores` transforms as "undo".

If the `cancel` argument is `true`, the `restores` transforms should be directly set, without any [`UndoRedo`](UndoRedo.md) action. As with all subgizmo methods, transforms are given in local space respect to the gizmo's Node3D. Called for this plugin's active gizmos.

### _CreateGizmo

```csharp
public virtual EditorNode3DGizmo _CreateGizmo(Node3D forNode3D)
```

引擎名：`_create_gizmo`

Override this method to return a custom [`EditorNode3DGizmo`](EditorNode3DGizmo.md) for the 3D nodes of your choice, return `null` for the rest of nodes. See also `_HasGizmo()`.

### _GetGizmoName

```csharp
public virtual string _GetGizmoName()
```

引擎名：`_get_gizmo_name`

Override this method to provide the name that will appear in the gizmo visibility menu.

### _GetHandleName

```csharp
public virtual string _GetHandleName(EditorNode3DGizmo gizmo, int handleId, bool secondary)
```

引擎名：`_get_handle_name`

Override this method to provide gizmo's handle names. The `secondary` argument is `true` when the requested handle is secondary (see [`EditorNode3DGizmo.AddHandles`](EditorNode3DGizmo.md) for more information). Called for this plugin's active gizmos.

### _GetHandleValue

```csharp
public virtual Variant _GetHandleValue(EditorNode3DGizmo gizmo, int handleId, bool secondary)
```

引擎名：`_get_handle_value`

Override this method to return the current value of a handle. This value will be requested at the start of an edit and used as the `restore` argument in `_CommitHandle()`.

The `secondary` argument is `true` when the requested handle is secondary (see [`EditorNode3DGizmo.AddHandles`](EditorNode3DGizmo.md) for more information).

Called for this plugin's active gizmos.

### _GetPriority

```csharp
public virtual int _GetPriority()
```

引擎名：`_get_priority`

Override this method to set the gizmo's priority. Gizmos with higher priority will have precedence when processing inputs like handles or subgizmos selection.

All built-in editor gizmos return a priority of `-1`. If not overridden, this method will return `0`, which means custom gizmos will automatically get higher priority than built-in gizmos.

### _GetSubgizmoTransform

```csharp
public virtual Transform3D _GetSubgizmoTransform(EditorNode3DGizmo gizmo, int subgizmoId)
```

引擎名：`_get_subgizmo_transform`

Override this method to return the current transform of a subgizmo. As with all subgizmo methods, the transform should be in local space respect to the gizmo's Node3D. This transform will be requested at the start of an edit and used in the `restore` argument in `_CommitSubgizmos()`. Called for this plugin's active gizmos.

### _HasGizmo

```csharp
public virtual bool _HasGizmo(Node3D forNode3D)
```

引擎名：`_has_gizmo`

Override this method to define which Node3D nodes have a gizmo from this plugin. Whenever a [`Node3D`](Node3D.md) node is added to a scene this method is called, if it returns `true` the node gets a generic [`EditorNode3DGizmo`](EditorNode3DGizmo.md) assigned and is added to this plugin's list of active gizmos.

### _IsHandleHighlighted

```csharp
public virtual bool _IsHandleHighlighted(EditorNode3DGizmo gizmo, int handleId, bool secondary)
```

引擎名：`_is_handle_highlighted`

Override this method to return `true` whenever to given handle should be highlighted in the editor. The `secondary` argument is `true` when the requested handle is secondary (see [`EditorNode3DGizmo.AddHandles`](EditorNode3DGizmo.md) for more information). Called for this plugin's active gizmos.

### _IsSelectableWhenHidden

```csharp
public virtual bool _IsSelectableWhenHidden()
```

引擎名：`_is_selectable_when_hidden`

Override this method to define whether Node3D with this gizmo should be selectable even when the gizmo is hidden.

### _Redraw

```csharp
public virtual void _Redraw(EditorNode3DGizmo gizmo)
```

引擎名：`_redraw`

Override this method to add all the gizmo elements whenever a gizmo update is requested. It's common to call [`EditorNode3DGizmo.Clear`](EditorNode3DGizmo.md) at the beginning of this method and then add visual elements depending on the node's properties.

### _SetHandle

```csharp
public virtual void _SetHandle(EditorNode3DGizmo gizmo, int handleId, bool secondary, Camera3D camera, Vector2 screenPos)
```

引擎名：`_set_handle`

Override this method to update the node's properties when the user drags a gizmo handle (previously added with [`EditorNode3DGizmo.AddHandles`](EditorNode3DGizmo.md)). The provided `screenPos` is the mouse position in screen coordinates and the `camera` can be used to convert it to raycasts.

The `secondary` argument is `true` when the edited handle is secondary (see [`EditorNode3DGizmo.AddHandles`](EditorNode3DGizmo.md) for more information).

Called for this plugin's active gizmos.

### _SetSubgizmoTransform

```csharp
public virtual void _SetSubgizmoTransform(EditorNode3DGizmo gizmo, int subgizmoId, Transform3D transform)
```

引擎名：`_set_subgizmo_transform`

Override this method to update the node properties during subgizmo editing (see `_SubgizmosIntersectRay()` and `_SubgizmosIntersectFrustum()`). The `transform` is given in the Node3D's local coordinate system. Called for this plugin's active gizmos.

### _SubgizmosIntersectFrustum

```csharp
public virtual int[] _SubgizmosIntersectFrustum(EditorNode3DGizmo gizmo, Camera3D camera, Array<Plane> frustumPlanes)
```

引擎名：`_subgizmos_intersect_frustum`

Override this method to allow selecting subgizmos using mouse drag box selection. Given a `camera` and `frustumPlanes`, this method should return which subgizmos are contained within the frustums. The `frustumPlanes` argument consists of an array with all the [`Plane`](Plane.md)s that make up the selection frustum. The returned value should contain a list of unique subgizmo identifiers, these identifiers can have any non-negative value and will be used in other virtual methods like `_GetSubgizmoTransform()` or `_CommitSubgizmos()`. Called for this plugin's active gizmos.

### _SubgizmosIntersectRay

```csharp
public virtual int _SubgizmosIntersectRay(EditorNode3DGizmo gizmo, Camera3D camera, Vector2 screenPos)
```

引擎名：`_subgizmos_intersect_ray`

Override this method to allow selecting subgizmos using mouse clicks. Given a `camera` and a `screenPos` in screen coordinates, this method should return which subgizmo should be selected. The returned value should be a unique subgizmo identifier, which can have any non-negative value and will be used in other virtual methods like `_GetSubgizmoTransform()` or `_CommitSubgizmos()`. Called for this plugin's active gizmos.

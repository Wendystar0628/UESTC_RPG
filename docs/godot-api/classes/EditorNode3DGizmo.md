<!-- 由 scripts/godot-api/gen-api-docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# EditorNode3DGizmo

命名空间 `Godot`；class；继承 [`Node3DGizmo`](Node3DGizmo.md) → [`RefCounted`](RefCounted.md) → [`GodotObject`](GodotObject.md)；仅编辑器（GodotSharpEditor）

Gizmo that is used for providing custom visualization and editing (handles and subgizmos) for [`Node3D`](Node3D.md) objects. Can be overridden to create custom gizmos, but for simple gizmos creating an [`EditorNode3DGizmoPlugin`](EditorNode3DGizmoPlugin.md) is usually recommended.

## 构造函数

### EditorNode3DGizmo

```csharp
public EditorNode3DGizmo()
```

## 方法

### AddCollisionSegments

```csharp
public void AddCollisionSegments(ReadOnlySpan<Vector3> segments)
```

引擎名：`add_collision_segments`

Adds the specified `segments` to the gizmo's collision shape for picking. Call this method during `_Redraw()`.

### AddCollisionSegments

```csharp
public void AddCollisionSegments(Vector3[] segments)
```

引擎名：`add_collision_segments`

Adds the specified `segments` to the gizmo's collision shape for picking. Call this method during `_Redraw()`.

### AddCollisionTriangles

```csharp
public void AddCollisionTriangles(TriangleMesh triangles)
```

引擎名：`add_collision_triangles`

Adds collision triangles to the gizmo for picking. A [`TriangleMesh`](TriangleMesh.md) can be generated from a regular [`Mesh`](Mesh.md) too. Call this method during `_Redraw()`.

### AddHandles

```csharp
public void AddHandles(ReadOnlySpan<Vector3> handles, Material material, ReadOnlySpan<int> ids, bool billboard, bool secondary)
```

引擎名：`add_handles`

Adds a list of handles (points) which can be used to edit the properties of the gizmo's [`Node3D`](Node3D.md). The `ids` argument can be used to specify a custom identifier for each handle, if an empty array is passed, the ids will be assigned automatically from the `handles` argument order.

The `secondary` argument marks the added handles as secondary, meaning they will normally have lower selection priority than regular handles. When the user is holding the shift key secondary handles will switch to have higher priority than regular handles. This change in priority can be used to place multiple handles at the same point while still giving the user control on their selection.

There are virtual methods which will be called upon editing of these handles. Call this method during `_Redraw()`.

### AddHandles

```csharp
public void AddHandles(Vector3[] handles, Material material, int[] ids, bool billboard = false, bool secondary = false)
```

引擎名：`add_handles`

Adds a list of handles (points) which can be used to edit the properties of the gizmo's [`Node3D`](Node3D.md). The `ids` argument can be used to specify a custom identifier for each handle, if an empty array is passed, the ids will be assigned automatically from the `handles` argument order.

The `secondary` argument marks the added handles as secondary, meaning they will normally have lower selection priority than regular handles. When the user is holding the shift key secondary handles will switch to have higher priority than regular handles. This change in priority can be used to place multiple handles at the same point while still giving the user control on their selection.

There are virtual methods which will be called upon editing of these handles. Call this method during `_Redraw()`.

### AddLines

```csharp
public void AddLines(ReadOnlySpan<Vector3> lines, Material material, bool billboard, Color? modulate)
```

引擎名：`add_lines`

Adds lines to the gizmo (as sets of 2 points), with a given material. The lines are used for visualizing the gizmo. Call this method during `_Redraw()`.

参数：
- `modulate`：If the parameter is null, then the default value is `new Color(1.0f, 1.0f, 1.0f, 1.0f)`.

### AddLines

```csharp
public void AddLines(Vector3[] lines, Material material, bool billboard = false, Color? modulate = null)
```

引擎名：`add_lines`

Adds lines to the gizmo (as sets of 2 points), with a given material. The lines are used for visualizing the gizmo. Call this method during `_Redraw()`.

参数：
- `modulate`：If the parameter is null, then the default value is `new Color(1.0f, 1.0f, 1.0f, 1.0f)`.

### AddMesh

```csharp
public void AddMesh(Mesh mesh, Material material = null, Transform3D? transform = null, SkinReference skeleton = null)
```

引擎名：`add_mesh`

Adds a mesh to the gizmo with the specified `material`, local `transform` and `skeleton`. Call this method during `_Redraw()`.

参数：
- `transform`：If the parameter is null, then the default value is `Transform3D.Identity`.

### AddUnscaledBillboard

```csharp
public void AddUnscaledBillboard(Material material, float defaultScale = 1f, Color? modulate = null)
```

引擎名：`add_unscaled_billboard`

Adds an unscaled billboard for visualization and selection. Call this method during `_Redraw()`.

参数：
- `modulate`：If the parameter is null, then the default value is `new Color(1.0f, 1.0f, 1.0f, 1.0f)`.

### Clear

```csharp
public void Clear()
```

引擎名：`clear`

Removes everything in the gizmo including meshes, collisions and handles.

### GetNode3D

```csharp
public Node3D GetNode3D()
```

Returns the [`Node3D`](Node3D.md) node associated with this gizmo.

### GetPlugin

```csharp
public EditorNode3DGizmoPlugin GetPlugin()
```

引擎名：`get_plugin`

Returns the [`EditorNode3DGizmoPlugin`](EditorNode3DGizmoPlugin.md) that owns this gizmo. It's useful to retrieve materials using [`EditorNode3DGizmoPlugin.GetMaterial`](EditorNode3DGizmoPlugin.md).

### GetSubgizmoSelection

```csharp
public int[] GetSubgizmoSelection()
```

引擎名：`get_subgizmo_selection`

Returns a list of the currently selected subgizmos. Can be used to highlight selected elements during `_Redraw()`.

### IsSubgizmoSelected

```csharp
public bool IsSubgizmoSelected(int id)
```

引擎名：`is_subgizmo_selected`

Returns `true` if the given subgizmo is currently selected. Can be used to highlight selected elements during `_Redraw()`.

### SetHidden

```csharp
public void SetHidden(bool hidden)
```

引擎名：`set_hidden`

Sets the gizmo's hidden state. If `true`, the gizmo will be hidden. If `false`, it will be shown.

### SetNode3D

```csharp
public void SetNode3D(Node node)
```

Sets the reference [`Node3D`](Node3D.md) node for the gizmo. `node` must inherit from [`Node3D`](Node3D.md).

### _BeginHandleAction

```csharp
public virtual void _BeginHandleAction(int id, bool secondary)
```

引擎名：`_begin_handle_action`

### _CommitHandle

```csharp
public virtual void _CommitHandle(int id, bool secondary, Variant restore, bool cancel)
```

引擎名：`_commit_handle`

Override this method to commit a handle being edited (handles must have been previously added by `AddHandles()`). This usually means creating an [`UndoRedo`](UndoRedo.md) action for the change, using the current handle value as "do" and the `restore` argument as "undo".

If the `cancel` argument is `true`, the `restore` value should be directly set, without any [`UndoRedo`](UndoRedo.md) action.

The `secondary` argument is `true` when the committed handle is secondary (see `AddHandles()` for more information).

### _CommitSubgizmos

```csharp
public virtual void _CommitSubgizmos(int[] ids, Array<Transform3D> restores, bool cancel)
```

引擎名：`_commit_subgizmos`

Override this method to commit a group of subgizmos being edited (see `_SubgizmosIntersectRay()` and `_SubgizmosIntersectFrustum()`). This usually means creating an [`UndoRedo`](UndoRedo.md) action for the change, using the current transforms as "do" and the `restores` transforms as "undo".

If the `cancel` argument is `true`, the `restores` transforms should be directly set, without any [`UndoRedo`](UndoRedo.md) action.

### _GetHandleName

```csharp
public virtual string _GetHandleName(int id, bool secondary)
```

引擎名：`_get_handle_name`

Override this method to return the name of an edited handle (handles must have been previously added by `AddHandles()`). Handles can be named for reference to the user when editing.

The `secondary` argument is `true` when the requested handle is secondary (see `AddHandles()` for more information).

### _GetHandleValue

```csharp
public virtual Variant _GetHandleValue(int id, bool secondary)
```

引擎名：`_get_handle_value`

Override this method to return the current value of a handle. This value will be requested at the start of an edit and used as the `restore` argument in `_CommitHandle()`.

The `secondary` argument is `true` when the requested handle is secondary (see `AddHandles()` for more information).

### _GetSubgizmoTransform

```csharp
public virtual Transform3D _GetSubgizmoTransform(int id)
```

引擎名：`_get_subgizmo_transform`

Override this method to return the current transform of a subgizmo. This transform will be requested at the start of an edit and used as the `restore` argument in `_CommitSubgizmos()`.

### _IsHandleHighlighted

```csharp
public virtual bool _IsHandleHighlighted(int id, bool secondary)
```

引擎名：`_is_handle_highlighted`

Override this method to return `true` whenever the given handle should be highlighted in the editor.

The `secondary` argument is `true` when the requested handle is secondary (see `AddHandles()` for more information).

### _Redraw

```csharp
public virtual void _Redraw()
```

引擎名：`_redraw`

Override this method to add all the gizmo elements whenever a gizmo update is requested. It's common to call `Clear()` at the beginning of this method and then add visual elements depending on the node's properties.

### _SetHandle

```csharp
public virtual void _SetHandle(int id, bool secondary, Camera3D camera, Vector2 point)
```

引擎名：`_set_handle`

Override this method to update the node properties when the user drags a gizmo handle (previously added with `AddHandles()`). The provided `point` is the mouse position in screen coordinates and the `camera` can be used to convert it to raycasts.

The `secondary` argument is `true` when the edited handle is secondary (see `AddHandles()` for more information).

### _SetSubgizmoTransform

```csharp
public virtual void _SetSubgizmoTransform(int id, Transform3D transform)
```

引擎名：`_set_subgizmo_transform`

Override this method to update the node properties during subgizmo editing (see `_SubgizmosIntersectRay()` and `_SubgizmosIntersectFrustum()`). The `transform` is given in the [`Node3D`](Node3D.md)'s local coordinate system.

### _SubgizmosIntersectFrustum

```csharp
public virtual int[] _SubgizmosIntersectFrustum(Camera3D camera, Array<Plane> frustum)
```

引擎名：`_subgizmos_intersect_frustum`

Override this method to allow selecting subgizmos using mouse drag box selection. Given a `camera` and a `frustum`, this method should return which subgizmos are contained within the frustum. The `frustum` argument consists of an array with all the [`Plane`](Plane.md)s that make up the selection frustum. The returned value should contain a list of unique subgizmo identifiers, which can have any non-negative value and will be used in other virtual methods like `_GetSubgizmoTransform()` or `_CommitSubgizmos()`.

### _SubgizmosIntersectRay

```csharp
public virtual int _SubgizmosIntersectRay(Camera3D camera, Vector2 point)
```

引擎名：`_subgizmos_intersect_ray`

Override this method to allow selecting subgizmos using mouse clicks. Given a `camera` and a `point` in screen coordinates, this method should return which subgizmo should be selected. The returned value should be a unique subgizmo identifier, which can have any non-negative value and will be used in other virtual methods like `_GetSubgizmoTransform()` or `_CommitSubgizmos()`.

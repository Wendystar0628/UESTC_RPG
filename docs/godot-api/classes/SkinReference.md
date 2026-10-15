<!-- 由 scripts/godot-api/gen_api_docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# SkinReference

命名空间 `Godot`；class；继承 [`RefCounted`](RefCounted.md) → [`GodotObject`](GodotObject.md)

An internal object containing a mapping from a [`Skin`](Skin.md) used within the context of a particular [`MeshInstance3D`](MeshInstance3D.md) to refer to the skeleton's [`Rid`](Rid.md) in the RenderingServer.

See also [`MeshInstance3D.GetSkinReference`](MeshInstance3D.md) and [`RenderingServer.InstanceAttachSkeleton`](RenderingServer.md).

Note that despite the similar naming, the skeleton RID used in the [`RenderingServer`](RenderingServer.md) does not have a direct one-to-one correspondence to a [`Skeleton3D`](Skeleton3D.md) node.

In particular, a [`Skeleton3D`](Skeleton3D.md) node with no [`MeshInstance3D`](MeshInstance3D.md) children may be unknown to the [`RenderingServer`](RenderingServer.md).

On the other hand, a [`Skeleton3D`](Skeleton3D.md) with multiple [`MeshInstance3D`](MeshInstance3D.md) nodes which each have different [`MeshInstance3D.Skin`](MeshInstance3D.md) objects may have multiple SkinReference instances (and hence, multiple skeleton [`Rid`](Rid.md)s).

## 方法

### GetSkeleton

```csharp
public Rid GetSkeleton()
```

引擎名：`get_skeleton`

Returns the [`Rid`](Rid.md) owned by this SkinReference, as returned by [`RenderingServer.SkeletonCreate`](RenderingServer.md).

### GetSkin

```csharp
public Skin GetSkin()
```

引擎名：`get_skin`

Returns the [`Skin`](Skin.md) connected to this SkinReference. In the case of [`MeshInstance3D`](MeshInstance3D.md) with no [`MeshInstance3D.Skin`](MeshInstance3D.md) assigned, this will reference an internal default [`Skin`](Skin.md) owned by that [`MeshInstance3D`](MeshInstance3D.md).

Note that a single [`Skin`](Skin.md) may have more than one `SkinReference` in the case that it is shared by meshes across multiple [`Skeleton3D`](Skeleton3D.md) nodes.

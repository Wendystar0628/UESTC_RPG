# Godot C# API 索引（Godot Engine v4.7.2.stable.mono.official）

由 `scripts/godot-api/gen-api-docs.py` 生成，不要手改。用法和命名规则见 [README](README.md)。共 1151 个顶层类型，33065 个成员签名。

## 本项目常用速查

- **场景与节点**：[`Node`](classes/Node.md)、[`Node3D`](classes/Node3D.md)、[`SceneTree`](classes/SceneTree.md)、[`PackedScene`](classes/PackedScene.md)、[`ResourceLoader`](classes/ResourceLoader.md)、[`ResourceSaver`](classes/ResourceSaver.md)、[`WorkerThreadPool`](classes/WorkerThreadPool.md)、[`Engine`](classes/Engine.md)、[`OS`](classes/OS.md)、[`Time`](classes/Time.md)
- **网格与几何生成**：[`ArrayMesh`](classes/ArrayMesh.md)、[`SurfaceTool`](classes/SurfaceTool.md)、[`MeshDataTool`](classes/MeshDataTool.md)、[`ImporterMesh`](classes/ImporterMesh.md)、[`Mesh`](classes/Mesh.md)、[`MeshInstance3D`](classes/MeshInstance3D.md)、[`MultiMesh`](classes/MultiMesh.md)、[`MultiMeshInstance3D`](classes/MultiMeshInstance3D.md)、[`GeometryInstance3D`](classes/GeometryInstance3D.md)、[`VisualInstance3D`](classes/VisualInstance3D.md)、[`OccluderInstance3D`](classes/OccluderInstance3D.md)、[`ArrayOccluder3D`](classes/ArrayOccluder3D.md)
- **材质、着色器与纹理**：[`Shader`](classes/Shader.md)、[`ShaderMaterial`](classes/ShaderMaterial.md)、[`ShaderInclude`](classes/ShaderInclude.md)、[`Image`](classes/Image.md)、[`ImageTexture`](classes/ImageTexture.md)、[`Texture2DArray`](classes/Texture2DArray.md)、[`ImageTexture3D`](classes/ImageTexture3D.md)
- **渲染服务器与后期**：[`RenderingServer`](classes/RenderingServer.md)、[`RenderingDevice`](classes/RenderingDevice.md)、[`Compositor`](classes/Compositor.md)、[`CompositorEffect`](classes/CompositorEffect.md)、[`RenderSceneBuffersRD`](classes/RenderSceneBuffersRD.md)、[`RenderData`](classes/RenderData.md)、[`RDShaderFile`](classes/RDShaderFile.md)、[`Viewport`](classes/Viewport.md)、[`SubViewport`](classes/SubViewport.md)、[`Camera3D`](classes/Camera3D.md)、[`CameraAttributesPractical`](classes/CameraAttributesPractical.md)、[`Performance`](classes/Performance.md)
- **光照、天空与环境**：[`DirectionalLight3D`](classes/DirectionalLight3D.md)、[`OmniLight3D`](classes/OmniLight3D.md)、[`Light3D`](classes/Light3D.md)、[`WorldEnvironment`](classes/WorldEnvironment.md)、[`Environment`](classes/Environment.md)、[`Sky`](classes/Sky.md)、[`ShaderMaterial`](classes/ShaderMaterial.md)
- **物理与行走**：[`CharacterBody3D`](classes/CharacterBody3D.md)、[`StaticBody3D`](classes/StaticBody3D.md)、[`CollisionObject3D`](classes/CollisionObject3D.md)、[`PhysicsServer3D`](classes/PhysicsServer3D.md)、[`PhysicsDirectSpaceState3D`](classes/PhysicsDirectSpaceState3D.md)、[`HeightMapShape3D`](classes/HeightMapShape3D.md)、[`BoxShape3D`](classes/BoxShape3D.md)、[`CylinderShape3D`](classes/CylinderShape3D.md)、[`ConvexPolygonShape3D`](classes/ConvexPolygonShape3D.md)、[`ConcavePolygonShape3D`](classes/ConcavePolygonShape3D.md)、[`KinematicCollision3D`](classes/KinematicCollision3D.md)
- **输入与相机**：[`Input`](classes/Input.md)、[`InputEvent`](classes/InputEvent.md)、[`InputEventMouseMotion`](classes/InputEventMouseMotion.md)、[`InputEventKey`](classes/InputEventKey.md)、[`InputMap`](classes/InputMap.md)、[`DisplayServer`](classes/DisplayServer.md)
- **界面**：[`Control`](classes/Control.md)、[`CanvasLayer`](classes/CanvasLayer.md)、[`Theme`](classes/Theme.md)、[`Label`](classes/Label.md)、[`Button`](classes/Button.md)、[`Tween`](classes/Tween.md)、[`TranslationServer`](classes/TranslationServer.md)、[`FontFile`](classes/FontFile.md)、[`FontVariation`](classes/FontVariation.md)
- **数据与文件**：[`FileAccess`](classes/FileAccess.md)、[`DirAccess`](classes/DirAccess.md)、[`Json`](classes/Json.md)、[`ConfigFile`](classes/ConfigFile.md)、[`ProjectSettings`](classes/ProjectSettings.md)
- **数学与内置值类型**：[`Vector2`](classes/Vector2.md)、[`Vector3`](classes/Vector3.md)、[`Vector3I`](classes/Vector3I.md)、[`Transform3D`](classes/Transform3D.md)、[`Basis`](classes/Basis.md)、[`Quaternion`](classes/Quaternion.md)、[`Aabb`](classes/Aabb.md)、[`Plane`](classes/Plane.md)、[`Color`](classes/Color.md)、[`Rid`](classes/Rid.md)、[`StringName`](classes/StringName.md)、[`NodePath`](classes/NodePath.md)、[`Variant`](classes/Variant.md)、[`Mathf`](classes/Mathf.md)、[`GD`](classes/GD.md)

## 3D 节点（121）

| 类型 | 种类 | 简介 |
| --- | --- | --- |
| [`AimModifier3D`](classes/AimModifier3D.md) | class | This is a simple version of [`LookAtModifier3D`](classes/LookAtModifier3D.md) that only allows bone to the reference without advanced options such as angle limitation or time-based interpolation. |
| [`AnimatableBody3D`](classes/AnimatableBody3D.md) | class | An animatable 3D physics body. |
| [`AnimatedSprite3D`](classes/AnimatedSprite3D.md) | class | `AnimatedSprite3D` is similar to the [`Sprite3D`](classes/Sprite3D.md) node, except it carries multiple textures as animation `SpriteFrames`. |
| [`Area3D`](classes/Area3D.md) | class | `Area3D` is a region of 3D space defined by one or multiple [`CollisionShape3D`](classes/CollisionShape3D.md) or [`CollisionPolygon3D`](classes/CollisionPolygon3D.md) child nodes. |
| [`AreaLight3D`](classes/AreaLight3D.md) | class | An area light is a type of [`Light3D`](classes/Light3D.md) node that emits light over a two-dimensional area, in the shape of a rectangle. |
| [`AudioListener3D`](classes/AudioListener3D.md) | class | Once added to the scene tree and enabled using `MakeCurrent()`, this node will override the location sounds are heard from. |
| [`AudioStreamPlayer3D`](classes/AudioStreamPlayer3D.md) | class | Plays audio with positional sound effects, based on the relative position of the audio listener. |
| [`BoneAttachment3D`](classes/BoneAttachment3D.md) | class | This node selects a bone in a [`Skeleton3D`](classes/Skeleton3D.md) and attaches to it. |
| [`BoneConstraint3D`](classes/BoneConstraint3D.md) | class | Base class of [`SkeletonModifier3D`](classes/SkeletonModifier3D.md) that modifies the bone set in `SetApplyBone()` based on the transform of the bone retrieved by `GetReferenceBone()`. |
| [`BoneTwistDisperser3D`](classes/BoneTwistDisperser3D.md) | class | This `BoneTwistDisperser3D` allows for smooth twist interpolation between multiple bones by dispersing the end bone's twist to the parents. |
| [`Camera3D`](classes/Camera3D.md) | class | `Camera3D` is a special node that displays what is visible from its current location. |
| [`Ccdik3D`](classes/Ccdik3D.md) | class | `Ccdik3D` is rotation based IK, enabling fast and effective tracking even with large joint rotations. |
| [`ChainIK3D`](classes/ChainIK3D.md) | class | Base class of [`SkeletonModifier3D`](classes/SkeletonModifier3D.md) that automatically generates a joint list from the bones between the root bone and the end bone. |
| [`CharacterBody3D`](classes/CharacterBody3D.md) | class | `CharacterBody3D` is a specialized class for physics bodies that are meant to be user-controlled. |
| [`CollisionObject3D`](classes/CollisionObject3D.md) | class | Abstract base class for 3D physics objects. |
| [`CollisionPolygon3D`](classes/CollisionPolygon3D.md) | class | A node that provides a thickened polygon shape (a prism) to a [`CollisionObject3D`](classes/CollisionObject3D.md) parent and allows it to be edited. |
| [`CollisionShape3D`](classes/CollisionShape3D.md) | class | A node that provides a [`Shape3D`](classes/Shape3D.md) to a [`CollisionObject3D`](classes/CollisionObject3D.md) parent and allows it to be edited. |
| [`ConeTwistJoint3D`](classes/ConeTwistJoint3D.md) | class | A physics joint that connects two 3D physics bodies in a way that simulates a ball-and-socket joint. |
| [`ConvertTransformModifier3D`](classes/ConvertTransformModifier3D.md) | class | Apply the copied transform of the bone set by [`BoneConstraint3D.SetReferenceBone`](classes/BoneConstraint3D.md) to the bone set by [`BoneConstraint3D.SetApplyBone`](classes/BoneConstraint3D.md) about the specific axis with remapping it… |
| [`CopyTransformModifier3D`](classes/CopyTransformModifier3D.md) | class | Apply the copied transform of the bone set by [`BoneConstraint3D.SetReferenceBone`](classes/BoneConstraint3D.md) to the bone set by [`BoneConstraint3D.SetApplyBone`](classes/BoneConstraint3D.md) with processing it with some masks and op… |
| [`CpuParticles3D`](classes/CpuParticles3D.md) | class | CPU-based 3D particle node used to create a variety of particle systems and effects. |
| [`CsgBox3D`](classes/CsgBox3D.md) | class | This node allows you to create a box for use with the CSG system. |
| [`CsgCombiner3D`](classes/CsgCombiner3D.md) | class | For complex arrangements of shapes, it is sometimes needed to add structure to your CSG nodes. |
| [`CsgCylinder3D`](classes/CsgCylinder3D.md) | class | This node allows you to create a cylinder (or cone) for use with the CSG system. |
| [`CsgMesh3D`](classes/CsgMesh3D.md) | class | This CSG node allows you to use any mesh resource as a CSG shape, provided it is *manifold*. |
| [`CsgPolygon3D`](classes/CsgPolygon3D.md) | class | An array of 2D points is extruded to quickly and easily create a variety of 3D meshes. |
| [`CsgPrimitive3D`](classes/CsgPrimitive3D.md) | class | Parent class for various CSG primitives. |
| [`CsgShape3D`](classes/CsgShape3D.md) | class | This is the CSG base class that provides CSG operation support to the various CSG nodes in Godot. |
| [`CsgSphere3D`](classes/CsgSphere3D.md) | class | This node allows you to create a sphere for use with the CSG system. |
| [`CsgTorus3D`](classes/CsgTorus3D.md) | class | This node allows you to create a torus for use with the CSG system. |
| [`Decal`](classes/Decal.md) | class | `Decal`s are used to project a texture onto a [`Mesh`](classes/Mesh.md) in the scene. |
| [`DirectionalLight3D`](classes/DirectionalLight3D.md) | class | A directional light is a type of [`Light3D`](classes/Light3D.md) node that models an infinite number of parallel rays covering the entire scene. |
| [`Fabrik3D`](classes/Fabrik3D.md) | class | `Fabrik3D` is position based IK, allowing precise and accurate tracking of targets. |
| [`FogVolume`](classes/FogVolume.md) | class | `FogVolume`s are used to add localized fog into the global volumetric fog effect. |
| [`Generic6DofJoint3D`](classes/Generic6DofJoint3D.md) | class | The `Generic6DofJoint3D` (6 Degrees Of Freedom) joint allows for implementing custom types of joints by locking the rotation and translation of certain axes. |
| [`GeometryInstance3D`](classes/GeometryInstance3D.md) | class | Base node for geometry-based visual instances. |
| [`GpuParticles3D`](classes/GpuParticles3D.md) | class | 3D particle node used to create a variety of particle systems and effects. |
| [`GpuParticlesAttractor3D`](classes/GpuParticlesAttractor3D.md) | class | Particle attractors can be used to attract particles towards the attractor's origin, or to push them away from the attractor's origin. |
| [`GpuParticlesAttractorBox3D`](classes/GpuParticlesAttractorBox3D.md) | class | A box-shaped attractor that influences particles from [`GpuParticles3D`](classes/GpuParticles3D.md) nodes. |
| [`GpuParticlesAttractorSphere3D`](classes/GpuParticlesAttractorSphere3D.md) | class | A spheroid-shaped attractor that influences particles from [`GpuParticles3D`](classes/GpuParticles3D.md) nodes. |
| [`GpuParticlesAttractorVectorField3D`](classes/GpuParticlesAttractorVectorField3D.md) | class | A box-shaped attractor with varying directions and strengths defined in it that influences particles from [`GpuParticles3D`](classes/GpuParticles3D.md) nodes. |
| [`GpuParticlesCollision3D`](classes/GpuParticlesCollision3D.md) | class | Particle collision shapes can be used to make particles stop or bounce against them. |
| [`GpuParticlesCollisionBox3D`](classes/GpuParticlesCollisionBox3D.md) | class | A box-shaped 3D particle collision shape affecting [`GpuParticles3D`](classes/GpuParticles3D.md) nodes. |
| [`GpuParticlesCollisionHeightField3D`](classes/GpuParticlesCollisionHeightField3D.md) | class | A real-time heightmap-shaped 3D particle collision shape affecting [`GpuParticles3D`](classes/GpuParticles3D.md) nodes. |
| [`GpuParticlesCollisionSdf3D`](classes/GpuParticlesCollisionSdf3D.md) | class | A baked signed distance field 3D particle collision shape affecting [`GpuParticles3D`](classes/GpuParticles3D.md) nodes. |
| [`GpuParticlesCollisionSphere3D`](classes/GpuParticlesCollisionSphere3D.md) | class | A sphere-shaped 3D particle collision shape affecting [`GpuParticles3D`](classes/GpuParticles3D.md) nodes. |
| [`GridMap`](classes/GridMap.md) | class | GridMap lets you place meshes on a grid interactively. |
| [`HingeJoint3D`](classes/HingeJoint3D.md) | class | A physics joint that restricts the rotation of a 3D physics body around an axis relative to another physics body. |
| [`IKModifier3D`](classes/IKModifier3D.md) | class | Base class of [`SkeletonModifier3D`](classes/SkeletonModifier3D.md)s that has some joint lists and applies inverse kinematics. |
| [`ImporterMeshInstance3D`](classes/ImporterMeshInstance3D.md) | class |  |
| [`IterateIK3D`](classes/IterateIK3D.md) | class | Base class of [`SkeletonModifier3D`](classes/SkeletonModifier3D.md) to approach the goal by repeating small rotations. |
| [`JacobianIK3D`](classes/JacobianIK3D.md) | class | `JacobianIK3D` calculates rotations for all joints simultaneously, producing natural and smooth movement. |
| [`Joint3D`](classes/Joint3D.md) | class | Abstract base class for all joints in 3D physics. |
| [`Label3D`](classes/Label3D.md) | class | A node for displaying plain text in 3D space. |
| [`Light3D`](classes/Light3D.md) | class | Light3D is the *abstract* base class for light nodes. |
| [`LightmapGI`](classes/LightmapGI.md) | class | The `LightmapGI` node is used to compute and store baked lightmaps. |
| [`LightmapProbe`](classes/LightmapProbe.md) | class | `LightmapProbe` represents the position of a single manually placed probe for dynamic object lighting with [`LightmapGI`](classes/LightmapGI.md). |
| [`LimitAngularVelocityModifier3D`](classes/LimitAngularVelocityModifier3D.md) | class | This modifier limits bone rotation angular velocity by comparing poses between previous and current frame. |
| [`LookAtModifier3D`](classes/LookAtModifier3D.md) | class | This [`SkeletonModifier3D`](classes/SkeletonModifier3D.md) rotates a bone to look at a target. |
| [`Marker3D`](classes/Marker3D.md) | class | Generic 3D position hint for editing. |
| [`MeshInstance3D`](classes/MeshInstance3D.md) | class | MeshInstance3D is a node that takes a [`Mesh`](classes/Mesh.md) resource and adds it to the current scenario by creating an instance of it. |
| [`ModifierBoneTarget3D`](classes/ModifierBoneTarget3D.md) | class | This node selects a bone in a [`Skeleton3D`](classes/Skeleton3D.md) and attaches to it. |
| [`MultiMeshInstance3D`](classes/MultiMeshInstance3D.md) | class | `MultiMeshInstance3D` is a specialized node to instance [`GeometryInstance3D`](classes/GeometryInstance3D.md)s based on a [`MultiMesh`](classes/MultiMesh.md) resource. |
| [`NavigationLink3D`](classes/NavigationLink3D.md) | class | A link between two positions on [`NavigationRegion3D`](classes/NavigationRegion3D.md)s that agents can be routed through. |
| [`NavigationObstacle3D`](classes/NavigationObstacle3D.md) | class | An obstacle needs a navigation map and outline `Vertices` defined to work correctly. |
| [`NavigationRegion3D`](classes/NavigationRegion3D.md) | class | A traversable 3D region based on a [`NavigationMesh`](classes/NavigationMesh.md) that [`NavigationAgent3D`](classes/NavigationAgent3D.md)s can use for pathfinding. |
| [`Node3D`](classes/Node3D.md) | class | The `Node3D` node is the base representation of a node in 3D space. |
| [`OccluderInstance3D`](classes/OccluderInstance3D.md) | class | Occlusion culling can improve rendering performance in closed/semi-open areas by hiding geometry that is occluded by other objects. |
| [`OmniLight3D`](classes/OmniLight3D.md) | class | An Omnidirectional light is a type of [`Light3D`](classes/Light3D.md) that emits light in all directions. |
| [`OpenXRCompositionLayer`](classes/OpenXRCompositionLayer.md) | class | Composition layers allow 2D viewports to be displayed inside of the headset by the XR compositor through special projections that retain their quality. |
| [`OpenXRCompositionLayerCylinder`](classes/OpenXRCompositionLayerCylinder.md) | class | An OpenXR composition layer that allows rendering a [`SubViewport`](classes/SubViewport.md) on an internal slice of a cylinder. |
| [`OpenXRCompositionLayerEquirect`](classes/OpenXRCompositionLayerEquirect.md) | class | An OpenXR composition layer that allows rendering a [`SubViewport`](classes/SubViewport.md) on an internal slice of a sphere. |
| [`OpenXRCompositionLayerQuad`](classes/OpenXRCompositionLayerQuad.md) | class | An OpenXR composition layer that allows rendering a [`SubViewport`](classes/SubViewport.md) on a quad. |
| [`OpenXRHand`](classes/OpenXRHand.md) | class | This node enables OpenXR's hand tracking functionality. |
| [`OpenXRRenderModel`](classes/OpenXRRenderModel.md) | class | This node will display an OpenXR render model by accessing the associated GLTF and processes all animation data (if supported by the XR runtime). |
| [`OpenXRRenderModelManager`](classes/OpenXRRenderModelManager.md) | class | This helper node will automatically manage displaying render models. |
| [`OpenXRVisibilityMask`](classes/OpenXRVisibilityMask.md) | class | The visibility mask allows us to black out the part of the render result that is invisible due to lens distortion. |
| [`Path3D`](classes/Path3D.md) | class | Can have [`PathFollow3D`](classes/PathFollow3D.md) child nodes moving along the [`Curve3D`](classes/Curve3D.md). |
| [`PathFollow3D`](classes/PathFollow3D.md) | class | This node takes its parent [`Path3D`](classes/Path3D.md), and returns the coordinates of a point within it, given a distance from the first vertex. |
| [`PhysicalBone3D`](classes/PhysicalBone3D.md) | class | The `PhysicalBone3D` node is a physics body that can be used to make bones in a [`Skeleton3D`](classes/Skeleton3D.md) react to physics. |
| [`PhysicalBoneSimulator3D`](classes/PhysicalBoneSimulator3D.md) | class | Node that can be the parent of [`PhysicalBone3D`](classes/PhysicalBone3D.md) and can apply the simulation results to [`Skeleton3D`](classes/Skeleton3D.md). |
| [`PhysicsBody3D`](classes/PhysicsBody3D.md) | class | `PhysicsBody3D` is an abstract base class for 3D game objects affected by physics. |
| [`PinJoint3D`](classes/PinJoint3D.md) | class | A physics joint that attaches two 3D physics bodies at a single point, allowing them to freely rotate. |
| [`RayCast3D`](classes/RayCast3D.md) | class | A raycast represents a ray from its origin to its `TargetPosition` that finds the closest object along its path, if it intersects any. |
| [`ReflectionProbe`](classes/ReflectionProbe.md) | class | Captures its surroundings as a cubemap, and stores versions of it with increasing levels of blur to simulate different material roughnesses. |
| [`RemoteTransform3D`](classes/RemoteTransform3D.md) | class | RemoteTransform3D pushes its own [`Transform3D`](classes/Transform3D.md) to another [`Node3D`](classes/Node3D.md) derived Node (called the remote node) in the scene. |
| [`RetargetModifier3D`](classes/RetargetModifier3D.md) | class | Retrieves the pose (or global pose) relative to the parent Skeleton's rest in model space and transfers it to the child Skeleton. |
| [`RigidBody3D`](classes/RigidBody3D.md) | class | `RigidBody3D` implements full 3D physics. |
| [`RootMotionView`](classes/RootMotionView.md) | class | *Root motion* refers to an animation technique where a mesh's skeleton is used to give impulse to a character. |
| [`ShapeCast3D`](classes/ShapeCast3D.md) | class | Shape casting allows to detect collision objects by sweeping its `Shape` along the cast direction determined by `TargetPosition`. |
| [`Skeleton3D`](classes/Skeleton3D.md) | class | `Skeleton3D` provides an interface for managing a hierarchy of bones, including pose, rest and animation (see [`Animation`](classes/Animation.md)). |
| [`SkeletonIK3D`](classes/SkeletonIK3D.md) | class | SkeletonIK3D is used to rotate all bones of a [`Skeleton3D`](classes/Skeleton3D.md) bone chain a way that places the end bone at a desired 3D position. |
| [`SkeletonModifier3D`](classes/SkeletonModifier3D.md) | class | `SkeletonModifier3D` retrieves a target [`Skeleton3D`](classes/Skeleton3D.md) by having a [`Skeleton3D`](classes/Skeleton3D.md) parent. |
| [`SliderJoint3D`](classes/SliderJoint3D.md) | class | A physics joint that restricts the movement of a 3D physics body along an axis relative to another physics body. |
| [`SoftBody3D`](classes/SoftBody3D.md) | class | A deformable 3D physics mesh. |
| [`SplineIK3D`](classes/SplineIK3D.md) | class | A [`SkeletonModifier3D`](classes/SkeletonModifier3D.md) for aligning bones along a [`Path3D`](classes/Path3D.md). |
| [`SpotLight3D`](classes/SpotLight3D.md) | class | A Spotlight is a type of [`Light3D`](classes/Light3D.md) node that emits lights in a specific direction, in the shape of a cone. |
| [`SpringArm3D`](classes/SpringArm3D.md) | class | `SpringArm3D` casts a ray or a shape along its Z axis and moves all its direct children to the collision point, with an optional margin. |
| [`SpringBoneCollision3D`](classes/SpringBoneCollision3D.md) | class | A collision can be a child of [`SpringBoneSimulator3D`](classes/SpringBoneSimulator3D.md). |
| [`SpringBoneCollisionCapsule3D`](classes/SpringBoneCollisionCapsule3D.md) | class | A capsule shape collision that interacts with [`SpringBoneSimulator3D`](classes/SpringBoneSimulator3D.md). |
| [`SpringBoneCollisionPlane3D`](classes/SpringBoneCollisionPlane3D.md) | class | An infinite plane collision that interacts with [`SpringBoneSimulator3D`](classes/SpringBoneSimulator3D.md). |
| [`SpringBoneCollisionSphere3D`](classes/SpringBoneCollisionSphere3D.md) | class | A sphere shape collision that interacts with [`SpringBoneSimulator3D`](classes/SpringBoneSimulator3D.md). |
| [`SpringBoneSimulator3D`](classes/SpringBoneSimulator3D.md) | class | This [`SkeletonModifier3D`](classes/SkeletonModifier3D.md) can be used to wiggle hair, cloth, and tails. |
| [`Sprite3D`](classes/Sprite3D.md) | class | A node that displays a 2D texture in a 3D environment. |
| [`SpriteBase3D`](classes/SpriteBase3D.md) | class | A node that displays 2D texture information in a 3D environment. |
| [`StaticBody3D`](classes/StaticBody3D.md) | class | A static 3D physics body. |
| [`TwoBoneIK3D`](classes/TwoBoneIK3D.md) | class | This [`IKModifier3D`](classes/IKModifier3D.md) requires a pole target. |
| [`VehicleBody3D`](classes/VehicleBody3D.md) | class | This physics body implements all the physics logic needed to simulate a car. |
| [`VehicleWheel3D`](classes/VehicleWheel3D.md) | class | A node used as a child of a [`VehicleBody3D`](classes/VehicleBody3D.md) parent to simulate the behavior of one of its wheels. |
| [`VisibleOnScreenEnabler3D`](classes/VisibleOnScreenEnabler3D.md) | class | `VisibleOnScreenEnabler3D` contains a box-shaped region of 3D space and a target node. |
| [`VisibleOnScreenNotifier3D`](classes/VisibleOnScreenNotifier3D.md) | class | `VisibleOnScreenNotifier3D` represents a box-shaped region of 3D space. |
| [`VisualInstance3D`](classes/VisualInstance3D.md) | class | The `VisualInstance3D` is used to connect a resource to a visual representation. |
| [`VoxelGI`](classes/VoxelGI.md) | class | `VoxelGI`s are used to provide high-quality real-time indirect light and reflections to scenes. |
| [`XRAnchor3D`](classes/XRAnchor3D.md) | class | The `XRAnchor3D` point is an [`XRNode3D`](classes/XRNode3D.md) that maps a real world location identified by the AR platform to a position within the game world. |
| [`XRBodyModifier3D`](classes/XRBodyModifier3D.md) | class | This node uses body tracking data from an [`XRBodyTracker`](classes/XRBodyTracker.md) to pose the skeleton of a body mesh. |
| [`XRCamera3D`](classes/XRCamera3D.md) | class | A camera node which automatically positions itself based on XR tracking data. |
| [`XRController3D`](classes/XRController3D.md) | class | This is a helper 3D node that is linked to the tracking of controllers. |
| [`XRFaceModifier3D`](classes/XRFaceModifier3D.md) | class | This node applies weights from an [`XRFaceTracker`](classes/XRFaceTracker.md) to a mesh with supporting face blend shapes. |
| [`XRHandModifier3D`](classes/XRHandModifier3D.md) | class | This node uses hand tracking data from an [`XRHandTracker`](classes/XRHandTracker.md) to pose the skeleton of a hand mesh. |
| [`XRNode3D`](classes/XRNode3D.md) | class | This node can be bound to a specific pose of an [`XRPositionalTracker`](classes/XRPositionalTracker.md) and will automatically have its [`Node3D.Transform`](classes/Node3D.md) updated by the [`XRServer`](classes/XRServer.md). |
| [`XROrigin3D`](classes/XROrigin3D.md) | class | This is a special node within the AR/VR system that maps the physical location of the center of our tracking space to the virtual location within our game world. |

## 资源（Resource）（414）

| 类型 | 种类 | 简介 |
| --- | --- | --- |
| [`AnimatedTexture`](classes/AnimatedTexture.md) | class | `AnimatedTexture` is a resource format for frame-based animations, where multiple textures can be chained automatically with a predefined delay for each frame. |
| [`Animation`](classes/Animation.md) | class | This resource holds data that can be used to animate anything in the engine. |
| [`AnimationLibrary`](classes/AnimationLibrary.md) | class | An animation library stores a set of animations accessible through [`StringName`](classes/StringName.md) keys, for use with [`AnimationPlayer`](classes/AnimationPlayer.md) nodes. |
| [`AnimationNode`](classes/AnimationNode.md) | class | Base resource for [`AnimationTree`](classes/AnimationTree.md) nodes. |
| [`AnimationNodeAdd2`](classes/AnimationNodeAdd2.md) | class | A resource to add to an [`AnimationNodeBlendTree`](classes/AnimationNodeBlendTree.md). |
| [`AnimationNodeAdd3`](classes/AnimationNodeAdd3.md) | class | A resource to add to an [`AnimationNodeBlendTree`](classes/AnimationNodeBlendTree.md). |
| [`AnimationNodeAnimation`](classes/AnimationNodeAnimation.md) | class | A resource to add to an [`AnimationNodeBlendTree`](classes/AnimationNodeBlendTree.md). |
| [`AnimationNodeBlend2`](classes/AnimationNodeBlend2.md) | class | A resource to add to an [`AnimationNodeBlendTree`](classes/AnimationNodeBlendTree.md). |
| [`AnimationNodeBlend3`](classes/AnimationNodeBlend3.md) | class | A resource to add to an [`AnimationNodeBlendTree`](classes/AnimationNodeBlendTree.md). |
| [`AnimationNodeBlendSpace1D`](classes/AnimationNodeBlendSpace1D.md) | class | A resource used by [`AnimationNodeBlendTree`](classes/AnimationNodeBlendTree.md). |
| [`AnimationNodeBlendSpace2D`](classes/AnimationNodeBlendSpace2D.md) | class | A resource used by [`AnimationNodeBlendTree`](classes/AnimationNodeBlendTree.md). |
| [`AnimationNodeBlendTree`](classes/AnimationNodeBlendTree.md) | class | This animation node may contain a sub-tree of any other type animation nodes, such as [`AnimationNodeTransition`](classes/AnimationNodeTransition.md), [`AnimationNodeBlend2`](classes/AnimationNodeBlend2.md), [`AnimationNodeBlend3`](Anim… |
| [`AnimationNodeExtension`](classes/AnimationNodeExtension.md) | class | `AnimationNodeExtension` exposes the APIs of [`AnimationRootNode`](classes/AnimationRootNode.md) to allow users to extend it from GDScript, C#, or C++. |
| [`AnimationNodeOneShot`](classes/AnimationNodeOneShot.md) | class | A resource to add to an [`AnimationNodeBlendTree`](classes/AnimationNodeBlendTree.md). |
| [`AnimationNodeOutput`](classes/AnimationNodeOutput.md) | class | A node created automatically in an [`AnimationNodeBlendTree`](classes/AnimationNodeBlendTree.md) that outputs the final animation. |
| [`AnimationNodeStateMachine`](classes/AnimationNodeStateMachine.md) | class | Contains multiple [`AnimationRootNode`](classes/AnimationRootNode.md)s representing animation states, connected in a graph. |
| [`AnimationNodeStateMachinePlayback`](classes/AnimationNodeStateMachinePlayback.md) | class | Allows control of [`AnimationTree`](classes/AnimationTree.md) state machines created with [`AnimationNodeStateMachine`](classes/AnimationNodeStateMachine.md). |
| [`AnimationNodeStateMachineTransition`](classes/AnimationNodeStateMachineTransition.md) | class | The path generated when using [`AnimationNodeStateMachinePlayback.Travel`](classes/AnimationNodeStateMachinePlayback.md) is limited to the nodes connected by `AnimationNodeStateMachineTransition`. |
| [`AnimationNodeSub2`](classes/AnimationNodeSub2.md) | class | A resource to add to an [`AnimationNodeBlendTree`](classes/AnimationNodeBlendTree.md). |
| [`AnimationNodeSync`](classes/AnimationNodeSync.md) | class | An animation node used to combine, mix, or blend two or more animations together while keeping them synchronized within an [`AnimationTree`](classes/AnimationTree.md). |
| [`AnimationNodeTimeScale`](classes/AnimationNodeTimeScale.md) | class | Allows to scale the speed of the animation (or reverse it) in any child [`AnimationNode`](classes/AnimationNode.md)s. |
| [`AnimationNodeTimeSeek`](classes/AnimationNodeTimeSeek.md) | class | This animation node can be used to cause a seek command to happen to any sub-children of the animation graph. |
| [`AnimationNodeTransition`](classes/AnimationNodeTransition.md) | class | Simple state machine for cases which don't require a more advanced [`AnimationNodeStateMachine`](classes/AnimationNodeStateMachine.md). |
| [`AnimationRootNode`](classes/AnimationRootNode.md) | class | `AnimationRootNode` is a base class for [`AnimationNode`](classes/AnimationNode.md)s that hold a complete animation. |
| [`ArrayMesh`](classes/ArrayMesh.md) | class | The `ArrayMesh` is used to construct a [`Mesh`](classes/Mesh.md) by specifying the attributes as arrays. |
| [`ArrayOccluder3D`](classes/ArrayOccluder3D.md) | class | `ArrayOccluder3D` stores an arbitrary 3D polygon shape that can be used by the engine's occlusion culling system. |
| [`AtlasTexture`](classes/AtlasTexture.md) | class | [`Texture2D`](classes/Texture2D.md) resource that draws only part of its `Atlas` texture, as defined by the `Region`. |
| [`AudioBusLayout`](classes/AudioBusLayout.md) | class | Stores position, muting, solo, bypass, effects, effect position, volume, and the connections between buses. |
| [`AudioEffect`](classes/AudioEffect.md) | class | The base [`Resource`](classes/Resource.md) for every audio effect. |
| [`AudioEffectAmplify`](classes/AudioEffectAmplify.md) | class | Increases or decreases the volume being routed through the audio bus. |
| [`AudioEffectBandLimitFilter`](classes/AudioEffectBandLimitFilter.md) | class | A "band-limit" filter attenuates the frequencies at [`AudioEffectFilter.CutoffHz`](classes/AudioEffectFilter.md), and allows frequencies outside the frequency threshold to pass unchanged. |
| [`AudioEffectBandPassFilter`](classes/AudioEffectBandPassFilter.md) | class | A "band-pass" filter allows the frequencies at [`AudioEffectFilter.CutoffHz`](classes/AudioEffectFilter.md) to pass unchanged, and attenuates frequencies outside the frequency threshold. |
| [`AudioEffectCapture`](classes/AudioEffectCapture.md) | class | Copies all audio frames, also known as "samples" or "audio samples", from the attached audio bus into its internal ring buffer. |
| [`AudioEffectChorus`](classes/AudioEffectChorus.md) | class | A "chorus" effect creates multiple copies of the original audio (called "voices") with variations in pitch, and layers on top of the original, giving the impression that the sound comes from multiple sources. |
| [`AudioEffectCompressor`](classes/AudioEffectCompressor.md) | class | A "compressor" decreases the volume of sounds when it exceeds a certain volume threshold level. |
| [`AudioEffectDelay`](classes/AudioEffectDelay.md) | class | A "delay" effect plays the input audio signal back after a period of time. |
| [`AudioEffectDistortion`](classes/AudioEffectDistortion.md) | class | A "distortion" effect modifies the waveform via a nonlinear mathematical function (see available ones in [`AudioEffectDistortion.ModeEnum`](classes/AudioEffectDistortion.md)), based on the amplitude of the waveform's samples. |
| [`AudioEffectEQ`](classes/AudioEffectEQ.md) | class | An "equalizer" gives you control over the gain of frequencies in the entire spectrum, by allowing their adjustment through bands. |
| [`AudioEffectEQ10`](classes/AudioEffectEQ10.md) | class | Frequency bands: |
| [`AudioEffectEQ21`](classes/AudioEffectEQ21.md) | class | Frequency bands: |
| [`AudioEffectEQ6`](classes/AudioEffectEQ6.md) | class | Frequency bands: |
| [`AudioEffectFilter`](classes/AudioEffectFilter.md) | class | A "filter" controls the gain of frequencies, using `CutoffHz` as a frequency threshold. |
| [`AudioEffectHardLimiter`](classes/AudioEffectHardLimiter.md) | class | A "limiter" disallows audio signals from exceeding a given volume threshold level in dB. |
| [`AudioEffectHighPassFilter`](classes/AudioEffectHighPassFilter.md) | class | A "high-pass" filter attenuates frequencies lower than [`AudioEffectFilter.CutoffHz`](classes/AudioEffectFilter.md) and allows higher frequencies to pass unchanged. |
| [`AudioEffectHighShelfFilter`](classes/AudioEffectHighShelfFilter.md) | class | A "high-shelf" filter controls the gain of all frequencies above [`AudioEffectFilter.CutoffHz`](classes/AudioEffectFilter.md). |
| [`AudioEffectLimiter`](classes/AudioEffectLimiter.md) | class | A "limiter" is an audio effect designed to stop audio signals from exceeding a specified volume threshold level, and usually works by decreasing the volume or soft-clipping the audio. |
| [`AudioEffectLowPassFilter`](classes/AudioEffectLowPassFilter.md) | class | A "low-pass" filter attenuates frequencies higher than [`AudioEffectFilter.CutoffHz`](classes/AudioEffectFilter.md) and allows lower frequencies to pass unchanged. |
| [`AudioEffectLowShelfFilter`](classes/AudioEffectLowShelfFilter.md) | class | A "low-shelf" filter controls the gain of all frequencies below [`AudioEffectFilter.CutoffHz`](classes/AudioEffectFilter.md). |
| [`AudioEffectNotchFilter`](classes/AudioEffectNotchFilter.md) | class | A "notch" filter attenuates frequencies at [`AudioEffectFilter.CutoffHz`](classes/AudioEffectFilter.md) and allows frequencies outside the frequency threshold to pass unchanged. |
| [`AudioEffectPanner`](classes/AudioEffectPanner.md) | class | Determines how much of the audio signal is sent to the left and right channels. |
| [`AudioEffectPhaser`](classes/AudioEffectPhaser.md) | class | A "phaser" effect creates a copy of the original audio that phase-rotates differently across the entire frequency spectrum, with the use of a series of all-pass filter stages (6 in this effect). |
| [`AudioEffectPitchShift`](classes/AudioEffectPitchShift.md) | class | Allows modulation of pitch without modifying speed. |
| [`AudioEffectRecord`](classes/AudioEffectRecord.md) | class | Allows the user to record the sound from an audio bus into an [`AudioStreamWav`](classes/AudioStreamWav.md). |
| [`AudioEffectReverb`](classes/AudioEffectReverb.md) | class | A "reverb" effect plays the input audio back continuously, decaying over a period of time. |
| [`AudioEffectSpectrumAnalyzer`](classes/AudioEffectSpectrumAnalyzer.md) | class | Calculates a Fourier Transform of the audio signal. |
| [`AudioEffectStereoEnhance`](classes/AudioEffectStereoEnhance.md) | class | Adjusts gain of the left and right channels, and makes mono sounds stereo through phase shifting. |
| [`AudioStream`](classes/AudioStream.md) | class | Base class for audio streams. |
| [`AudioStreamGenerator`](classes/AudioStreamGenerator.md) | class | `AudioStreamGenerator` is a type of audio stream that does not play back sounds on its own; instead, it expects a script to generate audio data for it. |
| [`AudioStreamInteractive`](classes/AudioStreamInteractive.md) | class | This is an audio stream that can playback music interactively, combining clips and a transition table. |
| [`AudioStreamMicrophone`](classes/AudioStreamMicrophone.md) | class | When used directly in an [`AudioStreamPlayer`](classes/AudioStreamPlayer.md) node, `AudioStreamMicrophone` plays back microphone input in real-time. |
| [`AudioStreamMP3`](classes/AudioStreamMP3.md) | class | MP3 audio stream driver. |
| [`AudioStreamOggVorbis`](classes/AudioStreamOggVorbis.md) | class | The AudioStreamOggVorbis class is a specialized [`AudioStream`](classes/AudioStream.md) for handling Ogg Vorbis file formats. |
| [`AudioStreamPlaylist`](classes/AudioStreamPlaylist.md) | class | An audio stream that can play back sub-streams in sequence. |
| [`AudioStreamPolyphonic`](classes/AudioStreamPolyphonic.md) | class | AudioStream that lets the user play custom streams at any time from code, simultaneously using a single player. |
| [`AudioStreamRandomizer`](classes/AudioStreamRandomizer.md) | class | Picks a random AudioStream from the pool, depending on the playback mode, and applies random pitch shifting and volume shifting during playback. |
| [`AudioStreamSynchronized`](classes/AudioStreamSynchronized.md) | class | This is a stream that can be fitted with sub-streams, which will be played in-sync. |
| [`AudioStreamWav`](classes/AudioStreamWav.md) | class | AudioStreamWAV stores sound samples loaded from WAV files. |
| [`BaseMaterial3D`](classes/BaseMaterial3D.md) | class | This class serves as a default material with a wide variety of rendering features and properties without the need to write shader code. |
| [`Bitmap`](classes/Bitmap.md) | class | A two-dimensional array of boolean values, can be used to efficiently store a binary matrix (every matrix element takes only one bit) and query the values using natural cartesian coordinates. |
| [`BlitMaterial`](classes/BlitMaterial.md) | class | A material resource that can be used by DrawableTextures when processing blit calls to draw. |
| [`BoneMap`](classes/BoneMap.md) | class | This class contains a dictionary that uses a list of bone names in [`SkeletonProfile`](classes/SkeletonProfile.md) as key names. |
| [`BoxMesh`](classes/BoxMesh.md) | class | Generate an axis-aligned box [`PrimitiveMesh`](classes/PrimitiveMesh.md). |
| [`BoxOccluder3D`](classes/BoxOccluder3D.md) | class | `BoxOccluder3D` stores a cuboid shape that can be used by the engine's occlusion culling system. |
| [`BoxShape3D`](classes/BoxShape3D.md) | class | A 3D box shape, intended for use in physics. |
| [`ButtonGroup`](classes/ButtonGroup.md) | class | A group of [`BaseButton`](classes/BaseButton.md)-derived buttons. |
| [`CameraAttributes`](classes/CameraAttributes.md) | class | Controls camera-specific attributes such as depth of field and exposure override. |
| [`CameraAttributesPhysical`](classes/CameraAttributesPhysical.md) | class | `CameraAttributesPhysical` is used to set rendering settings based on a physically-based camera's settings. |
| [`CameraAttributesPractical`](classes/CameraAttributesPractical.md) | class | Controls camera-specific attributes such as auto-exposure, depth of field, and exposure override. |
| [`CameraTexture`](classes/CameraTexture.md) | class | This texture gives access to the camera texture provided by a [`CameraFeed`](classes/CameraFeed.md). |
| [`CanvasItemMaterial`](classes/CanvasItemMaterial.md) | class | `CanvasItemMaterial`s provide a means of modifying the textures associated with a CanvasItem. |
| [`CanvasTexture`](classes/CanvasTexture.md) | class | `CanvasTexture` is an alternative to [`ImageTexture`](classes/ImageTexture.md) for 2D rendering. |
| [`CapsuleMesh`](classes/CapsuleMesh.md) | class | Class representing a capsule-shaped [`PrimitiveMesh`](classes/PrimitiveMesh.md). |
| [`CapsuleShape2D`](classes/CapsuleShape2D.md) | class | A 2D capsule shape, intended for use in physics. |
| [`CapsuleShape3D`](classes/CapsuleShape3D.md) | class | A 3D capsule shape, intended for use in physics. |
| [`CircleShape2D`](classes/CircleShape2D.md) | class | A 2D circle shape, intended for use in physics. |
| [`CodeHighlighter`](classes/CodeHighlighter.md) | class | By adjusting various properties of this resource, you can change the colors of strings, comments, numbers, and other text patterns inside a [`TextEdit`](classes/TextEdit.md) control. |
| [`ColorPalette`](classes/ColorPalette.md) | class | The `ColorPalette` resource is designed to store and manage a collection of colors. |
| [`Compositor`](classes/Compositor.md) | class | The compositor resource stores attributes used to customize how a [`Viewport`](classes/Viewport.md) is rendered. |
| [`CompositorEffect`](classes/CompositorEffect.md) | class | This resource defines a custom rendering effect that can be applied to [`Viewport`](classes/Viewport.md)s through the viewports' [`Environment`](classes/Environment.md). |
| [`CompressedCubemap`](classes/CompressedCubemap.md) | class | A cubemap that is loaded from a `.ccube` file. |
| [`CompressedCubemapArray`](classes/CompressedCubemapArray.md) | class | A cubemap array that is loaded from a `.ccubearray` file. |
| [`CompressedTexture2D`](classes/CompressedTexture2D.md) | class | A texture that is loaded from a `.ctex` file. |
| [`CompressedTexture2DArray`](classes/CompressedTexture2DArray.md) | class | A texture array that is loaded from a `.ctexarray` file. |
| [`CompressedTexture3D`](classes/CompressedTexture3D.md) | class | `CompressedTexture3D` is the VRAM-compressed counterpart of [`ImageTexture3D`](classes/ImageTexture3D.md). |
| [`CompressedTextureLayered`](classes/CompressedTextureLayered.md) | class | Base class for [`CompressedTexture2DArray`](classes/CompressedTexture2DArray.md) and [`CompressedTexture3D`](classes/CompressedTexture3D.md). |
| [`ConcavePolygonShape2D`](classes/ConcavePolygonShape2D.md) | class | A 2D polyline shape, intended for use in physics. |
| [`ConcavePolygonShape3D`](classes/ConcavePolygonShape3D.md) | class | A 3D trimesh shape, intended for use in physics. |
| [`ConvexPolygonShape2D`](classes/ConvexPolygonShape2D.md) | class | A 2D convex polygon shape, intended for use in physics. |
| [`ConvexPolygonShape3D`](classes/ConvexPolygonShape3D.md) | class | A 3D convex polyhedron shape, intended for use in physics. |
| [`CryptoKey`](classes/CryptoKey.md) | class | The CryptoKey class represents a cryptographic key. |
| [`CSharpScript`](classes/CSharpScript.md) | class | This class represents a C# script. |
| [`Cubemap`](classes/Cubemap.md) | class | A cubemap is made of 6 textures organized in layers. |
| [`CubemapArray`](classes/CubemapArray.md) | class | `CubemapArray`s are made of an array of [`Cubemap`](classes/Cubemap.md)s. |
| [`Curve`](classes/Curve.md) | class | This resource describes a mathematical curve by defining a set of points and tangents at each point. |
| [`Curve2D`](classes/Curve2D.md) | class | This class describes a Bézier curve in 2D space. |
| [`Curve3D`](classes/Curve3D.md) | class | This class describes a Bézier curve in 3D space. |
| [`CurveTexture`](classes/CurveTexture.md) | class | A 1D texture where pixel brightness corresponds to points on a unit [`Curve`](classes/Curve.md) resource, either in grayscale or in red. |
| [`CurveXyzTexture`](classes/CurveXyzTexture.md) | class | A 1D texture where the red, green, and blue color channels correspond to points on 3 unit [`Curve`](classes/Curve.md) resources. |
| [`CylinderMesh`](classes/CylinderMesh.md) | class | Class representing a cylindrical [`PrimitiveMesh`](classes/PrimitiveMesh.md). |
| [`CylinderShape3D`](classes/CylinderShape3D.md) | class | A 3D cylinder shape, intended for use in physics. |
| [`DpiTexture`](classes/DpiTexture.md) | class | An automatically scalable [`Texture2D`](classes/Texture2D.md) based on an SVG image. |
| [`DrawableTexture2D`](classes/DrawableTexture2D.md) | class | A 2D texture that can be modified via blit calls, copying from a target texture to itself. |
| [`Environment`](classes/Environment.md) | class | Resource for environment nodes (like [`WorldEnvironment`](classes/WorldEnvironment.md)) that define multiple environment operations (such as background [`Sky`](classes/Sky.md) or [`Color`](classes/Color.md), ambient light, fog, depth-of-field..… |
| [`ExternalTexture`](classes/ExternalTexture.md) | class | Displays the content of an external buffer provided by the platform. |
| [`FastNoiseLite`](classes/FastNoiseLite.md) | class | This class generates noise using the FastNoiseLite library, which is a collection of several noise algorithms including Cellular, Perlin, Value, and more. |
| [`FbxDocument`](classes/FbxDocument.md) | class | The FBXDocument handles FBX documents. |
| [`FbxState`](classes/FbxState.md) | class | The FBXState handles the state data imported from FBX files. |
| [`FogMaterial`](classes/FogMaterial.md) | class | A [`Material`](classes/Material.md) resource that can be used by [`FogVolume`](classes/FogVolume.md)s to draw volumetric effects. |
| [`FoldableGroup`](classes/FoldableGroup.md) | class | A group of [`FoldableContainer`](classes/FoldableContainer.md)-derived nodes. |
| [`Font`](classes/Font.md) | class | Abstract base class for different font types. |
| [`FontFile`](classes/FontFile.md) | class | `FontFile` contains a set of glyphs to represent Unicode characters imported from a font file, as well as a cache of rasterized glyphs, and a set of fallback [`Font`](classes/Font.md)s to use. |
| [`FontVariation`](classes/FontVariation.md) | class | Provides OpenType variations, simulated bold / slant, and additional font settings like OpenType features and extra spacing. |
| [`GDExtension`](classes/GDExtension.md) | class | The `GDExtension` resource type represents a [shared library](https://en.wikipedia.org/wiki/Shared_library) which can expand the functionality of the engine. |
| [`GDScript`](classes/GDScript.md) | class | A script implemented in the GDScript programming language, saved with the `.gd` extension. |
| [`GltfAccessor`](classes/GltfAccessor.md) | class | GLTFAccessor is a data structure representing a glTF `accessor` that would be found in the `"accessors"` array. |
| [`GltfAnimation`](classes/GltfAnimation.md) | class |  |
| [`GltfBufferView`](classes/GltfBufferView.md) | class | GLTFBufferView is a data structure representing a glTF `bufferView` that would be found in the `"bufferViews"` array. |
| [`GltfCamera`](classes/GltfCamera.md) | class | Represents a camera as defined by the base glTF spec. |
| [`GltfDocument`](classes/GltfDocument.md) | class | GLTFDocument supports reading data from a glTF file, buffer, or Godot scene. |
| [`GltfDocumentExtension`](classes/GltfDocumentExtension.md) | class | Extends the functionality of the [`GltfDocument`](classes/GltfDocument.md) class by allowing you to run arbitrary code at various stages of glTF import or export. |
| [`GltfDocumentExtensionConvertImporterMesh`](classes/GltfDocumentExtensionConvertImporterMesh.md) | class |  |
| [`GltfLight`](classes/GltfLight.md) | class | Represents a light as defined by the `KHR_lights_punctual` glTF extension. |
| [`GltfMesh`](classes/GltfMesh.md) | class | GLTFMesh handles 3D mesh data imported from glTF files. |
| [`GltfNode`](classes/GltfNode.md) | class | Represents a glTF node. |
| [`GltfPhysicsBody`](classes/GltfPhysicsBody.md) | class | Represents a physics body as an intermediary between the `OMI_physics_body` glTF data and Godot's nodes, and it's abstracted in a way that allows adding support for different glTF physics extensions in the future. |
| [`GltfPhysicsShape`](classes/GltfPhysicsShape.md) | class | Represents a physics shape as defined by the `OMI_physics_shape` or `OMI_collider` glTF extensions. |
| [`GltfSkeleton`](classes/GltfSkeleton.md) | class |  |
| [`GltfSkin`](classes/GltfSkin.md) | class |  |
| [`GltfSpecGloss`](classes/GltfSpecGloss.md) | class | KHR_materials_pbrSpecularGlossiness is an archived glTF extension. |
| [`GltfState`](classes/GltfState.md) | class | Contains all nodes and resources of a glTF file. |
| [`GltfTexture`](classes/GltfTexture.md) | class |  |
| [`GltfTextureSampler`](classes/GltfTextureSampler.md) | class | Represents a texture sampler as defined by the base glTF spec. |
| [`Gradient`](classes/Gradient.md) | class | This resource describes a color transition by defining a set of colored points and how to interpolate between them. |
| [`GradientTexture1D`](classes/GradientTexture1D.md) | class | A 1D texture that obtains colors from a [`Gradient`](classes/Gradient.md) to fill the texture data. |
| [`GradientTexture2D`](classes/GradientTexture2D.md) | class | A 2D texture that obtains colors from a [`Gradient`](classes/Gradient.md) to fill the texture data. |
| [`HeightMapShape3D`](classes/HeightMapShape3D.md) | class | A 3D heightmap shape, intended for use in physics to provide a shape for a [`CollisionShape3D`](classes/CollisionShape3D.md). |
| [`Image`](classes/Image.md) | class | Native image datatype. |
| [`ImageTexture`](classes/ImageTexture.md) | class | A [`Texture2D`](classes/Texture2D.md) based on an [`Image`](classes/Image.md). |
| [`ImageTexture3D`](classes/ImageTexture3D.md) | class | `ImageTexture3D` is a 3-dimensional [`ImageTexture`](classes/ImageTexture.md) that has a width, height, and depth. |
| [`ImageTextureLayered`](classes/ImageTextureLayered.md) | class | Base class for [`Texture2DArray`](classes/Texture2DArray.md), [`Cubemap`](classes/Cubemap.md) and [`CubemapArray`](classes/CubemapArray.md). |
| [`ImmediateMesh`](classes/ImmediateMesh.md) | class | A mesh type optimized for creating geometry manually, similar to OpenGL 1.x immediate mode. |
| [`ImporterMesh`](classes/ImporterMesh.md) | class | ImporterMesh is a type of [`Resource`](classes/Resource.md) analogous to [`ArrayMesh`](classes/ArrayMesh.md). |
| [`InputEvent`](classes/InputEvent.md) | class | Abstract base class of all types of input events. |
| [`InputEventAction`](classes/InputEventAction.md) | class | Contains a generic action which can be targeted from several types of inputs. |
| [`InputEventFromWindow`](classes/InputEventFromWindow.md) | class | InputEventFromWindow represents events specifically received by windows. |
| [`InputEventGesture`](classes/InputEventGesture.md) | class | InputEventGestures are sent when a user performs a supported gesture on a touch screen. |
| [`InputEventJoypadButton`](classes/InputEventJoypadButton.md) | class | Input event type for gamepad buttons. |
| [`InputEventJoypadMotion`](classes/InputEventJoypadMotion.md) | class | Stores information about joystick motions. |
| [`InputEventKey`](classes/InputEventKey.md) | class | An input event for keys on a keyboard. |
| [`InputEventMagnifyGesture`](classes/InputEventMagnifyGesture.md) | class | Stores the factor of a magnifying touch gesture. |
| [`InputEventMidi`](classes/InputEventMidi.md) | class | InputEventMIDI stores information about messages from [MIDI](https://en.wikipedia.org/wiki/MIDI) (Musical Instrument Digital Interface) devices. |
| [`InputEventMouse`](classes/InputEventMouse.md) | class | Stores general information about mouse events. |
| [`InputEventMouseButton`](classes/InputEventMouseButton.md) | class | Stores information about mouse click events. |
| [`InputEventMouseMotion`](classes/InputEventMouseMotion.md) | class | Stores information about a mouse or a pen motion. |
| [`InputEventPanGesture`](classes/InputEventPanGesture.md) | class | Stores information about pan gestures. |
| [`InputEventScreenDrag`](classes/InputEventScreenDrag.md) | class | Stores information about screen drag events. |
| [`InputEventScreenTouch`](classes/InputEventScreenTouch.md) | class | Stores information about multi-touch press/release input events. |
| [`InputEventShortcut`](classes/InputEventShortcut.md) | class | InputEventShortcut is a special event that can be received in [`Node._Input()`](classes/Node.md), [`Node._ShortcutInput()`](classes/Node.md), and [`Node._UnhandledInput()`](classes/Node.md). |
| [`InputEventWithModifiers`](classes/InputEventWithModifiers.md) | class | Stores information about mouse, keyboard, and touch gesture input events. |
| [`JointLimitation3D`](classes/JointLimitation3D.md) | class | The limitation is attached to each joint and limits the rotation of the bone. |
| [`JointLimitationCone3D`](classes/JointLimitationCone3D.md) | class | A cone shape limitation that interacts with [`ChainIK3D`](classes/ChainIK3D.md). |
| [`Json`](classes/Json.md) | class | The `Json` class enables all data types to be converted to and from a JSON string. |
| [`LabelSettings`](classes/LabelSettings.md) | class | `LabelSettings` is a resource that provides common settings to customize the text in a [`Label`](classes/Label.md). |
| [`LightmapGIData`](classes/LightmapGIData.md) | class | `LightmapGIData` contains baked lightmap and dynamic object probe data for [`LightmapGI`](classes/LightmapGI.md). |
| [`Material`](classes/Material.md) | class | `Material` is a base resource used for coloring and shading geometry. |
| [`Mesh`](classes/Mesh.md) | class | Mesh is a type of [`Resource`](classes/Resource.md) that contains vertex array-based geometry, divided in *surfaces*. |
| [`MeshLibrary`](classes/MeshLibrary.md) | class | A library of meshes. |
| [`MeshTexture`](classes/MeshTexture.md) | class | Simple texture that uses a mesh to draw itself. |
| [`MissingResource`](classes/MissingResource.md) | class | This is an internal editor class intended for keeping data of resources of unknown type (most likely this type was supplied by an extension that is no longer loaded). |
| [`MultiMesh`](classes/MultiMesh.md) | class | MultiMesh provides low-level mesh instancing. |
| [`NavigationMesh`](classes/NavigationMesh.md) | class | A navigation mesh is a collection of polygons that define which areas of an environment are traversable to aid agents in pathfinding through complicated spaces. |
| [`NavigationMeshSourceGeometryData2D`](classes/NavigationMeshSourceGeometryData2D.md) | class | Container for parsed source geometry data used in navigation mesh baking. |
| [`NavigationMeshSourceGeometryData3D`](classes/NavigationMeshSourceGeometryData3D.md) | class | Container for parsed source geometry data used in navigation mesh baking. |
| [`NavigationPolygon`](classes/NavigationPolygon.md) | class | A navigation mesh can be created either by baking it with the help of the [`NavigationServer2D`](classes/NavigationServer2D.md), or by adding vertices and convex polygon indices arrays manually. |
| [`Noise`](classes/Noise.md) | class | This class defines the interface for noise generation libraries to inherit from. |
| [`NoiseTexture2D`](classes/NoiseTexture2D.md) | class | Uses the [`FastNoiseLite`](classes/FastNoiseLite.md) library or other noise generators to fill the texture data of your desired size. |
| [`NoiseTexture3D`](classes/NoiseTexture3D.md) | class | Uses the [`FastNoiseLite`](classes/FastNoiseLite.md) library or other noise generators to fill the texture data of your desired size. |
| [`Occluder3D`](classes/Occluder3D.md) | class | `Occluder3D` stores an occluder shape that can be used by the engine's occlusion culling system. |
| [`OccluderPolygon2D`](classes/OccluderPolygon2D.md) | class | Editor facility that helps you draw a 2D polygon used as resource for [`LightOccluder2D`](classes/LightOccluder2D.md). |
| [`OggPacketSequence`](classes/OggPacketSequence.md) | class | A sequence of Ogg packets. |
| [`OpenXRAction`](classes/OpenXRAction.md) | class | This resource defines an OpenXR action. |
| [`OpenXRActionBindingModifier`](classes/OpenXRActionBindingModifier.md) | class | Binding modifier that applies on individual actions related to an interaction profile. |
| [`OpenXRActionMap`](classes/OpenXRActionMap.md) | class | OpenXR uses an action system similar to Godots Input map system to bind inputs and outputs on various types of XR controllers to named actions. |
| [`OpenXRActionSet`](classes/OpenXRActionSet.md) | class | Action sets in OpenXR define a collection of actions that can be activated in unison. |
| [`OpenXRAnalogThresholdModifier`](classes/OpenXRAnalogThresholdModifier.md) | class | The analog threshold binding modifier can modify a float input to a boolean input with specified thresholds. |
| [`OpenXRBindingModifier`](classes/OpenXRBindingModifier.md) | class | Binding modifier base class. |
| [`OpenXRDpadBindingModifier`](classes/OpenXRDpadBindingModifier.md) | class | The DPad binding modifier converts an axis input to a dpad output, emulating a DPad. |
| [`OpenXRHapticBase`](classes/OpenXRHapticBase.md) | class | This is a base class for haptic feedback resources. |
| [`OpenXRHapticVibration`](classes/OpenXRHapticVibration.md) | class | This haptic feedback resource makes it possible to define a vibration based haptic feedback pulse that can be triggered through actions in the OpenXR action map. |
| [`OpenXRInteractionProfile`](classes/OpenXRInteractionProfile.md) | class | This object stores suggested bindings for an interaction profile. |
| [`OpenXRIPBinding`](classes/OpenXRIPBinding.md) | class | This binding resource binds an [`OpenXRAction`](classes/OpenXRAction.md) to an input or output. |
| [`OpenXripBindingModifier`](classes/OpenXripBindingModifier.md) | class | Binding modifier that applies directly on an interaction profile. |
| [`OptimizedTranslation`](classes/OptimizedTranslation.md) | class | An optimized translation. |
| [`OrmMaterial3D`](classes/OrmMaterial3D.md) | class | ORMMaterial3D's properties are inherited from [`BaseMaterial3D`](classes/BaseMaterial3D.md). |
| [`PackedDataContainer`](classes/PackedDataContainer.md) | class | `PackedDataContainer` can be used to efficiently store data from untyped containers. |
| [`PackedScene`](classes/PackedScene.md) | class | A simplified interface to a scene file. |
| [`PanoramaSkyMaterial`](classes/PanoramaSkyMaterial.md) | class | A resource referenced in a [`Sky`](classes/Sky.md) that is used to draw a background. |
| [`ParticleProcessMaterial`](classes/ParticleProcessMaterial.md) | class | `ParticleProcessMaterial` defines particle properties and behavior. |
| [`PhysicalSkyMaterial`](classes/PhysicalSkyMaterial.md) | class | The `PhysicalSkyMaterial` uses the Preetham analytic daylight model to draw a sky based on physical properties. |
| [`PhysicsMaterial`](classes/PhysicsMaterial.md) | class | Holds physics-related properties of a surface, namely its roughness and bounciness. |
| [`PlaceholderCubemap`](classes/PlaceholderCubemap.md) | class | This class replaces a [`Cubemap`](classes/Cubemap.md) or a [`Cubemap`](classes/Cubemap.md)-derived class in 2 conditions: |
| [`PlaceholderCubemapArray`](classes/PlaceholderCubemapArray.md) | class | This class replaces a [`CubemapArray`](classes/CubemapArray.md) or a [`CubemapArray`](classes/CubemapArray.md)-derived class in 2 conditions: |
| [`PlaceholderMaterial`](classes/PlaceholderMaterial.md) | class | This class is used when loading a project that uses a [`Material`](classes/Material.md) subclass in 2 conditions: |
| [`PlaceholderMesh`](classes/PlaceholderMesh.md) | class | This class is used when loading a project that uses a [`Mesh`](classes/Mesh.md) subclass in 2 conditions: |
| [`PlaceholderTexture2D`](classes/PlaceholderTexture2D.md) | class | This class is used when loading a project that uses a [`Texture2D`](classes/Texture2D.md) subclass in 2 conditions: |
| [`PlaceholderTexture2DArray`](classes/PlaceholderTexture2DArray.md) | class | This class is used when loading a project that uses a [`Texture2D`](classes/Texture2D.md) subclass in 2 conditions: |
| [`PlaceholderTexture3D`](classes/PlaceholderTexture3D.md) | class | This class is used when loading a project that uses a [`Texture3D`](classes/Texture3D.md) subclass in 2 conditions: |
| [`PlaceholderTextureLayered`](classes/PlaceholderTextureLayered.md) | class | This class is used when loading a project that uses a [`TextureLayered`](classes/TextureLayered.md) subclass in 2 conditions: |
| [`PlaneMesh`](classes/PlaneMesh.md) | class | Class representing a planar [`PrimitiveMesh`](classes/PrimitiveMesh.md). |
| [`PointMesh`](classes/PointMesh.md) | class | A `PointMesh` is a primitive mesh composed of a single point. |
| [`PolygonOccluder3D`](classes/PolygonOccluder3D.md) | class | `PolygonOccluder3D` stores a polygon shape that can be used by the engine's occlusion culling system. |
| [`PolygonPathFinder`](classes/PolygonPathFinder.md) | class |  |
| [`PortableCompressedTexture2D`](classes/PortableCompressedTexture2D.md) | class | This class allows storing compressed textures as self contained (not imported) resources. |
| [`PrimitiveMesh`](classes/PrimitiveMesh.md) | class | Base class for all primitive meshes. |
| [`PrismMesh`](classes/PrismMesh.md) | class | Class representing a prism-shaped [`PrimitiveMesh`](classes/PrimitiveMesh.md). |
| [`ProceduralSkyMaterial`](classes/ProceduralSkyMaterial.md) | class | `ProceduralSkyMaterial` provides a way to create an effective background quickly by defining procedural parameters for the sun, the sky and the ground. |
| [`QuadMesh`](classes/QuadMesh.md) | class | Class representing a square [`PrimitiveMesh`](classes/PrimitiveMesh.md). |
| [`QuadOccluder3D`](classes/QuadOccluder3D.md) | class | `QuadOccluder3D` stores a flat plane shape that can be used by the engine's occlusion culling system. |
| [`RDShaderFile`](classes/RDShaderFile.md) | class | Compiled shader file in SPIR-V form. |
| [`RDShaderSpirV`](classes/RDShaderSpirV.md) | class | `RDShaderSpirV` represents an [`RDShaderFile`](classes/RDShaderFile.md)'s [SPIR-V](https://www.khronos.org/spir/) code for various shader stages, as well as possible compilation error messages. |
| [`RectangleShape2D`](classes/RectangleShape2D.md) | class | A 2D rectangle shape, intended for use in physics. |
| [`Resource`](classes/Resource.md) | class | Resource is the base class for all Godot-specific resource types, serving primarily as data containers. |
| [`RibbonTrailMesh`](classes/RibbonTrailMesh.md) | class | `RibbonTrailMesh` represents a straight ribbon-shaped mesh with variable width. |
| [`RichTextEffect`](classes/RichTextEffect.md) | class | A custom effect for a [`RichTextLabel`](classes/RichTextLabel.md), which can be loaded in the [`RichTextLabel`](classes/RichTextLabel.md) inspector or using [`RichTextLabel.InstallEffect`](classes/RichTextLabel.md). |
| [`SceneReplicationConfig`](classes/SceneReplicationConfig.md) | class | Configuration for properties to synchronize with a [MultiplayerSynchronizer]. |
| [`Script`](classes/Script.md) | class | A class stored as a resource. |
| [`ScriptExtension`](classes/ScriptExtension.md) | class |  |
| [`SegmentShape2D`](classes/SegmentShape2D.md) | class | A 2D line segment shape, intended for use in physics. |
| [`SeparationRayShape2D`](classes/SeparationRayShape2D.md) | class | A 2D ray shape, intended for use in physics. |
| [`SeparationRayShape3D`](classes/SeparationRayShape3D.md) | class | A 3D ray shape, intended for use in physics. |
| [`Shader`](classes/Shader.md) | class | A custom shader program implemented in the Godot shading language, saved with the `.gdshader` extension. |
| [`ShaderInclude`](classes/ShaderInclude.md) | class | A shader include file, saved with the `.gdshaderinc` extension. |
| [`ShaderMaterial`](classes/ShaderMaterial.md) | class | A material that uses a custom [`Shader`](classes/Shader.md) program to render visual items (canvas items, meshes, skies, fog), or to process particles. |
| [`Shape2D`](classes/Shape2D.md) | class | Abstract base class for all 2D shapes, intended for use in physics. |
| [`Shape3D`](classes/Shape3D.md) | class | Abstract base class for all 3D shapes, intended for use in physics. |
| [`Shortcut`](classes/Shortcut.md) | class | Shortcuts (also known as hotkeys) are containers of [`InputEvent`](classes/InputEvent.md) resources. |
| [`SkeletonModification2D`](classes/SkeletonModification2D.md) | class | This resource provides an interface that can be expanded so code that operates on [`Bone2D`](classes/Bone2D.md) nodes in a [`Skeleton2D`](classes/Skeleton2D.md) can be mixed and matched together to create complex interactions. |
| [`SkeletonModification2DCcdik`](classes/SkeletonModification2DCcdik.md) | class | This [`SkeletonModification2D`](classes/SkeletonModification2D.md) uses an algorithm called Cyclic Coordinate Descent Inverse Kinematics, or CCDIK, to manipulate a chain of bones in a [`Skeleton2D`](classes/Skeleton2D.md) so it reaches … |
| [`SkeletonModification2DFabrik`](classes/SkeletonModification2DFabrik.md) | class | This [`SkeletonModification2D`](classes/SkeletonModification2D.md) uses an algorithm called Forward And Backward Reaching Inverse Kinematics, or FABRIK, to rotate a bone chain so that it reaches a target. |
| [`SkeletonModification2DJiggle`](classes/SkeletonModification2DJiggle.md) | class | This modification moves a series of bones, typically called a bone chain, towards a target. |
| [`SkeletonModification2DLookAt`](classes/SkeletonModification2DLookAt.md) | class | This [`SkeletonModification2D`](classes/SkeletonModification2D.md) rotates a bone to look a target. |
| [`SkeletonModification2DPhysicalBones`](classes/SkeletonModification2DPhysicalBones.md) | class | This modification takes the transforms of [`PhysicalBone2D`](classes/PhysicalBone2D.md) nodes and applies them to [`Bone2D`](classes/Bone2D.md) nodes. |
| [`SkeletonModification2DStackHolder`](classes/SkeletonModification2DStackHolder.md) | class | This [`SkeletonModification2D`](classes/SkeletonModification2D.md) holds a reference to a [`SkeletonModificationStack2D`](classes/SkeletonModificationStack2D.md), allowing you to use multiple modification stacks on a single [`Skeleton2D… |
| [`SkeletonModification2DTwoBoneIK`](classes/SkeletonModification2DTwoBoneIK.md) | class | This [`SkeletonModification2D`](classes/SkeletonModification2D.md) uses an algorithm typically called TwoBoneIK. |
| [`SkeletonModificationStack2D`](classes/SkeletonModificationStack2D.md) | class | This resource is used by the Skeleton and holds a stack of [`SkeletonModification2D`](classes/SkeletonModification2D.md)s. |
| [`SkeletonProfile`](classes/SkeletonProfile.md) | class | This resource is used in `EditorScenePostImport`. |
| [`SkeletonProfileHumanoid`](classes/SkeletonProfileHumanoid.md) | class | A [`SkeletonProfile`](classes/SkeletonProfile.md) as a preset that is optimized for the human form. |
| [`Skin`](classes/Skin.md) | class |  |
| [`Sky`](classes/Sky.md) | class | The `Sky` class uses a [`Material`](classes/Material.md) to render a 3D environment's background and the light it emits by updating the reflection/radiance cubemaps. |
| [`SphereMesh`](classes/SphereMesh.md) | class | Class representing a spherical [`PrimitiveMesh`](classes/PrimitiveMesh.md). |
| [`SphereOccluder3D`](classes/SphereOccluder3D.md) | class | `SphereOccluder3D` stores a sphere shape that can be used by the engine's occlusion culling system. |
| [`SphereShape3D`](classes/SphereShape3D.md) | class | A 3D sphere shape, intended for use in physics. |
| [`SpriteFrames`](classes/SpriteFrames.md) | class | Sprite frame library for an [`AnimatedSprite2D`](classes/AnimatedSprite2D.md) or [`AnimatedSprite3D`](classes/AnimatedSprite3D.md) node. |
| [`StandardMaterial3D`](classes/StandardMaterial3D.md) | class | `StandardMaterial3D`'s properties are inherited from [`BaseMaterial3D`](classes/BaseMaterial3D.md). |
| [`StyleBox`](classes/StyleBox.md) | class | `StyleBox` is an abstract base class for drawing stylized boxes for UI elements. |
| [`StyleBoxEmpty`](classes/StyleBoxEmpty.md) | class | An empty [`StyleBox`](classes/StyleBox.md) that can be used to display nothing instead of the default style (e.g. |
| [`StyleBoxFlat`](classes/StyleBoxFlat.md) | class | By configuring various properties of this style box, you can achieve many common looks without the need of a texture. |
| [`StyleBoxLine`](classes/StyleBoxLine.md) | class | A [`StyleBox`](classes/StyleBox.md) that displays a single line of a given color and thickness. |
| [`StyleBoxTexture`](classes/StyleBoxTexture.md) | class | A texture-based nine-patch [`StyleBox`](classes/StyleBox.md), in a way similar to [`NinePatchRect`](classes/NinePatchRect.md). |
| [`SyntaxHighlighter`](classes/SyntaxHighlighter.md) | class | Base class for syntax highlighters. |
| [`SystemFont`](classes/SystemFont.md) | class | `SystemFont` loads a font from a system font with the first matching name from `FontNames`. |
| [`TextMesh`](classes/TextMesh.md) | class | Generate a [`PrimitiveMesh`](classes/PrimitiveMesh.md) from the text. |
| [`Texture`](classes/Texture.md) | class | `Texture` is the base class for all texture types. |
| [`Texture2D`](classes/Texture2D.md) | class | A texture works by registering an image in the video hardware, which then can be used in 3D models or 2D [`Sprite2D`](classes/Sprite2D.md) or GUI [`Control`](classes/Control.md). |
| [`Texture2DArray`](classes/Texture2DArray.md) | class | A Texture2DArray is different from a Texture3D: The Texture2DArray does not support trilinear interpolation between the [`Image`](classes/Image.md)s, i.e. |
| [`Texture2DArrayRD`](classes/Texture2DArrayRD.md) | class | This texture array class allows you to use a 2D array texture created directly on the [`RenderingDevice`](classes/RenderingDevice.md) as a texture for materials, meshes, etc. |
| [`Texture2Drd`](classes/Texture2Drd.md) | class | This texture class allows you to use a 2D texture created directly on the [`RenderingDevice`](classes/RenderingDevice.md) as a texture for materials, meshes, etc. |
| [`Texture3D`](classes/Texture3D.md) | class | Base class for [`ImageTexture3D`](classes/ImageTexture3D.md) and [`CompressedTexture3D`](classes/CompressedTexture3D.md). |
| [`Texture3Drd`](classes/Texture3Drd.md) | class | This texture class allows you to use a 3D texture created directly on the [`RenderingDevice`](classes/RenderingDevice.md) as a texture for materials, meshes, etc. |
| [`TextureCubemapArrayRD`](classes/TextureCubemapArrayRD.md) | class | This texture class allows you to use a cubemap array texture created directly on the [`RenderingDevice`](classes/RenderingDevice.md) as a texture for materials, meshes, etc. |
| [`TextureCubemapRD`](classes/TextureCubemapRD.md) | class | This texture class allows you to use a cubemap texture created directly on the [`RenderingDevice`](classes/RenderingDevice.md) as a texture for materials, meshes, etc. |
| [`TextureLayered`](classes/TextureLayered.md) | class | Base class for [`ImageTextureLayered`](classes/ImageTextureLayered.md) and [`CompressedTextureLayered`](classes/CompressedTextureLayered.md). |
| [`TextureLayeredRD`](classes/TextureLayeredRD.md) | class | Base class for [`Texture2DArrayRD`](classes/Texture2DArrayRD.md), [`TextureCubemapRD`](classes/TextureCubemapRD.md) and [`TextureCubemapArrayRD`](classes/TextureCubemapArrayRD.md). |
| [`Theme`](classes/Theme.md) | class | A resource used for styling/skinning [`Control`](classes/Control.md) and [`Window`](classes/Window.md) nodes. |
| [`TileMapPattern`](classes/TileMapPattern.md) | class | This resource holds a set of cells to help bulk manipulations of [`TileMap`](classes/TileMap.md). |
| [`TileSet`](classes/TileSet.md) | class | A TileSet is a library of tiles for a [`TileMapLayer`](classes/TileMapLayer.md). |
| [`TileSetAtlasSource`](classes/TileSetAtlasSource.md) | class | An atlas is a grid of tiles laid out on a texture. |
| [`TileSetScenesCollectionSource`](classes/TileSetScenesCollectionSource.md) | class | When placed on a [`TileMapLayer`](classes/TileMapLayer.md), tiles from `TileSetScenesCollectionSource` will automatically instantiate an associated scene at the cell's position in the TileMapLayer. |
| [`TileSetSource`](classes/TileSetSource.md) | class | Exposes a set of tiles for a [`TileSet`](classes/TileSet.md) resource. |
| [`TorusMesh`](classes/TorusMesh.md) | class | Class representing a torus [`PrimitiveMesh`](classes/PrimitiveMesh.md). |
| [`Translation`](classes/Translation.md) | class | `Translation` maps a collection of strings to their individual translations, and also provides convenience methods for pluralization. |
| [`TubeTrailMesh`](classes/TubeTrailMesh.md) | class | `TubeTrailMesh` represents a straight tube-shaped mesh with variable width. |
| [`VideoStream`](classes/VideoStream.md) | class | Base resource type for all video streams. |
| [`VideoStreamPlayback`](classes/VideoStreamPlayback.md) | class | This class is intended to be overridden by video decoder extensions with custom implementations of [`VideoStream`](classes/VideoStream.md). |
| [`VideoStreamTheora`](classes/VideoStreamTheora.md) | class | [`VideoStream`](classes/VideoStream.md) resource handling the [Ogg Theora](https://www.theora.org/) video format with `.ogv` extension. |
| [`ViewportTexture`](classes/ViewportTexture.md) | class | A `ViewportTexture` provides the content of a [`Viewport`](classes/Viewport.md) as a dynamic [`Texture2D`](classes/Texture2D.md). |
| [`VisualShader`](classes/VisualShader.md) | class | This class provides a graph-like visual editor for creating a [`Shader`](classes/Shader.md). |
| [`VisualShaderNode`](classes/VisualShaderNode.md) | class | Visual shader graphs consist of various nodes. |
| [`VisualShaderNodeBillboard`](classes/VisualShaderNodeBillboard.md) | class | The output port of this node needs to be connected to `Model View Matrix` port of [`VisualShaderNodeOutput`](classes/VisualShaderNodeOutput.md). |
| [`VisualShaderNodeBooleanConstant`](classes/VisualShaderNodeBooleanConstant.md) | class | Has only one output port and no inputs. |
| [`VisualShaderNodeBooleanParameter`](classes/VisualShaderNodeBooleanParameter.md) | class | Translated to `uniform bool` in the shader language. |
| [`VisualShaderNodeClamp`](classes/VisualShaderNodeClamp.md) | class | Constrains a value to lie between `min` and `max` values. |
| [`VisualShaderNodeColorConstant`](classes/VisualShaderNodeColorConstant.md) | class | Has two output ports representing RGB and alpha channels of [`Color`](classes/Color.md). |
| [`VisualShaderNodeColorFunc`](classes/VisualShaderNodeColorFunc.md) | class | Accept a [`Color`](classes/Color.md) to the input port and transform it according to `Function`. |
| [`VisualShaderNodeColorOp`](classes/VisualShaderNodeColorOp.md) | class | Applies `Operator` to two color inputs. |
| [`VisualShaderNodeColorParameter`](classes/VisualShaderNodeColorParameter.md) | class | Translated to `uniform vec4` in the shader language. |
| [`VisualShaderNodeComment`](classes/VisualShaderNodeComment.md) | class | This node was replaced by [`VisualShaderNodeFrame`](classes/VisualShaderNodeFrame.md) and only exists to preserve compatibility. |
| [`VisualShaderNodeCompare`](classes/VisualShaderNodeCompare.md) | class | Compares `a` and `b` of `Type` by `Function`. |
| [`VisualShaderNodeConstant`](classes/VisualShaderNodeConstant.md) | class | This is an abstract class. |
| [`VisualShaderNodeCubemap`](classes/VisualShaderNodeCubemap.md) | class | Translated to `texture(cubemap, vec3)` in the shader language. |
| [`VisualShaderNodeCubemapParameter`](classes/VisualShaderNodeCubemapParameter.md) | class | Translated to `uniform samplerCube` in the shader language. |
| [`VisualShaderNodeCurveTexture`](classes/VisualShaderNodeCurveTexture.md) | class | Comes with a built-in editor for texture's curves. |
| [`VisualShaderNodeCurveXyzTexture`](classes/VisualShaderNodeCurveXyzTexture.md) | class | Comes with a built-in editor for texture's curves. |
| [`VisualShaderNodeCustom`](classes/VisualShaderNodeCustom.md) | class | By inheriting this class you can create a custom [`VisualShader`](classes/VisualShader.md) script addon which will be automatically added to the Visual Shader Editor. |
| [`VisualShaderNodeDerivativeFunc`](classes/VisualShaderNodeDerivativeFunc.md) | class | This node is only available in `Fragment` and `Light` visual shaders. |
| [`VisualShaderNodeDeterminant`](classes/VisualShaderNodeDeterminant.md) | class | Translates to `determinant(x)` in the shader language. |
| [`VisualShaderNodeDistanceFade`](classes/VisualShaderNodeDistanceFade.md) | class | The distance fade effect fades out each pixel based on its distance to another object. |
| [`VisualShaderNodeDotProduct`](classes/VisualShaderNodeDotProduct.md) | class | Translates to `dot(a, b)` in the shader language. |
| [`VisualShaderNodeExpression`](classes/VisualShaderNodeExpression.md) | class | Custom Godot Shading Language expression, with a custom number of input and output ports. |
| [`VisualShaderNodeFaceForward`](classes/VisualShaderNodeFaceForward.md) | class | Translates to `faceforward(N, I, Nref)` in the shader language. |
| [`VisualShaderNodeFloatConstant`](classes/VisualShaderNodeFloatConstant.md) | class | Translated to `float` in the shader language. |
| [`VisualShaderNodeFloatFunc`](classes/VisualShaderNodeFloatFunc.md) | class | Accept a floating-point scalar (`x`) to the input port and transform it according to `Function`. |
| [`VisualShaderNodeFloatOp`](classes/VisualShaderNodeFloatOp.md) | class | Applies `Operator` to two floating-point inputs: `a` and `b`. |
| [`VisualShaderNodeFloatParameter`](classes/VisualShaderNodeFloatParameter.md) | class | Translated to `uniform float` in the shader language. |
| [`VisualShaderNodeFrame`](classes/VisualShaderNodeFrame.md) | class | A rectangular frame that can be used to group visual shader nodes together to improve organization. |
| [`VisualShaderNodeFresnel`](classes/VisualShaderNodeFresnel.md) | class | Returns falloff based on the dot product of surface normal and view direction of camera (pass associated inputs to it). |
| [`VisualShaderNodeGlobalExpression`](classes/VisualShaderNodeGlobalExpression.md) | class | Custom Godot Shader Language expression, which is placed on top of the generated shader. |
| [`VisualShaderNodeGroupBase`](classes/VisualShaderNodeGroupBase.md) | class | Currently, has no direct usage, use the derived classes instead. |
| [`VisualShaderNodeIf`](classes/VisualShaderNodeIf.md) | class | This visual shader node has six input ports: |
| [`VisualShaderNodeInput`](classes/VisualShaderNodeInput.md) | class | Gives access to input variables (built-ins) available for the shader. |
| [`VisualShaderNodeIntConstant`](classes/VisualShaderNodeIntConstant.md) | class | Translated to `int` in the shader language. |
| [`VisualShaderNodeIntFunc`](classes/VisualShaderNodeIntFunc.md) | class | Accept an integer scalar (`x`) to the input port and transform it according to `Function`. |
| [`VisualShaderNodeIntOp`](classes/VisualShaderNodeIntOp.md) | class | Applies `Operator` to two integer inputs: `a` and `b`. |
| [`VisualShaderNodeIntParameter`](classes/VisualShaderNodeIntParameter.md) | class | A [`VisualShaderNodeParameter`](classes/VisualShaderNodeParameter.md) of type `System.Int32`. |
| [`VisualShaderNodeIs`](classes/VisualShaderNodeIs.md) | class | Returns the boolean result of the comparison between `INF` or `NaN` and a scalar parameter. |
| [`VisualShaderNodeLinearSceneDepth`](classes/VisualShaderNodeLinearSceneDepth.md) | class | This node can be used in fragment shaders. |
| [`VisualShaderNodeMix`](classes/VisualShaderNodeMix.md) | class | Translates to `mix(a, b, weight)` in the shader language. |
| [`VisualShaderNodeMultiplyAdd`](classes/VisualShaderNodeMultiplyAdd.md) | class | Uses three operands to compute `(a * b + c)` expression. |
| [`VisualShaderNodeOuterProduct`](classes/VisualShaderNodeOuterProduct.md) | class | `OuterProduct` treats the first parameter `c` as a column vector (matrix with one column) and the second parameter `r` as a row vector (matrix with one row) and does a linear algebraic matrix multiply `c * r`, yielding a… |
| [`VisualShaderNodeOutput`](classes/VisualShaderNodeOutput.md) | class | This visual shader node is present in all shader graphs in form of "Output" block with multiple output value ports. |
| [`VisualShaderNodeParameter`](classes/VisualShaderNodeParameter.md) | class | A parameter represents a variable in the shader which is set externally, i.e. |
| [`VisualShaderNodeParameterRef`](classes/VisualShaderNodeParameterRef.md) | class | Creating a reference to a [`VisualShaderNodeParameter`](classes/VisualShaderNodeParameter.md) allows you to reuse this parameter in different shaders or shader stages easily. |
| [`VisualShaderNodeParticleAccelerator`](classes/VisualShaderNodeParticleAccelerator.md) | class | Particle accelerator can be used in "process" step of particle shader. |
| [`VisualShaderNodeParticleBoxEmitter`](classes/VisualShaderNodeParticleBoxEmitter.md) | class | [`VisualShaderNodeParticleEmitter`](classes/VisualShaderNodeParticleEmitter.md) that makes the particles emitted in box shape with the specified extents. |
| [`VisualShaderNodeParticleConeVelocity`](classes/VisualShaderNodeParticleConeVelocity.md) | class | This node can be used in "start" step of particle shader. |
| [`VisualShaderNodeParticleEmit`](classes/VisualShaderNodeParticleEmit.md) | class | This node internally calls `emit_subparticle` shader method. |
| [`VisualShaderNodeParticleEmitter`](classes/VisualShaderNodeParticleEmitter.md) | class | Particle emitter nodes can be used in "start" step of particle shaders and they define the starting position of the particles. |
| [`VisualShaderNodeParticleMeshEmitter`](classes/VisualShaderNodeParticleMeshEmitter.md) | class | [`VisualShaderNodeParticleEmitter`](classes/VisualShaderNodeParticleEmitter.md) that makes the particles emitted in a shape of the assigned `Mesh`. |
| [`VisualShaderNodeParticleMultiplyByAxisAngle`](classes/VisualShaderNodeParticleMultiplyByAxisAngle.md) | class | This node helps to multiply a position input vector by rotation using specific axis. |
| [`VisualShaderNodeParticleOutput`](classes/VisualShaderNodeParticleOutput.md) | class | This node defines how particles are emitted. |
| [`VisualShaderNodeParticleRandomness`](classes/VisualShaderNodeParticleRandomness.md) | class | Randomness node will output pseudo-random values of the given type based on the specified minimum and maximum values. |
| [`VisualShaderNodeParticleRingEmitter`](classes/VisualShaderNodeParticleRingEmitter.md) | class | [`VisualShaderNodeParticleEmitter`](classes/VisualShaderNodeParticleEmitter.md) that makes the particles emitted in ring shape with the specified inner and outer radii and height. |
| [`VisualShaderNodeParticleSphereEmitter`](classes/VisualShaderNodeParticleSphereEmitter.md) | class | [`VisualShaderNodeParticleEmitter`](classes/VisualShaderNodeParticleEmitter.md) that makes the particles emitted in sphere shape with the specified inner and outer radii. |
| [`VisualShaderNodeProximityFade`](classes/VisualShaderNodeProximityFade.md) | class | The proximity fade effect fades out each pixel based on its distance to another object. |
| [`VisualShaderNodeRandomRange`](classes/VisualShaderNodeRandomRange.md) | class | Random range node will output a pseudo-random scalar value in the specified range, based on the seed. |
| [`VisualShaderNodeRemap`](classes/VisualShaderNodeRemap.md) | class | Remap will transform the input range into output range, e.g. |
| [`VisualShaderNodeReroute`](classes/VisualShaderNodeReroute.md) | class | Automatically adapts its port type to the type of the incoming connection and ensures valid connections. |
| [`VisualShaderNodeResizableBase`](classes/VisualShaderNodeResizableBase.md) | class | Resizable nodes have a handle that allows the user to adjust their size as needed. |
| [`VisualShaderNodeRotationByAxis`](classes/VisualShaderNodeRotationByAxis.md) | class | RotationByAxis node will transform the vertices of a mesh with specified axis and angle in radians. |
| [`VisualShaderNodeSample3D`](classes/VisualShaderNodeSample3D.md) | class | A virtual class, use the descendants instead. |
| [`VisualShaderNodeScreenNormalWorldSpace`](classes/VisualShaderNodeScreenNormalWorldSpace.md) | class | The ScreenNormalWorldSpace node allows to create outline effects. |
| [`VisualShaderNodeScreenUVToSdf`](classes/VisualShaderNodeScreenUVToSdf.md) | class | Translates to `screen_uv_to_sdf(uv)` in the shader language. |
| [`VisualShaderNodeSdfRaymarch`](classes/VisualShaderNodeSdfRaymarch.md) | class | Casts a ray against the screen SDF (signed-distance field) and returns the distance travelled. |
| [`VisualShaderNodeSdfToScreenUV`](classes/VisualShaderNodeSdfToScreenUV.md) | class | Translates to `sdf_to_screen_uv(sdf_pos)` in the shader language. |
| [`VisualShaderNodeSmoothStep`](classes/VisualShaderNodeSmoothStep.md) | class | Translates to `smoothstep(edge0, edge1, x)` in the shader language. |
| [`VisualShaderNodeStep`](classes/VisualShaderNodeStep.md) | class | Translates to `step(edge, x)` in the shader language. |
| [`VisualShaderNodeSwitch`](classes/VisualShaderNodeSwitch.md) | class | Returns an associated value of the `OpType` type if the provided boolean value is `true` or `false`. |
| [`VisualShaderNodeTexture`](classes/VisualShaderNodeTexture.md) | class | Performs a lookup operation on the provided texture, with support for multiple texture sources to choose from. |
| [`VisualShaderNodeTexture2DArray`](classes/VisualShaderNodeTexture2DArray.md) | class | Translated to `uniform sampler2DArray` in the shader language. |
| [`VisualShaderNodeTexture2DArrayParameter`](classes/VisualShaderNodeTexture2DArrayParameter.md) | class | This parameter allows to provide a collection of textures for the shader. |
| [`VisualShaderNodeTexture2DParameter`](classes/VisualShaderNodeTexture2DParameter.md) | class | Translated to `uniform sampler2D` in the shader language. |
| [`VisualShaderNodeTexture3D`](classes/VisualShaderNodeTexture3D.md) | class | Performs a lookup operation on the provided texture, with support for multiple texture sources to choose from. |
| [`VisualShaderNodeTexture3DParameter`](classes/VisualShaderNodeTexture3DParameter.md) | class | Translated to `uniform sampler3D` in the shader language. |
| [`VisualShaderNodeTextureParameter`](classes/VisualShaderNodeTextureParameter.md) | class | Performs a lookup operation on the texture provided as a uniform for the shader. |
| [`VisualShaderNodeTextureParameterTriplanar`](classes/VisualShaderNodeTextureParameterTriplanar.md) | class | Performs a lookup operation on the texture provided as a uniform for the shader, with support for triplanar mapping. |
| [`VisualShaderNodeTextureSdf`](classes/VisualShaderNodeTextureSdf.md) | class | Translates to `texture_sdf(sdf_pos)` in the shader language. |
| [`VisualShaderNodeTextureSdfNormal`](classes/VisualShaderNodeTextureSdfNormal.md) | class | Translates to `texture_sdf_normal(sdf_pos)` in the shader language. |
| [`VisualShaderNodeTransformCompose`](classes/VisualShaderNodeTransformCompose.md) | class | Creates a 4×4 transform matrix using four vectors of type `vec3`. |
| [`VisualShaderNodeTransformConstant`](classes/VisualShaderNodeTransformConstant.md) | class | A constant [`Transform3D`](classes/Transform3D.md), which can be used as an input node. |
| [`VisualShaderNodeTransformDecompose`](classes/VisualShaderNodeTransformDecompose.md) | class | Takes a 4×4 transform matrix and decomposes it into four `vec3` values, one from each row of the matrix. |
| [`VisualShaderNodeTransformFunc`](classes/VisualShaderNodeTransformFunc.md) | class | Computes an inverse or transpose function on the provided [`Transform3D`](classes/Transform3D.md). |
| [`VisualShaderNodeTransformOp`](classes/VisualShaderNodeTransformOp.md) | class | Applies `Operator` to two transform (4×4 matrices) inputs. |
| [`VisualShaderNodeTransformParameter`](classes/VisualShaderNodeTransformParameter.md) | class | Translated to `uniform mat4` in the shader language. |
| [`VisualShaderNodeTransformVecMult`](classes/VisualShaderNodeTransformVecMult.md) | class | A multiplication operation on a transform (4×4 matrix) and a vector, with support for different multiplication operators. |
| [`VisualShaderNodeUIntConstant`](classes/VisualShaderNodeUIntConstant.md) | class | Translated to `uint` in the shader language. |
| [`VisualShaderNodeUIntFunc`](classes/VisualShaderNodeUIntFunc.md) | class | Accept an unsigned integer scalar (`x`) to the input port and transform it according to `Function`. |
| [`VisualShaderNodeUIntOp`](classes/VisualShaderNodeUIntOp.md) | class | Applies `Operator` to two unsigned integer inputs: `a` and `b`. |
| [`VisualShaderNodeUIntParameter`](classes/VisualShaderNodeUIntParameter.md) | class | A [`VisualShaderNodeParameter`](classes/VisualShaderNodeParameter.md) of type unsigned `System.Int32`. |
| [`VisualShaderNodeUVFunc`](classes/VisualShaderNodeUVFunc.md) | class | UV functions are similar to [`Vector2`](classes/Vector2.md) functions, but the input port of this node uses the shader's UV value by default. |
| [`VisualShaderNodeUVPolarCoord`](classes/VisualShaderNodeUVPolarCoord.md) | class | UV polar coord node will transform UV values into polar coordinates, with specified scale, zoom strength and repeat parameters. |
| [`VisualShaderNodeVarying`](classes/VisualShaderNodeVarying.md) | class | Varying values are shader variables that can be passed between shader functions, e.g. |
| [`VisualShaderNodeVaryingGetter`](classes/VisualShaderNodeVaryingGetter.md) | class | Outputs a value of a varying defined in the shader. |
| [`VisualShaderNodeVaryingSetter`](classes/VisualShaderNodeVaryingSetter.md) | class | Inputs a value to a varying defined in the shader. |
| [`VisualShaderNodeVec2Constant`](classes/VisualShaderNodeVec2Constant.md) | class | A constant [`Vector2`](classes/Vector2.md), which can be used as an input node. |
| [`VisualShaderNodeVec2Parameter`](classes/VisualShaderNodeVec2Parameter.md) | class | Translated to `uniform vec2` in the shader language. |
| [`VisualShaderNodeVec3Constant`](classes/VisualShaderNodeVec3Constant.md) | class | A constant [`Vector3`](classes/Vector3.md), which can be used as an input node. |
| [`VisualShaderNodeVec3Parameter`](classes/VisualShaderNodeVec3Parameter.md) | class | Translated to `uniform vec3` in the shader language. |
| [`VisualShaderNodeVec4Constant`](classes/VisualShaderNodeVec4Constant.md) | class | A constant 4D vector, which can be used as an input node. |
| [`VisualShaderNodeVec4Parameter`](classes/VisualShaderNodeVec4Parameter.md) | class | Translated to `uniform vec4` in the shader language. |
| [`VisualShaderNodeVectorBase`](classes/VisualShaderNodeVectorBase.md) | class | This is an abstract class. |
| [`VisualShaderNodeVectorCompose`](classes/VisualShaderNodeVectorCompose.md) | class | Creates a `vec2`, `vec3` or `vec4` using scalar values that can be provided from separate inputs. |
| [`VisualShaderNodeVectorDecompose`](classes/VisualShaderNodeVectorDecompose.md) | class | Takes a `vec2`, `vec3` or `vec4` and decomposes it into scalar values that can be used as separate outputs. |
| [`VisualShaderNodeVectorDistance`](classes/VisualShaderNodeVectorDistance.md) | class | Calculates distance from point represented by vector `p0` to vector `p1`. |
| [`VisualShaderNodeVectorFunc`](classes/VisualShaderNodeVectorFunc.md) | class | A visual shader node able to perform different functions using vectors. |
| [`VisualShaderNodeVectorLen`](classes/VisualShaderNodeVectorLen.md) | class | Translated to `length(p0)` in the shader language. |
| [`VisualShaderNodeVectorOp`](classes/VisualShaderNodeVectorOp.md) | class | A visual shader node for use of vector operators. |
| [`VisualShaderNodeVectorRefract`](classes/VisualShaderNodeVectorRefract.md) | class | Translated to `refract(I, N, eta)` in the shader language, where `I` is the incident vector, `N` is the normal vector and `eta` is the ratio of the indices of the refraction. |
| [`VisualShaderNodeWorldPositionFromDepth`](classes/VisualShaderNodeWorldPositionFromDepth.md) | class | The WorldPositionFromDepth node reconstructs the depth position of the pixel in world space. |
| [`VoxelGIData`](classes/VoxelGIData.md) | class | `VoxelGIData` contains baked voxel global illumination for use in a [`VoxelGI`](classes/VoxelGI.md) node. |
| [`World2D`](classes/World2D.md) | class | Class that has everything pertaining to a 2D world: A physics space, a canvas, and a sound space. |
| [`World3D`](classes/World3D.md) | class | Class that has everything pertaining to a world: A physics space, a visual scenario, and a sound space. |
| [`WorldBoundaryShape2D`](classes/WorldBoundaryShape2D.md) | class | A 2D world boundary shape, intended for use in physics. |
| [`WorldBoundaryShape3D`](classes/WorldBoundaryShape3D.md) | class | A 3D world boundary shape, intended for use in physics. |
| [`X509Certificate`](classes/X509Certificate.md) | class | The X509Certificate class represents an X509 certificate. |

## 静态类与单例（45）

| 类型 | 种类 | 简介 |
| --- | --- | --- |
| [`AccessibilityServer`](classes/AccessibilityServer.md) | static class | A server interface for screen reader support. |
| [`AudioServer`](classes/AudioServer.md) | static class | `AudioServer` is a low-level server interface for audio access. |
| [`CameraServer`](classes/CameraServer.md) | static class | The `CameraServer` keeps track of different cameras accessible in Godot. |
| [`ClassDB`](classes/ClassDB.md) | static class | Provides access to metadata stored for every available engine class. |
| [`Colors`](classes/Colors.md) | static class | This class contains color constants created from standardized color names. |
| [`CustomGCHandle`](classes/CustomGCHandle.md) | static class | Provides a GCHandle that becomes weak when unloading the assembly load context, without having to manually replace the GCHandle. |
| [`Dispatcher`](classes/Dispatcher.md) | static class |  |
| [`DisplayServer`](classes/DisplayServer.md) | static class | `DisplayServer` handles everything related to window management. |
| [`Engine`](classes/Engine.md) | static class | The `Engine` singleton allows you to query and modify the project's run-time parameters, such as frames per second, time scale, and others. |
| [`EngineDebugger`](classes/EngineDebugger.md) | static class | `EngineDebugger` handles the communication between the editor and the running game. |
| [`GD`](classes/GD.md) | static class | Godot's global functions. |
| [`GDExtensionManager`](classes/GDExtensionManager.md) | static class | The GDExtensionManager loads, initializes, and keeps track of all available [`GDExtension`](classes/GDExtension.md) libraries in the project. |
| [`Geometry2D`](classes/Geometry2D.md) | static class | Provides a set of helper functions to create geometric shapes, compute intersections between shapes, and process various other geometric operations in 2D. |
| [`Geometry3D`](classes/Geometry3D.md) | static class | Provides a set of helper functions to create geometric shapes, compute intersections between shapes, and process various other geometric operations in 3D. |
| [`Input`](classes/Input.md) | static class | The `Input` singleton handles key presses, mouse buttons and movement, gamepads, and input actions. |
| [`InputMap`](classes/InputMap.md) | static class | Manages all [`InputEventAction`](classes/InputEventAction.md) which can be created/modified from the project settings menu **Project > Project Settings > Input Map** or in code with `AddAction()` and `ActionAddEvent()`. |
| [`IP`](classes/IP.md) | static class | IP contains support functions for the Internet Protocol (IP). |
| [`JavaClassWrapper`](classes/JavaClassWrapper.md) | static class | The JavaClassWrapper singleton provides a way for the Godot application to send and receive data through the [Java Native Interface](https://developer.android.com/training/articles/perf-jni) (JNI). |
| [`JavaScriptBridge`](classes/JavaScriptBridge.md) | static class | The JavaScriptBridge singleton is implemented only in the Web export. |
| [`Marshalls`](classes/Marshalls.md) | static class | Provides data transformation and encoding utility functions. |
| [`Mathf`](classes/Mathf.md) | static class | Provides constants and static methods for common mathematical functions. |
| [`NativeMenu`](classes/NativeMenu.md) | static class | `NativeMenu` handles low-level access to the OS native global menu bar and popup menus. |
| [`NavigationMeshGenerator`](classes/NavigationMeshGenerator.md) | static class | This class is responsible for creating and clearing 3D navigation meshes used as [`NavigationMesh`](classes/NavigationMesh.md) resources inside [`NavigationRegion3D`](classes/NavigationRegion3D.md). |
| [`NavigationServer2D`](classes/NavigationServer2D.md) | static class | NavigationServer2D is the server that handles navigation maps, regions and agents. |
| [`NavigationServer2DManager`](classes/NavigationServer2DManager.md) | static class | `NavigationServer2DManager` is the API for registering [`NavigationServer2D`](classes/NavigationServer2D.md) implementations and setting the default implementation. |
| [`NavigationServer3D`](classes/NavigationServer3D.md) | static class | NavigationServer3D is the server that handles navigation maps, regions and agents. |
| [`NavigationServer3DManager`](classes/NavigationServer3DManager.md) | static class | `NavigationServer3DManager` is the API for registering [`NavigationServer3D`](classes/NavigationServer3D.md) implementations and setting the default implementation. |
| [`OS`](classes/OS.md) | static class | The `OS` class wraps the most common functionalities for communicating with the host operating system, such as the video driver, delays, environment variables, execution of binaries, command line, etc. |
| [`Performance`](classes/Performance.md) | static class | This class provides access to a number of different monitors related to performance, such as memory usage, draw calls, and FPS. |
| [`PhysicsServer2D`](classes/PhysicsServer2D.md) | static class | PhysicsServer2D is the server responsible for all 2D physics. |
| [`PhysicsServer2DManager`](classes/PhysicsServer2DManager.md) | static class | `PhysicsServer2DManager` is the API for registering [`PhysicsServer2D`](classes/PhysicsServer2D.md) implementations and for setting the default implementation. |
| [`PhysicsServer3D`](classes/PhysicsServer3D.md) | static class | PhysicsServer3D is the server responsible for all 3D physics. |
| [`PhysicsServer3DManager`](classes/PhysicsServer3DManager.md) | static class | `PhysicsServer3DManager` is the API for registering [`PhysicsServer3D`](classes/PhysicsServer3D.md) implementations and for setting the default implementation. |
| [`ProjectSettings`](classes/ProjectSettings.md) | static class | Stores variables that can be accessed from everywhere. |
| [`RenderingServer`](classes/RenderingServer.md) | static class | The rendering server is the API backend for everything visible. |
| [`ResourceLoader`](classes/ResourceLoader.md) | static class | A singleton used to load resource files from the filesystem. |
| [`ResourceSaver`](classes/ResourceSaver.md) | static class | A singleton for saving resource types to the filesystem. |
| [`ResourceUid`](classes/ResourceUid.md) | static class | Resource UIDs (Unique IDentifiers) allow the engine to keep references between resources intact, even if files are renamed or moved. |
| [`StringExtensions`](classes/StringExtensions.md) | static class | Extension methods to manipulate strings. |
| [`TextServerManager`](classes/TextServerManager.md) | static class | `TextServerManager` is the API backend for loading, enumerating, and switching [`TextServer`](classes/TextServer.md)s. |
| [`ThemeDB`](classes/ThemeDB.md) | static class | This singleton provides access to static information about [`Theme`](classes/Theme.md) resources used by the engine and by your projects. |
| [`Time`](classes/Time.md) | static class | The Time singleton allows converting time between various formats and also getting time information from the system. |
| [`TranslationServer`](classes/TranslationServer.md) | static class | The translation server is the API backend that manages all language translations. |
| [`WorkerThreadPool`](classes/WorkerThreadPool.md) | static class | The `WorkerThreadPool` singleton allocates a set of [`GodotThread`](classes/GodotThread.md)s (called worker threads) on project startup and provides methods for offloading tasks to them. |
| [`XRServer`](classes/XRServer.md) | static class | The AR/VR server is the heart of our Advanced and Virtual Reality solution and handles all the processing. |

## 内置值类型（20）

| 类型 | 种类 | 简介 |
| --- | --- | --- |
| [`Aabb`](classes/Aabb.md) | struct | Axis-Aligned Bounding Box. |
| [`Basis`](classes/Basis.md) | struct | 3×3 matrix used for 3D rotation and scale. |
| [`Callable`](classes/Callable.md) | struct | Callable is a first class object which can be held in variables and passed to functions. |
| [`Color`](classes/Color.md) | struct | A color represented by red, green, blue, and alpha (RGBA) components. |
| [`Plane`](classes/Plane.md) | struct | Plane represents a normalized plane equation. |
| [`Projection`](classes/Projection.md) | struct | A 4x4 matrix used for 3D projective transformations. |
| [`Quaternion`](classes/Quaternion.md) | struct | A unit quaternion used for representing 3D rotations. |
| [`Rect2`](classes/Rect2.md) | struct | 2D axis-aligned bounding box. |
| [`Rect2I`](classes/Rect2I.md) | struct | 2D axis-aligned bounding box using integers. |
| [`Rid`](classes/Rid.md) | struct | The RID type is used to access a low-level resource by its unique ID. |
| [`Signal`](classes/Signal.md) | struct | Represents a signal defined in an object. |
| [`Transform2D`](classes/Transform2D.md) | struct | 2×3 matrix (2 rows, 3 columns) used for 2D linear transformations. |
| [`Transform3D`](classes/Transform3D.md) | struct | 3×4 matrix (3 rows, 4 columns) used for 3D linear transformations. |
| [`Variant`](classes/Variant.md) | struct |  |
| [`Vector2`](classes/Vector2.md) | struct | 2-element structure that can be used to represent positions in 2D space or any other pair of numeric values. |
| [`Vector2I`](classes/Vector2I.md) | struct | 2-element structure that can be used to represent 2D grid coordinates or pairs of integers. |
| [`Vector3`](classes/Vector3.md) | struct | 3-element structure that can be used to represent positions in 3D space or any other pair of numeric values. |
| [`Vector3I`](classes/Vector3I.md) | struct | 3-element structure that can be used to represent 3D grid coordinates or sets of integers. |
| [`Vector4`](classes/Vector4.md) | struct | 4-element structure that can be used to represent positions in 4D space or any other pair of numeric values. |
| [`Vector4I`](classes/Vector4I.md) | struct | 4-element structure that can be used to represent 4D grid coordinates or sets of integers. |

## 界面节点（Control）（63）

| 类型 | 种类 | 简介 |
| --- | --- | --- |
| [`AspectRatioContainer`](classes/AspectRatioContainer.md) | class | A container type that arranges its child controls in a way that preserves their proportions automatically when the container is resized. |
| [`BaseButton`](classes/BaseButton.md) | class | `BaseButton` is an abstract base class for GUI buttons. |
| [`BoxContainer`](classes/BoxContainer.md) | class | A container that arranges its child controls horizontally or vertically, rearranging them automatically when their minimum size changes. |
| [`Button`](classes/Button.md) | class | `Button` is the standard themed button. |
| [`CenterContainer`](classes/CenterContainer.md) | class | `CenterContainer` is a container that keeps all of its child controls in its center at their minimum size. |
| [`CheckBox`](classes/CheckBox.md) | class | `CheckBox` allows the user to choose one of only two possible options. |
| [`CheckButton`](classes/CheckButton.md) | class | `CheckButton` is a toggle button displayed as a check field. |
| [`CodeEdit`](classes/CodeEdit.md) | class | CodeEdit is a specialized [`TextEdit`](classes/TextEdit.md) designed for editing plain text code files. |
| [`ColorPicker`](classes/ColorPicker.md) | class | A widget that provides an interface for selecting or modifying a color. |
| [`ColorPickerButton`](classes/ColorPickerButton.md) | class | Encapsulates a [`ColorPicker`](classes/ColorPicker.md), making it accessible by pressing a button. |
| [`ColorRect`](classes/ColorRect.md) | class | Displays a rectangle filled with a solid `Color`. |
| [`Container`](classes/Container.md) | class | Base class for all GUI containers. |
| [`Control`](classes/Control.md) | class | Base class for all UI-related nodes. |
| [`FlowContainer`](classes/FlowContainer.md) | class | A container that arranges its child controls horizontally or vertically and wraps them around at the borders. |
| [`FoldableContainer`](classes/FoldableContainer.md) | class | A container that can be expanded/collapsed, with a title that can be filled with controls, such as buttons. |
| [`GraphEdit`](classes/GraphEdit.md) | class | `GraphEdit` provides tools for creation, manipulation, and display of various graphs. |
| [`GraphElement`](classes/GraphElement.md) | class | `GraphElement` allows to create custom elements for a [`GraphEdit`](classes/GraphEdit.md) graph. |
| [`GraphFrame`](classes/GraphFrame.md) | class | GraphFrame is a special [`GraphElement`](classes/GraphElement.md) to which other [`GraphElement`](classes/GraphElement.md)s can be attached. |
| [`GraphNode`](classes/GraphNode.md) | class | `GraphNode` allows to create nodes for a [`GraphEdit`](classes/GraphEdit.md) graph with customizable content based on its child controls. |
| [`GridContainer`](classes/GridContainer.md) | class | `GridContainer` arranges its child controls in a grid layout. |
| [`HBoxContainer`](classes/HBoxContainer.md) | class | A variant of [`BoxContainer`](classes/BoxContainer.md) that can only arrange its child controls horizontally. |
| [`HFlowContainer`](classes/HFlowContainer.md) | class | A variant of [`FlowContainer`](classes/FlowContainer.md) that can only arrange its child controls horizontally, wrapping them around at the borders. |
| [`HScrollBar`](classes/HScrollBar.md) | class | A horizontal scrollbar, typically used to navigate through content that extends beyond the visible width of a control. |
| [`HSeparator`](classes/HSeparator.md) | class | A horizontal separator used for separating other controls that are arranged **vertically**. |
| [`HSlider`](classes/HSlider.md) | class | A horizontal slider, used to adjust a value by moving a grabber along a horizontal axis. |
| [`HSplitContainer`](classes/HSplitContainer.md) | class | A container that accepts only two child controls, then arranges them horizontally and creates a divisor between them. |
| [`ItemList`](classes/ItemList.md) | class | This control provides a vertical list of selectable items that may be in a single or in multiple columns, with each item having options for text and an icon. |
| [`Label`](classes/Label.md) | class | A control for displaying plain text. |
| [`LineEdit`](classes/LineEdit.md) | class | `LineEdit` provides an input field for editing a single line of text. |
| [`LinkButton`](classes/LinkButton.md) | class | A button that represents a link. |
| [`MarginContainer`](classes/MarginContainer.md) | class | `MarginContainer` adds an adjustable margin on each side of its child controls. |
| [`MenuBar`](classes/MenuBar.md) | class | A horizontal menu bar that creates a menu for each [`PopupMenu`](classes/PopupMenu.md) child. |
| [`MenuButton`](classes/MenuButton.md) | class | A button that brings up a [`PopupMenu`](classes/PopupMenu.md) when clicked. |
| [`NinePatchRect`](classes/NinePatchRect.md) | class | Also known as 9-slice panels, `NinePatchRect` produces clean panels of any size based on a small texture. |
| [`OptionButton`](classes/OptionButton.md) | class | `OptionButton` is a type of button that brings up a dropdown with selectable items when pressed. |
| [`Panel`](classes/Panel.md) | class | `Panel` is a GUI control that displays a [`StyleBox`](classes/StyleBox.md). |
| [`PanelContainer`](classes/PanelContainer.md) | class | A container that keeps its child controls within the area of a [`StyleBox`](classes/StyleBox.md). |
| [`ProgressBar`](classes/ProgressBar.md) | class | A control used for visual representation of a percentage. |
| [`Range`](classes/Range.md) | class | Range is an abstract base class for controls that represent a number within a range, using a configured `Step` and `Page` size. |
| [`ReferenceRect`](classes/ReferenceRect.md) | class | A rectangular box that displays only a colored border around its rectangle (see [`Control.GetRect`](classes/Control.md)). |
| [`RichTextLabel`](classes/RichTextLabel.md) | class | A control for displaying text that can contain custom fonts, images, and basic formatting. |
| [`ScrollBar`](classes/ScrollBar.md) | class | Abstract base class for scrollbars, typically used to navigate through content that extends beyond the visible area of a control. |
| [`ScrollContainer`](classes/ScrollContainer.md) | class | A container used to provide a child control with scrollbars when needed. |
| [`Separator`](classes/Separator.md) | class | Abstract base class for separators, used for separating other controls. |
| [`Slider`](classes/Slider.md) | class | Abstract base class for sliders, used to adjust a value by moving a grabber along a horizontal or vertical axis. |
| [`SpinBox`](classes/SpinBox.md) | class | `SpinBox` is a numerical input text field. |
| [`SplitContainer`](classes/SplitContainer.md) | class | A container that arranges child controls horizontally or vertically and creates grabbers between them. |
| [`SubViewportContainer`](classes/SubViewportContainer.md) | class | A container that displays the contents of underlying [`SubViewport`](classes/SubViewport.md) child nodes. |
| [`TabBar`](classes/TabBar.md) | class | A control that provides a horizontal bar with tabs. |
| [`TabContainer`](classes/TabContainer.md) | class | Arranges child controls into a tabbed view, creating a tab for each one. |
| [`TextEdit`](classes/TextEdit.md) | class | A multiline text editor. |
| [`TextureButton`](classes/TextureButton.md) | class | `TextureButton` has the same functionality as [`Button`](classes/Button.md), except it uses sprites instead of Godot's [`Theme`](classes/Theme.md) resource. |
| [`TextureProgressBar`](classes/TextureProgressBar.md) | class | TextureProgressBar works like [`ProgressBar`](classes/ProgressBar.md), but uses up to 3 textures instead of Godot's [`Theme`](classes/Theme.md) resource. |
| [`TextureRect`](classes/TextureRect.md) | class | A control that displays a texture, for example an icon inside a GUI. |
| [`Tree`](classes/Tree.md) | class | A control used to show a set of internal [`TreeItem`](classes/TreeItem.md)s in a hierarchical structure. |
| [`VBoxContainer`](classes/VBoxContainer.md) | class | A variant of [`BoxContainer`](classes/BoxContainer.md) that can only arrange its child controls vertically. |
| [`VFlowContainer`](classes/VFlowContainer.md) | class | A variant of [`FlowContainer`](classes/FlowContainer.md) that can only arrange its child controls vertically, wrapping them around at the borders. |
| [`VideoStreamPlayer`](classes/VideoStreamPlayer.md) | class | A control used for playback of [`VideoStream`](classes/VideoStream.md) resources. |
| [`VirtualJoystick`](classes/VirtualJoystick.md) | class | A customizable on-screen joystick control designed for touchscreen devices. |
| [`VScrollBar`](classes/VScrollBar.md) | class | A vertical scrollbar, typically used to navigate through content that extends beyond the visible height of a control. |
| [`VSeparator`](classes/VSeparator.md) | class | A vertical separator used for separating other controls that are arranged **horizontally**. |
| [`VSlider`](classes/VSlider.md) | class | A vertical slider, used to adjust a value by moving a grabber along a vertical axis. |
| [`VSplitContainer`](classes/VSplitContainer.md) | class | A container that accepts only two child controls, then arranges them vertically and creates a divisor between them. |

## 其他节点（28）

| 类型 | 种类 | 简介 |
| --- | --- | --- |
| [`AcceptDialog`](classes/AcceptDialog.md) | class | The default use of `AcceptDialog` is to allow it to only be accepted or closed, with the same result. |
| [`AnimationMixer`](classes/AnimationMixer.md) | class | Base class for [`AnimationPlayer`](classes/AnimationPlayer.md) and [`AnimationTree`](classes/AnimationTree.md) to manage animation lists. |
| [`AnimationPlayer`](classes/AnimationPlayer.md) | class | An animation player is used for general-purpose playback of animations. |
| [`AnimationTree`](classes/AnimationTree.md) | class | A node used for advanced animation transitions in an [`AnimationPlayer`](classes/AnimationPlayer.md). |
| [`AudioStreamPlayer`](classes/AudioStreamPlayer.md) | class | The `AudioStreamPlayer` node plays an audio stream non-positionally. |
| [`CanvasLayer`](classes/CanvasLayer.md) | class | [`CanvasItem`](classes/CanvasItem.md)-derived nodes that are direct or indirect children of a `CanvasLayer` will be drawn in that layer. |
| [`ConfirmationDialog`](classes/ConfirmationDialog.md) | class | A dialog used for confirmation of actions. |
| [`FileDialog`](classes/FileDialog.md) | class | `FileDialog` is a preset dialog used to choose files and directories in the filesystem. |
| [`HttpRequest`](classes/HttpRequest.md) | class | A node with the ability to send HTTP requests. |
| [`InstancePlaceholder`](classes/InstancePlaceholder.md) | class | Turning on the option **Load As Placeholder** for an instantiated scene in the editor causes it to be replaced by an `InstancePlaceholder` when running the game, this will not replace the node in the editor. |
| [`MissingNode`](classes/MissingNode.md) | class | This is an internal editor class intended for keeping data of nodes of unknown type (most likely this type was supplied by an extension that is no longer loaded). |
| [`MultiplayerSpawner`](classes/MultiplayerSpawner.md) | class | Spawnable scenes can be configured in the editor or through code (see `AddSpawnableScene()`). |
| [`MultiplayerSynchronizer`](classes/MultiplayerSynchronizer.md) | class | By default, `MultiplayerSynchronizer` synchronizes configured properties to all peers. |
| [`NavigationAgent2D`](classes/NavigationAgent2D.md) | class | A 2D agent used to pathfind to a position while avoiding static and dynamic obstacles. |
| [`NavigationAgent3D`](classes/NavigationAgent3D.md) | class | A 3D agent used to pathfind to a position while avoiding static and dynamic obstacles. |
| [`Node`](classes/Node.md) | class | Nodes are Godot's building blocks. |
| [`ParallaxBackground`](classes/ParallaxBackground.md) | class | A ParallaxBackground uses one or more [`ParallaxLayer`](classes/ParallaxLayer.md) child nodes to create a parallax effect. |
| [`Popup`](classes/Popup.md) | class | `Popup` is a base class for contextual windows and panels with fixed position. |
| [`PopupMenu`](classes/PopupMenu.md) | class | `PopupMenu` is a modal window used to display a list of options. |
| [`PopupPanel`](classes/PopupPanel.md) | class | A popup with a configurable panel background. |
| [`ResourcePreloader`](classes/ResourcePreloader.md) | class | This node is used to preload sub-resources inside a scene, so when the scene is loaded, all the resources are ready to use and can be retrieved from the preloader. |
| [`ShaderGlobalsOverride`](classes/ShaderGlobalsOverride.md) | class | Similar to how a [`WorldEnvironment`](classes/WorldEnvironment.md) node can be used to override the environment while a specific scene is loaded, `ShaderGlobalsOverride` can be used to override global shader parameters temporari… |
| [`StatusIndicator`](classes/StatusIndicator.md) | class | Application status indicator (aka notification area icon). [b]Note:[/b] Status indicator is implemented on macOS and Windows. |
| [`SubViewport`](classes/SubViewport.md) | class | `SubViewport` Isolates a rectangular region of a scene to be displayed independently. |
| [`Timer`](classes/Timer.md) | class | The `Timer` node is a countdown timer and is the simplest way to handle time-based logic in the engine. |
| [`Viewport`](classes/Viewport.md) | class | A `Viewport` creates a different view into the screen, or a sub-view inside another viewport. |
| [`Window`](classes/Window.md) | class | A node that creates a window. |
| [`WorldEnvironment`](classes/WorldEnvironment.md) | class | The `WorldEnvironment` node is used to configure the default [`Environment`](classes/Environment.md) for the scene. |

## 2D 节点（52）

| 类型 | 种类 | 简介 |
| --- | --- | --- |
| [`AnimatableBody2D`](classes/AnimatableBody2D.md) | class | An animatable 2D physics body. |
| [`AnimatedSprite2D`](classes/AnimatedSprite2D.md) | class | `AnimatedSprite2D` is similar to the [`Sprite2D`](classes/Sprite2D.md) node, except it carries multiple textures as animation frames. |
| [`Area2D`](classes/Area2D.md) | class | `Area2D` is a region of 2D space defined by one or multiple [`CollisionShape2D`](classes/CollisionShape2D.md) or [`CollisionPolygon2D`](classes/CollisionPolygon2D.md) child nodes. |
| [`AudioListener2D`](classes/AudioListener2D.md) | class | Once added to the scene tree and enabled using `MakeCurrent()`, this node will override the location sounds are heard from. |
| [`AudioStreamPlayer2D`](classes/AudioStreamPlayer2D.md) | class | Plays audio that is attenuated with distance to the listener. |
| [`BackBufferCopy`](classes/BackBufferCopy.md) | class | Node for back-buffering the currently-displayed screen. |
| [`Bone2D`](classes/Bone2D.md) | class | A hierarchy of `Bone2D`s can be bound to a [`Skeleton2D`](classes/Skeleton2D.md) to control and animate other [`Node2D`](classes/Node2D.md) nodes. |
| [`Camera2D`](classes/Camera2D.md) | class | Camera node for 2D scenes. |
| [`CanvasGroup`](classes/CanvasGroup.md) | class | Child [`CanvasItem`](classes/CanvasItem.md) nodes of a `CanvasGroup` are drawn as a single object. |
| [`CanvasItem`](classes/CanvasItem.md) | class | Abstract base class for everything in 2D space. |
| [`CanvasModulate`](classes/CanvasModulate.md) | class | `CanvasModulate` applies a color tint to all nodes on a canvas. |
| [`CharacterBody2D`](classes/CharacterBody2D.md) | class | `CharacterBody2D` is a specialized class for physics bodies that are meant to be user-controlled. |
| [`CollisionObject2D`](classes/CollisionObject2D.md) | class | Abstract base class for 2D physics objects. |
| [`CollisionPolygon2D`](classes/CollisionPolygon2D.md) | class | A node that provides a polygon shape to a [`CollisionObject2D`](classes/CollisionObject2D.md) parent and allows it to be edited. |
| [`CollisionShape2D`](classes/CollisionShape2D.md) | class | A node that provides a [`Shape2D`](classes/Shape2D.md) to a [`CollisionObject2D`](classes/CollisionObject2D.md) parent and allows it to be edited. |
| [`CpuParticles2D`](classes/CpuParticles2D.md) | class | CPU-based 2D particle node used to create a variety of particle systems and effects. |
| [`DampedSpringJoint2D`](classes/DampedSpringJoint2D.md) | class | A physics joint that connects two 2D physics bodies with a spring-like force. |
| [`DirectionalLight2D`](classes/DirectionalLight2D.md) | class | A directional light is a type of [`Light2D`](classes/Light2D.md) node that models an infinite number of parallel rays covering the entire scene. |
| [`GpuParticles2D`](classes/GpuParticles2D.md) | class | 2D particle node used to create a variety of particle systems and effects. |
| [`GrooveJoint2D`](classes/GrooveJoint2D.md) | class | A physics joint that restricts the movement of two 2D physics bodies to a fixed axis. |
| [`Joint2D`](classes/Joint2D.md) | class | Abstract base class for all joints in 2D physics. |
| [`Light2D`](classes/Light2D.md) | class | Casts light in a 2D environment. |
| [`LightOccluder2D`](classes/LightOccluder2D.md) | class | Occludes light cast by a Light2D, casting shadows. |
| [`Line2D`](classes/Line2D.md) | class | This node draws a 2D polyline, i.e. |
| [`Marker2D`](classes/Marker2D.md) | class | Generic 2D position hint for editing. |
| [`MeshInstance2D`](classes/MeshInstance2D.md) | class | Node used for displaying a [`Mesh`](classes/Mesh.md) in 2D. |
| [`MultiMeshInstance2D`](classes/MultiMeshInstance2D.md) | class | `MultiMeshInstance2D` is a specialized node to instance a [`MultiMesh`](classes/MultiMesh.md) resource in 2D. |
| [`NavigationLink2D`](classes/NavigationLink2D.md) | class | A link between two positions on [`NavigationRegion2D`](classes/NavigationRegion2D.md)s that agents can be routed through. |
| [`NavigationObstacle2D`](classes/NavigationObstacle2D.md) | class | An obstacle needs a navigation map and outline `Vertices` defined to work correctly. |
| [`NavigationRegion2D`](classes/NavigationRegion2D.md) | class | A traversable 2D region based on a [`NavigationPolygon`](classes/NavigationPolygon.md) that [`NavigationAgent2D`](classes/NavigationAgent2D.md)s can use for pathfinding. |
| [`Node2D`](classes/Node2D.md) | class | A 2D game object, with a transform (position, rotation, and scale). |
| [`Parallax2D`](classes/Parallax2D.md) | class | A `Parallax2D` is used to create a parallax effect. |
| [`ParallaxLayer`](classes/ParallaxLayer.md) | class | A ParallaxLayer must be the child of a [`ParallaxBackground`](classes/ParallaxBackground.md) node. |
| [`Path2D`](classes/Path2D.md) | class | Can have [`PathFollow2D`](classes/PathFollow2D.md) child nodes moving along the [`Curve2D`](classes/Curve2D.md). |
| [`PathFollow2D`](classes/PathFollow2D.md) | class | This node takes its parent [`Path2D`](classes/Path2D.md), and returns the coordinates of a point within it, given a distance from the first vertex. |
| [`PhysicalBone2D`](classes/PhysicalBone2D.md) | class | The `PhysicalBone2D` node is a [`RigidBody2D`](classes/RigidBody2D.md)-based node that can be used to make [`Bone2D`](classes/Bone2D.md)s in a [`Skeleton2D`](classes/Skeleton2D.md) react to physics. |
| [`PhysicsBody2D`](classes/PhysicsBody2D.md) | class | `PhysicsBody2D` is an abstract base class for 2D game objects affected by physics. |
| [`PinJoint2D`](classes/PinJoint2D.md) | class | A physics joint that attaches two 2D physics bodies at a single point, allowing them to freely rotate. |
| [`PointLight2D`](classes/PointLight2D.md) | class | Casts light in a 2D environment. |
| [`Polygon2D`](classes/Polygon2D.md) | class | A Polygon2D is defined by a set of points. |
| [`RayCast2D`](classes/RayCast2D.md) | class | A raycast represents a ray from its origin to its `TargetPosition` that finds the closest object along its path, if it intersects any. |
| [`RemoteTransform2D`](classes/RemoteTransform2D.md) | class | RemoteTransform2D pushes its own [`Transform2D`](classes/Transform2D.md) to another [`Node2D`](classes/Node2D.md) derived node (called the remote node) in the scene. |
| [`RigidBody2D`](classes/RigidBody2D.md) | class | `RigidBody2D` implements full 2D physics. |
| [`ShapeCast2D`](classes/ShapeCast2D.md) | class | Shape casting allows to detect collision objects by sweeping its `Shape` along the cast direction determined by `TargetPosition`. |
| [`Skeleton2D`](classes/Skeleton2D.md) | class | `Skeleton2D` parents a hierarchy of [`Bone2D`](classes/Bone2D.md) nodes. |
| [`Sprite2D`](classes/Sprite2D.md) | class | A node that displays a 2D texture. |
| [`StaticBody2D`](classes/StaticBody2D.md) | class | A static 2D physics body. |
| [`TileMap`](classes/TileMap.md) | class | Node for 2D tile-based maps. |
| [`TileMapLayer`](classes/TileMapLayer.md) | class | Node for 2D tile-based maps. |
| [`TouchScreenButton`](classes/TouchScreenButton.md) | class | TouchScreenButton allows you to create on-screen buttons for touch devices. |
| [`VisibleOnScreenEnabler2D`](classes/VisibleOnScreenEnabler2D.md) | class | `VisibleOnScreenEnabler2D` contains a rectangular region of 2D space and a target node. |
| [`VisibleOnScreenNotifier2D`](classes/VisibleOnScreenNotifier2D.md) | class | `VisibleOnScreenNotifier2D` represents a rectangular region of 2D space. |

## 其他对象（296）

| 类型 | 种类 | 简介 |
| --- | --- | --- |
| [`AccessibilityServerInstance`](classes/AccessibilityServerInstance.md) | class |  |
| [`AesContext`](classes/AesContext.md) | class | This class holds the context information required for encryption and decryption operations with AES (Advanced Encryption Standard). |
| [`AssemblyHasScriptsAttribute`](classes/AssemblyHasScriptsAttribute.md) | class | Attribute that determines that the assembly contains Godot scripts and, optionally, the collection of types that implement scripts; otherwise, retrieving the types requires lookup. |
| [`AStar2D`](classes/AStar2D.md) | class | An implementation of the A* algorithm, used to find the shortest path between two vertices on a connected graph in 2D space. |
| [`AStar3D`](classes/AStar3D.md) | class | A* (A star) is a computer algorithm used in pathfinding and graph traversal, the process of plotting short paths among vertices (points), passing through a given set of edges (segments). |
| [`AStarGrid2D`](classes/AStarGrid2D.md) | class | `AStarGrid2D` is a variant of [`AStar2D`](classes/AStar2D.md) that is specialized for partial 2D grids. |
| [`AudioEffectInstance`](classes/AudioEffectInstance.md) | class | An audio effect instance manipulates the audio it receives for a given effect. |
| [`AudioEffectSpectrumAnalyzerInstance`](classes/AudioEffectSpectrumAnalyzerInstance.md) | class | The runtime part of an [`AudioEffectSpectrumAnalyzer`](classes/AudioEffectSpectrumAnalyzer.md), which can be used to query the magnitude of a frequency range on its host bus. |
| [`AudioSample`](classes/AudioSample.md) | class | Base class for audio samples. |
| [`AudioSamplePlayback`](classes/AudioSamplePlayback.md) | class | Meta class for playing back audio samples. |
| [`AudioServerInstance`](classes/AudioServerInstance.md) | class | [`AudioServer`](classes/AudioServer.md) is a low-level server interface for audio access. |
| [`AudioStreamGeneratorPlayback`](classes/AudioStreamGeneratorPlayback.md) | class | This class is meant to be used with [`AudioStreamGenerator`](classes/AudioStreamGenerator.md) to play back the generated audio in real-time. |
| [`AudioStreamPlayback`](classes/AudioStreamPlayback.md) | class | Can play, loop, pause a scroll through audio. |
| [`AudioStreamPlaybackInteractive`](classes/AudioStreamPlaybackInteractive.md) | class | Playback component of [`AudioStreamInteractive`](classes/AudioStreamInteractive.md). |
| [`AudioStreamPlaybackOggVorbis`](classes/AudioStreamPlaybackOggVorbis.md) | class | Playback component of [`AudioStreamOggVorbis`](classes/AudioStreamOggVorbis.md). |
| [`AudioStreamPlaybackPlaylist`](classes/AudioStreamPlaybackPlaylist.md) | class | Playback component of [`AudioStreamPlaylist`](classes/AudioStreamPlaylist.md). |
| [`AudioStreamPlaybackPolyphonic`](classes/AudioStreamPlaybackPolyphonic.md) | class | Playback instance for [`AudioStreamPolyphonic`](classes/AudioStreamPolyphonic.md). |
| [`AudioStreamPlaybackResampled`](classes/AudioStreamPlaybackResampled.md) | class | Playback class used to mix an [`AudioStream`](classes/AudioStream.md)'s audio samples to [`AudioServer.GetMixRate`](classes/AudioServer.md) using cubic interpolation. |
| [`AudioStreamPlaybackSynchronized`](classes/AudioStreamPlaybackSynchronized.md) | class | Playback component of [`AudioStreamSynchronized`](classes/AudioStreamSynchronized.md). |
| [`AwaitTweener`](classes/AwaitTweener.md) | class | `AwaitTweener` is used to await a specified signal, allowing asynchronous steps in [`Tween`](classes/Tween.md) animation. |
| [`CallbackTweener`](classes/CallbackTweener.md) | class | `CallbackTweener` is used to call a method in a tweening sequence. |
| [`CameraFeed`](classes/CameraFeed.md) | class | A camera feed gives you access to a single physical camera attached to your device. |
| [`CameraServerInstance`](classes/CameraServerInstance.md) | class | The [`CameraServer`](classes/CameraServer.md) keeps track of different cameras accessible in Godot. |
| [`CharFXTransform`](classes/CharFXTransform.md) | class | By setting various properties on this object, you can control how individual characters will be displayed in a [`RichTextEffect`](classes/RichTextEffect.md). |
| [`ClassDBInstance`](classes/ClassDBInstance.md) | class | Provides access to metadata stored for every available engine class. |
| [`ConfigFile`](classes/ConfigFile.md) | class | This helper class can be used to store [`Variant`](classes/Variant.md) values on the filesystem using INI-style formatting. |
| [`Crypto`](classes/Crypto.md) | class | The Crypto class provides access to advanced cryptographic functionalities. |
| [`DirAccess`](classes/DirAccess.md) | class | This class is used to manage directories and their content, even outside of the project folder. |
| [`DisplayServerInstance`](classes/DisplayServerInstance.md) | class | [`DisplayServer`](classes/DisplayServer.md) handles everything related to window management. |
| [`DtlsServer`](classes/DtlsServer.md) | class | This class is used to store the state of a DTLS server. |
| [`EncodedObjectAsId`](classes/EncodedObjectAsId.md) | class | Utility class which holds a reference to the internal identifier of an [`GodotObject`](classes/GodotObject.md) instance, as given by [`GodotObject.GetInstanceId`](classes/GodotObject.md). |
| [`ENetConnection`](classes/ENetConnection.md) | class | ENet's purpose is to provide a relatively thin, simple and robust network communication layer on top of UDP (User Datagram Protocol). |
| [`ENetMultiplayerPeer`](classes/ENetMultiplayerPeer.md) | class | A MultiplayerPeer implementation that should be passed to [`MultiplayerApi.MultiplayerPeer`](classes/MultiplayerApi.md) after being initialized as either a client, server, or mesh. |
| [`ENetPacketPeer`](classes/ENetPacketPeer.md) | class | A PacketPeer implementation representing a peer of an [`ENetConnection`](classes/ENetConnection.md). |
| [`EngineDebuggerInstance`](classes/EngineDebuggerInstance.md) | class | [`EngineDebugger`](classes/EngineDebugger.md) handles the communication between the editor and the running game. |
| [`EngineInstance`](classes/EngineInstance.md) | class | The [`Engine`](classes/Engine.md) singleton allows you to query and modify the project's run-time parameters, such as frames per second, time scale, and others. |
| [`EngineProfiler`](classes/EngineProfiler.md) | class | This class can be used to implement custom profilers that are able to interact with the engine and editor debugger. |
| [`ExportAttribute`](classes/ExportAttribute.md) | class | Exports the annotated member as a property of the Godot Object. |
| [`ExportCategoryAttribute`](classes/ExportCategoryAttribute.md) | class | Define a new category for the following exported properties. |
| [`ExportGroupAttribute`](classes/ExportGroupAttribute.md) | class | Define a new group for the following exported properties. |
| [`ExportSubgroupAttribute`](classes/ExportSubgroupAttribute.md) | class | Define a new subgroup for the following exported properties. |
| [`ExportToolButtonAttribute`](classes/ExportToolButtonAttribute.md) | class | Exports the annotated [`Callable`](classes/Callable.md) as a clickable button. |
| [`Expression`](classes/Expression.md) | class | An expression can be made of any arithmetic operation, built-in math function call, method call of a passed instance, or built-in type construction call. |
| [`FileAccess`](classes/FileAccess.md) | class | This class can be used to permanently store data in the user device's file system and to read from it. |
| [`FramebufferCacheRD`](classes/FramebufferCacheRD.md) | class | Framebuffer cache manager for [`RenderingDevice`](classes/RenderingDevice.md)-based renderers. |
| [`GDExtensionManagerInstance`](classes/GDExtensionManagerInstance.md) | class | The GDExtensionManager loads, initializes, and keeps track of all available [`GDExtension`](classes/GDExtension.md) libraries in the project. |
| [`Geometry2DInstance`](classes/Geometry2DInstance.md) | class | Provides a set of helper functions to create geometric shapes, compute intersections between shapes, and process various other geometric operations in 2D. |
| [`Geometry3DInstance`](classes/Geometry3DInstance.md) | class | Provides a set of helper functions to create geometric shapes, compute intersections between shapes, and process various other geometric operations in 3D. |
| [`GlobalClassAttribute`](classes/GlobalClassAttribute.md) | class | Exposes the target class as a global script class to Godot Engine. |
| [`GltfObjectModelProperty`](classes/GltfObjectModelProperty.md) | class | GLTFObjectModelProperty defines a mapping between a property in the glTF object model and a NodePath in the Godot scene tree. |
| [`GodotClassNameAttribute`](classes/GodotClassNameAttribute.md) | class | Attribute that specifies the engine class name when it's not the same as the generated C# class name. |
| [`GodotInstance`](classes/GodotInstance.md) | class | GodotInstance represents a running Godot instance that is controlled from an outside codebase, without a perpetual main loop. |
| [`GodotObject`](classes/GodotObject.md) | class | An advanced [`Variant`](classes/Variant.md) type. |
| [`GodotSynchronizationContext`](classes/GodotSynchronizationContext.md) | class |  |
| [`GodotTaskScheduler`](classes/GodotTaskScheduler.md) | class | GodotTaskScheduler contains a linked list of tasks to perform as a queue. |
| [`GodotThread`](classes/GodotThread.md) | class | A unit of execution in a process. |
| [`HashingContext`](classes/HashingContext.md) | class | The HashingContext class provides an interface for computing cryptographic hashes over multiple iterations. |
| [`HmacContext`](classes/HmacContext.md) | class | The HMACContext class is useful for advanced HMAC use cases, such as streaming the message as it supports creating the message over time rather than providing it all at once. |
| [`HttpClient`](classes/HttpClient.md) | class | Hyper-text transfer protocol client (sometimes called "User Agent"). |
| [`IconAttribute`](classes/IconAttribute.md) | class | Specifies a custom icon for representing this class in the Godot Editor. |
| [`ImageFormatLoader`](classes/ImageFormatLoader.md) | class | The engine supports multiple image formats out of the box (PNG, SVG, JPEG, WebP to name a few), but you can choose to implement support for additional image formats by extending [`ImageFormatLoaderExtension`](ImageFormat… |
| [`ImageFormatLoaderExtension`](classes/ImageFormatLoaderExtension.md) | class | The engine supports multiple image formats out of the box (PNG, SVG, JPEG, WebP to name a few), but you can choose to implement support for additional image formats by extending this class. |
| [`InputInstance`](classes/InputInstance.md) | class | The [`Input`](classes/Input.md) singleton handles key presses, mouse buttons and movement, gamepads, and input actions. |
| [`InputMapInstance`](classes/InputMapInstance.md) | class | Manages all [`InputEventAction`](classes/InputEventAction.md) which can be created/modified from the project settings menu **Project > Project Settings > Input Map** or in code with `AddAction()` and `ActionAddEvent()`. |
| [`IntervalTweener`](classes/IntervalTweener.md) | class | `IntervalTweener` is used to make delays in a tweening sequence. |
| [`IPInstance`](classes/IPInstance.md) | class | IP contains support functions for the Internet Protocol (IP). |
| [`JavaClass`](classes/JavaClass.md) | class | Represents a class from the Java Native Interface. |
| [`JavaClassWrapperInstance`](classes/JavaClassWrapperInstance.md) | class | The JavaClassWrapper singleton provides a way for the Godot application to send and receive data through the [Java Native Interface](https://developer.android.com/training/articles/perf-jni) (JNI). |
| [`JavaObject`](classes/JavaObject.md) | class | Represents an object from the Java Native Interface. |
| [`JavaScriptBridgeInstance`](classes/JavaScriptBridgeInstance.md) | class | The JavaScriptBridge singleton is implemented only in the Web export. |
| [`JavaScriptObject`](classes/JavaScriptObject.md) | class | JavaScriptObject is used to interact with JavaScript objects retrieved or created via [`JavaScriptBridge.GetInterface`](classes/JavaScriptBridge.md), [`JavaScriptBridge.CreateObject`](classes/JavaScriptBridge.md), or [`JavaScriptBridge.… |
| [`JniSingleton`](classes/JniSingleton.md) | class | The JNISingleton is implemented only in the Android export. |
| [`JsonRpc`](classes/JsonRpc.md) | class | [JSON-RPC](https://www.jsonrpc.org/) is a standard which wraps a method call in a [`Json`](classes/Json.md) object. |
| [`KinematicCollision2D`](classes/KinematicCollision2D.md) | class | Holds collision data from the movement of a [`PhysicsBody2D`](classes/PhysicsBody2D.md), usually from [`PhysicsBody2D.MoveAndCollide`](classes/PhysicsBody2D.md). |
| [`KinematicCollision3D`](classes/KinematicCollision3D.md) | class | Holds collision data from the movement of a [`PhysicsBody3D`](classes/PhysicsBody3D.md), usually from [`PhysicsBody3D.MoveAndCollide`](classes/PhysicsBody3D.md). |
| [`Lightmapper`](classes/Lightmapper.md) | class | This class should be extended by custom lightmapper classes. |
| [`LightmapperRD`](classes/LightmapperRD.md) | class | LightmapperRD ("RD" stands for [`RenderingDevice`](classes/RenderingDevice.md)) is the built-in GPU-based lightmapper for use with [`LightmapGI`](classes/LightmapGI.md). |
| [`Logger`](classes/Logger.md) | class | Custom logger to receive messages from the internal error/warning stream. |
| [`MainLoop`](classes/MainLoop.md) | class | `MainLoop` is the abstract base class for a Godot project's game loop. |
| [`MarshallsInstance`](classes/MarshallsInstance.md) | class | Provides data transformation and encoding utility functions. |
| [`MeshConvexDecompositionSettings`](classes/MeshConvexDecompositionSettings.md) | class | Parameters to be used with a [`Mesh`](classes/Mesh.md) convex decomposition operation. |
| [`MeshDataTool`](classes/MeshDataTool.md) | class | MeshDataTool provides access to individual vertices in a [`Mesh`](classes/Mesh.md). |
| [`MethodTweener`](classes/MethodTweener.md) | class | `MethodTweener` is similar to a combination of [`CallbackTweener`](classes/CallbackTweener.md) and [`PropertyTweener`](classes/PropertyTweener.md). |
| [`MobileVRInterface`](classes/MobileVRInterface.md) | class | This is a generic mobile VR implementation where you need to provide details about the phone and HMD used. |
| [`MovieWriter`](classes/MovieWriter.md) | class | Godot can record videos with non-real-time simulation. |
| [`MultiplayerApi`](classes/MultiplayerApi.md) | class | Base class for high-level multiplayer API implementations. |
| [`MultiplayerApiExtension`](classes/MultiplayerApiExtension.md) | class | This class can be used to extend or replace the default [`MultiplayerApi`](classes/MultiplayerApi.md) implementation via script or extensions. |
| [`MultiplayerPeer`](classes/MultiplayerPeer.md) | class | Manages the connection with one or more remote peers acting as server or client and assigning unique IDs to each of them. |
| [`MultiplayerPeerExtension`](classes/MultiplayerPeerExtension.md) | class | This class is designed to be inherited from a GDExtension plugin to implement custom networking layers for the multiplayer API (such as WebRTC). |
| [`MustBeVariantAttribute`](classes/MustBeVariantAttribute.md) | class | Attribute that restricts generic type parameters to be only types that can be marshaled from/to a [`Variant`](classes/Variant.md). |
| [`Mutex`](classes/Mutex.md) | class | A synchronization mutex (mutual exclusion). |
| [`NativeMenuInstance`](classes/NativeMenuInstance.md) | class | [`NativeMenu`](classes/NativeMenu.md) handles low-level access to the OS native global menu bar and popup menus. |
| [`NavigationMeshGeneratorInstance`](classes/NavigationMeshGeneratorInstance.md) | class | This class is responsible for creating and clearing 3D navigation meshes used as [`NavigationMesh`](classes/NavigationMesh.md) resources inside [`NavigationRegion3D`](classes/NavigationRegion3D.md). |
| [`NavigationPathQueryParameters2D`](classes/NavigationPathQueryParameters2D.md) | class | By changing various properties of this object, such as the start and target position, you can configure path queries to the [`NavigationServer2D`](classes/NavigationServer2D.md). |
| [`NavigationPathQueryParameters3D`](classes/NavigationPathQueryParameters3D.md) | class | By changing various properties of this object, such as the start and target position, you can configure path queries to the [`NavigationServer3D`](classes/NavigationServer3D.md). |
| [`NavigationPathQueryResult2D`](classes/NavigationPathQueryResult2D.md) | class | This class stores the result of a 2D navigation path query from the [`NavigationServer2D`](classes/NavigationServer2D.md). |
| [`NavigationPathQueryResult3D`](classes/NavigationPathQueryResult3D.md) | class | This class stores the result of a 3D navigation path query from the [`NavigationServer3D`](classes/NavigationServer3D.md). |
| [`NavigationServer2DInstance`](classes/NavigationServer2DInstance.md) | class | NavigationServer2D is the server that handles navigation maps, regions and agents. |
| [`NavigationServer2DManagerInstance`](classes/NavigationServer2DManagerInstance.md) | class | [`NavigationServer2DManager`](classes/NavigationServer2DManager.md) is the API for registering [`NavigationServer2D`](classes/NavigationServer2D.md) implementations and setting the default implementation. |
| [`NavigationServer3DInstance`](classes/NavigationServer3DInstance.md) | class | NavigationServer3D is the server that handles navigation maps, regions and agents. |
| [`NavigationServer3DManagerInstance`](classes/NavigationServer3DManagerInstance.md) | class | [`NavigationServer3DManager`](classes/NavigationServer3DManager.md) is the API for registering [`NavigationServer3D`](classes/NavigationServer3D.md) implementations and setting the default implementation. |
| [`Node3DGizmo`](classes/Node3DGizmo.md) | class | This abstract class helps connect the [`Node3D`](classes/Node3D.md) scene with the editor-specific `EditorNode3DGizmo` class. |
| [`NodePath`](classes/NodePath.md) | class | A pre-parsed relative or absolute path in a scene tree, for use with [`Node.GetNode`](classes/Node.md) and similar functions. |
| [`OfflineMultiplayerPeer`](classes/OfflineMultiplayerPeer.md) | class | This is the default [`MultiplayerApi.MultiplayerPeer`](classes/MultiplayerApi.md) for the [`Node.Multiplayer`](classes/Node.md). |
| [`OggPacketSequencePlayback`](classes/OggPacketSequencePlayback.md) | class |  |
| [`OpenXRAnchorTracker`](classes/OpenXRAnchorTracker.md) | class | Positional tracker for our OpenXR spatial entity anchor extension, it tracks a user defined location in real space and maps it to our virtual space. |
| [`OpenXRAndroidThreadSettingsExtension`](classes/OpenXRAndroidThreadSettingsExtension.md) | class | For XR to be comfortable, it is important for applications to deliver frames quickly and consistently. |
| [`OpenXRApiExtension`](classes/OpenXRApiExtension.md) | class | `OpenXRApiExtension` makes OpenXR available for GDExtension. |
| [`OpenXRExtensionWrapper`](classes/OpenXRExtensionWrapper.md) | class | `OpenXRExtensionWrapper` allows implementing OpenXR extensions with GDExtension. |
| [`OpenXRExtensionWrapperExtension`](classes/OpenXRExtensionWrapperExtension.md) | class | `OpenXRExtensionWrapperExtension` allows implementing OpenXR extensions with GDExtension. |
| [`OpenXRFrameSynthesisExtension`](classes/OpenXRFrameSynthesisExtension.md) | class | This class implements the [OpenXR Frame synthesis extension](https://registry.khronos.org/OpenXR/specs/1.1/html/xrspec.html#XR_EXT_frame_synthesis). |
| [`OpenXRFutureExtension`](classes/OpenXRFutureExtension.md) | class | This is a support extension in OpenXR that allows other OpenXR extensions to start asynchronous functions and get a callback after this function finishes. |
| [`OpenXRFutureResult`](classes/OpenXRFutureResult.md) | class | Result object tracking the asynchronous result of an OpenXR Future object, you can use this object to track the result status. |
| [`OpenXRInteractionProfileMetadata`](classes/OpenXRInteractionProfileMetadata.md) | class | This class allows OpenXR core and extensions to register metadata relating to supported interaction devices such as controllers, trackers, haptic devices, etc. |
| [`OpenXRInterface`](classes/OpenXRInterface.md) | class | The OpenXR interface allows Godot to interact with OpenXR runtimes and make it possible to create XR experiences and games. |
| [`OpenXRMarkerTracker`](classes/OpenXRMarkerTracker.md) | class | Spatial entity tracker for our OpenXR spatial entity marker tracking extension. |
| [`OpenXRPlaneTracker`](classes/OpenXRPlaneTracker.md) | class | Spatial entity tracker for our OpenXR spatial entity plane tracking extension. |
| [`OpenXRRenderModelExtension`](classes/OpenXRRenderModelExtension.md) | class | This class implements the OpenXR Render Model Extension, if enabled it will maintain a list of active render models and provides an interface to the render model data. |
| [`OpenXRSpatialAnchorCapability`](classes/OpenXRSpatialAnchorCapability.md) | class | This is an internal class that handles the OpenXR anchor spatial entity extension. |
| [`OpenXRSpatialCapabilityConfigurationAnchor`](classes/OpenXRSpatialCapabilityConfigurationAnchor.md) | class | Configuration header for spatial anchors. |
| [`OpenXRSpatialCapabilityConfigurationAprilTag`](classes/OpenXRSpatialCapabilityConfigurationAprilTag.md) | class | Configuration header for April tag markers. |
| [`OpenXRSpatialCapabilityConfigurationAruco`](classes/OpenXRSpatialCapabilityConfigurationAruco.md) | class | Configuration header for Aruco markers. |
| [`OpenXRSpatialCapabilityConfigurationBaseHeader`](classes/OpenXRSpatialCapabilityConfigurationBaseHeader.md) | class | Wrapper base class for OpenXR Spatial Capability Configuration headers. |
| [`OpenXRSpatialCapabilityConfigurationMicroQrCode`](classes/OpenXRSpatialCapabilityConfigurationMicroQrCode.md) | class | Configuration header for QR code markers. |
| [`OpenXRSpatialCapabilityConfigurationPlaneTracking`](classes/OpenXRSpatialCapabilityConfigurationPlaneTracking.md) | class | Configuration header for plane tracking. |
| [`OpenXRSpatialCapabilityConfigurationQrCode`](classes/OpenXRSpatialCapabilityConfigurationQrCode.md) | class | Configuration header for micro QR code markers. |
| [`OpenXRSpatialComponentAnchorList`](classes/OpenXRSpatialComponentAnchorList.md) | class | Object for storing the queries anchor result data when calling [`OpenXRSpatialEntityExtension.QuerySnapshot`](classes/OpenXRSpatialEntityExtension.md). |
| [`OpenXRSpatialComponentBounded2DList`](classes/OpenXRSpatialComponentBounded2DList.md) | class | Object for storing the queries 2D bounding rectangle result data when calling [`OpenXRSpatialEntityExtension.QuerySnapshot`](classes/OpenXRSpatialEntityExtension.md). |
| [`OpenXRSpatialComponentBounded3DList`](classes/OpenXRSpatialComponentBounded3DList.md) | class | Object for storing the queries 3d bounding box result data when calling [`OpenXRSpatialEntityExtension.QuerySnapshot`](classes/OpenXRSpatialEntityExtension.md). |
| [`OpenXRSpatialComponentData`](classes/OpenXRSpatialComponentData.md) | class | Object for storing OpenXR spatial entity component data. |
| [`OpenXRSpatialComponentMarkerList`](classes/OpenXRSpatialComponentMarkerList.md) | class | Object for storing the queries marker result data when calling [`OpenXRSpatialEntityExtension.QuerySnapshot`](classes/OpenXRSpatialEntityExtension.md). |
| [`OpenXRSpatialComponentMesh2DList`](classes/OpenXRSpatialComponentMesh2DList.md) | class | Object for storing the queries 2D mesh result data when calling [`OpenXRSpatialEntityExtension.QuerySnapshot`](classes/OpenXRSpatialEntityExtension.md). |
| [`OpenXRSpatialComponentMesh3DList`](classes/OpenXRSpatialComponentMesh3DList.md) | class | Object for storing the queries 3d mesh result data when calling [`OpenXRSpatialEntityExtension.QuerySnapshot`](classes/OpenXRSpatialEntityExtension.md). |
| [`OpenXRSpatialComponentParentList`](classes/OpenXRSpatialComponentParentList.md) | class | Object for storing the queries parent result data when calling [`OpenXRSpatialEntityExtension.QuerySnapshot`](classes/OpenXRSpatialEntityExtension.md). |
| [`OpenXRSpatialComponentPersistenceList`](classes/OpenXRSpatialComponentPersistenceList.md) | class | Object for storing the query persistence result data when calling [`OpenXRSpatialEntityExtension.QuerySnapshot`](classes/OpenXRSpatialEntityExtension.md). |
| [`OpenXRSpatialComponentPlaneAlignmentList`](classes/OpenXRSpatialComponentPlaneAlignmentList.md) | class | Object for storing the queries plane alignment result data when calling [`OpenXRSpatialEntityExtension.QuerySnapshot`](classes/OpenXRSpatialEntityExtension.md). |
| [`OpenXRSpatialComponentPlaneSemanticLabelList`](classes/OpenXRSpatialComponentPlaneSemanticLabelList.md) | class | Object for storing the queries plane semantic label result data when calling [`OpenXRSpatialEntityExtension.QuerySnapshot`](classes/OpenXRSpatialEntityExtension.md). |
| [`OpenXRSpatialComponentPolygon2DList`](classes/OpenXRSpatialComponentPolygon2DList.md) | class | Object for storing the queries 2D polygon result data when calling [`OpenXRSpatialEntityExtension.QuerySnapshot`](classes/OpenXRSpatialEntityExtension.md). |
| [`OpenXRSpatialContextPersistenceConfig`](classes/OpenXRSpatialContextPersistenceConfig.md) | class | Configuration header for spatial persistence. |
| [`OpenXRSpatialEntityExtension`](classes/OpenXRSpatialEntityExtension.md) | class | OpenXR extension that handles spatial entities and, when enabled, allows querying those spatial entities. |
| [`OpenXRSpatialEntityTracker`](classes/OpenXRSpatialEntityTracker.md) | class | These are trackers created and managed by OpenXR's spatial entity extensions that give access to specific data related to OpenXR's spatial entities. |
| [`OpenXRSpatialMarkerTrackingCapability`](classes/OpenXRSpatialMarkerTrackingCapability.md) | class | This class handles the OpenXR marker tracking spatial entity extension. |
| [`OpenXRSpatialPlaneTrackingCapability`](classes/OpenXRSpatialPlaneTrackingCapability.md) | class | This class handles the OpenXR plane tracking spatial entity extension. |
| [`OpenXRSpatialQueryResultData`](classes/OpenXRSpatialQueryResultData.md) | class | Object for storing the main query result data when calling [`OpenXRSpatialEntityExtension.QuerySnapshot`](classes/OpenXRSpatialEntityExtension.md). |
| [`OpenXRStructureBase`](classes/OpenXRStructureBase.md) | class | Object for storing OpenXR structure data that is passed when calling into OpenXR APIs. |
| [`OSInstance`](classes/OSInstance.md) | class | The [`OS`](classes/OS.md) class wraps the most common functionalities for communicating with the host operating system, such as the video driver, delays, environment variables, execution of binaries, command line, etc. |
| [`PackedDataContainerRef`](classes/PackedDataContainerRef.md) | class | When packing nested containers using [`PackedDataContainer`](classes/PackedDataContainer.md), they are recursively packed into `PackedDataContainerRef` (only applies to [`Collections.Array`](classes/Collections.Array.md) and [`Collectio… |
| [`PacketPeer`](classes/PacketPeer.md) | class | PacketPeer is an abstraction and base class for packet-based protocols (such as UDP). |
| [`PacketPeerDtls`](classes/PacketPeerDtls.md) | class | This class represents a DTLS peer connection. |
| [`PacketPeerExtension`](classes/PacketPeerExtension.md) | class |  |
| [`PacketPeerStream`](classes/PacketPeerStream.md) | class | PacketStreamPeer provides a wrapper for working using packets over a stream. |
| [`PacketPeerUdp`](classes/PacketPeerUdp.md) | class | UDP packet peer. |
| [`PckPacker`](classes/PckPacker.md) | class | The `PckPacker` is used to create packages that can be loaded into a running project using [`ProjectSettings.LoadResourcePack`](classes/ProjectSettings.md). |
| [`PerformanceInstance`](classes/PerformanceInstance.md) | class | This class provides access to a number of different monitors related to performance, such as memory usage, draw calls, and FPS. |
| [`PhysicsDirectBodyState2D`](classes/PhysicsDirectBodyState2D.md) | class | Provides direct access to a physics body in the [`PhysicsServer2D`](classes/PhysicsServer2D.md), allowing safe changes to physics properties. |
| [`PhysicsDirectBodyState2DExtension`](classes/PhysicsDirectBodyState2DExtension.md) | class | This class extends [`PhysicsDirectBodyState2D`](classes/PhysicsDirectBodyState2D.md) by providing additional virtual methods that can be overridden. |
| [`PhysicsDirectBodyState3D`](classes/PhysicsDirectBodyState3D.md) | class | Provides direct access to a physics body in the [`PhysicsServer3D`](classes/PhysicsServer3D.md), allowing safe changes to physics properties. |
| [`PhysicsDirectBodyState3DExtension`](classes/PhysicsDirectBodyState3DExtension.md) | class | This class extends [`PhysicsDirectBodyState3D`](classes/PhysicsDirectBodyState3D.md) by providing additional virtual methods that can be overridden. |
| [`PhysicsDirectSpaceState2D`](classes/PhysicsDirectSpaceState2D.md) | class | Provides direct access to a physics space in the [`PhysicsServer2D`](classes/PhysicsServer2D.md). |
| [`PhysicsDirectSpaceState2DExtension`](classes/PhysicsDirectSpaceState2DExtension.md) | class | This class extends [`PhysicsDirectSpaceState2D`](classes/PhysicsDirectSpaceState2D.md) by providing additional virtual methods that can be overridden. |
| [`PhysicsDirectSpaceState3D`](classes/PhysicsDirectSpaceState3D.md) | class | Provides direct access to a physics space in the [`PhysicsServer3D`](classes/PhysicsServer3D.md). |
| [`PhysicsDirectSpaceState3DExtension`](classes/PhysicsDirectSpaceState3DExtension.md) | class | This class extends [`PhysicsDirectSpaceState3D`](classes/PhysicsDirectSpaceState3D.md) by providing additional virtual methods that can be overridden. |
| [`PhysicsPointQueryParameters2D`](classes/PhysicsPointQueryParameters2D.md) | class | By changing various properties of this object, such as the point position, you can configure the parameters for [`PhysicsDirectSpaceState2D.IntersectPoint`](classes/PhysicsDirectSpaceState2D.md). |
| [`PhysicsPointQueryParameters3D`](classes/PhysicsPointQueryParameters3D.md) | class | By changing various properties of this object, such as the point position, you can configure the parameters for [`PhysicsDirectSpaceState3D.IntersectPoint`](classes/PhysicsDirectSpaceState3D.md). |
| [`PhysicsRayQueryParameters2D`](classes/PhysicsRayQueryParameters2D.md) | class | By changing various properties of this object, such as the ray position, you can configure the parameters for [`PhysicsDirectSpaceState2D.IntersectRay`](classes/PhysicsDirectSpaceState2D.md). |
| [`PhysicsRayQueryParameters3D`](classes/PhysicsRayQueryParameters3D.md) | class | By changing various properties of this object, such as the ray position, you can configure the parameters for [`PhysicsDirectSpaceState3D.IntersectRay`](classes/PhysicsDirectSpaceState3D.md). |
| [`PhysicsServer2DExtension`](classes/PhysicsServer2DExtension.md) | class | This class extends [`PhysicsServer2D`](classes/PhysicsServer2D.md) by providing additional virtual methods that can be overridden. |
| [`PhysicsServer2DInstance`](classes/PhysicsServer2DInstance.md) | class | PhysicsServer2D is the server responsible for all 2D physics. |
| [`PhysicsServer2DManagerInstance`](classes/PhysicsServer2DManagerInstance.md) | class | [`PhysicsServer2DManager`](classes/PhysicsServer2DManager.md) is the API for registering [`PhysicsServer2D`](classes/PhysicsServer2D.md) implementations and for setting the default implementation. |
| [`PhysicsServer3DExtension`](classes/PhysicsServer3DExtension.md) | class | This class extends [`PhysicsServer3D`](classes/PhysicsServer3D.md) by providing additional virtual methods that can be overridden. |
| [`PhysicsServer3DInstance`](classes/PhysicsServer3DInstance.md) | class | PhysicsServer3D is the server responsible for all 3D physics. |
| [`PhysicsServer3DManagerInstance`](classes/PhysicsServer3DManagerInstance.md) | class | [`PhysicsServer3DManager`](classes/PhysicsServer3DManager.md) is the API for registering [`PhysicsServer3D`](classes/PhysicsServer3D.md) implementations and for setting the default implementation. |
| [`PhysicsServer3DRenderingServerHandler`](classes/PhysicsServer3DRenderingServerHandler.md) | class | A class used to provide [method PhysicsServer3DExtension._soft_body_update_rendering_server] with a rendering handler for soft bodies. |
| [`PhysicsShapeQueryParameters2D`](classes/PhysicsShapeQueryParameters2D.md) | class | By changing various properties of this object, such as the shape, you can configure the parameters for [`PhysicsDirectSpaceState2D`](classes/PhysicsDirectSpaceState2D.md)'s methods. |
| [`PhysicsShapeQueryParameters3D`](classes/PhysicsShapeQueryParameters3D.md) | class | By changing various properties of this object, such as the shape, you can configure the parameters for [`PhysicsDirectSpaceState3D`](classes/PhysicsDirectSpaceState3D.md)'s methods. |
| [`PhysicsTestMotionParameters2D`](classes/PhysicsTestMotionParameters2D.md) | class | By changing various properties of this object, such as the motion, you can configure the parameters for [`PhysicsServer2D.BodyTestMotion`](classes/PhysicsServer2D.md). |
| [`PhysicsTestMotionParameters3D`](classes/PhysicsTestMotionParameters3D.md) | class | By changing various properties of this object, such as the motion, you can configure the parameters for [`PhysicsServer3D.BodyTestMotion`](classes/PhysicsServer3D.md). |
| [`PhysicsTestMotionResult2D`](classes/PhysicsTestMotionResult2D.md) | class | Describes the motion and collision result from [`PhysicsServer2D.BodyTestMotion`](classes/PhysicsServer2D.md). |
| [`PhysicsTestMotionResult3D`](classes/PhysicsTestMotionResult3D.md) | class | Describes the motion and collision result from [`PhysicsServer3D.BodyTestMotion`](classes/PhysicsServer3D.md). |
| [`ProjectSettingsInstance`](classes/ProjectSettingsInstance.md) | class | Stores variables that can be accessed from everywhere. |
| [`PropertyTweener`](classes/PropertyTweener.md) | class | `PropertyTweener` is used to interpolate a property in an object. |
| [`RandomNumberGenerator`](classes/RandomNumberGenerator.md) | class | RandomNumberGenerator is a class for generating pseudo-random numbers. |
| [`RDAccelerationStructureGeometry`](classes/RDAccelerationStructureGeometry.md) | class | `RDAccelerationStructureGeometry` describes a set of triangles used as raytracing geometry in the [`RenderingDevice.BlasCreate`](classes/RenderingDevice.md) method. |
| [`RDAccelerationStructureInstance`](classes/RDAccelerationStructureInstance.md) | class | `RDAccelerationStructureInstance` describes an instance of a Bottom-Level Acceleration Structure (BLAS) used in the [`RenderingDevice.TlasBuild`](classes/RenderingDevice.md) method. |
| [`RDAttachmentFormat`](classes/RDAttachmentFormat.md) | class | This object is used by [`RenderingDevice`](classes/RenderingDevice.md). |
| [`RDFramebufferPass`](classes/RDFramebufferPass.md) | class | This class contains the list of attachment descriptions for a framebuffer pass. |
| [`RDHitGroup`](classes/RDHitGroup.md) | class | Defines a hit group for use with [`RenderingDevice.RaytracingPipelineCreate`](classes/RenderingDevice.md). |
| [`RDPipelineColorBlendState`](classes/RDPipelineColorBlendState.md) | class | This object is used by [`RenderingDevice`](classes/RenderingDevice.md). |
| [`RDPipelineColorBlendStateAttachment`](classes/RDPipelineColorBlendStateAttachment.md) | class | Controls how blending between source and destination fragments is performed when using [`RenderingDevice`](classes/RenderingDevice.md). |
| [`RDPipelineDepthStencilState`](classes/RDPipelineDepthStencilState.md) | class | `RDPipelineDepthStencilState` controls the way depth and stencil comparisons are performed when sampling those values using [`RenderingDevice`](classes/RenderingDevice.md). |
| [`RDPipelineMultisampleState`](classes/RDPipelineMultisampleState.md) | class | `RDPipelineMultisampleState` is used to control how multisample or supersample antialiasing is being performed when rendering using [`RenderingDevice`](classes/RenderingDevice.md). |
| [`RDPipelineRasterizationState`](classes/RDPipelineRasterizationState.md) | class | This object is used by [`RenderingDevice`](classes/RenderingDevice.md). |
| [`RDPipelineShader`](classes/RDPipelineShader.md) | class | Wraps a shader resource and allows specialization constants to be applied at pipeline creation time. |
| [`RDPipelineSpecializationConstant`](classes/RDPipelineSpecializationConstant.md) | class | A *specialization constant* is a way to create additional variants of shaders without actually increasing the number of shader versions that are compiled. |
| [`RDSamplerState`](classes/RDSamplerState.md) | class | This object is used by [`RenderingDevice`](classes/RenderingDevice.md). |
| [`RDShaderSource`](classes/RDShaderSource.md) | class | Shader source code in text form. |
| [`RDTextureFormat`](classes/RDTextureFormat.md) | class | This object is used by [`RenderingDevice`](classes/RenderingDevice.md). |
| [`RDTextureView`](classes/RDTextureView.md) | class | This object is used by [`RenderingDevice`](classes/RenderingDevice.md). |
| [`RDUniform`](classes/RDUniform.md) | class | This object is used by [`RenderingDevice`](classes/RenderingDevice.md). |
| [`RDVertexAttribute`](classes/RDVertexAttribute.md) | class | This object is used by [`RenderingDevice`](classes/RenderingDevice.md). |
| [`RefCounted`](classes/RefCounted.md) | class | Base class for any object that keeps a reference count. |
| [`RegEx`](classes/RegEx.md) | class | A regular expression (or regex) is a compact language that can be used to recognize strings that follow a specific pattern, such as URLs, email addresses, complete sentences, etc. |
| [`RegExMatch`](classes/RegExMatch.md) | class | Contains the results of a single [`RegEx`](classes/RegEx.md) match returned by [`RegEx.Search`](classes/RegEx.md) and [`RegEx.SearchAll`](classes/RegEx.md). |
| [`RenderData`](classes/RenderData.md) | class | Abstract render data object, exists for the duration of rendering a single viewport. |
| [`RenderDataExtension`](classes/RenderDataExtension.md) | class | This class allows for a RenderData implementation to be made in GDExtension. |
| [`RenderDataRD`](classes/RenderDataRD.md) | class | This object manages all render data for the [`RenderingDevice`](classes/RenderingDevice.md)-based renderers. |
| [`RenderingDevice`](classes/RenderingDevice.md) | class | `RenderingDevice` is an abstraction for working with modern low-level graphics APIs such as Vulkan. |
| [`RenderingServerInstance`](classes/RenderingServerInstance.md) | class | The rendering server is the API backend for everything visible. |
| [`RenderSceneBuffers`](classes/RenderSceneBuffers.md) | class | Abstract scene buffers object, created for each viewport for which 3D rendering is done. |
| [`RenderSceneBuffersConfiguration`](classes/RenderSceneBuffersConfiguration.md) | class | This configuration object is created and populated by the render engine on a viewport change and used to (re)configure a [`RenderSceneBuffers`](classes/RenderSceneBuffers.md) object. |
| [`RenderSceneBuffersExtension`](classes/RenderSceneBuffersExtension.md) | class | This class allows for a RenderSceneBuffer implementation to be made in GDExtension. |
| [`RenderSceneBuffersRD`](classes/RenderSceneBuffersRD.md) | class | This object manages all 3D rendering buffers for the rendering device based renderers. |
| [`RenderSceneData`](classes/RenderSceneData.md) | class | Abstract scene data object, exists for the duration of rendering a single viewport. |
| [`RenderSceneDataExtension`](classes/RenderSceneDataExtension.md) | class | This class allows for a RenderSceneData implementation to be made in GDExtension. |
| [`RenderSceneDataRD`](classes/RenderSceneDataRD.md) | class | Object holds scene data related to rendering a single frame of a viewport. |
| [`ResourceFormatLoader`](classes/ResourceFormatLoader.md) | class | Godot loads resources in the editor or in exported games using ResourceFormatLoaders. |
| [`ResourceFormatSaver`](classes/ResourceFormatSaver.md) | class | The engine can save resources when you do it from the editor, or when you use the [`ResourceSaver`](classes/ResourceSaver.md) singleton. |
| [`ResourceImporter`](classes/ResourceImporter.md) | class | This is the base class for Godot's resource importers. |
| [`ResourceLoaderInstance`](classes/ResourceLoaderInstance.md) | class | A singleton used to load resource files from the filesystem. |
| [`ResourceSaverInstance`](classes/ResourceSaverInstance.md) | class | A singleton for saving resource types to the filesystem. |
| [`ResourceUidInstance`](classes/ResourceUidInstance.md) | class | Resource UIDs (Unique IDentifiers) allow the engine to keep references between resources intact, even if files are renamed or moved. |
| [`RpcAttribute`](classes/RpcAttribute.md) | class | Attribute that changes the RPC mode for the annotated `method` to the given `Mode`, optionally specifying the `TransferMode` and `TransferChannel` (on supported peers). |
| [`SceneMultiplayer`](classes/SceneMultiplayer.md) | class | This class is the default implementation of [`MultiplayerApi`](classes/MultiplayerApi.md), used to provide multiplayer functionalities in Godot Engine. |
| [`SceneState`](classes/SceneState.md) | class | Maintains a list of resources, nodes, exported and overridden properties, and built-in scripts associated with a scene. |
| [`SceneTree`](classes/SceneTree.md) | class | As one of the most important classes, the `SceneTree` manages the hierarchy of nodes in a scene, as well as scenes themselves. |
| [`SceneTreeTimer`](classes/SceneTreeTimer.md) | class | A one-shot timer managed by the scene tree, which emits `Timeout` on completion. |
| [`ScriptBacktrace`](classes/ScriptBacktrace.md) | class | `ScriptBacktrace` holds an already captured backtrace of a specific script language, such as GDScript or C#, which are captured using [`Engine.CaptureScriptBacktraces`](classes/Engine.md). |
| [`ScriptLanguage`](classes/ScriptLanguage.md) | class |  |
| [`ScriptLanguageExtension`](classes/ScriptLanguageExtension.md) | class |  |
| [`ScriptPathAttribute`](classes/ScriptPathAttribute.md) | class | An attribute that contains the path to the object's script. |
| [`Semaphore`](classes/Semaphore.md) | class | A synchronization semaphore that can be used to synchronize multiple [`GodotThread`](classes/GodotThread.md)s. |
| [`ShaderIncludeDB`](classes/ShaderIncludeDB.md) | class | This object contains shader fragments from Godot's internal shaders. |
| [`SignalAttribute`](classes/SignalAttribute.md) | class | Declares a `delegate` as a signal. |
| [`SignalAwaiter`](classes/SignalAwaiter.md) | class |  |
| [`SkinReference`](classes/SkinReference.md) | class | An internal object containing a mapping from a [`Skin`](classes/Skin.md) used within the context of a particular [`MeshInstance3D`](classes/MeshInstance3D.md) to refer to the skeleton's [`Rid`](classes/Rid.md) in the RenderingServer. |
| [`SocketServer`](classes/SocketServer.md) | class | A socket server. |
| [`StreamPeer`](classes/StreamPeer.md) | class | StreamPeer is an abstract base class mostly used for stream-based protocols (such as TCP). |
| [`StreamPeerBuffer`](classes/StreamPeerBuffer.md) | class | A data buffer stream peer that uses a byte array as the stream. |
| [`StreamPeerExtension`](classes/StreamPeerExtension.md) | class |  |
| [`StreamPeerGZip`](classes/StreamPeerGZip.md) | class | This class allows to compress or decompress data using GZIP/deflate in a streaming fashion. |
| [`StreamPeerSocket`](classes/StreamPeerSocket.md) | class | StreamPeerSocket is an abstract base class that defines common behavior for socket-based streams. |
| [`StreamPeerTcp`](classes/StreamPeerTcp.md) | class | A stream peer that handles TCP connections. |
| [`StreamPeerTls`](classes/StreamPeerTls.md) | class | A stream peer that handles TLS connections. |
| [`StreamPeerUds`](classes/StreamPeerUds.md) | class | A stream peer that handles UNIX Domain Socket (UDS) connections. |
| [`StringName`](classes/StringName.md) | class | StringNames are immutable strings designed for general-purpose representation of unique names. |
| [`SubtweenTweener`](classes/SubtweenTweener.md) | class | `SubtweenTweener` is used to execute a [`Tween`](classes/Tween.md) as one step in a sequence defined by another [`Tween`](classes/Tween.md). |
| [`SurfaceTool`](classes/SurfaceTool.md) | class | The `SurfaceTool` is used to construct a [`Mesh`](classes/Mesh.md) by specifying vertex attributes individually. |
| [`TcpServer`](classes/TcpServer.md) | class | A TCP server. |
| [`TextLine`](classes/TextLine.md) | class | Abstraction over [`TextServer`](classes/TextServer.md) for handling a single line of text. |
| [`TextParagraph`](classes/TextParagraph.md) | class | Abstraction over [`TextServer`](classes/TextServer.md) for handling a single paragraph of text. |
| [`TextServer`](classes/TextServer.md) | class | `TextServer` is the API backend for managing fonts and rendering text. |
| [`TextServerAdvanced`](classes/TextServerAdvanced.md) | class | An implementation of [`TextServer`](classes/TextServer.md) that uses HarfBuzz, ICU and SIL Graphite to support BiDi, complex text layouts and contextual OpenType features. |
| [`TextServerDummy`](classes/TextServerDummy.md) | class | A dummy [`TextServer`](classes/TextServer.md) interface that doesn't do anything. |
| [`TextServerExtension`](classes/TextServerExtension.md) | class | External [`TextServer`](classes/TextServer.md) implementations should inherit from this class. |
| [`TextServerManagerInstance`](classes/TextServerManagerInstance.md) | class | [`TextServerManager`](classes/TextServerManager.md) is the API backend for loading, enumerating, and switching [`TextServer`](classes/TextServer.md)s. |
| [`ThemeDBInstance`](classes/ThemeDBInstance.md) | class | This singleton provides access to static information about [`Theme`](classes/Theme.md) resources used by the engine and by your projects. |
| [`TileData`](classes/TileData.md) | class | `TileData` object represents a single tile in a [`TileSet`](classes/TileSet.md). |
| [`TimeInstance`](classes/TimeInstance.md) | class | The Time singleton allows converting time between various formats and also getting time information from the system. |
| [`TlsOptions`](classes/TlsOptions.md) | class | TLSOptions abstracts the configuration options for the [`StreamPeerTls`](classes/StreamPeerTls.md) and [`PacketPeerDtls`](classes/PacketPeerDtls.md) classes. |
| [`ToolAttribute`](classes/ToolAttribute.md) | class | Allows the annotated class to execute in the editor. |
| [`TranslationDomain`](classes/TranslationDomain.md) | class | `TranslationDomain` is a self-contained collection of [`Translation`](classes/Translation.md) resources. |
| [`TranslationServerInstance`](classes/TranslationServerInstance.md) | class | The translation server is the API backend that manages all language translations. |
| [`TreeItem`](classes/TreeItem.md) | class | A single item of a [`Tree`](classes/Tree.md) control. |
| [`TriangleMesh`](classes/TriangleMesh.md) | class | Creates a bounding volume hierarchy (BVH) tree structure around triangle geometry. |
| [`Tween`](classes/Tween.md) | class | Tweens are mostly useful for animations requiring a numerical property to be interpolated over a range of values. |
| [`Tweener`](classes/Tweener.md) | class | Tweeners are objects that perform a specific animating task, e.g. |
| [`UdpServer`](classes/UdpServer.md) | class | A simple server that opens a UDP socket and returns connected [`PacketPeerUdp`](classes/PacketPeerUdp.md) upon receiving new packets. |
| [`UdsServer`](classes/UdsServer.md) | class | A Unix Domain Socket (UDS) server. |
| [`UndoRedo`](classes/UndoRedo.md) | class | UndoRedo works by registering methods and property changes inside "actions". |
| [`UniformSetCacheRD`](classes/UniformSetCacheRD.md) | class | Uniform set cache manager for [`RenderingDevice`](classes/RenderingDevice.md)-based renderers. |
| [`Upnp`](classes/Upnp.md) | class | This class can be used to discover compatible [`UpnpDevice`](classes/UpnpDevice.md)s on the local network and execute commands on them, like managing port mappings (for port forwarding/NAT traversal) and querying the local and r… |
| [`UpnpDevice`](classes/UpnpDevice.md) | class | Universal Plug and Play (UPnP) device. |
| [`WeakRef`](classes/WeakRef.md) | class | A weakref can hold a [`RefCounted`](classes/RefCounted.md) without contributing to the reference counter. |
| [`WebRtcDataChannel`](classes/WebRtcDataChannel.md) | class |  |
| [`WebRtcDataChannelExtension`](classes/WebRtcDataChannelExtension.md) | class |  |
| [`WebRtcMultiplayerPeer`](classes/WebRtcMultiplayerPeer.md) | class | This class constructs a full mesh of [`WebRtcPeerConnection`](classes/WebRtcPeerConnection.md) (one connection for each peer) that can be used as a [`MultiplayerApi.MultiplayerPeer`](classes/MultiplayerApi.md). |
| [`WebRtcPeerConnection`](classes/WebRtcPeerConnection.md) | class | A WebRTC connection between the local computer and a remote peer. |
| [`WebRtcPeerConnectionExtension`](classes/WebRtcPeerConnectionExtension.md) | class |  |
| [`WebSocketMultiplayerPeer`](classes/WebSocketMultiplayerPeer.md) | class | Base class for WebSocket server and client, allowing them to be used as multiplayer peer for the [`MultiplayerApi`](classes/MultiplayerApi.md). |
| [`WebSocketPeer`](classes/WebSocketPeer.md) | class | This class represents WebSocket connection, and can be used as a WebSocket client ([RFC 6455](https://datatracker.ietf.org/doc/html/rfc6455)-compliant) or as a remote peer of a WebSocket server. |
| [`WebXRInterface`](classes/WebXRInterface.md) | class | WebXR is an open standard that allows creating VR and AR applications that run in the web browser. |
| [`WorkerThreadPoolInstance`](classes/WorkerThreadPoolInstance.md) | class | The [`WorkerThreadPool`](classes/WorkerThreadPool.md) singleton allocates a set of [`GodotThread`](classes/GodotThread.md)s (called worker threads) on project startup and provides methods for offloading tasks to them. |
| [`XmlParser`](classes/XmlParser.md) | class | Provides a low-level interface for creating parsers for [XML](https://en.wikipedia.org/wiki/XML) files. |
| [`XRBodyTracker`](classes/XRBodyTracker.md) | class | A body tracking system will create an instance of this object and add it to the [`XRServer`](classes/XRServer.md). |
| [`XRControllerTracker`](classes/XRControllerTracker.md) | class | An instance of this object represents a controller that is tracked. |
| [`XRFaceTracker`](classes/XRFaceTracker.md) | class | An instance of this object represents a tracked face and its corresponding blend shapes. |
| [`XRHandTracker`](classes/XRHandTracker.md) | class | A hand tracking system will create an instance of this object and add it to the [`XRServer`](classes/XRServer.md). |
| [`XRInterface`](classes/XRInterface.md) | class | This class needs to be implemented to make an AR or VR platform available to Godot and these should be implemented as C++ modules or GDExtension modules. |
| [`XRInterfaceExtension`](classes/XRInterfaceExtension.md) | class | External XR interface plugins should inherit from this class. |
| [`XRPose`](classes/XRPose.md) | class | XR runtimes often identify multiple locations on devices such as controllers that are spatially tracked. |
| [`XRPositionalTracker`](classes/XRPositionalTracker.md) | class | An instance of this object represents a device that is tracked, such as a controller or anchor point. |
| [`XRServerInstance`](classes/XRServerInstance.md) | class | The AR/VR server is the heart of our Advanced and Virtual Reality solution and handles all the processing. |
| [`XRTracker`](classes/XRTracker.md) | class | This object is the base of all XR trackers. |
| [`Xrvrs`](classes/Xrvrs.md) | class | This class is used by various XR interfaces to generate VRS textures that can be used to speed up rendering. |
| [`ZipPacker`](classes/ZipPacker.md) | class | This class implements a writer that allows storing the multiple blobs in a ZIP archive. |
| [`ZipReader`](classes/ZipReader.md) | class | This class implements a reader that can extract the content of individual files inside a ZIP archive. |

## 全局枚举（20）

| 类型 | 种类 | 简介 |
| --- | --- | --- |
| [`ClockDirection`](classes/ClockDirection.md) | enum |  |
| [`Corner`](classes/Corner.md) | enum |  |
| [`Error`](classes/Error.md) | enum |  |
| [`EulerOrder`](classes/EulerOrder.md) | enum |  |
| [`HorizontalAlignment`](classes/HorizontalAlignment.md) | enum |  |
| [`InlineAlignment`](classes/InlineAlignment.md) | enum |  |
| [`JoyAxis`](classes/JoyAxis.md) | enum |  |
| [`JoyButton`](classes/JoyButton.md) | enum |  |
| [`Key`](classes/Key.md) | enum |  |
| [`KeyLocation`](classes/KeyLocation.md) | enum |  |
| [`KeyModifierMask`](classes/KeyModifierMask.md) | enum |  |
| [`MethodFlags`](classes/MethodFlags.md) | enum |  |
| [`MidiMessage`](classes/MidiMessage.md) | enum |  |
| [`MouseButton`](classes/MouseButton.md) | enum |  |
| [`MouseButtonMask`](classes/MouseButtonMask.md) | enum |  |
| [`Orientation`](classes/Orientation.md) | enum |  |
| [`PropertyHint`](classes/PropertyHint.md) | enum |  |
| [`PropertyUsageFlags`](classes/PropertyUsageFlags.md) | enum |  |
| [`Side`](classes/Side.md) | enum |  |
| [`VerticalAlignment`](classes/VerticalAlignment.md) | enum |  |

## 委托与接口（5）

| 类型 | 种类 | 简介 |
| --- | --- | --- |
| [`IAwaitable`](classes/IAwaitable.md) | interface | An interface that requires a GetAwaiter() method to get a reference to the Awaiter. |
| [`IAwaitable<TResult>`](classes/IAwaitable-1.md) | interface | A templated interface that requires a GetAwaiter() method to get a reference to the Awaiter. |
| [`IAwaiter`](classes/IAwaiter.md) | interface | An interface that requires a boolean for completion status and a method that gets the result of completion. |
| [`IAwaiter<TResult>`](classes/IAwaiter-1.md) | interface | A templated interface that requires a boolean for completion status and a method that gets the result of completion and returns it. |
| [`ISerializationListener`](classes/ISerializationListener.md) | interface | Allows a GodotObject to react to the serialization/deserialization that occurs when Godot reloads assemblies. |

## 其他命名空间（4）

| 类型 | 种类 | 简介 |
| --- | --- | --- |
| [`Array`](classes/Collections.Array.md) | class | Wrapper around Godot's Array class, an array of Variant typed elements allocated in the engine in C++. |
| [`Array<T>`](classes/Collections.Array-1.md) | class | Typed wrapper around Godot's Array class, an array of `T` annotated, Variant typed elements allocated in the engine in C++. |
| [`Dictionary`](classes/Collections.Dictionary.md) | class | Wrapper around Godot's Dictionary class, a dictionary of Variant typed elements allocated in the engine in C++. |
| [`Dictionary<TKey, TValue>`](classes/Collections.Dictionary-2.md) | class | Typed wrapper around Godot's Dictionary class, a dictionary of `TKey` and `TValue` annotated, Variant typed elements allocated in the engine in C++. |

## 编辑器（83）

| 类型 | 种类 | 简介 |
| --- | --- | --- |
| [`EditorCommandPalette`](classes/EditorCommandPalette.md) | class | Object that holds all the available Commands and their shortcuts text. |
| [`EditorContextMenuPlugin`](classes/EditorContextMenuPlugin.md) | class | `EditorContextMenuPlugin` allows for the addition of custom options in the editor's context menu. |
| [`EditorDebuggerPlugin`](classes/EditorDebuggerPlugin.md) | class | `EditorDebuggerPlugin` provides functions related to the editor side of the debugger. |
| [`EditorDebuggerSession`](classes/EditorDebuggerSession.md) | class | This class cannot be directly instantiated and must be retrieved via an [`EditorDebuggerPlugin`](classes/EditorDebuggerPlugin.md). |
| [`EditorDock`](classes/EditorDock.md) | class | EditorDock is a [`Container`](classes/Container.md) node that can be docked in one of the editor's dock slots. |
| [`EditorExportPlatform`](classes/EditorExportPlatform.md) | class | Base resource that provides the functionality of exporting a release build of a project to a platform, from the editor. |
| [`EditorExportPlatformAndroid`](classes/EditorExportPlatformAndroid.md) | class | Exporter for Android. |
| [`EditorExportPlatformAppleEmbedded`](classes/EditorExportPlatformAppleEmbedded.md) | class | The base class for Apple embedded platform exporters. |
| [`EditorExportPlatformExtension`](classes/EditorExportPlatformExtension.md) | class | External [`EditorExportPlatform`](classes/EditorExportPlatform.md) implementations should inherit from this class. |
| [`EditorExportPlatformIos`](classes/EditorExportPlatformIos.md) | class |  |
| [`EditorExportPlatformLinuxBsd`](classes/EditorExportPlatformLinuxBsd.md) | class |  |
| [`EditorExportPlatformMacOS`](classes/EditorExportPlatformMacOS.md) | class | Exporter for macOS. |
| [`EditorExportPlatformPC`](classes/EditorExportPlatformPC.md) | class | The base class for the desktop platform exporters. |
| [`EditorExportPlatformVisionOS`](classes/EditorExportPlatformVisionOS.md) | class | Exporter for visionOS. |
| [`EditorExportPlatformWeb`](classes/EditorExportPlatformWeb.md) | class | The Web exporter customizes how a web build is handled. |
| [`EditorExportPlatformWindows`](classes/EditorExportPlatformWindows.md) | class | The Windows exporter customizes how a Windows build is handled. |
| [`EditorExportPlugin`](classes/EditorExportPlugin.md) | class | `EditorExportPlugin`s are automatically invoked whenever the user exports the project. |
| [`EditorExportPreset`](classes/EditorExportPreset.md) | class | Represents the configuration of an export preset, as created by the editor's export dialog. |
| [`EditorFeatureProfile`](classes/EditorFeatureProfile.md) | class | An editor feature profile can be used to disable specific features of the Godot editor. |
| [`EditorFileDialog`](classes/EditorFileDialog.md) | class | `EditorFileDialog` is a [`FileDialog`](classes/FileDialog.md) tweaked to work in the editor. |
| [`EditorFileSystem`](classes/EditorFileSystem.md) | class | This object holds information of all resources in the filesystem, their types, etc. |
| [`EditorFileSystemDirectory`](classes/EditorFileSystemDirectory.md) | class | A more generalized, low-level variation of the directory concept. |
| [`EditorFileSystemImportFormatSupportQuery`](classes/EditorFileSystemImportFormatSupportQuery.md) | class | This class is used to query and configure a certain import format. |
| [`EditorImportPlugin`](classes/EditorImportPlugin.md) | class | `EditorImportPlugin`s provide a way to extend the editor's resource import functionality. |
| [`EditorInspector`](classes/EditorInspector.md) | class | This is the control that implements property editing in the editor's Settings dialogs, the Inspector dock, etc. |
| [`EditorInspectorPlugin`](classes/EditorInspectorPlugin.md) | class | `EditorInspectorPlugin` allows adding custom property editors to [`EditorInspector`](classes/EditorInspector.md). |
| [`EditorInterface`](classes/EditorInterface.md) | class | `EditorInterface` gives you control over Godot editor's window. |
| [`EditorNode3DGizmo`](classes/EditorNode3DGizmo.md) | class | Gizmo that is used for providing custom visualization and editing (handles and subgizmos) for [`Node3D`](classes/Node3D.md) objects. |
| [`EditorNode3DGizmoPlugin`](classes/EditorNode3DGizmoPlugin.md) | class | `EditorNode3DGizmoPlugin` allows you to define a new type of Gizmo. |
| [`EditorPaths`](classes/EditorPaths.md) | class | This editor-only singleton returns OS-specific paths to various data folders and files. |
| [`EditorPlugin`](classes/EditorPlugin.md) | class | Plugins are used by the editor to extend functionality. |
| [`EditorProperty`](classes/EditorProperty.md) | class | A custom control for editing properties that can be added to the [`EditorInspector`](classes/EditorInspector.md). |
| [`EditorResourceConversionPlugin`](classes/EditorResourceConversionPlugin.md) | class | `EditorResourceConversionPlugin` is invoked when the context menu is brought up for a resource in the editor inspector. |
| [`EditorResourcePicker`](classes/EditorResourcePicker.md) | class | This [`Control`](classes/Control.md) node is used in the editor's Inspector dock to allow editing of [`Resource`](classes/Resource.md) type properties. |
| [`EditorResourcePreview`](classes/EditorResourcePreview.md) | class | This node is used to generate previews for resources or files. |
| [`EditorResourcePreviewGenerator`](classes/EditorResourcePreviewGenerator.md) | class | Custom code to generate previews. |
| [`EditorResourceTooltipPlugin`](classes/EditorResourceTooltipPlugin.md) | class | Resource tooltip plugins are used by [`FileSystemDock`](classes/FileSystemDock.md) to generate customized tooltips for specific resources. |
| [`EditorSceneFormatImporter`](classes/EditorSceneFormatImporter.md) | class | `EditorSceneFormatImporter` allows to define an importer script for a third-party 3D format. |
| [`EditorSceneFormatImporterBlend`](classes/EditorSceneFormatImporterBlend.md) | class | Imports Blender scenes in the `.blend` file format through the glTF 2.0 3D import pipeline. |
| [`EditorSceneFormatImporterFbx2Gltf`](classes/EditorSceneFormatImporterFbx2Gltf.md) | class | Imports Autodesk FBX 3D scenes by way of converting them to glTF 2.0 using the FBX2glTF command line tool. |
| [`EditorSceneFormatImporterGltf`](classes/EditorSceneFormatImporterGltf.md) | class |  |
| [`EditorSceneFormatImporterUfbx`](classes/EditorSceneFormatImporterUfbx.md) | class | EditorSceneFormatImporterUFBX is designed to load FBX files and supports both binary and ASCII FBX files from version 3000 onward. |
| [`EditorScenePostImport`](classes/EditorScenePostImport.md) | class | Imported scenes can be automatically modified right after import by setting their **Custom Script** Import property to a `tool` script that inherits from this class. |
| [`EditorScenePostImportPlugin`](classes/EditorScenePostImportPlugin.md) | class | This plugin type exists to modify the process of importing scenes, allowing to change the content as well as add importer options at every stage of the process. |
| [`EditorScript`](classes/EditorScript.md) | class | Scripts extending this class and implementing its `_Run()` method can be executed from the Script Editor's **File > Run** menu option (or by pressing Ctrl + Shift + X) while the editor is running. |
| [`EditorScriptPicker`](classes/EditorScriptPicker.md) | class | Similar to [`EditorResourcePicker`](classes/EditorResourcePicker.md) this [`Control`](classes/Control.md) node is used in the editor's Inspector dock, but only to edit the `script` property of a [`Node`](classes/Node.md). |
| [`EditorSelection`](classes/EditorSelection.md) | class | This object manages the SceneTree selection in the editor. |
| [`EditorSettings`](classes/EditorSettings.md) | class | Object that holds the project-independent editor settings. |
| [`EditorSpinSlider`](classes/EditorSpinSlider.md) | class | This [`Control`](classes/Control.md) node is used in the editor's Inspector dock to allow editing of numeric values. |
| [`EditorSyntaxHighlighter`](classes/EditorSyntaxHighlighter.md) | class | Base class that all [`SyntaxHighlighter`](classes/SyntaxHighlighter.md)s used by the [`ScriptEditor`](classes/ScriptEditor.md) extend from. |
| [`EditorToaster`](classes/EditorToaster.md) | class | This object manages the functionality and display of toast notifications within the editor, ensuring immediate and informative alerts are presented to the user. |
| [`EditorTranslationParserPlugin`](classes/EditorTranslationParserPlugin.md) | class | `EditorTranslationParserPlugin` is invoked when a file is being parsed to extract strings that require translation. |
| [`EditorUndoRedoManager`](classes/EditorUndoRedoManager.md) | class | `EditorUndoRedoManager` is a manager for [`UndoRedo`](classes/UndoRedo.md) objects associated with edited scenes. |
| [`EditorVcsInterface`](classes/EditorVcsInterface.md) | class | Defines the API that the editor uses to extract information from the underlying VCS. |
| [`FileSystemDock`](classes/FileSystemDock.md) | class | This class is available only in [`EditorPlugin`](classes/EditorPlugin.md)s and can't be instantiated. |
| [`GDScriptLanguageProtocol`](classes/GDScriptLanguageProtocol.md) | static class | Provides access to certain features that are implemented in the language server. |
| [`GDScriptLanguageProtocolInstance`](classes/GDScriptLanguageProtocolInstance.md) | class | Provides access to certain features that are implemented in the language server. |
| [`GDScriptSyntaxHighlighter`](classes/GDScriptSyntaxHighlighter.md) | class | **Note:** This class can only be used for editor plugins because it relies on editor settings. |
| [`GDScriptTextDocument`](classes/GDScriptTextDocument.md) | class | Provides language server functionality related to documents. |
| [`GDScriptWorkspace`](classes/GDScriptWorkspace.md) | class | Provides language server functionality related to the workspace. |
| [`GridMapEditorPlugin`](classes/GridMapEditorPlugin.md) | class | GridMapEditorPlugin provides access to the [`GridMap`](classes/GridMap.md) editor functionality. |
| [`OpenXRBindingModifierEditor`](classes/OpenXRBindingModifierEditor.md) | class | This is the default binding modifier editor used in the OpenXR action map. |
| [`OpenXRInteractionProfileEditor`](classes/OpenXRInteractionProfileEditor.md) | class | This is the default OpenXR interaction profile editor that provides a generic interface for editing any interaction profile for which no custom editor has been defined. |
| [`OpenXRInteractionProfileEditorBase`](classes/OpenXRInteractionProfileEditorBase.md) | class | This is a base class for interaction profile editors used by the OpenXR action map editor. |
| [`ResourceImporterBitMap`](classes/ResourceImporterBitMap.md) | class | [`Bitmap`](classes/Bitmap.md) resources are typically used as click masks in [`TextureButton`](classes/TextureButton.md) and [`TouchScreenButton`](classes/TouchScreenButton.md). |
| [`ResourceImporterBMFont`](classes/ResourceImporterBMFont.md) | class | The BMFont format is a format created by the [BMFont](https://www.angelcode.com/products/bmfont/) program. |
| [`ResourceImporterCsvTranslation`](classes/ResourceImporterCsvTranslation.md) | class | Comma-separated values are a plain text table storage format. |
| [`ResourceImporterDynamicFont`](classes/ResourceImporterDynamicFont.md) | class | Unlike bitmap fonts, dynamic fonts can be resized to any size and still look crisp. |
| [`ResourceImporterImage`](classes/ResourceImporterImage.md) | class | This importer imports [`Image`](classes/Image.md) resources, as opposed to [`CompressedTexture2D`](classes/CompressedTexture2D.md). |
| [`ResourceImporterImageFont`](classes/ResourceImporterImageFont.md) | class | This image-based workflow can be easier to use than [`ResourceImporterBMFont`](classes/ResourceImporterBMFont.md), but it requires all glyphs to have the same width and height, glyph advances and drawing offsets can be customize… |
| [`ResourceImporterLayeredTexture`](classes/ResourceImporterLayeredTexture.md) | class | This imports a 3-dimensional texture, which can then be used in custom shaders, as a [`FogMaterial`](classes/FogMaterial.md) density map or as a [`GpuParticlesAttractorVectorField3D`](classes/GpuParticlesAttractorVectorField3D.md). |
| [`ResourceImporterMP3`](classes/ResourceImporterMP3.md) | class | MP3 is a lossy audio format, with worse audio quality compared to [`ResourceImporterOggVorbis`](classes/ResourceImporterOggVorbis.md) at a given bitrate. |
| [`ResourceImporterObj`](classes/ResourceImporterObj.md) | class | Unlike [`ResourceImporterScene`](classes/ResourceImporterScene.md), `ResourceImporterObj` will import a single [`Mesh`](classes/Mesh.md) resource by default instead of importing a [`PackedScene`](classes/PackedScene.md). |
| [`ResourceImporterOggVorbis`](classes/ResourceImporterOggVorbis.md) | class | Ogg Vorbis is a lossy audio format, with better audio quality compared to [`ResourceImporterMP3`](classes/ResourceImporterMP3.md) at a given bitrate. |
| [`ResourceImporterScene`](classes/ResourceImporterScene.md) | class | See also [`ResourceImporterObj`](classes/ResourceImporterObj.md), which is used for OBJ models that can be imported as an independent [`Mesh`](classes/Mesh.md) or a scene. |
| [`ResourceImporterShaderFile`](classes/ResourceImporterShaderFile.md) | class | This imports native GLSL shaders as [`RDShaderFile`](classes/RDShaderFile.md) resources, for use with low-level [`RenderingDevice`](classes/RenderingDevice.md) operations. |
| [`ResourceImporterSvg`](classes/ResourceImporterSvg.md) | class | This importer imports [`DpiTexture`](classes/DpiTexture.md) resources. |
| [`ResourceImporterTexture`](classes/ResourceImporterTexture.md) | class | This importer imports [`CompressedTexture2D`](classes/CompressedTexture2D.md) resources. |
| [`ResourceImporterTextureAtlas`](classes/ResourceImporterTextureAtlas.md) | class | This imports a collection of textures from a PNG image into an [`AtlasTexture`](classes/AtlasTexture.md) or 2D [`ArrayMesh`](classes/ArrayMesh.md). |
| [`ResourceImporterWav`](classes/ResourceImporterWav.md) | class | WAV is an uncompressed format, which can provide higher quality compared to Ogg Vorbis and MP3. |
| [`ScriptCreateDialog`](classes/ScriptCreateDialog.md) | class | The `ScriptCreateDialog` creates script files according to a given template for a given scripting language. |
| [`ScriptEditor`](classes/ScriptEditor.md) | class | Godot editor's script editor. |
| [`ScriptEditorBase`](classes/ScriptEditorBase.md) | class | Base editor for editing scripts in the [`ScriptEditor`](classes/ScriptEditor.md). |

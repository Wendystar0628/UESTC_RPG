<!-- 由 scripts/godot-api/gen_api_docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# AnimationNode

命名空间 `Godot`；class；继承 [`Resource`](Resource.md) → [`RefCounted`](RefCounted.md) → [`GodotObject`](GodotObject.md)

Base resource for [`AnimationTree`](AnimationTree.md) nodes. In general, it's not used directly, but you can create custom ones with custom blending formulas.

Inherit this when creating animation nodes mainly for use in [`AnimationNodeBlendTree`](AnimationNodeBlendTree.md), otherwise [`AnimationRootNode`](AnimationRootNode.md) should be used instead.

You can access the time information as read-only parameter which is processed and stored in the previous frame for all nodes except [`AnimationNodeOutput`](AnimationNodeOutput.md).

**Note:** If multiple inputs exist in the `AnimationNode`, which time information takes precedence depends on the type of `AnimationNode`.

```csharp
var current_length = $AnimationTree["parameters/AnimationNodeName/current_length"]
var current_position = $AnimationTree["parameters/AnimationNodeName/current_position"]
var current_delta = $AnimationTree["parameters/AnimationNodeName/current_delta"]

```

## 构造函数

### AnimationNode

```csharp
public AnimationNode()
```

## 属性

### FilterEnabled

```csharp
public bool FilterEnabled { get; set; }
```

引擎名：`filter_enabled`

If `true`, filtering is enabled.

### Filters

```csharp
public Array Filters { get; set; }
```

引擎名：`filters`

## 方法

### AddInput

```csharp
public bool AddInput(string name)
```

引擎名：`add_input`

Adds an input to the animation node. This is only useful for animation nodes created for use in an [`AnimationNodeBlendTree`](AnimationNodeBlendTree.md). If the addition fails, returns `false`.

### BlendAnimation

```csharp
public void BlendAnimation(StringName animation, double time, double delta, bool seeked, bool isExternalSeeking, float blend, Animation.LoopedFlag loopedFlag = Animation.LoopedFlag.None)
```

引擎名：`blend_animation`

Blends an animation by `blend` amount (name must be valid in the linked [`AnimationPlayer`](AnimationPlayer.md)). A `time` and `delta` may be passed, as well as whether `seeked` happened.

A `loopedFlag` is used by internal processing immediately after the loop.

### BlendInput

```csharp
public double BlendInput(int inputIndex, double time, bool seek, bool isExternalSeeking, float blend, AnimationNode.FilterAction filter = AnimationNode.FilterAction.Ignore, bool sync = true, bool testOnly = false)
```

引擎名：`blend_input`

Blends an input. This is only useful for animation nodes created for an [`AnimationNodeBlendTree`](AnimationNodeBlendTree.md). The `time` parameter is a relative delta, unless `seek` is `true`, in which case it is absolute. A filter mode may be optionally passed.

### BlendInput

```csharp
public double BlendInput(int inputIndex, double time, bool seek, bool isExternalSeeking, float blend, AnimationNode.FilterAction filter, bool sync)
```

引擎名：`blend_input`

Blends an input. This is only useful for animation nodes created for an [`AnimationNodeBlendTree`](AnimationNodeBlendTree.md). The `time` parameter is a relative delta, unless `seek` is `true`, in which case it is absolute. A filter mode may be optionally passed.

### BlendNode

```csharp
public double BlendNode(StringName name, AnimationNode node, double time, bool seek, bool isExternalSeeking, float blend, AnimationNode.FilterAction filter = AnimationNode.FilterAction.Ignore, bool sync = true, bool testOnly = false)
```

引擎名：`blend_node`

Blend another animation node (in case this animation node contains child animation nodes). This function is only useful if you inherit from [`AnimationRootNode`](AnimationRootNode.md) instead, otherwise editors will not display your animation node for addition.

### BlendNode

```csharp
public double BlendNode(StringName name, AnimationNode node, double time, bool seek, bool isExternalSeeking, float blend, AnimationNode.FilterAction filter, bool sync)
```

引擎名：`blend_node`

Blend another animation node (in case this animation node contains child animation nodes). This function is only useful if you inherit from [`AnimationRootNode`](AnimationRootNode.md) instead, otherwise editors will not display your animation node for addition.

### FindInput

```csharp
public int FindInput(string name)
```

引擎名：`find_input`

Returns the input index which corresponds to `name`. If not found, returns `-1`.

### GetInputCount

```csharp
public int GetInputCount()
```

引擎名：`get_input_count`

Amount of inputs in this animation node, only useful for animation nodes that go into [`AnimationNodeBlendTree`](AnimationNodeBlendTree.md).

### GetInputName

```csharp
public string GetInputName(int input)
```

引擎名：`get_input_name`

Gets the name of an input by index.

### GetParameter

```csharp
public Variant GetParameter(StringName name)
```

引擎名：`get_parameter`

Gets the value of a parameter. Parameters are custom local memory used for your animation nodes, given a resource can be reused in multiple trees.

### GetProcessingAnimationTreeInstanceId

```csharp
public ulong GetProcessingAnimationTreeInstanceId()
```

引擎名：`get_processing_animation_tree_instance_id`

Returns the object id of the [`AnimationTree`](AnimationTree.md) that owns this node.

**Note:** This method should only be called from within the [`AnimationNodeExtension._ProcessAnimationNode()`](AnimationNodeExtension.md) method, and will return an invalid id otherwise.

### IsFilterEnabled

```csharp
public bool IsFilterEnabled()
```

引擎名：`is_filter_enabled`

### IsPathFiltered

```csharp
public bool IsPathFiltered(NodePath path)
```

引擎名：`is_path_filtered`

Returns `true` if the given path is filtered.

### IsProcessTesting

```csharp
public bool IsProcessTesting()
```

引擎名：`is_process_testing`

Returns `true` if this animation node is being processed in test-only mode.

### RemoveInput

```csharp
public void RemoveInput(int index)
```

引擎名：`remove_input`

Removes an input, call this only when inactive.

### SetFilterEnabled

```csharp
public void SetFilterEnabled(bool enable)
```

引擎名：`set_filter_enabled`

### SetFilterPath

```csharp
public void SetFilterPath(NodePath path, bool enable)
```

引擎名：`set_filter_path`

Adds or removes a path for the filter.

### SetInputName

```csharp
public bool SetInputName(int input, string name)
```

引擎名：`set_input_name`

Sets the name of the input at the given `input` index. If the setting fails, returns `false`.

### SetParameter

```csharp
public void SetParameter(StringName name, Variant value)
```

引擎名：`set_parameter`

Sets a custom parameter. These are used as local memory, because resources can be reused across the tree or scenes.

### _GetCaption

```csharp
public virtual string _GetCaption()
```

引擎名：`_get_caption`

When inheriting from [`AnimationRootNode`](AnimationRootNode.md), implement this virtual method to override the text caption for this animation node.

### _GetChildByName

```csharp
public virtual AnimationNode _GetChildByName(StringName name)
```

引擎名：`_get_child_by_name`

When inheriting from [`AnimationRootNode`](AnimationRootNode.md), implement this virtual method to return a child animation node by its `name`.

### _GetChildNodes

```csharp
public virtual Dictionary _GetChildNodes()
```

引擎名：`_get_child_nodes`

When inheriting from [`AnimationRootNode`](AnimationRootNode.md), implement this virtual method to return all child animation nodes in order as a `name: node` dictionary.

### _GetParameterDefaultValue

```csharp
public virtual Variant _GetParameterDefaultValue(StringName parameter)
```

引擎名：`_get_parameter_default_value`

When inheriting from [`AnimationRootNode`](AnimationRootNode.md), implement this virtual method to return the default value of a `parameter`. Parameters are custom local memory used for your animation nodes, given a resource can be reused in multiple trees.

### _GetParameterList

```csharp
public virtual Array _GetParameterList()
```

引擎名：`_get_parameter_list`

When inheriting from [`AnimationRootNode`](AnimationRootNode.md), implement this virtual method to return a list of the properties on this animation node. Parameters are custom local memory used for your animation nodes, given a resource can be reused in multiple trees. Format is similar to [`GodotObject.GetPropertyList`](GodotObject.md).

### _HasFilter

```csharp
public virtual bool _HasFilter()
```

引擎名：`_has_filter`

When inheriting from [`AnimationRootNode`](AnimationRootNode.md), implement this virtual method to return whether the blend tree editor should display filter editing on this animation node.

### _IsParameterReadOnly

```csharp
public virtual bool _IsParameterReadOnly(StringName parameter)
```

引擎名：`_is_parameter_read_only`

When inheriting from [`AnimationRootNode`](AnimationRootNode.md), implement this virtual method to return whether the `parameter` is read-only. Parameters are custom local memory used for your animation nodes, given a resource can be reused in multiple trees.

### _Process

```csharp
public virtual double _Process(double time, bool seek, bool isExternalSeeking, bool testOnly)
```

引擎名：`_process`

When inheriting from [`AnimationRootNode`](AnimationRootNode.md), implement this virtual method to run some code when this animation node is processed. The `time` parameter is a relative delta, unless `seek` is `true`, in which case it is absolute.

Here, call the `BlendInput()`, `BlendNode()` or `BlendAnimation()` functions. You can also use `GetParameter()` and `SetParameter()` to modify local memory.

This function should return the delta.

## 事件（信号）

### AnimationNodeRemoved

```csharp
public event AnimationNode.AnimationNodeRemovedEventHandler AnimationNodeRemoved
```

引擎名：`animation_node_removed`

Emitted by nodes that inherit from this class and that have an internal tree when one of their animation nodes removes. The animation nodes that emit this signal are [`AnimationNodeBlendSpace1D`](AnimationNodeBlendSpace1D.md), [`AnimationNodeBlendSpace2D`](AnimationNodeBlendSpace2D.md), [`AnimationNodeStateMachine`](AnimationNodeStateMachine.md), and [`AnimationNodeBlendTree`](AnimationNodeBlendTree.md).

### AnimationNodeRenamed

```csharp
public event AnimationNode.AnimationNodeRenamedEventHandler AnimationNodeRenamed
```

引擎名：`animation_node_renamed`

Emitted by nodes that inherit from this class and that have an internal tree when one of their animation node names changes. The animation nodes that emit this signal are [`AnimationNodeBlendSpace1D`](AnimationNodeBlendSpace1D.md), [`AnimationNodeBlendSpace2D`](AnimationNodeBlendSpace2D.md), [`AnimationNodeStateMachine`](AnimationNodeStateMachine.md), and [`AnimationNodeBlendTree`](AnimationNodeBlendTree.md).

### NodeUpdated

```csharp
public event AnimationNode.NodeUpdatedEventHandler NodeUpdated
```

引擎名：`node_updated`

Emitted by [`AnimationNodeAnimation`](AnimationNodeAnimation.md) when its [`AnimationNodeAnimation.Animation`](AnimationNodeAnimation.md) resource is changed, or by [`AnimationNodeBlendTree`](AnimationNodeBlendTree.md) when its connections change.

### TreeChanged

```csharp
public event Action TreeChanged
```

引擎名：`tree_changed`

Emitted by nodes that inherit from this class and that have an internal tree when one of their animation nodes changes. The animation nodes that emit this signal are [`AnimationNodeBlendSpace1D`](AnimationNodeBlendSpace1D.md), [`AnimationNodeBlendSpace2D`](AnimationNodeBlendSpace2D.md), [`AnimationNodeStateMachine`](AnimationNodeStateMachine.md), [`AnimationNodeBlendTree`](AnimationNodeBlendTree.md) and [`AnimationNodeTransition`](AnimationNodeTransition.md).

## AnimationNodeRemovedEventHandler

命名空间 `Godot`；delegate

Represents the method that handles the [`AnimationNode.AnimationNodeRemoved`](AnimationNode.md) event of a [`AnimationNode`](AnimationNode.md) class.

### 签名

#### Invoke

```csharp
delegate void AnimationNodeRemovedEventHandler(long objectId, string nodeName)
```

## AnimationNodeRenamedEventHandler

命名空间 `Godot`；delegate

Represents the method that handles the [`AnimationNode.AnimationNodeRenamed`](AnimationNode.md) event of a [`AnimationNode`](AnimationNode.md) class.

### 签名

#### Invoke

```csharp
delegate void AnimationNodeRenamedEventHandler(long objectId, string oldName, string newName)
```

## FilterAction

命名空间 `Godot`；enum

### 枚举值

- `Ignore = 0`：Do not use filtering.
- `Pass = 1`：Paths matching the filter will be allowed to pass.
- `Stop = 2`：Paths matching the filter will be discarded.
- `Blend = 3`：Paths matching the filter will be blended (by the blend value).

## NodeUpdatedEventHandler

命名空间 `Godot`；delegate

Represents the method that handles the [`AnimationNode.NodeUpdated`](AnimationNode.md) event of a [`AnimationNode`](AnimationNode.md) class.

### 签名

#### Invoke

```csharp
delegate void NodeUpdatedEventHandler(long objectId)
```

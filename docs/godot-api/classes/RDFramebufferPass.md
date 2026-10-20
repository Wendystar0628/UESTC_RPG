<!-- 由 scripts/godot-api/gen-api-docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# RDFramebufferPass

命名空间 `Godot`；class；继承 [`RefCounted`](RefCounted.md) → [`GodotObject`](GodotObject.md)

This class contains the list of attachment descriptions for a framebuffer pass. Each points with an index to a previously supplied list of texture attachments.

Multipass framebuffers can optimize some configurations in mobile. On desktop, they provide little to no advantage.

This object is used by [`RenderingDevice`](RenderingDevice.md).

## 构造函数

### RDFramebufferPass

```csharp
public RDFramebufferPass()
```

## 属性

### ColorAttachments

```csharp
public int[] ColorAttachments { get; set; }
```

引擎名：`color_attachments`

Color attachments in order starting from 0. If this attachment is not used by the shader, pass ATTACHMENT_UNUSED to skip.

### DepthAttachment

```csharp
public int DepthAttachment { get; set; }
```

引擎名：`depth_attachment`

Depth attachment. ATTACHMENT_UNUSED should be used if no depth buffer is required for this pass.

### InputAttachments

```csharp
public int[] InputAttachments { get; set; }
```

引擎名：`input_attachments`

Used for multipass framebuffers (more than one render pass). Converts an attachment to an input. Make sure to also supply it properly in the [`RDUniform`](RDUniform.md) for the uniform set.

### PreserveAttachments

```csharp
public int[] PreserveAttachments { get; set; }
```

引擎名：`preserve_attachments`

Attachments to preserve in this pass (otherwise they are erased).

### ResolveAttachments

```csharp
public int[] ResolveAttachments { get; set; }
```

引擎名：`resolve_attachments`

If the color attachments are multisampled, non-multisampled resolve attachments can be provided.

## 方法

### GetColorAttachments

```csharp
public int[] GetColorAttachments()
```

引擎名：`get_color_attachments`

### GetDepthAttachment

```csharp
public int GetDepthAttachment()
```

引擎名：`get_depth_attachment`

### GetInputAttachments

```csharp
public int[] GetInputAttachments()
```

引擎名：`get_input_attachments`

### GetPreserveAttachments

```csharp
public int[] GetPreserveAttachments()
```

引擎名：`get_preserve_attachments`

### GetResolveAttachments

```csharp
public int[] GetResolveAttachments()
```

引擎名：`get_resolve_attachments`

### SetColorAttachments

```csharp
public void SetColorAttachments(ReadOnlySpan<int> pMember)
```

引擎名：`set_color_attachments`

### SetColorAttachments

```csharp
public void SetColorAttachments(int[] pMember)
```

引擎名：`set_color_attachments`

### SetDepthAttachment

```csharp
public void SetDepthAttachment(int pMember)
```

引擎名：`set_depth_attachment`

### SetInputAttachments

```csharp
public void SetInputAttachments(ReadOnlySpan<int> pMember)
```

引擎名：`set_input_attachments`

### SetInputAttachments

```csharp
public void SetInputAttachments(int[] pMember)
```

引擎名：`set_input_attachments`

### SetPreserveAttachments

```csharp
public void SetPreserveAttachments(ReadOnlySpan<int> pMember)
```

引擎名：`set_preserve_attachments`

### SetPreserveAttachments

```csharp
public void SetPreserveAttachments(int[] pMember)
```

引擎名：`set_preserve_attachments`

### SetResolveAttachments

```csharp
public void SetResolveAttachments(ReadOnlySpan<int> pMember)
```

引擎名：`set_resolve_attachments`

### SetResolveAttachments

```csharp
public void SetResolveAttachments(int[] pMember)
```

引擎名：`set_resolve_attachments`

## 常量

### AttachmentUnused

```csharp
public const long AttachmentUnused = -1
```

Attachment is unused.

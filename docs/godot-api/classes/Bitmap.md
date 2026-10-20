<!-- 由 scripts/godot-api/gen-api-docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# Bitmap

命名空间 `Godot`；class；继承 [`Resource`](Resource.md) → [`RefCounted`](RefCounted.md) → [`GodotObject`](GodotObject.md)

A two-dimensional array of boolean values, can be used to efficiently store a binary matrix (every matrix element takes only one bit) and query the values using natural cartesian coordinates.

## 构造函数

### Bitmap

```csharp
public Bitmap()
```

## 属性

### Data

```csharp
public Dictionary Data { get; set; }
```

## 方法

### ConvertToImage

```csharp
public Image ConvertToImage()
```

Returns an image of the same size as the bitmap and with an [`Image.Format`](Image.md) of type [`Image.Format.L8`](Image.md). `true` bits of the bitmap are being converted into white pixels, and `false` bits into black.

### Create

```csharp
public void Create(Vector2I size)
```

Creates a bitmap with the specified size, filled with `false`.

### CreateFromImageAlpha

```csharp
public void CreateFromImageAlpha(Image image, float threshold = 0.1f)
```

Creates a bitmap that matches the given image dimensions, every element of the bitmap is set to `false` if the alpha value of the image at that position is equal to `threshold` or less, and `true` in other case.

### GetBit

```csharp
public bool GetBit(int x, int y)
```

Returns bitmap's value at the specified position.

### GetBitv

```csharp
public bool GetBitv(Vector2I position)
```

Returns bitmap's value at the specified position.

### GetSize

```csharp
public Vector2I GetSize()
```

Returns bitmap's dimensions.

### GetTrueBitCount

```csharp
public int GetTrueBitCount()
```

Returns the number of bitmap elements that are set to `true`.

### GrowMask

```csharp
public void GrowMask(int pixels, Rect2I rect)
```

Applies morphological dilation or erosion to the bitmap. If `pixels` is positive, dilation is applied to the bitmap. If `pixels` is negative, erosion is applied to the bitmap. `rect` defines the area where the morphological operation is applied. Pixels located outside the `rect` are unaffected by `GrowMask()`.

### OpaqueToPolygons

```csharp
public Array<Vector2[]> OpaqueToPolygons(Rect2I rect, float epsilon = 2f)
```

Creates an [`Collections.Array`](Collections.Array.md) of polygons covering a rectangular portion of the bitmap. It uses a marching squares algorithm, followed by Ramer-Douglas-Peucker (RDP) reduction of the number of vertices. Each polygon is described as a [`Vector2`](Vector2.md)[] of its vertices.

To get polygons covering the whole bitmap, pass:

```csharp
Rect2(Vector2(), get_size())

```

`epsilon` is passed to RDP to control how accurately the polygons cover the bitmap: a lower `epsilon` corresponds to more points in the polygons.

### Resize

```csharp
public void Resize(Vector2I newSize)
```

Resizes the image to `newSize`.

### SetBit

```csharp
public void SetBit(int x, int y, bool bit)
```

Sets the bitmap's element at the specified position, to the specified value.

### SetBitRect

```csharp
public void SetBitRect(Rect2I rect, bool bit)
```

Sets a rectangular portion of the bitmap to the specified value.

### SetBitv

```csharp
public void SetBitv(Vector2I position, bool bit)
```

Sets the bitmap's element at the specified position, to the specified value.

<!-- 由 scripts/godot-api/gen-api-docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# Noise

命名空间 `Godot`；class；继承 [`Resource`](Resource.md) → [`RefCounted`](RefCounted.md) → [`GodotObject`](GodotObject.md)

This class defines the interface for noise generation libraries to inherit from.

A default `GetSeamlessImage()` implementation is provided for libraries that do not provide seamless noise. This function requests a larger image from the `GetImage()` method, reverses the quadrants of the image, then uses the strips of extra width to blend over the seams.

Inheriting noise classes can optionally override this function to provide a more optimal algorithm.

## 方法

### GetImage

```csharp
public Image GetImage(int width, int height, bool invert = false, bool in3DSpace = false, bool normalize = true)
```

引擎名：`get_image`

Returns an [`Image`](Image.md) containing 2D noise values.

**Note:** With `normalize` set to `false`, the default implementation expects the noise generator to return values in the range `-1.0` to `1.0`.

### GetImage3D

```csharp
public Array<Image> GetImage3D(int width, int height, int depth, bool invert = false, bool normalize = true)
```

Returns an [`Collections.Array`](Collections.Array.md) of [`Image`](Image.md)s containing 3D noise values for use with [`ImageTexture3D.Create`](ImageTexture3D.md).

**Note:** With `normalize` set to `false`, the default implementation expects the noise generator to return values in the range `-1.0` to `1.0`.

### GetNoise1D

```csharp
public float GetNoise1D(float x)
```

Returns the 1D noise value at the given (x) coordinate.

### GetNoise2D

```csharp
public float GetNoise2D(float x, float y)
```

Returns the 2D noise value at the given position.

### GetNoise2Dv

```csharp
public float GetNoise2Dv(Vector2 v)
```

Returns the 2D noise value at the given position.

### GetNoise3D

```csharp
public float GetNoise3D(float x, float y, float z)
```

Returns the 3D noise value at the given position.

### GetNoise3Dv

```csharp
public float GetNoise3Dv(Vector3 v)
```

Returns the 3D noise value at the given position.

### GetSeamlessImage

```csharp
public Image GetSeamlessImage(int width, int height, bool invert = false, bool in3DSpace = false, float skirt = 0.1f, bool normalize = true)
```

引擎名：`get_seamless_image`

Returns an [`Image`](Image.md) containing seamless 2D noise values.

**Note:** With `normalize` set to `false`, the default implementation expects the noise generator to return values in the range `-1.0` to `1.0`.

### GetSeamlessImage3D

```csharp
public Array<Image> GetSeamlessImage3D(int width, int height, int depth, bool invert = false, float skirt = 0.1f, bool normalize = true)
```

Returns an [`Collections.Array`](Collections.Array.md) of [`Image`](Image.md)s containing seamless 3D noise values for use with [`ImageTexture3D.Create`](ImageTexture3D.md).

**Note:** With `normalize` set to `false`, the default implementation expects the noise generator to return values in the range `-1.0` to `1.0`.

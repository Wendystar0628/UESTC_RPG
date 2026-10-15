<!-- 由 scripts/godot-api/gen_api_docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# Image

命名空间 `Godot`；class；继承 [`Resource`](Resource.md) → [`RefCounted`](RefCounted.md) → [`GodotObject`](GodotObject.md)

Native image datatype. Contains image data which can be converted to an [`ImageTexture`](ImageTexture.md) and provides commonly used *image processing* methods. The maximum width and height for an `Image` are `MaxWidth` and `MaxHeight`.

An `Image` cannot be assigned to a texture property of an object directly (such as [`Sprite2D.Texture`](Sprite2D.md)), and has to be converted manually to an [`ImageTexture`](ImageTexture.md) first.

**Note:** Methods that modify the image data cannot be used on VRAM-compressed images. Use `Decompress()` to convert the image to an uncompressed format first.

**Note:** The maximum image size is 16384×16384 pixels due to graphics hardware limitations. Larger images may fail to import.

## 构造函数

### Image

```csharp
public Image()
```

## 属性

### Data

```csharp
public Dictionary Data { get; set; }
```

引擎名：`data`

Holds all the image's color data in a given format. See [`Image.Format`](Image.md) constants.

## 方法

### AdjustBcs

```csharp
public void AdjustBcs(float brightness, float contrast, float saturation)
```

引擎名：`adjust_bcs`

Adjusts this image's `brightness`, `contrast`, and `saturation` by the given values. Does not work if the image is compressed (see `IsCompressed()`).

### BlendRect

```csharp
public void BlendRect(Image src, Rect2I srcRect, Vector2I dst)
```

引擎名：`blend_rect`

Alpha-blends `srcRect` from `src` image to this image at coordinates `dst`, clipped accordingly to both image bounds. This image and `src` image **must** have the same format. `srcRect` with non-positive size is treated as empty.

### BlendRectMask

```csharp
public void BlendRectMask(Image src, Image mask, Rect2I srcRect, Vector2I dst)
```

引擎名：`blend_rect_mask`

Alpha-blends `srcRect` from `src` image to this image using `mask` image at coordinates `dst`, clipped accordingly to both image bounds. Alpha channels are required for both `src` and `mask`. `dst` pixels and `src` pixels will blend if the corresponding mask pixel's alpha value is not 0. This image and `src` image **must** have the same format. `src` image and `mask` image **must** have the same size (width and height) but they can have different formats. `srcRect` with non-positive size is treated as empty.

### BlitRect

```csharp
public void BlitRect(Image src, Rect2I srcRect, Vector2I dst)
```

引擎名：`blit_rect`

Copies `srcRect` from `src` image to this image at coordinates `dst`, clipped accordingly to both image bounds. This image and `src` image **must** have the same format. `srcRect` with non-positive size is treated as empty.

**Note:** The alpha channel data in `src` will overwrite the corresponding data in this image at the target position. To blend alpha channels, use `BlendRect()` instead.

### BlitRectMask

```csharp
public void BlitRectMask(Image src, Image mask, Rect2I srcRect, Vector2I dst)
```

引擎名：`blit_rect_mask`

Blits `srcRect` area from `src` image to this image at the coordinates given by `dst`, clipped accordingly to both image bounds. `src` pixel is copied onto `dst` if the corresponding `mask` pixel's alpha value is not 0. This image and `src` image **must** have the same format. `src` image and `mask` image **must** have the same size (width and height) but they can have different formats. `srcRect` with non-positive size is treated as empty.

### BumpMapToNormalMap

```csharp
public void BumpMapToNormalMap(float bumpScale = 1f)
```

引擎名：`bump_map_to_normal_map`

Converts a bump map to a normal map. A bump map provides a height offset per-pixel, while a normal map provides a normal direction per pixel.

### ClearMipmaps

```csharp
public void ClearMipmaps()
```

引擎名：`clear_mipmaps`

Removes the image's mipmaps.

### Compress

```csharp
public Error Compress(Image.CompressMode mode, Image.CompressSource source = Image.CompressSource.Generic, Image.AstcFormat astcFormat = Image.AstcFormat.Format4X4)
```

引擎名：`compress`

Compresses the image with a VRAM-compressed format to use less memory. Can not directly access pixel data while the image is compressed. Returns error if the chosen compression mode is not available.

The `source` parameter helps to pick the best compression method for DXT and ETC2 formats. It is ignored for ASTC compression.

The `astcFormat` parameter is only taken into account when using ASTC compression; it is ignored for all other formats.

**Note:** `Compress()` is only supported in editor builds. When run in an exported project, this method always returns [`Error.Unavailable`](Error.md).

### CompressFromChannels

```csharp
public Error CompressFromChannels(Image.CompressMode mode, Image.UsedChannels channels, Image.AstcFormat astcFormat = Image.AstcFormat.Format4X4)
```

引擎名：`compress_from_channels`

Compresses the image with a VRAM-compressed format to use less memory. Can not directly access pixel data while the image is compressed. Returns error if the chosen compression mode is not available.

This is an alternative to `Compress()` that lets the user supply the channels used in order for the compressor to pick the best DXT and ETC2 formats. For other formats (non DXT or ETC2), this argument is ignored.

The `astcFormat` parameter is only taken into account when using ASTC compression; it is ignored for all other formats.

**Note:** `CompressFromChannels()` is only supported in editor builds. When run in an exported project, this method always returns [`Error.Unavailable`](Error.md).

### ComputeImageMetrics

```csharp
public Dictionary ComputeImageMetrics(Image comparedImage, bool useLuma)
```

引擎名：`compute_image_metrics`

Compute image metrics on the current image and the compared image. This can be used to calculate the similarity between two images.

The dictionary contains `max`, `mean`, `mean_squared`, `root_mean_squared` and `peak_snr`.

### Convert

```csharp
public void Convert(Image.Format format)
```

引擎名：`convert`

Converts this image's format to the given `format`.

### CopyFrom

```csharp
public void CopyFrom(Image src)
```

引擎名：`copy_from`

Copies `src` image to this image.

### Create

```csharp
public static Image Create(int width, int height, bool useMipmaps, Image.Format format)
```

引擎名：`create`

Creates an empty image of the given size and format. If `useMipmaps` is `true`, generates mipmaps for this image (see `GenerateMipmaps()`).

### CreateEmpty

```csharp
public static Image CreateEmpty(int width, int height, bool useMipmaps, Image.Format format)
```

引擎名：`create_empty`

Creates an empty image of the given size and format. If `useMipmaps` is `true`, generates mipmaps for this image (see `GenerateMipmaps()`).

### CreateFromData

```csharp
public static Image CreateFromData(int width, int height, bool useMipmaps, Image.Format format, ReadOnlySpan<byte> data)
```

引擎名：`create_from_data`

Creates a new image of the given size and format. Fills the image with the given raw data. If `useMipmaps` is `true`, loads the mipmaps for this image from `data`. See `GenerateMipmaps()`.

### CreateFromData

```csharp
public static Image CreateFromData(int width, int height, bool useMipmaps, Image.Format format, byte[] data)
```

引擎名：`create_from_data`

Creates a new image of the given size and format. Fills the image with the given raw data. If `useMipmaps` is `true`, loads the mipmaps for this image from `data`. See `GenerateMipmaps()`.

### Crop

```csharp
public void Crop(int width, int height)
```

引擎名：`crop`

Crops the image to the given `width` and `height`. If the specified size is larger than the current size, the extra area is filled with black pixels.

### Decompress

```csharp
public Error Decompress()
```

引擎名：`decompress`

Decompresses the image if it is VRAM-compressed in a supported format. This increases memory utilization, but allows modifying the image. Returns [`Error.Ok`](Error.md) if the format is supported, otherwise [`Error.Unavailable`](Error.md). All VRAM-compressed formats supported by Godot can be decompressed with this method, except [`Image.Format.Etc2R11S`](Image.md), [`Image.Format.Etc2Rg11S`](Image.md), and [`Image.Format.Etc2Rgb8A1`](Image.md).

### DetectAlpha

```csharp
public Image.AlphaMode DetectAlpha()
```

引擎名：`detect_alpha`

Returns [`Image.AlphaMode.Blend`](Image.md) if the image has data for alpha values. Returns [`Image.AlphaMode.Bit`](Image.md) if all the alpha values are stored in a single bit. Returns [`Image.AlphaMode.None`](Image.md) if no data for alpha values is found.

### DetectUsedChannels

```csharp
public Image.UsedChannels DetectUsedChannels(Image.CompressSource source = Image.CompressSource.Generic)
```

引擎名：`detect_used_channels`

Returns the color channels used by this image. If the image is compressed, the original `source` must be specified.

### Fill

```csharp
public void Fill(Color color)
```

引擎名：`fill`

Fills the image with `color`.

### FillRect

```csharp
public void FillRect(Rect2I rect, Color color)
```

引擎名：`fill_rect`

Fills `rect` with `color`.

### FixAlphaEdges

```csharp
public void FixAlphaEdges()
```

引擎名：`fix_alpha_edges`

Blends low-alpha pixels with nearby pixels.

### FlipX

```csharp
public void FlipX()
```

引擎名：`flip_x`

Flips the image horizontally.

### FlipY

```csharp
public void FlipY()
```

引擎名：`flip_y`

Flips the image vertically.

### GenerateMipmaps

```csharp
public Error GenerateMipmaps(bool renormalize = false)
```

引擎名：`generate_mipmaps`

Generates mipmaps for the image. Mipmaps are precalculated lower-resolution copies of the image that are automatically used if the image needs to be scaled down when rendered. They help improve image quality and performance when rendering. This method returns an error if the image is compressed, in a custom format, or if the image's width/height is `0`. Enabling `renormalize` when generating mipmaps for normal map textures will make sure all resulting vector values are normalized.

It is possible to check if the image has mipmaps by calling `HasMipmaps()` or `GetMipmapCount()`. Calling `GenerateMipmaps()` on an image that already has mipmaps will replace existing mipmaps in the image.

### GetData

```csharp
public byte[] GetData()
```

引擎名：`get_data`

Returns a copy of the image's raw data.

### GetDataSize

```csharp
public long GetDataSize()
```

引擎名：`get_data_size`

Returns size (in bytes) of the image's raw data.

### GetFormat

```csharp
public Image.Format GetFormat()
```

引擎名：`get_format`

Returns this image's format.

### GetHeight

```csharp
public int GetHeight()
```

引擎名：`get_height`

Returns the image's height.

### GetMipmapCount

```csharp
public int GetMipmapCount()
```

引擎名：`get_mipmap_count`

Returns the number of mipmap levels or 0 if the image has no mipmaps. The largest main level image is not counted as a mipmap level by this method, so if you want to include it you can add 1 to this count.

### GetMipmapOffset

```csharp
public long GetMipmapOffset(int mipmap)
```

引擎名：`get_mipmap_offset`

Returns the offset where the image's mipmap with index `mipmap` is stored in the `Data` dictionary.

### GetPixel

```csharp
public Color GetPixel(int x, int y)
```

引擎名：`get_pixel`

Returns the color of the pixel at `(x, y)`.

This is the same as `GetPixelv()`, but with two integer arguments instead of a [`Vector2I`](Vector2I.md) argument.

### GetPixelv

```csharp
public Color GetPixelv(Vector2I point)
```

引擎名：`get_pixelv`

Returns the color of the pixel at `point`.

This is the same as `GetPixel()`, but with a [`Vector2I`](Vector2I.md) argument instead of two integer arguments.

### GetRegion

```csharp
public Image GetRegion(Rect2I region)
```

引擎名：`get_region`

Returns a new `Image` that is a copy of this `Image`'s area specified with `region`.

### GetSize

```csharp
public Vector2I GetSize()
```

引擎名：`get_size`

Returns the image's size (width and height).

### GetUsedRect

```csharp
public Rect2I GetUsedRect()
```

引擎名：`get_used_rect`

Returns a [`Rect2I`](Rect2I.md) enclosing the visible portion of the image, considering each pixel with a non-zero alpha channel as visible.

### GetWidth

```csharp
public int GetWidth()
```

引擎名：`get_width`

Returns the image's width.

### HasMipmaps

```csharp
public bool HasMipmaps()
```

引擎名：`has_mipmaps`

Returns `true` if the image has generated mipmaps.

### IsCompressed

```csharp
public bool IsCompressed()
```

引擎名：`is_compressed`

Returns `true` if the image is compressed.

### IsEmpty

```csharp
public bool IsEmpty()
```

引擎名：`is_empty`

Returns `true` if the image has no data.

### IsInvisible

```csharp
public bool IsInvisible()
```

引擎名：`is_invisible`

Returns `true` if all the image's pixels have an alpha value of 0. Returns `false` if any pixel has an alpha value higher than 0.

### LinearToSrgb

```csharp
public void LinearToSrgb()
```

引擎名：`linear_to_srgb`

Converts the entire image from linear encoding to nonlinear sRGB encoding by using a lookup table. Only works on images with [`Image.Format.Rgb8`](Image.md) or [`Image.Format.Rgba8`](Image.md) formats.

### Load

```csharp
public Error Load(string path)
```

引擎名：`load`

Loads an image from file `path`. See [Supported image formats]($DOCS_URL/tutorials/assets_pipeline/importing_images.html#supported-image-formats) for a list of supported image formats and limitations.

**Warning:** This method should only be used in the editor or in cases when you need to load external images at run-time, such as images located at the `user://` directory, and may not work in exported projects.

See also [`ImageTexture`](ImageTexture.md) description for usage examples.

### LoadBmpFromBuffer

```csharp
public Error LoadBmpFromBuffer(ReadOnlySpan<byte> buffer)
```

引擎名：`load_bmp_from_buffer`

Loads an image from the binary contents of a BMP file.

**Note:** Godot's BMP module doesn't support 16-bit per pixel images. Only 1-bit, 4-bit, 8-bit, 24-bit, and 32-bit per pixel images are supported.

**Note:** This method is only available in engine builds with the BMP module enabled. By default, the BMP module is enabled, but it can be disabled at build-time using the `module_bmp_enabled=no` SCons option.

### LoadBmpFromBuffer

```csharp
public Error LoadBmpFromBuffer(byte[] buffer)
```

引擎名：`load_bmp_from_buffer`

Loads an image from the binary contents of a BMP file.

**Note:** Godot's BMP module doesn't support 16-bit per pixel images. Only 1-bit, 4-bit, 8-bit, 24-bit, and 32-bit per pixel images are supported.

**Note:** This method is only available in engine builds with the BMP module enabled. By default, the BMP module is enabled, but it can be disabled at build-time using the `module_bmp_enabled=no` SCons option.

### LoadDdsFromBuffer

```csharp
public Error LoadDdsFromBuffer(ReadOnlySpan<byte> buffer)
```

引擎名：`load_dds_from_buffer`

Loads an image from the binary contents of a DDS file.

**Note:** This method is only available in engine builds with the DDS module enabled. By default, the DDS module is enabled, but it can be disabled at build-time using the `module_dds_enabled=no` SCons option.

### LoadDdsFromBuffer

```csharp
public Error LoadDdsFromBuffer(byte[] buffer)
```

引擎名：`load_dds_from_buffer`

Loads an image from the binary contents of a DDS file.

**Note:** This method is only available in engine builds with the DDS module enabled. By default, the DDS module is enabled, but it can be disabled at build-time using the `module_dds_enabled=no` SCons option.

### LoadExrFromBuffer

```csharp
public Error LoadExrFromBuffer(ReadOnlySpan<byte> buffer)
```

引擎名：`load_exr_from_buffer`

Loads an image from the binary contents of an OpenEXR file.

### LoadExrFromBuffer

```csharp
public Error LoadExrFromBuffer(byte[] buffer)
```

引擎名：`load_exr_from_buffer`

Loads an image from the binary contents of an OpenEXR file.

### LoadFromFile

```csharp
public static Image LoadFromFile(string path)
```

引擎名：`load_from_file`

Creates a new `Image` and loads data from the specified file.

### LoadJpgFromBuffer

```csharp
public Error LoadJpgFromBuffer(ReadOnlySpan<byte> buffer)
```

引擎名：`load_jpg_from_buffer`

Loads an image from the binary contents of a JPEG file.

### LoadJpgFromBuffer

```csharp
public Error LoadJpgFromBuffer(byte[] buffer)
```

引擎名：`load_jpg_from_buffer`

Loads an image from the binary contents of a JPEG file.

### LoadKtxFromBuffer

```csharp
public Error LoadKtxFromBuffer(ReadOnlySpan<byte> buffer)
```

引擎名：`load_ktx_from_buffer`

Loads an image from the binary contents of a [KTX](https://github.com/KhronosGroup/KTX-Software) file. Unlike most image formats, KTX can store VRAM-compressed data and embed mipmaps.

**Note:** Godot's libktx implementation only supports 2D images. Cubemaps, texture arrays, and de-padding are not supported.

**Note:** This method is only available in engine builds with the KTX module enabled. By default, the KTX module is enabled, but it can be disabled at build-time using the `module_ktx_enabled=no` SCons option.

### LoadKtxFromBuffer

```csharp
public Error LoadKtxFromBuffer(byte[] buffer)
```

引擎名：`load_ktx_from_buffer`

Loads an image from the binary contents of a [KTX](https://github.com/KhronosGroup/KTX-Software) file. Unlike most image formats, KTX can store VRAM-compressed data and embed mipmaps.

**Note:** Godot's libktx implementation only supports 2D images. Cubemaps, texture arrays, and de-padding are not supported.

**Note:** This method is only available in engine builds with the KTX module enabled. By default, the KTX module is enabled, but it can be disabled at build-time using the `module_ktx_enabled=no` SCons option.

### LoadPngFromBuffer

```csharp
public Error LoadPngFromBuffer(ReadOnlySpan<byte> buffer)
```

引擎名：`load_png_from_buffer`

Loads an image from the binary contents of a PNG file.

### LoadPngFromBuffer

```csharp
public Error LoadPngFromBuffer(byte[] buffer)
```

引擎名：`load_png_from_buffer`

Loads an image from the binary contents of a PNG file.

### LoadSvgFromBuffer

```csharp
public Error LoadSvgFromBuffer(ReadOnlySpan<byte> buffer, float scale)
```

引擎名：`load_svg_from_buffer`

Loads an image from the UTF-8 binary contents of an **uncompressed** SVG file (**.svg**).

**Note:** Beware when using compressed SVG files (like **.svgz**), they need to be `decompressed` before loading.

**Note:** This method is only available in engine builds with the SVG module enabled. By default, the SVG module is enabled, but it can be disabled at build-time using the `module_svg_enabled=no` SCons option.

### LoadSvgFromBuffer

```csharp
public Error LoadSvgFromBuffer(byte[] buffer, float scale = 1f)
```

引擎名：`load_svg_from_buffer`

Loads an image from the UTF-8 binary contents of an **uncompressed** SVG file (**.svg**).

**Note:** Beware when using compressed SVG files (like **.svgz**), they need to be `decompressed` before loading.

**Note:** This method is only available in engine builds with the SVG module enabled. By default, the SVG module is enabled, but it can be disabled at build-time using the `module_svg_enabled=no` SCons option.

### LoadSvgFromString

```csharp
public Error LoadSvgFromString(string svgStr, float scale = 1f)
```

引擎名：`load_svg_from_string`

Loads an image from the string contents of an SVG file (**.svg**).

**Note:** This method is only available in engine builds with the SVG module enabled. By default, the SVG module is enabled, but it can be disabled at build-time using the `module_svg_enabled=no` SCons option.

### LoadTgaFromBuffer

```csharp
public Error LoadTgaFromBuffer(ReadOnlySpan<byte> buffer)
```

引擎名：`load_tga_from_buffer`

Loads an image from the binary contents of a TGA file.

**Note:** This method is only available in engine builds with the TGA module enabled. By default, the TGA module is enabled, but it can be disabled at build-time using the `module_tga_enabled=no` SCons option.

### LoadTgaFromBuffer

```csharp
public Error LoadTgaFromBuffer(byte[] buffer)
```

引擎名：`load_tga_from_buffer`

Loads an image from the binary contents of a TGA file.

**Note:** This method is only available in engine builds with the TGA module enabled. By default, the TGA module is enabled, but it can be disabled at build-time using the `module_tga_enabled=no` SCons option.

### LoadWebpFromBuffer

```csharp
public Error LoadWebpFromBuffer(ReadOnlySpan<byte> buffer)
```

引擎名：`load_webp_from_buffer`

Loads an image from the binary contents of a WebP file.

### LoadWebpFromBuffer

```csharp
public Error LoadWebpFromBuffer(byte[] buffer)
```

引擎名：`load_webp_from_buffer`

Loads an image from the binary contents of a WebP file.

### NormalMapToXy

```csharp
public void NormalMapToXy()
```

引擎名：`normal_map_to_xy`

Converts the image's data to represent coordinates on a 3D plane. This is used when the image represents a normal map. A normal map can add lots of detail to a 3D surface without increasing the polygon count.

### PremultiplyAlpha

```csharp
public void PremultiplyAlpha()
```

引擎名：`premultiply_alpha`

Multiplies color values with alpha values. Resulting color values for a pixel are `(color * alpha)/256`. See also [`CanvasItemMaterial.BlendMode`](CanvasItemMaterial.md).

### Resize

```csharp
public void Resize(int width, int height, Image.Interpolation interpolation = Image.Interpolation.Bilinear)
```

引擎名：`resize`

Resizes the image to the given `width` and `height`. New pixels are calculated using the `interpolation` mode defined via [`Image.Interpolation`](Image.md) constants.

**Note:** If the image's format is [`Image.Format.Rgba4444`](Image.md), [`Image.Format.Rgb565`](Image.md), or [`Image.Format.Rgbe9995`](Image.md), it will be temporarily converted to either [`Image.Format.Rgba8`](Image.md) or [`Image.Format.Rgbah`](Image.md). This can affect the quality of the resized image.

### ResizeToPo2

```csharp
public void ResizeToPo2(bool square = false, Image.Interpolation interpolation = Image.Interpolation.Bilinear)
```

引擎名：`resize_to_po2`

Resizes the image to the nearest power of 2 for the width and height. If `square` is `true`, sets width and height to be the same. New pixels are calculated using the `interpolation` mode defined via [`Image.Interpolation`](Image.md) constants.

### RgbeToSrgb

```csharp
public Image RgbeToSrgb()
```

引擎名：`rgbe_to_srgb`

Converts a standard linear RGBE (Red Green Blue Exponent) image to an image that uses nonlinear sRGB encoding.

### Rotate180

```csharp
public void Rotate180()
```

Rotates the image by `180` degrees. The width and height of the image must be greater than `1`.

### Rotate90

```csharp
public void Rotate90(ClockDirection direction)
```

Rotates the image in the specified `direction` by `90` degrees. The width and height of the image must be greater than `1`. If the width and height are not equal, the image will be resized.

### SaveDds

```csharp
public Error SaveDds(string path)
```

引擎名：`save_dds`

Saves the image as a DDS (DirectDraw Surface) file to `path`. DDS is a container format that can store textures in various compression formats, such as DXT1, DXT5, or BC7. This function will return [`Error.Unavailable`](Error.md) if Godot was compiled without the DDS module.

**Note:** The DDS module may be disabled in certain builds, which means `SaveDds()` will return [`Error.Unavailable`](Error.md) when it is called from an exported project.

### SaveDdsToBuffer

```csharp
public byte[] SaveDdsToBuffer()
```

引擎名：`save_dds_to_buffer`

Saves the image as a DDS (DirectDraw Surface) file to a byte array. DDS is a container format that can store textures in various compression formats, such as DXT1, DXT5, or BC7. This function will return an empty byte array if Godot was compiled without the DDS module.

**Note:** The DDS module may be disabled in certain builds, which means `SaveDdsToBuffer()` will return an empty byte array when it is called from an exported project.

### SaveExr

```csharp
public Error SaveExr(string path, bool grayscale = false, bool colorImage = false, float maxLinearValue = -1f)
```

引擎名：`save_exr`

Saves the image as an EXR file to `path`. If `grayscale` is `true` and the image has only one channel, it will be saved explicitly as monochrome rather than one red channel. Set `colorImage` to `true` when saving a color image, such as a screenshot. Negative values will be included when `colorImage` is `false`, which may be useful for saving raw floating point data such as a lightmap that includes negative light information. Color component values in the resulting EXR file will not exceed `maxLinearValue` if `maxLinearValue` is not negative. This function will return [`Error.Unavailable`](Error.md) if Godot was compiled without the TinyEXR module.

When saving screenshots of a project that uses HDR output, use [`Window.GetOutputMaxLinearValue`](Window.md) for `maxLinearValue`.

### SaveExr

```csharp
public Error SaveExr(string path, bool grayscale)
```

引擎名：`save_exr`

Saves the image as an EXR file to `path`. If `grayscale` is `true` and the image has only one channel, it will be saved explicitly as monochrome rather than one red channel. Set `colorImage` to `true` when saving a color image, such as a screenshot. Negative values will be included when `colorImage` is `false`, which may be useful for saving raw floating point data such as a lightmap that includes negative light information. Color component values in the resulting EXR file will not exceed `maxLinearValue` if `maxLinearValue` is not negative. This function will return [`Error.Unavailable`](Error.md) if Godot was compiled without the TinyEXR module.

When saving screenshots of a project that uses HDR output, use [`Window.GetOutputMaxLinearValue`](Window.md) for `maxLinearValue`.

### SaveExrToBuffer

```csharp
public byte[] SaveExrToBuffer(bool grayscale = false, bool colorImage = false, float maxLinearValue = -1f)
```

引擎名：`save_exr_to_buffer`

Saves the image as an EXR file to a byte array. If `grayscale` is `true` and the image has only one channel, it will be saved explicitly as monochrome rather than one red channel. Set `colorImage` to `true` when saving a color image, such as a screenshot. Negative values will be included when `colorImage` is `false`, which may be useful for saving raw floating point data such as a lightmap that includes negative light information. Color component values in the resulting EXR file will not exceed `maxLinearValue` if `maxLinearValue` is not negative. This function will return an empty byte array if Godot was compiled without the TinyEXR module.

When saving screenshots of a project that uses HDR output, use [`Window.GetOutputMaxLinearValue`](Window.md) for `maxLinearValue`.

### SaveExrToBuffer

```csharp
public byte[] SaveExrToBuffer(bool grayscale)
```

引擎名：`save_exr_to_buffer`

Saves the image as an EXR file to a byte array. If `grayscale` is `true` and the image has only one channel, it will be saved explicitly as monochrome rather than one red channel. Set `colorImage` to `true` when saving a color image, such as a screenshot. Negative values will be included when `colorImage` is `false`, which may be useful for saving raw floating point data such as a lightmap that includes negative light information. Color component values in the resulting EXR file will not exceed `maxLinearValue` if `maxLinearValue` is not negative. This function will return an empty byte array if Godot was compiled without the TinyEXR module.

When saving screenshots of a project that uses HDR output, use [`Window.GetOutputMaxLinearValue`](Window.md) for `maxLinearValue`.

### SaveJpg

```csharp
public Error SaveJpg(string path, float quality = 0.75f)
```

引擎名：`save_jpg`

Saves the image as a JPEG file to `path` with the specified `quality` between `0.01` and `1.0` (inclusive). Higher `quality` values result in better-looking output at the cost of larger file sizes. Recommended `quality` values are between `0.75` and `0.90`. Even at quality `1.00`, JPEG compression remains lossy.

**Note:** JPEG does not save an alpha channel. If the `Image` contains an alpha channel, the image will still be saved, but the resulting JPEG file won't contain the alpha channel.

### SaveJpgToBuffer

```csharp
public byte[] SaveJpgToBuffer(float quality = 0.75f)
```

引擎名：`save_jpg_to_buffer`

Saves the image as a JPEG file to a byte array with the specified `quality` between `0.01` and `1.0` (inclusive). Higher `quality` values result in better-looking output at the cost of larger byte array sizes (and therefore memory usage). Recommended `quality` values are between `0.75` and `0.90`. Even at quality `1.00`, JPEG compression remains lossy.

**Note:** JPEG does not save an alpha channel. If the `Image` contains an alpha channel, the image will still be saved, but the resulting byte array won't contain the alpha channel.

### SavePng

```csharp
public Error SavePng(string path)
```

引擎名：`save_png`

Saves the image as a PNG file to the file at `path`.

### SavePngToBuffer

```csharp
public byte[] SavePngToBuffer()
```

引擎名：`save_png_to_buffer`

Saves the image as a PNG file to a byte array.

### SaveWebp

```csharp
public Error SaveWebp(string path, bool lossy = false, float quality = 0.75f)
```

引擎名：`save_webp`

Saves the image as a WebP (Web Picture) file to the file at `path`. By default it will save lossless. If `lossy` is `true`, the image will be saved lossy, using the `quality` setting between `0.0` and `1.0` (inclusive). Lossless WebP offers more efficient compression than PNG.

**Note:** The WebP format is limited to a size of 16383×16383 pixels, while PNG can save larger images.

### SaveWebpToBuffer

```csharp
public byte[] SaveWebpToBuffer(bool lossy = false, float quality = 0.75f)
```

引擎名：`save_webp_to_buffer`

Saves the image as a WebP (Web Picture) file to a byte array. By default it will save lossless. If `lossy` is `true`, the image will be saved lossy, using the `quality` setting between `0.0` and `1.0` (inclusive). Lossless WebP offers more efficient compression than PNG.

**Note:** The WebP format is limited to a size of 16383×16383 pixels, while PNG can save larger images.

### SetData

```csharp
public void SetData(int width, int height, bool useMipmaps, Image.Format format, ReadOnlySpan<byte> data)
```

引擎名：`set_data`

Overwrites data of an existing `Image`. Non-static equivalent of `CreateFromData()`.

### SetData

```csharp
public void SetData(int width, int height, bool useMipmaps, Image.Format format, byte[] data)
```

引擎名：`set_data`

Overwrites data of an existing `Image`. Non-static equivalent of `CreateFromData()`.

### SetPixel

```csharp
public void SetPixel(int x, int y, Color color)
```

引擎名：`set_pixel`

Sets the [`Color`](Color.md) of the pixel at `(x, y)` to `color`.

```csharp
int imgWidth = 10;
int imgHeight = 5;
var img = Image.Create(imgWidth, imgHeight, false, Image.Format.Rgba8);

img.SetPixel(1, 2, Colors.Red); // Sets the color at (1, 2) to red.

```

This is the same as `SetPixelv()`, but with a two integer arguments instead of a [`Vector2I`](Vector2I.md) argument.

**Note:** Depending on the image's format, the color set here may be clamped or lose precision. Do not assume the color returned by `GetPixel()` to be identical to the one set here; any comparisons will likely need to use an approximation like `Color.IsEqualApprox`.

**Note:** On grayscale image formats, only the red channel of `color` is used (and alpha if relevant). The green and blue channels are ignored.

### SetPixelv

```csharp
public void SetPixelv(Vector2I point, Color color)
```

引擎名：`set_pixelv`

Sets the [`Color`](Color.md) of the pixel at `point` to `color`.

```csharp
int imgWidth = 10;
int imgHeight = 5;
var img = Image.Create(imgWidth, imgHeight, false, Image.Format.Rgba8);

img.SetPixelv(new Vector2I(1, 2), Colors.Red); // Sets the color at (1, 2) to red.

```

This is the same as `SetPixel()`, but with a [`Vector2I`](Vector2I.md) argument instead of two integer arguments.

**Note:** Depending on the image's format, the color set here may be clamped or lose precision. Do not assume the color returned by `GetPixelv()` to be identical to the one set here; any comparisons will likely need to use an approximation like `Color.IsEqualApprox`.

**Note:** On grayscale image formats, only the red channel of `color` is used (and alpha if relevant). The green and blue channels are ignored.

### ShrinkX2

```csharp
public void ShrinkX2()
```

引擎名：`shrink_x2`

Shrinks the image by a factor of 2 on each axis (this divides the pixel count by 4).

### SrgbToLinear

```csharp
public void SrgbToLinear()
```

引擎名：`srgb_to_linear`

Converts the raw data from nonlinear sRGB encoding to linear encoding using a lookup table. Only works on images with [`Image.Format.Rgb8`](Image.md) or [`Image.Format.Rgba8`](Image.md) formats.

**Note:** The 8-bit formats required by this method are not suitable for storing linearly encoded values; a significant amount of color information will be lost in darker values. To maintain image quality, this method should not be used.

## 常量

### MaxHeight

```csharp
public const long MaxHeight = 16777216
```

The maximal height allowed for `Image` resources.

### MaxWidth

```csharp
public const long MaxWidth = 16777216
```

The maximal width allowed for `Image` resources.

## AlphaMode

命名空间 `Godot`；enum

### 枚举值

- `None = 0`：Image is fully opaque.
- `Bit = 1`：Image stores either fully opaque or fully transparent pixels.
- `Blend = 2`：Image stores alpha data with values varying between `0.0` and `1.0`.

## AstcFormat

命名空间 `Godot`；enum

### 枚举值

- `Format4X4 = 0`：Hint to indicate that the high quality 4×4 ASTC compression format should be used.
- `Format8X8 = 1`：Hint to indicate that the low quality 8×8 ASTC compression format should be used.

## CompressMode

命名空间 `Godot`；enum

### 枚举值

- `S3Tc = 0`：Use S3TC compression.
- `Etc = 1`：Use ETC compression.
- `Etc2 = 2`：Use ETC2 compression.
- `Bptc = 3`：Use BPTC compression.
- `Astc = 4`：Use ASTC compression.
- `Max = 5`：Represents the size of the `Image.CompressMode` enum.

## CompressSource

命名空间 `Godot`；enum

### 枚举值

- `Generic = 0`：Source texture (before compression) is a regular texture.
- `Srgb = 1`：Source texture (before compression) uses nonlinear sRGB encoding.
- `Normal = 2`：Source texture (before compression) is a normal texture (e.g.

## Format

命名空间 `Godot`；enum

### 枚举值

- `L8 = 0`：Texture format with a single 8-bit depth representing luminance.
- `La8 = 1`：OpenGL texture format with two values, luminance and alpha each stored with 8 bits.
- `R8 = 2`：OpenGL texture format `RED` with a single component and a bitdepth of 8.
- `Rg8 = 3`：OpenGL texture format `RG` with two components and a bitdepth of 8 for each.
- `Rgb8 = 4`：OpenGL texture format `RGB` with three components, each with a bitdepth of 8.
- `Rgba8 = 5`：OpenGL texture format `RGBA` with four components, each with a bitdepth of 8.
- `Rgba4444 = 6`：OpenGL texture format `RGBA` with four components, each with a bitdepth of 4.
- `Rgb565 = 7`：OpenGL texture format `RGB` with three components.
- `Rf = 8`：OpenGL texture format `GL_R32F` where there's one component, a 32-bit floating-point value.
- `Rgf = 9`：OpenGL texture format `GL_RG32F` where there are two components, each a 32-bit floating-point values.
- `Rgbf = 10`：OpenGL texture format `GL_RGB32F` where there are three components, each a 32-bit floating-point values.
- `Rgbaf = 11`：OpenGL texture format `GL_RGBA32F` where there are four components, each a 32-bit floating-point values.
- `Rh = 12`：OpenGL texture format `GL_R16F` where there's one component, a 16-bit "half-precision" floating-point value.
- `Rgh = 13`：OpenGL texture format `GL_RG16F` where there are two components, each a 16-bit "half-precision" floating-point value.
- `Rgbh = 14`：OpenGL texture format `GL_RGB16F` where there are three components, each a 16-bit "half-precision" floating-point value.
- `Rgbah = 15`：OpenGL texture format `GL_RGBA16F` where there are four components, each a 16-bit "half-precision" floating-point value.
- `Rgbe9995 = 16`：A special OpenGL texture format where the three color components have 9 bits of precision and all three share a single 5-bit exponent.
- `Dxt1 = 17`：The [S3TC](https://en.wikipedia.org/wiki/S3_Texture_Compression) texture format that uses Block Compression 1, and is the smallest variation of S3TC, only providing 1 bit of alpha and color data being premultiplied with …
- `Dxt3 = 18`：The [S3TC](https://en.wikipedia.org/wiki/S3_Texture_Compression) texture format that uses Block Compression 2, and color data is interpreted as not having been premultiplied by alpha.
- `Dxt5 = 19`：The [S3TC](https://en.wikipedia.org/wiki/S3_Texture_Compression) texture format also known as Block Compression 3 or BC3 that contains 64 bits of alpha channel data followed by 64 bits of DXT1-encoded color data.
- `RgtcR = 20`：Texture format that uses [Red Green Texture Compression](https://www.khronos.org/opengl/wiki/Red_Green_Texture_Compression), normalizing the red channel data using the same compression algorithm that DXT5 uses for the al…
- `RgtcRg = 21`：Texture format that uses [Red Green Texture Compression](https://www.khronos.org/opengl/wiki/Red_Green_Texture_Compression), normalizing the red and green channel data using the same compression algorithm that DXT5 uses …
- `BptcRgba = 22`：Texture format that uses [BPTC](https://www.khronos.org/opengl/wiki/BPTC_Texture_Compression) compression with unsigned normalized RGBA components.
- `BptcRgbf = 23`：Texture format that uses [BPTC](https://www.khronos.org/opengl/wiki/BPTC_Texture_Compression) compression with signed floating-point RGB components.
- `BptcRgbfu = 24`：Texture format that uses [BPTC](https://www.khronos.org/opengl/wiki/BPTC_Texture_Compression) compression with unsigned floating-point RGB components.
- `Etc = 25`：[Ericsson Texture Compression format 1](https://en.wikipedia.org/wiki/Ericsson_Texture_Compression#ETC1), also referred to as "ETC1", and is part of the OpenGL ES graphics standard.
- `Etc2R11 = 26`：[Ericsson Texture Compression format 2](https://en.wikipedia.org/wiki/Ericsson_Texture_Compression#ETC2_and_EAC) (`R11_EAC` variant), which provides one channel of unsigned data.
- `Etc2R11S = 27`：[Ericsson Texture Compression format 2](https://en.wikipedia.org/wiki/Ericsson_Texture_Compression#ETC2_and_EAC) (`SIGNED_R11_EAC` variant), which provides one channel of signed data.
- `Etc2Rg11 = 28`：[Ericsson Texture Compression format 2](https://en.wikipedia.org/wiki/Ericsson_Texture_Compression#ETC2_and_EAC) (`RG11_EAC` variant), which provides two channels of unsigned data.
- `Etc2Rg11S = 29`：[Ericsson Texture Compression format 2](https://en.wikipedia.org/wiki/Ericsson_Texture_Compression#ETC2_and_EAC) (`SIGNED_RG11_EAC` variant), which provides two channels of signed data.
- `Etc2Rgb8 = 30`：[Ericsson Texture Compression format 2](https://en.wikipedia.org/wiki/Ericsson_Texture_Compression#ETC2_and_EAC) (`RGB8` variant), which is a follow-up of ETC1 and compresses RGB888 data.
- `Etc2Rgba8 = 31`：[Ericsson Texture Compression format 2](https://en.wikipedia.org/wiki/Ericsson_Texture_Compression#ETC2_and_EAC) (`RGBA8`variant), which compresses RGBA8888 data with full alpha support.
- `Etc2Rgb8A1 = 32`：[Ericsson Texture Compression format 2](https://en.wikipedia.org/wiki/Ericsson_Texture_Compression#ETC2_and_EAC) (`RGB8_PUNCHTHROUGH_ALPHA1` variant), which compresses RGBA data to make alpha either fully transparent or …
- `Etc2RaAsRg = 33`：[Ericsson Texture Compression format 2](https://en.wikipedia.org/wiki/Ericsson_Texture_Compression#ETC2_and_EAC) (`RGBA8` variant), which compresses RA data and interprets it as two channels (red and green).
- `Dxt5RaAsRg = 34`：The [S3TC](https://en.wikipedia.org/wiki/S3_Texture_Compression) texture format also known as Block Compression 3 or BC3, which compresses RA data and interprets it as two channels (red and green).
- `Astc4X4 = 35`：[Adaptive Scalable Texture Compression](https://en.wikipedia.org/wiki/Adaptive_scalable_texture_compression).
- `Astc4X4Hdr = 36`：Same format as `Astc4X4`, but with the hint to let the GPU know it is used for HDR.
- `Astc8X8 = 37`：[Adaptive Scalable Texture Compression](https://en.wikipedia.org/wiki/Adaptive_scalable_texture_compression).
- `Astc8X8Hdr = 38`：Same format as `Astc8X8`, but with the hint to let the GPU know it is used for HDR.
- `R16 = 39`：OpenGL texture format `GL_R16` where there's one component, a 16-bit unsigned normalized integer value.
- `Rg16 = 40`：OpenGL texture format `GL_RG16` where there are two components, each a 16-bit unsigned normalized integer value.
- `Rgb16 = 41`：OpenGL texture format `GL_RGB16` where there are three components, each a 16-bit unsigned normalized integer value.
- `Rgba16 = 42`：OpenGL texture format `GL_RGBA16` where there are four components, each a 16-bit unsigned normalized integer value.
- `R16I = 43`：OpenGL texture format `GL_R16UI` where there's one component, a 16-bit unsigned integer value.
- `Rg16I = 44`：OpenGL texture format `GL_RG16UI` where there are two components, each a 16-bit unsigned integer value.
- `Rgb16I = 45`：OpenGL texture format `GL_RGB16UI` where there are three components, each a 16-bit unsigned integer value.
- `Rgba16I = 46`：OpenGL texture format `GL_RGBA16UI` where there are four components, each a 16-bit unsigned integer value.
- `Max = 47`：Represents the size of the `Image.Format` enum.

## Interpolation

命名空间 `Godot`；enum

### 枚举值

- `Nearest = 0`：Performs nearest-neighbor interpolation.
- `Bilinear = 1`：Performs bilinear interpolation.
- `Cubic = 2`：Performs cubic interpolation.
- `Trilinear = 3`：Performs bilinear separately on the two most-suited mipmap levels, then linearly interpolates between them.
- `Lanczos = 4`：Performs Lanczos interpolation.

## UsedChannels

命名空间 `Godot`；enum

### 枚举值

- `L = 0`：The image only uses one channel for luminance (grayscale).
- `La = 1`：The image uses two channels for luminance and alpha, respectively.
- `R = 2`：The image only uses the red channel.
- `Rg = 3`：The image uses two channels for red and green.
- `Rgb = 4`：The image uses three channels for red, green, and blue.
- `Rgba = 5`：The image uses four channels for red, green, blue, and alpha.

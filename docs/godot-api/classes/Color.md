<!-- 由 scripts/godot-api/gen-api-docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# Color

命名空间 `Godot`；struct

A color represented by red, green, blue, and alpha (RGBA) components.
The alpha component is often used for transparency.
Values are in floating-point and usually range from 0 to 1.
Some properties (such as [`CanvasItem.Modulate`](CanvasItem.md)) may accept values
greater than 1 (overbright or HDR colors).

If you want to supply values in a range of 0 to 255, you should use
`Color8()` and the `r8`/`g8`/`b8`/`a8` properties.

## 构造函数

### Color

```csharp
public Color(Color c, float a = 1f)
```

Constructs a `Color` from an existing color and an alpha value.

参数：
- `c`：The color to construct from. Only its RGB values are used.
- `a`：The color's alpha value, typically on the range of 0 to 1.
A value of 0 means that the color is fully transparent.
A value of 1 means that the color is fully opaque.

### Color

```csharp
public Color(float r, float g, float b, float a = 1f)
```

Constructs a `Color` from RGBA values, typically on the range of 0 to 1.

参数：
- `r`：The color's red component, typically on the range of 0 to 1.
- `g`：The color's green component, typically on the range of 0 to 1.
- `b`：The color's blue component, typically on the range of 0 to 1.
- `a`：The color's alpha value, typically on the range of 0 to 1.
A value of 0 means that the color is fully transparent.
A value of 1 means that the color is fully opaque.

### Color

```csharp
public Color(string code)
```

Constructs a `Color` either from an HTML color code or from a
standardized color name. Supported color names are the same as the
[`Colors`](Colors.md) constants.

参数：
- `code`：The HTML color code or color name to construct from.

### Color

```csharp
public Color(string code, float alpha)
```

Constructs a `Color` either from an HTML color code or from a
standardized color name, with `alpha` on the range of 0 to 1. Supported
color names are the same as the [`Colors`](Colors.md) constants.

参数：
- `code`：The HTML color code or color name to construct from.
- `alpha`：The alpha (transparency) value, typically on the range of 0 to 1.

### Color

```csharp
public Color(uint rgba)
```

Constructs a `Color` from an unsigned 32-bit integer in RGBA format
(each byte represents a color channel).

参数：
- `rgba`：The `uint` representing the color as 0xRRGGBBAA.

### Color

```csharp
public Color(ulong rgba)
```

Constructs a `Color` from an unsigned 64-bit integer in RGBA format
(each word represents a color channel).

参数：
- `rgba`：The `ulong` representing the color as 0xRRRRGGGGBBBBAAAA.

## 属性

### A8

```csharp
public int A8 { get; set; }
```

引擎名：`a8`

Wrapper for `A` that uses the range 0 to 255 instead of 0 to 1.

### B8

```csharp
public int B8 { get; set; }
```

引擎名：`b8`

Wrapper for `B` that uses the range 0 to 255 instead of 0 to 1.

### G8

```csharp
public int G8 { get; set; }
```

引擎名：`g8`

Wrapper for `G` that uses the range 0 to 255 instead of 0 to 1.

### H

```csharp
public float H { get; set; }
```

引擎名：`h`

The HSV hue of this color, on the range 0 to 1.

### Item

```csharp
public float this[int index] { get; set; }
```

Access color components using their index.

### Luminance

```csharp
public float Luminance { get; }
```

Returns the light intensity of the color, as a value between 0.0 and 1.0 (inclusive).
This is useful when determining light or dark color. Colors with a luminance smaller
than 0.5 can be generally considered dark.
Note: `Luminance` relies on the color being in the linear color space to
return an accurate relative luminance value. If the color is in the sRGB color space
use `SrgbToLinear()` to convert it to the linear color space first.

### OkHslH

```csharp
public float OkHslH { get; set; }
```

引擎名：`ok_hsl_h`

The OKHSL hue of this color, on the range 0 to 1.

### OkHslL

```csharp
public float OkHslL { get; set; }
```

引擎名：`ok_hsl_l`

The OKHSL lightness of this color, on the range 0 to 1.

### OkHslS

```csharp
public float OkHslS { get; set; }
```

引擎名：`ok_hsl_s`

The OKHSL saturation of this color, on the range 0 to 1.

### R8

```csharp
public int R8 { get; set; }
```

引擎名：`r8`

Wrapper for `R` that uses the range 0 to 255 instead of 0 to 1.

### S

```csharp
public float S { get; set; }
```

引擎名：`s`

The HSV saturation of this color, on the range 0 to 1.

### V

```csharp
public float V { get; set; }
```

引擎名：`v`

The HSV value (brightness) of this color, on the range 0 to 1.

## 方法

### Blend

```csharp
public Color Blend(Color over)
```

引擎名：`blend`

Returns a new color resulting from blending this color over another.
If the color is opaque, the result is also opaque.
The second color may have a range of alpha values.

参数：
- `over`：The color to blend over.

返回：This color blended over `over`.

### Clamp

```csharp
public Color Clamp(Color? min = null, Color? max = null)
```

引擎名：`clamp`

Returns a new color with all components clamped between the
components of `min` and `max`
using [`Mathf.Clamp`](Mathf.md).

参数：
- `min`：The color with minimum allowed values.
- `max`：The color with maximum allowed values.

返回：The color with all components clamped.

### Color8

```csharp
public static Color Color8(byte r8, byte g8, byte b8, byte a8 = 255)
```

Returns a color constructed from integer red, green, blue, and alpha channels.
Each channel should have 8 bits of information ranging from 0 to 255.

参数：
- `r8`：The red component represented on the range of 0 to 255.
- `g8`：The green component represented on the range of 0 to 255.
- `b8`：The blue component represented on the range of 0 to 255.
- `a8`：The alpha (transparency) component represented on the range of 0 to 255.

返回：The constructed color.

### Darkened

```csharp
public Color Darkened(float amount)
```

引擎名：`darkened`

Returns a new color resulting from making this color darker
by the specified ratio (on the range of 0 to 1).

参数：
- `amount`：The ratio to darken by.

返回：The darkened color.

### Equals

```csharp
public bool Equals(Color other)
```

Returns `true` if the colors are exactly equal.
Note: Due to floating-point precision errors, consider using
`IsEqualApprox()` instead, which is more reliable.

参数：
- `other`：The other color.

返回：Whether or not the colors are equal.

### Equals

```csharp
public virtual bool Equals(object obj)
```

Returns `true` if this color and `obj` are equal.

参数：
- `obj`：The other object to compare.

返回：Whether or not the color and the other object are equal.

### FromHsv

```csharp
public static Color FromHsv(float hue, float saturation, float value, float alpha = 1f)
```

引擎名：`from_hsv`

Constructs a color from an HSV profile. The `hue`,
`saturation`, and `value` are typically
between 0.0 and 1.0.

参数：
- `hue`：The HSV hue, typically on the range of 0 to 1.
- `saturation`：The HSV saturation, typically on the range of 0 to 1.
- `value`：The HSV value (brightness), typically on the range of 0 to 1.
- `alpha`：The alpha (transparency) value, typically on the range of 0 to 1.

返回：The constructed color.

### FromHtml

```csharp
public static Color FromHtml(ReadOnlySpan<char> rgba)
```

Constructs a `Color` from the HTML hexadecimal color string in RGBA format.

参数：
- `rgba`：A string for the HTML hexadecimal representation of this color.

### FromOkHsl

```csharp
public static Color FromOkHsl(float hue, float saturation, float lightness, float alpha = 1f)
```

引擎名：`from_ok_hsl`

Constructs a color from an OK HSL profile. The `hue`,
`saturation`, and `lightness` are typically
between 0.0 and 1.0.

参数：
- `hue`：The OK HSL hue, typically on the range of 0 to 1.
- `saturation`：The OK HSL saturation, typically on the range of 0 to 1.
- `lightness`：The OK HSL lightness, typically on the range of 0 to 1.
- `alpha`：The alpha (transparency) value, typically on the range of 0 to 1.

返回：The constructed color.

### FromRgbe9995

```csharp
public static Color FromRgbe9995(uint rgbe)
```

引擎名：`from_rgbe9995`

Encodes a `Color` from a RGBE9995 format integer.
See [`Image.Format.Rgbe9995`](Image.md).

参数：
- `rgbe`：The RGBE9995 encoded color.

返回：The constructed color.

### FromString

```csharp
public static Color FromString(string str, Color default)
```

引擎名：`from_string`

Constructs a color from the given string, which can be either an HTML color
code or a named color. Returns `default` if the color cannot
be inferred from the string. Supported color names are the same as the
[`Colors`](Colors.md) constants.

参数：
- `str`：The HTML color code or color name.
- `default`：The fallback color to return if the color cannot be inferred.

返回：The constructed color.

### GetHashCode

```csharp
public virtual int GetHashCode()
```

Serves as the hash function for `Color`.

返回：A hash code for this color.

### HtmlIsValid

```csharp
public static bool HtmlIsValid(ReadOnlySpan<char> color)
```

引擎名：`html_is_valid`

Returns `true` if `color` is a valid HTML hexadecimal
color string. The string must be a hexadecimal value (case-insensitive) of either 3,
4, 6 or 8 digits, and may be prefixed by a hash sign (`#`). This method is
identical to [`StringExtensions.IsValidHtmlColor`](StringExtensions.md).

参数：
- `color`：The HTML hexadecimal color string.

返回：Whether or not the string was a valid HTML hexadecimal color string.

### Inverted

```csharp
public Color Inverted()
```

引擎名：`inverted`

Returns the inverted color: `(1 - r, 1 - g, 1 - b, a)`.

返回：The inverted color.

### IsEqualApprox

```csharp
public bool IsEqualApprox(Color other)
```

引擎名：`is_equal_approx`

Returns `true` if this color and `other` are approximately equal,
by running [`Mathf.IsEqualApprox`](Mathf.md) on each component.

参数：
- `other`：The other color to compare.

返回：Whether or not the colors are approximately equal.

### Lerp

```csharp
public Color Lerp(Color to, float weight)
```

引擎名：`lerp`

Returns the result of the linear interpolation between
this color and `to` by amount `weight`.

参数：
- `to`：The destination color for interpolation.
- `weight`：A value on the range of 0.0 to 1.0, representing the amount of interpolation.

返回：The resulting color of the interpolation.

### Lightened

```csharp
public Color Lightened(float amount)
```

引擎名：`lightened`

Returns a new color resulting from making this color lighter
by the specified ratio (on the range of 0 to 1).

参数：
- `amount`：The ratio to lighten by.

返回：The lightened color.

### LinearToSrgb

```csharp
public Color LinearToSrgb()
```

引擎名：`linear_to_srgb`

Returns the color converted to the sRGB color space.
This method assumes the original color is in the linear color space.
See also `SrgbToLinear()` which performs the opposite operation.

返回：The sRGB color.

### SrgbToLinear

```csharp
public Color SrgbToLinear()
```

引擎名：`srgb_to_linear`

Returns the color converted to linear color space.
This method assumes the original color already is in sRGB color space.
See also `LinearToSrgb()` which performs the opposite operation.

返回：The color in linear color space.

### ToAbgr32

```csharp
public uint ToAbgr32()
```

引擎名：`to_abgr32`

Returns the color converted to an unsigned 32-bit integer in ABGR
format (each byte represents a color channel).
ABGR is the reversed version of the default format.

返回：A `uint` representing this color in ABGR32 format.

### ToAbgr64

```csharp
public ulong ToAbgr64()
```

引擎名：`to_abgr64`

Returns the color converted to an unsigned 64-bit integer in ABGR
format (each word represents a color channel).
ABGR is the reversed version of the default format.

返回：A `ulong` representing this color in ABGR64 format.

### ToArgb32

```csharp
public uint ToArgb32()
```

引擎名：`to_argb32`

Returns the color converted to an unsigned 32-bit integer in ARGB
format (each byte represents a color channel).
ARGB is more compatible with DirectX, but not used much in Godot.

返回：A `uint` representing this color in ARGB32 format.

### ToArgb64

```csharp
public ulong ToArgb64()
```

引擎名：`to_argb64`

Returns the color converted to an unsigned 64-bit integer in ARGB
format (each word represents a color channel).
ARGB is more compatible with DirectX, but not used much in Godot.

返回：A `ulong` representing this color in ARGB64 format.

### ToHsv

```csharp
public void ToHsv(out float hue, out float saturation, out float value)
```

Converts a color to HSV values. This is equivalent to using each of
the `h`/`s`/`v` properties, but much more efficient.

参数：
- `hue`：Output parameter for the HSV hue.
- `saturation`：Output parameter for the HSV saturation.
- `value`：Output parameter for the HSV value.

### ToHtml

```csharp
public string ToHtml(bool includeAlpha = true)
```

引擎名：`to_html`

Returns the color's HTML hexadecimal color string in RGBA format.

参数：
- `includeAlpha`：Whether or not to include alpha. If `false`, the color is RGB instead of RGBA.

返回：A string for the HTML hexadecimal representation of this color.

### ToRgba32

```csharp
public uint ToRgba32()
```

引擎名：`to_rgba32`

Returns the color converted to an unsigned 32-bit integer in RGBA
format (each byte represents a color channel).
RGBA is Godot's default and recommended format.

返回：A `uint` representing this color in RGBA32 format.

### ToRgba64

```csharp
public ulong ToRgba64()
```

引擎名：`to_rgba64`

Returns the color converted to an unsigned 64-bit integer in RGBA
format (each word represents a color channel).
RGBA is Godot's default and recommended format.

返回：A `ulong` representing this color in RGBA64 format.

### ToString

```csharp
public string ToString(string format)
```

Converts this `Color` to a string with the given `format`.

返回：A string representation of this color.

### ToString

```csharp
public virtual string ToString()
```

Converts this `Color` to a string.

返回：A string representation of this color.

## 运算符

### op_Addition

```csharp
public static Color op_Addition(Color left, Color right)
```

Adds each component of the `Color`
with the components of the given `Color`.

参数：
- `left`：The left color.
- `right`：The right color.

返回：The added color.

### op_Division

```csharp
public static Color op_Division(Color color, float scale)
```

Divides each component of the `Color`
by the given `float`.

参数：
- `color`：The dividend vector.
- `scale`：The divisor value.

返回：The divided color.

### op_Division

```csharp
public static Color op_Division(Color left, Color right)
```

Divides each component of the `Color`
by the components of the given `Color`.

参数：
- `left`：The dividend color.
- `right`：The divisor color.

返回：The divided color.

### op_Equality

```csharp
public static bool op_Equality(Color left, Color right)
```

Returns `true` if the colors are exactly equal.
Note: Due to floating-point precision errors, consider using
`IsEqualApprox()` instead, which is more reliable.

参数：
- `left`：The left color.
- `right`：The right color.

返回：Whether or not the colors are equal.

### op_GreaterThan

```csharp
public static bool op_GreaterThan(Color left, Color right)
```

Compares two `Color`s by first checking if
the red value of the `left` color is greater than
the red value of the `right` color.
If the red values are exactly equal, then it repeats this check
with the green values of the two colors, then with the blue values,
and then with the alpha value.
This operator is useful for sorting colors.

参数：
- `left`：The left color.
- `right`：The right color.

返回：Whether or not the left is greater than the right.

### op_GreaterThanOrEqual

```csharp
public static bool op_GreaterThanOrEqual(Color left, Color right)
```

Compares two `Color`s by first checking if
the red value of the `left` color is greater than
or equal to the red value of the `right` color.
If the red values are exactly equal, then it repeats this check
with the green values of the two colors, then with the blue values,
and then with the alpha value.
This operator is useful for sorting colors.

参数：
- `left`：The left color.
- `right`：The right color.

返回：Whether or not the left is greater than or equal to the right.

### op_Inequality

```csharp
public static bool op_Inequality(Color left, Color right)
```

Returns `true` if the colors are not equal.
Note: Due to floating-point precision errors, consider using
`IsEqualApprox()` instead, which is more reliable.

参数：
- `left`：The left color.
- `right`：The right color.

返回：Whether or not the colors are equal.

### op_LessThan

```csharp
public static bool op_LessThan(Color left, Color right)
```

Compares two `Color`s by first checking if
the red value of the `left` color is less than
the red value of the `right` color.
If the red values are exactly equal, then it repeats this check
with the green values of the two colors, then with the blue values,
and then with the alpha value.
This operator is useful for sorting colors.

参数：
- `left`：The left color.
- `right`：The right color.

返回：Whether or not the left is less than the right.

### op_LessThanOrEqual

```csharp
public static bool op_LessThanOrEqual(Color left, Color right)
```

Compares two `Color`s by first checking if
the red value of the `left` color is less than
or equal to the red value of the `right` color.
If the red values are exactly equal, then it repeats this check
with the green values of the two colors, then with the blue values,
and then with the alpha value.
This operator is useful for sorting colors.

参数：
- `left`：The left color.
- `right`：The right color.

返回：Whether or not the left is less than or equal to the right.

### op_Multiply

```csharp
public static Color op_Multiply(Color color, float scale)
```

Multiplies each component of the `Color`
by the given `float`.

参数：
- `color`：The color to multiply.
- `scale`：The value to multiply by.

返回：The multiplied color.

### op_Multiply

```csharp
public static Color op_Multiply(Color left, Color right)
```

Multiplies each component of the `Color`
by the components of the given `Color`.

参数：
- `left`：The left color.
- `right`：The right color.

返回：The multiplied color.

### op_Multiply

```csharp
public static Color op_Multiply(float scale, Color color)
```

Multiplies each component of the `Color`
by the given `float`.

参数：
- `scale`：The value to multiply by.
- `color`：The color to multiply.

返回：The multiplied color.

### op_Subtraction

```csharp
public static Color op_Subtraction(Color left, Color right)
```

Subtracts each component of the `Color`
by the components of the given `Color`.

参数：
- `left`：The left color.
- `right`：The right color.

返回：The subtracted color.

### op_UnaryNegation

```csharp
public static Color op_UnaryNegation(Color color)
```

Inverts the given color. This is equivalent to
`Colors.White - c` or
`new Color(1 - c.R, 1 - c.G, 1 - c.B, 1 - c.A)`.

参数：
- `color`：The color to invert.

返回：The inverted color.

## 字段

### A

```csharp
public float A
```

The color's alpha component, typically on the range of 0 to 1.
A value of 0 means that the color is fully transparent.
A value of 1 means that the color is fully opaque.

### B

```csharp
public float B
```

The color's blue component, typically on the range of 0 to 1.

### G

```csharp
public float G
```

The color's green component, typically on the range of 0 to 1.

### R

```csharp
public float R
```

The color's red component, typically on the range of 0 to 1.

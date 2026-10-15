<!-- 由 scripts/godot-api/gen_api_docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# Variant

命名空间 `Godot`；struct

## 属性

### Obj

```csharp
public object Obj { get; }
```

### VariantType

```csharp
public Variant.Type VariantType { get; }
```

## 方法

### As

```csharp
public T As<T>()
```

### AsAabb

```csharp
public Aabb AsAabb()
```

### AsBasis

```csharp
public Basis AsBasis()
```

### AsBool

```csharp
public bool AsBool()
```

### AsByte

```csharp
public byte AsByte()
```

### AsByteArray

```csharp
public byte[] AsByteArray()
```

### AsCallable

```csharp
public Callable AsCallable()
```

### AsChar

```csharp
public char AsChar()
```

### AsColor

```csharp
public Color AsColor()
```

### AsColorArray

```csharp
public Color[] AsColorArray()
```

### AsDouble

```csharp
public double AsDouble()
```

### AsFloat32Array

```csharp
public float[] AsFloat32Array()
```

### AsFloat64Array

```csharp
public double[] AsFloat64Array()
```

### AsGodotArray

```csharp
public Array AsGodotArray()
```

### AsGodotArray

```csharp
public Array<T> AsGodotArray<T>()
```

### AsGodotDictionary

```csharp
public Dictionary AsGodotDictionary()
```

### AsGodotDictionary

```csharp
public Dictionary<TKey, TValue> AsGodotDictionary<TKey, TValue>()
```

### AsGodotObject

```csharp
public GodotObject AsGodotObject()
```

### AsGodotObjectArray

```csharp
public T[] AsGodotObjectArray<T>()
```

### AsInt16

```csharp
public short AsInt16()
```

### AsInt32

```csharp
public int AsInt32()
```

### AsInt32Array

```csharp
public int[] AsInt32Array()
```

### AsInt64

```csharp
public long AsInt64()
```

### AsInt64Array

```csharp
public long[] AsInt64Array()
```

### AsNodePath

```csharp
public NodePath AsNodePath()
```

### AsPlane

```csharp
public Plane AsPlane()
```

### AsProjection

```csharp
public Projection AsProjection()
```

### AsQuaternion

```csharp
public Quaternion AsQuaternion()
```

### AsRect2

```csharp
public Rect2 AsRect2()
```

### AsRect2I

```csharp
public Rect2I AsRect2I()
```

### AsRid

```csharp
public Rid AsRid()
```

### AsSByte

```csharp
public sbyte AsSByte()
```

### AsSignal

```csharp
public Signal AsSignal()
```

### AsSingle

```csharp
public float AsSingle()
```

### AsString

```csharp
public string AsString()
```

### AsStringArray

```csharp
public string[] AsStringArray()
```

### AsStringName

```csharp
public StringName AsStringName()
```

### AsSystemArrayOfNodePath

```csharp
public NodePath[] AsSystemArrayOfNodePath()
```

### AsSystemArrayOfRid

```csharp
public Rid[] AsSystemArrayOfRid()
```

### AsSystemArrayOfStringName

```csharp
public StringName[] AsSystemArrayOfStringName()
```

### AsTransform2D

```csharp
public Transform2D AsTransform2D()
```

### AsTransform3D

```csharp
public Transform3D AsTransform3D()
```

### AsUInt16

```csharp
public ushort AsUInt16()
```

### AsUInt32

```csharp
public uint AsUInt32()
```

### AsUInt64

```csharp
public ulong AsUInt64()
```

### AsVector2

```csharp
public Vector2 AsVector2()
```

### AsVector2Array

```csharp
public Vector2[] AsVector2Array()
```

### AsVector2I

```csharp
public Vector2I AsVector2I()
```

### AsVector3

```csharp
public Vector3 AsVector3()
```

### AsVector3Array

```csharp
public Vector3[] AsVector3Array()
```

### AsVector3I

```csharp
public Vector3I AsVector3I()
```

### AsVector4

```csharp
public Vector4 AsVector4()
```

### AsVector4Array

```csharp
public Vector4[] AsVector4Array()
```

### AsVector4I

```csharp
public Vector4I AsVector4I()
```

### CopyNativeVariant

```csharp
public godot_variant CopyNativeVariant()
```

Constructs a new `NativeInterop.godot_variant` from this instance.
The caller is responsible of disposing the new instance to avoid memory leaks.

### CreateCopyingBorrowed

```csharp
public static Variant CreateCopyingBorrowed(in godot_variant nativeValueToOwn)
```

### CreateFrom

```csharp
public static Variant CreateFrom(Aabb from)
```

### CreateFrom

```csharp
public static Variant CreateFrom(Array from)
```

### CreateFrom

```csharp
public static Variant CreateFrom(Basis from)
```

### CreateFrom

```csharp
public static Variant CreateFrom(Callable from)
```

### CreateFrom

```csharp
public static Variant CreateFrom(Color from)
```

### CreateFrom

```csharp
public static Variant CreateFrom(Dictionary from)
```

### CreateFrom

```csharp
public static Variant CreateFrom(GodotObject from)
```

### CreateFrom

```csharp
public static Variant CreateFrom(GodotObject[] from)
```

### CreateFrom

```csharp
public static Variant CreateFrom(NodePath from)
```

### CreateFrom

```csharp
public static Variant CreateFrom(Plane from)
```

### CreateFrom

```csharp
public static Variant CreateFrom(Projection from)
```

### CreateFrom

```csharp
public static Variant CreateFrom(Quaternion from)
```

### CreateFrom

```csharp
public static Variant CreateFrom(Rect2 from)
```

### CreateFrom

```csharp
public static Variant CreateFrom(Rect2I from)
```

### CreateFrom

```csharp
public static Variant CreateFrom(Rid from)
```

### CreateFrom

```csharp
public static Variant CreateFrom(Signal from)
```

### CreateFrom

```csharp
public static Variant CreateFrom(Span<Color> from)
```

### CreateFrom

```csharp
public static Variant CreateFrom(Span<NodePath> from)
```

### CreateFrom

```csharp
public static Variant CreateFrom(Span<Rid> from)
```

### CreateFrom

```csharp
public static Variant CreateFrom(Span<StringName> from)
```

### CreateFrom

```csharp
public static Variant CreateFrom(Span<Vector2> from)
```

### CreateFrom

```csharp
public static Variant CreateFrom(Span<Vector3> from)
```

### CreateFrom

```csharp
public static Variant CreateFrom(Span<Vector4> from)
```

### CreateFrom

```csharp
public static Variant CreateFrom(Span<byte> from)
```

### CreateFrom

```csharp
public static Variant CreateFrom(Span<double> from)
```

### CreateFrom

```csharp
public static Variant CreateFrom(Span<float> from)
```

### CreateFrom

```csharp
public static Variant CreateFrom(Span<int> from)
```

### CreateFrom

```csharp
public static Variant CreateFrom(Span<long> from)
```

### CreateFrom

```csharp
public static Variant CreateFrom(Span<string> from)
```

### CreateFrom

```csharp
public static Variant CreateFrom(StringName from)
```

### CreateFrom

```csharp
public static Variant CreateFrom(Transform2D from)
```

### CreateFrom

```csharp
public static Variant CreateFrom(Transform3D from)
```

### CreateFrom

```csharp
public static Variant CreateFrom(Vector2 from)
```

### CreateFrom

```csharp
public static Variant CreateFrom(Vector2I from)
```

### CreateFrom

```csharp
public static Variant CreateFrom(Vector3 from)
```

### CreateFrom

```csharp
public static Variant CreateFrom(Vector3I from)
```

### CreateFrom

```csharp
public static Variant CreateFrom(Vector4 from)
```

### CreateFrom

```csharp
public static Variant CreateFrom(Vector4I from)
```

### CreateFrom

```csharp
public static Variant CreateFrom(bool from)
```

### CreateFrom

```csharp
public static Variant CreateFrom(byte from)
```

### CreateFrom

```csharp
public static Variant CreateFrom(char from)
```

### CreateFrom

```csharp
public static Variant CreateFrom(double from)
```

### CreateFrom

```csharp
public static Variant CreateFrom(float from)
```

### CreateFrom

```csharp
public static Variant CreateFrom(int from)
```

### CreateFrom

```csharp
public static Variant CreateFrom(long from)
```

### CreateFrom

```csharp
public static Variant CreateFrom(sbyte from)
```

### CreateFrom

```csharp
public static Variant CreateFrom(short from)
```

### CreateFrom

```csharp
public static Variant CreateFrom(string from)
```

### CreateFrom

```csharp
public static Variant CreateFrom(uint from)
```

### CreateFrom

```csharp
public static Variant CreateFrom(ulong from)
```

### CreateFrom

```csharp
public static Variant CreateFrom(ushort from)
```

### CreateFrom

```csharp
public static Variant CreateFrom<T>(Array<T> from)
```

### CreateFrom

```csharp
public static Variant CreateFrom<TKey, TValue>(Dictionary<TKey, TValue> from)
```

### CreateTakingOwnershipOfDisposableValue

```csharp
public static Variant CreateTakingOwnershipOfDisposableValue(in godot_variant nativeValueToOwn)
```

### Dispose

```csharp
public void Dispose()
```

### From

```csharp
public static Variant From<T>(in T from)
```

### ToString

```csharp
public virtual string ToString()
```

## 运算符

### op_Explicit

```csharp
public static Aabb op_Explicit(Variant from)
```

### op_Explicit

```csharp
public static Array op_Explicit(Variant from)
```

### op_Explicit

```csharp
public static Basis op_Explicit(Variant from)
```

### op_Explicit

```csharp
public static Callable op_Explicit(Variant from)
```

### op_Explicit

```csharp
public static Color op_Explicit(Variant from)
```

### op_Explicit

```csharp
public static Color[] op_Explicit(Variant from)
```

### op_Explicit

```csharp
public static Dictionary op_Explicit(Variant from)
```

### op_Explicit

```csharp
public static GodotObject op_Explicit(Variant from)
```

### op_Explicit

```csharp
public static NodePath op_Explicit(Variant from)
```

### op_Explicit

```csharp
public static NodePath[] op_Explicit(Variant from)
```

### op_Explicit

```csharp
public static Plane op_Explicit(Variant from)
```

### op_Explicit

```csharp
public static Projection op_Explicit(Variant from)
```

### op_Explicit

```csharp
public static Quaternion op_Explicit(Variant from)
```

### op_Explicit

```csharp
public static Rect2 op_Explicit(Variant from)
```

### op_Explicit

```csharp
public static Rect2I op_Explicit(Variant from)
```

### op_Explicit

```csharp
public static Rid op_Explicit(Variant from)
```

### op_Explicit

```csharp
public static Rid[] op_Explicit(Variant from)
```

### op_Explicit

```csharp
public static Signal op_Explicit(Variant from)
```

### op_Explicit

```csharp
public static StringName op_Explicit(Variant from)
```

### op_Explicit

```csharp
public static StringName[] op_Explicit(Variant from)
```

### op_Explicit

```csharp
public static Transform2D op_Explicit(Variant from)
```

### op_Explicit

```csharp
public static Transform3D op_Explicit(Variant from)
```

### op_Explicit

```csharp
public static Vector2 op_Explicit(Variant from)
```

### op_Explicit

```csharp
public static Vector2I op_Explicit(Variant from)
```

### op_Explicit

```csharp
public static Vector2[] op_Explicit(Variant from)
```

### op_Explicit

```csharp
public static Vector3 op_Explicit(Variant from)
```

### op_Explicit

```csharp
public static Vector3I op_Explicit(Variant from)
```

### op_Explicit

```csharp
public static Vector3[] op_Explicit(Variant from)
```

### op_Explicit

```csharp
public static Vector4 op_Explicit(Variant from)
```

### op_Explicit

```csharp
public static Vector4I op_Explicit(Variant from)
```

### op_Explicit

```csharp
public static Vector4[] op_Explicit(Variant from)
```

### op_Explicit

```csharp
public static bool op_Explicit(Variant from)
```

### op_Explicit

```csharp
public static byte op_Explicit(Variant from)
```

### op_Explicit

```csharp
public static byte[] op_Explicit(Variant from)
```

### op_Explicit

```csharp
public static char op_Explicit(Variant from)
```

### op_Explicit

```csharp
public static double op_Explicit(Variant from)
```

### op_Explicit

```csharp
public static double[] op_Explicit(Variant from)
```

### op_Explicit

```csharp
public static float op_Explicit(Variant from)
```

### op_Explicit

```csharp
public static float[] op_Explicit(Variant from)
```

### op_Explicit

```csharp
public static int op_Explicit(Variant from)
```

### op_Explicit

```csharp
public static int[] op_Explicit(Variant from)
```

### op_Explicit

```csharp
public static long op_Explicit(Variant from)
```

### op_Explicit

```csharp
public static long[] op_Explicit(Variant from)
```

### op_Explicit

```csharp
public static sbyte op_Explicit(Variant from)
```

### op_Explicit

```csharp
public static short op_Explicit(Variant from)
```

### op_Explicit

```csharp
public static string op_Explicit(Variant from)
```

### op_Explicit

```csharp
public static string[] op_Explicit(Variant from)
```

### op_Explicit

```csharp
public static uint op_Explicit(Variant from)
```

### op_Explicit

```csharp
public static ulong op_Explicit(Variant from)
```

### op_Explicit

```csharp
public static ushort op_Explicit(Variant from)
```

### op_Implicit

```csharp
public static Variant op_Implicit(Aabb from)
```

### op_Implicit

```csharp
public static Variant op_Implicit(Array from)
```

### op_Implicit

```csharp
public static Variant op_Implicit(Basis from)
```

### op_Implicit

```csharp
public static Variant op_Implicit(Callable from)
```

### op_Implicit

```csharp
public static Variant op_Implicit(Color from)
```

### op_Implicit

```csharp
public static Variant op_Implicit(Color[] from)
```

### op_Implicit

```csharp
public static Variant op_Implicit(Dictionary from)
```

### op_Implicit

```csharp
public static Variant op_Implicit(GodotObject from)
```

### op_Implicit

```csharp
public static Variant op_Implicit(GodotObject[] from)
```

### op_Implicit

```csharp
public static Variant op_Implicit(NodePath from)
```

### op_Implicit

```csharp
public static Variant op_Implicit(NodePath[] from)
```

### op_Implicit

```csharp
public static Variant op_Implicit(Plane from)
```

### op_Implicit

```csharp
public static Variant op_Implicit(Projection from)
```

### op_Implicit

```csharp
public static Variant op_Implicit(Quaternion from)
```

### op_Implicit

```csharp
public static Variant op_Implicit(Rect2 from)
```

### op_Implicit

```csharp
public static Variant op_Implicit(Rect2I from)
```

### op_Implicit

```csharp
public static Variant op_Implicit(Rid from)
```

### op_Implicit

```csharp
public static Variant op_Implicit(Rid[] from)
```

### op_Implicit

```csharp
public static Variant op_Implicit(Signal from)
```

### op_Implicit

```csharp
public static Variant op_Implicit(Span<Color> from)
```

### op_Implicit

```csharp
public static Variant op_Implicit(Span<NodePath> from)
```

### op_Implicit

```csharp
public static Variant op_Implicit(Span<Rid> from)
```

### op_Implicit

```csharp
public static Variant op_Implicit(Span<StringName> from)
```

### op_Implicit

```csharp
public static Variant op_Implicit(Span<Vector2> from)
```

### op_Implicit

```csharp
public static Variant op_Implicit(Span<Vector3> from)
```

### op_Implicit

```csharp
public static Variant op_Implicit(Span<Vector4> from)
```

### op_Implicit

```csharp
public static Variant op_Implicit(Span<byte> from)
```

### op_Implicit

```csharp
public static Variant op_Implicit(Span<double> from)
```

### op_Implicit

```csharp
public static Variant op_Implicit(Span<float> from)
```

### op_Implicit

```csharp
public static Variant op_Implicit(Span<int> from)
```

### op_Implicit

```csharp
public static Variant op_Implicit(Span<long> from)
```

### op_Implicit

```csharp
public static Variant op_Implicit(Span<string> from)
```

### op_Implicit

```csharp
public static Variant op_Implicit(StringName from)
```

### op_Implicit

```csharp
public static Variant op_Implicit(StringName[] from)
```

### op_Implicit

```csharp
public static Variant op_Implicit(Transform2D from)
```

### op_Implicit

```csharp
public static Variant op_Implicit(Transform3D from)
```

### op_Implicit

```csharp
public static Variant op_Implicit(Vector2 from)
```

### op_Implicit

```csharp
public static Variant op_Implicit(Vector2I from)
```

### op_Implicit

```csharp
public static Variant op_Implicit(Vector2[] from)
```

### op_Implicit

```csharp
public static Variant op_Implicit(Vector3 from)
```

### op_Implicit

```csharp
public static Variant op_Implicit(Vector3I from)
```

### op_Implicit

```csharp
public static Variant op_Implicit(Vector3[] from)
```

### op_Implicit

```csharp
public static Variant op_Implicit(Vector4 from)
```

### op_Implicit

```csharp
public static Variant op_Implicit(Vector4I from)
```

### op_Implicit

```csharp
public static Variant op_Implicit(Vector4[] from)
```

### op_Implicit

```csharp
public static Variant op_Implicit(bool from)
```

### op_Implicit

```csharp
public static Variant op_Implicit(byte from)
```

### op_Implicit

```csharp
public static Variant op_Implicit(byte[] from)
```

### op_Implicit

```csharp
public static Variant op_Implicit(char from)
```

### op_Implicit

```csharp
public static Variant op_Implicit(double from)
```

### op_Implicit

```csharp
public static Variant op_Implicit(double[] from)
```

### op_Implicit

```csharp
public static Variant op_Implicit(float from)
```

### op_Implicit

```csharp
public static Variant op_Implicit(float[] from)
```

### op_Implicit

```csharp
public static Variant op_Implicit(int from)
```

### op_Implicit

```csharp
public static Variant op_Implicit(int[] from)
```

### op_Implicit

```csharp
public static Variant op_Implicit(long from)
```

### op_Implicit

```csharp
public static Variant op_Implicit(long[] from)
```

### op_Implicit

```csharp
public static Variant op_Implicit(sbyte from)
```

### op_Implicit

```csharp
public static Variant op_Implicit(short from)
```

### op_Implicit

```csharp
public static Variant op_Implicit(string from)
```

### op_Implicit

```csharp
public static Variant op_Implicit(string[] from)
```

### op_Implicit

```csharp
public static Variant op_Implicit(uint from)
```

### op_Implicit

```csharp
public static Variant op_Implicit(ulong from)
```

### op_Implicit

```csharp
public static Variant op_Implicit(ushort from)
```

## Operator

命名空间 `Godot`；enum

### 枚举值

- `Equal = 0`：Equality operator (`==`).
- `NotEqual = 1`：Inequality operator (`!=`).
- `Less = 2`：Less than operator (`<`).
- `LessEqual = 3`：Less than or equal operator (`<=`).
- `Greater = 4`：Greater than operator (`>`).
- `GreaterEqual = 5`：Greater than or equal operator (`>=`).
- `Add = 6`：Addition operator (`+`).
- `Subtract = 7`：Subtraction operator (`-`).
- `Multiply = 8`：Multiplication operator (`*`).
- `Divide = 9`：Division operator (`/`).
- `Negate = 10`：Unary negation operator (`-`).
- `Positive = 11`：Unary plus operator (`+`).
- `Module = 12`：Remainder/modulo operator (`%`).
- `Power = 13`：Power operator (`**`).
- `ShiftLeft = 14`：Left shift operator (`<<`).
- `ShiftRight = 15`：Right shift operator (`>>`).
- `BitAnd = 16`：Bitwise AND operator (`&`).
- `BitOr = 17`：Bitwise OR operator (`|`).
- `BitXor = 18`：Bitwise XOR operator (`^`).
- `BitNegate = 19`：Bitwise NOT operator (`~`).
- `And = 20`：Logical AND operator (`and` or `&&`).
- `Or = 21`：Logical OR operator (`or` or `||`).
- `Xor = 22`：Logical XOR operator (not implemented in GDScript).
- `Not = 23`：Logical NOT operator (`not` or `!`).
- `In = 24`：Logical IN operator (`in`).
- `Max = 25`：Represents the size of the `Variant.Operator` enum.

## Type

命名空间 `Godot`；enum

### 枚举值

- `Nil = 0`：Variable is `null`.
- `Bool = 1`：Variable is of type `System.Boolean`.
- `Int = 2`：Variable is of type `System.Int32`.
- `Float = 3`：Variable is of type `System.Single`.
- `String = 4`：Variable is of type `System.String`.
- `Vector2 = 5`：Variable is of type [`Vector2`](Vector2.md).
- `Vector2I = 6`：Variable is of type [`Vector2I`](Vector2I.md).
- `Rect2 = 7`：Variable is of type [`Rect2`](Rect2.md).
- `Rect2I = 8`：Variable is of type [`Rect2I`](Rect2I.md).
- `Vector3 = 9`：Variable is of type [`Vector3`](Vector3.md).
- `Vector3I = 10`：Variable is of type [`Vector3I`](Vector3I.md).
- `Transform2D = 11`：Variable is of type [`Transform2D`](Transform2D.md).
- `Vector4 = 12`：Variable is of type [`Vector4`](Vector4.md).
- `Vector4I = 13`：Variable is of type [`Vector4I`](Vector4I.md).
- `Plane = 14`：Variable is of type [`Plane`](Plane.md).
- `Quaternion = 15`：Variable is of type [`Quaternion`](Quaternion.md).
- `Aabb = 16`：Variable is of type [`Aabb`](Aabb.md).
- `Basis = 17`：Variable is of type [`Basis`](Basis.md).
- `Transform3D = 18`：Variable is of type [`Transform3D`](Transform3D.md).
- `Projection = 19`：Variable is of type [`Projection`](Projection.md).
- `Color = 20`：Variable is of type [`Color`](Color.md).
- `StringName = 21`：Variable is of type [`StringName`](StringName.md).
- `NodePath = 22`：Variable is of type [`NodePath`](NodePath.md).
- `Rid = 23`：Variable is of type [`Rid`](Rid.md).
- `Object = 24`：Variable is of type [`GodotObject`](GodotObject.md).
- `Callable = 25`：Variable is of type [`Callable`](Callable.md).
- `Signal = 26`：Variable is of type [`Signal`](Signal.md).
- `Dictionary = 27`：Variable is of type [`Collections.Dictionary`](Collections.Dictionary.md).
- `Array = 28`：Variable is of type [`Collections.Array`](Collections.Array.md).
- `PackedByteArray = 29`：Variable is of type `System.Byte`[].
- `PackedInt32Array = 30`：Variable is of type `System.Int32`[].
- `PackedInt64Array = 31`：Variable is of type `System.Int64`[].
- `PackedFloat32Array = 32`：Variable is of type `System.Single`[].
- `PackedFloat64Array = 33`：Variable is of type `System.Double`[].
- `PackedStringArray = 34`：Variable is of type `System.String`[].
- `PackedVector2Array = 35`：Variable is of type [`Vector2`](Vector2.md)[].
- `PackedVector3Array = 36`：Variable is of type [`Vector3`](Vector3.md)[].
- `PackedColorArray = 37`：Variable is of type [`Color`](Color.md)[].
- `PackedVector4Array = 38`：Variable is of type [`Vector4`](Vector4.md)[].
- `Max = 39`：Represents the size of the `Variant.Type` enum.

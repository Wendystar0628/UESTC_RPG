<!-- 由 scripts/godot-api/gen-api-docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# VisualShaderNodeCompare

命名空间 `Godot`；class；继承 [`VisualShaderNode`](VisualShaderNode.md) → [`Resource`](Resource.md) → [`RefCounted`](RefCounted.md) → [`GodotObject`](GodotObject.md)

Compares `a` and `b` of `Type` by `Function`. Returns a boolean scalar. Translates to `if` instruction in shader code.

## 构造函数

### VisualShaderNodeCompare

```csharp
public VisualShaderNodeCompare()
```

## 属性

### Condition

```csharp
public VisualShaderNodeCompare.ConditionEnum Condition { get; set; }
```

引擎名：`condition`

Extra condition which is applied if `Type` is set to [`VisualShaderNodeCompare.ComparisonType.Vector3D`](VisualShaderNodeCompare.md).

### Function

```csharp
public VisualShaderNodeCompare.FunctionEnum Function { get; set; }
```

引擎名：`function`

A comparison function.

### Type

```csharp
public VisualShaderNodeCompare.ComparisonType Type { get; set; }
```

引擎名：`type`

The type to be used in the comparison.

## 方法

### GetComparisonType

```csharp
public VisualShaderNodeCompare.ComparisonType GetComparisonType()
```

引擎名：`get_comparison_type`

### GetCondition

```csharp
public VisualShaderNodeCompare.ConditionEnum GetCondition()
```

引擎名：`get_condition`

### GetFunction

```csharp
public VisualShaderNodeCompare.FunctionEnum GetFunction()
```

引擎名：`get_function`

### SetComparisonType

```csharp
public void SetComparisonType(VisualShaderNodeCompare.ComparisonType type)
```

引擎名：`set_comparison_type`

### SetCondition

```csharp
public void SetCondition(VisualShaderNodeCompare.ConditionEnum condition)
```

引擎名：`set_condition`

### SetFunction

```csharp
public void SetFunction(VisualShaderNodeCompare.FunctionEnum func)
```

引擎名：`set_function`

## ComparisonType

命名空间 `Godot`；enum

### 枚举值

- `Scalar = 0`：A floating-point scalar.
- `ScalarInt = 1`：An integer scalar.
- `ScalarUint = 2`：An unsigned integer scalar.
- `Vector2D = 3`：A 2D vector type.
- `Vector3D = 4`：A 3D vector type.
- `Vector4D = 5`：A 4D vector type.
- `Boolean = 6`：A boolean type.
- `Transform = 7`：A transform (`mat4`) type.
- `Max = 8`：Represents the size of the `VisualShaderNodeCompare.ComparisonType` enum.

## ConditionEnum

命名空间 `Godot`；enum

### 枚举值

- `All = 0`：The result will be `true` if all components in the vector satisfy the comparison condition.
- `Any = 1`：The result will be `true` if any component in the vector satisfies the comparison condition.
- `Max = 2`：Represents the size of the `VisualShaderNodeCompare.ConditionEnum` enum.

## FunctionEnum

命名空间 `Godot`；enum

### 枚举值

- `Equal = 0`：Comparison for equality (`a == b`).
- `NotEqual = 1`：Comparison for inequality (`a != b`).
- `GreaterThan = 2`：Comparison for greater than (`a > b`).
- `GreaterThanEqual = 3`：Comparison for greater than or equal (`a >= b`).
- `LessThan = 4`：Comparison for less than (`a < b`).
- `LessThanEqual = 5`：Comparison for less than or equal (`a <= b`).
- `Max = 6`：Represents the size of the `VisualShaderNodeCompare.FunctionEnum` enum.

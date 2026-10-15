<!-- 由 scripts/godot-api/gen_api_docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# Expression

命名空间 `Godot`；class；继承 [`RefCounted`](RefCounted.md) → [`GodotObject`](GodotObject.md)

An expression can be made of any arithmetic operation, built-in math function call, method call of a passed instance, or built-in type construction call.

An example expression text using the built-in math functions could be `sqrt(pow(3, 2) + pow(4, 2))`.

In the following example we use a [`LineEdit`](LineEdit.md) node to write our expression and show the result.

```csharp
private Expression _expression = new Expression();

public override void _Ready()
{
GetNode<LineEdit>("LineEdit").TextSubmitted += OnTextEntered;
}

private void OnTextEntered(string command)
{
Error error = _expression.Parse(command);
if (error != Error.Ok)
{
GD.Print(_expression.GetErrorText());
return;
}
Variant result = _expression.Execute();
if (!_expression.HasExecuteFailed())
{
GetNode<LineEdit>("LineEdit").Text = result.ToString();
}
}

```

## 构造函数

### Expression

```csharp
public Expression()
```

## 方法

### Execute

```csharp
public Variant Execute(Array inputs = null, GodotObject baseInstance = null, bool showError = true, bool constCallsOnly = false)
```

引擎名：`execute`

Executes the expression that was previously parsed by `Parse()` and returns the result. Before you use the returned object, you should check if the method failed by calling `HasExecuteFailed()`.

If you defined input variables in `Parse()`, you can specify their values in the inputs array, in the same order.

### GetErrorText

```csharp
public string GetErrorText()
```

引擎名：`get_error_text`

Returns the error text if `Parse()` or `Execute()` has failed.

### HasExecuteFailed

```csharp
public bool HasExecuteFailed()
```

引擎名：`has_execute_failed`

Returns `true` if `Execute()` has failed.

### Parse

```csharp
public Error Parse(string expression, ReadOnlySpan<string> inputNames)
```

引擎名：`parse`

Parses the expression and returns an [`Error`](Error.md) code.

You can optionally specify names of variables that may appear in the expression with `inputNames`, so that you can bind them when it gets executed.

### Parse

```csharp
public Error Parse(string expression, string[] inputNames = null)
```

引擎名：`parse`

Parses the expression and returns an [`Error`](Error.md) code.

You can optionally specify names of variables that may appear in the expression with `inputNames`, so that you can bind them when it gets executed.

参数：
- `inputNames`：If the parameter is null, then the default value is `Array.Empty<string>()`.

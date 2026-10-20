<!-- 由 scripts/godot-api/gen-api-docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# JavaClassWrapperInstance

命名空间 `Godot`；class；继承 [`GodotObject`](GodotObject.md)

The JavaClassWrapper singleton provides a way for the Godot application to send and receive data through the [Java Native Interface](https://developer.android.com/training/articles/perf-jni) (JNI).

**Note:** This singleton is only available in Android builds.

```csharp
var LocalDateTime = JavaClassWrapper.wrap("java.time.LocalDateTime")
var DateTimeFormatter = JavaClassWrapper.wrap("java.time.format.DateTimeFormatter")

var datetime = LocalDateTime.now()
var formatter = DateTimeFormatter.ofPattern("dd-MM-yyyy HH:mm:ss")

print(datetime.format(formatter))

```

**Warning:** When calling Java methods, be sure to check [`JavaClassWrapper.GetException`](JavaClassWrapper.md) to check if the method threw an exception.

## 方法

### CreateProxy

```csharp
public JavaObject CreateProxy(GodotObject object, ReadOnlySpan<string> interfaces)
```

Creates a [`JavaObject`](JavaObject.md) implementing the given Java interfaces using the given [`GodotObject`](GodotObject.md) as the implementation.

The `object` must contain methods signatures matching the methods signatures from the passed Java `interfaces`. Invoking methods from the Java `interfaces` will route to the matching `object` method.

```csharp
class PrintProxy:
func println(content: String) -> void:
print(content)

var print_proxy = PrintProxy.new()
var printer_object = JavaClassWrapper.create_proxy(print_proxy, ["android.util.Printer"])
printer_object.println("Hello Godot World!")

```

**Note:** This method only works on Android. On every other platform, this method will always return `null`.

### CreateProxy

```csharp
public JavaObject CreateProxy(GodotObject object, string[] interfaces)
```

Creates a [`JavaObject`](JavaObject.md) implementing the given Java interfaces using the given [`GodotObject`](GodotObject.md) as the implementation.

The `object` must contain methods signatures matching the methods signatures from the passed Java `interfaces`. Invoking methods from the Java `interfaces` will route to the matching `object` method.

```csharp
class PrintProxy:
func println(content: String) -> void:
print(content)

var print_proxy = PrintProxy.new()
var printer_object = JavaClassWrapper.create_proxy(print_proxy, ["android.util.Printer"])
printer_object.println("Hello Godot World!")

```

**Note:** This method only works on Android. On every other platform, this method will always return `null`.

### CreateSamCallback

```csharp
public JavaObject CreateSamCallback(string samInterface, Callable callable)
```

Creates a [`JavaObject`](JavaObject.md) implementing the Java Single Abstract Method (SAM) interface using the Godot [`Callable`](Callable.md) as the implementation.

The `samInterface` **must be** a Java SAM interface, meaning it must only have a single abstract method to implement.

The `callable` must be able to handle the same parameter types as the SAM interface method, and must provide the same return type. The `callable` will be invoked as a callback, passing the arguments from the Java SAM interface method.

```csharp
var cb = func (content: String) -> void:
print(content)
var callback = JavaClassWrapper.create_sam_callback("android.util.Printer", cb)
callback.println("Hello Godot World!")

```

**Note:** This method only works on Android. On every other platform, this method will always return `null`.

### GetException

```csharp
public JavaObject GetException()
```

Returns the Java exception from the last call into a Java class. If there was no exception, it will return `null`.

**Note:** This method only works on Android. On every other platform, this method will always return `null`.

### Wrap

```csharp
public JavaClass Wrap(string name)
```

Wraps a class defined in Java, and returns it as a [`JavaClass`](JavaClass.md) [`GodotObject`](GodotObject.md) type that Godot can interact with.

When wrapping inner (nested) classes, use `$` instead of `.` to separate them. For example, `JavaClassWrapper.wrap("android.view.WindowManager$LayoutParams")` wraps the **WindowManager.LayoutParams** class.

**Note:** To invoke a constructor, call a method with the same name as the class. For example:

```csharp
var Intent = JavaClassWrapper.wrap("android.content.Intent")
var intent = Intent.Intent()

```

**Note:** This method only works on Android. On every other platform, this method does nothing and returns an empty [`JavaClass`](JavaClass.md).

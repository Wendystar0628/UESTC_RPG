<!-- 由 scripts/godot-api/gen-api-docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# Array<T>

命名空间 `Godot.Collections`；class

Typed wrapper around Godot's Array class, an array of `T`
annotated, Variant typed elements allocated in the engine in C++.
Useful when interfacing with the engine. Otherwise prefer .NET collections
such as arrays or `System.Collections.Generic.List`.

## 构造函数

### Array

```csharp
public Array()
```

Constructs a new empty [`Collections.Array`](Collections.Array.md).

返回：A new Godot Array.

### Array

```csharp
public Array(Array array)
```

Constructs a typed [`Collections.Array`](Collections.Array.md) from an untyped [`Collections.Array`](Collections.Array.md).

参数：
- `array`：The untyped array to construct from.

返回：A new Godot Array.

### Array

```csharp
public Array(IEnumerable<T> collection)
```

Constructs a new [`Collections.Array`](Collections.Array.md) from the given collection's elements.

参数：
- `collection`：The collection of elements to construct from.

返回：A new Godot Array.

### Array

```csharp
public Array(T[] array)
```

Constructs a new [`Collections.Array`](Collections.Array.md) from the given items.

参数：
- `array`：The items to put in the new array.

返回：A new Godot Array.

## 属性

### Count

```csharp
public int Count { get; }
```

引擎名：`count`

Returns the number of elements in this [`Collections.Array`](Collections.Array.md).
This is also known as the size or length of the array.

返回：The number of elements.

### IsReadOnly

```csharp
public bool IsReadOnly { get; }
```

引擎名：`is_read_only`

Returns `true` if the array is read-only.
See [`Collections.Array.MakeReadOnly()`](Collections.Array.md).

### Item

```csharp
public T this[int index] { get; set; }
```

Returns the item at the given `index`.

## 方法

### Add

```csharp
public void Add(T item)
```

Adds an item to the end of this [`Collections.Array`](Collections.Array.md).
This is the same as `append` or `push_back` in GDScript.

参数：
- `item`：The [`Variant`](Variant.md) item to add.

### AddRange

```csharp
public void AddRange(IEnumerable<T> collection)
```

Adds the elements of the specified collection to the end of this [`Collections.Array`](Collections.Array.md).

参数：
- `collection`：Collection of [`Variant`](Variant.md) items to add.

### AddRange

```csharp
public void AddRange(ReadOnlySpan<T> span)
```

Adds the elements of the specified span to the end of this [`Collections.Array`](Collections.Array.md).

参数：
- `span`：The span of [`Variant`](Variant.md) items to add.

### AddRange

```csharp
public void AddRange(T[] array)
```

Adds the elements of the specified array to the end of this [`Collections.Array`](Collections.Array.md).

参数：
- `array`：The array of [`Variant`](Variant.md) items to add.

### BinarySearch

```csharp
public int BinarySearch(T item)
```

Finds the index of an existing value using binary search.
If the value is not present in the array, it returns the bitwise
complement of the insertion index that maintains sorting order.
Note: Calling [`Collections.Array.BinarySearch()`](Collections.Array.md) on an unsorted
array results in unexpected behavior.

参数：
- `item`：The object to locate.

返回：The index of the item in the array, if `item` is found;
otherwise, a negative number that is the bitwise complement of the index
of the next element that is larger than `item` or, if
there is no larger element, the bitwise complement of [`Collections.Array.Count`](Collections.Array.md).

### BinarySearch

```csharp
public int BinarySearch(int index, int count, T item)
```

Finds the index of an existing value using binary search.
If the value is not present in the array, it returns the bitwise
complement of the insertion index that maintains sorting order.
Note: Calling [`Collections.Array.BinarySearch()`](Collections.Array.md) on an unsorted
array results in unexpected behavior.

参数：
- `index`：The starting index of the range to search.
- `count`：The length of the range to search.
- `item`：The object to locate.

返回：The index of the item in the array, if `item` is found;
otherwise, a negative number that is the bitwise complement of the index
of the next element that is larger than `item` or, if
there is no larger element, the bitwise complement of [`Collections.Array.Count`](Collections.Array.md).

### Clear

```csharp
public void Clear()
```

引擎名：`clear`

Clears the array. This is the equivalent to using [`Collections.Array.Resize()`](Collections.Array.md)
with a size of `0`

### Contains

```csharp
public bool Contains(T item)
```

Returns `true` if the array contains the given value.

参数：
- `item`：The item to look for.

返回：Whether or not this array contains the given item.

### CopyTo

```csharp
public void CopyTo(T[] array, int arrayIndex)
```

Copies the elements of this [`Collections.Array`](Collections.Array.md) to the given
C# array, starting at the given index.

参数：
- `array`：The C# array to copy to.
- `arrayIndex`：The index to start at.

### Duplicate

```csharp
public Array<T> Duplicate(bool deep = false)
```

引擎名：`duplicate`

Duplicates this [`Collections.Array`](Collections.Array.md).

参数：
- `deep`：If `true`, performs a deep copy.

返回：A new Godot Array.

### Fill

```csharp
public void Fill(T value)
```

引擎名：`fill`

Assigns the given value to all elements in the array. This can typically be
used together with [`Collections.Array.Resize()`](Collections.Array.md) to create an array with a given
size and initialized elements.
Note: If `value` is of a reference type ([`GodotObject`](GodotObject.md)
derived, [`Collections.Array`](Collections.Array.md) or [`Collections.Dictionary`](Collections.Dictionary.md), etc.) then the array
is filled with the references to the same object, i.e. no duplicates are
created.

参数：
- `value`：The value to fill the array with.

### GetEnumerator

```csharp
public IEnumerator<T> GetEnumerator()
```

Gets an enumerator for this [`Collections.Array`](Collections.Array.md).

返回：An enumerator.

### GetSliceRange

```csharp
public Array<T> GetSliceRange(int start, int end, int step = 1, bool deep = false)
```

Returns the slice of the [`Collections.Array`](Collections.Array.md), from `start`
(inclusive) to `end` (exclusive), as a new [`Collections.Array`](Collections.Array.md).
The absolute value of `start` and `end`
will be clamped to the array size.
If either `start` or `end` are negative, they
will be relative to the end of the array (i.e. `arr.GetSliceRange(0, -2)`
is a shorthand for `arr.GetSliceRange(0, arr.Count - 2)`).
If specified, `step` is the relative index between source
elements. It can be negative, then `start` must be higher than
`end`. For example, `[0, 1, 2, 3, 4, 5].GetSliceRange(5, 1, -2)`
returns `[5, 3]`.
If `deep` is true, each element will be copied by value
rather than by reference.

参数：
- `start`：The zero-based index at which the range starts.
- `end`：The zero-based index at which the range ends.
- `step`：The relative index between source elements to take.
- `deep`：If `true`, performs a deep copy.

返回：A new array that contains the elements inside the slice range.

### IndexOf

```csharp
public int IndexOf(T item)
```

Searches the array for a value and returns its index or `-1` if not found.

参数：
- `item`：The [`Variant`](Variant.md) item to search for.

返回：The index of the item, or -1 if not found.

### IndexOf

```csharp
public int IndexOf(T item, int index)
```

Searches the array for a value and returns its index or `-1` if not found.

参数：
- `item`：The [`Variant`](Variant.md) item to search for.
- `index`：The initial search index to start from.

返回：The index of the item, or -1 if not found.

### Insert

```csharp
public void Insert(int index, T item)
```

引擎名：`insert`

Inserts a new element at a given position in the array. The position
must be valid, or at the end of the array (`pos == Count - 1`).
Existing items will be moved to the right.

参数：
- `index`：The index to insert at.
- `item`：The [`Variant`](Variant.md) item to insert.

### LastIndexOf

```csharp
public int LastIndexOf(Variant item)
```

Searches the array for a value in reverse order and returns its index
or `-1` if not found.

参数：
- `item`：The [`Variant`](Variant.md) item to search for.

返回：The index of the item, or -1 if not found.

### LastIndexOf

```csharp
public int LastIndexOf(Variant item, int index)
```

Searches the array for a value in reverse order and returns its index
or `-1` if not found.

参数：
- `item`：The [`Variant`](Variant.md) item to search for.
- `index`：The initial search index to start from.

返回：The index of the item, or -1 if not found.

### MakeReadOnly

```csharp
public void MakeReadOnly()
```

引擎名：`make_read_only`

Makes the [`Collections.Array`](Collections.Array.md) read-only, i.e. disabled modying of the
array's elements. Does not apply to nested content, e.g. content of
nested arrays.

### Max

```csharp
public T Max()
```

引擎名：`max`

Returns the maximum value contained in the array if all elements are of
comparable types. If the elements can't be compared, `default`
is returned.

返回：The maximum value contained in the array.

### Min

```csharp
public T Min()
```

引擎名：`min`

Returns the minimum value contained in the array if all elements are of
comparable types. If the elements can't be compared, `default`
is returned.

返回：The minimum value contained in the array.

### PickRandom

```csharp
public T PickRandom()
```

引擎名：`pick_random`

Returns a random value from the target array.

返回：A random element from the array.

### RecursiveEqual

```csharp
public bool RecursiveEqual(Array<T> other)
```

Compares this [`Collections.Array`](Collections.Array.md) against the `other`
[`Collections.Array`](Collections.Array.md) recursively. Returns `true` if the
sizes and contents of the arrays are equal, `false`
otherwise.

参数：
- `other`：The other array to compare against.

返回：`true` if the sizes and contents of the arrays are equal,
`false` otherwise.

### Remove

```csharp
public bool Remove(T item)
```

Removes the first occurrence of the specified `item`
from this [`Collections.Array`](Collections.Array.md).

参数：
- `item`：The value to remove.

返回：A `bool` indicating success or failure.

### RemoveAt

```csharp
public void RemoveAt(int index)
```

引擎名：`remove_at`

Removes an element from the array by index.
To remove an element by searching for its value, use
[`Collections.Array.Remove()`](Collections.Array.md) instead.

参数：
- `index`：The index of the element to remove.

### Resize

```csharp
public Error Resize(int newSize)
```

引擎名：`resize`

Resizes this [`Collections.Array`](Collections.Array.md) to the given size.

参数：
- `newSize`：The new size of the array.

返回：[`Error.Ok`](Error.md) if successful, or an error code.

### Reverse

```csharp
public void Reverse()
```

引擎名：`reverse`

Reverses the order of the elements in the array.

### Shuffle

```csharp
public void Shuffle()
```

引擎名：`shuffle`

Shuffles the array such that the items will have a random order.
This method uses the global random number generator common to methods
such as [`GD.Randi`](GD.md). Call [`GD.Randomize`](GD.md) to
ensure that a new seed will be used each time if you want
non-reproducible shuffling.

### Slice

```csharp
public Array<T> Slice(int start)
```

引擎名：`slice`

Creates a shallow copy of a range of elements in the source [`Collections.Array`](Collections.Array.md).

参数：
- `start`：The zero-based index at which the range starts.

返回：A new array that contains the elements inside the slice range.

### Slice

```csharp
public Array<T> Slice(int start, int length)
```

引擎名：`slice`

Creates a shallow copy of a range of elements in the source [`Collections.Array`](Collections.Array.md).

参数：
- `start`：The zero-based index at which the range starts.
- `length`：The length of the range.

返回：A new array that contains the elements inside the slice range.

### Sort

```csharp
public void Sort()
```

引擎名：`sort`

Sorts the array.
Note: The sorting algorithm used is not stable. This means that values
considered equal may have their order changed when using [`Collections.Array.Sort()`](Collections.Array.md).
Note: Strings are sorted in alphabetical order (as opposed to natural order).
This may lead to unexpected behavior when sorting an array of strings ending
with a sequence of numbers.
To sort with a custom predicate use
`System.Linq.Enumerable.OrderBy()`.

### ToString

```csharp
public virtual string ToString()
```

Converts this [`Collections.Array`](Collections.Array.md) to a string.

返回：A string representation of this array.

## 运算符

### op_Addition

```csharp
public static Array<T> op_Addition(Array<T> left, Array<T> right)
```

Concatenates two [`Collections.Array`](Collections.Array.md)s together, with the `right`
being added to the end of the [`Collections.Array`](Collections.Array.md) specified in `left`.
For example, `[1, 2] + [3, 4]` results in `[1, 2, 3, 4]`.

参数：
- `left`：The first array.
- `right`：The second array.

返回：A new Godot Array with the contents of both arrays.

### op_Explicit

```csharp
public static Array op_Explicit(Array<T> from)
```

Converts this typed [`Collections.Array`](Collections.Array.md) to an untyped [`Collections.Array`](Collections.Array.md).

参数：
- `from`：The typed array to convert.

返回：A new Godot Array, or `null` if  was null.

### op_Explicit

```csharp
public static Array<T> op_Explicit(Variant from)
```

### op_Implicit

```csharp
public static Variant op_Implicit(Array<T> from)
```

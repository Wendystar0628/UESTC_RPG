<!-- 由 scripts/godot-api/gen-api-docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# Array

命名空间 `Godot.Collections`；class

Wrapper around Godot's Array class, an array of Variant
typed elements allocated in the engine in C++. Useful when
interfacing with the engine. Otherwise prefer .NET collections
such as `System.Array` or `System.Collections.Generic.List`.

## 构造函数

### Array

```csharp
public Array()
```

Constructs a new empty `Collections.Array`.

### Array

```csharp
public Array(IEnumerable<Variant> collection)
```

Constructs a new `Collections.Array` from the given collection's elements.

参数：
- `collection`：The collection of elements to construct from.

返回：A new Godot Array.

### Array

```csharp
public Array(ReadOnlySpan<GodotObject> span)
```

Constructs a new `Collections.Array` from the given ReadOnlySpan's elements.

返回：A new Godot Array.

### Array

```csharp
public Array(ReadOnlySpan<NodePath> span)
```

Constructs a new `Collections.Array` from the given span's elements.

返回：A new Godot Array.

### Array

```csharp
public Array(ReadOnlySpan<Rid> span)
```

Constructs a new `Collections.Array` from the given span's elements.

返回：A new Godot Array.

### Array

```csharp
public Array(ReadOnlySpan<StringName> span)
```

Constructs a new `Collections.Array` from the given span's elements.

返回：A new Godot Array.

### Array

```csharp
public Array(Span<NodePath> span)
```

Constructs a new `Collections.Array` from the given span's elements.

返回：A new Godot Array.

### Array

```csharp
public Array(Span<Rid> span)
```

Constructs a new `Collections.Array` from the given span's elements.

返回：A new Godot Array.

### Array

```csharp
public Array(Span<StringName> span)
```

Constructs a new `Collections.Array` from the given span's elements.

返回：A new Godot Array.

### Array

```csharp
public Array(Variant[] array)
```

Constructs a new `Collections.Array` from the given objects.

参数：
- `array`：The objects to put in the new array.

返回：A new Godot Array.

## 属性

### Count

```csharp
public int Count { get; }
```

引擎名：`count`

Returns the number of elements in this `Collections.Array`.
This is also known as the size or length of the array.

返回：The number of elements.

### IsReadOnly

```csharp
public bool IsReadOnly { get; }
```

引擎名：`is_read_only`

Returns `true` if the array is read-only.
See `MakeReadOnly()`.

### Item

```csharp
public Variant this[int index] { get; set; }
```

Returns the item at the given `index`.

## 方法

### Add

```csharp
public void Add(Variant item)
```

Adds an item to the end of this `Collections.Array`.
This is the same as `append` or `push_back` in GDScript.

参数：
- `item`：The [`Variant`](Variant.md) item to add.

### AddRange

```csharp
public void AddRange<T>(IEnumerable<T> collection)
```

Adds the elements of the specified collection to the end of this `Collections.Array`.

参数：
- `collection`：Collection of [`Variant`](Variant.md) items to add.

### AddRange

```csharp
public void AddRange<T>(ReadOnlySpan<T> span)
```

Adds the elements of the specified collection to the end of this `Collections.Array`.

参数：
- `span`：The span of [`Variant`](Variant.md) items to add.

### AddRange

```csharp
public void AddRange<T>(T[] array)
```

Adds the elements of the specified array to the end of this `Collections.Array`.

参数：
- `array`：The array of [`Variant`](Variant.md) items to add.

### BinarySearch

```csharp
public int BinarySearch(Variant item)
```

Finds the index of an existing value using binary search.
If the value is not present in the array, it returns the bitwise
complement of the insertion index that maintains sorting order.
Note: Calling `BinarySearch()` on an unsorted
array results in unexpected behavior.

参数：
- `item`：The object to locate.

返回：The index of the item in the array, if `item` is found;
otherwise, a negative number that is the bitwise complement of the index
of the next element that is larger than `item` or, if
there is no larger element, the bitwise complement of `Count`.

### BinarySearch

```csharp
public int BinarySearch(int index, int count, Variant item)
```

Finds the index of an existing value using binary search.
If the value is not present in the array, it returns the bitwise
complement of the insertion index that maintains sorting order.
Note: Calling `BinarySearch()` on an
unsorted array results in unexpected behavior.

参数：
- `index`：The starting index of the range to search.
- `count`：The length of the range to search.
- `item`：The object to locate.

返回：The index of the item in the array, if `item` is found;
otherwise, a negative number that is the bitwise complement of the index
of the next element that is larger than `item` or, if
there is no larger element, the bitwise complement of `Count`.

### Clear

```csharp
public void Clear()
```

引擎名：`clear`

Clears the array. This is the equivalent to using `Resize()`
with a size of `0`

### Contains

```csharp
public bool Contains(Variant item)
```

Returns `true` if the array contains the given value.

参数：
- `item`：The [`Variant`](Variant.md) item to look for.

返回：Whether or not this array contains the given item.

### CopyTo

```csharp
public void CopyTo(Variant[] array, int arrayIndex)
```

Copies the elements of this `Collections.Array` to the given
[`Variant`](Variant.md) C# array, starting at the given index.

参数：
- `array`：The array to copy to.
- `arrayIndex`：The index to start at.

### Dispose

```csharp
public void Dispose()
```

Disposes of this `Collections.Array`.

### Dispose

```csharp
public void Dispose(bool disposing)
```

### Duplicate

```csharp
public Array Duplicate(bool deep = false)
```

引擎名：`duplicate`

Returns a copy of the `Collections.Array`.
If `deep` is `true`, a deep copy if performed:
all nested arrays and dictionaries are duplicated and will not be shared with
the original array. If `false`, a shallow copy is made and
references to the original nested arrays and dictionaries are kept, so that
modifying a sub-array or dictionary in the copy will also impact those
referenced in the source array. Note that any [`GodotObject`](GodotObject.md) derived
elements will be shallow copied regardless of the `deep`
setting.

参数：
- `deep`：If `true`, performs a deep copy.

返回：A new Godot Array.

### Fill

```csharp
public void Fill(Variant value)
```

引擎名：`fill`

Assigns the given value to all elements in the array. This can typically be
used together with `Resize()` to create an array with a given
size and initialized elements.
Note: If `value` is of a reference type ([`GodotObject`](GodotObject.md)
derived, `Collections.Array` or [`Collections.Dictionary`](Collections.Dictionary.md), etc.) then the array
is filled with the references to the same object, i.e. no duplicates are
created.

参数：
- `value`：The value to fill the array with.

### GetEnumerator

```csharp
public IEnumerator<Variant> GetEnumerator()
```

Gets an enumerator for this `Collections.Array`.

返回：An enumerator.

### GetSliceRange

```csharp
public Array GetSliceRange(int start, int end, int step = 1, bool deep = false)
```

Returns the slice of the `Collections.Array`, from `start`
(inclusive) to `end` (exclusive), as a new `Collections.Array`.
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
public int IndexOf(Variant item)
```

Searches the array for a value and returns its index or `-1` if not found.

参数：
- `item`：The [`Variant`](Variant.md) item to search for.

返回：The index of the item, or -1 if not found.

### IndexOf

```csharp
public int IndexOf(Variant item, int index)
```

Searches the array for a value and returns its index or `-1` if not found.

参数：
- `item`：The [`Variant`](Variant.md) item to search for.
- `index`：The initial search index to start from.

返回：The index of the item, or -1 if not found.

### Insert

```csharp
public void Insert(int index, Variant item)
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

Makes the `Collections.Array` read-only, i.e. disabled modying of the
array's elements. Does not apply to nested content, e.g. content of
nested arrays.

### Max

```csharp
public Variant Max()
```

引擎名：`max`

Returns the maximum value contained in the array if all elements are of
comparable types. If the elements can't be compared, `null`
is returned.

返回：The maximum value contained in the array.

### Min

```csharp
public Variant Min()
```

引擎名：`min`

Returns the minimum value contained in the array if all elements are of
comparable types. If the elements can't be compared, `null`
is returned.

返回：The minimum value contained in the array.

### PickRandom

```csharp
public Variant PickRandom()
```

引擎名：`pick_random`

Returns a random value from the target array.

返回：A random element from the array.

### RecursiveEqual

```csharp
public bool RecursiveEqual(Array other)
```

Compares this `Collections.Array` against the `other`
`Collections.Array` recursively. Returns `true` if the
sizes and contents of the arrays are equal, `false`
otherwise.

参数：
- `other`：The other array to compare against.

返回：`true` if the sizes and contents of the arrays are equal,
`false` otherwise.

### Remove

```csharp
public bool Remove(Variant item)
```

Removes the first occurrence of the specified `item`
from this `Collections.Array`.

参数：
- `item`：The value to remove.

### RemoveAt

```csharp
public void RemoveAt(int index)
```

引擎名：`remove_at`

Removes an element from the array by index.
To remove an element by searching for its value, use
`Remove()` instead.

参数：
- `index`：The index of the element to remove.

### Resize

```csharp
public Error Resize(int newSize)
```

引擎名：`resize`

Resizes the array to contain a different number of elements. If the array
size is smaller, elements are cleared, if bigger, new elements are
`null`.

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
public Array Slice(int start)
```

引擎名：`slice`

Creates a shallow copy of a range of elements in the source `Collections.Array`.

参数：
- `start`：The zero-based index at which the range starts.

返回：A new array that contains the elements inside the slice range.

### Slice

```csharp
public Array Slice(int start, int length)
```

引擎名：`slice`

Creates a shallow copy of a range of elements in the source `Collections.Array`.

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
considered equal may have their order changed when using `Sort()`.
Note: Strings are sorted in alphabetical order (as opposed to natural order).
This may lead to unexpected behavior when sorting an array of strings ending
with a sequence of numbers.
To sort with a custom predicate use
`System.Linq.Enumerable.OrderBy()`.

### ToString

```csharp
public virtual string ToString()
```

Converts this `Collections.Array` to a string.

返回：A string representation of this array.

## 运算符

### op_Addition

```csharp
public static Array op_Addition(Array left, Array right)
```

Concatenates two `Collections.Array`s together, with the `right`
being added to the end of the `Collections.Array` specified in `left`.
For example, `[1, 2] + [3, 4]` results in `[1, 2, 3, 4]`.

参数：
- `left`：The first array.
- `right`：The second array.

返回：A new Godot Array with the contents of both arrays.

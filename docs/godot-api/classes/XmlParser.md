<!-- 由 scripts/godot-api/gen-api-docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# XmlParser

命名空间 `Godot`；class；继承 [`RefCounted`](RefCounted.md) → [`GodotObject`](GodotObject.md)

Provides a low-level interface for creating parsers for [XML](https://en.wikipedia.org/wiki/XML) files. This class can serve as base to make custom XML parsers.

To parse XML, you must open a file with the `Open()` method or a buffer with the `OpenBuffer()` method. Then, the `Read()` method must be called to parse the next nodes. Most of the methods take into consideration the currently parsed node.

Here is an example of using `XmlParser` to parse an SVG file (which is based on XML), printing each element and its attributes as a dictionary:

```csharp
var parser = new XmlParser();
parser.Open("path/to/file.svg");
while (parser.Read() != Error.FileEof)
{
if (parser.GetNodeType() == XmlParser.NodeType.Element)
{
var nodeName = parser.GetNodeName();
var attributesDict = new Godot.Collections.Dictionary();
for (int idx = 0; idx < parser.GetAttributeCount(); idx++)
{
attributesDict[parser.GetAttributeName(idx)] = parser.GetAttributeValue(idx);
}
GD.Print($"The {nodeName} element has the following attributes: {attributesDict}");
}
}

```

## 构造函数

### XmlParser

```csharp
public XmlParser()
```

## 方法

### GetAttributeCount

```csharp
public int GetAttributeCount()
```

Returns the number of attributes in the currently parsed element.

**Note:** If this method is used while the currently parsed node is not [`XmlParser.NodeType.Element`](XmlParser.md) or [`XmlParser.NodeType.ElementEnd`](XmlParser.md), this count will not be updated and will still reflect the last element.

### GetAttributeName

```csharp
public string GetAttributeName(int idx)
```

Returns the name of an attribute of the currently parsed element, specified by the `idx` index.

### GetAttributeValue

```csharp
public string GetAttributeValue(int idx)
```

Returns the value of an attribute of the currently parsed element, specified by the `idx` index.

### GetCurrentLine

```csharp
public int GetCurrentLine()
```

Returns the current line in the parsed file, counting from 0.

### GetNamedAttributeValue

```csharp
public string GetNamedAttributeValue(string name)
```

Returns the value of an attribute of the currently parsed element, specified by its `name`. This method will raise an error if the element has no such attribute.

### GetNamedAttributeValueSafe

```csharp
public string GetNamedAttributeValueSafe(string name)
```

Returns the value of an attribute of the currently parsed element, specified by its `name`. This method will return an empty string if the element has no such attribute.

### GetNodeData

```csharp
public string GetNodeData()
```

Returns the contents of a text node. This method will raise an error if the current parsed node is of any other type.

### GetNodeName

```csharp
public string GetNodeName()
```

Returns the name of a node. This method will raise an error if the currently parsed node is a text node.

**Note:** The content of a [`XmlParser.NodeType.Cdata`](XmlParser.md) node and the comment string of a [`XmlParser.NodeType.Comment`](XmlParser.md) node are also considered names.

### GetNodeOffset

```csharp
public ulong GetNodeOffset()
```

Returns the byte offset of the currently parsed node since the beginning of the file or buffer. This is usually equivalent to the number of characters before the read position.

### GetNodeType

```csharp
public XmlParser.NodeType GetNodeType()
```

Returns the type of the current node. Compare with [`XmlParser.NodeType`](XmlParser.md) constants.

### HasAttribute

```csharp
public bool HasAttribute(string name)
```

Returns `true` if the currently parsed element has an attribute with the `name`.

### IsEmpty

```csharp
public bool IsEmpty()
```

Returns `true` if the currently parsed element is empty, e.g. `<element />`.

### Open

```csharp
public Error Open(string file)
```

Opens an XML `file` for parsing. This method returns an error code.

### OpenBuffer

```csharp
public Error OpenBuffer(ReadOnlySpan<byte> buffer)
```

Opens an XML raw `buffer` for parsing. This method returns an error code.

### OpenBuffer

```csharp
public Error OpenBuffer(byte[] buffer)
```

Opens an XML raw `buffer` for parsing. This method returns an error code.

### Read

```csharp
public Error Read()
```

Parses the next node in the file. This method returns an error code.

### Seek

```csharp
public Error Seek(ulong position)
```

Moves the buffer cursor to a certain offset (since the beginning) and reads the next node there. This method returns an error code.

### SkipSection

```csharp
public void SkipSection()
```

Skips the current section. If the currently parsed node contains more inner nodes, they will be ignored and the cursor will go to the closing of the current element.

## NodeType

命名空间 `Godot`；enum

### 枚举值

- `None = 0`：There's no node (no file or buffer opened).
- `Element = 1`：An element node type, also known as a tag, e.g.
- `ElementEnd = 2`：An end of element node type, e.g.
- `Text = 3`：A text node type, i.e.
- `Comment = 4`：A comment node type, e.g.
- `Cdata = 5`：A node type for CDATA (Character Data) sections, e.g.
- `Unknown = 6`：An unknown node type.

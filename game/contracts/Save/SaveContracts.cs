using Campus.Contracts.Maps;

namespace Campus.Contracts.Save;

public readonly record struct SlotId(string Value);   // "s1"–"s3"：三个存档位，文件夹名就是 Value

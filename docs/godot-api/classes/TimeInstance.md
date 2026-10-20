<!-- 由 scripts/godot-api/gen-api-docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# TimeInstance

命名空间 `Godot`；class；继承 [`GodotObject`](GodotObject.md)

The Time singleton allows converting time between various formats and also getting time information from the system.

This class conforms with as many of the ISO 8601 standards as possible. All dates follow the Proleptic Gregorian calendar. As such, the day before `1582-10-15` is `1582-10-14`, not `1582-10-04`. The year before 1 AD (aka 1 BC) is number `0`, with the year before that (2 BC) being `-1`, etc.

Conversion methods assume "the same timezone", and do not handle timezone conversions or DST automatically. Leap seconds are also not handled, they must be done manually if desired. Suffixes such as "Z" are not handled, you need to strip them away manually.

When getting time information from the system, the time can either be in the local timezone or UTC depending on the `utc` parameter. However, the `GetUnixTimeFromSystem()` method always uses UTC as it returns the seconds passed since the [Unix epoch](https://en.wikipedia.org/wiki/Unix_time).

**Important:** The `_from_system` methods use the system clock that the user can manually set. **Never use** this method for precise time calculation since its results are subject to automatic adjustments by the user or the operating system. **Always use** `GetTicksUsec()` or `GetTicksMsec()` for precise time calculation instead, since they are guaranteed to be monotonic (i.e. never decrease).

## 方法

### GetDateDictFromSystem

```csharp
public Dictionary GetDateDictFromSystem(bool utc = false)
```

Returns the current date as a dictionary of keys: `year`, `month`, `day`, and `weekday`.

The returned values are in the system's local time when `utc` is `false`, otherwise they are in UTC.

### GetDateDictFromUnixTime

```csharp
public Dictionary GetDateDictFromUnixTime(long unixTimeVal)
```

Converts the given Unix timestamp to a dictionary of keys: `year`, `month`, `day`, and `weekday`.

### GetDateStringFromSystem

```csharp
public string GetDateStringFromSystem(bool utc = false)
```

Returns the current date as an ISO 8601 date string (YYYY-MM-DD).

The returned values are in the system's local time when `utc` is `false`, otherwise they are in UTC.

### GetDateStringFromUnixTime

```csharp
public string GetDateStringFromUnixTime(long unixTimeVal)
```

Converts the given Unix timestamp to an ISO 8601 date string (YYYY-MM-DD).

### GetDatetimeDictFromDatetimeString

```csharp
public Dictionary GetDatetimeDictFromDatetimeString(string datetime, bool weekday)
```

Converts the given ISO 8601 date and time string (YYYY-MM-DDTHH:MM:SS) to a dictionary of keys: `year`, `month`, `day`, `weekday`, `hour`, `minute`, and `second`.

If `weekday` is `false`, then the `weekday` entry is excluded (the calculation is relatively expensive).

**Note:** Any decimal fraction in the time string will be ignored silently.

### GetDatetimeDictFromSystem

```csharp
public Dictionary GetDatetimeDictFromSystem(bool utc = false)
```

Returns the current date as a dictionary of keys: `year`, `month`, `day`, `weekday`, `hour`, `minute`, `second`, and `dst` (Daylight Savings Time).

### GetDatetimeDictFromUnixTime

```csharp
public Dictionary GetDatetimeDictFromUnixTime(long unixTimeVal)
```

Converts the given Unix timestamp to a dictionary of keys: `year`, `month`, `day`, `weekday`, `hour`, `minute`, and `second`.

The returned Dictionary's values will be the same as the `GetDatetimeDictFromSystem()` if the Unix timestamp is the current time, with the exception of Daylight Savings Time as it cannot be determined from the epoch.

### GetDatetimeStringFromDatetimeDict

```csharp
public string GetDatetimeStringFromDatetimeDict(Dictionary datetime, bool useSpace)
```

Converts the given dictionary of keys to an ISO 8601 date and time string (YYYY-MM-DDTHH:MM:SS).

The given dictionary can be populated with the following keys: `year`, `month`, `day`, `hour`, `minute`, and `second`. Any other entries (including `dst`) are ignored.

If the dictionary is empty, `0` is returned. If some keys are omitted, they default to the equivalent values for the Unix epoch timestamp 0 (1970-01-01 at 00:00:00).

If `useSpace` is `true`, the date and time bits are separated by an empty space character instead of the letter T.

### GetDatetimeStringFromSystem

```csharp
public string GetDatetimeStringFromSystem(bool utc = false, bool useSpace = false)
```

Returns the current date and time as an ISO 8601 date and time string (YYYY-MM-DDTHH:MM:SS).

The returned values are in the system's local time when `utc` is `false`, otherwise they are in UTC.

If `useSpace` is `true`, the date and time bits are separated by an empty space character instead of the letter T.

### GetDatetimeStringFromUnixTime

```csharp
public string GetDatetimeStringFromUnixTime(long unixTimeVal, bool useSpace = false)
```

Converts the given Unix timestamp to an ISO 8601 date and time string (YYYY-MM-DDTHH:MM:SS).

If `useSpace` is `true`, the date and time bits are separated by an empty space character instead of the letter T.

### GetOffsetStringFromOffsetMinutes

```csharp
public string GetOffsetStringFromOffsetMinutes(long offsetMinutes)
```

Converts the given timezone offset in minutes to a timezone offset string. For example, -480 returns "-08:00", 345 returns "+05:45", and 0 returns "+00:00".

### GetTicksMsec

```csharp
public ulong GetTicksMsec()
```

Returns the amount of time passed in milliseconds since the engine started.

Will always be positive or 0 and uses a 64-bit value (it will wrap after roughly 500 million years).

### GetTicksUsec

```csharp
public ulong GetTicksUsec()
```

Returns the amount of time passed in microseconds since the engine started.

Will always be positive or 0 and uses a 64-bit value (it will wrap after roughly half a million years).

### GetTimeDictFromSystem

```csharp
public Dictionary GetTimeDictFromSystem(bool utc = false)
```

Returns the current time as a dictionary of keys: `hour`, `minute`, and `second`.

The returned values are in the system's local time when `utc` is `false`, otherwise they are in UTC.

### GetTimeDictFromUnixTime

```csharp
public Dictionary GetTimeDictFromUnixTime(long unixTimeVal)
```

Converts the given time to a dictionary of keys: `hour`, `minute`, and `second`.

### GetTimeStringFromSystem

```csharp
public string GetTimeStringFromSystem(bool utc = false)
```

Returns the current time as an ISO 8601 time string (HH:MM:SS).

The returned values are in the system's local time when `utc` is `false`, otherwise they are in UTC.

### GetTimeStringFromUnixTime

```csharp
public string GetTimeStringFromUnixTime(long unixTimeVal)
```

Converts the given Unix timestamp to an ISO 8601 time string (HH:MM:SS).

### GetTimeZoneFromSystem

```csharp
public Dictionary GetTimeZoneFromSystem()
```

Returns the current time zone as a dictionary of keys: `bias` and `name`.

- `bias` is the offset from UTC in minutes, since not all time zones are multiples of an hour from UTC.

- `name` is the localized name of the time zone, according to the OS locale settings of the current user.

### GetUnixTimeFromDatetimeDict

```csharp
public long GetUnixTimeFromDatetimeDict(Dictionary datetime)
```

Converts a dictionary of time values to a Unix timestamp.

The given dictionary can be populated with the following keys: `year`, `month`, `day`, `hour`, `minute`, and `second`. Any other entries (including `dst`) are ignored.

If the dictionary is empty, `0` is returned. If some keys are omitted, they default to the equivalent values for the Unix epoch timestamp 0 (1970-01-01 at 00:00:00).

You can pass the output from `GetDatetimeDictFromUnixTime()` directly into this function and get the same as what was put in.

**Note:** Unix timestamps are often in UTC. This method does not do any timezone conversion, so the timestamp will be in the same timezone as the given datetime dictionary.

### GetUnixTimeFromDatetimeString

```csharp
public long GetUnixTimeFromDatetimeString(string datetime)
```

Converts the given ISO 8601 date and/or time string to a Unix timestamp. The string can contain a date only, a time only, or both.

**Note:** Unix timestamps are often in UTC. This method does not do any timezone conversion, so the timestamp will be in the same timezone as the given datetime string.

**Note:** Any decimal fraction in the time string will be ignored silently.

### GetUnixTimeFromSystem

```csharp
public double GetUnixTimeFromSystem()
```

Returns the current Unix timestamp in seconds based on the system time in UTC. This method is implemented by the operating system and always returns the time in UTC. The Unix timestamp is the number of seconds passed since 1970-01-01 at 00:00:00, the [Unix epoch](https://en.wikipedia.org/wiki/Unix_time).

**Note:** Unlike other methods that use integer timestamps, this method returns the timestamp as a `System.Single` for sub-second precision.

<!-- 由 scripts/godot-api/gen-api-docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# Time

命名空间 `Godot`；static class

The Time singleton allows converting time between various formats and also getting time information from the system.

This class conforms with as many of the ISO 8601 standards as possible. All dates follow the Proleptic Gregorian calendar. As such, the day before `1582-10-15` is `1582-10-14`, not `1582-10-04`. The year before 1 AD (aka 1 BC) is number `0`, with the year before that (2 BC) being `-1`, etc.

Conversion methods assume "the same timezone", and do not handle timezone conversions or DST automatically. Leap seconds are also not handled, they must be done manually if desired. Suffixes such as "Z" are not handled, you need to strip them away manually.

When getting time information from the system, the time can either be in the local timezone or UTC depending on the `utc` parameter. However, the `GetUnixTimeFromSystem()` method always uses UTC as it returns the seconds passed since the [Unix epoch](https://en.wikipedia.org/wiki/Unix_time).

**Important:** The `_from_system` methods use the system clock that the user can manually set. **Never use** this method for precise time calculation since its results are subject to automatic adjustments by the user or the operating system. **Always use** `GetTicksUsec()` or `GetTicksMsec()` for precise time calculation instead, since they are guaranteed to be monotonic (i.e. never decrease).

## 属性

### Singleton

```csharp
public static TimeInstance Singleton { get; }
```

## 方法

### GetDateDictFromSystem

```csharp
public static Dictionary GetDateDictFromSystem(bool utc = false)
```

引擎名：`get_date_dict_from_system`

Returns the current date as a dictionary of keys: `year`, `month`, `day`, and `weekday`.

The returned values are in the system's local time when `utc` is `false`, otherwise they are in UTC.

### GetDateDictFromUnixTime

```csharp
public static Dictionary GetDateDictFromUnixTime(long unixTimeVal)
```

引擎名：`get_date_dict_from_unix_time`

Converts the given Unix timestamp to a dictionary of keys: `year`, `month`, `day`, and `weekday`.

### GetDateStringFromSystem

```csharp
public static string GetDateStringFromSystem(bool utc = false)
```

引擎名：`get_date_string_from_system`

Returns the current date as an ISO 8601 date string (YYYY-MM-DD).

The returned values are in the system's local time when `utc` is `false`, otherwise they are in UTC.

### GetDateStringFromUnixTime

```csharp
public static string GetDateStringFromUnixTime(long unixTimeVal)
```

引擎名：`get_date_string_from_unix_time`

Converts the given Unix timestamp to an ISO 8601 date string (YYYY-MM-DD).

### GetDatetimeDictFromDatetimeString

```csharp
public static Dictionary GetDatetimeDictFromDatetimeString(string datetime, bool weekday)
```

引擎名：`get_datetime_dict_from_datetime_string`

Converts the given ISO 8601 date and time string (YYYY-MM-DDTHH:MM:SS) to a dictionary of keys: `year`, `month`, `day`, `weekday`, `hour`, `minute`, and `second`.

If `weekday` is `false`, then the `weekday` entry is excluded (the calculation is relatively expensive).

**Note:** Any decimal fraction in the time string will be ignored silently.

### GetDatetimeDictFromSystem

```csharp
public static Dictionary GetDatetimeDictFromSystem(bool utc = false)
```

引擎名：`get_datetime_dict_from_system`

Returns the current date as a dictionary of keys: `year`, `month`, `day`, `weekday`, `hour`, `minute`, `second`, and `dst` (Daylight Savings Time).

### GetDatetimeDictFromUnixTime

```csharp
public static Dictionary GetDatetimeDictFromUnixTime(long unixTimeVal)
```

引擎名：`get_datetime_dict_from_unix_time`

Converts the given Unix timestamp to a dictionary of keys: `year`, `month`, `day`, `weekday`, `hour`, `minute`, and `second`.

The returned Dictionary's values will be the same as the `GetDatetimeDictFromSystem()` if the Unix timestamp is the current time, with the exception of Daylight Savings Time as it cannot be determined from the epoch.

### GetDatetimeStringFromDatetimeDict

```csharp
public static string GetDatetimeStringFromDatetimeDict(Dictionary datetime, bool useSpace)
```

引擎名：`get_datetime_string_from_datetime_dict`

Converts the given dictionary of keys to an ISO 8601 date and time string (YYYY-MM-DDTHH:MM:SS).

The given dictionary can be populated with the following keys: `year`, `month`, `day`, `hour`, `minute`, and `second`. Any other entries (including `dst`) are ignored.

If the dictionary is empty, `0` is returned. If some keys are omitted, they default to the equivalent values for the Unix epoch timestamp 0 (1970-01-01 at 00:00:00).

If `useSpace` is `true`, the date and time bits are separated by an empty space character instead of the letter T.

### GetDatetimeStringFromSystem

```csharp
public static string GetDatetimeStringFromSystem(bool utc = false, bool useSpace = false)
```

引擎名：`get_datetime_string_from_system`

Returns the current date and time as an ISO 8601 date and time string (YYYY-MM-DDTHH:MM:SS).

The returned values are in the system's local time when `utc` is `false`, otherwise they are in UTC.

If `useSpace` is `true`, the date and time bits are separated by an empty space character instead of the letter T.

### GetDatetimeStringFromUnixTime

```csharp
public static string GetDatetimeStringFromUnixTime(long unixTimeVal, bool useSpace = false)
```

引擎名：`get_datetime_string_from_unix_time`

Converts the given Unix timestamp to an ISO 8601 date and time string (YYYY-MM-DDTHH:MM:SS).

If `useSpace` is `true`, the date and time bits are separated by an empty space character instead of the letter T.

### GetOffsetStringFromOffsetMinutes

```csharp
public static string GetOffsetStringFromOffsetMinutes(long offsetMinutes)
```

引擎名：`get_offset_string_from_offset_minutes`

Converts the given timezone offset in minutes to a timezone offset string. For example, -480 returns "-08:00", 345 returns "+05:45", and 0 returns "+00:00".

### GetTicksMsec

```csharp
public static ulong GetTicksMsec()
```

引擎名：`get_ticks_msec`

Returns the amount of time passed in milliseconds since the engine started.

Will always be positive or 0 and uses a 64-bit value (it will wrap after roughly 500 million years).

### GetTicksUsec

```csharp
public static ulong GetTicksUsec()
```

引擎名：`get_ticks_usec`

Returns the amount of time passed in microseconds since the engine started.

Will always be positive or 0 and uses a 64-bit value (it will wrap after roughly half a million years).

### GetTimeDictFromSystem

```csharp
public static Dictionary GetTimeDictFromSystem(bool utc = false)
```

引擎名：`get_time_dict_from_system`

Returns the current time as a dictionary of keys: `hour`, `minute`, and `second`.

The returned values are in the system's local time when `utc` is `false`, otherwise they are in UTC.

### GetTimeDictFromUnixTime

```csharp
public static Dictionary GetTimeDictFromUnixTime(long unixTimeVal)
```

引擎名：`get_time_dict_from_unix_time`

Converts the given time to a dictionary of keys: `hour`, `minute`, and `second`.

### GetTimeStringFromSystem

```csharp
public static string GetTimeStringFromSystem(bool utc = false)
```

引擎名：`get_time_string_from_system`

Returns the current time as an ISO 8601 time string (HH:MM:SS).

The returned values are in the system's local time when `utc` is `false`, otherwise they are in UTC.

### GetTimeStringFromUnixTime

```csharp
public static string GetTimeStringFromUnixTime(long unixTimeVal)
```

引擎名：`get_time_string_from_unix_time`

Converts the given Unix timestamp to an ISO 8601 time string (HH:MM:SS).

### GetTimeZoneFromSystem

```csharp
public static Dictionary GetTimeZoneFromSystem()
```

引擎名：`get_time_zone_from_system`

Returns the current time zone as a dictionary of keys: `bias` and `name`.

- `bias` is the offset from UTC in minutes, since not all time zones are multiples of an hour from UTC.

- `name` is the localized name of the time zone, according to the OS locale settings of the current user.

### GetUnixTimeFromDatetimeDict

```csharp
public static long GetUnixTimeFromDatetimeDict(Dictionary datetime)
```

引擎名：`get_unix_time_from_datetime_dict`

Converts a dictionary of time values to a Unix timestamp.

The given dictionary can be populated with the following keys: `year`, `month`, `day`, `hour`, `minute`, and `second`. Any other entries (including `dst`) are ignored.

If the dictionary is empty, `0` is returned. If some keys are omitted, they default to the equivalent values for the Unix epoch timestamp 0 (1970-01-01 at 00:00:00).

You can pass the output from `GetDatetimeDictFromUnixTime()` directly into this function and get the same as what was put in.

**Note:** Unix timestamps are often in UTC. This method does not do any timezone conversion, so the timestamp will be in the same timezone as the given datetime dictionary.

### GetUnixTimeFromDatetimeString

```csharp
public static long GetUnixTimeFromDatetimeString(string datetime)
```

引擎名：`get_unix_time_from_datetime_string`

Converts the given ISO 8601 date and/or time string to a Unix timestamp. The string can contain a date only, a time only, or both.

**Note:** Unix timestamps are often in UTC. This method does not do any timezone conversion, so the timestamp will be in the same timezone as the given datetime string.

**Note:** Any decimal fraction in the time string will be ignored silently.

### GetUnixTimeFromSystem

```csharp
public static double GetUnixTimeFromSystem()
```

引擎名：`get_unix_time_from_system`

Returns the current Unix timestamp in seconds based on the system time in UTC. This method is implemented by the operating system and always returns the time in UTC. The Unix timestamp is the number of seconds passed since 1970-01-01 at 00:00:00, the [Unix epoch](https://en.wikipedia.org/wiki/Unix_time).

**Note:** Unlike other methods that use integer timestamps, this method returns the timestamp as a `System.Single` for sub-second precision.

## Month

命名空间 `Godot`；enum

### 枚举值

- `January = 1`：The month of January, represented numerically as `01`.
- `February = 2`：The month of February, represented numerically as `02`.
- `March = 3`：The month of March, represented numerically as `03`.
- `April = 4`：The month of April, represented numerically as `04`.
- `May = 5`：The month of May, represented numerically as `05`.
- `June = 6`：The month of June, represented numerically as `06`.
- `July = 7`：The month of July, represented numerically as `07`.
- `August = 8`：The month of August, represented numerically as `08`.
- `September = 9`：The month of September, represented numerically as `09`.
- `October = 10`：The month of October, represented numerically as `10`.
- `November = 11`：The month of November, represented numerically as `11`.
- `December = 12`：The month of December, represented numerically as `12`.

## Weekday

命名空间 `Godot`；enum

### 枚举值

- `Sunday = 0`：The day of the week Sunday, represented numerically as `0`.
- `Monday = 1`：The day of the week Monday, represented numerically as `1`.
- `Tuesday = 2`：The day of the week Tuesday, represented numerically as `2`.
- `Wednesday = 3`：The day of the week Wednesday, represented numerically as `3`.
- `Thursday = 4`：The day of the week Thursday, represented numerically as `4`.
- `Friday = 5`：The day of the week Friday, represented numerically as `5`.
- `Saturday = 6`：The day of the week Saturday, represented numerically as `6`.

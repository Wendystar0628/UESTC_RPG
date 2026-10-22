using System.Runtime.CompilerServices;

namespace Campus.Contracts;

/// <summary>Pure .NET derived values owned by the frozen world or context that supplies their inputs.</summary>
public static class DerivedMemo
{
    private static readonly ConditionalWeakTable<object, Dictionary<(Type Type, object Key), Lazy<object?>>> Values = new();

    /// <summary>Remembers a value derived only from this frozen world, including null results and factory failures.</summary>
    public static T ForWorld<T>(IWorldModel world, object key, Func<T> create) => ForOwner(world, key, create);

    internal static T ForOwner<T>(object owner, object key, Func<T> create)
    {
        ArgumentNullException.ThrowIfNull(owner);
        ArgumentNullException.ThrowIfNull(key);
        ArgumentNullException.ThrowIfNull(create);
        var memo = Values.GetValue(owner, static _ => []);
        Lazy<object?> value;
        lock (memo)
        {
            var entry = (typeof(T), key);
            if (!memo.TryGetValue(entry, out value!))
            {
                value = new Lazy<object?>(() => create(), LazyThreadSafetyMode.ExecutionAndPublication);
                memo.Add(entry, value);
            }
        }
        return (T)value.Value!;
    }
}

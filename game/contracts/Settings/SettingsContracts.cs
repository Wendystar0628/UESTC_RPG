namespace Campus.Contracts.Settings;

/// <summary>Profile settings shared by section; the registered store owns persistence.</summary>
public interface ISettingsStore
{
    ISettingsSection Section(string name);
    bool TrySave();
    Task SaveAsync();
}

/// <summary>Primitive settings within one section, with caller-defined defaults and ranges.</summary>
public interface ISettingsSection
{
    ISettingsSection Child(string name);
    bool Has(string key);
    IReadOnlyList<string> Keys();
    bool Bool(string key, bool fallback);
    int Int(string key, int fallback, int min, int max);
    float Float(string key, float fallback, float min, float max);
    bool TryFloat(string key, out float value);
    string? String(string key);
    void Set(string key, bool value);
    void Set(string key, int value);
    void Set(string key, float value);
    void Set(string key, string value);
    void Remove(string key);
    void Clear();
}

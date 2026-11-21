namespace FlagsRallyTests;

/// <summary>An in-memory stand-in for the app's preferences.</summary>
sealed class MemoryPreferences : IPreferences
{
    readonly Dictionary<string, object?> _values = [];
    public bool ContainsKey(string key, string? sharedName = null) => _values.ContainsKey(key);
    public void Remove(string key, string? sharedName = null) => _values.Remove(key);
    public void Clear(string? sharedName = null) => _values.Clear();
    public void Set<T>(string key, T value, string? sharedName = null) => _values[key] = value;
    public T Get<T>(string key, T defaultValue, string? sharedName = null) =>
        _values.TryGetValue(key, out var value) && value is T typed ? typed : defaultValue;
}

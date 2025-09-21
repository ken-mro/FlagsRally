using System.Text.Json;

namespace FlagsRally.Helpers;

public static class ExtensionDataHelper
{
    /// <summary>
    /// Gets a typed value from extension data
    /// </summary>
    /// <typeparam name="T">The type to deserialize to</typeparam>
    /// <param name="extensionData">The extension data dictionary</param>
    /// <param name="propertyName">The property name to look for</param>
    /// <param name="defaultValue">Default value if property not found</param>
    /// <returns>The typed value or default</returns>
    public static T? GetValue<T>(Dictionary<string, JsonElement>? extensionData, string propertyName, T? defaultValue = default)
    {
        if (extensionData == null || !extensionData.ContainsKey(propertyName))
            return defaultValue;

        try
        {
            var jsonElement = extensionData[propertyName];
            return JsonSerializer.Deserialize<T>(jsonElement.GetRawText());
        }
        catch
        {
            return defaultValue;
        }
    }

    /// <summary>
    /// Gets a string value from extension data
    /// </summary>
    /// <param name="extensionData">The extension data dictionary</param>
    /// <param name="propertyName">The property name to look for</param>
    /// <param name="defaultValue">Default value if property not found</param>
    /// <returns>The string value or default</returns>
    public static string GetString(Dictionary<string, JsonElement>? extensionData, string propertyName, string defaultValue = "")
    {
        return GetValue(extensionData, propertyName, defaultValue) ?? defaultValue;
    }

    /// <summary>
    /// Gets an integer value from extension data
    /// </summary>
    /// <param name="extensionData">The extension data dictionary</param>
    /// <param name="propertyName">The property name to look for</param>
    /// <param name="defaultValue">Default value if property not found</param>
    /// <returns>The integer value or default</returns>
    public static int GetInt(Dictionary<string, JsonElement>? extensionData, string propertyName, int defaultValue = 0)
    {
        return GetValue(extensionData, propertyName, defaultValue);
    }

    /// <summary>
    /// Gets a boolean value from extension data
    /// </summary>
    /// <param name="extensionData">The extension data dictionary</param>
    /// <param name="propertyName">The property name to look for</param>
    /// <param name="defaultValue">Default value if property not found</param>
    /// <returns>The boolean value or default</returns>
    public static bool GetBool(Dictionary<string, JsonElement>? extensionData, string propertyName, bool defaultValue = false)
    {
        return GetValue(extensionData, propertyName, defaultValue);
    }

    /// <summary>
    /// Gets a double value from extension data
    /// </summary>
    /// <param name="extensionData">The extension data dictionary</param>
    /// <param name="propertyName">The property name to look for</param>
    /// <param name="defaultValue">Default value if property not found</param>
    /// <returns>The double value or default</returns>
    public static double GetDouble(Dictionary<string, JsonElement>? extensionData, string propertyName, double defaultValue = 0.0)
    {
        return GetValue(extensionData, propertyName, defaultValue);
    }

    /// <summary>
    /// Checks if a property exists in extension data
    /// </summary>
    /// <param name="extensionData">The extension data dictionary</param>
    /// <param name="propertyName">The property name to check</param>
    /// <returns>True if the property exists</returns>
    public static bool HasProperty(Dictionary<string, JsonElement>? extensionData, string propertyName)
    {
        return extensionData?.ContainsKey(propertyName) == true;
    }

    /// <summary>
    /// Gets all property names from extension data
    /// </summary>
    /// <param name="extensionData">The extension data dictionary</param>
    /// <returns>Collection of property names</returns>
    public static IEnumerable<string> GetPropertyNames(Dictionary<string, JsonElement>? extensionData)
    {
        return extensionData?.Keys ?? Enumerable.Empty<string>();
    }

    /// <summary>
    /// Serializes extension data to JSON string for storage
    /// </summary>
    /// <param name="extensionData">The extension data dictionary</param>
    /// <returns>JSON string or null</returns>
    public static string? SerializeExtensionData(Dictionary<string, JsonElement>? extensionData)
    {
        if (extensionData == null || extensionData.Count == 0)
            return null;

        return JsonSerializer.Serialize(extensionData);
    }

    /// <summary>
    /// Deserializes JSON string back to extension data dictionary
    /// </summary>
    /// <param name="json">The JSON string</param>
    /// <returns>Extension data dictionary or null</returns>
    public static Dictionary<string, JsonElement>? DeserializeExtensionData(string? json)
    {
        if (string.IsNullOrEmpty(json))
            return null;

        try
        {
            return JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(json);
        }
        catch
        {
            return null;
        }
    }
}
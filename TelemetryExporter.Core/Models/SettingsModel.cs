namespace TelemetryExporter.Core.Models
{
    /// <summary>
    /// Example:
    /// <![CDATA[
    /// SettingsModel<string>, SettingsModel<int>, SettingsModel<double>, SettingsModel<bool>, SettingsModel<DateTime>, SettingsModel<List<string>>
    /// ]]>
    /// <para/>
    /// The property type will be used to determine how to render the setting in the UI.
    /// </summary>
    /// <param name="RecommendedType">Used to determine control for the UI! Could be different from typeof(<see cref="Value"/>)</param>
    public record SettingsModel(
        string Title,
        object? Value,
        Type? RecommendedType = null,
        string? Info = null)
    {
        T? GetValue<T>() => (T?)Value!;
        Type GetValueType() => RecommendedType ?? Value!.GetType();
    }
}

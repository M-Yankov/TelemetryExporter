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
    /// <param name="CanBeDisabled">Some widget may use or not at all the widget property. Example background color.</param>
    public record SettingsModel(
        string Title,
        object? Value,
        Type? RecommendedType = null,
        string? Info = null,
        bool CanBeDisabled = true,
        float? Min = null,
        float? Max = null)
    {
        public T? GetValue<T>() => (T?)Value!;
        public Type GetValueType() => RecommendedType ?? Value!.GetType();
    }
}

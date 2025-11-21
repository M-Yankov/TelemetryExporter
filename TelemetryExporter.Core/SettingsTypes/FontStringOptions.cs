namespace TelemetryExporter.Core.SettingsTypes
{
    /// <summary>
    /// A type to indicate using options of string fonts.
    /// </summary>
    public class FontStringOptions
    {
        /// <summary>
        /// A limited set of font family names.
        /// </summary>
        public static List<string> Values => [
            "Consolas",
            "Arial",
            "Verdana",
            "Times New Roman",
            "Courier New",
            "Georgia",
            "Trebuchet MS",
            "Impact",
            ];
    }
}

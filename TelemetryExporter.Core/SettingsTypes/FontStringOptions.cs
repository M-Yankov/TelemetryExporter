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
            "Arial",
            "Consolas",
            "Courier New",
            "Impact",
            "Georgia",
            "Times New Roman",
            "Trebuchet MS",
            "Verdana",
            ];
    }
}

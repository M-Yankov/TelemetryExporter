namespace TelemetryExporter.UI.Resources
{
    /// <summary>
    /// TE = TelemetryExporter 
    /// </summary>
    internal static class TEConstants
    {
        public const string ApplicationName = "Telemetry Exporter";

        internal static class QueryKeys
        {
            public const string FitStreamKey = "FitStream";

            public const string SelectedFileName = "SelectedFile";
        }

        internal static class FileExtensions
        {
            public const string GarminActivity = ".fit";
            public const string GarminMacCatalystActivity = "fit";
        }
    }
}

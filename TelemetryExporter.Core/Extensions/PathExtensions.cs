namespace TelemetryExporter.Core.Extensions
{
    public static class PathExtensions
    {
        /// <summary>
        /// Using to access solution resources like images with "BuildAcion=None", "CopyToOutputDirectory=CopyAlways" <para />
        /// Path.Combine([AppDomain.CurrentDomain.BaseDirectory, ..paths])
        /// </summary>
        /// <param name="paths">Example: ["Images", "example.png"]</param>
        public static string Combine(params string[] paths)
            => Path.Combine([AppDomain.CurrentDomain.BaseDirectory, ..paths]);
    }
}

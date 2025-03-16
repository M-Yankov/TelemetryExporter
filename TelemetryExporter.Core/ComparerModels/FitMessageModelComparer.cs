using TelemetryExporter.Core.Models;

namespace TelemetryExporter.Core.ComparerModels
{
    public class FitMessageModelComparer : IComparer<FitMessageModel>
    {
        public int Compare(FitMessageModel? x, FitMessageModel? y)
        {
            if (x is null && y is null)
            {
                return 0;
            }

            if (x is null)
            {
                return -1;
            }

            if (y is null)
            {
                return 1;
            }

            return x.RecordDateTime.CompareTo(y.RecordDateTime);
        }
    }
}

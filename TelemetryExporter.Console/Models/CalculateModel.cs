using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TelemetryExporter.Console.Models
{
    public class CalculateModel
    {
        public CalculateModel(System.DateTime previousDate, System.DateTime nextDate, double? previousValue, double? nextValue, System.DateTime calculateAt)
        {
            PreviousDate = previousDate;
            NextDate = nextDate;
            PreviousValue = previousValue;
            NextValue = nextValue;
            CalculateAt = calculateAt;
        }

        public System.DateTime PreviousDate { get; set; }

        public System.DateTime NextDate { get; set; }

        public double? PreviousValue { get; set; }

        public double? NextValue { get; set; }

        public System.DateTime CalculateAt { get; set; }
    }
}

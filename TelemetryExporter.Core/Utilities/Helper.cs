using TelemetryExporter.Core.Models;

namespace TelemetryExporter.Core.Utilities
{
    public static class Helper
    {
        /// <summary>
        /// The method cannot be converted to generic because of <see langword="double"/> type usage
        /// which is the result from devision between two TimeSpans objects.  
        /// </summary>
        public static double? GetValueBetweenDates(CalculateModel calculateModel)
        {
            DateTime previousDate = calculateModel.PreviousDate;
            DateTime nextDate = calculateModel.NextDate;
            DateTime dateForCalculation = calculateModel.CalculateAt;

            double? previousValue = calculateModel.PreviousValue;
            double? nextValue = calculateModel.NextValue;

            if ((previousDate < dateForCalculation && dateForCalculation < nextDate) == false)
            {
                throw new ArgumentException("dateForCalculation should be between previous and next records!");
            }

            TimeSpan timeDifference = nextDate - previousDate;
            TimeSpan timeForCalculationInRange = dateForCalculation - previousDate;

            double? difference = nextValue - previousValue;

            if (!difference.HasValue || previousValue.HasValue == false)
            {
                return null;
            }

            // no change in value. Return the same value
            if (difference.HasValue && difference.Value == 0)
            {
                return previousValue.Value;
            }

            double valueDifference = Math.Abs(difference.Value);

            // should we use seconds here !
            double percentageOfTotalTime = timeForCalculationInRange / timeDifference;

            bool isAscending = nextValue - previousValue > 0;
            bool isDescending = nextValue - previousValue < 0;

            double result = previousValue.Value;

            if (isAscending)
            {
                result += (valueDifference * percentageOfTotalTime);
            }
            else if (isDescending)
            {
                result -= (valueDifference * percentageOfTotalTime);
            }

            return result;
        }
    }
}

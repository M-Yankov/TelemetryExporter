using TelemetryExporter.Console.Models;

namespace TelemetryExporter.Console.Utilities
{
    public static class Helper
    {
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

            // no change in speed. Return the same speed
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

using Dynastream.Fit;

using TelemetryExporter.Core.Models;

namespace TelemetryExporter.Core.Utilities
{
    public class FitInitializer
    {
        /// <summary>
        /// Used to calculate the grade %,
        /// calculations below that threshold gives significant deviations (like 100% incline which is not normal).
        /// </summary>
        private const int ThresholdDistanceMeters = 5;

        public System.DateTime StartDate { get; private set; }

        public System.DateTime EndDate { get; private set; }

        public double FirstDistance { get; set; }

        /// <summary>
        /// Total distance
        /// </summary>
        public double Distance { get; private set; }

        public IReadOnlyCollection<FitMessageModel> Records { get; private set; } = [];

        public IReadOnlyDictionary<System.DateTime, double?> Grades { get; private set; }
            = new Dictionary<System.DateTime, double?>();

        public ushort MaxPower { get; private set; }

        public double MaxSpeed { get; private set; }

        public IReadOnlyCollection<(System.DateTime start, System.DateTime end)> ActivePeriods { get; private set; } = [];
        public IReadOnlyCollection<(System.DateTime start, System.DateTime end)> PausePeriods { get; private set; } = [];
        public IReadOnlyCollection<ChartDataModel> ChartDataStats { get; private set; } = [];

        /// <summary>
        /// Prevent initialization.
        /// </summary>
        private FitInitializer()
        {
        }

        public static FitInitializer Initialize(
            FitMessages fitMessages,
            System.DateTime? startDate = null,
            System.DateTime? endDate = null,
            bool addChartdataWhenOnlyInRange = false)
        {
            List<FitMessageModel> messages = [];
            Dictionary<System.DateTime, double?> gradesResults = [];
            List<ChartDataModel> chartDataStats = [];

            FitInitializer fitInitializer = new();
            fitInitializer.StartDate = startDate ?? fitMessages.RecordMesgs[0].GetTimestamp().GetDateTime();
            fitInitializer.EndDate = endDate ?? fitMessages.RecordMesgs[^1].GetTimestamp().GetDateTime();

            double maxSpeed = 0;
            ushort maxPower = 0;
            double? firstDistance = null;
            double lastRecordDitance = 0;

            bool isWholeRange = startDate.HasValue == false && endDate.HasValue == false;

            RecordMesg recordMesgForGrade = fitMessages.RecordMesgs[0];

            for (int i = 0; i < fitMessages.RecordMesgs.Count; i++)
            {
                RecordMesg rec = fitMessages.RecordMesgs[i];
                System.DateTime recordDateTime = rec.GetTimestamp().GetDateTime().ToUniversalTime();

                bool inStartRange = !startDate.HasValue || startDate.Value.ToUniversalTime() <= recordDateTime;
                bool inEndRange = !endDate.HasValue || recordDateTime <= endDate.Value.ToUniversalTime();

                bool addChartData = (addChartdataWhenOnlyInRange && inStartRange && inEndRange)
                    || (addChartdataWhenOnlyInRange == false);

                if (addChartData)
                {
                    ChartDataModel chartData = new()
                    {
                        Altitude = rec.GetEnhancedAltitude(),
                        Latitude = rec.GetPositionLat(),
                        Longitude = rec.GetPositionLong(),
                        RecordDateTime = rec.GetTimestamp().GetDateTime()
                    };

                    chartDataStats.Add(chartData);
                }

                if (!inStartRange || !inEndRange)
                {
                    continue;
                }

                FitMessageModel message = new()
                {
                    Altitude = rec.GetEnhancedAltitude(),
                    Speed = rec.GetEnhancedSpeed(),
                    Distance = rec.GetDistance(),
                    Longitude = rec.GetPositionLong(),
                    Lattitude = rec.GetPositionLat(),
                    Power = rec.GetPower(),
                    RecordDateTime = recordDateTime,
                };

                if (message.Speed.HasValue && message.Speed.Value > maxSpeed)
                {
                    maxSpeed = message.Speed.Value;
                }

                if (message.Power.HasValue && message.Power.Value > maxPower)
                {
                    maxPower = message.Power.Value;
                }

                if (message.Distance.HasValue)
                {
                    if (!firstDistance.HasValue)
                    {
                        firstDistance = message.Distance.Value;
                    }

                    lastRecordDitance = message.Distance.Value;
                }

                #region GradeCalculation
                double? previousDistance = recordMesgForGrade.GetDistance();
                double? previousAltitude = recordMesgForGrade.GetEnhancedAltitude();
                
                if (previousAltitude.HasValue == false 
                    || previousDistance.HasValue == false)
                {
                    // altitude and distance are required for grade calculation
                    recordMesgForGrade = rec;
                }
                else if (previousAltitude.HasValue && previousDistance.HasValue
                    && message.Altitude.HasValue && message.Distance.HasValue)
                {
                    double? run = message.Distance - previousDistance;
                    // ensure calculate adequate incline
                    if (run >= ThresholdDistanceMeters)
                    {
                        double? rise = message.Altitude - previousAltitude;

                        // if the climbed/descended altitude is greater than the passed distance it means that it's vertical 
                        // which is not possible that's why take the next message until "run" is greater
                        if (rise.HasValue && Math.Abs(rise.Value) < run)
                        {
                            double? grade = 100.0f * (float?)(rise / run);

                            recordMesgForGrade = rec;

                            var date = rec.GetTimestamp().GetDateTime();
                            gradesResults[date] = grade;
                        }
                    }
                }
                #endregion

                messages.Add(message);
            }

            if (isWholeRange)
            {
                fitInitializer.Distance = fitMessages.SessionMesgs[0].GetTotalDistance() ??
                    (lastRecordDitance - (firstDistance ?? 0)); // for backup if distance is missing from session
                fitInitializer.MaxSpeed = fitMessages.SessionMesgs[0].GetEnhancedMaxSpeed() ?? maxSpeed;
                fitInitializer.MaxPower = fitMessages.SessionMesgs[0].GetMaxPower() ?? maxPower;
            }
            else
            {
                fitInitializer.Distance = lastRecordDitance - (firstDistance ?? 0);
                fitInitializer.MaxSpeed = maxSpeed;
                fitInitializer.MaxPower = maxPower;
            }

            fitInitializer.Records = messages;
            fitInitializer.Grades = gradesResults;
            fitInitializer.FirstDistance = firstDistance ?? 0;
            fitInitializer.ChartDataStats = chartDataStats;

            fitInitializer.ActivePeriods = InitializeActivePeriods(fitMessages.EventMesgs);
            fitInitializer.PausePeriods = InitializePausePeriods(fitMessages.EventMesgs);

            return fitInitializer;
        }

        private static List<(System.DateTime start, System.DateTime end)> InitializeActivePeriods(
            IReadOnlyCollection<EventMesg> eventMesgs)
        {
            List<(System.DateTime start, System.DateTime end)> activePeriods = [];

            List<EventMesg> eventMessages = eventMesgs
                //.Where(e => e.GetEvent() == Event.Timer)
                .OrderBy(e => e.GetTimestamp().GetDateTime()).ToList();

            int lastIndexStart = -1;
            for (int i = 0; i < eventMessages.Count; i++)
            {
                EventMesg eventMesg = eventMessages[i];
                EventType? eventType = eventMesg.GetEventType();
                if (eventType.HasValue)
                {
                    switch (eventType.Value)
                    {
                        case EventType.Start:
                            lastIndexStart = i;
                            break;
                        case EventType.Stop:
                        case EventType.StopAll:

                            if (lastIndexStart != -1)
                            {
                                System.DateTime periodStartDate = eventMessages[lastIndexStart].GetTimestamp().GetDateTime();
                                System.DateTime periodStopDate = eventMesg.GetTimestamp().GetDateTime();
                                activePeriods.Add((periodStartDate, periodStopDate));
                            }

                            lastIndexStart = -1;
                            break;
                        default:
                            break;
                    }
                }
            }

            return activePeriods;
        }

        private static List<(System.DateTime start, System.DateTime end)> InitializePausePeriods(
            IReadOnlyCollection<EventMesg> eventMesgs)
        {
            List<(System.DateTime start, System.DateTime end)> pausePeriods = [];

            List<EventMesg> eventMessages = eventMesgs
                //.Where(e => e.GetEvent() == Event.Timer)
                .OrderBy(e => e.GetTimestamp().GetDateTime()).ToList();

            for (int i = 0; i < eventMessages.Count; i++)
            {
                EventMesg eventMessage = eventMessages[i];
                EventType? eventType = eventMessage.GetEventType();

                if (eventType.HasValue
                    && (eventType.Value == EventType.Stop || eventType.Value == EventType.StopAll))
                {
                    for (int y = ++i; y < eventMessages.Count; y++, i++)
                    {
                        EventMesg nextEventMessage = eventMessages[y];
                        EventType? nextEventType = nextEventMessage.GetEventType();

                        if (nextEventType.HasValue && nextEventType == EventType.Start)
                        {
                            System.DateTime stopEventTime = eventMessage.GetTimestamp().GetDateTime().ToLocalTime();
                            System.DateTime startEventTime = nextEventMessage.GetTimestamp().GetDateTime().ToLocalTime();
                            pausePeriods.Add((stopEventTime, startEventTime));

                            // AjdustStartEndTimes(stopEventTime, startEventTime);

                            break;
                        }
                    }
                }
            }

            return pausePeriods;
        }

        public bool IsRecordInActiveTime(System.DateTime date)
        {
            return IsRecordInActiveTime(date, this.ActivePeriods);
        }

        public static bool IsRecordInActiveTime(
            System.DateTime date,
            IReadOnlyCollection<(System.DateTime start, System.DateTime end)> activePeriods)
        {
            bool isRecordInActivePeriod = activePeriods.Any(x => x.start <= date && date <= x.end);
            return isRecordInActivePeriod;
        }

        /// <summary>
        /// This can fix the logic when activity immediately went into paused state after start.
        /// </summary>
        private void AjdustStartEndTimes(System.DateTime date1, System.DateTime date2)
        {
            if (date1 < StartDate || date2 < StartDate)
            {
                StartDate = date1 < date2 ? date1 : date2;
            }

            if (EndDate < date1 || EndDate < date2)
            {
                EndDate = date1 > date2 ? date1 : date2;
            }
        }
    }
}

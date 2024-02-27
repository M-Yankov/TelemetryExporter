using Dynastream.Fit;

using SkiaSharp;
using TelemetryExporter.Core.Models;
using TelemetryExporter.Core.Utilities;
using TelemetryExporter.Core.Widgets.Trace;
using TelemetryExporter.Core.Widgets.Distance;
using TelemetryExporter.Core.Widgets.Speed;
using TelemetryExporter.Core.Widgets.Pace;
using TelemetryExporter.Core.Widgets.Elevation;

namespace TelemetryExporter.Core
{
    /// <summary>
    /// This code is just for help in some case. Nothing production at all.
    /// </summary>
    internal class CodeStuff
    {
        internal static void Stuff()
        {
            FitListener fitListener = new();
            #region Calculating Active time
            TimeSpan elaspedTimeAll = TimeSpan.Zero;
            System.DateTime? lastDate = null;
            foreach (EventMesg eventMesg in fitListener.FitMessages.EventMesgs)
            {
                EventType? eventType = eventMesg.GetEventType();

                var localDateTime = eventMesg.GetTimestamp().GetDateTime().ToLocalTime();
                if (lastDate is null && eventType == EventType.Start)
                {
                    lastDate = localDateTime;
                }

                if ((eventType == EventType.Stop || eventType == EventType.StopAll) && lastDate.HasValue)
                {
                    TimeSpan newTime = localDateTime.Subtract(lastDate.Value);
                    elaspedTimeAll = elaspedTimeAll.Add(newTime);

                    lastDate = null;
                }

                Event? @event = eventMesg.GetEvent();
                System.Console.Write(localDateTime.ToString("hh:mm:ss  "));
                System.Console.WriteLine($"{eventType,12},{@event,15}");
            }

            // This is the correct time according to garmin
            System.Console.WriteLine(elaspedTimeAll.ToString("hh\\:mm\\:ss"));
            #endregion

            #region Show telemetry data (First try)
            TimeSpan elapsedWhenHaveSpeed = TimeSpan.Zero;
            System.DateTime lastActiveDate = fitListener.FitMessages.RecordMesgs[0].GetTimestamp().GetDateTime();

            foreach (RecordMesg recordMesg in fitListener.FitMessages.RecordMesgs)
            {
                byte hr = recordMesg.GetHeartRate() ?? 0;
                float speed = recordMesg.GetEnhancedSpeed() ?? 0;

                // var field = recordMesg.GetField(RecordMesg.FieldDefNum. 73);
                // var distField = recordMesg.GetField(RecordMesg.FieldDefNum.Distance);

                var distance = recordMesg.GetDistance() ?? 0;

                // random default value for pace if speed is not present
                double? pace = null;
                var elapsed =
                    recordMesg.GetTimestamp().GetDateTime().Subtract(fitListener.FitMessages.RecordMesgs[0].GetTimestamp().GetDateTime());
                if (speed > 0)
                {
                    pace = 60 / (speed * 3.6);

                    TimeSpan activeRange = recordMesg.GetTimestamp().GetDateTime().Subtract(lastActiveDate);
                    elapsedWhenHaveSpeed = elapsedWhenHaveSpeed.Add(activeRange);
                }

                lastActiveDate = recordMesg.GetTimestamp().GetDateTime();

                var dat = recordMesg.GetTimestamp().GetDateTime().ToLocalTime().ToString("HH:mm:ss:f");

                System.Console.WriteLine($"time:{dat} HR: {hr,3}, Speed {pace,6:F2} min/km, Distance {distance / 1000,5:F2} km, {elapsed:hh\\:mm\\:ss}");
            }

            System.Console.WriteLine($"Elapsed Active time: {elapsedWhenHaveSpeed:hh\\:mm\\:ss}");

            System.Console.WriteLine("Done!");
            #endregion
        }

        static decimal ConvertToDegreesFromSemicircles(int value)
        {
            decimal result = (long)int.MaxValue + 1;
            decimal res = 180M / result;

            decimal res2 = result / 180M;

            return value * res;
        }

        #region generageImage
        async Task GenerateExampleImagtes()
        {
            string testFilePath = @"C:\Users\<username>\Desktop\13900511806_ACTIVITY.fit";
            FitMessages messages = new FitDecoder(testFilePath).FitMessages;

            SessionData sessionData = new()
            {
                MaxSpeed = 70,
                TotalDistance = 300000,
                CountOfRecords = messages.RecordMesgs.Count,
            };


            var frame = new FrameData()
            {
                FileName = string.Empty,
                Altitude = 800,
                Distance = 113000,
                Latitude = messages.RecordMesgs[5233].GetPositionLat().Value,
                IndexOfCurrentRecord = 1233,
                Longitude = messages.RecordMesgs[5233].GetPositionLong().Value,
                Speed = 3
            };

            SKData data = new SpeedWidget().GenerateImage(sessionData, frame);
            SKData data2 = new ElevationWidget(messages.RecordMesgs).GenerateImage(sessionData, frame);
            SKData data3 = new DistanceWidget().GenerateImage(sessionData, frame);
            SKData data4 = new TraceWidget(messages.RecordMesgs).GenerateImage(sessionData, frame);
            SKData data5 = new PaceWidget().GenerateImage(sessionData, frame);

            await SaveImageAsync("speed.png", data);
            await SaveImageAsync("elevation.png", data2);
            await SaveImageAsync("distance.png", data3);
            await SaveImageAsync("trace.png", data4);
            await SaveImageAsync("pace.png", data5);
        }

        static async Task SaveImageAsync(string name, SKData data)
        {
            using FileStream stream = System.IO.File.OpenWrite(name);
            data.SaveTo(stream);
            using Stream s = data.AsStream();
            await s.CopyToAsync(stream);
            await stream.FlushAsync();
        }
        #endregion
    }
}

// See https://aka.ms/new-console-template for more information
using System.Collections.ObjectModel;
using System.Drawing;
using System.Text;
using SkiaSharp;
using Dynastream.Fit;

using TelemetryExporter.Console.Models;
using System.IO;
using System.Globalization;
using System.Collections.Generic;

// Check this: /activity-service/activity/12092921949/details

// below that speed it's assumed as walking
// https://www.convert-me.com/en/convert/speed/?u=minperkm_1&v=30
const double SpeedCutoff = 0.5556;

const int GpxPictureWidthPixels = 1000;
const int GpxPictureOffsetPixels = 50;

GPSContainer.Instance.PictureOffsetPixels = GpxPictureOffsetPixels;
GPSContainer.Instance.PictureWidthPixels = GpxPictureWidthPixels;

const int ElevationPictureWidthPixels = 700;
const int ElevationPictureHeightPixels = 250;

ElevationContainer.Instance.PictureWidthPixels = ElevationPictureWidthPixels;
ElevationContainer.Instance.PictureHeightPixels = ElevationPictureHeightPixels;
ElevationContainer.Instance.OffsetPixelsY = ElevationContainer.Instance.PictureHeightPixels * .20f;

const int FPS = 2;

FileInfo fitFile = new(@"C:\Users\M.Yankov\Desktop\12027002954_Lidl_Run_ACTIVITY.fit");
Decode decode = new();
FitListener fitListener = new();
decode.MesgEvent += fitListener.OnMesg;
using FileStream fitStream = fitFile.Open(FileMode.Open, FileAccess.Read, FileShare.Read);
Console.WriteLine("Decoding...");
decode.Read(fitStream);

ProcessMethod(fitListener.FitMessages);
return 0;

#region Calculatuing Active time
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
    Console.Write(localDateTime.ToString("hh:mm:ss  "));
    Console.WriteLine($"{eventType,12},{@event,15}");
}

// This is the correct time according to garmin
Console.WriteLine(elaspedTimeAll.ToString("hh\\:mm\\:ss"));
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

    // radnom default value for pace if speed is not present
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

    Console.WriteLine($"time:{dat} HR: {hr,3}, Speed {pace,6:F2} min/km, Distance {distance / 1000,5:F2} km, {elapsed:hh\\:mm\\:ss}");
}

Console.WriteLine($"Elapsed Active time: {elapsedWhenHaveSpeed:hh\\:mm\\:ss}");

Console.WriteLine("Done!");
#endregion

static void ProcessMethod(FitMessages fitMessages)
{
    List<(System.DateTime start, System.DateTime end)> activePeriods = new();

    List<EventMesg> eventMessages = fitMessages.EventMesgs.OrderBy(e => e.GetTimestamp().GetDateTime()).ToList();

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

    if (!fitMessages.RecordMesgs.Any())
    {
        return;
    }

    // if this is always = true (not runtime calculated) the code could generate the transition (fake) frames.
    // my options is that it's not necessary since the activity is in pause mode. The used can be one place not moving, different heart beat
    // but on the other hand the fake frames are something average moving (similar to strava flyBy mode when some one has paused an activity, but was resume far later)
    bool isActiveTime = true; 

    List<RecordMesg> orderedRecordMessages = fitMessages.RecordMesgs.OrderBy(x => x.GetTimestamp().GetDateTime()).ToList();

    Queue<RecordMesg> queue = new(orderedRecordMessages);
    System.DateTime startDate = fitMessages.RecordMesgs[0].GetTimestamp().GetDateTime();
    System.DateTime endDate = fitMessages.RecordMesgs[^1].GetTimestamp().GetDateTime();

    System.DateTime currentTimeFrame = startDate;

    TimeSpan activeTimeDuration = TimeSpan.Zero;

    RecordMesg currentRecord = queue.Dequeue(); // default (nullable);

    StringBuilder textResult = new();

    Point? lastKnownGpsLocation = null;
    int frame = default;

    SKPath tracePath = BuildTracePath(fitMessages.RecordMesgs);
    SKPath elevationPath = BuildElevationPath(fitMessages.RecordMesgs);
    
    do
    {
        double? speed = null;
        double? distance = null;
        double? altitude = null;
        int? lattitude = null;
        int? longitude = null;

        isActiveTime = IsRecordInActiveTime(currentTimeFrame, activePeriods);

        if (queue.TryPeek(out RecordMesg? nextRcordMesg))
        {
            System.DateTime recordDate = nextRcordMesg.GetTimestamp().GetDateTime();
            System.DateTime? currentRecordDate = currentRecord.GetTimestamp().GetDateTime();

            if (currentRecordDate < currentTimeFrame && currentTimeFrame < recordDate /*&& currentRecord != null*/ && isActiveTime)
            {
                CalculateModel speedMetrics = new(
                    currentRecordDate.Value,
                    recordDate,
                    currentRecord.GetEnhancedSpeed(),
                    nextRcordMesg.GetEnhancedSpeed(),
                    currentTimeFrame);

                CalculateModel distanceMetrics = new(
                    currentRecordDate.Value,
                    recordDate,
                    currentRecord.GetDistance(),
                    nextRcordMesg.GetDistance(),
                    currentTimeFrame);

                CalculateModel altitudeMetrics = new(
                    currentRecordDate.Value,
                    recordDate,
                    currentRecord.GetEnhancedAltitude(),
                    nextRcordMesg.GetEnhancedAltitude(),
                    currentTimeFrame);

                CalculateModel lattitudeMetrics = new(
                    currentRecordDate.Value,
                    recordDate,
                    currentRecord.GetPositionLat(),
                    nextRcordMesg.GetPositionLat(),
                    currentTimeFrame);

                CalculateModel longitudeMetrics = new(
                    currentRecordDate.Value,
                    recordDate,
                    currentRecord.GetPositionLong(),
                    nextRcordMesg.GetPositionLong(),
                    currentTimeFrame);

                speed = GetValueBetweenDates(speedMetrics);
                distance = GetValueBetweenDates(distanceMetrics);
                altitude = GetValueBetweenDates(altitudeMetrics);

                lattitude = (int?)GetValueBetweenDates(lattitudeMetrics);
                longitude = (int?)GetValueBetweenDates(longitudeMetrics);
            }

            if ((currentRecordDate.HasValue && recordDate > currentRecordDate && currentTimeFrame >= recordDate)
                /*|| currentRecord == null*/)
            {
                if (isActiveTime)
                {
                    currentRecord = nextRcordMesg;
                }

                queue.Dequeue();
            }
        }


        if (isActiveTime)
        {
            altitude ??= currentRecord?.GetEnhancedAltitude();
            speed ??= currentRecord?.GetEnhancedSpeed();
            distance ??= currentRecord?.GetDistance();

            longitude ??= currentRecord?.GetPositionLong();
            lattitude ??= currentRecord?.GetPositionLat();

            if (lattitude.HasValue && longitude.HasValue)
            {
                lastKnownGpsLocation = new(longitude.Value, lattitude.Value);
            }
        }

        if (speed < SpeedCutoff)
        {
            speed = 0;
        }

        double pace = speed > 0 ? 60 / (speed.Value * 3.6) : default;

        // it is to take 0.86214 from 7.86214
        double paceHundreds = pace - (int)pace;
        double paceSeconds = (paceHundreds * 60);

        TimeSpan duration = (currentTimeFrame - startDate);

        float? hr = currentRecord?.GetHeartRate(); // beats per minute  
        sbyte? t = currentRecord?.GetTemperature();

        // instead of directly generate images I can made model with ready points for drawing calculating - it could take more memory
        textResult.AppendLine($"{duration,16} - speed: {(int)pace,3}:{(int)paceSeconds:D2}, {activeTimeDuration,16}, Dist: {distance / 1000,4:F2}km, {altitude,4:F1}↑ ♥{hr,3}, {t,2}℃");

        string frameFileName = $"frame_{++frame}.png";
        if (lastKnownGpsLocation.HasValue)
        {
            SKPoint imageCoords = GPSContainer.Instance.CalculateImageCoordinates(lastKnownGpsLocation.Value.X, lastKnownGpsLocation.Value.Y);
            imageCoords.AddOffset(GpxPictureOffsetPixels);

            SaveTraceImage(tracePath, imageCoords, frameFileName);
        }
        
        SavePaceImage(pace, frameFileName);

        // This needs to be checked if the field doesn't exist or if the field is null;
        float? totalDistance = fitMessages.SessionMesgs[0].GetTotalDistance();
        SaveDistanceImage(distance, totalDistance.Value, frameFileName);

        SKPoint elevationPoint = SKPoint.Empty;
        if (altitude.HasValue)
        {
            // float? seconds = fitMessages.SessionMesgs[0].GetTotalTimerTime();

            int indexOfRecord = orderedRecordMessages.IndexOf(currentRecord) + 1;

            elevationPoint = ElevationContainer.Instance.CalculateImageCoordinates((float)altitude.Value, indexOfRecord, orderedRecordMessages.Count);
        }

        SaveElevationImage(elevationPath, altitude, elevationPoint, frameFileName);

        // this will result 2 fps https://fpstoms.com/
        int millsecondsStep = 1000 / FPS;
        currentTimeFrame = currentTimeFrame.AddMilliseconds(millsecondsStep);

        if (isActiveTime)
        {
            activeTimeDuration = activeTimeDuration.Add(TimeSpan.FromMilliseconds(millsecondsStep));
        }

    } while (currentTimeFrame <= endDate);

    using FileStream fileStream = new("results.txt", FileMode.OpenOrCreate);
    using StreamWriter w = new(fileStream);
    w.WriteLine(textResult.ToString());

    /// Do I need this at all ?
    static decimal ConvertToDegreesFromSemicircles(int value)
    {
        decimal result = (long)int.MaxValue + 1;
        decimal res = 180M / result;

        decimal res2 = result / 180M;

        return value * res;
    }

    static bool IsRecordInActiveTime(System.DateTime date, IReadOnlyCollection<(System.DateTime start, System.DateTime end)> activePeriods)
    {
        bool isRecordInActivePeriod = activePeriods.Any(x => x.start <= date && date <= x.end);
        return isRecordInActivePeriod;
    }
}

static double? GetValueBetweenDates(CalculateModel calculateModel)
{
    System.DateTime previousDate = calculateModel.PreviousDate;
    System.DateTime nextDate = calculateModel.NextDate;
    System.DateTime dateForCalculation = calculateModel.CalculateAt;

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

// Merge this method with method below:
static SKPath BuildElevationPath(ReadOnlyCollection<RecordMesg> dataMessages)
{
    List<float> altitudeValues = new();

    foreach (float? altitude in dataMessages.OrderBy(x => x.GetTimestamp().GetDateTime()).Select(x => x?.GetEnhancedAltitude()))
    {
        if (!altitude.HasValue)
        {
            continue;
        }

        if (altitude.Value < ElevationContainer.Instance.MinElevation)
        {
            ElevationContainer.Instance.MinElevation = altitude.Value;
        }

        if (altitude.Value > ElevationContainer.Instance.MaxElevation)
        {
            ElevationContainer.Instance.MaxElevation = altitude.Value;
        }

        altitudeValues.Add(altitude.Value);
    }

    ElevationContainer.Instance.TotalElevation = Math.Abs(ElevationContainer.Instance.MaxElevation - ElevationContainer.Instance.MinElevation);

    SKPath skPath = new();

    for (int i = 0; i < altitudeValues.Count; i++)
    {
        float currentAltitude = altitudeValues[i];

        SKPoint point = ElevationContainer.Instance.CalculateImageCoordinates(altitudeValues[i], i + 1, altitudeValues.Count);
        //float calculatedAltitude = currentAltitude - ElevationContainer.Instance.MinElevation;
        //float calculatedPercentage = calculatedAltitude / ElevationContainer.Instance.TotalElevation;

        //float y = ElevationContainer.Instance.PictureHeightPixels - ((ElevationContainer.Instance.PictureHeightPixels - ElevationContainer.Instance.OffsetPixelsY) * calculatedPercentage);

        //float xPerentage = (i + 1) / (float)altitudeValues.Count;
        //float x = ElevationContainer.Instance.PictureWidthPixels * xPerentage;

        if (i == 0)
        {
            skPath.MoveTo(point);
        }
        else
        {
            skPath.LineTo(point);
        }
    }

    return skPath;
}

static SKPath BuildTracePath(ReadOnlyCollection<RecordMesg> gpsDataMessages)
{
    foreach (RecordMesg? gpsMessage in gpsDataMessages.OrderBy(x => x.GetTimestamp().GetDateTime()))
    {
        int? lattitude = gpsMessage.GetPositionLat(); // y  y=0 equator      south ↓ negative  | positive ↑ north  max ±90
        int? longitute = gpsMessage.GetPositionLong(); // x  x=0 Prime Meridian (London)  west <- negative  |  positive -> east ±180

        if (lattitude.HasValue && longitute.HasValue)
        {
            Point currentPoint = new(longitute.Value, lattitude.Value);
            GPSContainer.Instance.Coordinates.Add(currentPoint);

            if (lattitude.Value > GPSContainer.Instance.TheMostTopPoint.Y)
            {
                GPSContainer.Instance.TheMostTopPoint = currentPoint;
            }

            if (lattitude.Value < GPSContainer.Instance.TheMostBottomPoint.Y)
            {
                GPSContainer.Instance.TheMostBottomPoint = currentPoint;
            }

            if (longitute.Value < GPSContainer.Instance.TheMostLeftPoint.X)
            {
                GPSContainer.Instance.TheMostLeftPoint = currentPoint;
            }

            if (longitute.Value > GPSContainer.Instance.TheMostRightPoint.X)
            {
                GPSContainer.Instance.TheMostRightPoint = currentPoint;
            }
        }
    }

    // make sure the canvas is blank
    // canvas.Clear(SKColors.White);

    SKPath path = new();
    if (!GPSContainer.Instance.Coordinates.Any())
    {
        return path;
    }

    for (int i = 0; i < GPSContainer.Instance.Coordinates.Count; i++)
    {
        int latitude = GPSContainer.Instance.Coordinates[i].Y; // y
        int longitude = GPSContainer.Instance.Coordinates[i].X; // x
        
        SKPoint p0 = GPSContainer.Instance.CalculateImageCoordinates(longitude, latitude);
        p0.AddOffset(GpxPictureOffsetPixels);
        // gpsContainer.ImageCoordinates.Add(p0);
        if (i == 0)
        {
            path.MoveTo(p0);
        }
        else
        {
            path.LineTo(p0);
        }
    }

    return path;
}

static void SaveElevationImage(SKPath path, double? altitude, SKPoint point, string fileName)
{
    SKImageInfo info = new(ElevationPictureWidthPixels, ElevationPictureHeightPixels, SKImageInfo.PlatformColorType, SKAlphaType.Unpremul);
    using SKPaint blackPaint = new()
    {
        Color = SKColors.Black,
        IsAntialias = true,
        Style = SKPaintStyle.Stroke,
        StrokeWidth = 2
    };

    using SKPaint trasparentBlack = new()
    {
        Color = new SKColor(0, 0, 0, 170),
        IsAntialias = true,
        Style = SKPaintStyle.Fill,
    };

    using SKPaint textPaint = new()
    {
        Color = SKColors.White,
        IsAntialias = true,
        Typeface = SKTypeface.FromFamilyName("Consolas"),
        TextSize = 35,
    };

    using SKPaint redPaint = new()
    {
        Color = SKColors.Red,
        IsAntialias = true,
        Style = SKPaintStyle.Fill,
        StrokeWidth = 2
    };

    using SKPaint transparentPaint = new()
    {
        Color = new SKColor(0, 0, 0, 100),
        IsAntialias = true,
        Style = SKPaintStyle.Fill
    };

    string folderName = Path.Combine("Telemetry", "Elevation");
    if (!Directory.Exists(folderName))
    {
        Directory.CreateDirectory(folderName);
    }

    using SKSurface surface = SKSurface.Create(info);
    SKCanvas canvas = surface.Canvas;
    canvas.DrawPaint(transparentPaint);

    SKPath fillPath = new(path);
    fillPath.LineTo(ElevationPictureWidthPixels, ElevationPictureHeightPixels);
    fillPath.LineTo(0, ElevationPictureHeightPixels);

    canvas.DrawPath(fillPath, trasparentBlack);
    canvas.DrawPath(path, blackPaint);

    canvas.DrawText("ELEVATION", new SKPoint(25, 35), textPaint);
    using SKPaint linePaint = textPaint.Clone();
    linePaint.StrokeWidth = 10;

    canvas.DrawLine(0, 0, ElevationPictureWidthPixels, 0, linePaint);

    if (!point.IsEmpty)
    {
        canvas.DrawCircle(point, radius: 5, redPaint);

        float reachedToEndPercentage = point.X / ElevationPictureWidthPixels;

        int xOffset = 10;

        using SKPaint elvationPaint = textPaint.Clone();
        if (reachedToEndPercentage > .80f)
        {
            xOffset *= -1;
            elvationPaint.TextAlign = SKTextAlign.Right;
        }

        canvas.DrawText($"{altitude:F1} m", point.X + xOffset, point.Y - 10, elvationPaint);
    }

    using SKImage image = surface.Snapshot();
    using SKData data = image.Encode(SKEncodedImageFormat.Png, 100);

    using FileStream stream = System.IO.File.OpenWrite(Path.Combine(folderName, fileName));
    data.SaveTo(stream);
}

static void SaveTraceImage(SKPath path, SKPoint circleCoords, string fileName) 
{
    SKImageInfo info = new(GpxPictureWidthPixels, GpxPictureWidthPixels, SKImageInfo.PlatformColorType, SKAlphaType.Unpremul);
    using SKPaint blackPaint = new()
    {
        Color = SKColors.Black,
        IsAntialias = true,
        Style = SKPaintStyle.Stroke,
        StrokeWidth = 2
    };

    using SKPaint redPaint = new()
    {
        Color = SKColors.Red,
        IsAntialias = true,
        Style = SKPaintStyle.Fill,
        StrokeWidth = 2
    };

    string folderName = Path.Combine("Telemetry", "Trace");
    if (!Directory.Exists(folderName))
    {
        Directory.CreateDirectory(folderName);
    }

    using SKSurface surface = SKSurface.Create(info);

    SKCanvas canvas = surface.Canvas;
    canvas.DrawPath(path, blackPaint);

    const int CircleWidth = 5;
    canvas.DrawCircle(circleCoords, CircleWidth, redPaint);

    using SKImage image = surface.Snapshot();
    using SKData data = image.Encode(SKEncodedImageFormat.Png, 100);

    using FileStream stream = System.IO.File.OpenWrite(Path.Combine(folderName, fileName));
    data.SaveTo(stream);
}
static void SaveDistanceImage(double? currentDistance, double totalDistance, string fileName)
{
    string folderName = Path.Combine("Telemetry", "Distance");
    if (!Directory.Exists(folderName))
    {
        Directory.CreateDirectory(folderName);
    }

    const int DistanceImageWidth = 700;
    const int DistanceImageHeight = 100;

    SKImageInfo info = new(DistanceImageWidth, DistanceImageHeight, SKImageInfo.PlatformColorType, SKAlphaType.Unpremul);
    using SKPaint transparentDistancePaint = new()
    {
        Color = new SKColor(0, 0, 0, 170),
        IsAntialias = true,
        Style = SKPaintStyle.Fill
    };

    using SKPaint trasparentBlack = new()
    {
        Color = new SKColor(0, 0, 0, 100),
        IsAntialias = true,
        Style = SKPaintStyle.Fill,
    };

    using SKPaint textDistancePaint = new()
    {
        Color = SKColors.White,
        IsAntialias = true,
        Style = SKPaintStyle.Fill,
        Typeface = SKTypeface.FromFamilyName("Consolas"),
        TextSize = 35,
    };

    using SKPaint textDistanceNumbersPaint = textDistancePaint.Clone();
    textDistanceNumbersPaint.TextSize += (textDistanceNumbersPaint.TextSize * .15f);

    using SKSurface surface = SKSurface.Create(info);

    SKCanvas canvas = surface.Canvas;
    canvas.DrawPaint(trasparentBlack);

    string distanceAsText;
    if (currentDistance.HasValue)
    {
        // https://www.calculatorsoup.com/calculators/math/percentage.php
        double currentDistancePercentage = currentDistance.Value / totalDistance;
        float imagePixelsDistanceX = DistanceImageWidth * (float)currentDistancePercentage;
        canvas.DrawRect(0, 0, imagePixelsDistanceX, DistanceImageHeight, transparentDistancePaint);
        distanceAsText = $"{currentDistance / 1000f:F3} KM";
    }
    else
    {
        distanceAsText = $"-- KM";
    }

    // the Points should be percentage, not hardcoded
    canvas.DrawText("DISTANCE", new SKPoint(25, 35), textDistancePaint);
    canvas.DrawText(distanceAsText, new SKPoint(25, 75), textDistanceNumbersPaint);

    using SKImage image = surface.Snapshot();
    using SKData data = image.Encode(SKEncodedImageFormat.Png, 100);

    using FileStream stream = System.IO.File.OpenWrite(Path.Combine(folderName, fileName));
    data.SaveTo(stream);
}

static void SavePaceImage(double paceValue, string fileName)
{
    const int PaceImageWidth = 400;
    const int PaceImageHeight = 100;

    float percentageOffsetWidth = PaceImageWidth * .05f;

    string folderName = Path.Combine("Telemetry", "Pace");
    if (!Directory.Exists(folderName))
    {
        Directory.CreateDirectory(folderName);
    }

    SKImageInfo info = new(PaceImageWidth, PaceImageHeight, SKImageInfo.PlatformColorType, SKAlphaType.Unpremul);
    using SKPaint blackPaint = new()
    {
        Color = SKColors.Black,
        IsAntialias = true,
        Style = SKPaintStyle.Stroke,
        StrokeWidth = 2
    };

    using SKSurface surface = SKSurface.Create(info);

    SKCanvas canvas = surface.Canvas;

    //  canvas.DrawPaint(blackPaint);
    SKPath pathRegion = new ();
    SKPoint topLeftDrawArea = new(percentageOffsetWidth, 0);
    SKPoint topRightDrawArea = new(PaceImageWidth, 0);
    SKPoint bottomLeftDrawArea = new(0, PaceImageHeight);
    SKPoint bottomRightDrawArea = new(PaceImageWidth - percentageOffsetWidth, PaceImageHeight);

    pathRegion.MoveTo(topLeftDrawArea);
    pathRegion.LineTo(topRightDrawArea);
    pathRegion.LineTo(bottomRightDrawArea);
    pathRegion.LineTo(bottomLeftDrawArea);
    pathRegion.LineTo(topLeftDrawArea);

    SKRegion region = new (pathRegion);

    using SKPaint trasparentBlack = new()
    {
        Color = new SKColor(0, 0, 0, 100),
        IsAntialias = true,
        Style = SKPaintStyle.Fill,
        // StrokeWidth = 2
    };

    canvas.DrawRegion(region, trasparentBlack);

    const int FontSize = 60;
    using SKPaint textColor = new()
    {
        Color = SKColors.White,
        IsAntialias = true,
        Style = SKPaintStyle.Fill,
        TextAlign = SKTextAlign.Right,
        Typeface = SKTypeface.FromFamilyName("Consolas", SKFontStyle.Bold),
        TextSize = FontSize,
    };

    const float MagicNumberAlignFontY = FontSize * 0.25f;
    float textPointY = (PaceImageHeight / 2) + MagicNumberAlignFontY;
    const string ColonSymbol = ":";

    NumberFormatInfo nfi = new()
    {
        NumberDecimalSeparator = ColonSymbol
    };

    string text;
    if (paceValue > 0)
    {
        text = paceValue.ToString("F2", nfi).PadLeft(5, ' ');
    }
    else
    {
        text = "--";
    }

    text += "/KM";

    SKPoint textCoordinates = new(bottomRightDrawArea.X - 10, textPointY);

    using SKPaint linePaint = blackPaint.Clone();
    linePaint.StrokeWidth = 10;

    canvas.DrawText(text, textCoordinates, textColor);
    canvas.DrawLine(bottomLeftDrawArea, bottomRightDrawArea, linePaint);

    using SKImage image = surface.Snapshot();
    using SKData data = image.Encode(SKEncodedImageFormat.Png, 100);

    using FileStream stream = System.IO.File.OpenWrite(Path.Combine(folderName, fileName));
    data.SaveTo(stream);
}

namespace TelemetryExporter.Console.Models
{
    public static class PointExtensions
    {
        public static decimal Longitude(this Point point) => ConvertFromSemiCircles(point.X);

        public static decimal Latitude(this Point point) => ConvertFromSemiCircles(point.Y);

        public static void AddOffset(this ref SKPoint skPoint, int offset) => skPoint.AddOffset(offset, offset);

        public static void AddOffset(this ref SKPoint skPoint, int xOffset, int yOffset)
        {
            skPoint.X += xOffset;
            skPoint.Y += yOffset;
        }

        private static decimal ConvertFromSemiCircles(int value)
        {
            decimal result = (long)int.MaxValue + 1;
            decimal res = 180M / result;

            return value * res;
        }
    }

    public class GPSContainer
    {
        public Point TheMostLeftPoint { get; set; } = new(int.MaxValue, 0);

        public Point TheMostBottomPoint { get; set; } = new(0, int.MaxValue);

        public Point TheMostRightPoint { get; set; } = new(int.MinValue, 0);

        public Point TheMostTopPoint { get; set; } = new(0, int.MinValue);

        public List<Point> Coordinates { get; set; } = new();

        public int PictureWidthPixels { get; set; } // 1000;

        public int PictureOffsetPixels { get; set;} // 50;

        public static readonly GPSContainer Instance = new();

        public SKPoint CalculateImageCoordinates(int longitude, int latitude)
        {
            // Calculate only X, because it's rectangular
            float drawAreaWidthX = PictureWidthPixels - (PictureOffsetPixels * 2);

            float resX = Math.Abs(GPSContainer.Instance.TheMostLeftPoint.X - GPSContainer.Instance.TheMostRightPoint.X);
            float resY = Math.Abs(GPSContainer.Instance.TheMostTopPoint.Y - GPSContainer.Instance.TheMostBottomPoint.Y);

            float percentageOfX = resX / drawAreaWidthX;
            float percentageOfY = resY / drawAreaWidthX;

            float y = Math.Abs(latitude - Instance.TheMostTopPoint.Y) / percentageOfY;
            float x = Math.Abs(longitude - Instance.TheMostLeftPoint.X) / percentageOfX;

            return new(x, y);
        }
    }

    public class ElevationContainer
    {
        public float MaxElevation { get; set; } = float.MinValue;

        public float MinElevation { get; set; } = float.MaxValue;

        public float TotalElevation { get; set; } = default;

        public int PictureWidthPixels { get; set; }

        public int PictureHeightPixels { get; set; }

        public float OffsetPixelsY { get; set; }

        public static readonly ElevationContainer Instance = new();

        public SKPoint CalculateImageCoordinates(float altitude, int currentPoint, float totalPoints)
        {
            float calculatedAltitude = altitude - MinElevation;
            float calculatedPercentage = calculatedAltitude / TotalElevation;

            float y = ElevationContainer.Instance.PictureHeightPixels - ((ElevationContainer.Instance.PictureHeightPixels - OffsetPixelsY) * calculatedPercentage);

            float xPerentage = currentPoint / totalPoints;
            float x = ElevationContainer.Instance.PictureWidthPixels * xPerentage;

            return new(x, y);
        }
    }

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

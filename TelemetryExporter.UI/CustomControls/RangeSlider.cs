using System.ComponentModel;
using System.Diagnostics;

using MAUI = Microsoft.Maui.Controls.Shapes;

namespace TelemetryExporter.UI.CustomControls;

// Make Range slider Generic and initialize it from code behind.
// For example RangeSlider<T> where T : Struct (or something comparable)
// currently not possible due to the specific logic with date that are not compatible with other types.
public class RangeSlider : ContentView, INotifyPropertyChanged
{
    private readonly MAUI.Path startPoint;
    private readonly MAUI.Path endPoint;

    private DateTime startValue;
    private DateTime endValue;

    private double accumolatedX = 0;
    private double accumolatedXEnd = 0;
    private double lastUsedWidth = 0;

    private double startPercentage = 0;
    private double endPercentage = 1;

    private readonly MAUI.Rectangle selectedRange;

    public event RangeSliderChangedEventHandler<DateTime>? OnSliderValuesChanged;

    public RangeSlider()
    {
        // code in constructor should be moved into XAML
        // https://learn.microsoft.com/en-us/dotnet/maui/xaml/runtime-load?view=net-maui-8.0
        PanGestureRecognizer panGesture = new();
        panGesture.PanUpdated += OnPanUpdated;
        PanGestureRecognizer endPanGesture = new();
        endPanGesture.PanUpdated += EndPointOnPanUpdated;

        startPoint = new MAUI.Path()
        {
            WidthRequest = 20,
            HeightRequest = 60,
            Fill = Colors.Black,
            Stroke = Colors.Black,
            StrokeThickness = 2,
            Data = (MAUI.Geometry?)new MAUI.PathGeometryConverter()
                .ConvertFromInvariantString("M0,0 L0,60 20,60 20,45 0,20 Z")
        };

        endPoint = new MAUI.Path()
        {
            WidthRequest = 20,
            HeightRequest = 60,
            Fill = Colors.Black,
            Stroke = Colors.Black,
            StrokeThickness = 2,
            Data = (MAUI.Geometry?)new MAUI.PathGeometryConverter()
                .ConvertFromInvariantString("M18,0 L18,60 0,60 0,45 18,20 18,0 Z")
        };

        selectedRange = new MAUI.Rectangle()
        {
            MinimumHeightRequest = 20,
            Fill = Colors.Blue,
        };

        MAUI.Rectangle selectedRangeBoundaries = new()
        {
            MinimumHeightRequest = selectedRange.MinimumHeightRequest,
            Stroke = Colors.LightBlue,
            StrokeThickness = 3
        };

        startPoint.GestureRecognizers.Add(panGesture);
        endPoint.GestureRecognizers.Add(endPanGesture);

        AbsoluteLayout.SetLayoutBounds(startPoint, new Rect(0, 0, startPoint.Width, startPoint.Height));
        AbsoluteLayout.SetLayoutBounds(endPoint, new Rect(0, 0, endPoint.Width, endPoint.Height));

        AbsoluteLayout.SetLayoutFlags(selectedRangeBoundaries, Microsoft.Maui.Layouts.AbsoluteLayoutFlags.WidthProportional);
        AbsoluteLayout.SetLayoutBounds(selectedRangeBoundaries, new Rect(0, 0, 1, selectedRangeBoundaries.MinimumHeightRequest));

        Content = new AbsoluteLayout
        {
            Children = {
                selectedRangeBoundaries, selectedRange, startPoint, endPoint
            }
        };
    }

    public DateTime MinValue { get; private set; }

    public DateTime MaxValue { get; private set; }

    public DateTime StartValue
    {
        get => startValue;
        private set
        {
            startValue = value;
            OnPropertyChanged(nameof(StartValue));
            RangeSliderChangedEventArgs<DateTime> eventArgs = new()
            {
                StartValuePercentage = startPercentage,
                EndValuePercentage = endPercentage,
                StartValue = startValue,
                EndValue = endValue,
            };

            OnSliderValuesChanged?.Invoke(this, eventArgs);
        }
    }

    public DateTime EndValue
    {
        get => endValue;
        private set
        {
            endValue = value;
            OnPropertyChanged(nameof(EndValue));
            RangeSliderChangedEventArgs<DateTime> eventArgs = new()
            {
                StartValuePercentage = startPercentage,
                EndValuePercentage = endPercentage,
                StartValue = startValue,
                EndValue = endValue,
            };

            OnSliderValuesChanged?.Invoke(this, eventArgs);
        }
    }

    public void InitializeMinMax(DateTime min, DateTime max)
    {
        if (max == min || min > max)
        {
            throw new ArgumentException($"{nameof(max)} should be greater than {nameof(min)}", nameof(min));
        }

        if (min == new DateTime() && max == new DateTime())
        {
            throw new InvalidOperationException("Min/Max ranges already initialized");
        }

        StartValue = MinValue = min;
        EndValue = MaxValue = max;
    }

     // Using this event to use Content.Width
    protected override void OnSizeAllocated(double width, double height)
    {
        base.OnSizeAllocated(width, height);

        if (this.Window == null)
        {
            return;
        }

        // initialize
        if (accumolatedXEnd == 0)
        {
            accumolatedXEnd = Content.Width - endPoint.Width;
        }

        if (lastUsedWidth != 0)
        {
            double percentage = accumolatedX / lastUsedWidth;

            // set new relative value according to new resized window
            accumolatedX = Content.Width * percentage;

            double percentageForEnd = accumolatedXEnd / lastUsedWidth;
            accumolatedXEnd = Content.Width * percentageForEnd;
        }

        double boundsX = Content.Width;
        lastUsedWidth = Content.Width;
        double value = Math.Clamp(accumolatedX, 0, boundsX - startPoint.Width);
        startPoint.TranslationX = value;

        double valueEnd = Math.Clamp(accumolatedXEnd, 0, boundsX - endPoint.Width);
        endPoint.TranslationX = valueEnd;

        AbsoluteLayout.SetLayoutBounds(selectedRange, new Rect(value, 0, endPoint.Width + valueEnd - accumolatedX, selectedRange.MinimumHeightRequest));
    }

    //protected override void LayoutChildren(double x, double y, double width, double height)
    //{
    //    base.LayoutChildren(x, y, width, height);
    //}

    // unfortunately https://github.com/dotnet/maui/issues/15576
    // could be fixed with: https://stackoverflow.com/questions/28472205/c-sharp-event-debounce
    public void OnPanUpdated(object? sender, PanUpdatedEventArgs e)
    {
        switch (e.StatusType)
        {
            case GestureStatus.Running:

                double value = Math.Clamp(accumolatedX + e.TotalX, 0, accumolatedXEnd);
                startPoint.TranslationX = value;
                // SetLayoutBounds has some strange behavior
                // AbsoluteLayout.SetLayoutBounds(startPoint, new Rect(value, 0, startPoint.WidthRequest, startPoint.HeightRequest));
                AbsoluteLayout.SetLayoutBounds(selectedRange, new Rect(value, 0, endPoint.Width + accumolatedXEnd - value, selectedRange.Height));

                startPercentage = value / Content.Width;
                StartValue = new DateTime((long)(MinValue.Ticks + ((MaxValue.Ticks - MinValue.Ticks) * startPercentage)));
                break;

            case GestureStatus.Completed:
                accumolatedX = startPoint.TranslationX;

                break;
            case GestureStatus.Canceled:
                break;
        }
    }

    public void EndPointOnPanUpdated(object? sender, PanUpdatedEventArgs e)
    {
        switch (e.StatusType)
        {
            case GestureStatus.Running:
                double boundsX = Content.Width;
                double value = Math.Clamp(accumolatedXEnd + e.TotalX, accumolatedX, boundsX - endPoint.Width);
                
                endPoint.TranslationX = value;

                AbsoluteLayout.SetLayoutBounds(selectedRange, new Rect(accumolatedX, 0, endPoint.Width + value - accumolatedX, selectedRange.Height));

                // need to add the end slider width because it's pointer is at the right side.
                endPercentage = (value + endPoint.Width) / Content.Width;
                EndValue = new DateTime((long)(MinValue.Ticks + ((MaxValue.Ticks - MinValue.Ticks) * endPercentage)));
                break;

            case GestureStatus.Completed:
                accumolatedXEnd = endPoint.TranslationX;

                break;
            case GestureStatus.Canceled:
                break;
        }
    }
}
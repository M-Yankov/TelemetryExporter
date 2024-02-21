using System.Diagnostics;

using Microsoft.Maui.Controls.Shapes;

namespace TelemetryExporter.UI.CustomControls;

public class RangeSlider : ContentView
{
    private readonly Rectangle startPoint;
    private readonly Rectangle endPoint;
   
    private double accumolatedX = 0;
    private double accumolatedXEnd = 0;
    private double lastUsedWidth = 0;

    private readonly Rectangle selectedRange;

    public RangeSlider()
	{
        PanGestureRecognizer panGesture = new ();
        panGesture.PanUpdated += OnPanUpdated;
        PanGestureRecognizer endPanGesture = new();
        endPanGesture.PanUpdated += EndPointOnPanUpdated;

        startPoint = new Rectangle()
        {
            WidthRequest = 110,
            HeightRequest = 60,
            Fill = Colors.Black,
        };

        endPoint = new Rectangle()
        {
            WidthRequest = 110,
            HeightRequest = 60,
            Fill = Colors.DarkRed,
        };

        selectedRange = new Rectangle()
        {
            MinimumHeightRequest = 20,
            Fill = Colors.Blue,
        };

        startPoint.GestureRecognizers.Add(panGesture);
        endPoint.GestureRecognizers.Add(endPanGesture);
        
        AbsoluteLayout.SetLayoutBounds(startPoint, new Rect(0, 0, startPoint.Width, startPoint.Height));
        AbsoluteLayout.SetLayoutBounds(endPoint, new Rect(0, 0, endPoint.Width, endPoint.Height));

        Rectangle leftBorder = new() { WidthRequest = 2, Fill = Colors.Black, HeightRequest = selectedRange.MinimumHeightRequest };
        Rectangle rightBorder = new() { WidthRequest = 2, Fill = Colors.Black, HeightRequest = selectedRange.MinimumHeightRequest };

        AbsoluteLayout.SetLayoutFlags(rightBorder, Microsoft.Maui.Layouts.AbsoluteLayoutFlags.XProportional);
        AbsoluteLayout.SetLayoutBounds(rightBorder, new Rect(1, 0, rightBorder.WidthRequest, selectedRange.MinimumHeightRequest));

        Content = new AbsoluteLayout
		{
			Children = {
                leftBorder, rightBorder, startPoint, endPoint, selectedRange,
            }
		};
	}

    // Using this event to use Content.Width
    protected override void OnSizeAllocated(double width, double height)
    {
        base.OnSizeAllocated(width, height);

        if (this.Window != null)
        {
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

                double boundsX = Content.Width;
                double value = Math.Clamp(accumolatedX + e.TotalX, 0, boundsX - startPoint.Width);
                startPoint.TranslationX = value; // SetLayoutBounds has some strange behavior
                // AbsoluteLayout.SetLayoutBounds(startPoint, new Rect(value, 0, startPoint.WidthRequest, startPoint.HeightRequest));
                AbsoluteLayout.SetLayoutBounds(selectedRange, new Rect(value, 0, endPoint.Width + accumolatedXEnd - value, selectedRange.Height));
                break;

            case GestureStatus.Completed:
                accumolatedX = startPoint.TranslationX;
                break;
            case GestureStatus.Canceled:
                Debug.WriteLine("Canceled");
                break;
        }
        Debug.WriteLine($"x:{e.TotalX} y:{e.TotalY}");
    }

    public void EndPointOnPanUpdated(object? sender, PanUpdatedEventArgs e)
    {
        switch (e.StatusType)
        {
            case GestureStatus.Running:
                double boundsX = Content.Width;
                double value = Math.Clamp(accumolatedXEnd + e.TotalX, 0, boundsX - endPoint.Width);
                endPoint.TranslationX = value;
                
                AbsoluteLayout.SetLayoutBounds(selectedRange, new Rect(accumolatedX, 0, endPoint.Width + value - accumolatedX, selectedRange.Height));

                break;

            case GestureStatus.Completed:
                accumolatedXEnd = endPoint.TranslationX;
                break;
            case GestureStatus.Canceled:
                Debug.WriteLine("Canceled");
                break;
        }
    }
}
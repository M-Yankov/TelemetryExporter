using TelemetryExporter.UI.CustomControls;

using UIKit;

#pragma warning disable IDE0130 // Namespace does not match folder structure
namespace TelemetryExporter.UI.Platforms.Behaviors;
#pragma warning restore IDE0130 // Namespace does not match folder structure

public partial class RangeSliderBehavior : PlatformBehavior<RangeSlider>
{
    protected override void OnAttachedTo(RangeSlider slider, UIView platformView)
    {
        base.OnAttachedTo(slider, platformView);
        RangeSliderEventsHelper.SubscribeEvents(slider);
    }

    protected override void OnDetachedFrom(RangeSlider slider, UIView platformView)
    {
        base.OnDetachedFrom(slider, platformView);
        RangeSliderEventsHelper.UnsubscribeEvents(slider);
    }
}

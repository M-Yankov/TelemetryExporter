using System.ComponentModel;

using TelemetryExporter.UI.CustomControls;
using TelemetryExporter.UI.Resources;
using TelemetryExporter.UI.ViewModels;

namespace TelemetryExporter.UI.Views;

public partial class SelectWidgets : ContentPage, IQueryAttributable
{
    public SelectWidgets()
    {
        InitializeComponent();
    }

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        SelectWidgetsViewModel model = new((Stream)query[TEConstants.QueryKeys.FitStreamKey]);
        BindingContext = model;

        elevationImage.SetBinding(Image.SourceProperty, new Binding(nameof(model.MyImage), source: model));

        rangeDatesActivity.OnSliderValuesChanged += RangeDatesActivity_OnSliderValuesChanged;
        rangeDatesActivity.InitializeMinMax(model.StartActivityDate, model.EndActivityDate);

        selectedEndTime.SetBinding(Label.TextProperty, new Binding(nameof(rangeDatesActivity.EndValue), source: rangeDatesActivity));
        selectedStartTime.SetBinding(Label.TextProperty, new Binding(nameof(rangeDatesActivity.StartValue), source: rangeDatesActivity));

        void onLabelTextChanged(object? sender, PropertyChangedEventArgs eventArgs)
        {
            if (eventArgs.PropertyName == Label.TextProperty.PropertyName
                && sender is Label senderLabel)
            {
                new Animation((v) => { senderLabel.BackgroundColor = Color.FromRgba(0, 220, 0, v); }, 1, 0)
                .Commit(senderLabel, $"{senderLabel.Id}", 16, 1000, Easing.BounceIn);
            }
        }

        selectedStartTime.PropertyChanged += onLabelTextChanged;
        selectedEndTime.PropertyChanged += onLabelTextChanged;
    }

    private void RangeDatesActivity_OnSliderValuesChanged(RangeSlider rangeSlider, RangeSliderChangedEventArgs<DateTime> e)
    {
        const string TimeFormat = @"hh\:mm\:ss\.fff";

        TimeSpan startTimeFromBeginning = e.StartValue - rangeSlider.MinValue;
        TimeSpan endFromBeginning = e.EndValue - rangeSlider.MinValue;

        selectedDuration.Text = (e.EndValue - e.StartValue).ToString(TimeFormat);
        if (BindingContext is SelectWidgetsViewModel model)
        {
            double rangeDistance = (e.EndValuePercentage - e.StartValuePercentage) * model.TotalDistance;
            selectedDistance.Text = $"{rangeDistance / 1000f:F3} KM";
        }

        startSinceBeginning.Text = startTimeFromBeginning.ToString(TimeFormat);
        endSinceBeginning.Text = endFromBeginning.ToString(TimeFormat);
    }
}
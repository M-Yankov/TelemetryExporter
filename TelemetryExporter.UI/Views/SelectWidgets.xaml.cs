using System.ComponentModel;

using TelemetryExporter.UI.CustomControls;
using TelemetryExporter.UI.Resources;
using TelemetryExporter.UI.ViewModels;

namespace TelemetryExporter.UI.Views;

public partial class SelectWidgets : ContentPage, IQueryAttributable
{
    private readonly List<int> selectWidgetIds;
    private CancellationTokenSource cancellationTokenForExport;

    public SelectWidgets()
    {
        selectWidgetIds = [];
        InitializeComponent();

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

        rangeDatesActivity.OnSliderValuesChanged += RangeDatesActivity_OnSliderValuesChanged;

        // XAML SelectedIndex not working :/
        selectedFps.SelectedIndex = 0;
        saveLocation.Text = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
    }

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        SelectWidgetsViewModel model = (SelectWidgetsViewModel)BindingContext;

        model.Initialize((Stream)query[TEConstants.QueryKeys.FitStreamKey]);

        elevationImage.SetBinding(Image.SourceProperty, new Binding(nameof(model.MyImage), source: model));
        rangeDatesActivity.InitializeMinMax(model.StartActivityDate, model.EndActivityDate);
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

    private void WidgetCheckBox_CheckedChanged(object? sender, WidgetCheckedChangedEventArgs e)
    {
        if (e.Value)
        {
            selectWidgetIds.Add(e.ElementValue);
        }
        else
        {
            selectWidgetIds.RemoveAll(x => e.ElementValue == x);
        }
    }

    private async void ExportBtn_Clicked(object sender, EventArgs e)
    {
        string message = "";
        if (selectWidgetIds?.Count == 0)
        {
            message = "Please select widgets!";
        }

        if (!Directory.Exists(saveLocation.Text))
        {
            message = "Directory not exist!";
        }

        if (!string.IsNullOrWhiteSpace(message))
        {
            await DisplayAlert("Error", message, "OK");
            return;
        }

        cancellationTokenForExport = new CancellationTokenSource();
        exportLoaderIndicator.IsRunning = true;
        exportBtn.IsEnabled = !exportLoaderIndicator.IsRunning;

        SelectWidgetsViewModel model = (SelectWidgetsViewModel)BindingContext;
        await new Core.Program().ProcessMethod(
            model.FitMessages, 
            selectWidgetIds!,
            saveLocation.Text,
            FileSystem.AppDataDirectory,
            cancellationTokenForExport,
            (byte)selectedFps.SelectedItem,
            rangeDatesActivity.StartValue.ToUniversalTime(),
            rangeDatesActivity.EndValue.ToUniversalTime(),
            useStartMarker.IsChecked
            );
    }

    private void CancelBtn_Clicked(object sender, EventArgs e)
    {
        if (cancellationTokenForExport.Token.CanBeCanceled)
        {
            exportLoaderIndicator.IsRunning = false;
            exportBtn.IsEnabled = !exportLoaderIndicator.IsRunning;

            cancellationTokenForExport.Cancel();
        }
    }
}
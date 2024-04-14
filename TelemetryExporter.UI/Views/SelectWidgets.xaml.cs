using System.ComponentModel;

using Microsoft.Maui.Controls.Shapes;

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

    // First comes ApplyQueryAttributes then  OnSizeAllocated
    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        SelectWidgetsViewModel model = (SelectWidgetsViewModel)BindingContext;

        model.Initialize((Stream)query[TEConstants.QueryKeys.FitStreamKey]);

        elevationImage.SetBinding(Image.SourceProperty, new Binding(nameof(model.MyImage), source: model));
        rangeDatesActivity.InitializeMinMax(model.StartActivityDate, model.EndActivityDate);
    }

    protected override void OnSizeAllocated(double width, double height)
    {
        base.OnSizeAllocated(width, height);

        if (this.Window == null)
        {
            return;
        }

        pausedIntervals.Children.Clear();

        SelectWidgetsViewModel model = (SelectWidgetsViewModel)BindingContext;

        long range = model.EndActivityDate.Ticks - model.StartActivityDate.Ticks;

        foreach ((DateTime start, DateTime end) in model.PausePeriods)
        {
            long startRelativeToRange = start.Ticks - model.StartActivityDate.Ticks;
            long endRelativeToRange = end.Ticks - model.StartActivityDate.Ticks;

            double percentageStart = startRelativeToRange / (double)range;
            double percentageEnd = endRelativeToRange / (double)range;

            double xStart = Content.Width * percentageStart;
            double xEnd = Content.Width * percentageEnd;

            Rectangle pausePeriod = new() { HeightRequest = 3, Fill = Colors.Red };
            AbsoluteLayout.SetLayoutBounds(pausePeriod, new Rect(xStart, 0, xEnd - xStart, pausePeriod.HeightRequest));
            pausedIntervals.Children.Add(pausePeriod);
        }
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
        Core.Program exporter = new();
        exporter.OnProgress += Exporter_OnProgress;

        try
        {
            await exporter.ExportImageFramesAsync(
                model.FitMessages,
                selectWidgetIds!,
                saveLocation.Text,
                FileSystem.CacheDirectory,
                cancellationTokenForExport.Token,
                (byte)selectedFps.SelectedItem,
                rangeDatesActivity.StartValue.ToUniversalTime(),
                rangeDatesActivity.EndValue.ToUniversalTime(),
                useStartMarker.IsChecked);

            if (cancellationTokenForExport.Token.IsCancellationRequested)
            {
                this.statusPanel.Text = "Canceled";
                this.exportProgress.Progress = 0;
            }
            else
            {
                await DisplayAlert("Done!", "Export Done!", "OK");
                this.exportProgress.Progress = 1;
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", ex.Message, "OK");

            this.statusPanel.Text = "Canceled";
            this.exportProgress.Progress = 0;
        }
        finally
        {
            exporter.OnProgress -= Exporter_OnProgress;

            exportLoaderIndicator.IsRunning = false;
            exportBtn.IsEnabled = !exportLoaderIndicator.IsRunning;
        }
    }

    /// <summary>
    /// The code is invoked from exporting main logic. In order to update UI needs to execute on UI thread.
    /// </summary>
    /// <param name="progressArgs">A <see cref="Dictionary{TKey, TValue}"/>
    /// of widgets as keys and values are their progress in percentage. From 0 to 1.</param>
    private void Exporter_OnProgress(object? _, Dictionary<string, double> progressArgs)
    {
        void SetTextDate(Dictionary<string, double> data)
        {
            string resultProgress = string.Join(
                Environment.NewLine,
                data.Select(pair => $"{pair.Key}: {pair.Value:P}"));

            this.statusPanel.Text = resultProgress;
            exportProgress.Progress = progressArgs.Values.Average();
        }

        if (MainThread.IsMainThread)
        {
            SetTextDate(progressArgs);
        }
        else
        {
            MainThread.BeginInvokeOnMainThread(() => SetTextDate(progressArgs));
        }
    }

    private void CancelBtn_Clicked(object sender, EventArgs e)
    {
        if (cancellationTokenForExport.Token.CanBeCanceled)
        {
            exportLoaderIndicator.IsRunning = false;
            exportBtn.IsEnabled = !exportLoaderIndicator.IsRunning;

            cancellationTokenForExport.Cancel();

            this.statusPanel.Text = "Canceled";
            this.exportProgress.Progress = 0;
        }
    }
}
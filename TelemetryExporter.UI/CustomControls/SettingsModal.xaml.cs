namespace TelemetryExporter.UI.CustomControls;

using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Internals;

using SkiaSharp;

using TelemetryExporter.Core.Models;
using TelemetryExporter.Core.SettingsTypes;
using TelemetryExporter.Core.Widgets.Interfaces;
using TelemetryExporter.UI.Converters;
using TelemetryExporter.UI.Extensions;

public partial class SettingsModal : ContentPage
{
    private static readonly LinearGradientBrush OuterBrushNormal = new()
    {
        StartPoint = new Point(0, 0),
        EndPoint = new Point(1, 1),
        GradientStops =
        [
            new GradientStop { Color = Color.FromArgb("#E0E0E0"), Offset = 0.5f },
            new GradientStop { Color = Color.FromArgb("#282828"), Offset = 0.5f },
        ]
    };

    private static readonly LinearGradientBrush OuterBrushHover = new()
    {
        StartPoint = new Point(0, 0),
        EndPoint = new Point(1, 1),
        GradientStops =
        [
            new GradientStop { Color = Color.FromArgb("#282828"), Offset = 0.5f },
            new GradientStop { Color = Color.FromArgb("#E0E0E0"), Offset = 0.5f },
        ]
    };

    private static readonly FrameData DefaultFrameData = new() { FileName = string.Empty };

    public SettingsModal(IWidget widget)
    {
        InitializeComponent();
        InitializeBurhesAsResource();

        widgetTitle.Text = $"{widget.Name} settings";
        if (widget == null || widget.Settings.Count == 0)
        {
            Label notDefinedSettingsMessage = new() { HorizontalOptions = LayoutOptions.Center, Text = "Settings not defined!" };
            gridContainer.AddWithSpan(notDefinedSettingsMessage, columnSpan: 3);
            return;
        }

        Image imgPreview = new() { HorizontalOptions = LayoutOptions.Center, WidthRequest = 250 };
        Slider sliderExampleUnit = new() { Maximum = 100, Minimum = 0, Value = 50, WidthRequest = 250, HorizontalOptions = LayoutOptions.Center, VerticalOptions = LayoutOptions.Center };

        CheckBox useDefaultOrEmptyUnits = new();
        Label defaultOrEmptyUnitsLabel = new() { Text = "Empty or default value", VerticalOptions = LayoutOptions.Center };
        TapGestureRecognizer labelTab = new();
        labelTab.Tapped += (s, e) =>
        {
            useDefaultOrEmptyUnits.IsChecked = !useDefaultOrEmptyUnits.IsChecked;
        };
        defaultOrEmptyUnitsLabel.GestureRecognizers.Add(labelTab);

        async void updatePreview()
        {
            imgPreview.Source = await GetPreviewImage(widget, sliderExampleUnit.Value, useDefaultOrEmptyUnits.IsChecked);
        };

        for (int row = 0; row < widget.Settings.Count; row++)
        {
            SettingsModel setting = widget.Settings[row];

            bool newSettingAdded = true;
            switch (setting.GetValueType())
            {
                case Type t when t == typeof(SKColor):
                    AddColorPickerRow(gridContainer, setting, row, widget, updatePreview);

                    break;
                case Type t when t == typeof(FontStringOptions):
                   AddFontPickerRow(gridContainer, setting, row, widget, updatePreview);
                    break;

                case Type t when t == typeof(string):
                    AddInputRow(gridContainer, setting, row, widget, updatePreview);
                    break;

                case Type t when t == typeof(float):
                    AddNumericSlider(gridContainer, setting, row, widget, updatePreview);
                    break;

                default:
                    newSettingAdded = false;
                    break;
            }

            if (newSettingAdded)
            {
                Label settingNameLabel = new()
                {
                    Text = setting.Title,
                    HorizontalOptions = LayoutOptions.End,
                    Padding = 10
                };

                gridContainer.AddWithSpan(settingNameLabel, row);

                //TODO: add into and enabled checkbox later
                //TODO: reset button
            }
        }

        gridContainer.AddWithSpan(sliderExampleUnit, widget.Settings.Count, columnSpan: 3);

        HorizontalStackLayout horizontalStackLayout = [];
        horizontalStackLayout.HorizontalOptions = LayoutOptions.End;
        
        horizontalStackLayout.Add(defaultOrEmptyUnitsLabel);
        horizontalStackLayout.Add(useDefaultOrEmptyUnits);
        gridContainer.AddWithSpan(horizontalStackLayout, widget.Settings.Count, 2);

        sliderExampleUnit.BindingContext = useDefaultOrEmptyUnits;
        sliderExampleUnit.SetBinding(Slider.IsEnabledProperty, nameof(useDefaultOrEmptyUnits.IsChecked), BindingMode.OneWay, converter: new DisabledControlConverter());

        Label previewButton = new() { Text = "Preview", HorizontalOptions = LayoutOptions.End, Padding = 10, FontSize = 20 };
        
        sliderExampleUnit.PropertyChanged += async (s, e) =>
        {
            // TODO: if widget is INeedInitialization
            // how to setup needed data?
            // a) use values from uploaded .fit file (too much data)
            // b) use static predefined data
            imgPreview.Source = await GetPreviewImage(widget, sliderExampleUnit.Value, useDefaultOrEmptyUnits.IsChecked);
        };

        gridContainer.AddWithSpan(previewButton, widget.Settings.Count);

        Image imageCheckboard = new() { WidthRequest = imgPreview.WidthRequest, HorizontalOptions = LayoutOptions.Center, HeightRequest = imgPreview.HeightRequest };

        imgPreview.SizeChanged += (s, e) =>
        {
            if (imgPreview.Height < 1)
            {
                // Causes issues with GenerateCheckedBoardBackground()
                return;
            }

            if (Math.Floor(imageCheckboard.HeightRequest) == Math.Floor(imgPreview.Height))
            {
                // avoid unnecessary initializations
                return;
            }

            imageCheckboard.HeightRequest = imgPreview.Height;
            imageCheckboard.MaximumHeightRequest = imgPreview.Height;
            imageCheckboard.GenerateCheckedBoardBackground();
        };
        gridContainer.AddWithSpan(imageCheckboard, widget.Settings.Count + 1, columnSpan: 3);
        gridContainer.AddWithSpan(imgPreview, widget.Settings.Count + 1, columnSpan: 3);
    }

    private async void CloseModal(object? sender, EventArgs e)
    {
        await Navigation.PopModalAsync();
    }

    private void InitializeBurhesAsResource()
    {
        Content.Resources.Add(nameof(OuterBrushNormal), OuterBrushNormal);
        Content.Resources.Add(nameof(OuterBrushHover), OuterBrushHover);
    }

    private void AddColorPickerRow(Grid gridContainer, SettingsModel setting, int row, IWidget widget,
        Action updatePreviewImage)
    {
        ColorPicker colorPickerControl = new()
        {
            Margin = new Thickness(10, 0, 0, 0),
            SelectedColor = Color.FromArgb(setting.GetValue<SKColor>().ToString()),
            IsVisible = false
        };

        colorPickerControl.OnColorChanged += (sender, newColor) =>
        {
            if (newColor != null)
            {
                widget.SetSetting(setting.Title, SKColor.Parse(newColor.ToArgbHex()));
                updatePreviewImage();
            }
        };

        Button butt = new()
        {
            CornerRadius = 0,
            BindingContext = colorPickerControl,
        };
        butt.Pressed += (_, _) =>
        {
            colorPickerControl.IsVisible = !colorPickerControl.IsVisible;
        };

        butt.SetBinding(Button.BackgroundColorProperty, nameof(colorPickerControl.SelectedColor));

        int colorSquareSize = 46;
        AbsoluteLayout viewAdd = [];
        Image imageCheckBoard = new() { WidthRequest = colorSquareSize, HeightRequest = colorSquareSize, VerticalOptions = LayoutOptions.Start, HorizontalOptions = LayoutOptions.Center };
        imageCheckBoard.GenerateCheckedBoardBackground();

        viewAdd.Children.Add(imageCheckBoard);
        viewAdd.Children.Add(butt);

        Border innerBorder = new()
        {
            HeightRequest = colorSquareSize,
            WidthRequest = colorSquareSize,
            StrokeThickness = 2,
            Stroke = OuterBrushHover,
            Content = viewAdd
        };

        string innerBorderName = $"innerBorder_{row}";
        // Keep in mind that is non-standard code for assign name of the component
        NameScope.GetNameScope(this).RegisterName(innerBorderName, innerBorder);

        Border outerBorder = new()
        {
            HeightRequest = 50,
            WidthRequest = 50,
            StrokeThickness = 2,
            HorizontalOptions = LayoutOptions.Start,
            VerticalOptions = LayoutOptions.Start,
            Stroke = OuterBrushNormal,
            Content = innerBorder
        };

        VisualStateGroupList gList =
        [
            new VisualStateGroup()
            {
                Name = "PointerOver",
                States =
                {
                    new VisualState() { Name = "Normal" },
                    new VisualState()
                    {
                        Name = "PointerOver",
                        Setters =
                        {
                            new Setter()
                            {
                                Property = Border.StrokeProperty,
                                Value = OuterBrushHover
                            },
                            new Setter()
                            {
                                TargetName = innerBorderName,
                                Property = Border.StrokeProperty,
                                Value = OuterBrushNormal
                            }
                        }
                    }
                }
            }
        ];

        VisualStateManager.SetVisualStateGroups(outerBorder, gList);
        ToolTipProperties.SetText(outerBorder, "Change color");

        outerBorder.SetDynamicResource(Border.StrokeProperty, nameof(OuterBrushNormal));

        Entry colorCodeEntry = new()
        {
            VerticalOptions = LayoutOptions.Start,
            HorizontalOptions = LayoutOptions.Start,
            WidthRequest = 100,
            MinimumHeightRequest = 40,
            HeightRequest = 40,
        };

        colorPickerControl.Margin = colorPickerControl.Margin with
        {
            Left = colorPickerControl.Margin.Left + colorCodeEntry.WidthRequest + outerBorder.WidthRequest
        };

        outerBorder.Margin = outerBorder.Margin with
        {
            Left = colorCodeEntry.WidthRequest + 5
        };

        ToolTipProperties.SetText(colorCodeEntry, "ARGB format");
        colorCodeEntry.BindingContext = colorPickerControl;
        colorCodeEntry.SetBinding(
            Entry.TextProperty,
            nameof(colorPickerControl.SelectedColor),
            converter: new ARGBColorConverter());

        gridContainer.AddWithSpan(outerBorder, row, column: 1);
        gridContainer.AddWithSpan(colorPickerControl, row, column: 1);
        gridContainer.AddWithSpan(colorCodeEntry, row, column: 1);
    }

    private static void AddFontPickerRow(Grid gridContainer, SettingsModel setting, int row, IWidget widget,
        Action updatePreviewImage)
    {
        Picker fontPicker = new()
        {
            ItemsSource = FontStringOptions.Values,
            SelectedItem = setting.GetValue<string>(),
        };
        fontPicker.SelectedIndexChanged += (_, _) =>
        {
            if (fontPicker.SelectedItem != null && fontPicker.SelectedIndex != -1)
            {
                widget.SetSetting(setting.Title, fontPicker.SelectedItem);
            }

            updatePreviewImage();
        };
        gridContainer.AddWithSpan(fontPicker, row, column: 1);
    }

    private static void AddInputRow(Grid gridContainer, SettingsModel setting, int row, IWidget widget,
        Action updatePreviewImage)
    {
        Entry inputEntry = new()
        {
            Text = setting.GetValue<string>(),
        };
        inputEntry.TextChanged += (_, e) =>
        {
            widget.SetSetting(setting.Title, e.NewTextValue);
            updatePreviewImage();
        };
        gridContainer.AddWithSpan(inputEntry, row, column: 1);
    }

    private static void AddNumericSlider(Grid gridContainer, SettingsModel setting, int row, IWidget widget,
        Action updatePreviewImage)
    {
        float value = setting.GetValue<float>();
        Slider floatSlider = new()
        {
            Minimum = setting.Min ?? 0,
            Maximum = setting.Max ?? value,
            Value = value,
            Margin = new Thickness(25, 0, 0, 0)
        };
        floatSlider.ValueChanged += (_, e) =>
        {
            widget.SetSetting(setting.Title, (float)e.NewValue);
            updatePreviewImage();
        };
        Label sliderValuePresenter = new()
        {
            BindingContext = floatSlider,
            HorizontalOptions = LayoutOptions.Start,
            VerticalOptions = LayoutOptions.Center,
        };

        sliderValuePresenter.SetBinding(Label.TextProperty, nameof(floatSlider.Value), stringFormat: "{0:F0}");
        gridContainer.AddWithSpan(floatSlider, row, column: 1);
        gridContainer.AddWithSpan(sliderValuePresenter, row, column: 1);
    }

    private static async Task<ImageSource> GetPreviewImage(IWidget widget, double value, bool useDefaultValue = false)
    {
        SKData data = await widget.GenerateImage(
                new SessionData() { TotalDistance = 1000 },
                useDefaultValue ? DefaultFrameData : new FrameData()
                {
                    Distance = value * 10,
                    Altitude = value,
                    Grade = value - 50,
                    Power = (ushort)value,
                    FileName = string.Empty,
                    Speed = value,
                    ElapsedTime = TimeOnly.FromTimeSpan(TimeSpan.FromSeconds(value)),
                    CurrentTime = TimeOnly.FromDateTime(DateTime.Now.AddSeconds(value)),
                });

        MemoryStream memoryStream = new(data.ToArray());
        return ImageSource.FromStream(() => memoryStream);
    }
}
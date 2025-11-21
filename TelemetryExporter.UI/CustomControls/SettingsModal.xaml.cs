namespace TelemetryExporter.UI.CustomControls;

using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Internals;

using SkiaSharp;

using TelemetryExporter.Core.Models;
using TelemetryExporter.Core.SettingsTypes;
using TelemetryExporter.Core.Widgets.Interfaces;
using TelemetryExporter.UI.Converters;

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

    public SettingsModal(IWidget widget)
    {
        InitializeComponent();
        InitializeBurhesAsResource();

        widgetTitle.Text = $"{widget.Name} settings";
        if (widget == null)
        {
            return;
        }

        for (int row = 0; row < widget.Settings.Count; row++)
        {
            SettingsModel setting = widget.Settings[row];

            bool newSettingAdded = true;
            switch (setting.GetValueType())
            {
                case Type t when t == typeof(SKColor):
                    gridContainer.RowDefinitions.Add(new RowDefinition(GridLength.Auto));
                    AddColorPickerRow(gridContainer, setting, row, widget);
                    
                    break;
                case Type t when t == typeof(FontStringOptions):
                    gridContainer.RowDefinitions.Add(new RowDefinition(GridLength.Auto));
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
                    };
                    gridContainer.Add(fontPicker, column: 1, row);
                    break;
                case Type t when t == typeof(string):
                    gridContainer.RowDefinitions.Add(new RowDefinition(GridLength.Auto));
                    Entry inputEntry = new()
                    {
                        Text = setting.GetValue<string>(),
                    };
                    inputEntry.TextChanged += (_, e) =>
                    {
                        widget.SetSetting(setting.Title, e.NewTextValue);
                    };
                    gridContainer.Add(inputEntry, column: 1, row);
                    break;
                case Type t when t == typeof(float):
                    gridContainer.RowDefinitions.Add(new RowDefinition(GridLength.Auto));
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
                    };
                    Label sliderValuePresenter = new()
                    {
                        BindingContext = floatSlider,
                        HorizontalOptions = LayoutOptions.Start,
                        VerticalOptions = LayoutOptions.Center,
                    };

                    sliderValuePresenter.SetBinding(Label.TextProperty, nameof(floatSlider.Value), stringFormat: "{0:F0}");
                    gridContainer.Add(floatSlider, column: 1, row);
                    gridContainer.Add(sliderValuePresenter, column: 1, row);
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

                gridContainer.Add(settingNameLabel, column: 0, row);

                // add into and enabled checkbox later
            }
        }

        gridContainer.RowDefinitions.Add(new RowDefinition(GridLength.Auto));
        Slider currentValue = new() { Minimum = 0, Maximum = 100 };
        gridContainer.Add(currentValue, 0, gridContainer.RowDefinitions.Count - 1);
        Button bb = new() { Text = "Preview", HorizontalOptions = LayoutOptions.Center };
        Image imgPreview = new() { HorizontalOptions = LayoutOptions.Center, WidthRequest = 250 };
        bb.Pressed += async (s, e) => 
        {
            SKData data = await widget.GenerateImage(
                new SessionData() { TotalDistance = 1000 },
                new FrameData() { Distance = currentValue.Value * 10, FileName = string.Empty });

            MemoryStream memoryStream = new(data.ToArray());

            imgPreview.Source = ImageSource.FromStream(() => memoryStream);
        };

        gridContainer.Add(bb, 0, gridContainer.RowDefinitions.Count - 1);
        gridContainer.SetColumnSpan(bb, 3);

        gridContainer.RowDefinitions.Add(new RowDefinition(GridLength.Auto));
        gridContainer.Add(imgPreview, 0, gridContainer.RowDefinitions.Count - 1);
        gridContainer.SetColumnSpan(imgPreview, 3);
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

    private void AddColorPickerRow(Grid gridContainer, SettingsModel setting, int row, IWidget widget)
    {
        ColorPicker colorPickerControl = new()
        {
            Margin = new Thickness(10, 0, 0, 0),
            SelectedColor = Color.FromArgb(setting.GetValue<SKColor>().ToString()),
            IsVisible = false
        };

        colorPickerControl.OnColorChanged += (object? sender, Color? newColor) =>
        {
            if (newColor != null)
            {
                widget.SetSetting(setting.Title, SKColor.Parse(newColor.ToArgbHex()));
                
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

        Border innerBorder = new()
        {
            HeightRequest = 46,
            WidthRequest = 46,
            StrokeThickness = 2,
            Stroke = OuterBrushHover,
            Content = butt
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
            Left = colorPickerControl.Margin.Left + colorCodeEntry.WidthRequest + outerBorder .WidthRequest
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

        gridContainer.Add(outerBorder, column: 1, row);
        gridContainer.Add(colorPickerControl, column: 1, row);
        gridContainer.Add(colorCodeEntry, column: 1, row);
    }
}
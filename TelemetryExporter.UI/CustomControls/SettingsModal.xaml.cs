namespace TelemetryExporter.UI.CustomControls;
using Microsoft.Maui.Controls;

using TelemetryExporter.Core.Widgets.Distance;

public partial class SettingsModal : ContentPage
{
    public SettingsModal()
    {
        InitializeComponent();
        // Can I add widget programmatically here?

        var theColorPicker = new CustomControls.ColorPicker()
        {
            HorizontalOptions = LayoutOptions.Center,
            WidthRequest = 300,
            SelectedColor = Color.FromArgb("#0909D4")
        };

        BoxView boxView = new() { };


        boxView.BindingContext = theColorPicker;
        //boxView.BackgroundColor = Colors.Aqua;
        // below will work when switch to MAUI 9 ....  :/ 
        // boxView.SetBinding(BoxView.BackgroundColorProperty, static (ColorPicker picker) => picker.SelectedColor);
        boxView.SetBinding(BoxView.BackgroundColorProperty, "SelectedColor");

        gridContainer.Add(theColorPicker, row: 1);
        gridContainer.Add(boxView, row: 2);

        
    }

    private async void CloseModal(object sender, EventArgs e)
    {
        await Navigation.PopModalAsync();
    }
}
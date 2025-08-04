namespace TelemetryExporter.UI.CustomControls;

public partial class SettingsModal : ContentPage
{
	public SettingsModal()
	{
		InitializeComponent();
	}

    private async void CloseModal(object sender, EventArgs e)
    {
		await Navigation.PopModalAsync();
    }
}
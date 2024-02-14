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
    }
}
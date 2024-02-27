using CommunityToolkit.Maui.Views;

namespace TelemetryExporter.UI.CustomControls
{
    internal class ExpanderWidgetArgs(Expander expander, Label label)
    {
        public Label ExpanderIconElement { get; set; } = label;
        public Expander Sender { get; set; } = expander;
    }
}

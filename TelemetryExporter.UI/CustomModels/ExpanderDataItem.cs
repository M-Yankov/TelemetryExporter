using System.Windows.Input;

using TelemetryExporter.UI.CustomControls;

namespace TelemetryExporter.UI.CustomModels
{
    public class ExpanderDataItem
    {
        public string? Category { get; set; }

        public List<WidgetData> Widgets { get; set; } = [];

        public ICommand SwitchStateCommand => new Command<ExpanderWidgetArgs>(SwitchLabelIconExpandedState);

        private void SwitchLabelIconExpandedState(ExpanderWidgetArgs expanderWidgetArgs)
        {
            expanderWidgetArgs.ExpanderIconElement.Text = expanderWidgetArgs.Sender.IsExpanded ? "X" : "V";
        }
    }
}

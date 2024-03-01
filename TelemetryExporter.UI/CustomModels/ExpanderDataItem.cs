using System.Windows.Input;

using TelemetryExporter.Core.Attributes;
using TelemetryExporter.UI.CustomControls;

namespace TelemetryExporter.UI.CustomModels
{
    public class ExpanderDataItem
    {
        public string? Category { get; set; }

        public WidgetDataAttribute[]? Widgets { get; set; }

        public ICommand SwitchStateCommand => new Command<ExpanderWidgetArgs>(SwitchLabelIconExpandedState);

        private void SwitchLabelIconExpandedState(ExpanderWidgetArgs expanderWidgetArgs)
        {
            expanderWidgetArgs.ExpanderIconElement.Text = expanderWidgetArgs.Sender.IsExpanded ? "X" : "V";
        }
    }
}

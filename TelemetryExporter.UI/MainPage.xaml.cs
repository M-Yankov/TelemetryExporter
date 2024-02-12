namespace TelemetryExporter.UI
{
    public partial class MainPage : ContentPage
    {
        int count = 0;

        public MainPage()
        {
            InitializeComponent();
        }

        // SemanticScreenReader.Announce(CounterBtn.Text);

        private void DropGestureRecognizer_DragOver(object sender, DragEventArgs e)
        {
            TheBorder.Stroke = Colors.Red;
        }

        private void DropGestureRecognizer_DragLeave(object sender, DragEventArgs e)
        {
            TheBorder.Stroke = Colors.White;
        }
    }
}

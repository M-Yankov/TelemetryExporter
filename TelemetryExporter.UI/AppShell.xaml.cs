﻿namespace TelemetryExporter.UI
{
    public partial class AppShell : Shell
    {
        public AppShell()
        {
            InitializeComponent();
            AppShellRouterConfig.Configure();
        }
    }
}

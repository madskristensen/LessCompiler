using EnvDTE;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Text.Editor;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;

namespace LessCompiler
{
    class LessAdornment : TextBlock
    {
        private Project _project;

        public LessAdornment(IWpfTextView view, Project project)
        {
            _project = project;
            Visibility = Visibility.Hidden;

            Loaded += (s, e) =>
            {
                Initialize();
            };

            view.Closed += ViewClosed;
            Settings.Changed += SettingsChanged;

            IAdornmentLayer adornmentLayer = view.GetAdornmentLayer(AdornmentLayer.LayerName);

            if (adornmentLayer.IsEmpty)
                adornmentLayer.AddAdornment(AdornmentPositioningBehavior.ViewportRelative, null, null, this, null);

            ThreadHelper.Generic.BeginInvoke(DispatcherPriority.ApplicationIdle, () =>
            {
                SetAdornmentLocation(view, EventArgs.Empty);

                view.ViewportHeightChanged += SetAdornmentLocation;
                view.ViewportWidthChanged += SetAdornmentLocation;
            });
        }

        private void Initialize()
        {
            bool enabled = Settings.IsEnabled(_project);

            SetText(enabled);

            FontSize = 17;
            Cursor = Cursors.Hand;
            Opacity = 0.5;

            SetResourceReference(Control.ForegroundProperty, VsBrushes.CaptionTextKey);
            SetValue(TextOptions.TextRenderingModeProperty, TextRenderingMode.Aliased);
            SetValue(TextOptions.TextFormattingModeProperty, TextFormattingMode.Ideal);
            MouseLeftButtonUp += OnClick;
        }

        private void SetText(bool enabled)
        {
            string onOff = enabled ? "On" : "Off";
            Text = $"Compile: {onOff}";

            if (enabled)
                ToolTip = $"The LESS Compiler is enabled for project \"{_project.Name}\".\r\nClick to disable it.";
            else
                ToolTip = $"The LESS Compiler is disabled for project \"{_project.Name}\".\r\nClick to enable it.";
        }

        private void SetAdornmentLocation(object sender, EventArgs e)
        {
            var view = (IWpfTextView)sender;
            Canvas.SetLeft(this, view.ViewportRight - ActualWidth - 20);
            Canvas.SetTop(this, view.ViewportBottom - ActualHeight - 20);
            Visibility = Visibility.Visible;
        }

        private void OnClick(object sender, MouseButtonEventArgs e)
        {
            bool enabled = Settings.IsEnabled(_project);
            Settings.Enable(_project, !enabled);

            e.Handled = true;
        }

        private void SettingsChanged(object sender, SettingsChangedEventArgs e)
        {
            var project = (Project)sender;

            if (project.UniqueName == _project.UniqueName)
                SetText(e.Enabled);
        }

        private void ViewClosed(object sender, EventArgs e)
        {
            var view = (IWpfTextView)sender;
            view.Closed -= ViewClosed;
            view.ViewportHeightChanged -= SetAdornmentLocation;
            view.ViewportWidthChanged -= SetAdornmentLocation;
            Settings.Changed -= SettingsChanged;
        }
    }
}

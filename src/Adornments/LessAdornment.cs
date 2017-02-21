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
    class LessAdornment : StackPanel
    {
        private Project _project;
        private CompilerOptions _options;
        private TextBlock _text;
        private ITextView _view;

        public LessAdornment(IWpfTextView view, Project project)
        {
            _view = view;
            _project = project;

            Visibility = Visibility.Hidden;

            view.Closed += ViewClosed;
            Settings.Changed += SettingsChanged;

            IAdornmentLayer adornmentLayer = view.GetAdornmentLayer(AdornmentLayer.LayerName);

            if (adornmentLayer.IsEmpty)
                adornmentLayer.AddAdornment(AdornmentPositioningBehavior.ViewportRelative, null, null, this, null);
        }

        protected override void OnInitialized(EventArgs e)
        {
            bool enabled = _project.IsLessCompilationEnabled();

            Opacity = 0.5;
            Cursor = Cursors.Hand;
            MouseLeftButtonUp += OnClick;

            var header = new TextBlock()
            {
                FontSize = 20,
                Text = "LESS Compiler"
            };

            _text = new TextBlock
            {
                FontSize = 16
            };

            ThemeControl(header);
            ThemeControl(_text);

            SetText(enabled);

            Children.Add(header);
            Children.Add(_text);

            Dispatcher.BeginInvoke(DispatcherPriority.Loaded, new Action(() =>
            {
                SetAdornmentLocation(_view, EventArgs.Empty);

                _view.ViewportHeightChanged += SetAdornmentLocation;
                _view.ViewportWidthChanged += SetAdornmentLocation;
            }));
        }

        private void ThemeControl(TextBlock text)
        {
            text.SetResourceReference(Control.ForegroundProperty, VsBrushes.CaptionTextKey);
            text.SetValue(TextOptions.TextRenderingModeProperty, TextRenderingMode.Aliased);
            text.SetValue(TextOptions.TextFormattingModeProperty, TextFormattingMode.Ideal);
        }

        public async System.Threading.Tasks.Task Update(CompilerOptions options)
        {
            _options = options;

            await Dispatcher.BeginInvoke(DispatcherPriority.Input, new Action(() =>
            {
                bool enabled = _project.IsLessCompilationEnabled();
                SetText(enabled);

                SetAdornmentLocation(_view, EventArgs.Empty);
            }));
        }

        private void SetText(bool projectEnabled)
        {
            string projectOnOff = projectEnabled ? "On" : "Off";
            string fileOnOff = _options == null ? "Ignored" : (_options.Compile ? "On" : "Off");

            if (!projectEnabled && fileOnOff == "On")
                fileOnOff = "Off";

            _text.Text = $"   Project: {projectOnOff}\r\n" +
                         $"   File: {fileOnOff}";

            if (projectEnabled)
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
            bool enabled = _project.IsLessCompilationEnabled();
            _project.EnableLessCompilation(!enabled);

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

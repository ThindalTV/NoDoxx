using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using NoDoxx.Interfaces;
using NoDoxx.ValueLocators;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Controls;
using System.Windows.Media;

namespace NoDoxx.Adorners
{
    internal sealed class ConfigurationHiderAdorner
    {
        private readonly IAdornmentLayer _buttonsLayer;
        private readonly IAdornmentLayer _configValueLayer;
        private readonly IAdornmentLayer _commentLayer;
        private readonly IWpfTextView _view;
        private readonly Brush _brush;
        private readonly Pen _pen;
        private readonly StackPanel _buttonsPanel;

        public ConfigurationHiderAdorner(IWpfTextView view)
        {
            if (view == null)
            {
                throw new ArgumentNullException("view");
            }

            _configValueLayer = view.GetAdornmentLayer("ConfigurationHiderValuesAdorner");
            _commentLayer = view.GetAdornmentLayer("ConfigurationHiderCommentsAdorner");
            _buttonsLayer = view.GetAdornmentLayer("ConfigurationHiderButtonsAdorner");

            _view = view;

            // Create the pen and brush to color the box hiding the config values
            _brush = _view.Background;
            var outlineBrush = new SolidColorBrush(Colors.LightBlue);
            _pen = new Pen(outlineBrush, 0.5);
            _pen.Freeze();

            _view.LayoutChanged += OnLayoutChanged;

            _buttonsPanel = CreateButtonsPanel();

        }

        internal StackPanel CreateButtonsPanel()
        {
            var buttonsPanel = new StackPanel()
            {
                Orientation = Orientation.Horizontal
            };

            var showConfigValuesButton = new Button()
            {
                Content = "Display config values",
                Padding = new System.Windows.Thickness(20),
                Cursor = System.Windows.Input.Cursors.Hand,
                Margin = new System.Windows.Thickness(100, 10, 10, 10),
                Visibility = _configValueLayer.Opacity == 0
                ? System.Windows.Visibility.Collapsed
                : System.Windows.Visibility.Visible
            };

            var showCommentsButton = new Button()
            {
                Content = "Display comments",
                Cursor = System.Windows.Input.Cursors.Hand,
                Padding = new System.Windows.Thickness(20),
                Margin = new System.Windows.Thickness(10),
                Visibility = _commentLayer.Opacity == 0
                ? System.Windows.Visibility.Collapsed
                : System.Windows.Visibility.Visible
            };

            buttonsPanel.Children.Add(showConfigValuesButton);
            buttonsPanel.Children.Add(showCommentsButton);

            showConfigValuesButton.Click +=
                (object sender,
                System.Windows.RoutedEventArgs e) =>
                {
                    _configValueLayer.Opacity = 0; // Flip opacity
                    _commentLayer.Opacity = 0; // Flip opacity
                    _buttonsLayer.Opacity = 0;
                    showConfigValuesButton.Visibility = System.Windows.Visibility.Collapsed;
                    showCommentsButton.Visibility = System.Windows.Visibility.Collapsed;
                };

            showCommentsButton.Click +=
                (object sender,
                System.Windows.RoutedEventArgs e) =>
                {
                    _commentLayer.Opacity = 0; // Flip opacity
                    showCommentsButton.Visibility = System.Windows.Visibility.Collapsed;
                };

            return buttonsPanel;
        }

        internal void OnLayoutChanged(object sender, TextViewLayoutChangedEventArgs e)
        {
            var type = _view.TextSnapshot.ContentType.TypeName;
            IValueLocator locator = null;

            if (type == "JSON")
            {
                locator = new JsonValueLocator();
            }
            else if (type == "XML")
            {
                locator = new XmlValueLocator();
            }

            if (locator == null) return;

            var contents = _view.TextSnapshot.GetText();
            try
            {
                HideByIndexes(locator.FindConfigValues(contents));
            }
            catch
            {
                HideByIndexes(new[] { new ConfigPosition(0, contents.Length, ConfigType.Value) });
            }
        }

        internal void HideByIndexes(IEnumerable<ConfigPosition> positions)
        {
            Clear();

            var pos = positions.Where(p => p.StartIndex != p.EndIndex).GroupBy(p => p.StartIndex).Select(p => p.First()).ToList();
            foreach (var p in pos)
            {
                HideData(p.StartIndex, p.EndIndex, p.Type);
            }

            Canvas.SetTop(_buttonsPanel, _buttonsLayer.TextView.ViewportTop);
            Canvas.SetLeft(_buttonsPanel, _buttonsLayer.TextView.ViewportLeft);
            _buttonsLayer.AddAdornment(AdornmentPositioningBehavior.OwnerControlled, null, null, _buttonsPanel, null);
        }

        private void Clear()
        {
            _configValueLayer.RemoveAllAdornments();
            _commentLayer.RemoveAllAdornments();
            _buttonsLayer.RemoveAllAdornments();
        }

        private void HideData(int startOffset, int stopOffset, ConfigType type)
        {
            IWpfTextViewLineCollection textViewLines = _view.TextViewLines;
            SnapshotSpan span = new SnapshotSpan(_view.TextSnapshot, Span.FromBounds(startOffset, stopOffset));
            Geometry geometry = textViewLines.GetMarkerGeometry(span);
            if (geometry != null)
            {
                var drawing = new GeometryDrawing(_brush, _pen, geometry);
                drawing.Freeze();

                var drawingImage = new DrawingImage(drawing);
                drawingImage.Freeze();

                var image = new Image
                {
                    Source = drawingImage,
                };

                // Align the image with the top of the bounds of the text geometry
                Canvas.SetLeft(image, geometry.Bounds.Left);
                Canvas.SetTop(image, geometry.Bounds.Top);

                if (type == ConfigType.Value)
                {
                    _configValueLayer.AddAdornment(AdornmentPositioningBehavior.TextRelative, span, null, image, null);
                }
                else if (type == ConfigType.Comment)
                {
                    _commentLayer.AddAdornment(AdornmentPositioningBehavior.TextRelative, span, null, image, null);
                }
                else
                {
                    throw new ArgumentException($"{type} is not supported.");
                }

            }
        }
    }
}

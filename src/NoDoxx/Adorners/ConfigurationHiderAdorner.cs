using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Controls;
using System.Windows.Media;
using NoDoxx.Interfaces;
using NoDoxx.ValueLocators;
using Microsoft.VisualStudio.PlatformUI;
using Microsoft.VisualStudio.Shell;
using System.Windows;
using System.Windows.Media.Animation;

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

        private readonly object _locatorLock = new object();

        private int _currentContentsHash;
        private List<ConfigPosition> _configValuePositions;

        private bool ValuesAreHidden => _commentLayer.Opacity == 1;

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

            _buttonsLayer.AddAdornment(AdornmentPositioningBehavior.ViewportRelative, null, null, _buttonsPanel, null);
        }

        internal StackPanel CreateButtonsPanel()
        {
            var buttonsPanel = new StackPanel()
            {
                Orientation = Orientation.Horizontal
            };

            var showConfigValuesButton = new Button()
            {
                Content = "Display config values & comments",
                Padding = new Thickness(20),
                Cursor = System.Windows.Input.Cursors.Hand,
                Margin = new Thickness(10, 10, 10, 10),
                Visibility = _configValueLayer.Opacity == 0
                ? Visibility.Collapsed
                : Visibility.Visible,
            };

            var showCommentsButton = new Button()
            {
                Content = "Display comments",
                Cursor = System.Windows.Input.Cursors.Hand,
                Padding = new Thickness(20),
                Margin = new Thickness(10, 10, 25, 10),
                Visibility = _commentLayer.Opacity == 0
                ? Visibility.Collapsed
                : Visibility.Visible
            };

            buttonsPanel.Children.Add(showConfigValuesButton);
            buttonsPanel.Children.Add(showCommentsButton);

            showConfigValuesButton.Click +=
                (object sender,
                RoutedEventArgs e) =>
                {
                    _configValueLayer.Opacity = 0; // Flip opacity
                    _commentLayer.Opacity = 0; // Flip opacity
                    _buttonsLayer.Opacity = 0;
                    showConfigValuesButton.Visibility = Visibility.Collapsed;
                    showCommentsButton.Visibility = Visibility.Collapsed;
                };

            showCommentsButton.Click +=
                (object sender,
                RoutedEventArgs e) =>
                {
                    _commentLayer.Opacity = 0; // Flip opacity
                    showCommentsButton.Visibility = Visibility.Collapsed;
                };

            return buttonsPanel;
        }

        internal void OnLayoutChanged(object sender, TextViewLayoutChangedEventArgs e)
        {
            var contentsLength = _view.TextSnapshot.Length;

            try
            {
                if (!ValuesAreHidden && _currentContentsHash != 0)
                {
                    return;
                }

                // Adjust button layer position
                if (_view.ViewportRight != 0 && _buttonsPanel.ActualWidth != 0)
                {
                    _buttonsPanel.Margin = new Thickness(
                        _view.ViewportWidth - _buttonsPanel.ActualWidth,
                        _view.ViewportHeight - _buttonsPanel.ActualHeight,
                        _view.ViewportWidth,
                        _view.ViewportHeight);
                }

                // HUGE files are a really bad experience right now. If it's over ½ mb, skip it.
                // TODO: Revisit this.
                if (contentsLength > 500_000)
                {
                    throw new Exception("File is too big");
                }

                // Identify file renderer and select the value locator
                IValueLocator locator = null;
                switch (_view.TextSnapshot.ContentType.TypeName.ToUpper())
                {
                    case "JSON":
                        locator = new JsonValueLocator();
                        break;
                    case "XML":
                        locator = new RegExXmlValueLocator();
                        break;
                    default:
                        return;
                }

                lock (_locatorLock)
                {
                    var contents = _view.TextSnapshot.GetText();
                    if (contents.GetHashCode() != _currentContentsHash)
                    {
                        // Locate the config values because the contents has changed
                        _configValuePositions = locator.FindConfigValues(contents).ToList();
                    }

                    HideByIndexes(_configValuePositions);

                    // Only update the current hash if we successfully hid the values
                    _currentContentsHash = contents.GetHashCode();
                }
            }
            catch (Exception ex)
            {
                // Hide everything if we encounter errors while hiding
                System.Diagnostics.Debug.WriteLine(ex.Message);
                HideByIndexes(new[] { new ConfigPosition(0, contentsLength, ConfigType.Value, ContentsType.Null, "ERROR, cannot parse") }.ToList());
            }
        }

        internal void HideByIndexes(List<ConfigPosition> positions)
        {
            Clear();

            var pos = positions.Where(p => p.StartIndex != p.EndIndex).GroupBy(p => p.StartIndex).Select(p => p.First()).ToList();
            foreach (var p in pos)
            {
                HideData(p.StartIndex, p.EndIndex, p.Type);
            }
        }

        private void Clear()
        {
            _configValueLayer.RemoveAllAdornments();
            _commentLayer.RemoveAllAdornments();
        }

        private void HideData(int startOffset, int stopOffset, ConfigType type)
        {
            IWpfTextViewLineCollection textViewLines = _view.TextViewLines;
            var span = new SnapshotSpan(_view.TextSnapshot, Span.FromBounds(startOffset, stopOffset));
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
                else if (type != ConfigType.Name)
                {
                    throw new ArgumentException($"{type} is not supported.");
                }
            }
        }
    }
}

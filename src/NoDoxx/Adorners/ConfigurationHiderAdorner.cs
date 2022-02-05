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
        private readonly IAdornmentLayer _configValueLayer;
        private readonly IAdornmentLayer _commentLayer;
        private readonly IWpfTextView _view;
        private readonly Brush _brush;
        private readonly Pen _pen;

        public ConfigurationHiderAdorner(IWpfTextView view)
        {
            if (view == null)
            {
                throw new ArgumentNullException("view");
            }

            _configValueLayer = view.GetAdornmentLayer("ConfigurationHiderAdorner");
            _commentLayer = view.GetAdornmentLayer("ConfigurationHiderCommentAdorner");

            _view = view;

            // Create the pen and brush to color the box hiding the config values
            _brush = _view.Background;
            var outlineBrush = new SolidColorBrush(Colors.LightBlue);
            _pen = new Pen(outlineBrush, 0.5);
            _pen.Freeze();

            _view.LayoutChanged += OnLayoutChanged;
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
                //HideByIndexes(new[] { new ConfigPosition(0, contents.Length) });
            }
        }

        internal void HideByIndexes(IEnumerable<ConfigPosition> positions)
        {
            var pos = positions.Where(p=>p.StartIndex != p.EndIndex).GroupBy(p => p.StartIndex).Select(p => p.First()).ToList();
            Clear();
            foreach (var p in pos)
            {
                HideData(p.StartIndex, p.EndIndex, p.Type);
            }

            var showConfigValuesButton = new Button()
            {
                Content = "Display config values",
                Cursor = System.Windows.Input.Cursors.Hand,
                Width = 150,
                Height = 30
            };

            showConfigValuesButton.Click +=
                (object sender,
                System.Windows.RoutedEventArgs e) =>
                {
                    _configValueLayer.Opacity = _configValueLayer.Opacity == 1 ? 0 : 1; // Flip opacity
                    };

            var showCommentsButton = new Button()
            {
                Content = "Display comments",
                Cursor = System.Windows.Input.Cursors.Hand,
                Width = 150,
                Height = 30,
                Margin = new System.Windows.Thickness(0, 100, 0, 0)
            };

            showCommentsButton.Click +=
                (object sender,
                System.Windows.RoutedEventArgs e) =>
                {
                    _commentLayer.Opacity = _commentLayer.Opacity == 1 ? 0 : 1; // Flip opacity
                    };

            Canvas.SetTop(showConfigValuesButton, _configValueLayer.TextView.ViewportTop);
            Canvas.SetTop(showCommentsButton, _commentLayer.TextView.ViewportTop);

            _configValueLayer.AddAdornment(AdornmentPositioningBehavior.OwnerControlled, null, null, showConfigValuesButton, null);

            _commentLayer.AddAdornment(AdornmentPositioningBehavior.OwnerControlled, null, null, showCommentsButton, null);
        }

        private void Clear()
        {
            _configValueLayer.RemoveAllAdornments();
            _commentLayer.RemoveAllAdornments();
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
                } else if(type == ConfigType.Comment)
                {
                    _commentLayer.AddAdornment(AdornmentPositioningBehavior.TextRelative, span, null, image, null);
                } else
                {
                    throw new ArgumentException($"{type} is not supported.");
                }

            }
        }
    }
}

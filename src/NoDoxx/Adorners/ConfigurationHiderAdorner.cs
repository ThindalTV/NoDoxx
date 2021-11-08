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
        private readonly IAdornmentLayer _layer;
        private readonly IWpfTextView _view;
        private readonly Brush _brush;
        private readonly Pen _pen;

        public ConfigurationHiderAdorner(IWpfTextView view)
        {
            if (view == null)
            {
                throw new ArgumentNullException("view");
            }

            _layer = view.GetAdornmentLayer("ConfigurationHiderAdorner");

            _view = view;

            // Create the pen and brush to color the box behind the a's
            _brush = new SolidColorBrush(Colors.Blue);
            _brush.Freeze();


            var penBrush = new SolidColorBrush(Colors.Blue);
            penBrush.Freeze();
            _pen = new Pen(penBrush, 0.5);
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
            HideByIndexes(locator.FindConfigValues(contents));
        }

        internal void HideByIndexes(IEnumerable<ConfigPosition> positions)
        {
            var pos = positions.GroupBy(p => p.StartIndex).Select(p => p.First()).ToList();

            foreach( var p in pos)
            {
                HideData(p.StartIndex, p.EndIndex);
            }
        }

        private void HideData(int startOffset, int stopOffset)
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

                _layer.AddAdornment(AdornmentPositioningBehavior.TextRelative, span, null, image, null);
            }
        }
    }
}

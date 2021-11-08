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

        /// <summary>
        /// Initializes a new instance of the <see cref="ConfigurationHiderAdorner"/> class.
        /// </summary>
        /// <param name="view">Text view to create the adornment for</param>
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

        /// <summary>
        /// Handles whenever the text displayed in the view changes by adding the adornment to any reformatted lines
        /// </summary>
        /// <remarks><para>This event is raised whenever the rendered text displayed in the <see cref="ITextView"/> changes.</para>
        /// <para>It is raised whenever the view does a layout (which happens when DisplayTextLineContainingBufferPosition is called or in response to text or classification changes).</para>
        /// <para>It is also raised whenever the view scrolls horizontally or when its size changes.</para>
        /// </remarks>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event arguments.</param>
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
            // Loop through each character, and place a box around any 'a'
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

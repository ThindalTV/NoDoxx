using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using System;
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

            if (type == "JSON")
            {
                var contents = _view.TextSnapshot.GetText();
                HideJson(contents);

            }
            else if (type == "XML")
            {
                var contents = _view.TextSnapshot.GetText();
                HideXml(contents);
            }

            /*foreach (ITextViewLine line in e.NewOrReformattedLines)
            {
                CreateVisuals(line);
            }*/
        }

        #region XML parsing
        private void HideXml(string fullContents, string contents = null, int contentsStartIndex = 0)
        {
            if (contents == null) contents = fullContents;

            if (String.IsNullOrWhiteSpace(contents)) return;

            var xmlDocument = new System.Xml.XmlDocument();
            xmlDocument.LoadXml(contents);

            if (xmlDocument[xmlDocument.DocumentElement.Name].Attributes != null &&
                xmlDocument[xmlDocument.DocumentElement.Name].Attributes.Count > 0)
            {
                foreach (var attr in xmlDocument[xmlDocument.DocumentElement.Name].Attributes)
                {
                    ;
                }
            }

            foreach (var tag in xmlDocument.ChildNodes)
            {
                var xTag = tag as System.Xml.XmlElement; // Or if child element is content(pure text), hide it right away
                if (xTag != null)
                {
                    HideXml(fullContents, xTag.InnerXml, 0);
                }
            }
        }
        #endregion

        #region JSON parsing
        private void HideJson(string fullJson, string json = null, int jsonStartIndex = 0)
        {
            if (json == null) json = fullJson;

            System.Text.Json.JsonDocument jsonObject;
            try
            {
                jsonObject = System.Text.Json.JsonDocument.Parse(json);
            }
            catch (Exception)
            {
                var start = fullJson.IndexOf(json);
                var end = start + json.Length;
                HideData(start, end);
                return;
            }

            var obj = jsonObject.RootElement.EnumerateObject();
            foreach (var o in obj)
            {
                if (o.Value.ValueKind == System.Text.Json.JsonValueKind.Object)
                {
                    var objString = o.Value.ToString();
                    HideJson(fullJson, objString, fullJson.IndexOf(objString));
                    continue;
                }

                if (o.Value.ValueKind == System.Text.Json.JsonValueKind.Array)
                {
                    var objString = o.Value.ToString();
                    foreach (var arrayItem in o.Value.EnumerateArray())
                    {
                        HideJson(fullJson, arrayItem.ToString(), fullJson.IndexOf(objString));
                    }
                    continue;
                }

                // Get value part of property
                int index = jsonStartIndex;
                var propertyText = o.ToString();
                while ((index = fullJson.IndexOf(propertyText, index + 1)) > 0)
                {
                    var caseInsensitive = o.Value.ValueKind == System.Text.Json.JsonValueKind.True || o.Value.ValueKind == System.Text.Json.JsonValueKind.False;
                    var valuePartIndex = fullJson.IndexOf(o.Value.ToString(), index + 2, caseInsensitive ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal);
                    var endIndex = index + propertyText.Length;

                    // For strings it doesn't catch the trailing " because it only works with the value.
                    int stringPad = o.Value.ValueKind == System.Text.Json.JsonValueKind.String ? -1 : 0;

                    HideData(valuePartIndex, endIndex + stringPad);
                }
            }
        }
        #endregion

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

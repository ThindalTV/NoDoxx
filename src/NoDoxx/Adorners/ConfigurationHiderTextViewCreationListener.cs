using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Utilities;
using System.ComponentModel.Composition;

namespace NoDoxx.Adorners
{
    [Export(typeof(IWpfTextViewCreationListener))]
    [ContentType("XML")]
    [ContentType("JSON")]
    [TextViewRole(PredefinedTextViewRoles.Document)]
    internal sealed class ConfigurationHiderTextViewCreationListener : IWpfTextViewCreationListener
    {
        // Disable "Field is never assigned to..." and "Field is never used" compiler's warnings. Justification: the field is used by MEF.
#pragma warning disable 649, 169


        [Export(typeof(AdornmentLayerDefinition))]
        [Name("ConfigurationHiderValuesAdorner")]
        [Order(After = PredefinedAdornmentLayers.Text)]
        [TextViewRole(PredefinedTextViewRoles.Structured)]
        private AdornmentLayerDefinition valuesAdornmentLayer;

        [Export(typeof(AdornmentLayerDefinition))]
        [Name("ConfigurationHiderCommentsAdorner")]
        [Order(After = PredefinedAdornmentLayers.Text)]
        [TextViewRole(PredefinedTextViewRoles.Structured)]
        private AdornmentLayerDefinition commentsAdornmentLayer;

        [Export(typeof(AdornmentLayerDefinition))]
        [Name("ConfigurationHiderButtonsAdorner")]
        [Order(After = "ConfigurationHiderCommentsAdorner")]
        [TextViewRole(PredefinedTextViewRoles.Structured)]
        private AdornmentLayerDefinition buttonsAdornmentLayer;



#pragma warning restore 649, 169

        #region IWpfTextViewCreationListener

        /// <summary>
        /// Called when a text view having matching roles is created over a text data model having a matching content type.
        /// Instantiates a ConfigurationHider manager when the textView is created.
        /// </summary>
        /// <param name="textView">The <see cref="IWpfTextView"/> upon which the adornment should be placed</param>
        public void TextViewCreated(IWpfTextView textView)
        {
            // The adornment will listen to any event that changes the layout (text changes, scrolling, etc)
            new ConfigurationHiderAdorner(textView);
        }

        #endregion
    }
}

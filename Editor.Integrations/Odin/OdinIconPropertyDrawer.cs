using Sirenix.OdinInspector.Editor;
using UnityEngine;
using UnityEditor;

namespace TriInspector.Editor.Integrations.Odin
{
    /// <summary>
    /// A custom Odin drawer that inherits from OdinValueDrawer<T> to handle the IconAttribute
    /// on any property. It modifies the property's label to include the specified icon.
    /// </summary>
    [DrawerPriority(0.0, 10001.0, 0.1)]
    public class OdinIconPropertyDrawer : OdinAttributeDrawer<IconPropertyAttribute>
    {
        private Texture2D _iconTexture;
        private bool _isInitialized = false; // Flag to ensure icon loading happens only once per drawer instance.

        /// <summary>
        /// Draws the property layout, modifying its label to include the custom icon.
        /// </summary>
        /// <param name="label">The original GUI content label for the property.</param>
        protected override void DrawPropertyLayout(GUIContent label)
        {
            // Load the icon texture only once when the drawer is initialized for this property.
            if (!_isInitialized)
            {
                if (Attribute != null)
                {
                    _iconTexture = TriIconClassDrawUtility.LoadIconTexture(Attribute.IconPath, Attribute.SourceType);
                }
                _isInitialized = true;
            }

            // Create a new GUIContent based on the original label,
            // so we don't modify the original label object directly if it's reused.
            GUIContent customLabel = new GUIContent(label);

            // If an icon texture is loaded, assign it to the custom label.
            if (_iconTexture != null)
            {
                customLabel.image = _iconTexture;
            }

            CallNextDrawer(label);
        }
    }
}

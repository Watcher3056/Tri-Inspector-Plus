#if UNITY_EDITOR
using System;
using System.Reflection;
using TriInspector;
using UnityEditor;
using UnityEngine;

[CustomPropertyDrawer(typeof(TriInspector.IconPropertyAttribute))]
public class IconPropertyAttributeDrawer : PropertyDrawer
{
    private Texture2D _iconTexture;
    private TriInspector.IconPropertyAttribute _iconAttribute;

    /// <summary>
    /// Called when the Inspector GUI is drawn for a property with this drawer.
    /// This method is responsible for drawing the property's label and field,
    /// and applying the custom icon.
    /// </summary>
    /// <param name="position">The rectangle on the screen to draw the property within.</param>
    /// <param name="property">The SerializedProperty instance for the property being drawn.</param>
    /// <param name="label">The GUIContent for the property's label.</param>
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        // Get the IconAttribute instance applied to this property.
        // The 'attribute' field of PropertyDrawer gives direct access to the attribute.
        _iconAttribute = (TriInspector.IconPropertyAttribute) attribute;

        // Ensure the icon texture is loaded. Load it once per drawer instance.
        if (_iconTexture == null && _iconAttribute != null)
        {
            _iconTexture = TriIconClassDrawUtility.LoadIconTexture(_iconAttribute.IconPath, _iconAttribute.SourceType);
        }

        // Create a new GUIContent based on the original label.
        GUIContent customLabel = new GUIContent(label);

        // If an icon texture is loaded, assign it to the custom label.
        if (_iconTexture != null)
        {
            customLabel.image = _iconTexture;
        }

        // Begin a property scope to handle indentation and Undo/Redo.
        // This is crucial for proper property drawing in the Inspector.
        EditorGUI.BeginProperty(position, customLabel, property);

        // Draw the property field using Unity's standard drawing method.
        // Pass the customLabel with the icon.
        // includeChildren: true ensures that if the property is a complex type (e.g., a custom class),
        // its internal fields are also drawn.
        EditorGUI.PropertyField(position, property, customLabel, includeChildren: true);

        // If the property represents a MonoBehaviour or ScriptableObject (the root object being inspected),
        // we can also attempt to set its icon in the Project window and Inspector header.
        // This is distinct from the label icon but addresses the original user request for class icons.
        if (property.serializedObject.targetObject != null && _iconTexture != null)
        {
            // Only set the icon for the main object being inspected, not for nested properties.
            // This condition is heuristic; a more robust solution for asset icons is ClassIconSetter.cs.
            if (property.depth == 0) // Check if this is a root-level property
            {
                // Check if the IconAttribute is applied directly to the class/asset
                // This ensures we're only setting the asset icon if the attribute is intended for it.
                // We specifically check the type of the targetObject.
                Type targetType = property.serializedObject.targetObject.GetType();
                if (targetType.GetCustomAttribute<TriInspector.IconPropertyAttribute>() == _iconAttribute)
                {
                    EditorGUIUtility.SetIconForObject(property.serializedObject.targetObject, _iconTexture);
                }
            }
        }


        // End the property scope.
        EditorGUI.EndProperty();
    }

    /// <summary>
    /// Calculates the height required to draw the property.
    /// </summary>
    /// <param name="property">The SerializedProperty instance for the property being drawn.</param>
    /// <param name="label">The GUIContent for the property's label.</param>
    /// <returns>The height needed for the property, including its children if expanded.</returns>
    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        // Calculate the height needed for the property, including its children if it's a complex type and expanded.
        return EditorGUI.GetPropertyHeight(property, label, includeChildren: true);
    }
}
#endif

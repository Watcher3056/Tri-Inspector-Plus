using System;
using UnityEngine;

namespace TriInspector
{
    /// <summary>
    /// Attribute to assign a custom icon to a property's label in the TriInspector.
    /// </summary>
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
    public class IconPropertyAttribute : PropertyAttribute // Changed base class
    {
        public string IconPath { get; private set; }
        public IconSourceType SourceType { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="IconPropertyAttribute"/> class.
        /// </summary>
        /// <param name="iconPath">The path or name of the icon texture.</param>
        /// <param name="sourceType">The source type of the icon (Resources or EditorResources).</param>
        public IconPropertyAttribute(string iconPath, IconSourceType sourceType = IconSourceType.Resources)
        {
            IconPath = iconPath;
            SourceType = sourceType;
        }
    }

    /// <summary>
    /// Specifies the source type for loading an icon.
    /// </summary>
    public enum IconSourceType
    {
        /// <summary>
        /// Load the icon from a Resources folder (e.g., "Assets/Resources/MyIcon.png" -> "MyIcon").
        /// </summary>
        Resources,
        /// <summary>
        /// Load the icon from Unity Editor's internal resources or Editor Default Resources folder.
        /// (e.g., "d_GameObject Icon", "Assets/Editor Default Resources/MyCustomEditorIcon.png").
        /// </summary>
        EditorResources
    }
}
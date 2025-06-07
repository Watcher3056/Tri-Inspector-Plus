using System;
using UnityEngine;

namespace TriInspector
{
    /// <summary>
    /// Attribute to assign a custom icon to a property's label in the TriInspector.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
    public class IconClassAttribute : Attribute
    {
        public string IconPath { get; private set; }
        public IconSourceType SourceType { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="IconPropertyAttribute"/> class.
        /// </summary>
        /// <param name="iconPath">The path or name of the icon texture.</param>
        /// <param name="sourceType">The source type of the icon (Resources or EditorResources).</param>
        public IconClassAttribute(string iconPath, IconSourceType sourceType = IconSourceType.Resources)
        {
            IconPath = iconPath;
            SourceType = sourceType;
        }
    }
}
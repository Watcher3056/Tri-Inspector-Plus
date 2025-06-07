using System;
using System.Reflection;
using Sirenix.Utilities;
using Sirenix.OdinInspector.Editor;
using UnityEngine;

namespace TriInspector.Editor.Integrations.Odin
{
    /// <summary>
    /// A specialized Odin drawer that extends OdinObjectDrawer to handle the IconAttribute
    /// on MonoBehaviour and ScriptableObject classes. It sets the object's icon in the
    /// Project window and Inspector header if the class is decorated with IconAttribute.
    /// This drawer ensures the base OdinObjectDrawer's drawing logic is still executed.
    /// </summary>
    [DrawerPriority(0.0, 10001.0, 0.5)]
    public class IconOdinObjectDrawer<T> : OdinValueDrawer<T>
        where T : UnityEngine.Object
    {
        /// <summary>
        /// Determines if this drawer can draw the given type.
        /// This drawer only applies if the type has the IconAttribute applied to its class.
        /// </summary>
        /// <param name="type">The type to check.</param>
        /// <returns>True if this drawer can handle the type; otherwise, false.</returns>
        public override bool CanDrawTypeFilter(Type type)
        {
            // First, ensure the base OdinObjectDrawer would consider drawing this type.
            // This is important to maintain the existing filtering logic of the base class.
            if (type == null)
            {
                return false;
            }

            // Additionally, this specific drawer only applies if the class itself
            // has the IconAttribute attached.
            // This prevents it from drawing for all UnityEngine.Object types.

            IconClassAttribute iconAttribute = type.GetCustomAttribute<IconClassAttribute>(inherit: true);
            return iconAttribute != null;
        }

        /// <summary>
        /// Draws the property layout for the object, applying the custom icon if the
        /// IconAttribute is present on the class, and then executing the base Odin drawing.
        /// </summary>
        /// <param name="label">The GUI content label for the property.</param>
        protected override void DrawPropertyLayout(GUIContent label)
        {
            // Get the actual MonoBehaviour/ScriptableObject instance being drawn by this OdinObjectDrawer.
            UnityEngine.Object targetObject = ValueEntry.SmartValue;

            TriIconClassDrawUtility.TryDrawIconForObject(targetObject);

            CallNextDrawer(label);
        }
    }
}

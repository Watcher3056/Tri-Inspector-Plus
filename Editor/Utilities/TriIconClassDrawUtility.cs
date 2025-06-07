using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace TriInspector
{
    public static class TriIconClassDrawUtility
    {
        public static void TryDrawIconForObject(UnityEngine.Object obj)
        {
            if (obj == null)
                return;

            Type targetType = obj.GetType();
            IconClassAttribute iconAttribute = targetType.GetCustomAttribute<IconClassAttribute>(inherit: true);

            if (iconAttribute != null)
            {
                // Load the icon texture using the helper method.
                Texture2D iconTexture = LoadIconTexture(iconAttribute.IconPath, iconAttribute.SourceType);
                if (iconTexture != null)
                {
                    // Set the icon for the target Unity.Object asset itself.
                    // This affects its display in the Project window and the Inspector header.
                    EditorGUIUtility.SetIconForObject(obj, iconTexture);
                    // Debug.Log($"[IconedOdinObjectDrawer] Set class icon for '{targetObject.name}' using '{iconAttribute.IconPath}'.");
                }
                else
                {
                    // Resetting to null usually works, but sometimes Unity needs a full refresh.
                    EditorGUIUtility.SetIconForObject(obj, null);
                }
            }
        }

        /// <summary>
        /// Helper method to load a Texture2D from Resources or Editor Resources.
        /// This method is duplicated here for self-containment of this drawer's logic.
        /// </summary>
        /// <param name="iconPath">The path or name of the icon texture.</param>
        /// <param name="sourceType">The source type of the icon.</param>
        /// <returns>The loaded Texture2D, or null if not found.</returns>
        public static Texture2D LoadIconTexture(string iconPath, IconSourceType sourceType)
        {
            if (string.IsNullOrEmpty(iconPath))
            {
                return null;
            }

            Texture2D loadedTexture = null;
            switch (sourceType)
            {
                case IconSourceType.Resources:
                    loadedTexture = Resources.Load<Texture2D>(iconPath);
                    break;
                case IconSourceType.EditorResources:
                    loadedTexture = EditorGUIUtility.Load(iconPath) as Texture2D;
                    break;
                default:
                    break;
            }

            if (loadedTexture == null)
            {
                Debug.LogWarning($"[IconedOdinObjectDrawer] Failed to load icon at path '{iconPath}' from source '{sourceType}'. " +
                                 "Ensure the path is correct and the asset is in a 'Resources' folder (for Resources) or " +
                                 "'Editor Default Resources' (for EditorResources asset paths), or is a valid internal Unity icon name.");
            }
            return loadedTexture;
        }
    }
}

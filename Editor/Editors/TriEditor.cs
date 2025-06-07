using System;
using System.Reflection; // Required for Reflection
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements; // For VisualElement, if used

namespace TriInspector.Editors
{
    public abstract class TriEditor : Editor
    {
        private TriEditorCore _core;

        protected virtual void OnEnable()
        {
            _core = new TriEditorCore(this);

            // --- Custom Icon Attribute Handling for the Class ---
            if (target != null)
            {
                Type targetType = target.GetType();
                IconClassAttribute iconAttribute = targetType.GetCustomAttribute<IconClassAttribute>();

                if (iconAttribute != null)
                {
                    Texture2D iconTexture = LoadIconTexture(iconAttribute.IconPath, iconAttribute.SourceType);
                    if (iconTexture != null)
                    {
                        // Set the icon for the target Unity.Object asset itself
                        EditorGUIUtility.SetIconForObject(target, iconTexture);
                        // Debug.Log($"[TriEditor] Set class icon for '{target.name}' using '{iconAttribute.IconPath}'.");
                    }
                }
                // Optional: If you want to clear a previously set icon when the attribute is removed
                // else
                // {
                //    // Resetting to null usually works, but sometimes Unity needs a full refresh.
                //    EditorGUIUtility.SetIconForObject(target, null);
                // }
            }
            // --- End Custom Icon Attribute Handling ---
        }

        protected virtual void OnDisable()
        {
            _core.Dispose();
        }

        public override void OnInspectorGUI()
        {
            _core.OnInspectorGUI();
        }

        public override VisualElement CreateInspectorGUI()
        {
            return _core.CreateVisualElement();
        }

        /// <summary>
        /// Helper method to load a Texture2D from Resources or Editor Resources.
        /// Duplicated here to make this class self-contained for icon loading,
        /// but in a real project, this might be a shared utility.
        /// </summary>
        /// <param name="iconPath">The path or name of the icon texture.</param>
        /// <param name="sourceType">The source type of the icon.</param>
        /// <returns>The loaded Texture2D, or null if not found.</returns>
        private static Texture2D LoadIconTexture(string iconPath, IconSourceType sourceType)
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
                Debug.LogWarning($"[TriEditor] Failed to load icon at path '{iconPath}' from source '{sourceType}'. " +
                                 "Ensure the path is correct and the asset is in a 'Resources' folder (for Resources) or " +
                                 "'Editor Default Resources' (for EditorResources asset paths), or is a valid internal Unity icon name.");
            }
            return loadedTexture;
        }
    }
}

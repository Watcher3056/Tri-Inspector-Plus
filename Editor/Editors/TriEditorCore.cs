using System.Collections.Generic;
using TriInspector.Utilities;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace TriInspector.Editors
{
    public class TriEditorCore
    {
        internal static readonly Dictionary<TriPropertyTree, VisualElement> UiElementsRoots
            = new Dictionary<TriPropertyTree, VisualElement>();

        private readonly Editor _editor;

        private TriPropertyTreeForSerializedObject _inspector;

        public TriEditorCore(Editor editor)
        {
            _editor = editor;
        }

        public void Enable()
        {
            TriIconClassDrawUtility.TryDrawIconForObject(_editor.target);
        }

        public void Dispose()
        {
            if (_inspector != null)
            {
                UiElementsRoots.Remove(_inspector);

                _inspector.Dispose();
            }

            _inspector = null;
        }

        public void OnInspectorGUI(VisualElement visualRoot = null)
        {
            var serializedObject = _editor.serializedObject;

            if (serializedObject.targetObjects.Length == 0)
            {
                return;
            }

            if (serializedObject.targetObject == null)
            {
                EditorGUILayout.HelpBox("Script is missing", MessageType.Warning);
                return;
            }

            foreach (var targetObject in serializedObject.targetObjects)
            {
                if (TriGuiHelper.IsEditorTargetPushed(targetObject))
                {
                    GUILayout.Label("Recursive inline editors not supported");
                    return;
                }
            }

            if (_inspector == null)
            {
                _inspector = new TriPropertyTreeForSerializedObject(serializedObject);
            }

            if (visualRoot != null)
            {
                UiElementsRoots[_inspector] = visualRoot;
            }

            serializedObject.UpdateIfRequiredOrScript();

            _inspector.Update();
            _inspector.RunValidationIfRequired();

            EditorGUIUtility.hierarchyMode = false;

            using (TriGuiHelper.PushEditorTarget(serializedObject.targetObject))
            {
                _inspector.Draw();
            }

            if (serializedObject.ApplyModifiedProperties())
            {
                _inspector.RequestValidation();
            }

            if (_inspector.RepaintRequired)
            {
                _editor.Repaint();
            }
        }

        public VisualElement CreateVisualElement()
        {
            var container = new VisualElement();
            var root = new VisualElement()
            {
                style =
                {
                    position = Position.Absolute,
                },
            };

            container.Add(new IMGUIContainer(() =>
            {
                const float labelExtraPadding = 2;
                const float labelWidthRatio = 0.45f;
                const float labelMinWidth = 120;

                var space = container.resolvedStyle.left + container.resolvedStyle.right + labelExtraPadding;

                EditorGUIUtility.wideMode = true;
                EditorGUIUtility.hierarchyMode = false;
                EditorGUIUtility.labelWidth = Mathf.Max(labelMinWidth,
                    container.resolvedStyle.width * labelWidthRatio - space);

                GUILayout.BeginVertical(Styles.RootLayout);
                OnInspectorGUI(root);
                GUILayout.EndVertical();
            })
            {
                style =
                {
                    marginLeft = -Styles.RootMarginLeft,
                    marginRight = -Styles.RootMarginRight,
                },
            });

            container.Add(root);

            return container;
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

        private static class Styles
        {
            public const int RootMarginLeft = 15;
            public const int RootMarginRight = 6;

            public static readonly GUIStyle RootLayout = new GUIStyle
            {
                padding = new RectOffset(RootMarginLeft, RootMarginRight, 0, 0),
            };
        }
    }
}
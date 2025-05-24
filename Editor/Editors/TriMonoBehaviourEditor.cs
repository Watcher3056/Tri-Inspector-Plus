using UnityEditor;
using UnityEngine;
using System.Reflection;

namespace TriInspector.Editors
{
    [CanEditMultipleObjects]
    [CustomEditor(typeof(MonoBehaviour), editorForChildClasses: true, isFallback = true)]
    internal sealed class TriMonoBehaviourEditor : TriEditor
    {
        private Component _component;
        private GameObject _go;

        protected override void OnEnable()
        {
            base.OnEnable();

            _component = target as Component;
            _go = _component.gameObject;
        }

        protected override void OnDisable()
        {
            base.OnDisable();

            if (_component == null)
            {
                if (_go == null) return;

                // Skip if this GameObject is part of an active runtime scene during play
                if (Application.isPlaying && _go.scene.IsValid() && _go.scene.isLoaded)
                    return;

                var behaviours = _go.GetComponents<MonoBehaviour>();
                var method = typeof(MonoBehaviour).GetMethod("OnValidate", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);

                foreach (var b in behaviours)
                {
                    if (b == null) continue;

                    var customMethod = b.GetType().GetMethod("OnValidate", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.DeclaredOnly);

                    if (customMethod != null)
                    {
                        customMethod.Invoke(b, null);
                        Debug.Log($"[EasyCS] Invoked OnValidate() on {b.GetType().Name}", b);
                    }
                }
            }
        }
    }
}
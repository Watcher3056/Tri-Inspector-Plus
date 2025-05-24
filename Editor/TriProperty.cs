using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using JetBrains.Annotations;
using TriInspector.Utilities;
using UnityEditor;
using UnityEngine;

namespace TriInspector
{
    public sealed class TriProperty
    {
        private static readonly StringBuilder SharedPropertyPathStringBuilder = new StringBuilder();

        private static readonly IReadOnlyList<TriValidationResult> EmptyValidationResults =
            new List<TriValidationResult>();

        // Added a static readonly empty list for children properties to avoid returning null
        private static readonly IReadOnlyList<TriProperty> EmptyChildrenProperties = new List<TriProperty>();
        private const int DefaultMaxDrawingDepth = 15;

        private readonly TriPropertyDefinition _definition;
        private readonly int _propertyIndex;
        [CanBeNull] private readonly SerializedObject _serializedObject;
        [CanBeNull] private readonly SerializedProperty _serializedProperty;
        private List<TriProperty> _childrenProperties; // This can now be null, but the public getter will handle it
        private List<TriValidationResult> _validationResults;

        private GUIContent _displayNameBackingField;
        private string _propertyPath;
        private string _isExpandedPrefsKey;

        private int _lastUpdateFrame;
        private bool _isUpdating;

        [CanBeNull] private object _value;
        [CanBeNull] private Type _valueType;
        private bool _isValueMixed;

        public event Action<TriProperty> ValueChanged;
        public event Action<TriProperty> ChildValueChanged;

        // Added Depth property to track the current recursion level
        public int Depth { get; }

        internal TriProperty(
            TriPropertyTree propertyTree,
            TriProperty parent,
            TriPropertyDefinition definition,
            SerializedObject serializedObject
        )
        {
            Parent = parent;
            _definition = definition;
            _propertyIndex = -1;
            _serializedProperty = null;
            _serializedObject = serializedObject;

            PropertyTree = propertyTree;
            PropertyType = GetPropertyType(this);
            // Initialize Depth: Root property has depth 0, its children 1, and so on.
            Depth = (parent?.Depth ?? -1) + 1;
        }

        internal TriProperty(
            TriPropertyTree propertyTree,
            TriProperty parent,
            TriPropertyDefinition definition,
            int propertyIndex,
            [CanBeNull] SerializedProperty serializedProperty)
        {
            Parent = parent;
            _definition = definition;
            _propertyIndex = propertyIndex;
            _serializedProperty = serializedProperty?.Copy();
            _serializedObject = _serializedProperty?.serializedObject;

            PropertyTree = propertyTree;
            PropertyType = GetPropertyType(this);
            // Initialize Depth: Array elements and other children increment depth.
            Depth = (parent?.Depth ?? -1) + 1;
        }

        internal TriPropertyDefinition Definition => _definition;

        [PublicAPI]
        public TriPropertyType PropertyType { get; }

        [PublicAPI]
        public TriPropertyTree PropertyTree { get; }

        [PublicAPI]
        public TriProperty Parent { get; }

        [PublicAPI]
        public TriProperty Owner => IsArrayElement ? Parent.Owner : Parent;

        [PublicAPI]
        public bool IsRootProperty => Parent == null;

        [PublicAPI]
        public string RawName => _definition.Name;

        [PublicAPI]
        public string DisplayName => DisplayNameContent.text;

        public IEqualityComparer Comparer => TriEqualityComparer.Of(ValueType);

        [PublicAPI]
        public GUIContent DisplayNameContent
        {
            get
            {
                if (TriPropertyOverrideContext.Current != null &&
                    TriPropertyOverrideContext.Current.TryGetDisplayName(this, out var overrideName))
                {
                    return overrideName;
                }

                if (_displayNameBackingField == null)
                {
                    if (TryGetAttribute(out HideLabelAttribute _) || IsArrayElement)
                    {
                        _displayNameBackingField = new GUIContent("");
                    }
                    else
                    {
                        _displayNameBackingField = new GUIContent(ObjectNames.NicifyVariableName(_definition.Name));
                    }
                }

                if (IsArrayElement)
                {
                    if (TriUnityInspectorUtilities.TryGetSpecialArrayElementName(this, out var specialName))
                    {
                        _displayNameBackingField.text = specialName;
                    }
                    else
                    {
                        _displayNameBackingField.text = TriUnityInspectorUtilities.GetStandardArrayElementName(this);
                    }
                }
                else
                {
                    if (_definition.CustomLabel != null)
                    {
                        _displayNameBackingField.text = _definition.CustomLabel.GetValue(this, "");
                    }

                    if (_definition.CustomTooltip != null)
                    {
                        _displayNameBackingField.tooltip = _definition.CustomTooltip.GetValue(this, "");
                    }
                }

                return _displayNameBackingField;
            }
        }

        [PublicAPI]
        public string PropertyPath
        {
            get
            {
                if (_propertyPath == null)
                {
                    SharedPropertyPathStringBuilder.Clear();
                    BuildPropertyPath(this, SharedPropertyPathStringBuilder);
                    _propertyPath = SharedPropertyPathStringBuilder.ToString();
                }

                return _propertyPath;
            }
        }

        [PublicAPI]
        public bool IsVisible
        {
            get
            {
                foreach (var processor in _definition.HideProcessors)
                {
                    if (processor.IsHidden(this))
                    {
                        return false;
                    }
                }

                return true;
            }
        }

        [PublicAPI]
        public bool IsEnabled
        {
            get
            {
                if (_definition.IsReadOnly)
                {
                    return false;
                }

                foreach (var processor in _definition.DisableProcessors)
                {
                    if (processor.IsDisabled(this))
                    {
                        return false;
                    }
                }

                return true;
            }
        }

        [PublicAPI]
        public Type FieldType => _definition.FieldType;

        [PublicAPI]
        public Type ArrayElementType => _definition.ArrayElementType;

        [PublicAPI]
        public bool IsArrayElement => _definition.IsArrayElement;

        [PublicAPI]
        public bool IsArray => _definition.IsArray;

        public int IndexInArray => IsArrayElement
            ? _propertyIndex
            : throw new InvalidOperationException("Cannot read IndexInArray for !IsArrayElement");

        public IReadOnlyList<TriCustomDrawer> AllDrawers => _definition.Drawers;

        internal IReadOnlyList<string> ExtensionErrors => _definition.ExtensionErrors;

        public bool HasValidators => _definition.Validators.Count != 0;

        public IReadOnlyList<TriValidationResult> ValidationResults =>
            _validationResults ?? EmptyValidationResults;

        [PublicAPI]
        public bool IsExpanded
        {
            get
            {
                if (_serializedProperty != null)
                {
                    return _serializedProperty.isExpanded;
                }

                if (_isExpandedPrefsKey == null)
                {
                    _isExpandedPrefsKey = $"TriInspector.expanded.{PropertyPath}";
                }

                return SessionState.GetBool(_isExpandedPrefsKey, false);
            }
            set
            {
                if (IsExpanded == value)
                {
                    return;
                }

                if (_serializedProperty != null)
                {
                    _serializedProperty.isExpanded = value;
                }
                else if (_isExpandedPrefsKey != null)
                {
                    SessionState.SetBool(_isExpandedPrefsKey, value);
                }
            }
        }

        [PublicAPI]
        [CanBeNull]
        public Type ValueType
        {
            get
            {
                if (PropertyType != TriPropertyType.Reference)
                {
                    return _definition.FieldType;
                }

                UpdateIfRequired();
                return _valueType;
            }
        }

        public bool IsValueMixed
        {
            get
            {
                if (PropertyTree.TargetsCount == 1)
                {
                    return false;
                }

                UpdateIfRequired();
                return _isValueMixed;
            }
        }


        [PublicAPI]
        [CanBeNull]
        public object Value
        {
            get
            {
                UpdateIfRequired();
                return _value;
            }
        }

        [PublicAPI]
        public IReadOnlyList<TriProperty> ChildrenProperties
        {
            get
            {
                UpdateIfRequired();

                if (_childrenProperties != null && PropertyType == TriPropertyType.Generic)
                {
                    // Return the actual children list, or an empty one if _childrenProperties is null.
                    return _childrenProperties ?? EmptyChildrenProperties;
                }

                // For other property types (e.g., Primitive, Array), they don't have 'ChildrenProperties'
                // in the same way Generic/Reference types do. Returning an empty list is the safe default.
                return PropertyType == TriPropertyType.Generic || PropertyType == TriPropertyType.Reference
                    ? _childrenProperties : EmptyChildrenProperties;
            }
        }

        [PublicAPI]
        public IReadOnlyList<TriProperty> ArrayElementProperties
        {
            get
            {
                // If the property type is Array, we process and return its array elements.
                // Otherwise, it means this property is not an array, so we return an empty list.
                if (PropertyType == TriPropertyType.Array)
                {
                    // Ensure children (array elements) are updated if needed.
                    UpdateIfRequired();
                    // Return the actual array elements list, or an empty one if _childrenProperties is null.
                    return _childrenProperties ?? EmptyChildrenProperties;
                }

                // For non-array property types, return an empty list.
                return EmptyChildrenProperties;
            }
        }

        [PublicAPI]
        public bool TryGetMemberInfo(out MemberInfo memberInfo)
        {
            return _definition.TryGetMemberInfo(out memberInfo);
        }

        public object GetValue(int targetIndex)
        {
            return _definition.GetValue(this, targetIndex);
        }

        [PublicAPI]
        public void SetValue(object value)
        {
            ModifyAndRecordForUndo(targetIndex => SetValueRecursive(this, value, targetIndex));
        }

        [PublicAPI]
        public void SetValues(Func<int, object> getValue)
        {
            ModifyAndRecordForUndo(targetIndex =>
            {
                var value = getValue.Invoke(targetIndex);
                SetValueRecursive(this, value, targetIndex);
            });
        }

        public void ModifyAndRecordForUndo(Action<int> call)
        {
            PropertyTree.ApplyChanges();

            PropertyTree.ForceCreateUndoGroup();

            for (var targetIndex = 0; targetIndex < PropertyTree.TargetsCount; targetIndex++)
            {
                call.Invoke(targetIndex);
            }

            PropertyTree.Update(forceUpdate: true);

            NotifyValueChanged();

            PropertyTree.RequestValidation();
            PropertyTree.RequestRepaint();
        }

        public void NotifyValueChanged()
        {
            NotifyValueChanged(this);
        }

        private void NotifyValueChanged(TriProperty property)
        {
            if (property == this)
            {
                ValueChanged?.Invoke(property);
            }
            else
            {
                ChildValueChanged?.Invoke(property);
            }

            Parent?.NotifyValueChanged(property);
        }

        private void UpdateIfRequired(bool forceUpdate = false)
        {
            if (_isUpdating)
            {
                // This indicates a recursive call during an update, which should be avoided.
                // It's a safeguard, but the depth limit should prevent most issues.
                throw new InvalidOperationException("Recursive call detected during property update.");
            }

            if (_lastUpdateFrame == PropertyTree.RepaintFrame && !forceUpdate)
            {
                return;
            }

            _isUpdating = true;

            try
            {
                _lastUpdateFrame = PropertyTree.RepaintFrame;

                ReadValue(this, out var newValue, out var newValueIsMixed);

                var newValueType = FieldType.IsValueType ? FieldType
                    : ReferenceEquals(_value, newValue) ? _valueType
                    : newValue?.GetType();
                var valueTypeChanged = _valueType != newValueType;

                _value = newValue;
                _valueType = newValueType;
                _isValueMixed = newValueIsMixed;

                // --- Depth Limit Implementation ---
                // Get the max depth from the attribute on the property definition, or use a default.
                // while still allowing sufficient inspection.
                int effectiveMaxDrawDepth = _definition.CustomMaxDrawDepth ?? DefaultMaxDrawingDepth;

                // If the current depth is greater than or equal to the effective max draw depth,
                // we stop generating children to prevent infinite recursion.
                if (Depth >= effectiveMaxDrawDepth)
                {
                    // Ensure _childrenProperties is initialized as an empty list if it's null,
                    // then clear it. This ensures ChildrenProperties getter always returns a non-null list.
                    if (_childrenProperties == null)
                    {
                        _childrenProperties = new List<TriProperty>();
                    }
                    _childrenProperties.Clear(); // Clear children if depth limit reached
                    return; // Stop further processing for this branch
                }
                // --- End Depth Limit Implementation ---


                switch (PropertyType)
                {
                    case TriPropertyType.Generic:
                    case TriPropertyType.Reference:
                        if (_childrenProperties == null || valueTypeChanged)
                        {
                            if (_childrenProperties == null)
                            {
                                _childrenProperties = new List<TriProperty>();
                            }

                            _childrenProperties.Clear();

                            var selfType = PropertyType == TriPropertyType.Reference ? _valueType : FieldType;
                            if (selfType != null)
                            {
                                var properties = TriTypeDefinition.GetCached(selfType).Properties;
                                for (var index = 0; index < properties.Count; index++)
                                {
                                    var childDefinition = properties[index];
                                    var childSerializedProperty = _serializedProperty != null
                                        ? _serializedProperty.FindPropertyRelative(childDefinition.Name)
                                        : _serializedObject?.FindProperty(childDefinition.Name);
                                    var childProperty = new TriProperty(PropertyTree, this,
                                        childDefinition, index, childSerializedProperty);

                                    _childrenProperties.Add(childProperty);
                                }
                            }
                        }

                        break;

                    case TriPropertyType.Array:
                        if (_childrenProperties == null)
                        {
                            _childrenProperties = new List<TriProperty>();
                        }

                        var listSize = ((IList) newValue)?.Count ?? 0;

                        while (_childrenProperties.Count < listSize)
                        {
                            var index = _childrenProperties.Count;
                            var elementDefinition = _definition.ArrayElementDefinition;
                            var elementSerializedReference = _serializedProperty?.GetArrayElementAtIndex(index);

                            var elementProperty = new TriProperty(PropertyTree, this,
                                elementDefinition, index, elementSerializedReference);

                            _childrenProperties.Add(elementProperty);
                        }

                        while (_childrenProperties.Count > listSize)
                        {
                            _childrenProperties.RemoveAt(_childrenProperties.Count - 1);
                        }

                        break;
                }
            }
            finally
            {
                _isUpdating = false;
            }
        }

        internal void RunValidation()
        {
            UpdateIfRequired();

            if (HasValidators)
            {
                _validationResults = _definition.Validators
                    .Select(it => it.Validate(this))
                    .Where(it => !it.IsValid)
                    .ToList();
            }

            // Only recurse into children if the property type can have children (Generic, Reference, or Array)
            // The ChildrenProperties and ArrayElementProperties getters now safely return empty lists for other types.
            if (PropertyType == TriPropertyType.Generic || PropertyType == TriPropertyType.Reference)
            {
                foreach (var childrenProperty in ChildrenProperties) // Use the public getter
                {
                    childrenProperty.RunValidation();
                }
            }
            else if (PropertyType == TriPropertyType.Array)
            {
                foreach (var arrayElementProperty in ArrayElementProperties) // Use the public getter
                {
                    arrayElementProperty.RunValidation();
                }
            }
        }

        internal void EnumerateValidationResults(Action<TriProperty, TriValidationResult> call)
        {
            UpdateIfRequired();

            if (_validationResults != null)
            {
                foreach (var result in _validationResults)
                {
                    call.Invoke(this, result);
                }
            }

            // Only recurse into children if the property type can have children (Generic, Reference, or Array)
            // The ChildrenProperties and ArrayElementProperties getters now safely return empty lists for other types.
            if (PropertyType == TriPropertyType.Generic || PropertyType == TriPropertyType.Reference)
            {
                foreach (var childrenProperty in ChildrenProperties) // Use the public getter
                {
                    childrenProperty.EnumerateValidationResults(call);
                }
            }
            else if (PropertyType == TriPropertyType.Array)
            {
                foreach (var arrayElementProperty in ArrayElementProperties) // Use the public getter
                {
                    arrayElementProperty.EnumerateValidationResults(call);
                }
            }
        }

        [PublicAPI]
        public bool TryGetSerializedProperty(out SerializedProperty serializedProperty)
        {
            serializedProperty = _serializedProperty;
            return serializedProperty != null;
        }

        [PublicAPI]
        public bool TryGetAttribute<TAttribute>(out TAttribute attribute)
            where TAttribute : Attribute
        {
            if (ValueType != null)
            {
                foreach (var attr in TriReflectionUtilities.GetAttributesCached(ValueType))
                {
                    if (attr is TAttribute typedAttr)
                    {
                        attribute = typedAttr;
                        return true;
                    }
                }
            }

            foreach (var attr in _definition.Attributes)
            {
                if (attr is TAttribute typedAttr)
                {
                    attribute = typedAttr;
                    return true;
                }
            }

            attribute = null;
            return false;
        }

        internal static void BuildPropertyPath(TriProperty property, StringBuilder sb)
        {
            if (property.IsRootProperty)
            {
                return;
            }

            if (property.Parent != null && !property.Parent.IsRootProperty)
            {
                BuildPropertyPath(property.Parent, sb);
                sb.Append('.');
            }

            if (property.IsArrayElement)
            {
                sb.Append("Array.data[").Append(property.IndexInArray).Append(']');
            }
            else
            {
                sb.Append(property.RawName);
            }
        }

        private static void SetValueRecursive(TriProperty property, object value, int targetIndex)
        {
            // for value types we must recursively set all parent objects
            // because we cannot directly modify structs
            // but we can re-set entire parent value
            while (property._definition.SetValue(property, value, targetIndex, out var parentValue) &&
                   property.Parent != null)
            {
                property = property.Parent;
                value = parentValue;
            }
        }

        private static void ReadValue(TriProperty property, out object newValue, out bool isMixed)
        {
            newValue = property.GetValue(0);

            if (property.PropertyTree.TargetsCount == 1)
            {
                isMixed = false;
                return;
            }

            switch (property.PropertyType)
            {
                case TriPropertyType.Array:
                    {
                        var list = (IList) newValue;
                        for (var i = 1; i < property.PropertyTree.TargetsCount; i++)
                        {
                            if (list == null)
                            {
                                break;
                            }

                            var otherList = (IList) property.GetValue(i);
                            if (otherList == null || otherList.Count < list.Count)
                            {
                                newValue = list = otherList;
                            }
                        }

                        isMixed = true;
                        return;
                    }
                case TriPropertyType.Reference:
                    {
                        for (var i = 1; i < property.PropertyTree.TargetsCount; i++)
                        {
                            var otherValue = property.GetValue(i);

                            if (newValue?.GetType() != otherValue?.GetType())
                            {
                                isMixed = true;
                                newValue = null;
                                return;
                            }
                        }

                        isMixed = false;
                        return;
                    }
                case TriPropertyType.Generic:
                    {
                        isMixed = false;
                        return;
                    }
                case TriPropertyType.Primitive:
                    {
                        for (var i = 1; i < property.PropertyTree.TargetsCount; i++)
                        {
                            var otherValue = property.GetValue(i);
                            if (!property.Comparer.Equals(otherValue, newValue))
                            {
                                isMixed = true;
                                return;
                            }
                        }

                        isMixed = false;
                        return;
                    }

                default:
                    {
                        Debug.LogError($"Unexpected property type: {property.PropertyType}");
                        isMixed = true;
                        return;
                    }
            }
        }

        private static TriPropertyType GetPropertyType(TriProperty property)
        {
            if (property._serializedProperty != null)
            {
                if (property._serializedProperty.isArray &&
                    property._serializedProperty.propertyType != SerializedPropertyType.String)
                {
                    return TriPropertyType.Array;
                }

                if (property._serializedProperty.propertyType == SerializedPropertyType.ManagedReference)
                {
                    return TriPropertyType.Reference;
                }

                if (property._serializedProperty.propertyType == SerializedPropertyType.Generic)
                {
                    return TriPropertyType.Generic;
                }

                return TriPropertyType.Primitive;
            }

            if (property._serializedObject != null)
            {
                return TriPropertyType.Generic;
            }

            if (property._definition.FieldType.IsPrimitive ||
                property._definition.FieldType == typeof(string) ||
                typeof(UnityEngine.Object).IsAssignableFrom(property._definition.FieldType))
            {
                return TriPropertyType.Primitive;
            }

            if (property._definition.FieldType.IsValueType)
            {
                return TriPropertyType.Generic;
            }

            if (property._definition.IsArray)
            {
                return TriPropertyType.Array;
            }

            return TriPropertyType.Reference;
        }
    }

    public enum TriPropertyType
    {
        Array,
        Reference,
        Generic,
        Primitive,
    }
}

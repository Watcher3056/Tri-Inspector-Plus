using System;
using UnityEngine;

namespace TriInspector
{
    /// <summary>
    /// Use this attribute to limit the drawing depth of a property in the TriInspector editor.
    /// This is particularly useful for preventing infinite recursion with self-referencing types.
    /// </summary>
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property | AttributeTargets.Class, Inherited = true, AllowMultiple = false)]
    public class MaxDrawDepthAttribute : PropertyAttribute
    {
        /// <summary>
        /// The maximum depth to which the property and its children will be drawn.
        /// A depth of 0 means only the property itself will be drawn, not its children.
        /// </summary>
        public readonly int MaxDepth;

        /// <summary>
        /// Initializes a new instance of the <see cref="MaxDrawDepthAttribute"/> class.
        /// </summary>
        /// <param name="maxDepth">The maximum depth for drawing this property.
        /// For example, a value of 1 will draw the property and its direct children, but not their children.</param>
        public MaxDrawDepthAttribute(int maxDepth)
        {
            if (maxDepth < 0)
            {
                Debug.LogWarning($"MaxDrawDepthAttribute: maxDepth cannot be negative. Setting to 0. Attribute applied to: {GetType().Name}");
                MaxDepth = 0;
            }
            else
            {
                MaxDepth = maxDepth;
            }
        }
    }
}

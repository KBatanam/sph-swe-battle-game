using UnityEditor;
using UnityEngine;
using Validation;

namespace Editor
{
    [CustomPropertyDrawer(typeof(RequiredAttribute))]
    public sealed class RequiredPropertyDrawer : PropertyDrawer
    {
        private const float HelpBoxHeight = 36f;
        private const float Spacing = 2f;

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            var propertyHeight = EditorGUI.GetPropertyHeight(property, label, true);
            return GetValidationMessage(property) == null
                ? propertyHeight
                : propertyHeight + Spacing + HelpBoxHeight;
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);

            var propertyHeight = EditorGUI.GetPropertyHeight(property, label, true);
            var propertyPosition = new Rect(position.x, position.y, position.width, propertyHeight);
            EditorGUI.PropertyField(propertyPosition, property, label, true);

            var validationMessage = GetValidationMessage(property);

            if (validationMessage != null)
            {
                var helpBoxPosition = new Rect(position.x, propertyPosition.yMax + Spacing, position.width, HelpBoxHeight);
                EditorGUI.HelpBox(helpBoxPosition, validationMessage, MessageType.Error);
            }

            EditorGUI.EndProperty();
        }

        private static string GetValidationMessage(SerializedProperty property)
        {
            if (property.propertyType != SerializedPropertyType.ObjectReference)
            {
                return "Required can only be used with Unity object references.";
            }

            return property.objectReferenceValue == null
                ? $"{property.displayName} must be assigned."
                : null;
        }
    }
}

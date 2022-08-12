using UnityEditor;
using UnityEngine;

namespace TrickCore
{
    [CustomPropertyDrawer(typeof(MinMaxRangeAttribute))]
    public class MinMaxValueRangeDrawer : PropertyDrawer
    {
        public MinMaxValueRangeDrawer()
        {
        }
    
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);
            int indent = EditorGUI.indentLevel;
        
            MinMaxRangeAttribute rangeAttribute = (MinMaxRangeAttribute)attribute;
        
            switch (rangeAttribute.Type)
            {
                case MinMaxRangeAttribute.ValueType.Float:
                    DrawFloatSlider(rangeAttribute, property, position);
                    break;
                case MinMaxRangeAttribute.ValueType.Integer:
                    DrawIntSlider(rangeAttribute, property, position);
                    break;
            }

            EditorGUI.indentLevel = indent;
            EditorGUI.EndProperty();
        }

        private void DrawIntSlider(MinMaxRangeAttribute rangeAttribute, SerializedProperty property, Rect position)
        {
            SerializedProperty minValue = property.FindPropertyRelative("MinValueInt");
            SerializedProperty maxValue = property.FindPropertyRelative("MaxValueInt");

            SerializedProperty valueType = property.FindPropertyRelative("ValueType");
            valueType.enumValueIndex = (int)MinMaxRangeAttribute.ValueType.Integer;

            var nameLabel = string.IsNullOrEmpty(rangeAttribute.Name) ? new GUIContent(fieldInfo.Name) : new GUIContent(rangeAttribute.Name, rangeAttribute.Description);
            var controlId = GUIUtility.GetControlID(FocusType.Keyboard, position);
            Rect offset = EditorGUI.PrefixLabel(position, controlId, nameLabel);

            float xDiff = position.width - offset.x;
            const float sliderSpacing = 4.0f;

            Rect nameLabelPos = new Rect(position.x, position.y, offset.width, 16);
            Rect slidePos = new Rect(offset.x + 48 + sliderSpacing, position.y, xDiff - (48 + sliderSpacing) * 2, 16);
            Rect minPos = new Rect(offset.x, position.y, 48, 16);
            Rect maxPos = new Rect(offset.x + xDiff - 48, position.y, 48, 16);

            EditorGUI.LabelField(nameLabelPos, nameLabel);

            minValue.intValue = EditorGUI.IntField(minPos, minValue.intValue);
            maxValue.intValue = EditorGUI.IntField(maxPos, maxValue.intValue);

            float minVal = minValue.intValue;
            float maxVal = maxValue.intValue;

            EditorGUI.MinMaxSlider(slidePos, GUIContent.none,
                ref minVal, ref maxVal,
                rangeAttribute.MinValueInt, rangeAttribute.MaxValueInt);

            minValue.intValue = (int) minVal;
            maxValue.intValue = (int) maxVal;

            if (minValue.intValue < rangeAttribute.MinValueInt)
                minValue.intValue = rangeAttribute.MinValueInt;
            if (maxValue.intValue > rangeAttribute.MaxValueInt)
                maxValue.intValue = rangeAttribute.MaxValueInt;
        }

        private void DrawFloatSlider(MinMaxRangeAttribute rangeAttribute, SerializedProperty property, Rect position)
        {
            SerializedProperty minValue = property.FindPropertyRelative("MinValueFloat");
            SerializedProperty maxValue = property.FindPropertyRelative("MaxValueFloat");

            SerializedProperty valueType = property.FindPropertyRelative("ValueType");
            valueType.enumValueIndex = (int) MinMaxRangeAttribute.ValueType.Float;

            var nameLabel = string.IsNullOrEmpty(rangeAttribute.Name) ? new GUIContent(fieldInfo.Name) : new GUIContent(rangeAttribute.Name, rangeAttribute.Description);
            var controlId = GUIUtility.GetControlID(FocusType.Keyboard, position);
            Rect offset = EditorGUI.PrefixLabel(position, controlId, nameLabel);

            float xDiff = position.width - offset.x;
            const float sliderSpacing = 4.0f;

            Rect nameLabelPos = new Rect(position.x, position.y, offset.width, 16);
            Rect slidePos = new Rect(offset.x + 48 + sliderSpacing, position.y, xDiff - (48 + sliderSpacing) * 2, 16);
            Rect minPos = new Rect(offset.x, position.y, 48, 16);
            Rect maxPos = new Rect(offset.x + xDiff - 48, position.y, 48, 16);


            EditorGUI.LabelField(nameLabelPos, nameLabel);

            minValue.floatValue = EditorGUI.FloatField(minPos, minValue.floatValue);
            maxValue.floatValue = EditorGUI.FloatField(maxPos, maxValue.floatValue);

            float minVal = minValue.floatValue;
            float maxVal = maxValue.floatValue;

            EditorGUI.MinMaxSlider(slidePos, GUIContent.none,
                ref minVal, ref maxVal,
                rangeAttribute.MinValueFloat, rangeAttribute.MaxValueFloat);

            minValue.floatValue = minVal;
            maxValue.floatValue = maxVal;

            if (minValue.floatValue < rangeAttribute.MinValueFloat)
                minValue.floatValue = rangeAttribute.MinValueFloat;
            if (maxValue.floatValue > rangeAttribute.MaxValueFloat)
                maxValue.floatValue = rangeAttribute.MaxValueFloat;
        }

    }
}
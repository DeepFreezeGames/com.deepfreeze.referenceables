using System;
using System.IO;
using System.Linq;
using Referenceables.Runtime;
using UnityEditor;
using UnityEngine;

namespace Referenceables.Editor
{
    [CustomPropertyDrawer(typeof(ReferenceAttribute))]
    public class ReferenceDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            if (ReferenceableHelper.IsDirty || !ReferenceableHelper.WasInitialized)
            {
                return;
            }

            var entityTypes = (attribute as ReferenceAttribute)?.Types;
            var exclusionTypes = (attribute as ReferenceAttribute)?.Excludes;
            if (entityTypes?.Length == 0)
            {
                entityTypes = ReferenceableHelper.GetTypes();
            }

            var ids = ReferenceableHelper.GetIds(entityTypes, exclusionTypes);
            var names = ReferenceableHelper.GetNames(entityTypes, exclusionTypes);
            
            ids.Insert(0, string.Empty);
            names.Insert(0, "None");

            var currentId = property.stringValue;
            var index = ids.IndexOf(currentId);
            var slices = SliceProportions(position, 0.85f);
            index = EditorGUI.Popup(slices[0], label.text, index, names.ToArray());
            if (index >= 0 && index < ids.Count)
            {
                var newId = ids[index];
                if (newId != currentId)
                {
                    property.stringValue = newId;
                }
            }

            if (index == 0)
            {
                var allowedTypes = ReferenceableHelper.GetSubTypes(entityTypes, exclusionTypes).ToArray();
                var typeNames = allowedTypes
                    .Select(t => t.Name)
                    .Prepend("New")
                    .ToArray();

                var newSelected = EditorGUI.Popup(slices[1], 0, typeNames);

                if (newSelected > 0)
                {
                    var create = allowedTypes[newSelected - 1];

                    var myObject = property.serializedObject.targetObject;
                    var myPath = Path.GetDirectoryName(AssetDatabase.GetAssetPath(myObject));
                    if (myPath != null)
                    {
                        var childPath = Path.Combine(myPath, $"{myObject.name}_{create.Name}.asset");
                        childPath = AssetDatabase.GenerateUniqueAssetPath(childPath);

                        var instance = ScriptableObject.CreateInstance(create);
                        AssetDatabase.CreateAsset(instance, childPath);
                    }

                    AssetDatabase.Refresh();
                }
            }
            else
            {
                if (GUI.Button(slices[1], "Select"))
                {
                    var guid = ReferenceableHelper.GetGuid(property.stringValue);
                    var assetPath = AssetDatabase.GUIDToAssetPath(guid);
                    var asset = AssetDatabase.LoadMainAssetAtPath(assetPath);
                    EditorUtility.FocusProjectWindow();
                    Selection.activeObject = asset;
                }
            }
        }

        private static Rect[] SliceProportions(Rect rect, float widthProportions)
        {
            var width = rect.width * widthProportions;
            var result = new Rect(rect.x, rect.y, width, rect.height);
            rect.x += width;
            rect.width -= width;
            return new[] {result, rect};
        }
    }
}
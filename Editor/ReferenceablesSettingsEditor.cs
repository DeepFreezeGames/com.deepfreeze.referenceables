using UnityEditor;

namespace Referenceables.Editor
{
    [CustomEditor(typeof(ReferenceablesSettings))]
    public class ReferenceablesSettingsEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            EditorGUI.BeginChangeCheck();
            serializedObject.Update();
            var iterator = serializedObject.GetIterator();
            var enterChildren = true;
            while (iterator.NextVisible(enterChildren))
            {
                if (iterator.propertyPath == "m_Script")
                {
                    continue;
                }

                EditorGUILayout.PropertyField(iterator, true);
                enterChildren = false;
            }

            serializedObject.ApplyModifiedProperties();
            if (EditorGUI.EndChangeCheck())
            {
                ReferenceablesSettings.Instance.Save();
            }
        }

        private class Styles
        {
            
        }

        [SettingsProvider]
        private static SettingsProvider CreateGameSettingsProvider()
        {
            var provider = new AssetSettingsProvider("Project/Referenceables Settings", () => ReferenceablesSettings.Instance);
            provider.PopulateSearchKeywordsFromGUIContentProperties<Styles>();
            return provider;
        }
    }
}

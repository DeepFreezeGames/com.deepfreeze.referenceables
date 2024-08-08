using System;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Referenceables.Editor
{
    public class ReferenceablesEditorWindow : EditorWindow
    {
        private Type _selectedType= null;
        private Vector2 _scrollPos = Vector2.zero;

        private bool _showingOptions;
        private UnityEditor.Editor _settingsEditor;
        
        [MenuItem("Window/Asset Management/Referenceables/Editor Window")]
        public static void Initialize()
        {
            var window = GetWindow<ReferenceablesEditorWindow>();
            window.titleContent = new GUIContent("Referenceables");
            window.Show();
        }

        public void OnGUI()
        {
            if (_showingOptions)
            {
                DrawSettings();
                return;
            }
            
            EditorGUILayout.BeginHorizontal();
            {
                DrawSidebar();
                EditorGUILayout.BeginVertical();
                {
                    DrawToolbar();
                    DrawInspector();
                }
                EditorGUILayout.EndVertical();
            }
            EditorGUILayout.EndHorizontal();
        }

        private void DrawSidebar()
        {
            EditorGUILayout.BeginVertical("box", GUILayout.Width(200));
            {
                var types = ReferenceableHelper.GetTypes();
                foreach (var type in types)
                {
                    GUI.color = _selectedType != null && _selectedType == type ? Color.cyan : Color.white;
                    if (GUILayout.Button(type.Name))
                    {
                        _selectedType = _selectedType != null && _selectedType == type ? null : type;
                        _scrollPos = Vector2.zero;
                        GUI.FocusControl(null);
                    }
                    GUI.color = Color.white;
                }
                
                GUILayout.FlexibleSpace();
            }
            EditorGUILayout.EndVertical();
        }

        private void DrawToolbar()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            {
                if (GUILayout.Button(EditorGUIUtility.IconContent("Refresh@2x"), EditorStyles.toolbarButton, GUILayout.Width(24f)))
                {
                    ReferenceableHelper.Refresh();
                }
                
                GUILayout.FlexibleSpace();

                if (GUILayout.Button(EditorGUIUtility.IconContent("d__Menu@2x"), EditorStyles.toolbarButton, GUILayout.Width(24f)))
                {
                    var optionsMenu = new GenericMenu();
                    optionsMenu.AddItem(new GUIContent("Show Options"), false, () =>
                    {
                        _showingOptions = true;
                        _scrollPos = Vector2.zero;
                        GUI.FocusControl(null);
                    });
                    optionsMenu.ShowAsContext();
                }
            }
            EditorGUILayout.EndHorizontal();
        }

        private void DrawInspector()
        {
            EditorGUILayout.BeginVertical("box", GUILayout.ExpandHeight(true), GUILayout.ExpandWidth(true));
            {
                if (_selectedType != null)
                {
                    var itemWidth = (position.width - 319) / 3f;
                    var items = ReferenceableHelper.GetIds(new []{_selectedType}, Type.EmptyTypes);
                    EditorGUILayout.BeginHorizontal("box");
                    {
                        GUILayout.Label("Asset Name", EditorStyles.boldLabel, GUILayout.Width(itemWidth));
                        GUILayout.Label("Asset Type", EditorStyles.boldLabel, GUILayout.Width(itemWidth));
                        GUILayout.Label("Reference ID", EditorStyles.boldLabel, GUILayout.Width(itemWidth + 31f));
                        GUILayout.FlexibleSpace();
                    }
                    EditorGUILayout.EndHorizontal();
                    _scrollPos = EditorGUILayout.BeginScrollView(_scrollPos);
                    {
                        foreach (var item in items)
                        {
                            EditorGUILayout.BeginHorizontal("box");
                            {
                                GUILayout.Label(ReferenceableHelper.GetName(_selectedType, item), GUILayout.Width(itemWidth));
                                GUILayout.Label(ReferenceableHelper.GetType(item).Name, GUILayout.Width(itemWidth));
                                GUI.contentColor =
                                    ReferenceableHelper.DuplicateAssets.Count != 0 &&
                                    ReferenceableHelper.DuplicateAssets.ContainsKey(item)
                                        ? Color.red
                                        : Color.white;
                                GUILayout.Label(item, GUILayout.Width(itemWidth));
                                GUILayout.FlexibleSpace();
                                GUI.contentColor = Color.white;
                                if(GUILayout.Button("Select", EditorStyles.miniButton, GUILayout.Width(50f)))
                                {
                                    var guid = AssetDatabase.FindAssets($"{ReferenceableHelper.GetName(item)}").First();
                                    if (guid != null)
                                    {
                                        var path = AssetDatabase.GUIDToAssetPath(guid);
                                        var asset = AssetDatabase.LoadAssetAtPath<ScriptableObject>(path);
                                        Selection.activeObject = asset;
                                        EditorUtility.FocusProjectWindow();
                                    }
                                }
                            }
                            EditorGUILayout.EndHorizontal();
                        }
                    }
                    EditorGUILayout.EndScrollView();
                }
                
                GUILayout.FlexibleSpace();
            }
            EditorGUILayout.EndVertical();
        }

        private void DrawSettings()
        {
            if (_settingsEditor == null || _settingsEditor.target != ReferenceablesSettings.Instance)
            {
                _settingsEditor = UnityEditor.Editor.CreateEditor(ReferenceablesSettings.Instance);
            }
            
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar, GUILayout.ExpandWidth(true));
            {
                GUILayout.FlexibleSpace();
                if (GUILayout.Button("X", EditorStyles.toolbarButton))
                {
                    _showingOptions = false;
                    _scrollPos = Vector2.zero;
                    GUI.FocusControl(null);
                }
            }
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.BeginVertical("box");
            {
                _scrollPos = EditorGUILayout.BeginScrollView(_scrollPos);
                {
                    _settingsEditor.serializedObject.Update();
                    _settingsEditor.OnInspectorGUI();
                    _settingsEditor.serializedObject.ApplyModifiedProperties();
                }
                EditorGUILayout.EndScrollView();
                GUILayout.FlexibleSpace();
            }
            EditorGUILayout.EndVertical();
        }
    }
}

using System;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Referenceables.Editor
{
    public class ReferenceablesEditor : EditorWindow
    {
        private Type _selectedType = null;
        private Vector2 _scrollPos = Vector2.zero;
        
        [MenuItem("Window/Asset Management/Referenceables/Editor Window")]
        public static void Initialize()
        {
            var window = GetWindow<ReferenceablesEditor>();
            window.titleContent = new GUIContent("Referenceables");
            window.Show();
        }

        public void OnGUI()
        {
            EditorGUILayout.BeginHorizontal();
            {
                DrawSidebar();
                EditorGUILayout.BeginVertical();
                {
                    DrawToolbar();
                    DrawMainArea();
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
                    GUI.enabled = type != _selectedType;
                    if (GUILayout.Button(type.Name))
                    {
                        _selectedType = type;
                        _scrollPos = Vector2.zero;
                    }
                    GUI.enabled = true;
                }
            }
            EditorGUILayout.EndVertical();
        }

        private void DrawToolbar()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            {
                GUILayout.FlexibleSpace();
                if (_selectedType != null)
                {
                    if (GUILayout.Button("Deselect", EditorStyles.toolbarButton))
                    {
                        _selectedType = null;
                    }
                }
                if (GUILayout.Button("Refresh", EditorStyles.toolbarButton))
                {
                    ReferenceableHelper.Refresh();
                }
            }
            EditorGUILayout.EndHorizontal();
        }

        private void DrawMainArea()
        {
            EditorGUILayout.BeginVertical();
            {
                if (_selectedType != null)
                {
                    var itemWidth = (position.width - 269) / 2f;
                    var items = ReferenceableHelper.GetIds(new []{_selectedType}, new Type[0]);
                    EditorGUILayout.BeginHorizontal("box");
                    {
                        GUILayout.Label("Asset Name", EditorStyles.boldLabel, GUILayout.Width(itemWidth));
                        GUILayout.Label("Reference ID", EditorStyles.boldLabel, GUILayout.Width(itemWidth + 31f));
                    }
                    EditorGUILayout.EndHorizontal();
                    _scrollPos = EditorGUILayout.BeginScrollView(_scrollPos);
                    {
                        foreach (var item in items)
                        {
                            EditorGUILayout.BeginHorizontal("box");
                            {
                                GUILayout.Label(ReferenceableHelper.GetName(_selectedType, item), GUILayout.Width(itemWidth));
                                GUI.contentColor =
                                    ReferenceableHelper.DuplicateAssets.Count != 0 &&
                                    ReferenceableHelper.DuplicateAssets.ContainsKey(item)
                                        ? Color.red
                                        : Color.white;
                                GUILayout.Label(item, GUILayout.Width(itemWidth));
                                GUI.contentColor = Color.white;
                                if (GUILayout.Button(EditorGUIUtility.IconContent("_Popup"), GUILayout.Width(24f)))
                                {
                                    var menu = new GenericMenu();
                                    
                                    menu.AddItem(new GUIContent("Select"), false, () =>
                                    {
                                        var guid = AssetDatabase.FindAssets($"{ReferenceableHelper.GetName(item)}").First();
                                        if (guid != null)
                                        {
                                            var path = AssetDatabase.GUIDToAssetPath(guid);
                                            var asset = AssetDatabase.LoadAssetAtPath<ScriptableObject>(path);
                                            Selection.activeObject = asset;
                                            EditorUtility.FocusProjectWindow();
                                        }
                                    } );
                                    
                                    menu.ShowAsContext();
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
    }
}

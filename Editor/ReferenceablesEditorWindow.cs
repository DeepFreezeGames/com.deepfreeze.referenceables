using System;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Referenceables.Editor
{
    public class ReferenceablesEditorWindow : EditorWindow
    {
        private Type _selectedType;
        private Vector2 _scrollPos = Vector2.zero;

        private bool _showingOptions;
        private UnityEditor.Editor _settingsEditor;
        private UnityEditor.Editor _selectedAsset;

        private string _searchValue = string.Empty;
        public string SearchValue
        {
            get => _searchValue;
            set
            {
                
            }
        }
        
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
                    DrawItemList();
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
                        _selectedAsset = null;
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

        private void DrawItemList()
        {
            EditorGUILayout.BeginVertical("box", GUILayout.ExpandHeight(true), GUILayout.ExpandWidth(true));
            {
                if (_selectedType != null)
                {
                    var itemWidth = (position.width - 319) / 2f;
                    var items = ReferenceableHelper.GetIds(new []{_selectedType}, Type.EmptyTypes);
                    EditorGUILayout.BeginHorizontal("box");
                    {
                        GUILayout.Label("Asset Name", EditorStyles.boldLabel, GUILayout.Width(itemWidth));
                        GUILayout.Label("Reference ID", EditorStyles.boldLabel, GUILayout.Width(itemWidth + 31f));
                        GUILayout.FlexibleSpace();
                    }
                    EditorGUILayout.EndHorizontal();
                    _scrollPos = EditorGUILayout.BeginScrollView(_scrollPos);
                    {
                        var currentId = _selectedAsset != null ? ((IReferenceable)_selectedAsset.target).Id : string.Empty;
                        foreach (var item in items)
                        {
                            GUI.color = currentId.Equals(item) ? Color.cyan : Color.white;
                            EditorGUILayout.BeginHorizontal("box");
                            {
                                GUILayout.Label(ReferenceableHelper.GetName(_selectedType, item), GUILayout.Width(itemWidth));
                                GUI.contentColor =
                                    ReferenceableHelper.DuplicateAssets.Count != 0 &&
                                    ReferenceableHelper.DuplicateAssets.ContainsKey(item)
                                        ? Color.red
                                        : Color.white;
                                GUILayout.Label(item);
                                //GUILayout.FlexibleSpace();
                                GUI.contentColor = Color.white;
                            }
                            EditorGUILayout.EndHorizontal();
                            GUI.color = Color.white;
                            var e = Event.current;
                            var rect = GUILayoutUtility.GetLastRect();
                            if (e.isMouse && e.type == EventType.MouseDown && rect.Contains(e.mousePosition))
                            {
                                switch (e.button)
                                {
                                    case 0:
                                        var asset = ReferenceableHelper.GetAssetWithId(item);
                                        _selectedAsset = (_selectedAsset != null ? _selectedAsset.target : null) == asset
                                            ? null
                                            : UnityEditor.Editor.CreateEditor(asset);
                                        Selection.activeObject = asset;
                                        GUI.FocusControl(null);
                                        e.Use();
                                        break;
                                    case 1:
                                        var menu = new GenericMenu();
                                        menu.AddItem(new GUIContent("Regenerate GUID"), false, () => {});
                                        menu.AddItem(new GUIContent("Edit GUID"), false, () => {});
                                        menu.AddSeparator("");
                                        menu.AddItem(new GUIContent("Add To Addressables"), false, () => {});
                                        menu.ShowAsContext();
                                        break;
                                }
                            }
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

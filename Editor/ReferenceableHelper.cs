using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Referenceables;
using UnityEditor;
using UnityEditor.Compilation;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Referenceables.Editor
{
    public static class ReferenceableHelper
    {
        private static List<ReferenceableInfo> _entitiesToLoad = new();
        private static Dictionary<Type, List<string>> _ids = new();
        private static Dictionary<Type, List<string>> _guids = new();
        private static Dictionary<Type, List<string>> _names = new();
        private static Dictionary<Type, List<ScriptableObject>> _objects = new();

        private static Dictionary<string, ReferenceableDetailInfo> _uniqueAssets = new();
        private static Dictionary<string, List<ReferenceableDetailInfo>> _duplicateAssets = new();
        
        public static bool IsDirty { get; private set; }
        public static bool WasInitialized { get; private set; }

        public static Dictionary<string, List<ReferenceableDetailInfo>> DuplicateAssets => _duplicateAssets;
        
        static ReferenceableHelper()
        {
            Initialize();
            Refresh();
            CompilationPipeline.compilationFinished += OnCompilationFinished;
        }

        private static void OnCompilationFinished(object compilationObject)
        {
            Refresh();
        }

        [MenuItem("Window/Asset Management/Referenceables/Refresh")]
        public static void Refresh()
        {
            IsDirty = true;
            Update();
            
            if (_duplicateAssets.Count != 0)
            {
                LogError("Duplicate guid refs were found!");
            }
        }

        private static void Initialize()
        {
            _entitiesToLoad = new List<ReferenceableInfo>();

            LoadEntitiesInfo();

            _ids = new Dictionary<Type, List<string>>();
            _guids = new Dictionary<Type, List<string>>();
            _names = new Dictionary<Type, List<string>>();
            _objects = new Dictionary<Type, List<ScriptableObject>>();
            
            _uniqueAssets = new Dictionary<string, ReferenceableDetailInfo>();
            _duplicateAssets = new Dictionary<string, List<ReferenceableDetailInfo>>();

            foreach (var referenceableInfo in _entitiesToLoad)
            {
                Log($"Searching for: {referenceableInfo.EntityType.Name}");
                LoadIds($"t:{referenceableInfo.AssetType.FullName}", referenceableInfo.EntityType);
            }

            WasInitialized = true;
        }

        private static void LoadEntitiesInfo()
        {
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                if (assembly.IsDynamic)
                {
                    continue;
                }

                foreach (var exportedType in assembly.GetExportedTypes())
                {
                    if (exportedType.IsAbstract || exportedType.IsInterface || !exportedType.IsPublic)
                    {
                        continue;
                    }

                    ReferenceableAttribute[] customAttributes;
                    try
                    {
                        customAttributes = exportedType.GetCustomAttributes(typeof(ReferenceableAttribute), true) as ReferenceableAttribute[];
                        if (customAttributes != null)
                        {
                            foreach (var customAttribute in customAttributes)
                            {
                                Log($"Custom attribute: {customAttribute.ReferenceType}");
                            }
                        }
                    }
                    catch (Exception exception)
                    {
                        LogError($"Could not get attributes for {exportedType.Name}");
                        Debug.LogException(exception);
                        continue;
                    }
                    if (customAttributes == null || customAttributes.Length == 0 || customAttributes.Length > 1)
                    {
                        continue;
                    }

                    _entitiesToLoad.Add(new ReferenceableInfo(customAttributes[0].ReferenceType, exportedType));
                }
            }
        }

        private static void LoadIds(string filter, Type type)
        {
            var assetGuids = AssetDatabase.FindAssets(filter);
            var ids = new List<string>();
            var guids = new List<string>();
            var names = new List<string>();
            var objs = new List<ScriptableObject>();

            foreach (var guid in assetGuids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                if (AssetDatabase.LoadAssetAtPath(path, typeof(IReferenceable)) is IReferenceable entity)
                {
                    ids.Add(entity.Id);
                    guids.Add(guid);
                    objs.Add((ScriptableObject)entity);
                    var name = Path.GetFileNameWithoutExtension(path);
                    names.Add(name);

                    if (string.IsNullOrEmpty(entity.Id))
                    {
                        LogError($"Entity of type {entity.GetType()} had null ID");
                        continue;
                    }

                    if (!_uniqueAssets.ContainsKey(entity.Id))
                    {
                        _uniqueAssets.Add(entity.Id, new ReferenceableDetailInfo(type, entity.Id, guid, path, (ScriptableObject)entity));
                    }
                    else
                    {
                        if (ReferenceablesSettings.Instance.allowDuplicates)
                        {
                            continue;
                        }
                        
                        if (!_duplicateAssets.ContainsKey(entity.Id))
                        {
                            _duplicateAssets.Add(entity.Id, new List<ReferenceableDetailInfo>());
                            _duplicateAssets[entity.Id].Add(_uniqueAssets[entity.Id]);
                        }
                        
                        _duplicateAssets[entity.Id].Add(new ReferenceableDetailInfo(type, entity.Id, guid, path, (ScriptableObject)entity));
                    }
                }
                else
                {
                    LogWarning($"Path: {path} was null");
                }
            }

            if (!_ids.ContainsKey(type))
            {
                _ids.Add(type, ids);
            }
            else
            {
                _ids[type].AddRange(ids);
            }

            if (!_guids.ContainsKey(type))
            {
                _guids.Add(type, guids);
            }
            else
            {
                _guids[type].AddRange(guids);
            }

            if (!_names.ContainsKey(type))
            {
                _names.Add(type, names);
            }
            else
            {
                _names[type].AddRange(names);
            }
            
            if (!_objects.ContainsKey(type))
            {
                _objects.Add(type, objs);
            }
            else
            {
                _objects[type].AddRange(objs);
            }
        }

        private static void Update()
        {
            if (!WasInitialized || IsDirty)
            {
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                Initialize();
            }

            switch (IsDirty)
            {
                //Only update the guids after compiling
                case true when !EditorApplication.isCompiling:
                {
                    IsDirty = false;
                    if (_names.Count == 0)
                    {
                        Log("No Referenceable objects were found");
                        return;
                    }
                
                    var stringBuilder = new StringBuilder();
                    stringBuilder.Append($"<b>[{nameof(ReferenceableHelper)}]</b> Added guids for:");
                    foreach (var keyValuePair in _names)
                    {
                        stringBuilder.AppendFormat("\n{0}: {1}", keyValuePair.Key, keyValuePair.Value.Count.ToString());
                    }

                    Log(stringBuilder.ToString());
                    return;
                }
                //Automatically update Guids after compilation
                case false when EditorApplication.isCompiling:
                    IsDirty = true;
                    break;
            }
        }

        public static List<string> GetIds(Type[] types, Type[] excludes)
        {
            var result = new List<string>();
            var all =
                _ids.Where(keyValuePair =>
                    {
                        foreach (var type in types)
                        {
                            var (key, _) = keyValuePair;
                            if (excludes != null)
                            {
                                if (excludes.Any(exclude => exclude.IsAssignableFrom(key)))
                                {
                                    return false;
                                }
                            }

                            if (type.IsAssignableFrom(key))
                            {
                                return true;
                            }
                        }

                        return false;
                    }
                );

            foreach (var pair in all)
            {
                result.AddRange(pair.Value);
            }

            return result;
        }
        
        public static List<string> GetGuids(Type[] types, Type[] excludes)
        {
            var result = new List<string>();
            var all =
                _guids.Where(keyValuePair =>
                    {
                        foreach (var type in types)
                        {
                            var (key, _) = keyValuePair;
                            if (excludes.Any(exclude => exclude.IsAssignableFrom(key)))
                            {
                                return false;
                            }

                            if (type.IsAssignableFrom(key))
                            {
                                return true;
                            }
                        }

                        return false;
                    }
                );

            foreach (var pair in all)
            {
                result.AddRange(pair.Value);
            }

            return result;
        }

        public static List<string> GetGuids(Type type)
        {
            return _guids.ContainsKey(type) ? _guids[type] : null;
        }

        public static Object GetAssetWithId(string id)
        {
            var guid = AssetDatabase.FindAssets($"{GetName(id)}").First();
            return guid == null 
                ? default(Object) 
                : AssetDatabase.LoadAssetAtPath<ScriptableObject>(AssetDatabase.GUIDToAssetPath(guid));
        }

        public static List<string> GetNames(Type[] types, Type[] excludes)
        {
            var result = new List<string>();

            var all =
                _names.Where(keyValuePair =>
                    {
                        foreach (var type in types)
                        {
                            var (key, _) = keyValuePair;
                            if(excludes != null)
                            {
                                if (excludes.Any(exclude => exclude.IsAssignableFrom(key)))
                                {
                                    return false;
                                }
                            }

                            if (type.IsAssignableFrom(key))
                            {
                                return true;
                            }
                        }

                        return false;
                    }
                );

            foreach (var pair in all)
            {
                result.AddRange(pair.Value);
            }

            return result;
        }

        public static Type[] GetTypes()
        {
            return _ids.Keys.ToArray();
        }

        public static HashSet<Type> GetSubTypes(Type[] parents, Type[] excludes)
        {
            var result = new HashSet<Type>();
            foreach (var type in _entitiesToLoad
                .Where(type => !excludes.Any(exclude => exclude.IsAssignableFrom(type.AssetType)))
                .Where(type => parents.Any(parent => parent.IsAssignableFrom(type.EntityType))))
            {
                result.Add(type.AssetType);
            }

            return result;
        }

        public static string GetName(Type type, string id)
        {
            if (!_names.ContainsKey(type))
            {
                return null;
            }
            var index = _ids[type].IndexOf(id);
            return index < 0 ? null : _names[type][index];
        }
        
        public static string GetName(string id)
        {
            foreach (var (key, value) in _ids)
            {
                for (var i = 0; i < value.Count; i++)
                {
                    if (value[i] == id)
                    {
                        return _names[key][i];
                    }
                }
            }

            return null;
        }

        public static string GetGuid(Type type, string id)
        {
            if (!_ids.ContainsKey(type))
            {
                return null;
            }
            var index = _guids[type].IndexOf(id);
            return index < 0 ? null : _guids[type][index];
        }

        public static string GetGuid(string id)
        {
            foreach (var (key, value) in _ids)
            {
                for (var i = 0; i < value.Count; i++)
                {
                    if (value[i] == id)
                    {
                        return _guids[key][i];
                    }
                }
            }

            return null;
        }

        private static void Log(string message)
        {
            if (ReferenceablesSettings.Instance.logMessages)
            {
                Debug.Log($"[REFERENCEABLES] {message}");
            }
        }

        private static void LogWarning(string message)
        {
            if (ReferenceablesSettings.Instance.logWarnings)
            {
                Debug.LogWarning($"[REFERENCEABLES] {message}");
            }
        }

        private static void LogError(string message)
        {
            if (ReferenceablesSettings.Instance.logErrors)
            {
                Debug.LogError($"[REFERENCEABLES] {message}");
            }
        }
    }
}

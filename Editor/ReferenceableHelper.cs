using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Referenceables.Runtime;
using UnityEditor;
using UnityEditor.Compilation;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Referenceables.Editor
{
    public static class ReferenceableHelper
    {
        private static List<ReferenceableInfo> _entitiesToLoad = new List<ReferenceableInfo>();
        private static Dictionary<Type, List<string>> _ids = new Dictionary<Type, List<string>>();
        private static Dictionary<Type, List<string>> _guids = new Dictionary<Type, List<string>>();
        private static Dictionary<Type, List<string>> _names = new Dictionary<Type, List<string>>();
        private static Dictionary<Type, List<ScriptableObject>> _objects = new Dictionary<Type, List<ScriptableObject>>();

        private static Dictionary<string, ReferenceableDetailInfo> _uniqueAssets = new Dictionary<string, ReferenceableDetailInfo>();
        private static Dictionary<string, List<ReferenceableDetailInfo>> _duplicateAssets = new Dictionary<string, List<ReferenceableDetailInfo>>();
        
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
                Debug.LogError("Duplicate guid refs were found!");
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
                Debug.Log($"Searching for: {referenceableInfo.EntityType.Name}");
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
                                Debug.Log($"Custom attribute: {customAttribute.ReferenceType}");
                            }
                        }
                    }
                    catch (Exception exception)
                    {
                        Debug.LogError($"Could not get attributes for {exportedType.Name}");
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
                var entity = AssetDatabase.LoadAssetAtPath(path, typeof(IReferenceable)) as IReferenceable;
                if (entity != null)
                {
                    ids.Add(entity.Id);
                    guids.Add(guid);
                    objs.Add((ScriptableObject)entity);
                    var name = Path.GetFileNameWithoutExtension(path);
                    names.Add(name);

                    if (entity.Id == null)
                    {
                        Debug.LogError($"Entity of type {entity.GetType()} had null ID");
                        continue;
                    }

                    if (!_uniqueAssets.ContainsKey(entity.Id))
                    {
                        _uniqueAssets.Add(entity.Id, new ReferenceableDetailInfo(type, entity.Id, guid, path, (ScriptableObject)entity));
                    }
                    else
                    {
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
                    Debug.LogWarningFormat("[DataForge References] Path: {0} was null", path);
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

            //Only update the guids after compiling
            if (IsDirty && !EditorApplication.isCompiling)
            {
                IsDirty = false;
                if (_names.Count == 0)
                {
                    Debug.Log("No Referenceable objects were found");
                    return;
                }
                
                var stringBuilder = new StringBuilder();
                stringBuilder.Append("[GuidRefHelper] Added guids for:");
                foreach (var keyValuePair in _names)
                {
                    stringBuilder.AppendFormat("\n{0}: {1}", keyValuePair.Key, keyValuePair.Value.Count.ToString());
                }

                Debug.Log(stringBuilder.ToString());
                return;
            }

            //Automatically update Guids after compilation
            if (!IsDirty && EditorApplication.isCompiling)
            {
                IsDirty = true;
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
                            if (excludes != null)
                            {
                                if (excludes.Any(exclude => exclude.IsAssignableFrom(keyValuePair.Key)))
                                {
                                    return false;
                                }
                            }

                            if (type.IsAssignableFrom(keyValuePair.Key))
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
                            if (excludes.Any(exclude => exclude.IsAssignableFrom(keyValuePair.Key)))
                            {
                                return false;
                            }

                            if (type.IsAssignableFrom(keyValuePair.Key))
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

        public static List<string> GetNames(Type[] types, Type[] excludes)
        {
            var result = new List<string>();

            var all =
                _names.Where(keyValuePair =>
                    {
                        foreach (var type in types)
                        {
                            if(excludes != null)
                            {
                                if (excludes.Any(exclude => exclude.IsAssignableFrom(keyValuePair.Key)))
                                {
                                    return false;
                                }
                            }

                            if (type.IsAssignableFrom(keyValuePair.Key))
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
            foreach (var idPair in _ids)
            {
                for (var i = 0; i < idPair.Value.Count; i++)
                {
                    if (idPair.Value[i] == id)
                    {
                        return _names[idPair.Key][i];
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
            foreach (var idPair in _ids)
            {
                for (var i = 0; i < idPair.Value.Count; i++)
                {
                    if (idPair.Value[i] == id)
                    {
                        return _guids[idPair.Key][i];
                    }
                }
            }

            return null;
        }
    }
}

using System;
using System.IO;
using UnityEngine;

namespace Referenceables.Editor
{
    public class ReferenceablesSettings : ScriptableObject
    {
        private static string _filePath;
        private static string FilePath
        {
            get
            {
                if (string.IsNullOrEmpty(_filePath))
                {
                    var rootPath = Directory.GetParent(Application.dataPath);
                    _filePath = Path.Combine(rootPath.FullName, "ProjectSettings/RefrenceablesSettings.json");
                }

                return _filePath;
            }
        }

        private static ReferenceablesSettings _instance;
        public static ReferenceablesSettings Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = CreateInstance<ReferenceablesSettings>();
                    if (File.Exists(FilePath))
                    {
                        var json = File.ReadAllText(FilePath);
                        if (!string.IsNullOrEmpty(json))
                        {
                            JsonUtility.FromJsonOverwrite(json, _instance);
                        }
                    }
                }

                return _instance;
            }
        }

        [Header("Logging")]
        public bool logMessages = true;
        public bool logWarnings = true;
        public bool logErrors = true;
        
        [Header("Duplicates")]
        public bool allowDuplicates = false;

        public void Save()
        {
            var json = JsonUtility.ToJson(_instance, true);
            File.WriteAllText(FilePath, json);
        }
    }
}
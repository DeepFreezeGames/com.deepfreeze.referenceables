using System;
using UnityEngine;

namespace Referenceables.Editor
{
    public class ReferenceableDetailInfo
    {
        public Type Type { get; }
        
        public string Id { get; }
        
        public string Guid { get; }
        
        public string Path { get; }
        
        public ScriptableObject ScriptableObject { get; }
        
        public ReferenceableDetailInfo(Type type, string id, string guid, string path, ScriptableObject scriptableObject)
        {
            Type = type;
            Id = id;
            Guid = guid;
            Path = path;
            ScriptableObject = scriptableObject;
        }
    }
}
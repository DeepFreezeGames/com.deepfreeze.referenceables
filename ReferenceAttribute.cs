using System;
using UnityEngine;

namespace Referenceables
{
    public class ReferenceAttribute : PropertyAttribute
    {
        public Type[] Types { get; private set; }
        public Type[] Excludes { get; private set; }

        public ReferenceAttribute()
        {
            Types = new Type[0];
            Excludes = new Type[0];
        }

        public ReferenceAttribute(params Type[] types)
        {
            Types = types;
            Excludes = new Type[0];
        }
        
        public ReferenceAttribute(Type[] types, Type[] excludes)
        {
            Types = types;
            Excludes = excludes;
        }
    }
}

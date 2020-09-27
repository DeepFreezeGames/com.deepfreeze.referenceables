using System;
using UnityEngine;

namespace Referenceables.Runtime
{
    public class ReferenceableAttribute : Attribute
    {
        public Type ReferenceType { get; private set; }

        public ReferenceableAttribute(Type referenceType)
        {
            ReferenceType = referenceType;
        }
    }
}
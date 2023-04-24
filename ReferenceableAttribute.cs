using System;

namespace Referenceables
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
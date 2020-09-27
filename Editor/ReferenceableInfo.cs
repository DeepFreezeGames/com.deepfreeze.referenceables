using System;

namespace Referenceables.Editor
{
    [Serializable]
    public class ReferenceableInfo
    {
        public Type EntityType { get; }
        public Type AssetType { get; }

        public ReferenceableInfo(Type entityType, Type assetType)
        {
            EntityType = entityType;
            AssetType = assetType;
        }
    }
}
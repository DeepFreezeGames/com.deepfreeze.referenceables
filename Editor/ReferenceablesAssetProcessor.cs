using UnityEditor;

namespace Referenceables.Editor
{
    public class ReferenceablesAssetProcessor : AssetPostprocessor
    {
        private static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths)
        {
            ReferenceableHelper.Refresh();
        }
    }
}

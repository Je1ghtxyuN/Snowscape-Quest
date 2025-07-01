#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

public class MaterialReconnector : AssetPostprocessor
{
    void OnPreprocessModel()
    {
        var importer = assetImporter as ModelImporter;
        if (importer == null) return;

        // 关键设置：强制使用外部材质
        importer.materialLocation = ModelImporterMaterialLocation.External;
        importer.materialName = ModelImporterMaterialName.BasedOnMaterialName;
        importer.materialSearch = ModelImporterMaterialSearch.Everywhere;
    }
}
#endif
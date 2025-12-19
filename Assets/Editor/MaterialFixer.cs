#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

public class MaterialFixer : EditorWindow
{
    [MenuItem("Tools/Fix All Materials")]
    static void Fix()
    {
        // 获取所有材质（包括未激活的）
        var materials = Resources.FindObjectsOfTypeAll<Material>();
        
        foreach(var mat in materials)
        {
            // 跳过Unity内置材质
            if(AssetDatabase.GetAssetPath(mat).StartsWith("Assets"))
            {
                var shader = Shader.Find(mat.shader.name);
                if(shader != null) 
                {
                    Undo.RecordObject(mat, "Fix Material Shader");
                    mat.shader = shader;
                }
                else
                {
                    Debug.LogWarning($"Shader not found: {mat.shader.name}", mat);
                }
            }
        }
        
        Debug.Log($"Fixed {materials.Length} materials");
    }
}
#endif
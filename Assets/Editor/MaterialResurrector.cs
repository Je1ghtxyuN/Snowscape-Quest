#if UNITY_EDITOR
using System.Linq;
using UnityEditor;
using UnityEngine;
using System.Collections.Generic;

public class MaterialResurrector : EditorWindow
{
    [MenuItem("Tools/Resurrect Materials")]
    static void Run() 
    {
        // 修正后的LINQ查询
        var matPaths = AssetDatabase.FindAssets("t:Material")
            .Select(guid => AssetDatabase.GUIDToAssetPath(guid))
            .ToList();
        
        foreach(var path in matPaths) 
        {
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if(mat.shader.name.Contains("Missing")) 
            {
                var shader = Shader.Find("Standard");
                if(shader != null) 
                {
                    Undo.RecordObject(mat, "Fix Material Shader");
                    mat.shader = shader;
                    EditorUtility.SetDirty(mat);
                }
            }
        }
        AssetDatabase.SaveAssets();
    }
}
#endif
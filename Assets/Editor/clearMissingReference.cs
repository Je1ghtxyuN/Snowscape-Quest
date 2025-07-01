
#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

public class ClearMissingReferences
{
    [MenuItem("Tools/Clean Missing Scripts")]
    static void Clean()
    {
           foreach(var go in Resources.FindObjectsOfTypeAll<GameObject>()) {
        var components = go.GetComponents<Component>();
        var serializedObj = new SerializedObject(go);
        bool modified = false;
        
        for(int i = components.Length - 1; i >= 0; i--) {
            if(components[i] == null) {
                serializedObj.FindProperty("m_Component")
                    .DeleteArrayElementAtIndex(i);
                modified = true;
            }
        }
        
        if(modified) serializedObj.ApplyModifiedProperties();
    }
    }
}
#endif
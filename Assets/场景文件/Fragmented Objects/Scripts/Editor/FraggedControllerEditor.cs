using UnityEngine;
using System;
using UnityEditor;
/****************************************
	Copyright Unluck Software	
 	www.chemicalbliss.com																															
*****************************************/
[CustomEditor(typeof(FraggedController))]

[System.Serializable]
public class FraggedControllerEditor: Editor {
    public override void OnInspectorGUI() {
		FraggedController target_cs = (FraggedController)target;
        DrawDefaultInspector();
		
			if(GUILayout.Button("Change Fragment Materials")) {
				target_cs.fragments = target_cs.transform.Find("Fragments");	
			 	target_cs.ChangeMaterials();
			 	target_cs.fragMaterials = null;
			}
		
		if (GUI.changed)	EditorUtility.SetDirty (target_cs);
    }
}
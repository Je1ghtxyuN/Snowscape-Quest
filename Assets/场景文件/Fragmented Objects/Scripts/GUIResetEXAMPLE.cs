using UnityEngine;
using System;


public class GUIResetEXAMPLE:MonoBehaviour{
    public void OnGUI() {
    	GUI.Label(new Rect(20.0f,20.0f,350.0f,18.0f),"Use WASD and mouse to move and shoot");
    	if(GUI.Button(new Rect(20.0f,40.0f,125.0f,18.0f),"Reset All"))
    		ResetAll();	
    }
    
    public void ResetAll() {
    	FraggedController[] frags = FindObjectsOfType(typeof(FraggedController)) as FraggedController[];
    	foreach(FraggedController frag in frags) {
    		frag.ResetFrags();
    	}
    }
}

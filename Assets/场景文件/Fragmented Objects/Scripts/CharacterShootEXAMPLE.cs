using UnityEngine;
using System;


public class CharacterShootEXAMPLE:MonoBehaviour{
    bool shoot;
    
    public void Start() {
    	InvokeRepeating("Shoot", .05f,.05f);
    }
    
    public void Update(){
    	if(Input.GetMouseButtonDown(0)){
    		shoot = true;
    	}else if(Input.GetMouseButtonUp(0)){
    		shoot = false;
    	}
    }
    
    public void Shoot(){
    	if(shoot){
    		RaycastHit hit = new RaycastHit();
    		Ray ray = Camera.main.ViewportPointToRay (new Vector3(0.5f,0.5f,0.0f));
    		if (Physics.Raycast (ray, out hit)){
    		////	SEND A MESSAGE DAMAGING THE OBJECT HIT
    			hit.collider.gameObject.SendMessage("Damage", 1f, SendMessageOptions.DontRequireReceiver);			
    		}
    	}
    }
}
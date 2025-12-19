using UnityEngine;
using System;

[AddComponentMenu("FragChild")]

public class FraggedChild:MonoBehaviour{
    
    int forceMax;
    int forceMin;
    [HideInInspector] 
    public bool fragged;
    
    //// SAVE TRANSFORM INFO
    Vector3 sPos;
    Quaternion sRot;
    Vector3 sScale;
    //// CONTROLLER
    
    protected FraggedController fragControl;
    [HideInInspector] 
    public float hitPoints = 1.0f;
    public bool stickyFrag;
    [HideInInspector] 
    public bool connected = true;
    [HideInInspector] 
    public bool released;
    
    bool checkToggle = true;
    
    public Rigidbody cacheRB;
    
    //// USE THIS FUNCTION TO DAMAGE THE FRAGMENTS SO THEY FALL OFF //// gameObject.SendMessage("Damage", 1f, SendMessageOptions.DontRequireReceiver);
    public void Damage(float damage) {
    		fragMe(fragControl.hitPointDecrease * damage);		
    		if(fragControl.fragAllOnDamage){
    			fragControl.FragAll();
    		}
    }
    
    public void Start() {
    	cacheRB = GetComponent<Rigidbody>();
    	cacheRB.isKinematic = true;
    	if (fragControl == null) fragControl = transform.parent.parent.GetComponent<FraggedController>();
    	GetComponent<Renderer>().enabled = false;
    	fragControl = gameObject.transform.parent.parent.GetComponent<FraggedController>();
    	forceMax = (int)(fragControl.forceMax * fragControl.fragMass);
    	forceMin = (int)(fragControl.forceMin * fragControl.fragMass);
    	sRot = transform.rotation;
    	sPos = transform.position;
    	sScale = transform.localScale;
    	cacheRB.mass = fragControl.fragMass;
    	
    }
    
    
    
    public void checkConnections() {
    		if (!stickyFrag && !this.fragged && !this.connected && (fragControl.stickyTop > 0 || fragControl.stickyBottom > 0)) {
    //			Debug.Log("checkConnections");
    			int counter = 0;
    			Collider[] colls = null;
    			colls = Physics.OverlapSphere(transform.position, fragControl.connectOverlapSphere * fragControl.transform.localScale.x, fragControl.stickyMask);
    			for(int i = 0; i <= colls.Length - 1; i++) {
    				FraggedChild frag = colls[i].transform.GetComponent<FraggedChild>();
    				if (frag != null && !frag.fragged && transform.parent == frag.transform.parent) {
    					if (frag.stickyFrag || frag.connected) {
    						counter++;
    					}
    				}
    			}
    			if (counter >= fragControl.connectedFragments) {
    				connected = true;
    			}
    		}
    	}
    	//frags fracture fragments on Collisions
    public void OnCollisionEnter(Collision collision) {
    	if((fragControl.collideMask.value & 1<<collision.gameObject.layer) == 1<<collision.gameObject.layer){
    		if (this.fragControl.collidefragMagnitude > 0 && collision.relativeVelocity.magnitude > this.fragControl.collidefragMagnitude) {
    		fragMe(collision.relativeVelocity.magnitude * .2f * fragControl.hitPointDecrease);
    		}
    		if (this.fragged && collision.relativeVelocity.magnitude > 1){	
    			fragControl.dustParticles.transform.position = this.transform.position;
    			fragControl.dustParticles.Emit(1);
    			fragControl.fragParticles.transform.position = this.transform.position;
    			fragControl.fragParticles.Emit((int)UnityEngine.Random.Range(fragControl.fragEmitMinMax.x*.5f, fragControl.fragEmitMinMax.y*.5f));
    		}
    	}
    	if(fragControl.disableDelay > 0 && (fragControl.disableMask.value & 1<<collision.gameObject.layer) == 1<<collision.gameObject.layer){
    		Invoke("Disable", fragControl.disableDelay);
    	}
    }
    
    
    
    public void addForce(int fMin,int fMax) {
    	
    	if(!cacheRB.isKinematic && this.cacheRB.velocity.magnitude < 1){
    	float forceX = (float)UnityEngine.Random.Range(fMin, fMax);
    	if (UnityEngine.Random.value > 0.5f) {
    		forceX *= -1.0f;
    	}
    	float forceY = (float)UnityEngine.Random.Range(fMin, fMax);
    	if (UnityEngine.Random.value > 0.5f) {
    		forceY *= -1.0f;
    	}
    	
    	//cacheRB.AddForce(forceX, Random.Range(fMin, fMax), forceY);
    	cacheRB.velocity = new Vector3(forceX, (float)UnityEngine.Random.Range(fMin, fMax), forceY)*.05f;
    	}
    }
    
    public void fragMe(float hitFor) {
    	if ((fragControl.startMesh != null) && fragControl.startMesh.GetComponent<Renderer>().enabled ) {
    		fragControl.startMesh.GetComponent<Renderer>().enabled = false;
    		fragControl.EnableRenderers();
    	}
    	fragControl.ReleaseFrags(false);
    	fragControl.reCounter = 0;
    	
    	if (this.connected ) {		
    		addForce(forceMin, forceMax);
    		if(fragged && checkToggle){
    			checkToggle = !checkToggle;
    			fragControl.checkConnections();
    			this.connected = false;
    		}
    	} else if(hitFor < 200) {
    			addForce((int)(forceMin * .5f), (int)(forceMax * .5f));
    	}
    	
    	if (!fragged) {
    		hitPoints -= hitFor;
    		if (fragControl.fragEnabled && hitPoints < 0) {
    			//Hitpoints lower than 0, fragment is now fragged
    			MeshCollider meshCollider = gameObject.GetComponent<MeshCollider>();
    			if(meshCollider != null) meshCollider.convex = true;
    			fragged = true;
    			if (fragControl.fragParticles != null) {
    				fragControl.fragParticles.transform.position = this.transform.position;
    				if (this.connected) fragControl.fragParticles.Emit((int)UnityEngine.Random.Range(fragControl.fragEmitMinMax.x, fragControl.fragEmitMinMax.y));
    				else fragControl.fragParticles.Emit((int)(UnityEngine.Random.Range(fragControl.fragEmitMinMax.x, fragControl.fragEmitMinMax.y) * .5f));
    			}		
    		//Set fragment scale
    		transform.localScale = sScale * fragControl.fragOffScale;
    		cacheRB.isKinematic = false;
    		released = true;
    		} else if (hitFor < 1 && hitPoints > 0) {
    			if(!this.released){
    				float rotateMultiplier = 1 - hitPoints;
    				gameObject.transform.Rotate(UnityEngine.Random.Range(-fragControl.rotateOnHit, fragControl.rotateOnHit + 1) * rotateMultiplier, 0.0f, UnityEngine.Random.Range(-fragControl.rotateOnHit, fragControl.rotateOnHit + 1) * rotateMultiplier);
    				transform.localEulerAngles = new Vector3((float)Mathf.Clamp((int)transform.localEulerAngles.x, -10, 10), (float)Mathf.Clamp((int)transform.localEulerAngles.y, -10, 10), (float)Mathf.Clamp((int)transform.localEulerAngles.z, -10, 10));
    			}
    			if (fragControl.dustParticles != null) {
    				fragControl.dustParticles.transform.position = this.transform.position;
    				fragControl.dustParticles.Emit(UnityEngine.Random.Range(3, 8));
    			}
    		} else {
    			gameObject.transform.Rotate((float)UnityEngine.Random.Range(-fragControl.rotateOnHit, fragControl.rotateOnHit + 1), 0.0f, 0.0f);
    		}
    	}
    }
    
    public void SpeedCheck(){
    	if(fragged && cacheRB.velocity.sqrMagnitude > fragControl.limitFragmentSpeed){
    		cacheRB.velocity = Vector3.zero;
    	}
    }
    
    public void Disable() {
    	gameObject.GetComponent<Collider>().enabled = false;
    	MeshCollider MC = GetComponent<MeshCollider>();
    	if(MC != null){
    		MC.enabled = false;
    	}
    	cacheRB.isKinematic = true;
    }
    
    public void resetMe() {
    	transform.gameObject.SetActive(true);
    	transform.GetComponent<Renderer>().enabled = false;
    	MeshCollider MC = GetComponent<MeshCollider>();
    	if(MC != null){
    		MC.convex = false;
    		MC.enabled = true;
    	}
    	//Resets transforms
    	transform.position = sPos;
    	transform.rotation = sRot;
    	transform.localScale = sScale;
    	fragged = false;
    	hitPoints = 1.0f;
    	cacheRB.isKinematic = true;
    	connected = true;
    	released = false;
    }
}

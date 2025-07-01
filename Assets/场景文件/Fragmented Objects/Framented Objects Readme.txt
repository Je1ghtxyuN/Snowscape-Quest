
#Fragmenting object:
// To start fragmenting objects the FraggedChild(fragments) has to be damaged.
// Example:
var hit: RaycastHit;
var ray : Ray = Camera.main.ViewportPointToRay (Vector3(0.5,0.5,0));
if (Physics.Raycast (ray, hit)){
	hit.collider.gameObject.SendMessage("Damage", 1f, SendMessageOptions.DontRequireReceiver);			
}
// Check out the CharacterShootExample script to see how it is used with the standard first person character controller.
// This method can also be used in a CollisionEnter function.


#Variables:
// Frag Enabled				- Can frags fall off, set to false for floors and walls where the frags only are suppose to rotate
// Force Max				- Maximum force put on fracture fragments when fragged
// Force Min				- Minimun force put on fracture fragments when fragged
// Frag Off Scale			- Scales fracture fragments after fragged
// Disable Delay			- Disables fracture fragments after fragged, 0 never disable. (seconds)
// Rotate On Hit			- Rotates randomly when hit (random degrees * fracture.hitPoints(0-1))
// Collidefrag Magnitude	- Fracture fragments on collision magnitude (0 disabled, 5 good, 25 max)
// Frag Emit Min Max		- How many fragments to emit from the fragment particle system
// Combine Frags			- Comine frags into a single mesh
// Frag Mass				- Each fracture fragments mass
// HitPoint Decrease 		- Amount decreased from fracture fragment hitPoints(1 =100%) on mouse over or *collide magnitude
// Sticky Top 				- Everything not connected to the bottom fragments frags off
// Sticky Bottom			- Everything not connected to the top fragments frags off
// Connected Fragments		- How many fragments needs to be connected before they break appart
// Connect Overlap Sphere	- Size of sphere connecting fragments
// Combine Meshes Delay		- Combines all fragments to one mesh after last fragged fragment+delay [seconds/10] (performance+++) (negative/zero=don't merge)


Unluck Software
www.chemicalbliss.com
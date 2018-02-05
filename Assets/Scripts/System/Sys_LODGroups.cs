using UnityEngine;
using System.Linq;
using System.Collections.Generic;

/// <summary>  
///  This class holds data about a LOD Group on a instance-based level.
/// </summary>  
[System.Serializable]
public class LODGroup {
	public	int					id							= -1;
	public	GameObject			mainObject					= null;
	public	Vector3 			origin						= Vector3.zero;
	public	bool 				culled						= false;
	public	bool				disableRendererOnly			= true;

	// UpdateByCount is a feature that allows the user to update the list of children if the object's child count changes.
	public	bool 				updateByCount				= false;		
	
	public	int					childCount					= -1;
	public	Renderer[]			childRenderers;
	public	GameObject[]		childObjects;
}

public class Sys_LODGroups : MonoBehaviour {

	public 				float				cullingDistance				= 150.0F;
	public 				bool				disableRendererOnly			= true;
	public 				bool				updateByCount				= false;
	public				Renderer[]			excludeRenderers			= new Renderer[0];
	private				int					i							= 0;

	static public		int					groupCount					= 0;
	private				int					ticket						= 0; // aka ID
	static public		bool				masterExists				= false;
	static public		Sys_LODGroups		instanceMaster				= null;
	public				LODGroup[]			lodGroups;
	static public		Transform			camTrans;

	void Awake(){

		// NOTE: Awake is called even when the script itself is disabled, so we manually have to check
		// if the script is disabled or not. But on the bright side we can delete it if it's not,
		// freeing up some precious memory.
		if(!this.enabled)
			return;

		// NOTE: This makes sure the LODGroup system resets when you load another scene
		if (masterExists) {
			masterExists = false;
			instanceMaster = null;
			camTrans = null;
			groupCount = 0;
		}

		if(transform.childCount > 0){
			// NOTE: At the end of the Awake function, we make every instance of the script pull a "ticket"
			ticket = groupCount++;
		}
		else{
			Debug.LogError("Slave group has no children!");
			this.enabled = false;
		}
	}

	void Start(){

		// NOTE: Because of the way the preceding Awake() is stuctured, any object still running this script
		// HAS to have at least 1 child object. Ineligible objects have already been filtered out.
		bool master = !masterExists;

		// NOTE: this block creates the LODGroup itself and fills in some of the data
		// It could be made into a neat LODGroup constructor call of course, but I think
		// this more easilly readable.
		LODGroup thisGroup = new LODGroup();
		thisGroup.id = ticket;
		thisGroup.mainObject = gameObject;
		thisGroup.origin = transform.position;
		thisGroup.updateByCount = updateByCount;
		thisGroup.disableRendererOnly = disableRendererOnly;

		if(master){ // NOTE: This is going to be the master object
			masterExists = true;
			instanceMaster = this;

			// NOTE: The reason I'm caching this transform is because Camera.main is actually
			// a search type function - VERY expensive.
			camTrans = Camera.main.transform;

			lodGroups = new LODGroup[groupCount];
						
			#if UNITY_EDITOR
			transform.name = "!LODGroup Master";
			#endif
		}
		#if UNITY_EDITOR
		else{
			//transform.name = "!LODGroup Slave " + ticket;
			if(ticket > groupCount){
			//	Debug.LogError("(ID " + lodGroups.Length +  ") can't be created because the array (Length " + lodGroups.Length + ") is not big enough!");
				return;
			}
		}
		#endif

		// NOTE: The master will always register as number 0, because of the way Unity's script priorities work
		// but in theory he can be any number he likes.
		// Slaves however, can't be ID 0. They also can't be the ID of a slave that's already registered

		//if(!master && (ticket == 0))// || lodGroups[ticket] != null))
		//	Debug.LogError("Slave (ID " + ticket + ") is already registered!");

		// NOTE: Grab Arrays
		GrabArrays(thisGroup);

		// NOTE: Do initial cull check
		thisGroup.culled = (Vector3.Distance(camTrans.position, transform.position) < cullingDistance);
		if(thisGroup.culled )
			Cull(thisGroup);

		// NOTE: Write to the array and kill all slaves
		if(!master){
			instanceMaster.lodGroups[ticket] = thisGroup;
			//print ("Slave (ID " + ticket + ") registered and terminated succesfully!");
			Destroy (this);
		}
		else
			lodGroups[ticket] = thisGroup;
	}

	void Update(){

		// NOTE: This basically translates to "if the player is dead" and
		// "if the game is paused and the camera is not in benchmarking mode".
		//if(Player_Life.life < 0f || (Time.timeScale == 0f && Player_Camera.cameraMode != CameraMode.Sleaze))
		//	return;

		// TODO: only do this a maximum of 30 times a second maybe?
		// or better yet, only do a limit number each frame?
		// lodGroups.Length checks every 1/30th or a seconds? something like that?

		// NOTE: This weird While() loop is the way I like to itterate over arrays
		// Why not a foreach loop? Foreach has a bit of an overhead in Mono (IEnumerator.Next), so this is
		// actually significantly faster.
		int i = lodGroups.Length;
		while(i-- > 0){

			#if UNITY_EDITOR
			if(lodGroups[i] == null){
				Debug.LogError("(ID " + i + ") LODGroup does not exist!");
				continue;
			}
			#endif

			// TODO: re-implement the childCount update
			//if(lodGroup.updateByCount && lodGroup.childCount != lodGroup.childCount)
			//	lodGroup = GrabArrays (lodGroup);

			CullCheck(lodGroups[i]);
		}
	}

	/// <summary>  
	///  This functions checks if the group is inside or outisde of the culling area. The LODGroup may be updated if its state has changed.
	/// </summary>  
	private void CullCheck(LODGroup lodGroup){

		// NOTE: enable if needed for debugging
		//print (Vector3.Distance(camTrans.position, lodGroup.origin) + " < " + cullingDistance);

		float dist = Vector3.Distance(camTrans.position, lodGroup.origin);

		if(lodGroup.culled){ // NOTE: the LODGroup we're currently working on is currently culled
			if(dist < cullingDistance)
				Draw (lodGroup);
		}
		else if(dist > cullingDistance)
			Cull (lodGroup);

		// NOTE: Enable for debugging
		/*if(lodGroup.culled)
			print ("(ID " + lodGroup.id + ") PostCull: CULLED");
		else
			print ("(ID " + lodGroup.id + ") PostCull: DRAWING");*/
	}

	/// <summary>  
	///  This function makes sure the object is fully enabled and visible. It does the opposite of the Cull() Function.
	/// </summary>  
	private void Draw(LODGroup lodGroup){
		
		if(!lodGroup.culled)
			return;
		
		//print ("(ID " + lodGroup.id + ") Draw request");
		
		lodGroup.culled = false;
		
		if (disableRendererOnly) {
			int childCount = lodGroup.childRenderers.Length;
			Renderer rend;
			while(childCount-- > 0){
				rend = lodGroup.childRenderers[childCount];
				//if(!rend)
				//	continue;
				//else
				if (!rend.enabled)
					rend.enabled = true;
			}
		}
		else {
			int childCount = lodGroup.childObjects.Length;
			GameObject child;
			while(childCount-- > 0){
				child = lodGroup.childObjects[childCount];
				if (!child.activeInHierarchy)
					child.SetActive (true);
			}
		}
	}

	/// <summary>  
	///  This function makes disabled all subobjects and/or associated renderers. It disables the objects so that they are no longer drawn on the screen. This can be undone by calling the Cull() Function.
	/// </summary>  
	private void Cull(LODGroup lodGroup){

		if(lodGroup.culled)
			return;

		lodGroup.culled = true;

		if(lodGroup.disableRendererOnly){
			int childRendererCount = lodGroup.childRenderers.Length;
			Renderer rend;
			while(childRendererCount > 0){
				childRendererCount --;
				rend = lodGroup.childRenderers[childRendererCount];
				//if(!rend)
				//	continue;
				//else
				if (rend.enabled)
					rend.enabled = false;
			}
		}
		else{
			//print (lodGroup.id);
			//print (lodGroup.childObjects.Length);
			int childObjectCount = lodGroup.childObjects.Length;
			GameObject child;
			while(childObjectCount > 0){
				childObjectCount --;
				child = lodGroup.childObjects[childObjectCount];
				if(child.activeInHierarchy)
					child.SetActive(false);
		
			}
		}
	}

	private void GrabArrays(LODGroup lodGroup){

		// TODO: Only grab one array, not both!

		// TODO: Further optimisation work:
		// GetComponentsInChildren: slow!
		// ToList: slow!
		// List.Add: slow!
		// List.RemoveAt: slow!

		List<Renderer> ChildRends = new List<Renderer>();
		List<GameObject> childObjs = new List <GameObject>();
		
		lodGroup.childCount = lodGroup.mainObject.transform.childCount;
		
		// TODO: excluded array is local, if childcount triggers a re-grab,
		// the exclusion will fuck up so we need to save the excluded objects in the struct as well 
		
		// NOTE: Now remove self and excluded from arrays
		ChildRends = lodGroup.mainObject.GetComponentsInChildren<Renderer>().ToList();
		i = ChildRends.Count;
		Renderer mainRend = lodGroup.mainObject.GetComponent<Renderer>();
		if(mainRend){
			while(i-- > 0){
				if(ChildRends[i] == mainRend){
					// NOTE: this doesn't actually use the exclude array at all, but it turned out I don't actually
					// need it, and having it in there just slows the loop down. The reason I'm keeping the array around
					// is because I may find another use of the exclusion array's markup later down the line and because this
					// code only runs on 64 executables in the first place, I'm not terribly concerned with RAM use at this point.
					ChildRends.RemoveAt(i);
					break; // TODO: remove this after we put exclusions back in
				}
				//else if(ArrayFunction.Contains(lodGroup.childRenderers[i], excludeRenderers)){ //excludeRenderers.Length > 0 && 
				//	lodGroup.childRenderers[i] = null;
				//}
			}
		}
		
		i = lodGroup.mainObject.transform.childCount;
		GameObject currObj;
		while(i-- > 0){
			currObj = lodGroup.mainObject.transform.GetChild(i).gameObject;
			if(currObj != lodGroup.mainObject)
				childObjs.Add(currObj);
		}
		
		if(ChildRends.Count == 0){
			lodGroup.childRenderers = new Renderer[0];
			#if UNITY_EDITOR
			Debug.LogError("Sys_LODGroups Error: No Child Renderers found!");	
			#endif
		}
		else
			lodGroup.childRenderers = ChildRends.ToArray();
		
		if(childObjs.Count == 0)
			lodGroup.childObjects = new GameObject[0];
		else
			lodGroup.childObjects = childObjs.ToArray();
	}
}

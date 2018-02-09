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
	public	GameObject[]		excludedObjects;
}

public class Sys_LODGroups : MonoBehaviour {

	public 				float				cullingDistance				= 150.0F;
	public 				bool				disableRendererOnly			= true;
	public				GameObject[]		excludedObjects				= new GameObject[0];

	// UpdateByCount is a feature that allows the user to update the list of children if the object's child count changes.
	public 				bool				updateByCount				= false;

	// Group instance ID
	private				int					instanceID					= 0;

	static public		bool				masterExists				= false;
	static public		Sys_LODGroups		instanceMaster				= null;
	public				LODGroup[]			lodGroups;

	static public		int					groupCount					= 0;
	static public		Transform			camTrans;

	void Awake () {

		// NOTE: Awake is called even when the script itself is disabled, so we manually have to check
		// if the script is disabled or not.
		if(!this.enabled)
			return;

		// NOTE: This makes sure the LODGroup system resets properly when you load another scene
		if (masterExists) {
			masterExists = false;
			instanceMaster = null;
			camTrans = null;
			groupCount = 0;
		}

		if(transform.childCount > 0) {
			// NOTE: At the end of the Awake() function, we give every instance of the script an instance ID
			instanceID = groupCount++;
		}
		else {
			Debug.LogError("Sys_LODGroups Error: LOD Group has no children!");
			this.enabled = false;
		}
	}

	void Start () {

		// NOTE: Because of the way the preceding Awake() is stuctured, any object still running this script
		// HAS to have at least 1 child object. Ineligible objects have already been filtered out.
		bool master = !masterExists;

		// This block creates the LODGroup itself and fills in some of the data. It could be made into a neat
		// LODGroup constructor call of course, but I think this is superfluous because construction of this
		// object will only ever take place here.
		LODGroup thisGroup = new LODGroup();
		thisGroup.id = instanceID;
		thisGroup.mainObject = gameObject;
		thisGroup.origin = transform.position;
		thisGroup.updateByCount = updateByCount;
		thisGroup.disableRendererOnly = disableRendererOnly;
		thisGroup.excludedObjects = excludedObjects;

		if(master) { // NOTE: This is going to be the master object
			masterExists = true;
			instanceMaster = this;

			// NOTE: The reason I'm caching this transform is because Camera.main is actually
			// a search type function - VERY expensive.
			camTrans = Camera.main.transform;

			lodGroups = new LODGroup[groupCount];
						
			#if UNITY_EDITOR
			transform.name = "!LODGroup Master";
			transform.SetAsFirstSibling();
			#endif
		}
		#if UNITY_EDITOR
		else if(instanceID > groupCount) {
			Debug.LogError("Sys_LODGroups Error: (ID " + lodGroups.Length +  ") can't be created because the array (Length " + lodGroups.Length + ") is not big enough!");
			return;
		}
		#endif

		// NOTE: Grab Arrays
		GrabArrays(thisGroup);

		// NOTE: Do initial cull check
		thisGroup.culled = (Vector3.Distance(camTrans.position, transform.position) < cullingDistance);
		if(thisGroup.culled)
			Cull(thisGroup);

		// NOTE: Write to the array and kill all slaves
		if(!master) {
			instanceMaster.lodGroups[instanceID] = thisGroup;
			//print ("Slave (ID " + ticket + ") registered and terminated succesfully!");
			Destroy (this);
		}
		else
			lodGroups[instanceID] = thisGroup;
	}

	private void Update () {

		// Backwards iteration is faster over large arrays
		LODGroup currentLODGroup;
		for (int currentGroupID = groupCount; currentGroupID --> 0;) {

			currentLODGroup = lodGroups[currentGroupID];
			#if UNITY_EDITOR
			                            if(currentLODGroup == null){
				Debug.LogError("Sys_LODGroups Error: (ID " + currentGroupID + ") LODGroup does not exist!");
				continue;
			}
			#endif

			if(currentLODGroup.updateByCount && currentLODGroup.childCount != currentLODGroup.mainObject.transform.childCount)
				GrabArrays (currentLODGroup);

			CullCheck(currentLODGroup);
		}
	}

	/// <summary>  
	///  This functions checks if the group is inside or outisde of the culling area. The LODGroup may be updated if its state has changed.
	/// </summary>  
	private void CullCheck (LODGroup lodGroup) {

		float distance = Vector3.Distance(camTrans.position, lodGroup.origin);

		if(lodGroup.culled) {
			if(distance < cullingDistance)
				Draw (lodGroup);
		}
		else if(distance > cullingDistance)
			Cull (lodGroup);

		// NOTE: Enable for debugging
		/*print (Vector3.Distance(camTrans.position, lodGroup.origin) + " < " + cullingDistance);
		if(lodGroup.culled)
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
		
		lodGroup.culled = false;
		
		if (disableRendererOnly) {
			Renderer currentRenderer;
			for(int childCount = lodGroup.childRenderers.Length; childCount --> 0;){
				currentRenderer = lodGroup.childRenderers[childCount];
				if (!currentRenderer.enabled)
					currentRenderer.enabled = true;
			}
		}
		else {
			GameObject currentChild;
			for(int childCount = lodGroup.childObjects.Length; childCount --> 0;){
				currentChild = lodGroup.childObjects[childCount];
				if (!currentChild.activeInHierarchy)
					currentChild.SetActive (true);
			}
		}
	}

	/// <summary>  
	///  This function makes disabled all subobjects and/or associated renderers. It disables the objects so that they are no longer drawn on the screen. This can be undone by calling the Cull() Function.
	/// </summary>  
	private void Cull (LODGroup lodGroup) {

		if(lodGroup.culled)
			return;

		lodGroup.culled = true;

		if (disableRendererOnly) {
			Renderer currentRenderer;
			for(int childCount = lodGroup.childRenderers.Length; childCount --> 0;) {
				currentRenderer = lodGroup.childRenderers[childCount];
				if (currentRenderer.enabled)
					currentRenderer.enabled = false;
			}
		}
		else {
			GameObject currentChild;
			for(int childCount = lodGroup.childObjects.Length; childCount --> 0;) {
				currentChild = lodGroup.childObjects[childCount];
				if (currentChild.activeInHierarchy)
					currentChild.SetActive (false);
			}
		}
	}

	private void GrabArrays (LODGroup lodGroup) {

		GameObject mainObject = lodGroup.mainObject;
		int childCount = lodGroup.childCount = mainObject.transform.childCount;
		Renderer mainRenderer = mainObject.GetComponent<Renderer>();

		List<Renderer> ChildRenderers = new List<Renderer>();
		List<GameObject> childObjects = new List <GameObject>();

		mainObject.GetComponentsInChildren<Renderer>(ChildRenderers);

		// Now we are going to exclude self and excluded objects from both arrays
		// -- Renderers are up first -- //
		Renderer currentRenderer;
		for (int currentObjectID = ChildRenderers.Count; currentObjectID --> 0;) {
			currentRenderer = ChildRenderers[currentObjectID];

			// Scan if the current Renderer is the main Object
			if(currentRenderer == mainRenderer) {
				ChildRenderers.RemoveAt(currentObjectID);
				continue;
			}
			// Scan if the current Renderer is excluded from being culled
			for(int currentExcludedRenderer = lodGroup.excludedObjects.Length; currentExcludedRenderer --> 0;) {
				if(currentRenderer.gameObject == lodGroup.excludedObjects[currentExcludedRenderer]) {
					ChildRenderers.RemoveAt(currentObjectID);
					break;
				}
			}
		}

		// -- GameObjects are up now -- //
		// This one's a little tricky - GameObject isn't a Component so you can't use a traditional
		// GetComponent<>() call - we're gonna have to do something cheeky instead.
		GameObject currentObject;
		for (int currentObjectID = childCount; currentObjectID --> 0;) {
				currentObject = mainObject.transform.GetChild(currentObjectID).gameObject;
			
			// Scan if the current Renderer is the main Object
			if(currentObject == mainObject)
				continue;
			// Scan if the current Renderer is excluded from being culled
			for(int currentExcludedObject = lodGroup.excludedObjects.Length; currentExcludedObject --> 0;) {
				if(currentObject == lodGroup.excludedObjects[currentExcludedObject])
					break;
			}

			childObjects.Add(currentObject);
		}
		
		if(ChildRenderers.Count >= 0)
			lodGroup.childRenderers = ChildRenderers.ToArray();
		else {
			lodGroup.childRenderers = new Renderer[0];
			#if UNITY_EDITOR
			Debug.LogError("Sys_LODGroups Error: No Child Renderers found!");	
			#endif
		}
		
		if(childObjects.Count >= 0)
			lodGroup.childObjects = childObjects.ToArray();
		else {
			lodGroup.childObjects = new GameObject[0];
			#if UNITY_EDITOR
			Debug.LogError("Sys_LODGroups Error: No Child GameObjects found!");	
			#endif
		}
	}
}

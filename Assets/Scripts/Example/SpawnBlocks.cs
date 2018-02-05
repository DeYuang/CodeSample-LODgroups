using UnityEngine;
using System.Collections;

[ExecuteInEditMode]
public class SpawnBlocks : MonoBehaviour {

	public	int				size		= 20;
	public	float			spacing		= 7.5f;
	public	GameObject 		prototype	= null;
	public	GameObject		blockPrefab	= null;
	
	private void Start () {
	
		if(!prototype || !blockPrefab)
			return;

		Vector3 currentPosition = prototype.transform.position - new Vector3(spacing * (size/2), 0.0f, spacing * (size/2));

		int currentX = size;
		int currentY = size;
		GameObject currentObject;
		while(currentX --> 0){
			while(currentY --> 0){
				if(currentPosition != prototype.transform.position){
					currentObject = Instantiate(blockPrefab, currentPosition, Quaternion.identity) as GameObject;
					currentObject.transform.parent = prototype.transform.parent;
				}
				currentPosition += new Vector3(spacing, 0.0f, 0.0f);
			}
			currentY = size;
			currentPosition += new Vector3(-spacing * size, 0.0f, spacing);
		}
		Destroy(this);
	}
}

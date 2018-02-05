using UnityEngine;
using System.Collections;

public class UI_DistanceSlider : MonoBehaviour {

	public	CameraMovement		cameraMovement			= null;
	public	float				minDistance				= 14.0f;
	public	float				maxDistance				= 50.0f;
	
	public void OnSliderValueChanged (float value) {
	
		cameraMovement.distance = Mathf.Lerp (minDistance, maxDistance, value);
	}
}

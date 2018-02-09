using UnityEngine;
using System.Collections;
using UnityEngine.UI;

[RequireComponent(typeof(Slider))]
public class UI_DistanceSlider : MonoBehaviour {

	private	Slider				slider					= null;

	public	CameraMovement		cameraMovement			= null;
	public	float				minDistance				= 14.0f;
	public	float				maxDistance				= 50.0f;
	public	float				scrollSpeed				= 0.2f;

	public void OnSliderValueChanged (float value) {
	
		cameraMovement.distance = Mathf.Lerp (minDistance, maxDistance, value);
	}

	private void Awake () {

		slider = GetComponent<Slider> ();
	}

	private void Update () {

		float initialValue = slider.value;
		float value = Mathf.Clamp01(initialValue - (Input.GetAxis("Mouse ScrollWheel") * scrollSpeed));

		if(value != initialValue)
			slider.value = value;
	}
}

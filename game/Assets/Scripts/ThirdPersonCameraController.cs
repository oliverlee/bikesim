using UnityEngine;
using System.Collections;

public class ThirdPersonCameraController : MonoBehaviour {

	public Vector3 ThirdPersonViewPosition;
	public Vector3 FirstPersonViewPosition;

	private bool isItFPView;
	/*public GameObject player;
	private Vector3 offset;*/
	
	// Use this for initialization
	void Start () {
		isItFPView = false;
		//offset = transform.position;
	}

	void Update() {
		float f = Input.GetAxis("Fire2");
		if(f == 1) {
			if(isItFPView) {
				transform.localPosition = ThirdPersonViewPosition;
				isItFPView = false;
			} else {
				transform.localPosition = FirstPersonViewPosition;
				isItFPView = true;
			}
		}
	}

	void LateUpdate () {
		//transform.position = player.transform.position + offset;
	}
}

using UnityEngine;
using System.Collections;

public class ThirdPersonCameraController : MonoBehaviour {

	public Vector3 ThirdPersonView;
	public Vector3 FirstPersonView;

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
				transform.localPosition = ThirdPersonView;
				isItFPView = false;
			} else {
				transform.localPosition = FirstPersonView;
				isItFPView = true;
			}
		}
	}

	void LateUpdate () {
		//transform.position = player.transform.position + offset;
	}
}

using UnityEngine;
using System.Collections;

public class MovingSphereScript : MonoBehaviour {

	public WheelCollider wheelColider;

	// Use this for initialization
	void Start () {
	
	}
	
	// Update is called once per frame
	void Update () {
	
	}

	void FixedUpdate () {

		float h = Input.GetAxis ("Horizontal");
		float v = Input.GetAxis ("Vertical");
		float r = Input.GetAxis ("Rotation");

		if (h != 0) {
			wheelColider.steerAngle = 10* h;		
		}
		
		if (v != 0) {
			wheelColider.motorTorque += 500*v;	
		}
	}
}

using UnityEngine;
using System.Collections;

public class MovingBikeScript : MonoBehaviour {

	public Rigidbody frontWheel;
	public Rigidbody rearWheel;

	// Use this for initialization
	void Start () {
	
	}
	
	// Update is called once per frame
	void Update () {

	}

	void FixedUpdate () {
		
		float h = Input.GetAxis ("Horizontal");
		float v = Input.GetAxis ("Vertical");
		//float r = Input.GetAxis ("Rotation");
		
		if (h != 0) {
				frontWheel.AddTorque(0,h,0);	
		}
		
		if (v != 0) {
				rearWheel.AddRelativeTorque(50*v,0,0);	
		}
	}
}

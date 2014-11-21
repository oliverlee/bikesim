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
		
		if (h > 0 && frontWheel.rotation.y < 0.5 || h < 0 && frontWheel.rotation.y > -0.5 ) {
				frontWheel.AddTorque(0,h/5,0);	
		}

		//Debug.Log(frontWheel.rotation);
		
		if (v != 0) {
				rearWheel.AddRelativeTorque(50000000*v,0,0);	
		}
	}
}

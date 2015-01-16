using UnityEngine;
using System.Collections;

public class OpponentAI : MonoBehaviour {

	
	// Fix a range how early u want your enemy detect the obstacle.
	private int range;
	private float speed ;
	private bool isThereAnyThing = false;
	
	// Specify the target for the enemy.
	private Transform target;
	//private Transform xform;
	private float rotationSpeed ;
	private RaycastHit hit;
	// Use this for initialization
	void Start ()
	{
		target = (GameObject.Find ("RaceGate")).transform;
		//xform = (GameObject.Find("Opponent")).transform;
		range = 2;
		speed = GameObject.Find ("Bike").GetComponent<BikePhysicsScript> ().GetSpeed () * 0.1f;
		rotationSpeed = 2.0f;
	}
	
	// Update is called once per frame
	void Update ()
	{
		if (MenuSelection.state != GameState.Playing)
			return;

		target = (GameObject.Find("RaceGate")).transform;
		//float speedDiff = GameObject.Find ("Bike").GetComponent<BikePhysicsScript> ().GetSpeed () - speed;
		
		//if (speedDiff < 0.0f)
		//	speed = speed;
		//else
		//	speed = speed + speedDiff;
		speed = 3f + (GeneralController.score / 200f);

		Vector3 relativePos = target.position - transform.position;
		Quaternion rotation = Quaternion.LookRotation (relativePos);

		//set roll
		//Vector3 eulerDiff = transform.rotation.eulerAngles - rotation.eulerAngles;
		//float angle = (eulerDiff.y + 360) % 360;
		//Debug.Log ("Euler diff: "+angle);
		
		transform.rotation = Quaternion.Slerp (transform.rotation, rotation, Time.deltaTime);
		
		// Enemy translate in forward direction.
		transform.Translate (Vector3.forward * Time.deltaTime * speed);
		
		//Use Phyics.RayCast to detect the obstacle
		if (Physics.Raycast (transform.position + transform.right, transform.forward, out hit, range))
		{
			if (hit.transform == (GameObject.Find("Bike")).transform) {
				transform.Translate(Vector3.left * Time.deltaTime * speed);
			}
		}
		if (Physics.Raycast (transform.position - transform.right, transform.forward, out hit, range))
		{
			if (hit.transform == (GameObject.Find("Bike")).transform) {
				transform.Translate(Vector3.right * Time.deltaTime * speed);
			}
		}

		//Debug.DrawRay (xform.position + (xform.right ), - xform.forward * 20, Color.yellow);
		//Debug.DrawRay (xform.position - (xform.right), xform.forward * 20, Color.yellow);
	}
}

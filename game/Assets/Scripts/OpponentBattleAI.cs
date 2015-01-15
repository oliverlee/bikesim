using UnityEngine;
using System.Collections;

public class OpponentBattleAI : MonoBehaviour {
	
	
	// Fix a range how early u want your enemy detect the obstacle.
	private int range;
	private float speed ;
	private bool isThereAnyThing = false;
	
	// Specify the target for the enemy.
	private Transform target;
	private Transform xform;
	private float rotationSpeed ;
	private RaycastHit hit;
	// Use this for initialization
	void Start ()
	{
		xform = (GameObject.Find("Opponent")).transform;
		range = 2;
		speed = 2.0f;
	}
	
	// Update is called once per frame
	void Update ()
	{
		if (MenuSelection.state != GameState.Playing)
			return;
		Vector3 vector = Quaternion.Euler(0, xform.rotation.y, 0) * Quaternion.Euler(xform.rotation.eulerAngles) * Vector3.forward;

		target.position = xform.position + vector;
		float speedDiff = GameObject.Find ("Bike").GetComponent<BikePhysicsScript> ().GetSpeed () - speed;
		
		if (speedDiff < 0.0f)
			speed = speed;
		else
			speed = speed + speedDiff;
		
		Vector3 relativePos = target.position - xform.position;
		Quaternion rotation = Quaternion.LookRotation (relativePos);
		xform.rotation = Quaternion.Slerp (xform.rotation, rotation, Time.deltaTime);
		
		// Enemy translate in forward direction.
		xform.Translate (Vector3.forward * Time.deltaTime * speed);
		
		//Checking for any Obstacle in front.
		Transform leftRay = xform;
		Transform rightRay = xform;
		
		//Use Phyics.RayCast to detect the obstacle
		
		if (Physics.Raycast (leftRay.position + xform.right, xform.forward,out hit, range))
		{
			if (hit.transform == (GameObject.Find("Bike")).transform) {
				xform.Translate(Vector3.left * Time.deltaTime * speed);
			}
		}
		
		if (Physics.Raycast (rightRay.position - xform.right, xform.forward,out hit, range))
		{
			if (hit.transform == (GameObject.Find("Bike")).transform) {
				xform.Translate(Vector3.right * Time.deltaTime * speed);
			}
		}
		
		Debug.DrawRay (xform.position + (xform.right ), - xform.forward * 20, Color.yellow);
		
		Debug.DrawRay (xform.position - (xform.right), xform.forward * 20, Color.yellow);
	}
}


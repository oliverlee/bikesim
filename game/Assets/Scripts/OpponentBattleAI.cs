using UnityEngine;
using System.Collections;

public class OpponentBattleAI : MonoBehaviour {
	
	
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
		//xform = (GameObject.Find("Opponent")).transform;
		//xform = transform;
		range = 2;
		speed = 2.0f;
		target = transform;
		target.position = Vector3.forward;
	}
	
	// Update is called once per frame
	void Update ()
	{
		if (MenuSelection.state != GameState.Playing)
			return;
		Vector3 vector = Quaternion.Euler(0, transform.rotation.y, 0) * Quaternion.Euler(transform.rotation.eulerAngles) * Vector3.forward;

		target.position = transform.position + vector;
		/*float speedDiff = GameObject.Find ("Bike").GetComponent<BikePhysicsScript> ().GetSpeed () - speed;
		
		if (speedDiff < 0.0f)
			speed = speed;
		else
			speed = speed + speedDiff;*/
		speed = 1f; //take a steady speed
		
		Vector3 relativePos = target.position - transform.position;
		Quaternion rotation = Quaternion.LookRotation (relativePos);
		transform.rotation = Quaternion.Slerp (transform.rotation, rotation, Time.deltaTime);
		
		// Enemy translate in forward direction.
		transform.Translate (Vector3.forward * Time.deltaTime * speed);
		
		//Checking for any Obstacle in front.
		Transform leftRay = transform;
		Transform rightRay = transform;
		
		//Use Phyics.RayCast to detect the obstacle
		
		if (Physics.Raycast (leftRay.position + transform.right, transform.forward, out hit, range))
		{
			if (hit.transform == (GameObject.Find("Bike")).transform) {
				transform.Translate(Vector3.left * Time.deltaTime * speed);
			}
		}
		
		if (Physics.Raycast (rightRay.position - transform.right, transform.forward, out hit, range))
		{
			if (hit.transform == (GameObject.Find("Bike")).transform) {
				transform.Translate(Vector3.right * Time.deltaTime * speed);
			}
		}
		//Debug.DrawRay (transform.position + (transform.right ), - transform.forward * 20, Color.yellow);
		//Debug.DrawRay (transform.position - (transform.right), transform.forward * 20, Color.yellow);
	}
}


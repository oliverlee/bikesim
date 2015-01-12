using UnityEngine;
using System.Collections;

public class OpponentAI : MonoBehaviour
{
	
	public Transform waypoint;
	public Vector3 waypointPos;
	private float damping = 0.1f;
	public bool loop = false;
	private float speed;
	public bool faceHeading = true;
	
	private Vector3 currentHeading,targetHeading;
	private int targetwaypoint;
	private Transform xform;
	private bool useRigidbody;
	private Rigidbody rigidmember;
	
	// Use this for initialization
	protected void Start ()
	{	
		speed = GameObject.Find ("Bike").GetComponent<BikePhysicsScript> ().GetSpeed () * 0.1f;
		waypoint = (GameObject.Find("Gate")).transform;
		xform = (GameObject.Find("Opponent")).transform;
		currentHeading = xform.forward;

		if(waypoint==null)
		{
			Debug.Log("No waypoints on "+name);
			enabled = false;
		}
		if(rigidbody!=null)
		{
			useRigidbody = true;
			rigidmember = rigidbody;
		}
		else
		{
			useRigidbody = false;
		}
	}
	
	
	// calculates a new heading
	protected void FixedUpdate ()
	{
        if (MenuSelection.state != GameState.Playing)
            return;

		targetHeading = waypointPos - xform.position;
		
		currentHeading = Vector3.Lerp(currentHeading,targetHeading,Time.deltaTime*damping);
	}

	// moves us along current heading
	protected void Update()
	{
        if (MenuSelection.state != GameState.Playing)
            return;

		waypointPos = (GameObject.Find("Gate")).transform.position;
		float speedDiff = GameObject.Find ("Bike").GetComponent<BikePhysicsScript> ().GetSpeed () - speed;

		if (speedDiff < 0.0f)
			speed = speed;
		else
			speed = speed + 0.01f;

		if(useRigidbody)
			rigidmember.velocity = currentHeading * speed;
		else
			xform.position += currentHeading * Time.deltaTime * speed;
		if(faceHeading)
			xform.LookAt(xform.position+currentHeading);

	}
	
}
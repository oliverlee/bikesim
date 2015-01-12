using UnityEngine;
using System.Collections;

public class OpponentAI : MonoBehaviour
{
	
	public Transform waypoint;
	public Vector3 waypointPos;
	public float waypointRadius = 1.5f;
	public float damping = 0.3f;
	public bool loop = false;
	public float speed = 0.5f;
	public bool faceHeading = true;
	
	private Vector3 currentHeading,targetHeading;
	private int targetwaypoint;
	private Transform xform;
	private bool useRigidbody;
	private Rigidbody rigidmember;
	
	// Use this for initialization
	protected void Start ()
	{	
		waypoint = (GameObject.Find("Gate")).transform;
		xform = transform;
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
		
		currentHeading = Vector3.Lerp(currentHeading,targetHeading,damping*Time.deltaTime);
	}

	// moves us along current heading
	protected void Update()
	{
        if (MenuSelection.state != GameState.Playing)
            return;

		if (waypoint == (GameObject.Find ("Gate")).transform)
			waypointPos = waypoint.position;
		else
			waypointPos = waypoint.position;

		//waypointPos = Random
		if(useRigidbody)
			rigidmember.velocity = currentHeading * speed;
		else
			xform.position +=currentHeading * Time.deltaTime * speed;
		if(faceHeading)
			xform.LookAt(xform.position+currentHeading);
		
		if(Vector3.Distance(xform.position,waypointPos)<=waypointRadius)
		{
			if(!loop)
				enabled = false;
		}
		//transform.position = Vector3.MoveTowards(transform.position, waypoint.position, speed * Time.deltaTime);
	}
	
}
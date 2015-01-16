using UnityEngine;
using System.Collections;

public class OpponentBattleAI : MonoBehaviour {
	
	
	// Fix a range how early u want your enemy detect the obstacle.
	private float range;
	private float speed ;
	
	// Specify the target for the enemy.
	private Transform target;
	//private Transform xform;
	public float rotationSpeed;
	private RaycastHit hit;
	// Use this for initialization
	void Start ()
	{
		//xform = (GameObject.Find("Opponent")).transform;
		//xform = transform;
		rotationSpeed = 10f;
		range = 2f;
		speed = 3.0f;

	}
	
	// Update is called once per frame
	void Update ()
	{
		if (MenuSelection.state != GameState.Playing)
			return;

		// Enemy translate in forward direction.

		//Checking for any Obstacle in front.

		//Use Phyics.RayCast to detect the obstacle
		if (Physics.Raycast (transform.position + 5*transform.forward, transform.forward, out hit, range))
		{
			if (!hit.transform.gameObject.name.Equals("Hexagon(Clone)"))
			{
				transform.Rotate(Vector3.up * rotationSpeed);
				Debug.Log (hit.distance);
			}
			else
				transform.position = transform.position + transform.forward * Time.deltaTime * speed;
		}
		else
			transform.position = transform.position + transform.forward * Time.deltaTime * speed;
		

	}
}


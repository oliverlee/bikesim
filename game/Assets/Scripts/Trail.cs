using UnityEngine;
using System.Collections;

public class Trail : MonoBehaviour {
	public Transform rearWheel;


	private Vector3[] lastPositions = new Vector3[2];
	// Use this for initialization
	void Start () {
		lastPositions[1] = rearWheel.position;
	}
	
	// Update is called once per frame
	void Update () {
		if(Vector3.Distance(rearWheel.transform.position,lastPositions[1]) > 0.5f)
		{
			CreateBox();
			lastPositions[0] = lastPositions[1];
			lastPositions[1] = rearWheel.position;
		}
	}

	void CreateBox()
	{
		var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
		//cube.AddComponent(Rigidbody);


		cube.transform.localScale = new Vector3 (0.2f, 1, 2*Vector3.Distance(lastPositions[0], lastPositions[1]) + 0.1f);
		cube.transform.position = lastPositions[0];
		cube.transform.LookAt (lastPositions[1], Vector3.up);
	}


}

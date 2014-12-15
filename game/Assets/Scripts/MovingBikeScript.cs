using UnityEngine;
using System.Collections;

public class MovingBikeScript : MonoBehaviour {
	
	public Rigidbody frontWheel;
	public Rigidbody rearWheel;
	public Rigidbody frame;
	
	public GUIText rotationText;
	public GUIText speedText;
	
	public Transform testSphere;
	public Transform testSphereRear;
	
	private float rotation = 0;
	private float rotationUnit = 0.5f;
	private Vector3[] lastPositions = new Vector3[2];
	// Use this for initialization
	void Start () {
		rotation = 0;
		lastPositions[1] = rearWheel.transform.position;
	}
	
	void Update () {
	}
	
	void FixedUpdate () {
		
		float h = Input.GetAxis ("Horizontal");
		float v = Input.GetAxis ("Vertical");
		//float r = Input.GetAxis ("Rotation");
		
		/*if (h > 0 && frontWheel.rotation.y < 0.5 || h < 0 && frontWheel.rotation.y > -0.5 ) {
				frontWheel.AddTorque(0,h/5,0);	
		}*/
		
		//Debug.Log (frontWheel.rotation);
		
		if(h != 0) {
			if (h > 0) {
				if(rotation > 0) {
					rotation += 1*rotationUnit;
				} else {
					rotation += 3*rotationUnit;
				}
			} else {
				if(rotation < 0) {
					rotation -= 1*rotationUnit;
				} else {
					rotation -= 3*rotationUnit;
				}
			}
		}
		
		if(Vector3.Distance(rearWheel.transform.position,lastPositions[1]) > 0.5f)
		{
			CreateBox();
			lastPositions[0] = lastPositions[1];
			lastPositions[1] = rearWheel.transform.position;
		}
		
		//Vector3 vectorBike = Quaternion.Euler (frame.rotation.eulerAngles) * Vector3.forward;
		Vector3 vector = Quaternion.Euler(0,rotation,0) * Quaternion.Euler(frame.rotation.eulerAngles) * Vector3.forward;
		
		//testSphere.position = frontWheel.transform.position + vector;
		
		//testSphereRear.position = frontWheel.transform.position + vectorBike;
		
		frontWheel.rotation = Quaternion.LookRotation(vector);
		
		if (v != 0) {
			rearWheel.AddRelativeTorque(50000000*v,0,0);	
		}
	}
	
	void LateUpdate () {
		rotationText.text = "Rotation : " + rotation + "º";
		speedText.text = "Speed : " + frame.rigidbody.velocity.magnitude.ToString();
	}
	
	void OnCollisionEnter(Collision col) //This does not work
	{
		if (col.gameObject.name.Equals ("Box"))
			Debug.Log ("You lost the game");
	}
	
	void CreateBox()
	{
		var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
		//cube.AddComponent(Rigidbody);
		
		cube.transform.localScale = new Vector3 (0.2f, 1, 0.6f);
		cube.transform.position = lastPositions[0];
		cube.transform.LookAt (lastPositions[1], Vector3.up);
	}
}

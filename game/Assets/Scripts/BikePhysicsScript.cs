using UnityEngine;
using System.Collections;

public class BikePhysicsScript : MonoBehaviour {

	public GameObject centerOfMass;
	public GameObject frontWheel;
	public GameObject rearWheel;

	public TextGUIScript TextGUI;

	private float forkRotation;
	private float speed;
	//private float bikeRotation;
	//private float currentPosition;
	//private float bikeRollingAngle;

	private float rotationUnit = 0.5f;
	private float speedUnit = 0.1f;

	// Use this for initialization
	void Start () {
		forkRotation = 0;
		speed = 0;
	}
	
	// Update is called once per frame
	void FixedUpdate () {

		float h = Input.GetAxis ("Horizontal");
		float v = Input.GetAxis ("Vertical");

		UpdateRotation (h);
		UpdateSpeed (v);


		// Moving and rotation part
		float angleSpeed = Mathf.Abs (forkRotation);

		Vector3 vector = Quaternion.Euler (0, this.transform.rotation.y, 0) * Quaternion.Euler (this.transform.rotation.eulerAngles) * Vector3.forward;

		Quaternion angle = Quaternion.Euler (0, forkRotation, 0) * this.transform.rotation;

		this.transform.position = Vector3.MoveTowards(this.transform.position,
		                                              this.transform.position + vector,
		                                              speed*Time.deltaTime);

		this.transform.rotation = Quaternion.RotateTowards(this.transform.rotation,
		                                                   angle,
		                                                   angleSpeed*Time.deltaTime);

		//Rolling and gravity part
		ApplyGravity ();
		ApplyInstability ();
	}

	void ApplyGravity() {

		if(frontWheel.transform.position.y != 0) {
			Vector3 vector;
			if(frontWheel.transform.position.y <0) {
				vector = Vector3.up;
			} else {
				vector = Vector3.down;
			}
			vector *= Mathf.Abs (frontWheel.transform.position.y);
			this.transform.position = Vector3.MoveTowards(this.transform.position,
			                                              this.transform.position + vector,
			                                              2*Time.deltaTime);
		}
	}

	void ApplyInstability() {

		if (this.transform.rotation.eulerAngles.z != 0) {
			Vector3 angleEuler = this.transform.rotation.eulerAngles;
			if(this.transform.rotation.eulerAngles.z <0) {
				angleEuler.z -= 1;
			} else {
				angleEuler.z += 1;
			}
			Quaternion angle = Quaternion.Euler(angleEuler);

			this.transform.rotation = Quaternion.RotateTowards(this.transform.rotation,
			                                                   angle,
			                                              2*Time.deltaTime);
		}
	}

	void UpdateRotation(float rot) {
		if(rot != 0) {
			if (rot > 0) {
				if(forkRotation > 0) {
					forkRotation += 1*rotationUnit;
				} else {
					forkRotation += 3*rotationUnit;
				}
			} else {
				if(forkRotation < 0) {
					forkRotation -= 1*rotationUnit;
				} else {
					forkRotation -= 3*rotationUnit;
				}
			}
		}
	}

	void UpdateSpeed(float force) {
		if(force != 0) {
			speed += force*speedUnit;
		}
	}

	void LateUpdate () {
		TextGUI.UpdateBikeValuesText (forkRotation, speed);
	}
}

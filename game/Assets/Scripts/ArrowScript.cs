using UnityEngine;
using System.Collections;

public class ArrowScript : MonoBehaviour {
	private GameObject gate; 
	public GameObject camera; 
	// Use this for initialization
	void Start () {

	}
	
	// Update is called once per frame
	void FixedUpdate() {
		gate = GameObject.Find("BattleGate");
		
		if (gate != null) {
						if (computeAngle (gate) > 60) {
				
								this.transform.LookAt (gate.transform);
								//transform.rotation = Quaternion.Euler(25, transform.rotation.y, transform.rotation.z);
								this.gameObject.transform.GetChild (0).renderer.enabled = true;
								this.gameObject.transform.GetChild (1).renderer.enabled = true;
				
				
						} else {
								Debug.Log (computeAngle (gate));
								this.gameObject.transform.GetChild (0).renderer.enabled = false;
								this.gameObject.transform.GetChild (1).renderer.enabled = false;
						}
				} else
						GameObject.Destroy (this.gameObject);
				
	}

	float computeAngle (GameObject obj)
	{
		float angle = Vector3.Angle (camera.transform.forward, obj.transform.position - camera.transform.position);

		return angle;
	}
}

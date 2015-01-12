using UnityEngine;
using System.Collections;

public class ArrowScript : MonoBehaviour {
	private GameObject gate; 
	public GameObject camera; 

	public GameObject picLeft;
	public GameObject picRight;
	public GameObject picForward;

	private bool displayingPics;
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
				string texture = "Assets/Textures/div2.png";

								Texture2D myTexture = (Texture2D) Resources.LoadAssetAtPath(texture, typeof(Texture2D));
								Graphics.DrawTexture(new Rect(0,0,100,100),myTexture); 
//				if (!displayingPics)
//					StartCoroutine("suggestTurnLeft");
				
						} else {
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
	IEnumerator suggestTurnLeft() {
		displayingPics = true;
		picForward.SetActive (true);
		yield return new WaitForSeconds (3);
		picForward.SetActive (false);
		picLeft.SetActive (true);
		yield return new WaitForSeconds (3);
		picLeft.SetActive (false);
		yield return new WaitForSeconds (3);
		displayingPics = false;
		
		
	}
}

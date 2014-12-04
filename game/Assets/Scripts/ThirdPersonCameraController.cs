using UnityEngine;
using System.Collections;

public class ThirdPersonCameraController : MonoBehaviour {

	public GameObject player;
	private Vector3 offset;

	// Use this for initialization
	void Start () {
		offset = transform.position;
	}
	
	void LateUpdate () {
		transform.position = player.transform.position + offset;
	}
}

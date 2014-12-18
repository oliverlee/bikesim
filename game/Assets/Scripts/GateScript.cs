using UnityEngine;
using System.Collections;

public class GateScript : MonoBehaviour {
	private BoxCollider coll;
	private Coordinates coords;
	private int rotation = 0;

	// Use this for initialization
	void Start () {
		coll = gameObject.GetComponent<BoxCollider>();
		coords = TileManager.PosToCoordinates(gameObject.transform.position);
	}
	
	// Update is called once per frame
	void Update () {

	}

	private void MoveGate(int relative_x, int relative_z, int relative_rotation){
		coords.X += relative_x;
		coords.Z += relative_z;
		rotation = (rotation + relative_rotation) % 360;
		gameObject.transform.position = TileManager.CoordinatesToPos(coords);
		gameObject.transform.eulerAngles = Vector3.up * (rotation);
	}

	void OnTriggerEnter(Collider other){
		if (other.gameObject.tag.Equals("Player")){
			GeneralController.score += 100;
			int angle_step = Random.Range(-2, 2);
			Vector3 relative_movement = TileManager.CoordinatesToPos(5*angle_step, 10);
			relative_movement = Quaternion.Euler(0, rotation, 0) * relative_movement;
			Coordinates relative_coords = TileManager.PosToCoordinates(relative_movement);
			MoveGate(relative_coords.X, relative_coords.Z, angle_step*30);
		}
	}
}

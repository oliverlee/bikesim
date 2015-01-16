using UnityEngine;
using System.Collections;

public class GateScript : MonoBehaviour {
	private Coordinates coords;
	private int rotation = 0;
	private Vector3 defaultPos;
	private Quaternion defaultRot;
	private bool flag = false;
	public GameObject opponent;
	private GameObject bgate;

	// Use this for initialization
	void Start () {
		coords = TileManager.PosToCoordinates(gameObject.transform.position);
		defaultPos = transform.position;
		defaultRot = transform.rotation;
		bgate = GameObject.Find ("BattleGate");
	}

	// Update is called once per frame
	void Update () {
		//pos = (GameObject.Find("Bike")).transform.position;
		//pos.x = pos.x + 2;
		//rot = Quaternion.identity;
	}

	private void MoveGate(int relative_x, int relative_z, int relative_rotation){
		coords.X += relative_x;
		coords.Z += relative_z;
		rotation = (rotation + relative_rotation) % 360;
		gameObject.transform.position = TileManager.CoordinatesToPos(coords);
		gameObject.transform.eulerAngles = Vector3.up * (rotation);
	}

	void OnTriggerEnter(Collider other){
		if (other.gameObject.name.Equals ("Opponent")) {
			Destroy (other.gameObject);
			GeneralController.addScoreRace(""+GeneralController.score);
			MenuSelection.state = GameState.Highscores;
		}
		if (other.gameObject.tag.Equals("Player")){
			GeneralController.score += 100;
			int angle_step = Random.Range(-2, 2);
			Vector3 relative_movement = TileManager.CoordinatesToPos(5*angle_step, 10);
			relative_movement = Quaternion.Euler(0, rotation, 0) * relative_movement;
			Coordinates relative_coords = TileManager.PosToCoordinates(relative_movement);
			MoveGate(relative_coords.X, relative_coords.Z, angle_step*30);
			if (flag == false) { 
				var opp = Instantiate(opponent, defaultPos+Vector3.right, defaultRot);
				opp.name = "Opponent";
				flag = true;
			}
		}
		bgate.SetActive(false);
	}

	public void Reset() {
		flag = false;
		transform.position = defaultPos;
		transform.rotation = defaultRot;
		rotation = 0;
		coords = TileManager.PosToCoordinates(defaultPos);
	}
}

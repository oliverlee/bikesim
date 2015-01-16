using UnityEngine;
using System.Collections;

public class GateBattleScript : MonoBehaviour {
	private BoxCollider coll;
	private Coordinates coords;
	private TileManager tileManager;
	private bool flag = false;
	public GameObject opponent;

	// Use this for initialization
	void Start () {
		coll = gameObject.GetComponent<BoxCollider>();
		coords = TileManager.PosToCoordinates(gameObject.transform.position);
		tileManager = GameObject.Find ("Third Person Camera").GetComponent<TileManager> ();
	}
	
	// Update is called once per frame
	void Update () {

	}

	void OnTriggerEnter(Collider other){
		if (other.gameObject.tag.Equals("Player")){
			tileManager.CreateArena();
			if (flag == false) { 
				var opp = Instantiate(opponent, transform.position+5*Vector3.right, Quaternion.identity);
				opp.name = "BattleOpponent";
				flag = true;
			}
			GameObject gate = GameObject.Find ("RaceGate");
			gate.SetActive(false);
			gameObject.SetActive(false);
		}
	}

	public void Reset() {
		flag = false;
	}
}

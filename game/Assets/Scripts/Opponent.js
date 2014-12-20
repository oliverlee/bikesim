#pragma strict

var opponent : GameObject;
var waypoint1 : GameObject;
var waypoint2 : GameObject;
var gate : GameObject;

var pos : Vector3;
var rot : Quaternion;
var flag : boolean;
var timer : float;

function Start () {
	pos = transform.position;
	pos.x = pos.x + 2;
	rot = Quaternion.identity;
	flag = false;
	timer = 0.2f;
}

function Update () {
	if (Input.GetKeyDown(KeyCode.Space) && flag == false){
		var opp = Instantiate(opponent, pos, rot);
		opp.name = "Opponent";
	}
	if (flag == false) {
		var w1 = Instantiate(waypoint1, opponent.transform.position, rot);
		var w2 = Instantiate(waypoint2, gate.transform.position, rot);
		w1.name = "Waypoint0";
		w2.name = "Waypoint1";
		flag = true;
	}
	timer = Time.deltaTime;
	if (flag == true) {
		Destroy(w1);
		Destroy(w2);
		flag = false;	
	}
		
}
#pragma strict

var opponent : GameObject;
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
		flag = true;
	}
	timer = Time.deltaTime;		
}
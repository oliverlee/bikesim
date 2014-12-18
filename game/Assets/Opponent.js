#pragma strict

var opponent : GameObject;
var waypoint1 : GameObject;
var waypoint2 : GameObject;

var pos : Vector3;
var rot : Quaternion;

function Start () {
	pos = transform.position;
	pos.z = pos.z + 2;
	rot = Quaternion.identity;
}

function Update () {
	if (Input.GetMouseButtonDown(1)){
		Instantiate(opponent, pos, rot);
		pos.y = 0;
		pos.x = pos.x + 2;
		var w1 = Instantiate(waypoint1, pos, rot);
		var w2 = Instantiate(waypoint2, pos + new Vector3(5,0,0), rot);
		w1.name = "Waypoint0";
		w2.name = "Waypoint1";
		}
}
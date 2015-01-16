using UnityEngine;
using System.Collections;

public class DieScript : MonoBehaviour {

	bool youHaveDied = false;

	// Use this for initialization
	void Start () {
	
	}
	
	// Update is called once per frame
	void Update () {
	
	}

	void OnGUI()
	{
		if (youHaveDied) {
			GUI.skin.label.alignment = TextAnchor.MiddleCenter;
			GUI.Label (new Rect (0, 0, Screen.width, Screen.height), "WASTED");
			GUI.skin.label.fontSize = 100;
			//GUI.skin.label.alignment = TextAnchor.MiddleCenter;
		}
	}

	public void setYouHaveDied (bool flag)
	{
		youHaveDied = flag;
	}
}

using UnityEngine;
using System.Collections;

public class GeneralController : MonoBehaviour {

	public static int score;
	public GUIText scoreText;

	// Use this for initialization
	void Start () {
		score = 0;
	}
	
	// Update is called once per frame
	void Update () {
		
	}

	void LateUpdate(){
		scoreText.text = "score: " + score;
	}
}

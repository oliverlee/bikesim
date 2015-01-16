using UnityEngine;
using System.Collections;

public class GeneralController : MonoBehaviour {

	public static int score;
	//public GUIText scoreText;

	public static bool battleModeActive = false;
	public static float battleModeStartTime = 0;
	public static string[] scoresRace = new string[5], scoresBattle = new string[5];

	// Use this for initialization
	void Start () {
		score = 0;
	}
	
	// Update is called once per frame
	void Update () {
		
	}

	/*void LateUpdate(){
		scoreText.text = "score: " + score;
	}*/

	public static void addScoreRace(string s) {
		for (int i = 3; i >= 0; i--) {
			scoresRace[i+1] = scoresRace[i];
		}
		scoresRace [0] = s;
	}

	public static void addScoreBattle(string s) {
		for (int i = 3; i >= 0; i--) {
			scoresBattle[i+1] = scoresBattle[i];
		}
		scoresBattle [0] = s;
	}
}

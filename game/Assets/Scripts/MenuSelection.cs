using UnityEngine;
using System.Collections;
using System;
using System.Collections.Generic;
using System.Linq;

public enum GameState { Playing, Mainmenu, Howtoplay, Options, Highscores, Credits }
public enum SubGameState { None, Free, Racing, Battle }

public class MenuSelection : MonoBehaviour
{
    public static GameState state = GameState.Mainmenu;
	public static SubGameState substate = SubGameState.Free;
    public Texture2D background, button;
	public string currentscore = "";

    private bool transitioning = false;
    private float transitionProgress = 0;
    private int activeIndex = 0;
    private List<Rect> menu3;
    private string[] menuElements = new string[] { "Start game", "How to play", "Options", "High scores", "Credits", "Bike settings" };
    private float menuAngleSnap = 30f;
    private float menuAngle = 0, menuAngleSource = 0, menuAngleTarget = 0;
    private GUIStyle backgrnd;

    // Use this for initialization
    void Start()
    {
        menu3 = new List<Rect>();
        for (int i = 0; i < menuElements.Length; i++)
        {
            int ind = ((i + (menuElements.Length / 2)) % menuElements.Length) - (menuElements.Length / 2);
            float angle = ind * menuAngleSnap * Mathf.Deg2Rad;
            menu3.Add(buttonAtAngle(angle));
        }
        backgrnd = new GUIStyle();
        backgrnd.fixedHeight = 0;
        backgrnd.fixedWidth = 0;
        backgrnd.stretchHeight = true;
        backgrnd.stretchWidth = true;
        backgrnd.border = new RectOffset(0, 0, 0, 0);
        backgrnd.overflow = new RectOffset(0, 0, 0, 0);
		GeneralController.addScoreRace ("200");
		GeneralController.addScoreRace ("100");
		GeneralController.addScoreRace ("300");
		GeneralController.addScoreBattle ("0:20.0");
		GeneralController.addScoreBattle ("0:10.0");
		GeneralController.addScoreBattle ("0:30.0");
    }

    // Update is called once per frame
    void Update()
    {
        if (state == GameState.Mainmenu)
        {
            if (!transitioning)
            {
                if (Input.GetKey(KeyCode.UpArrow))
                {
                    //go up
                    transitioning = true;
                    transitionProgress = 0;
                    menuAngleSource = menuAngle;
                    menuAngleTarget = menuAngle + menuAngleSnap;
                    activeIndex = (activeIndex - 1 + menuElements.Length) % menuElements.Length;
                }
                else if (Input.GetKey(KeyCode.DownArrow))
                {
                    //go down
                    transitioning = true;
                    transitionProgress = 0;
                    menuAngleSource = menuAngle;
                    menuAngleTarget = menuAngle - menuAngleSnap;
                    activeIndex = (activeIndex + 1) % menuElements.Length;
                }
            }
            else
            {
                transitionProgress += Time.deltaTime * 3;
                menuAngle = (1 - transitionProgress) * menuAngleSource + transitionProgress * menuAngleTarget;
                //move towards target

                if (transitionProgress >= 1f)
                {
                    menuAngle = menuAngleTarget % 360;
                    transitioning = false;
                }

                for (int i = 0; i < menuElements.Length; i++)
                {
                    float angle = ((menuAngle + 360 + menuAngleSnap * i) + 90f) % 180f - 90f;
                    menu3[i] = buttonAtAngle(Mathf.Deg2Rad*angle);
                }
            }
            if (Input.GetKeyDown(KeyCode.Escape))
                state = GameState.Playing;
            else if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.Space))
            {
                switch (activeIndex)
                {
                    case 0: //Start game
                        state = GameState.Playing;
						StartNewGame();
                        break;
                    case 1: //How to play
                        state = GameState.Howtoplay;
                        break;
                    case 2: //Options
                        state = GameState.Options;
                        break;
                    case 3: //High scores
                        state = GameState.Highscores;
                        break;
                    case 4: //Credits
                        state = GameState.Credits;
                        break;
                    case 5: //Bike settings
                        state = GameState.Options;
                        break;
                    default:
                        state = GameState.Mainmenu;
                        break;
                }
            }
        }
        else
        {
            if (Input.GetKeyDown(KeyCode.Escape))
                state = GameState.Mainmenu;
        }
    }

    void OnGUI()
    {
        if (state != GameState.Playing)
        {
            float imgratio = (background.height * 1f) / background.width;
            if (Screen.height > imgratio * Screen.width)
            {
                //vertical, crop left-right
                float newWidth = Screen.height / imgratio;
                float newx = (Screen.width - newWidth)/2;
                GUI.DrawTexture(new Rect(newx, 0, newWidth, Screen.height), background);
            }
            else
            {
                //horizontal, crop up-down
                float newHeight = Screen.width * imgratio;
                float newy = (Screen.height - newHeight)/2;
                GUI.DrawTexture(new Rect(0, newy, Screen.width, newHeight), background);
            }
        }
        GUI.Box(new Rect(0, 0, 150, 50), state.ToString());

        GUI.skin.button.fontSize = 25;
        GUI.skin.button.normal.background = button;
        GUI.skin.button.normal.textColor = Color.black;
        GUI.skin.button.hover.background = button;
        GUI.skin.button.hover.textColor = Color.black;
        GUI.skin.box.wordWrap = true;
        GUI.skin.box.fontSize = 35;
        GUI.skin.box.normal.background = button;
        GUI.skin.box.normal.textColor = Color.black;
        if (state == GameState.Mainmenu) {
			//sorted drawing, draw the smaller buttons first because they are further away
			List<KeyValuePair<int,float>> widths = new List<KeyValuePair<int,float>> ();
			for (int i = 0; i < menuElements.Length; i++)
				widths.Add (new KeyValuePair<int,float> (i, menu3 [i].width));
			var sorted = widths.OrderBy (x => x.Value).ToList ();
			List<int> idx = sorted.Select (x => x.Key).ToList ();

			for (int i = 0; i < idx.Count; i++) {
				int ind = idx [i];
				//caroussel
				GUI.skin.button.fontSize = Mathf.RoundToInt (menu3 [ind].height / 2f);
				if (GUI.Button (menu3 [ind], menuElements [ind])) {
					switch (ind) {
					case 0: //Start game
						state = GameState.Playing;
						StartNewGame();
						break;
					case 1: //How to play
						state = GameState.Howtoplay;
						break;
					case 2: //Options
						state = GameState.Options;
						break;
					case 3: //High scores
						state = GameState.Highscores;
						break;
					case 4: //Credits
						state = GameState.Credits;
						break;
					case 5: //Bike settings
						state = GameState.Options;
						break;
					default:
						state = GameState.Playing;
						break;
					}
				}
			}
		} else if (state == GameState.Howtoplay) {
			GUI.Box (new Rect ((Screen.width - 400) / 2, (Screen.height - 600) / 2, 400, 50), "How to play");
			GUI.Box (new Rect ((Screen.width - 800) / 2, (Screen.height - 500) / 2, 800, 500), "Sit on the cycle and start pedalling. Don't fall over!");
		} else if (state == GameState.Options) {
			GUI.Box (new Rect ((Screen.width - 400) / 2, (Screen.height - 600) / 2, 400, 50), "Options");
			GUI.Box (new Rect ((Screen.width - 800) / 2, (Screen.height - 500) / 2, 800, 500), "Select some wonderful options. Maybe the handling of the bike or the color scheme.");
		} else if (state == GameState.Highscores) {
			GUI.skin.label.fontSize = 35;
			GUI.skin.label.normal.textColor = Color.black;
			GUI.skin.label.alignment = TextAnchor.MiddleCenter;
			GUI.Box (new Rect ((Screen.width - 400) / 2, (Screen.height - 600) / 2, 400, 50), "High scores");
			GUI.Box (new Rect ((Screen.width - 800) / 2, (Screen.height - 500) / 2, 800, 500), "These are the last five scores");
			string scoresRace = "Race:\n";
			string scoresBattle = "Battle:\n";
			for(int i = 0; i < 5; i++) {
				scoresRace += "\n"+GeneralController.scoresRace[i];
				scoresBattle += "\n"+GeneralController.scoresBattle[i];
			}
			GUI.Label (new Rect((Screen.width - 500) / 2, (Screen.height - 200) / 2, 250, 300), scoresRace);
			GUI.Label (new Rect(Screen.width / 2, (Screen.height - 200) / 2, 250, 300), scoresBattle);
		} else if (state == GameState.Credits) {
			GUI.Box (new Rect ((Screen.width - 400) / 2, (Screen.height - 600) / 2, 400, 50), "Credits");
			GUI.Box (new Rect ((Screen.width - 800) / 2, (Screen.height - 500) / 2, 800, 500), "CycloTron was commissioned by:\nJodi Kooijman\nThom van Beek\n\n\nThe game was developed by:\nMatthijs Amesz\nGuillermo Currás Lorenzo\nPanchamy Krishnan\nAntony Löbker\nTiago Susano Pinto");
		} else {
			GUI.skin.label.fontSize = 50;
			GUI.skin.label.alignment = TextAnchor.UpperRight;
			if(substate == SubGameState.Racing || substate == SubGameState.Free) {
				GUI.Label(new Rect(Screen.width - 300, 10, 290, 60), ""+GeneralController.score);
			} else if(substate == SubGameState.Battle) {
				int totalsecs = Mathf.RoundToInt((Time.time - GeneralController.battleModeStartTime)*10);
				int msecs = totalsecs % 10;
				totalsecs /= 10;
				int secs = totalsecs % 60;
				int mins = totalsecs / 60;
				GUI.Label(new Rect(Screen.width - 300, 10, 290, 60), String.Format("{0}:{1:00}.{2}", mins, secs, msecs));
			}
		}
    }
    
    private Rect buttonAtAngle(float angle)
    {
        float centerY = Mathf.Sin(angle);
        float btnRadiusW = 0.75f;
        float btnRadiusH = 0.25f;
        float centerZ = Mathf.Cos(angle);
        float cameraZ = 3f;
        float projZ = 1.5f;

        float left = -btnRadiusW * (cameraZ-projZ)/(cameraZ-centerZ);
        float right = -left;
        float top = (centerY-btnRadiusH) *(cameraZ - projZ) / (cameraZ - centerZ);
        float bottom = (centerY+btnRadiusH)  *(cameraZ - projZ) / (cameraZ - centerZ);
        float width = right-left;
        float height = bottom-top;

        float factor = 400;
        return new Rect((Screen.width-width*factor)/2, Screen.height/2 + top*factor, width*factor, height*factor);
    }

	private void StartNewGame() {
		//reset bike, opponents, floor, walls, trails, gates, score
		GameObject bike = GameObject.Find ("Bike");
		BikePhysicsScript bps = bike.GetComponent<BikePhysicsScript> ();
		bps.ResetBike ();

		TileManager tileManager = gameObject.GetComponent<TileManager> ();
		if (substate == SubGameState.Battle)
			tileManager.RemoveArena ();
	}
}

using UnityEngine;
using System.Collections;
using System;
using System.Collections.Generic;
using System.Linq;

public enum GameState { Playing, Mainmenu, Howtoplay, Options, Highscores, Credits }

public class MenuSelection : MonoBehaviour
{
    public static GameState state = GameState.Mainmenu;
    public Texture2D background, button;

    private bool transitioning = false;
    private float transitionProgress = 0;
    private int activeIndex = 0;//, invisibleIndex = -1;
    private List<Rect> menu3; //, menu3target, menu3source;
    private string[] menuElements = new string[] { "Start game", "How to play", "Options", "High scores", "Credits", "Bike settings" };
    private float menuAngleSnap = 30f;
    private float menuAngle = 0, menuAngleSource = 0, menuAngleTarget = 0;
    /*private Rect[] allButtonPositions = new Rect[] { new Rect(-0.5f,-0.25f,1,0.5f), new Rect(-0.547209405f,-0.254958994f,1.09441881f,0.547209405f), new Rect(-0.6f,-0.219615242f,1.2f,0.6f), new Rect(-0.654195314f,-0.135488286f,1.308390629f,0.654195314f), 
        new Rect(-0.70291371f,0,1.40582742f,0.70291371f), new Rect(-0.737436235f,0.177855575f,1.47487247f,0.737436235f), new Rect(-0.75f,0.375f,1.5f,0.75f), new Rect(-0.737436235f,0.55958066f,1.47487247f,0.737436235f), 
        new Rect(-0.70291371f,0.70291371f,1.40582742f,0.70291371f), new Rect(-0.654195314f,0.7896836f,1.308390629f,0.654195314f), new Rect(-0.6f,0.819615242f,1.2f,0.6f), new Rect(-0.547209405f,0.8021684f,1.09441881f,0.547209405f), new Rect(-0.5f,0.75f,1,0.5f) };*/
    private GUIStyle backgrnd;

    // Use this for initialization
    void Start()
    {
        menu3 = new List<Rect>();
        for (int i = 0; i < menuElements.Length; i++)
        {
            int ind = ((i + (menuElements.Length / 2)) % menuElements.Length) - (menuElements.Length / 2);
            //menu3.Add(GetCarousselButton(Screen.width / 2, getCarousselY(ind), i < Mathf.Abs(i - menuElements.Length) ? i : i - menuElements.Length));
            float angle = ind * menuAngleSnap * Mathf.Deg2Rad;
            menu3.Add(buttonAtAngle(angle));
        }
        //menu3target = new List<Rect>();
        //menu3source = new List<Rect>();
        backgrnd = new GUIStyle();
        backgrnd.fixedHeight = 0;
        backgrnd.fixedWidth = 0;
        backgrnd.stretchHeight = true;
        backgrnd.stretchWidth = true;
        backgrnd.border = new RectOffset(0, 0, 0, 0);
        backgrnd.overflow = new RectOffset(0, 0, 0, 0);
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
                    /*
                    menu3source = new List<Rect>(menu3);
                    menu3target = new List<Rect>();

                    activeIndex = (activeIndex - 1 + menuElements.Length) % menuElements.Length;
                    for (int i = 0; i < menuElements.Length; i++)
                    {
                        int ind = (i - activeIndex + menuElements.Length + (menuElements.Length / 2)) % menuElements.Length;
                        int ind2 = (i - activeIndex + menuElements.Length) % menuElements.Length;
                        menu3target.Add(GetCarousselButton(Screen.width / 2, getCarousselY(ind), ind2 < Mathf.Abs(ind2 - menuElements.Length) ? ind2 : ind2 - menuElements.Length));
                        if (menu3source[i].yMin > menu3target[i].yMin)
                            invisibleIndex = i;
                    }*/
                }
                else if (Input.GetKey(KeyCode.DownArrow))
                {
                    //go down
                    transitioning = true;
                    transitionProgress = 0;
                    menuAngleSource = menuAngle;
                    menuAngleTarget = menuAngle - menuAngleSnap;
                    activeIndex = (activeIndex + 1) % menuElements.Length;
                    /*
                    menu3source = new List<Rect>(menu3);
                    menu3target = new List<Rect>();

                    activeIndex = (activeIndex + 1) % menuElements.Length;
                    for (int i = 0; i < menuElements.Length; i++)
                    {
                        int ind = (i - activeIndex + menuElements.Length + (menuElements.Length / 2)) % menuElements.Length;
                        int ind2 = (i - activeIndex + menuElements.Length) % menuElements.Length;
                        menu3target.Add(GetCarousselButton(Screen.width / 2, getCarousselY(ind), ind2 < Mathf.Abs(ind2 - menuElements.Length) ? ind2 : ind2 - menuElements.Length));
                        if (menu3source[i].yMin < menu3target[i].yMin)
                            invisibleIndex = i;
                    }*/
                }
            }
            else
            {
                transitionProgress += Time.deltaTime * 3;
                menuAngle = (1 - transitionProgress) * menuAngleSource + transitionProgress * menuAngleTarget;
                //move towards target
                /*for (int i = 0; i < menuElements.Length; i++)
                {
                    menu3.Insert(i, InterpolateRect(menu3source[i], menu3target[i], transitionProgress));
                }*/

                if (transitionProgress >= 1f)
                {
                    //menu3 = menu3target;
                    menuAngle = menuAngleTarget % 360;
                    transitioning = false;
                    //invisibleIndex = -1;
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
                //GUI.Box(, backgrnd);
            }
            else
            {
                //horizontal, crop up-down
                float newHeight = Screen.width * imgratio;
                float newy = (Screen.height - newHeight)/2;
                //GUI.Box(new Rect(0, 0, Screen.width, newHeight), background, backgrnd);
                GUI.DrawTexture(new Rect(0, newy, Screen.width, newHeight), background);
            }
            //GUI.Box(new Rect(0, 0, Screen.width, Screen.height), background);
        }
        GUI.Box(new Rect(0, 0, 150, 50), state.ToString());

        GUI.skin.button.fontSize = 25;
        GUI.skin.button.normal.background = button;
        GUI.skin.button.normal.textColor = Color.black;
        GUI.skin.button.hover.background = button;
        GUI.skin.button.hover.textColor = Color.black;
        GUI.skin.box.wordWrap = true;
        GUI.skin.box.fontSize = 25;
        GUI.skin.box.normal.background = button;
        GUI.skin.box.normal.textColor = Color.black;
        if (state == GameState.Mainmenu)
        {
            //sorted drawing, draw the smaller buttons first because they are further away
            List<KeyValuePair<int,float>> widths = new List<KeyValuePair<int,float>>();
            for (int i = 0; i < menuElements.Length; i++)
                widths.Add(new KeyValuePair<int,float>(i,menu3[i].width));
            var sorted = widths.OrderBy(x => x.Value).ToList();
            List<int> idx = sorted.Select(x => x.Key).ToList();

            for (int i = 0; i < idx.Count; i++)
            {
                int ind = idx[i];
                //caroussel
                GUI.skin.button.fontSize = Mathf.RoundToInt(menu3[ind].height/2f);
                if (GUI.Button(menu3[ind], menuElements[ind]))
                {
                    switch (ind)
                    {
                        case 0: //Start game
                            state = GameState.Playing;
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
        }
        else if (state == GameState.Howtoplay)
        {
            GUI.Box(new Rect((Screen.width - 600) / 2, (Screen.height - 400) / 2, 600, 400), "How to play\nSit on the cycle and start pedalling. Don't fall over!");
        }
        else if (state == GameState.Options)
        {
            GUI.Box(new Rect((Screen.width - 600) / 2, (Screen.height - 400) / 2, 600, 400), "Select some wonderful options. Maybe the handling of the bike or the color scheme.");
        }
        else if (state == GameState.Highscores)
        {
            GUI.Box(new Rect((Screen.width - 600) / 2, (Screen.height - 400) / 2, 600, 400), "These are the last three scores.\n\n1234\n2345 <- highest\n0123");
        }
        else if (state == GameState.Credits)
        {
            GUI.Box(new Rect((Screen.width - 600) / 2, (Screen.height - 400) / 2, 600, 400), "These amazing people made this game.\nPan\nTia\nGui\nMat\nAnt");
        }
    }
    
    private Rect GetCarousselButton(float cx, float cy, int dist)
    {
        /*int absDist = Mathf.Abs(dist);
        switch (absDist)
        {
            case 0:
                return new Rect(cx - 110, cy - 55, 220, 110);
            case 1:
                return new Rect(cx - 100, cy - 50, 200, 100);
            case 2:
                return new Rect(cx - 90, cy - 45, 180, 90);
            case 3:
                return new Rect(cx - 82, cy - 41, 164, 82);
            case 4:
                return new Rect(cx - 75, cy - 37.5f, 150, 75);
            default:
                return new Rect(cx - 70, cy - 35, 140, 70);
        }*/
        /*int i = 6+2*dist;
        float factor = 200;
        return new Rect(cx - allButtonPositions[i].width/2 * factor, cy - allButtonPositions[i].height/2 * factor, allButtonPositions[i].width * factor, allButtonPositions[i].height * factor);*/
        return new Rect();
    }

    private Rect InterpolateRect(Rect start, Rect end, float progress)
    {
        float inverse = 1f - progress;
        return new Rect(inverse * start.xMin + progress * end.xMin, inverse * start.yMin + progress * end.yMin, inverse * start.width + progress * end.width, inverse * start.height + progress * end.height);
    }

    private float getCarousselY(int index)
    {
        return 0;
        /*int i = 2 + 2 * index;
        float factor = 200;
        return Screen.height / 2 + (allButtonPositions[i].yMax + allButtonPositions[i].yMin) / 2 * factor;*/
        /*
        if (index == 0)
            return 0;
        float startY = (164 + 180) / 4;
        if (index == 1)
            return startY;
        startY += (180 + 200) / 4;
        if (index == 2)
            return startY;
        startY += (200 + 220) / 4;
        if (index == 3)
            return startY;
        startY += (220 + 200) / 4;
        if (index == 4)
            return startY;
        startY += (200 + 180) / 4;
        if (index == 5)
            return startY;
        startY += (180 + 164) / 4;
        if (index == 6)
            return startY;

        return 100 * index;
        */
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
}

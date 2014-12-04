using UnityEngine;
using System.Collections;
using System;

public class MenuSelection : MonoBehaviour {
    private bool transitioning = false;
    private float transitionProgress = 0;
    private int activeIndex = 0;
    private Rect[] menu1, menu1target, menu1source;
    private Rect[] menu2, menu2target, menu2source;
    private Rect[] menu3, menu3target, menu3source;
    private string[] menuElements = new string[]{ "Start game", "How to play", "Options", "High scores", "Credits" };

	// Use this for initialization
	void Start () {
        menu1 = new Rect[menuElements.Length];
        menu2 = new Rect[menuElements.Length];
        menu3 = new Rect[menuElements.Length];
        for (int i = 0; i < menuElements.Length; i++)
        {
            menu1[i] = new Rect(210, Screen.height / 2 - 300 + 110 * i, 180, 90);
            int ind = (i + (menuElements.Length/2)) % menuElements.Length;
            menu2[i] = new Rect(510, Screen.height / 2 - 300 + 110 * ind, 180, 90);
            menu3[i] = GetCarousselButton(900, Screen.height / 2 - 200 + 110 * ind, i < Mathf.Abs(i - menuElements.Length) ? i : i - menuElements.Length);
        }
        menu1[0] = new Rect(200, Screen.height / 2 - 300, 200, 100);
        menu2[0] = new Rect(500, Screen.height / 2 - 300 + 110 * (menuElements.Length/2), 200, 100);

        menu1target = new Rect[menuElements.Length];
        menu2target = new Rect[menuElements.Length];
        menu3target = new Rect[menuElements.Length];
        menu1source = new Rect[menuElements.Length];
        menu2source = new Rect[menuElements.Length];
        menu3source = new Rect[menuElements.Length];
        Array.Copy(menu1,menu1target,menu1.Length);
	}
	
	// Update is called once per frame
	void Update () {
        if (!transitioning)
        {
            if (Input.GetKey(KeyCode.UpArrow))
            {
                //go up
                transitioning = true;
                transitionProgress = 0;
                Array.Copy(menu1,menu1source,menu1.Length);
                Array.Copy(menu2,menu2source,menu2.Length);
                Array.Copy(menu3,menu3source,menu3.Length);
                //menu1source = menu1;
                //menu2source = menu2;
                //menu3source = menu3;

                menu1target[activeIndex] = new Rect(menu1[activeIndex].xMin + 10, menu1[activeIndex].yMin + 5, 180, 90);
                activeIndex = (activeIndex - 1 + menuElements.Length) % menuElements.Length;
                menu1target[activeIndex] = new Rect(menu1[activeIndex].xMin - 10, menu1[activeIndex].yMin - 5, 200, 100);
                for (int i = 0; i < menuElements.Length; i++)
                {
                    int ind = (i - activeIndex + menuElements.Length + (menuElements.Length / 2)) % menuElements.Length;
                    menu2target[i] = new Rect(510, Screen.height / 2 - 300 + 110 * ind, 180, 90);
                    int ind2 = (i - activeIndex + menuElements.Length) % menuElements.Length;
                    menu3target[i] = GetCarousselButton(900, Screen.height / 2 - 200 + 110 * ind, ind2 < Mathf.Abs(ind2 - menuElements.Length) ? ind2 : ind2 - menuElements.Length);
                }
                menu2[activeIndex] = new Rect(500, Screen.height / 2 - 300 + 110 * (menuElements.Length/2), 200, 100);
            }
            else if (Input.GetKey(KeyCode.DownArrow))
            {
                //go down
                transitioning = true;
                transitionProgress = 0;
                Array.Copy(menu1,menu1source,menu1.Length);
                Array.Copy(menu2,menu2source,menu2.Length);
                Array.Copy(menu3,menu3source,menu3.Length);
                //menu1source = menu1;
                //menu2source = menu2;
                //menu3source = menu3;

                menu1target[activeIndex] = new Rect(menu1[activeIndex].xMin + 10, menu1[activeIndex].yMin + 5, 180, 90);
                activeIndex = (activeIndex + 1) % menuElements.Length;
                menu1target[activeIndex] = new Rect(menu1[activeIndex].xMin - 10, menu1[activeIndex].yMin - 5, 200, 100);
                for (int i = 0; i < menuElements.Length; i++)
                {
                    int ind = (i - activeIndex + menuElements.Length + (menuElements.Length / 2)) % menuElements.Length;
                    menu2target[i] = new Rect(510, Screen.height / 2 - 300 + 110 * ind, 180, 90);
                    int ind2 = (i - activeIndex + menuElements.Length) % menuElements.Length;
                    menu3target[i] = GetCarousselButton(900, Screen.height / 2 - 200 + 110 * ind, ind2 < Mathf.Abs(ind2 - menuElements.Length) ? ind2 : ind2 - menuElements.Length);
                }
                menu2[activeIndex] = new Rect(500, Screen.height / 2 - 300 + 110 * (menuElements.Length/2), 200, 100);
            }
        }
        else
        {
            transitionProgress += Time.deltaTime*2;
            //move towards target
            for (int i = 0; i < menuElements.Length; i++)
            {
                menu1[i] = InterpolateRect(menu1source[i], menu1target[i], transitionProgress);
                menu2[i] = InterpolateRect(menu2source[i], menu2target[i], transitionProgress);
                menu3[i] = InterpolateRect(menu3source[i], menu3target[i], transitionProgress);
                Debug.Log("Menu2[" + i + "] " + menu2[i].yMin + " source: " + menu2source[i].yMin + " target: " + menu2target[i].yMin);
            }

            if (transitionProgress > 1f)
            {
                Array.Copy(menu1target,menu1,menu1target.Length);
                Array.Copy(menu2target,menu2,menu2target.Length);
                Array.Copy(menu3target,menu3,menu3target.Length);
                //menu1 = menu1target;
                //menu2 = menu2target;
                //menu3 = menu3target;
                transitioning = false;
            }
        }
	}

    void OnGUI()
    {
        for (int i = 0; i < menuElements.Length; i++)
        {
            //simple
            GUI.Box(menu1[i], menuElements[i]);

            //rotating
            GUI.Box(menu2[i], menuElements[i]);

            //caroussel
            GUI.Box(menu3[i], menuElements[i]);
        }
    }

    private Rect GetCarousselButton(int cx, int cy, int dist)
    {
        int absDist = Mathf.Abs(dist);
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
        }
    }

    private Rect InterpolateRect(Rect start, Rect end, float progress)
    {
        float inverse = 1f - progress;
        return new Rect(inverse * start.xMin + progress * end.xMin, inverse * start.yMin + progress * end.yMin, inverse * start.width + progress * end.width, inverse * start.height + progress * end.height);
    }
}

using UnityEngine;
using System.Collections;

public class ArrowScript : MonoBehaviour {

	public Texture2D texture;
	public Camera cam;
	public GameObject target;
	public Color color;

	private float angle;
	private Vector2 size = new Vector2(64, 48);
	private Vector2 pos = new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);
	private Rect rect;
	private Vector2 pivot;
	private bool draw = true;
	private Vector3 targetPos;
	private float screenDiagAngle;

	// Use this for initialization
	void Start () {
		screenDiagAngle = Mathf.Atan2(Screen.height, Screen.width);
	}
	
	// Update is called once per frame
	void Update () {

		targetPos = cam.WorldToScreenPoint(target.transform.position + Vector3.up);

		if (targetPos.x < 0 || targetPos.x > Screen.width || targetPos.y < 0 || targetPos.y > Screen.height){
			draw = true;
			angle = Mathf.Atan2(targetPos.y - Screen.height *0.5f, targetPos.x - Screen.width * 0.5f);
			if (targetPos.z > 0)
				angle = -angle;
			else
				angle = Mathf.PI-angle;
		}
		else {
			draw = false;
		}
		
		if (Mathf.Abs(angle) < screenDiagAngle){ //to right of screen
			pos.x = Screen.width - size.x;
			pos.y = Screen.height * 0.5f + Mathf.Sin(angle) * Screen.width * 0.5f;
		}
		else if (Mathf.PI-Mathf.Abs(angle) < screenDiagAngle){ //to left side of screen
			pos.x = size.x;
			pos.y = Screen.height * 0.5f + Mathf.Sin(angle) * Screen.width * 0.5f;
		}
		else if (angle < 0){ //to top of screen
			pos.y = size.x;
			pos.x = Screen.width * 0.5f + Mathf.Cos(angle) * Screen.width * 0.5f;
		}
		else { //to bottom of screen
			pos.y = Screen.height - size.x;
			pos.x = Screen.width * 0.5f + Mathf.Cos(angle) * Screen.width * 0.5f;
		}

		rect = new Rect(pos.x - size.x * 0.5f, pos.y - size.y * 0.5f, size.x, size.y);
		pivot = new Vector2(rect.xMin + rect.width * 0.5f, rect.yMin + rect.height * 0.5f);
	}

	void OnGUI() {
		if (draw){
			Matrix4x4 matrixBackup = GUI.matrix;
			GUIUtility.RotateAroundPivot(Mathf.Rad2Deg*angle, pivot);
			GUI.color = color;
			GUI.DrawTexture(rect, texture);
			GUI.matrix = matrixBackup;
		}
	}
}

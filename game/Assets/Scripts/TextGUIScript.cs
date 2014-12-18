using UnityEngine;
using System.Collections;

public class TextGUIScript : MonoBehaviour {

	public UnityEngine.Color defaultColor;

	public GUIText rotationText;
	public GUIText speedText;
	public GUIText mainText;
	public GUIText footnote;


	// Use this for initialization
	void Start () {
		MainStart ();
		FooterStart ();
		BikeValuesTextStart ();
	}

	void MainStart() {
		mainText.text = "";
		mainText.color = UnityEngine.Color.red;
		mainText.fontSize = 30;
	}

	void FooterStart() {
		footnote.text = "";
		footnote.color = UnityEngine.Color.green;
		footnote.fontSize = 10;
	}

	void BikeValuesTextStart() {
		speedText.fontSize = 13;
		rotationText.fontSize = 13;
	}
	
	// Update is called once per frame
	void Update () {

	}

	public void DisplayMessage(string message) {
		mainText.text = message;
		mainText.color = defaultColor;
	}

	public void DisplayMessage(string message, UnityEngine.Color textColor) {
		mainText.text = message;
		mainText.color = textColor;
	}

	public void EraseMessage() {
		mainText.text = "";
	}

	//TODO: add extra menssages
	/*public void AddMenssage(string menssage) {
		menssages.Enqueue (menssage);
	}*/

	public void DisplayFooter(string message, UnityEngine.Color textColor) {
		footnote.text = message;
		footnote.color = textColor;
	}
	
	public void EraseFooter() {
		footnote.text = "";
	}

	public void UpdateBikeValuesText(float steerRot, float speed) {
		rotationText.text = "Steer Rotation\n" + steerRot.ToString("F2") + "º";
		speedText.text = "Speed\n" + speed.ToString ("F2");
	}
}

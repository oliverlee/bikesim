using UnityEngine;
using System.Collections;

public class PlayerController : MonoBehaviour {
    private float speed = 3f;

	// Use this for initialization
	void Start () {
	
	}
	
	// Update is called once per frame
	void Update () {
        if (Input.GetKey(KeyCode.LeftArrow))
        {
            //left
            transform.position += Vector3.left * Time.deltaTime * speed;
        }
        else if (Input.GetKey(KeyCode.RightArrow))
        {
            //right
            transform.position += Vector3.right * Time.deltaTime * speed;
        }
        if (Input.GetKey(KeyCode.UpArrow))
        {
            //up
            transform.position += Vector3.forward * Time.deltaTime * speed;
        }
        else if (Input.GetKey(KeyCode.DownArrow))
        {
            //down
            transform.position += Vector3.back * Time.deltaTime * speed;
        }
        Camera.main.transform.position = new Vector3(transform.position.x, 5, transform.position.z - 5);
	}
}

using UnityEngine;
using System.Collections;

public class Trail : MonoBehaviour
{
    public Transform rearWheel;
    public Transform trailParent;

    public Vector3 lastPosition = new Vector3();
	public Vector3 oldPosition = new Vector3();
    // Use this for initialization
    void Start()
    {
		oldPosition = rearWheel.position + Vector3.back;
        lastPosition = rearWheel.position;
    }

    // Update is called once per frame
    void Update()
    {
        if (Vector3.Distance(rearWheel.transform.position, lastPosition) > 1f)
        {
			if (MenuSelection.substate == SubGameState.Battle)
				CreateBox(oldPosition, lastPosition);
			oldPosition = lastPosition;
            lastPosition = rearWheel.position;
        }
    }

	public void Reset() {
		foreach (Transform child in trailParent.GetComponentsInChildren<Transform>()){
			if( child.tag == "Trail") {
				DestroyImmediate(child.gameObject);
			}
		}
	}

    void CreateBox(Vector3 p1, Vector3 p2)
    {
        float distance = Vector3.Distance(p1, p2);
        Vector3 middle = (p1 + p2) / 2;

        var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
        //cube.AddComponent(Rigidbody);

        cube.transform.parent = trailParent;
        cube.transform.localScale = new Vector3(0.2f, 1, distance + 0.1f);
        cube.transform.position = middle + Vector3.up; //above floor
        cube.transform.LookAt(lastPosition + Vector3.up, Vector3.up);
		cube.tag = "Trail";
    }

	public void ClearTrail()
	{
		foreach (Transform child in trailParent) {
			Destroy(child.gameObject);
		}
		lastPosition = rearWheel.position;
	}
}

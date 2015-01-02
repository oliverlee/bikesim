using UnityEngine;
using System.Collections;

public class Trail : MonoBehaviour
{
    public Transform rearWheel;
    public Transform trailParent;

    private Vector3 lastPosition = new Vector3();
    // Use this for initialization
    void Start()
    {
        lastPosition = rearWheel.position;
    }

    // Update is called once per frame
    void Update()
    {
        if (Vector3.Distance(rearWheel.transform.position, lastPosition) > 0.5f)
        {
            CreateBox(lastPosition, rearWheel.position);
            lastPosition = rearWheel.position;
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
    }
}

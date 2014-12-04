using UnityEngine;
using System.Collections;

public class Tile : MonoBehaviour {
    public Vector3 targetPos;

    private Vector3 sourcePos;
    private float moveSpeed = 8f;
    private bool moving = true;

	// Use this for initialization
	void Start () {
        
	}
	
	// Update is called once per frame
	void Update () {
        if (moving)
        {
            transform.position = Vector3.MoveTowards(transform.position, targetPos, moveSpeed * Time.deltaTime);
            if (Vector3.Distance(transform.position, targetPos) < 0.1f)
            {
                transform.position = targetPos;
                moving = false;
                if (sourcePos.y > targetPos.y) //moving down aka deleting
                    Destroy(gameObject);
            }
        }
	}

    public void StartMoving()
    {
        sourcePos = transform.position;
		targetPos = sourcePos + Vector3.up*5f;
        moving = true;
    }

    public void RemoveTile()
    {
        sourcePos = transform.position;
		targetPos = sourcePos + Vector3.down * 5f;
        moving = true;
    }
}

using UnityEngine;
using System.Collections;

public class Tile : MonoBehaviour {
    public Vector3 targetPos;

    private Vector3 sourcePos;
    private float moveSpeed = 12f;
	private float distance = 4f;
    private bool moving = true;
	private bool deleting = false;

	public Coordinates coordinates;

	// Use this for initialization
	void Start () {
        
	}
	
	// Update is called once per frame
	void Update () {
        if (moving)
        {
            transform.position = Vector3.MoveTowards(transform.position, targetPos, moveSpeed * Time.deltaTime);
			if (sourcePos.y > targetPos.y) //moving down aka deleting
			{
				if(transform.position.y - 0.1f < targetPos.y) {
					Destroy(gameObject);
				}
			}
			else
			{
				if(transform.position.y + 0.1f > targetPos.y)
				{
					transform.position = targetPos;
					moving = false;
				}
			}
        }
	}

    public void StartMoving()
    {
        sourcePos = transform.position;
		targetPos = sourcePos + Vector3.up * distance;
        moving = true;
    }

    public void RemoveTile()
    {
		if (!deleting)
		{
			sourcePos = transform.position;
			targetPos = sourcePos + Vector3.down * distance;
			moving = true;
			deleting = true;
		}
    }
}

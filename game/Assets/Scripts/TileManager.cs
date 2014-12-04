using UnityEngine;
using System.Collections;

public class TileManager : MonoBehaviour {
    public GameObject TilePrefab;
    public GameObject Player, Floor;

    private int oldsize = 1, newsize = 1;
    private Vector3 lastPos = Vector3.zero;

	// Use this for initialization
	void Start () {
	
	}
	
	// Update is called once per frame
	void Update () {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            newsize++;
            for (int i = -(newsize - 2); i < newsize - 1; i++)
            {
                CreateTile(i, newsize - 1);
                CreateTile(newsize - 1, i);
                CreateTile(-i, -(newsize - 1));
                CreateTile(-(newsize - 1), -i);
            }
            CreateTile(newsize - 1, newsize - 1);
            CreateTile(-(newsize - 1), newsize - 1);
            CreateTile(newsize - 1, -(newsize - 1));
            CreateTile(-(newsize - 1), -(newsize - 1));
        }

        Vector3 playerPos = Player.transform.position;
        float minX = playerPos.x, maxX = playerPos.x, minZ = playerPos.z, maxZ = playerPos.z;
        if (playerPos.x != lastPos.x || playerPos.z != lastPos.z)
        {
            Transform floorT = Floor.transform;
            foreach(Transform child in floorT) {
                minX = Mathf.Min(minX, child.position.x);
                maxX = Mathf.Max(maxX, child.position.x);
                minZ = Mathf.Min(minZ, child.position.z);
                maxZ = Mathf.Max(maxZ, child.position.z);
            }
        }

        if (playerPos.x > lastPos.x)
        {
            //move right, create tiles on right
            if (maxX - playerPos.x < 5f)
            {
                //create tiles
				//max pos to index
				int x = Mathf.RoundToInt((maxX * Mathf.Sqrt(3))/3f  + 0.0001f); //make sure it is not 3.999999 -> 3
				int zmin = (int) minZ/2;
				int zmax = (int) maxZ/2;
				for(int i = zmin; i <= zmax; i++) {
					CreateTile(x+1,i);
				}

				//RemoveFarTiles();
            }
        }
        else if(playerPos.x < lastPos.x)
        {
            //move left, create tiles on left
            if (playerPos.x - minX < 5f)
            {
                //create tiles
				int x = Mathf.RoundToInt((minX * Mathf.Sqrt(3))/3f  + 0.0001f); //make sure it is not 3.999999 -> 3
				int zmin = (int) minZ/2;
				int zmax = (int) maxZ/2;
				for(int i = zmin; i <= zmax; i++) {
					CreateTile(x-1,i);
				}
			}
        }
        if (playerPos.z > lastPos.z)
        {
            //move up, create tiles up
            if (maxZ - playerPos.z < 5f)
            {
                //create tiles
				int z = (int) maxZ/2;
				int xmin = Mathf.RoundToInt((minX * Mathf.Sqrt(3))/3f  + 0.0001f);
				int xmax = Mathf.RoundToInt((maxX * Mathf.Sqrt(3))/3f  + 0.0001f);
				for(int i = xmin; i<= xmax; i++) {
					CreateTile(i,z+1);
				}
            }
        }
        else if (playerPos.z < lastPos.z)
        {
            //move down, create tiles down
            if (playerPos.z - minZ < 5f)
            {
                //create tiles
				int z = (int) minZ/2;
				int xmin = Mathf.RoundToInt((minX * Mathf.Sqrt(3))/3f  + 0.0001f);
				int xmax = Mathf.RoundToInt((maxX * Mathf.Sqrt(3))/3f  + 0.0001f);
				for(int i = xmin; i<= xmax; i++) {
					CreateTile(i,z-1);
				}
            }
        }

        lastPos = playerPos;
	}

    private void CreateTile(int x, int z)
    {
        float posX = 0, posZ = 0;
        posX = (x * 3f) / Mathf.Sqrt(3);
        if (x % 2 == 0)
        {
            posZ = z*2f;
        }
        else
        {
            posZ = z*2f+1;
        }
        GameObject newT = (GameObject) GameObject.Instantiate(TilePrefab);
        newT.transform.parent = Floor.transform;
        newT.transform.position = new Vector3(posX, -5f, posZ);
        Tile newTile = newT.GetComponent<Tile>();
        newTile.targetPos = new Vector3(posX, 0, posZ);
        newTile.StartMoving();
    }

	private void RemoveFarTiles()
	{
		//
	}
}

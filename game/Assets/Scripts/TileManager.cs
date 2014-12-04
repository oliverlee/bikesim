using UnityEngine;
using System.Collections;

public class TileManager : MonoBehaviour {
    public GameObject TilePrefab;
    public GameObject Player, Floor;

    private Vector3 lastPos = Vector3.zero;
	private Transform floorT;
	private float minX,maxX,minZ,maxZ;
	private float spawnDist = 7f, deleteDist = 9f;

	// Use this for initialization
	void Start () {
		floorT = Floor.transform;
		Vector3 playerPos = Player.transform.position;
		minX = playerPos.x;
		maxX = playerPos.x;
		minZ = playerPos.z;
		maxZ = playerPos.z;

		//create initial tiles
		for (int j = 2; j <= 3; j++)
		{
			for (int i = -(j - 2); i < j - 1; i++) {
				//edges
				CreateTile (i, j - 1);
				CreateTile (j - 1, i);
				CreateTile (-i, -(j - 1));
				CreateTile (-(j - 1), -i);
			}
			//corners
			CreateTile (j - 1, j - 1);
			CreateTile (-(j - 1), j - 1);
			CreateTile (j - 1, -(j - 1));
			CreateTile (-(j - 1), -(j - 1));
		}
		
		UpdateTileBounds ();
	}
	
	// Update is called once per frame
	void Update () {
        Vector3 playerPos = Player.transform.position;
		if(playerPos != lastPos)
			UpdateTileBounds();

        if (playerPos.x > lastPos.x)
        {
            //move right, create tiles on right
			if (maxX - playerPos.x < spawnDist)
            {
                //create tiles
				//max pos to index
				int x = Mathf.RoundToInt((maxX * Mathf.Sqrt(3))/3f  + 0.0001f); //make sure it is not 3.999999 -> 3
				int zmin = Mathf.FloorToInt(minZ/2f + 0.0001f);
				int zmax = Mathf.FloorToInt(maxZ/2f + 0.0001f);
				for(int i = zmin; i <= zmax; i++) {
					CreateTile(x+1,i);
				}
            }
			RemoveFarTiles();
		}
        else if(playerPos.x < lastPos.x)
        {
            //move left, create tiles on left
			if (playerPos.x - minX < spawnDist)
            {
                //create tiles
				int x = Mathf.RoundToInt((minX * Mathf.Sqrt(3))/3f  + 0.0001f); //make sure it is not 3.999999 -> 3
				int zmin = Mathf.FloorToInt(minZ/2f + 0.0001f);
				int zmax = Mathf.FloorToInt(maxZ/2f + 0.0001f);
				for(int i = zmin; i <= zmax; i++) {
					CreateTile(x-1,i);
				}
			}
			RemoveFarTiles();
		}
        if (playerPos.z > lastPos.z)
        {
            //move up, create tiles up
			if (maxZ - playerPos.z < spawnDist)
            {
                //create tiles
				int z = Mathf.FloorToInt(maxZ/2f + 0.0001f);
				int xmin = Mathf.RoundToInt((minX * Mathf.Sqrt(3))/3f  + 0.0001f);
				int xmax = Mathf.RoundToInt((maxX * Mathf.Sqrt(3))/3f  + 0.0001f);
				for(int i = xmin; i<= xmax; i++) {
					CreateTile(i,z+1);
				}
            }
			RemoveFarTiles();
		}
        else if (playerPos.z < lastPos.z)
        {
            //move down, create tiles down
			if (playerPos.z - minZ < spawnDist)
            {
                //create tiles
				int z = Mathf.FloorToInt(minZ/2f + 0.0001f);
				int xmin = Mathf.RoundToInt((minX * Mathf.Sqrt(3))/3f  + 0.0001f);
				int xmax = Mathf.RoundToInt((maxX * Mathf.Sqrt(3))/3f  + 0.0001f);
				for(int i = xmin; i<= xmax; i++) {
					CreateTile(i,z-1);
				}
            }
			RemoveFarTiles();
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
        newT.transform.parent = floorT;
        newT.transform.position = new Vector3(posX, -4f, posZ);
        Tile newTile = newT.GetComponent<Tile>();
        newTile.targetPos = new Vector3(posX, 0, posZ);
        newTile.StartMoving();
    }

	private void RemoveFarTiles()
	{
		Vector3 player = Player.transform.position;
		foreach (Transform child in floorT)
		{
			float tz = Mathf.Floor(child.position.z/2f + 0.0001f) * 2f; //make row uniform
			if(Mathf.Abs(child.position.x - player.x) > deleteDist || Mathf.Abs(tz - player.z) > deleteDist)
			{
				Tile ch = child.GetComponent<Tile>();
				ch.RemoveTile();
			}
		}
	}

	public void UpdateTileBounds()
	{
		minX = Player.transform.position.x;
		maxX = Player.transform.position.x;
		minZ = Player.transform.position.z;
		maxZ = Player.transform.position.z;

		foreach (Transform child in floorT) {
			minX = Mathf.Min (minX, child.position.x);
			maxX = Mathf.Max (maxX, child.position.x);
			minZ = Mathf.Min (minZ, child.position.z);
			maxZ = Mathf.Max (maxZ, child.position.z);
		}
	}
}

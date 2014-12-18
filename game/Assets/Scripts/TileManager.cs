using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public struct Coordinates {
	public int X;
	public int Z;
	
	public Coordinates(int x, int z){
		X = x;
		Z = z;
	}
	
	public override bool Equals(System.Object obj){
		if (obj is Coordinates){
			Coordinates c = (Coordinates)obj;
			return (c.X == X && c.Z == Z);
		} else
			return false;
	}
}

public class TileManager : MonoBehaviour {
    public GameObject TilePrefab;
    public GameObject Player, Floor;

    private Vector3 playerPos, lastPos = Vector3.zero;
	private Transform floorT;
	private static int spawnDist = 4, deleteDist = 4;
	private static float tileSizeX = 4f / Mathf.Sqrt (3f), tileSizeZ = 2f;
	private static float spawnDepth = -4f;
	private Coordinates playerCoordinates;
	private Dictionary<Coordinates, Tile> tiles = new Dictionary<Coordinates, Tile>();

	// Use this for initialization
	void Start () {
		floorT = Floor.transform;
		playerPos = Player.transform.position;
		playerCoordinates = PosToCoordinates (playerPos);

		//create initial tiles
		for (int i = playerCoordinates.X - spawnDist; i <= playerCoordinates.X + spawnDist; i++) {
			for (int j = playerCoordinates.Z - spawnDist; j <= playerCoordinates.Z + spawnDist; j++) {
				if (inRange(i, j))
					CreateTile(i, 0f, j);
			}
		}
	}

	//Returns the distance between the player and the center of the given tiles coordinates
	private float distToPlayer(int x, int z){
		Vector3 checkPos = CoordinatesToPos (x, z);
		float xDist = playerPos.x - checkPos.x;
		float zDist = playerPos.z - checkPos.z;
		return Mathf.Sqrt (xDist * xDist + zDist * zDist);
	}

	//Returns wether the tile at the given cordinates is within spawnrange of the player
	private bool inRange(int x, int z){
		return distToPlayer(x, z) <= spawnDist * 4 / Mathf.Sqrt(3f);
	}

	//Returns wether the tile at the given coordinates is outside the deletedistance from the player
	private bool outOfRange(int x, int z){
		return distToPlayer(x, z) > deleteDist * tileSizeX;
	}

	//Converts a Vector3 position to the corresponding Tile coordinate
	public static Coordinates PosToCoordinates(Vector3 pos){
		int x = (int)Mathf.Round (pos.x / Mathf.Sqrt(3f));
		int z;
		if (x % 2 == 0)
			z = (int)Mathf.Round (pos.z / tileSizeZ);
		else
			z = (int)Mathf.Round ((0.5f * tileSizeZ + pos.z )/ tileSizeZ);

		return new Coordinates (x, z);
	}

	//Converts a Coordinate to the Vector3 Position of that coordinates center
	public static Vector3 CoordinatesToPos(Coordinates coords){
		return CoordinatesToPos (coords.X, coords.Z);
	}

	public static Vector3 CoordinatesToPos(int x, int z){
		float posX, posZ;
		posX = (x * 3 / Mathf.Sqrt (3f));
		if (x % 2 == 0)
			posZ = z*tileSizeZ;
		else
			posZ = (z+0.5f)*tileSizeZ;
		return new Vector3 (posX, 0, posZ);
	}

	void Update(){
		lastPos = playerPos;
		playerPos = Player.transform.position;
		//If we have moved to a new tile, check for new/old tiles
		if (playerPos.x != lastPos.x || playerPos.z != lastPos.z) {
			playerCoordinates = PosToCoordinates(playerPos);
			UpdateTiles();
		}
	}

	void UpdateTiles(){
		//Check which tiles need to be removed
		List<Coordinates> toBeRemoved = new List<Coordinates>();
		foreach (Coordinates coords in tiles.Keys){
			if (!inRange(coords.X, coords.Z)){
				tiles[coords].RemoveTile();
				toBeRemoved.Add(coords); //cant delete while in a foreach loop
			}
		}
		foreach (Coordinates coords in toBeRemoved)
			tiles.Remove(coords);
		//Check where tiles need to be added
		for (int i = playerCoordinates.X - deleteDist; i <= playerCoordinates.X + deleteDist; i++) {
			for (int j = playerCoordinates.Z - deleteDist; j <= playerCoordinates.Z + deleteDist; j++) {
				Coordinates checkCoords = new Coordinates(i, j);
				if (inRange(i, j) && !tiles.ContainsKey(new Coordinates(i, j))) {
						CreateTile(i, spawnDepth, j);
				}
			}
		}
	}

    private void CreateTile(int x, float y, int z)
    {
		Coordinates tileCoords = new Coordinates (x, z);
		Vector3 finalPos = CoordinatesToPos (x, z);

		Vector3 startPos = new Vector3 (finalPos.x, y, finalPos.z);
        GameObject newT = (GameObject) GameObject.Instantiate(TilePrefab);
        newT.transform.parent = floorT;
        newT.transform.position = startPos;

        Tile newTile = newT.GetComponent<Tile>();
        newTile.targetPos = finalPos;
		newTile.coordinates = tileCoords;
		if (startPos.y != finalPos.y)
       		newTile.StartMoving();
		tiles.Add (tileCoords, newTile);
    }

}
